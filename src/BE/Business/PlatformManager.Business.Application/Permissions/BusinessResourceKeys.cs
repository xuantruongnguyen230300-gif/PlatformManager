namespace PlatformManager.Business.Application.Permissions;

/// <summary>
/// Permission-key của khối nghiệp vụ — spec/danh-muc-dti/business-rules.md §6.5 (Q27).
///
/// <para><b>Đúng MỘT key cho toàn bộ đường ghi DTI</b>: tạo/sửa/xoá chỉ tiêu, lưu đánh giá qua
/// dialog, sửa inline, VÀ import. Áp cho MỌI kỳ, kể cả kỳ đã qua và năm trước. Không tách
/// "sửa kỳ hiện tại" / "sửa kỳ cũ"; không có khái niệm "chốt kỳ".</para>
///
/// <para><b>Quyền XEM không cần key</b> — chỉ cần đăng nhập (Q21). Endpoint đọc (DM-1, DM-2,
/// DM-8, DB-1, DB-3) giữ <c>[Authorize]</c> trần.</para>
///
/// <para><b>Vì sao hằng số ở ĐÂY chứ không chỉ ở host:</b> §6.5 yêu cầu khai key trong danh mục
/// của host (<c>AppResourceKeySource</c>) — và host làm đúng vậy, bằng cách trỏ vào chính hằng số
/// này. Nhưng <c>Business.Api</c> cũng cần nó cho <c>[RequirePermission(...)]</c>, mà
/// <c>Business.Api → host</c> là vòng tròn tham chiếu. Đặt chuỗi ở tầng nghiệp vụ và để host tham
/// chiếu tới là cách duy nhất có ĐÚNG MỘT chuỗi literal trong repo. Cả hai đầu vẫn là
/// <c>const</c>, nên <c>[RequirePermission(BusinessResourceKeys.DtiManage)]</c> biên dịch được.</para>
///
/// <para>⚠️ <b>KHÔNG dùng lại <c>import.manage</c></b> (§6.5): key đó là DI SẢN của module đã gỡ,
/// giữ lại chỉ vì bảng <c>RolePermissions</c> đã seed có dòng mang nó. Dùng lại một key vì nó
/// "trông đúng tên" sẽ trộn quyền của một năng lực Core dùng chung với quyền của một nghiệp vụ.</para>
/// </summary>
public static class BusinessResourceKeys
{
    /// <summary>
    /// Ghi dữ liệu DTI. Tên <c>dti.manage</c> chứ không <c>criteria.manage</c> vì phạm vi rộng
    /// hơn <c>/api/criteria</c> — nó phủ cả đường import. Đặt tên hẹp là mời người sau khai thêm
    /// một key thứ hai cho import, đúng thứ Q27 cấm.
    /// </summary>
    public const string DtiManage = "dti.manage";

    /// <summary>
    /// Nhãn hiển thị trên màn Phân quyền — §6.5. Ở đây để host khai danh mục mà không phải chép
    /// lại câu chữ; ranh giới "nhãn là dữ liệu của dự án" không đổi, host vẫn là nơi khai
    /// <c>Definitions</c>.
    /// </summary>
    public const string DtiManageDisplayName = "Nhập & sửa dữ liệu DTI";
}
