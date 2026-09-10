using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using Xunit;

namespace PlatformManager.Core.UnitTests.Permissions;

/// <summary>
/// <see cref="ResourceKeyDefinition.SeedRoles"/> — vai được cấp key ở LẦN SEED ĐẦU, mở rộng
/// 2026-09-09 để host khai được một key chỉ dành cho vai quản trị mà Core không phải biết tên key
/// nghiệp vụ nào.
///
/// <para><b>Test quan trọng nhất của bộ này là test MẶC ĐỊNH.</b> Thay đổi vừa rồi chạm vào đường
/// seed dùng chung: nếu mặc định trượt khỏi <c>[Admin, User]</c> thì quyền của MỌI key hiện có đổi
/// cùng lúc, và triệu chứng là 403 ở nơi trước đó chạy được — không có lỗi biên dịch, không test
/// nào khác đỏ.</para>
///
/// <para>Ba guard còn lại đều chặn ca hỏng IM LẶNG (seed vẫn thoát 0, chỉ ma trận quyền khác thứ
/// người khai định làm), nên chúng phải được canh bằng test chứ không bằng review.</para>
/// </summary>
public class ResourceKeySeedRolesTests
{
    private sealed class StubSource(params ResourceKeyDefinition[] definitions) : ICoreResourceKeySource
    {
        public IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys() => definitions;
    }

    [Fact(DisplayName = "Không khai SeedRoles → mặc định ĐÚNG [Admin, User], KHÔNG có SuperAdmin")]
    public void Default_IsAdminAndUser_Exactly()
    {
        var definition = new ResourceKeyDefinition("x.manage", "Nhãn");

        Assert.Equal([Roles.Admin, Roles.User], definition.SeedRoles);

        // Chiều ngược, bắt buộc: một mặc định "gồm cả ba vai" vẫn qua được phép so nếu chỉ kiểm
        // Contains — mà thêm SuperAdmin vào đây là đẻ ra dòng chết trong RolePermissions.
        Assert.DoesNotContain(Roles.SuperAdmin, definition.SeedRoles);
    }

    [Fact(DisplayName = "Host thu hẹp SeedRoles → giữ đúng danh sách khai, Catalog() không sửa lại")]
    public void NarrowedSeedRoles_SurviveCatalog()
    {
        var catalog = new StubSource(
            new ResourceKeyDefinition("hep.manage", "Hẹp") { SeedRoles = [Roles.Admin] },
            new ResourceKeyDefinition("rong.manage", "Rộng")).Catalog();

        Assert.Equal([Roles.Admin], catalog.Single(d => d.Key == "hep.manage").SeedRoles);
        Assert.Equal([Roles.Admin, Roles.User], catalog.Single(d => d.Key == "rong.manage").SeedRoles);
    }

    [Fact(DisplayName = "SeedRoles rỗng → ném, nêu đích danh key và hậu quả 403")]
    public void EmptySeedRoles_Throws()
    {
        var source = new StubSource(new ResourceKeyDefinition("rong-khong.manage", "Nhãn") { SeedRoles = [] });

        var ex = Assert.Throws<InvalidOperationException>(() => source.Catalog());

        Assert.Contains("rong-khong.manage", ex.Message, StringComparison.Ordinal);
        Assert.Contains("403", ex.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SeedRoles khai tên vai lạ → ném, nêu tên sai VÀ tập vai hợp lệ")]
    public void UnknownRole_Throws()
    {
        const string Typo = "Admn"; // thiếu chữ 'i' của "Admin"
        var source = new StubSource(new ResourceKeyDefinition("go-nham.manage", "Nhãn") { SeedRoles = [Typo] });

        var ex = Assert.Throws<InvalidOperationException>(() => source.Catalog());

        Assert.Contains(Typo, ex.Message, StringComparison.Ordinal);
        Assert.Contains(Roles.Admin, ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// So khớp ORDINAL ở mọi nơi — <c>"admin"</c> không phải <c>Roles.Admin</c>. Không có guard
    /// này, một key khai chữ thường sẽ seed cho KHÔNG vai nào (seeder so ordinal) trong khi người
    /// khai tin là đã cấp cho Admin.
    /// </summary>
    [Fact(DisplayName = "SeedRoles khai sai hoa/thường → ném (so khớp ORDINAL)")]
    public void WrongCasedRole_Throws()
    {
        var source = new StubSource(new ResourceKeyDefinition("hoa-thuong.manage", "Nhãn") { SeedRoles = ["admin"] });

        Assert.Throws<InvalidOperationException>(() => source.Catalog());
    }

    [Fact(DisplayName = "SeedRoles khai SuperAdmin → ném (break-glass, dòng seed là dòng chết)")]
    public void SuperAdminInSeedRoles_Throws()
    {
        var source = new StubSource(
            new ResourceKeyDefinition("thua.manage", "Nhãn") { SeedRoles = [Roles.SuperAdmin, Roles.Admin] });

        var ex = Assert.Throws<InvalidOperationException>(() => source.Catalog());

        Assert.Contains(Roles.SuperAdmin, ex.Message, StringComparison.Ordinal);
        Assert.Contains("thua.manage", ex.Message, StringComparison.Ordinal);
    }
}
