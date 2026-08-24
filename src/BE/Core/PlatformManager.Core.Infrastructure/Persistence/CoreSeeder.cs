using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Seed role/2 tài khoản bootstrap (<c>SuperAdmin</c> + <c>Admin</c>, mỗi tài khoản MỘT
/// role)/SysMenu/SysMenuRole — DML, idempotent ("chưa có thì thêm"), chỉ được gọi khi
/// IsDevelopment() (xem Program.cs). KHÔNG seed dữ liệu đặc thù Module nào — xem
/// PlatformManager.Modules.DtiWeekly.Infrastructure/Persistence/DtiWeeklySeeder.cs cho danh
/// mục CSV chỉ tiêu. Gọi CoreSeeder TRƯỚC mọi ModuleSeeder khác (Module có thể cần role/user đã
/// tồn tại — xem doc/kien-truc-core-module.md). Mật khẩu 2 tài khoản đọc từ
/// <see cref="BootstrapOptions"/> (fail-fast — xem DependencyInjection.AddCoreModule), KHÔNG
/// hardcode — xem scripts/setup-database.sh bước 5/5 cho điều kiện cấu hình.
/// </summary>
public sealed class CoreSeeder(
    PlatformManagerDbContext db,
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IOptions<BootstrapOptions> bootstrapOptions,
    ILogger<CoreSeeder> logger)
{
    private const string SuperAdminUserName = "SuperAdmin";
    private const string AdminUserName = "Admin";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedRolePermissionsAsync(ct);
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

    /// <summary>
    /// `RequirePermissionFilter` là deny-by-default — bảng `RolePermissions` rỗng nghĩa là MỌI
    /// role (trừ SuperAdmin bypass) bị 403 ở Criteria/CriteriaGroups/Import ngay khi
    /// [RequirePermission] gắn lên controller. Cấp đủ 3 <see cref="ResourceKeys.All"/> cho
    /// Admin + User để GIỮ NGUYÊN hành vi trước khi vá (mọi user thao tác được) — xem
    /// doc/contracts/permissions.md §"Rủi ro rollout" + doc/huong_dan/wiki-core/be/
    /// 13-core-data-migration.md. SuperAdmin KHÔNG cần dòng nào ở đây (break-glass ở
    /// RequirePermissionFilter). CHỈ chạy Development (IsDevelopment() gate ở Program.cs) —
    /// production seed tương đương ở scripts/seed-role-permissions.sql, KHÔNG dựa vào seeder này.
    /// </summary>
    private async Task SeedRolePermissionsAsync(CancellationToken ct)
    {
        foreach (var roleName in new[] { Roles.Admin, Roles.User })
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue; // SeedRolesAsync chạy trước nên bình thường không xảy ra — phòng thủ nếu thứ tự đổi

            foreach (var resourceKey in ResourceKeys.All)
            {
                var exists = await db.RolePermissions
                    .AnyAsync(x => x.RoleId == role.Id && x.ResourceKey == resourceKey, ct);
                if (!exists)
                    await db.RolePermissions.AddAsync(RolePermission.Create(role.Id, resourceKey), ct);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>2 tài khoản RIÊNG BIỆT, mỗi tài khoản MỘT role — SuperAdmin thuần break-glass,
    /// Admin thuần vận hành hàng ngày. Khác thiết kế trước đó (1 tài khoản gộp cả 2 role) — quyết
    /// định người dùng 2026-08-24, xem doc/huong_dan/wiki-core/be/13-core-data-migration.md
    /// §"✅ Quyết định người dùng 2026-08-24 — tách tài khoản bootstrap SuperAdmin/Admin" (lý do:
    /// least-privilege, SuperAdmin là break-glass không thu hồi được qua UI). Cũng xem
    /// scripts/setup-database.sh bước 5/5 ("mỗi tài khoản MỘT role") và docstring
    /// BootstrapOptions ("2 tài khoản quản trị").</summary>
    private async Task SeedBootstrapUserAsync(CancellationToken ct)
    {
        await SeedBootstrapAccountAsync(
            SuperAdminUserName, "superadmin@platformmanager.local", "Quản trị viên hệ thống",
            bootstrapOptions.Value.SuperAdminPassword, Roles.SuperAdmin, ct);

        await SeedBootstrapAccountAsync(
            AdminUserName, "admin@platformmanager.local", "Quản trị viên",
            bootstrapOptions.Value.AdminPassword, Roles.Admin, ct);
    }

    private async Task SeedBootstrapAccountAsync(
        string userName, string email, string fullName, string password, string role, CancellationToken ct)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
            return;

        var user = new AppUser
        {
            Id = EntityId.New(),
            UserName = userName,
            Email = email, // quy ước đặt tên, không phải validate cứng
            FullName = fullName,
            MustChangePassword = true,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Seed tài khoản bootstrap '{UserName}' thất bại: {Errors}",
                userName, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRolesAsync(user, [role]);
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
