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
/// <c>Core.Persistence/CoreSeeder.cs</c> nghĩa là muốn tái dùng Core thì phải mổ
/// vào trong Core. Cơ chế seed (upsert theo <c>Code</c>, hồi sinh dòng đã xoá mềm, gán
/// <c>SysMenuRole</c>) vẫn ở Core — xem <see cref="ICoreMenuSeedSource"/>.</para>
///
/// <para>🔄 <b>Sửa 2026-09-11 — bảng này NAY GIỮ CẢ mục nghiệp vụ.</b> Bản trước ghi <i>"menu
/// NGHIỆP VỤ không khai ở đây … module nghiệp vụ tự có seeder riêng đóng góp mục menu của nó"</i>.
/// Câu đó đúng với mô hình N-module cũ; mô hình MỘT khối <c>Business</c> của v3 không có seeder
/// menu riêng, và <c>BusinessSeeder</c> hiện chỉ seed 6 nhóm chỉ tiêu.</para>
///
/// <para><b>Hậu quả đã trả giá thật:</b> màn Danh mục DTI chạy được, route
/// <c>/danh-muc/dti</c> tồn tại, nhưng <b>không có lối vào từ giao diện</b> — người dùng phải gõ
/// URL. Đây là lần thứ ba cùng một lớp lỗi trong cụm này (trước đó là <c>/doi-mat-khau</c> và
/// <c>/tong-quan/dti</c>): dựng xong thứ đó nhưng không dựng đường tới nó.</para>
///
/// <para>Ranh giới vẫn giữ: đây là <b>dữ liệu của dự án</b>, và host là nơi duy nhất được biết nó.
/// Core vẫn không biết mục nào tồn tại.</para>
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
    /// thống (doc/contracts/meta-menu.md). ✅ <b>Đã điền 2026-09-11</b> bằng nhánh
    /// <c>danh-muc</c>; khoảng trống đó nay không còn trống.</para>
    ///
    /// <para><b><c>Roles: []</c> ở <c>trang-chu</c>, <c>danh-muc</c> và <c>danh-muc-dti</c> là mở
    /// cho MỌI user đã đăng nhập</b>, không phải "chưa gán role" — xem
    /// <see cref="MenuSeedItem.Roles"/>.</para>
    /// </summary>
    private static readonly MenuSeedItem[] Items =
    [
        new MenuSeedItem(
            Code: "trang-chu", Name: "Trang chủ", Route: "/trang-chu", Icon: "pi-home",
            ParentCode: null, DisplayOrder: 1, Roles: []),

        // ── Nhánh NGHIỆP VỤ, chèn vào đúng khoảng trống thứ 2 đã để dành ────────────────────
        //
        // Nhánh (cha + con) chứ không phải một mục phẳng, vì hai lý do: route thật là HAI CẤP
        // (/danh-muc/dti, chốt Q33) nên cây menu phải phản ánh đúng nó; và "danh mục" vốn là chỗ
        // của nhiều màn — mục phẳng hôm nay sẽ phải tái cấu trúc thành nhánh ở màn thứ hai, mà đổi
        // cấu trúc menu sau khi đã seed là việc seeder CỐ Ý không làm (xem MenuSeedItem.Code:
        // gặp dòng đang sống thì trả nguyên trạng, không cập nhật ParentCode/DisplayOrder).
        //
        // 🛑 Roles: [] — MỞ CHO MỌI USER ĐÃ ĐĂNG NHẬP, KHÔNG phải [Admin]. Đây là Q39, chốt
        // 2026-09-06: người thiếu quyền GHI vẫn vào được màn ở chế độ chỉ đọc — "không chặn ở
        // route, KHÔNG ẩn mục menu" (spec/danh-muc-dti/ui-spec.md §Q39). Cấp [Admin] ở đây là
        // giấu mục menu khỏi đúng những người ĐƯỢC PHÉP xem nó, và triệu chứng lúc đó là "vai
        // User không thấy Danh mục DTI" — trông như lỗi phân quyền chứ không như một dòng seed.
        //
        // Quyền GHI thì vẫn do dti.manage canh ở BE ([RequirePermission]) và do cờ canWrite của
        // DM-2 canh ở FE. Menu KHÔNG phải lớp phân quyền.
        new MenuSeedItem(
            Code: "danh-muc", Name: "Danh mục", Route: null, Icon: "pi-book",
            ParentCode: null, DisplayOrder: 2, Roles: []),

        new MenuSeedItem(
            Code: "danh-muc-dti", Name: "Danh mục DTI", Route: "/danh-muc/dti", Icon: "pi-list-check",
            ParentCode: "danh-muc", DisplayOrder: 1, Roles: []),

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
