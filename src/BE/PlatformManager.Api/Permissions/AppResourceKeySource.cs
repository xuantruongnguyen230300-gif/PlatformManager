using PlatformManager.Business.Application.Permissions;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Api.Permissions;

/// <summary>
/// Permission-key của CHÍNH dự án PlatformManager, dạng hằng số — cần <c>const</c> vì
/// <c>[RequirePermission(...)]</c> là attribute, và đối số attribute phải biết được lúc biên dịch.
///
/// <para><b>Vì sao ở host chứ không ở Core</b> (chuyển 2026-09-03): trước đó danh sách này là lớp
/// <c>ResourceKeys</c> nằm trong <c>Core.Application</c> — một danh mục ĐÓNG kèm nhãn tiếng Việt,
/// và docstring của nó ra chỉ thị cho module nghiệp vụ khai key vào trong Core. Cả tập key lẫn
/// nhãn đều là thứ riêng của một dự án, nên để chúng trong Core nghĩa là muốn tái dùng CoreBase
/// thì phải mổ vào Core. Cơ chế thì vẫn ở Core và host không đụng tới được: kiểm quyền
/// deny-by-default, ma trận, luật "PUT phải phủ đủ danh mục", và CƠ CHẾ seed (idempotent,
/// SuperAdmin không cần dòng nào, mặc định <see cref="ResourceKeyDefinition.DefaultSeedRoles"/>)
/// — xem <see cref="ICoreResourceKeySource"/>.</para>
///
/// <para><b>Sửa 2026-09-09:</b> dòng trên trước đây ghi <i>"seed cấp đủ danh mục cho Admin +
/// User"</i> là phần Core giữ. Hết đúng từ lượt mở rộng cùng ngày: VAI được cấp khi seed nay là
/// DỮ LIỆU của host (<see cref="ResourceKeyDefinition.SeedRoles"/>), Core chỉ giữ cơ chế và giá
/// trị MẶC ĐỊNH. Cùng câu sai đó tồn tại song song ở docstring của
/// <see cref="ICoreResourceKeySource"/> và đã sửa cùng lượt.</para>
///
/// <para><b>Thêm key mới:</b> thêm một <c>const</c> ở đây, thêm một dòng vào
/// <see cref="AppResourceKeySource.Definitions"/>, rồi gắn <c>[RequirePermission(...)]</c> lên
/// action. Thiếu bước thứ hai thì key không hiện trên màn hình phân quyền và không role nào cấp
/// được — endpoint sẽ 403 cho tất cả trừ SuperAdmin. Key nào KHÔNG được cấp sẵn cho mọi người
/// đăng nhập thì thu hẹp ngay tại dòng đó:
/// <c>new(Key, "Nhãn") { SeedRoles = [Roles.Admin] }</c>.</para>
/// </summary>
public static class AppResourceKeys
{
    /// <summary>
    /// ⚠️ <b>DI SẢN của module DtiWeekly đã gỡ 2026-08-29</b> — hôm nay (2026-09-03) KHÔNG endpoint
    /// nào mang <c>[RequirePermission(Import)]</c> trong mã sản phẩm; nơi duy nhất dùng nó là
    /// controller thăm dò của integration test. Giữ lại vì đường Import CSV/Excel đang được đưa
    /// lên Core thành năng lực dùng chung, và vì bỏ hẳn là một quyết định có hậu quả trên dữ liệu:
    /// <c>RolePermissions</c> ở database đã seed đang có dòng mang key này, xoá key khỏi danh mục
    /// biến chúng thành dòng mồ côi (xem <c>ResourcePermissionEndpointTests</c> — <c>PUT</c> ghi
    /// đè toàn bộ nên chúng cũng không tự dọn).
    /// </summary>
    public const string Import = "import.manage";

