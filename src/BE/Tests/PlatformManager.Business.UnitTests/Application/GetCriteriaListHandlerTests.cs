using NSubstitute;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Permissions;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>ĐƯỜNG NỐI</b> của DM-2 — phần mà bộ test logic thuần KHÔNG chạm tới: handler chọn
/// <c>ErrorDescriptor</c> nào cho từng nhánh hỏng, và lắp <c>CriteriaGridDto</c> ra sao.
///
/// <para>Không cần DB: <c>ICriteriaGridRepository</c> là interface, <c>IDtiWriteAccess</c> và
/// <c>IDateTimeProvider</c> cũng vậy. Đây đúng ranh giới mà
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md xếp vào unit test — phần cần Postgres thật
/// (EF dịch LINQ sang SQL, ràng buộc unique) thuộc integration test.</para>
///
/// <para><b>Vì sao đường nối cần test riêng dù logic đã phủ:</b> một handler gọi đúng hàm nhưng
/// trả nhầm mã lỗi, hoặc quên gán một trường của DTO, thì mọi test logic vẫn xanh. Ở đây cả hai
/// thứ đó đều là hợp đồng với FE.</para>
/// </summary>
public class GetCriteriaListHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);   // thứ Tư, tuần ISO 33

    private readonly ICriteriaGridRepository _repository = Substitute.For<ICriteriaGridRepository>();
    private readonly IDtiWriteAccess _writeAccess = Substitute.For<IDtiWriteAccess>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public GetCriteriaListHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        _repository.GetPageAsync(Arg.Any<CriteriaFilterSpec>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CriteriaGridPage([], 0));
    }

    private GetCriteriaListHandler CreateHandler() => new(_repository, _writeAccess, _clock);

    [Fact]
    public async Task Returns_Grid_WithPagingAndAccessBlock()
    {
        _writeAccess.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new GetCriteriaListQuery(Period: "2026-W33", Page: 2, PageSize: 25), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var grid = Assert.IsType<CriteriaGridDto>(result.Data);
        Assert.Equal(2, grid.Page);
        Assert.Equal(25, grid.PageSize);
        Assert.True(grid.CanWrite);
        Assert.True(grid.IsEditable);
        Assert.Empty(grid.EditBlockedBy);
        Assert.True(grid.IsCurrentYear);
    }

    /// <summary>
    /// Q72 — hai trường kỳ hiện tại phải có mặt ngay cả ở ca LƯỚI RỖNG và KHÔNG CÓ QUYỀN. Đây
    /// đúng là nghiệm thu mà hợp đồng nêu: gọi trên DB chưa import lần nào, bằng tài khoản không
    /// quyền.
    /// </summary>
    [Fact]
    public async Task EmptyGrid_WithoutPermission_StillCarries_CurrentPeriod()
    {
        _writeAccess.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new GetCriteriaListQuery(), CancellationToken.None);

        var grid = Assert.IsType<CriteriaGridDto>(result.Data);
        Assert.Empty(grid.Items);
        Assert.False(grid.CanWrite);
        Assert.Equal([EditBlockReasons.NoWritePermission], grid.EditBlockedBy);
        Assert.Equal("2026-W33", grid.CurrentPeriod);
        Assert.Equal("Tuần 33/2026 (10/08 – 16/08/2026)", grid.CurrentPeriodLabel);
    }

    /// <summary>
    /// <c>status</c> lạ ⇒ <c>400 CRITERIA.STATUS_INVALID</c>, KHÔNG âm thầm bỏ lọc. Ca này còn
    /// khẳng định repository <b>không được gọi</b> — trả 200 với bộ lọc bị lờ đi là ca hỏng người
    /// dùng không có cách nào phát hiện.
    /// </summary>
    [Fact]
    public async Task UnknownStatus_Fails_WithStatusInvalid_AndDoesNotQuery()
    {
        _writeAccess.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new GetCriteriaListQuery(Status: "Đã xong"), CancellationToken.None);

        Assert.Equal(CriteriaErrors.StatusInvalid.BusinessCode, result.BusinessCode);
        Assert.Equal(ErrorCode.ValidationError, result.Code);
        Assert.Null(result.Data);

        await _repository.DidNotReceive().GetPageAsync(
            Arg.Any<CriteriaFilterSpec>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Trạng thái hợp lệ nhưng khác hoa/thường vẫn đi qua (§4 luật 4) — ca đối chứng của ca trên.</summary>
    [Fact]
    public async Task KnownStatus_InAnyCasing_IsAccepted()
    {
        _writeAccess.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new GetCriteriaListQuery(Status: "hoàn thành"), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        // Và nó được quy về ĐÚNG giá trị chuẩn trước khi xuống repository — nếu không, phép so
        // ordinal ở tầng SQL sẽ không khớp dòng nào.
        await _repository.Received(1).GetPageAsync(
            Arg.Is<CriteriaFilterSpec>(spec => spec.Status == AssessmentStatuses.Done),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary><c>period</c> sai khuôn ⇒ <c>400 CRITERIA.ASSESSMENT_PERIOD_INVALID</c>, không rơi về <c>"all"</c>.</summary>
    [Theory]
    [InlineData("2026-W99")]
    [InlineData("tuần 33")]
    [InlineData("2026-13")]
    public async Task MalformedPeriod_Fails_WithPeriodInvalid(string period)
    {
        _writeAccess.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new GetCriteriaListQuery(Period: period), CancellationToken.None);

        Assert.Equal(CriteriaErrors.AssessmentPeriodInvalid.BusinessCode, result.BusinessCode);
        Assert.Equal(ErrorCode.ValidationError, result.Code);
    }

    /// <summary>
    /// Bộ lọc đi xuống repository phải mang kỳ ĐÃ QUY ĐỔI, và từ khoá đã chuẩn hoá cho CẢ hai
    /// nhánh khớp (tên không dấu · mã nguyên văn) — Q47 + Q59.
    /// </summary>
    [Fact]
    public async Task Search_ReachesRepository_NormalizedForBothMatchingBranches()
    {
        _writeAccess.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(true);

        await CreateHandler().Handle(
            new GetCriteriaListQuery(Search: "  Đào Tạo  ", Year: 2026, Period: "2026-W33"), CancellationToken.None);

        await _repository.Received(1).GetPageAsync(
            Arg.Is<CriteriaFilterSpec>(spec =>
                spec.SearchNormalized == "dao tao"
                && spec.SearchCode == "Đào Tạo"
                && spec.Period.Value == "2026-W33"),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
