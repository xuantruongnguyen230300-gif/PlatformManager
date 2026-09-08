using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Menu;

namespace PlatformManager.Api.Seeding;

/// <summary>
/// Dữ liệu menu Core của CHÍNH dự án PlatformManager — nhãn tiếng Việt, route FE, class
/// PrimeIcons, và role được thấy từng mục.
///
/// <para><b>Vì sao ở host chứ không ở Core</b> (chuyển 2026-09-02): host là nơi DUY NHẤT được
/// biết dữ liệu riêng của dự án này. Ba thứ trong bảng dưới — nhãn, route, icon — đều chắc chắn
/// khác ở dự án thứ hai dựng trên cùng CoreBase; để chúng trong
/// <c>Core.Infrastructure/Persistence/CoreSeeder.cs</c> nghĩa là muốn tái dùng Core thì phải mổ
/// vào trong Core. Cơ chế seed (upsert theo <c>Code</c>, hồi sinh dòng đã xoá mềm, gán
/// <c>SysMenuRole</c>) vẫn ở Core — xem <see cref="ICoreMenuSeedSource"/>.</para>
///
/// <para><b>Menu NGHIỆP VỤ không khai ở đây.</b> Khi module nghiệp vụ đầu tiên được dựng lại, nó
/// tự có seeder riêng đóng góp mục menu của nó (doc/kien-truc-core-module.md). Bảng này chỉ giữ
/// phần điều hướng Core: trang chủ + nhánh quản trị hệ thống.</para>
///
/// <para>Đổi bảng này là đổi hợp đồng <c>GET /api/meta/menu</c> — cập nhật
/// doc/contracts/meta-menu.md cùng lượt, và đối chiếu <c>route</c> với
/// <c>src/FE/src/app/app.routes.ts</c>.</para>
/// </summary>
internal sealed class AppMenuSeedSource : ICoreMenuSeedSource
{
    /// <summary>
    /// Danh sách PHẲNG — quan hệ cha-con qua <c>ParentCode</c>, không lồng cấu trúc.
    ///
    /// <para><b>Icon là class CSS THẬT, không phải khoá trừu tượng:</b> FE
    /// (<c>shared/components/sidebar/sidebar.ts</c>) dùng nguyên giá trị này làm class, chỉ
    /// fallback khi <c>null</c>. Xem doc/contracts/meta-menu.md §Icon.</para>
    ///
    /// <para><b><c>quan-tri</c> có DisplayOrder = 3, không phải 2 — đừng "sửa cho liền số":</b>
    /// khoảng trống thứ 2 để dành cho nhánh nghiệp vụ chèn vào giữa trang chủ và quản trị hệ
    /// thống (doc/contracts/meta-menu.md).</para>
    ///
    /// <para><b><c>Roles: []</c> ở <c>trang-chu</c> là mở cho MỌI user đã đăng nhập</b>, không
    /// phải "chưa gán role" — xem <see cref="MenuSeedItem.Roles"/>.</para>
    /// </summary>
    private static readonly MenuSeedItem[] Items =
    [
        new MenuSeedItem(
            Code: "trang-chu", Name: "Trang chủ", Route: "/trang-chu", Icon: "pi-home",
            ParentCode: null, DisplayOrder: 1, Roles: []),

        new MenuSeedItem(
            Code: "quan-tri", Name: "Quản trị hệ thống", Route: null, Icon: "pi-cog",
            ParentCode: null, DisplayOrder: 3, Roles: [Roles.SuperAdmin, Roles.Admin]),

        new MenuSeedItem(
            Code: "sys-user", Name: "Người dùng", Route: "/quan-tri/nguoi-dung", Icon: "pi-user",
            ParentCode: "quan-tri", DisplayOrder: 1, Roles: [Roles.SuperAdmin, Roles.Admin]),

        new MenuSeedItem(
            Code: "phan-quyen", Name: "Phân quyền", Route: "/quan-tri/phan-quyen", Icon: "pi-shield",
            ParentCode: "quan-tri", DisplayOrder: 2, Roles: [Roles.SuperAdmin]),
    ];

    public IReadOnlyCollection<MenuSeedItem> GetMenuItems() => Items;
}
