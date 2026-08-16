using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Seed role/tài khoản bootstrap/SysMenu/SysMenuRole — DML, idempotent ("chưa có thì thêm"),
/// chỉ được gọi khi IsDevelopment() (xem Program.cs). KHÔNG seed dữ liệu đặc thù Module nào —
/// xem PlatformManager.Modules.DtiWeekly.Infrastructure/Persistence/DtiWeeklySeeder.cs cho danh
/// mục CSV chỉ tiêu. Gọi CoreSeeder TRƯỚC mọi ModuleSeeder khác (Module có thể cần role/user đã
/// tồn tại — xem doc/kien-truc-core-module.md).
/// </summary>
public sealed class CoreSeeder(
    PlatformManagerDbContext db,
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    ILogger<CoreSeeder> logger)
{
    private const string BootstrapUserName = "SuperAdmin";
    // Mật khẩu tạm HARDCODE — chỉ dùng demo/bootstrap local, bắt buộc đổi ngay lần đăng nhập
    // đầu (MustChangePassword=true). KHÔNG dùng giá trị này cho môi trường thật.
    private const string BootstrapPassword = "SuperAdmin@123";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedBootstrapUserAsync(ct);
        await SeedMenuAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        foreach (var roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            await roleManager.CreateAsync(new AppRole(roleName) { Id = EntityId.New() });
        }
    }

    private async Task SeedBootstrapUserAsync(CancellationToken ct)
    {
        if (await userManager.FindByNameAsync(BootstrapUserName) is not null)
            return;

        var user = new AppUser
        {
            Id = EntityId.New(),
            UserName = BootstrapUserName,
            Email = "superadmin@platformmanager.local", // quy ước đặt tên, không phải validate cứng
            FullName = "Quản trị viên hệ thống",
            MustChangePassword = true,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, BootstrapPassword);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Seed tài khoản bootstrap '{UserName}' thất bại: {Errors}",
                BootstrapUserName, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRolesAsync(user, [Roles.SuperAdmin, Roles.Admin]);
    }

    private async Task SeedMenuAsync(CancellationToken ct)
    {
        // Icon trả THẲNG class PrimeIcons thật (không qua bảng khoá trừu tượng nào) — FE
        // (shared/components/sidebar/sidebar.ts) dùng nguyên item.Icon làm class CSS, chỉ
        // fallback khi null. Xem doc/contracts/meta-menu.md §Icon.
        var dashboard = await UpsertMenuAsync("dashboard", "Dashboard", "/dashboard", "pi-th-large", null, 1, ct);
        var danhMuc = await UpsertMenuAsync("danh-muc", "Danh mục", null, "pi-folder", null, 2, ct);
        var dti = await UpsertMenuAsync("danh-muc-dti", "DTI", "/danh-muc/dti", "pi-list", danhMuc.Id, 1, ct);
        var quanTri = await UpsertMenuAsync("quan-tri", "Quản trị hệ thống", null, "pi-cog", null, 3, ct);
        var nguoiDung = await UpsertMenuAsync("sys-user", "Người dùng", "/quan-tri/nguoi-dung", "pi-user", quanTri.Id, 1, ct);
        var phanQuyen = await UpsertMenuAsync("phan-quyen", "Phân quyền", "/quan-tri/phan-quyen", "pi-shield", quanTri.Id, 2, ct);
        _ = dashboard;
        _ = dti;

        await db.SaveChangesAsync(ct);

        // Dashboard + Danh mục DTI: KHÔNG gán role -> mở cho mọi user đã đăng nhập.
        await UpsertMenuRoleAsync(quanTri.Id, [Roles.SuperAdmin, Roles.Admin], ct);
        await UpsertMenuRoleAsync(nguoiDung.Id, [Roles.SuperAdmin, Roles.Admin], ct);
        await UpsertMenuRoleAsync(phanQuyen.Id, [Roles.SuperAdmin], ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task<SysMenu> UpsertMenuAsync(
        string code, string name, string? route, string? icon, Guid? parentId, int displayOrder, CancellationToken ct)
    {
        var existing = await db.SysMenus.FirstOrDefaultAsync(m => m.Code == code, ct);
        if (existing is not null)
            return existing;

        var menu = SysMenu.Create(code, name, route, icon, parentId, displayOrder);
        await db.SysMenus.AddAsync(menu, ct);
        return menu;
    }

    private async Task UpsertMenuRoleAsync(Guid sysMenuId, string[] roleNames, CancellationToken ct)
    {
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            var exists = await db.SysMenuRoles.AnyAsync(x => x.SysMenuId == sysMenuId && x.RoleId == role.Id, ct);
            if (!exists)
                await db.SysMenuRoles.AddAsync(SysMenuRole.Create(sysMenuId, role.Id), ct);
        }
    }
}
