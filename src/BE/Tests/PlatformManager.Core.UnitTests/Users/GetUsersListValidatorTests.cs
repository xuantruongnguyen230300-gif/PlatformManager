using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.UnitTests.Users;

/// <summary>
/// CONTRACT USER-6 chốt: <c>role</c> ngoài <see cref="Roles.All"/> là LỖI ĐẦU VÀO (ValidationError
/// 400 kèm <c>fields.role</c>), không phải "bỏ qua bộ lọc" cũng không phải "trả danh sách rỗng".
///
/// <para>Test này giữ chính quyết định đó, vì cả hai lựa chọn bị loại đều IM LẶNG: bỏ qua bộ lọc
/// thì một tham số sai chính tả trả về toàn bộ danh sách; trả rỗng thì người quản trị đọc được
/// câu trả lời sai ("không có Admin nào") mà tưởng là đúng. Đổi hành vi này thì phải đổi luôn
/// doc/contracts/users.md §CONTRACT USER-6.</para>
/// </summary>
public sealed class GetUsersListValidatorTests
{
    private readonly GetUsersListValidator _validator = new();

    [Theory]
    [InlineData(Roles.SuperAdmin)]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.User)]
    public void ValidRole_Passes(string role)
        => Assert.True(_validator.Validate(new GetUsersListQuery(Role: role)).IsValid);

    [Theory]
    [InlineData(null)]      // bỏ trống = không lọc
    [InlineData("")]
    [InlineData("   ")]     // khoảng trắng thuần cũng là "không lọc", giống SearchText
    public void EmptyRole_Passes(string? role)
        => Assert.True(_validator.Validate(new GetUsersListQuery(Role: role)).IsValid);

    // ---------- Trần pageSize (finding BE-3, 2026-08-29) ----------

    [Theory]
    [InlineData(1)]
    [InlineData(20)]                                      // mặc định của query
    [InlineData(50)]                                      // tuỳ chọn lớn nhất trên FE
    [InlineData(GetUsersListValidator.MaxPageSize)]       // đúng biên trên — vẫn hợp lệ
    public void PageSizeWithinRange_Passes(int pageSize)
        => Assert.True(_validator.Validate(new GetUsersListQuery(PageSize: pageSize)).IsValid);

    /// <summary>
    /// Ca <c>1_000_000</c> là chính finding BE-3: trước bản này handler chỉ vá <c>PageSize &lt;= 0</c>
    /// nên mọi số dương đều đi thẳng xuống <c>Take()</c> — một request kéo trọn bảng user.
    /// <c>0</c>/số âm nay cũng là LỖI ĐẦU VÀO chứ không còn bị vá âm thầm về 20: giá trị hợp lệ
    /// chỉ được quyết ở MỘT nơi (validator này).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(GetUsersListValidator.MaxPageSize + 1)]
    [InlineData(1_000_000)]
    public void PageSizeOutOfRange_FailsOnPageSizeField(int pageSize)
    {
        var result = _validator.Validate(new GetUsersListQuery(PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(GetUsersListQuery.PageSize), Assert.Single(result.Errors).PropertyName);
    }

    /// <summary>
    /// <c>Page</c> phải kiểm cùng lượt: handler đã bỏ <c>Math.Max(Page, 1)</c>, mà <c>page=0</c>
    /// cho <c>Skip((0-1)*pageSize)</c> — offset ÂM, Npgsql ném lỗi ⇒ 500 thay vì 400.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void PageBelowOne_FailsOnPageField(int page)
    {
        var result = _validator.Validate(new GetUsersListQuery(Page: page));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(GetUsersListQuery.Page), Assert.Single(result.Errors).PropertyName);
    }

    [Fact(DisplayName = "Query mặc định (không tham số nào) vẫn hợp lệ — FE không phải sửa gì")]
    public void DefaultQuery_Passes()
        => Assert.True(_validator.Validate(new GetUsersListQuery()).IsValid);

    [Theory]
    [InlineData("admin")]        // sai casing — cùng luật với CreateUserValidator, so khớp ordinal
    [InlineData("SUPERADMIN")]
    [InlineData("Adminn")]       // sai chính tả: đúng ca mà "bỏ qua bộ lọc" sẽ nuốt mất
    [InlineData("Manager")]
    public void InvalidRole_FailsOnRoleField(string role)
    {
        var result = _validator.Validate(new GetUsersListQuery(Role: role));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(GetUsersListQuery.Role), Assert.Single(result.Errors).PropertyName);
    }
}