    /// <summary>
    /// Ghi dữ liệu DTI — ĐÚNG MỘT key cho toàn bộ đường ghi của nghiệp vụ (Q27, chốt 2026-09-05,
    /// spec/danh-muc-dti/business-rules.md §6.5): tạo/sửa/xoá chỉ tiêu, lưu đánh giá qua dialog,
    /// sửa inline, VÀ import. Áp cho MỌI kỳ, kể cả kỳ đã qua và năm trước.
    ///
    /// <para><b>Trỏ vào hằng số của <c>Business.Application</c> thay vì gõ lại chuỗi</b>: tầng
    /// nghiệp vụ cũng cần đúng chuỗi này cho <c>[RequirePermission(...)]</c> trên controller của
    /// nó, mà <c>Business.Api → host</c> là vòng tròn tham chiếu. Một chuỗi literal trong repo,
    /// hai nơi tiêu thụ, và cả hai vẫn là hằng số biết được lúc biên dịch (điều kiện bắt buộc để
    /// dùng làm đối số attribute).</para>
    ///
    /// <para>⚠️ KHÔNG dùng lại <see cref="Import"/> cho nghiệp vụ DTI: key đó là di sản của module
    /// đã gỡ. Dùng lại một key vì nó "trông đúng tên" sẽ trộn quyền của một năng lực Core dùng
    /// chung với quyền của một nghiệp vụ — và khi sản phẩm thứ hai dùng lại CoreBase, hai thứ đó
    /// phải tách rời được.</para>
    /// </summary>
    public const string DtiManage = BusinessResourceKeys.DtiManage;
}

/// <summary>
/// Hiện thực seam <see cref="ICoreResourceKeySource"/> cho dự án này. Singleton — bảng hằng số,
/// không trạng thái (cùng khuôn với <c>AppMenuSeedSource</c> và <c>AppBootstrapAccountSource</c>).
/// </summary>
internal sealed class AppResourceKeySource : ICoreResourceKeySource
{
    /// <summary>
    /// Thứ tự khai là thứ tự dòng trên màn hình phân quyền — Core không sắp lại (xem
    /// <see cref="ICoreResourceKeySource.GetResourceKeys"/>).
    ///
    /// <para><b>Nhãn là câu tiếng Việt, và đó là nợ đã biết chứ không phải chỗ đặt sai:</b> theo
    /// hướng "FE sở hữu câu chữ" thì trường này nên là một MÃ để FE tra bảng dịch. Đổi nó là đổi
    /// hợp đồng PERM-2 và kéo theo FE, nên để thành một lượt riêng — xem
    /// <see cref="ResourceKeyDefinition"/>.</para>
    /// </summary>
    private static readonly ResourceKeyDefinition[] Definitions =
    [
        new(AppResourceKeys.Import, "Import CSV/Excel"),

        // ── dti.manage — NGOẠI LỆ có chủ đích của luật seed mặc định (Q36, chốt 2026-09-06) ──
        //
        // SeedRoles = [Admin] và CHỈ Admin. Vai User KHÔNG được cấp sẵn; ai cần thì SuperAdmin cấp
        // tay ở màn Phân quyền.
        //
        // 🔴 Đây là NGOẠI LỆ, không phải hành vi mặc định — đọc trước khi "dọn cho nhất quán".
        // Mặc định của seam là [Admin, User] (ResourceKeyDefinition.DefaultSeedRoles), và mặc định
        // đó CỐ Ý: nó giữ nguyên hành vi trước khi bật deny-by-default, một quyết định DI TRÚ để
        // việc siết quyền không khoá mất người đang dùng hệ thống.
        //
        // Vì sao DTI được miễn: lý lẽ "giữ nguyên hành vi cũ" không áp cho một key MỚI TOANH —
        // không có ai đang ghi dữ liệu DTI để mà giữ nguyên hành vi cho họ (module gỡ 2026-08-29).
        // Cấp sẵn cho User ở đây không phải "không làm hỏng cái đang chạy", nó là MỞ QUYỀN GHI cho
        // toàn bộ người đăng nhập ngay ở lần seed đầu, trên một tập dữ liệu mà một lần nạp đè file
        // có thể ghi lại 62 dòng của một kỳ đã báo cáo.
        //
        // ⚠️ QUÊN mệnh đề SeedRoles thì key rơi về mặc định [Admin, User] và Q36 bị vi phạm mà
        // KHÔNG có gì báo — vì mặc định là một giá trị hợp lệ. Nghiệm thu bắt buộc sau khi seed:
        // mở GET /api/admin/permissions/resources và xác nhận dòng dti.manage có Admin và KHÔNG có
        // User (spec/danh-muc-dti/business-rules.md §6.5).
        new(AppResourceKeys.DtiManage, BusinessResourceKeys.DtiManageDisplayName)
        {
            SeedRoles = [Roles.Admin],
        },
    ];

    public IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys() => Definitions;
}
