using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using Xunit;

namespace PlatformManager.Core.UnitTests.Permissions;

/// <summary>
/// Bổ trợ cho <c>Tests/PlatformManager.Core.IntegrationTests/Permissions/PermissionMatrixDuplicateKeyTests.cs</c>
/// — bộ đó chốt MÃ TRẠNG THÁI thật (400, không phải 500) qua HTTP, bộ này chốt các chi tiết rẻ
/// tiền hơn nhiều khi kiểm bằng unit test: đúng tên field, đúng nội dung thông điệp, và các ca
/// biên (payload rỗng, thiếu khoá, khác hoa/thường).
///
/// <para>⚠️ Bộ này MỘT MÌNH thì không chứng minh được gì về hành vi của API: nó xanh y hệt kể cả
/// khi không ai gọi validator trước handler — đúng tình trạng hỏng đã sinh ra finding này. Đừng
/// xoá bộ integration kia rồi coi bộ này là đủ.</para>
/// </summary>
public sealed class PermissionMatrixValidatorTests
{
    // Validator nay nhận danh mục key qua DI (tách 2026-09-03) — xem FakeResourceKeySource cho lý
    // do dựng danh mục tại chỗ thay vì mượn danh mục thật của host.
    private readonly UpdateResourcePermissionMatrixValidator _resourceValidator = new(new FakeResourceKeySource());
    private readonly UpdatePermissionMatrixValidator _menuValidator = new();

    // ── PERM-2: resourceKey ──────────────────────────────────────────────────

    [Fact(DisplayName = "PERM-2: resourceKey lặp → lỗi trên field 'Entries', thông điệp nêu đích danh key")]
    public void DuplicateResourceKey_FailsOnEntriesField()
    {
        var result = _resourceValidator.Validate(new UpdateResourcePermissionMatrixCommand(
        [
            new(FakeResourceKeySource.Key, [Roles.Admin]),
            new(FakeResourceKeySource.Key, [Roles.User]),
        ]));

        Assert.False(result.IsValid);

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateResourcePermissionMatrixCommand.Entries), failure.PropertyName);

        // Nêu đích danh key bị lặp: ma trận có thể dài hàng chục dòng, thông điệp chung chung
        // ("dữ liệu không hợp lệ") không giúp ai sửa được payload.
        Assert.Contains(FakeResourceKeySource.Key, failure.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "PERM-2: entries hợp lệ, không trùng → qua")]
    public void UniqueResourceKeys_Passes()
        => Assert.True(_resourceValidator.Validate(new UpdateResourcePermissionMatrixCommand(
            [new(FakeResourceKeySource.Key, [Roles.Admin, Roles.User])])).IsValid);

    [Fact(DisplayName = "PERM-2: entries rỗng → qua (thu hồi TOÀN BỘ ma trận, hành vi đã công bố)")]
    public void EmptyEntries_Passes()
        => Assert.True(_resourceValidator.Validate(new UpdateResourcePermissionMatrixCommand([])).IsValid);

    /// <summary>
    /// Body <c>{}</c> cho <c>Entries = null</c>. Trước bản vá, <c>cmd.Entries.ToDictionary(...)</c>
    /// ném <see cref="NullReferenceException"/> ⇒ 500 — CÙNG lớp lỗi với key trùng.
    /// <c>Cascade(Stop)</c> ở validator là thứ giữ cho chính rule chống trùng không ném NRE khi
    /// chạy trên <c>null</c>: bỏ nó đi thì bản vá tự tái tạo lại đúng con 500 nó đi sửa.
    /// </summary>
    [Fact(DisplayName = "PERM-2: entries null → lỗi 'Entries', KHÔNG ném exception trong validator")]
    public void NullEntries_FailsWithoutThrowing()
    {
        var result = _resourceValidator.Validate(new UpdateResourcePermissionMatrixCommand(null!));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(UpdateResourcePermissionMatrixCommand.Entries), Assert.Single(result.Errors).PropertyName);
    }

    [Fact(DisplayName = "PERM-2: entry thiếu roles (null) → lỗi 'Entries[0].Roles', không phải 500")]
    public void NullRoles_FailsOnRolesField()
    {
        var result = _resourceValidator.Validate(new UpdateResourcePermissionMatrixCommand(
            [new(FakeResourceKeySource.Key, null!)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Entries[0].Roles");
    }

    /// <summary>
    /// So khớp trùng lặp phải ORDINAL — đúng bằng comparer mặc định của <c>ToDictionary</c>. Hai
    /// key khác hoa/thường KHÔNG trùng nhau với <c>ToDictionary</c>, nên validator cũng không được
    /// coi là trùng; ở đây chúng bị chặn vì lý do KHÁC (không nằm trong danh mục key),
    /// và test khẳng định đúng lý do đó — nếu validator báo "bị lặp" thì nó đang nghiêm hơn thứ
    /// nó bảo vệ.
    /// </summary>
    [Fact(DisplayName = "PERM-2: hai key khác hoa/thường KHÔNG bị coi là trùng (khớp comparer của ToDictionary)")]
    public void CaseDifferingKeys_AreNotTreatedAsDuplicates()
    {
        var result = _resourceValidator.Validate(new UpdateResourcePermissionMatrixCommand(
        [
            new(FakeResourceKeySource.Key, [Roles.Admin]),
            new(FakeResourceKeySource.Key.ToUpperInvariant(), [Roles.Admin]),
        ]));

        Assert.False(result.IsValid);
        Assert.DoesNotContain(
            result.Errors,
            e => e.PropertyName == nameof(UpdateResourcePermissionMatrixCommand.Entries));
        Assert.Contains(result.Errors, e => e.PropertyName == "Entries[1].ResourceKey");
    }

    // ── PERM-1: sysMenuId ────────────────────────────────────────────────────

    [Fact(DisplayName = "PERM-1: sysMenuId lặp → lỗi trên field 'Entries', thông điệp nêu đích danh id")]
    public void DuplicateSysMenuId_FailsOnEntriesField()
    {
        var duplicated = Guid.NewGuid();

        var result = _menuValidator.Validate(new UpdatePermissionMatrixCommand(
        [
            new(duplicated, [Roles.Admin]),
            new(duplicated, [Roles.User]),
        ]));

        Assert.False(result.IsValid);

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdatePermissionMatrixCommand.Entries), failure.PropertyName);
        Assert.Contains(duplicated.ToString(), failure.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "PERM-1: hai sysMenuId khác nhau → qua")]
    public void UniqueSysMenuIds_Passes()
        => Assert.True(_menuValidator.Validate(new UpdatePermissionMatrixCommand(
        [
            new(Guid.NewGuid(), [Roles.Admin]),
            new(Guid.NewGuid(), [Roles.User]),
        ])).IsValid);

    [Fact(DisplayName = "PERM-1: entries null → lỗi 'Entries', KHÔNG ném exception trong validator")]
    public void NullEntries_OnMenuMatrix_FailsWithoutThrowing()
    {
        var result = _menuValidator.Validate(new UpdatePermissionMatrixCommand(null!));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(UpdatePermissionMatrixCommand.Entries), Assert.Single(result.Errors).PropertyName);
    }

    [Fact(DisplayName = "PERM-1: entry thiếu roles (null) → lỗi 'Entries[0].Roles', không phải 500")]
    public void NullRoles_OnMenuMatrix_FailsOnRolesField()
    {
        var result = _menuValidator.Validate(new UpdatePermissionMatrixCommand(
            [new(Guid.NewGuid(), null!)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Entries[0].Roles");
    }
}
