using NSubstitute;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.CriteriaGroups;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Đường nối của DB-1, DB-3 và DM-1 — cùng lý do và cùng ranh giới với
/// <see cref="GetCriteriaListHandlerTests"/>.
/// </summary>
public class DashboardHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);   // thứ Tư, tuần ISO 33

    private readonly IDashboardRepository _repository = Substitute.For<IDashboardRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public DashboardHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        _repository.GetCriteriaAsync(Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetAssessmentFactsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetYearsWithDataAsync(Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetTableAsync(Arg.Any<CriteriaFilterSpec>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetDashboardHandler CreateDashboardHandler() => new(_repository, _clock);

    private GetPeriodOptionsHandler CreatePeriodsHandler() => new(_repository, _clock);

    // ── DB-1 ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Week_WithoutParameters_Returns_CurrentWeek()
    {
        var result = await CreateDashboardHandler().Handle(new GetDashboardQuery(Mode: "week"), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var data = Assert.IsType<DashboardAggregateDto>(result.Data);
        Assert.Equal("week", data.Mode);
        Assert.Equal("Tuần 33/2026 (10/08 – 16/08/2026)", data.PeriodLabel);
        Assert.Equal(new DateOnly(2026, 8, 10), data.PeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 16), data.PeriodEnd);
    }

    /// <summary>
    /// Ca "chưa có dữ liệu nào" (§0): <b>200</b>, KHÔNG phải 404. `groups`/`table` rỗng; `trend`
    /// có ĐỦ các kỳ nhưng không kỳ nào mang `value` (Q44); các trường số tổng hợp VẮNG MẶT.
    /// </summary>
    [Fact]
    public async Task NoData_Returns_Success_WithAbsentAggregatesAndFullTrend()
    {
        var result = await CreateDashboardHandler().Handle(new GetDashboardQuery(Mode: "week"), CancellationToken.None);

        var data = Assert.IsType<DashboardAggregateDto>(result.Data);

        Assert.Null(data.Kpi.OverallProgress);
        Assert.Null(data.Kpi.Delta);
        Assert.Null(data.Kpi.PreviousPeriodLabel);
        Assert.Equal(0, data.Kpi.Up);
        Assert.Equal(0, data.Kpi.Flat);
        Assert.Equal(0, data.Kpi.Done);
        Assert.Empty(data.Table);

        // Đủ 12 kỳ, không kỳ nào có value — KHÔNG phải mảng rỗng.
        Assert.Equal(DashboardPeriods.WeekTrendWindow, data.Trend.Count);
        Assert.All(data.Trend, point => Assert.Null(point.Value));
        Assert.All(data.Trend, point => Assert.False(string.IsNullOrWhiteSpace(point.PeriodLabel)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Week")]
    [InlineData("tuan")]
    public async Task InvalidMode_Fails_WithModeInvalid(string? mode)
    {
        var result = await CreateDashboardHandler().Handle(new GetDashboardQuery(Mode: mode), CancellationToken.None);

        Assert.Equal(DashboardErrors.ModeInvalid.BusinessCode, result.BusinessCode);
        Assert.Equal(ErrorCode.ValidationError, result.Code);
    }

    [Fact]
    public async Task UnknownStatus_Fails_WithDashboardStatusInvalid()
    {
        var result = await CreateDashboardHandler().Handle(
            new GetDashboardQuery(Mode: "week", Status: "Đã xong"), CancellationToken.None);

        Assert.Equal(DashboardErrors.StatusInvalid.BusinessCode, result.BusinessCode);
    }

    /// <summary>Q61 — gửi CẢ <c>date</c> lẫn <c>year</c> mà lệch năm ISO ⇒ 400.</summary>
    [Fact]
    public async Task Week_WithMismatchedDateAndYear_Fails_WithPeriodYearMismatch()
    {
        var result = await CreateDashboardHandler().Handle(
            new GetDashboardQuery(Mode: "week", Date: new DateOnly(2025, 12, 29), Year: 2025),
            CancellationToken.None);

        Assert.Equal(DashboardErrors.PeriodYearMismatch.BusinessCode, result.BusinessCode);
        Assert.Equal(ErrorCode.ValidationError, result.Code);
    }

    /// <summary>
    /// <b>Q71 qua HANDLER</b> — <c>year</c> của một năm chưa có dữ liệu, không <c>date</c>: phải
    /// ra <c>200</c> và trỏ vào tuần ISO CUỐI của năm đó. Đây là nghiệm thu nguyên văn của khối
    /// Q71 trong hợp đồng, và là ca mà bản cài trước trả 400.
    /// </summary>
    [Fact]
    public async Task Week_WithYearOnly_Returns_LastIsoWeekOfThatYear_NotAnError()
    {
        var result = await CreateDashboardHandler().Handle(
            new GetDashboardQuery(Mode: "week", Year: 2025), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var data = Assert.IsType<DashboardAggregateDto>(result.Data);
        Assert.Equal("Tuần 52/2025 (22/12 – 28/12/2025)", data.PeriodLabel);
        Assert.Equal(DashboardPeriods.WeekTrendWindow, data.Trend.Count);
    }

    /// <summary>Q63 — bỏ trống <c>year</c> thì lấy năm ISO của tuần chứa <c>date</c>, không 400.</summary>
    [Fact]
    public async Task Week_WithDateOnly_TakesIsoYearOfThatDate()
    {
        var result = await CreateDashboardHandler().Handle(
            new GetDashboardQuery(Mode: "week", Date: new DateOnly(2025, 12, 29)), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var data = Assert.IsType<DashboardAggregateDto>(result.Data);
        Assert.StartsWith("Tuần 1/2026", data.PeriodLabel, StringComparison.Ordinal);

        // Q57 — cửa sổ cắt ở đầu năm: xem tuần 1 thì trục chỉ có đúng một điểm.
        Assert.Single(data.Trend);
    }

    /// <summary>
    /// Luật 1 của DB-1: bộ lọc CHỈ áp cho <c>table</c>. Ca này chứng minh bộ lọc thật sự đi
    /// xuống đường bảng chi tiết, còn `kpi`/`groups`/`trend` thì đọc từ tập chỉ tiêu KHÔNG lọc.
    /// </summary>
    [Fact]
    public async Task TableFilters_ReachOnly_TheTableQuery()
    {
        var groupId = Guid.CreateVersion7();

        await CreateDashboardHandler().Handle(
            new GetDashboardQuery(Mode: "week", GroupId: groupId, Search: "Đào tạo"), CancellationToken.None);

        await _repository.Received(1).GetTableAsync(
            Arg.Is<CriteriaFilterSpec>(spec => spec.GroupId == groupId && spec.SearchNormalized == "dao tao"),
            Arg.Any<CancellationToken>());

        // GetCriteriaAsync KHÔNG nhận bộ lọc nào — đó là điều kiện để "Tiến độ chung" không đổi
        // theo ô lọc nhóm.
        await _repository.Received(1).GetCriteriaAsync(Arg.Any<CancellationToken>());
    }

    // ── DB-3 / DM-8 ─────────────────────────────────────────────────────

    /// <summary>
    /// Năm KHÔNG có dữ liệu ⇒ <c>200</c> + hai mảng RỖNG, và <c>years</c> vẫn LUÔN kèm năm hiện
    /// tại. Đây là điểm dễ làm sai nhất của DB-3: trả 404/400 sẽ khoá cứng ô <c>Năm</c> của người
    /// dùng ngay lần đầu họ mở một năm mới.
    /// </summary>
    [Fact]
    public async Task Periods_ForYearWithoutData_Returns_EmptyArrays_AndAlwaysCurrentYear()
    {
        var result = await CreatePeriodsHandler().Handle(new GetPeriodOptionsQuery(Year: 2030), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);

        var data = Assert.IsType<PeriodOptionsDto>(result.Data);
        Assert.Empty(data.WeeksInYear);
        Assert.Empty(data.MonthsInYear);
        Assert.Contains(2026, data.Years);
    }

    /// <summary>
    /// <c>years</c> gộp năm có dữ liệu với năm hiện tại, không trùng lặp, tăng dần — và
    /// <c>weeksInYear</c> chỉ chứa tuần thuộc đúng NĂM ISO đang hỏi.
    /// </summary>
    [Fact]
    public async Task Periods_ListsYearsWithData_PlusCurrentYear()
    {
        var criteriaId = Guid.CreateVersion7();
        _repository.GetYearsWithDataAsync(Arg.Any<CancellationToken>()).Returns([2024, 2026]);
        _repository.GetAssessmentFactsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new AssessmentFact(criteriaId, new DateOnly(2026, 8, 12), 40, null)]);

        var result = await CreatePeriodsHandler().Handle(new GetPeriodOptionsQuery(), CancellationToken.None);

        var data = Assert.IsType<PeriodOptionsDto>(result.Data);
        Assert.Equal([2024, 2026], data.Years);

        var week = Assert.Single(data.WeeksInYear);
        Assert.Equal("2026-W33", week.Value);
        Assert.Equal(new DateOnly(2026, 8, 10), week.Date);

        var month = Assert.Single(data.MonthsInYear);
        Assert.Equal("2026-08", month.Value);
    }

    // ── DM-1 ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CriteriaGroups_Returns_WhateverRepositoryGives_InOrder()
    {
        var repository = Substitute.For<ICriteriaGroupRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new CriteriaGroupDto(Guid.CreateVersion7(), "1", "Hạ tầng và Nền tảng số", 1),
            new CriteriaGroupDto(Guid.CreateVersion7(), "2", "Nhân lực số", 2),
        ]);

        var result = await new GetCriteriaGroupsListHandler(repository)
            .Handle(new GetCriteriaGroupsListQuery(), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Collection(
            Assert.IsAssignableFrom<IReadOnlyList<CriteriaGroupDto>>(result.Data),
            first => Assert.Equal("1", first.Code),
            second => Assert.Equal("2", second.Code));
    }

    [Fact]
    public async Task CriteriaGroups_EmptyCatalog_IsSuccess_NotAnError()
    {
        var repository = Substitute.For<ICriteriaGroupRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await new GetCriteriaGroupsListHandler(repository)
            .Handle(new GetCriteriaGroupsListQuery(), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<CriteriaGroupDto>>(result.Data));
    }
}
