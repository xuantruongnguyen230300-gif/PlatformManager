using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>
/// MỘT CHỖ DUY NHẤT cho mọi luật bảo vệ tài khoản quản trị. Đừng rải thêm điều kiện vào từng
/// handler — rải ra là chúng lệch nhau, và đây đúng là loại luật mà lệch nhau thành lỗ hổng.
///
/// Trả <see cref="ErrorDescriptor"/> (null = cho qua) chứ không phải bool: mỗi vi phạm có câu
/// thông báo riêng, vì người dùng gặp lỗi này sau khi đã bấm Lưu/Khoá — nói đúng lý do mới
/// giúp được họ (nói "thiếu quyền" cho một SuperAdmin đang tự gỡ quyền mình là sai sự thật).
///
/// 4 luật, gộp từ 2 đợt:
/// <list type="number">
///   <item><b>Đổi tư cách SuperAdmin của bất kỳ ai</b> → phải là SuperAdmin. Cả 2 chiều
///     (thêm = leo thang; gỡ = vô hiệu hoá break-glass). Audit 2026-08-19 §Finding 1.</item>
///   <item><b>Tự gỡ SuperAdmin của chính mình</b> → cấm TUYỆT ĐỐI, kể cả người gọi là
///     SuperAdmin. Đây là ca "hạ quyền im lặng" do fe-perf-core tìm ra: luật 1 cho qua vì
///     người gọi đúng là SuperAdmin, nên không có lỗi nào bật ra và người đó mất quyền mà
///     không biết. Nay chặn ở tầng dữ liệu, không còn phụ thuộc FE nhớ giữ role.</item>
///   <item><b>Khoá tài khoản SuperAdmin</b> → phải là SuperAdmin. Nếu không, Admin vẫn khoá
///     được đường vào cuối cùng — đúng kịch bản mà break-glass sinh ra để tránh.</item>
///   <item><b>Tự khoá chính mình</b> → cấm TUYỆT ĐỐI, áp cho MỌI role (xem lý do dưới).</item>
/// </list>
///
/// <para><b>Vì sao luật 4 áp cho mọi role, không riêng SuperAdmin:</b> tự khoá tài khoản mình
/// qua màn quản trị không có ca dùng hợp lệ nào (muốn rời máy thì đăng xuất), nên chặn hết
/// không mất gì; đổi lại nó bịt trọn một lớp tự khoá thay vì chỉ bịt cho SuperAdmin rồi để
/// Admin tự bắn vào chân mình và phải nhờ người khác mở. Luật hẹp hơn cũng khó giải thích hơn
/// ("sao SuperAdmin không tự khoá được mà tôi thì được?").</para>
///
/// <para><b>Vì sao KHÔNG chặn tương tự cho mở khoá (Unlock):</b> mở khoá đi theo chiều KHÔI
/// PHỤC quyền truy cập, không phải chiều gây hại — chặn nó là chặn đúng đường sửa sai. Nó cũng
/// không cấp thêm gì cho người gọi (không leo thang). Rủi ro còn lại đã cân nhắc và chấp nhận:
/// Admin có thể mở khoá một tài khoản SuperAdmin vừa bị khoá có chủ đích; đây là hoàn tác một
/// hành động quản trị, không phải chiếm quyền, và người khoá vẫn khoá lại được. Nếu sau này
/// cần chặt hơn thì đó là quyết định sản phẩm — hỏi người dùng, đừng tự thêm.</para>
///
/// <para><b>Vì sao KHÔNG có luật "không được hạ SuperAdmin cuối cùng":</b> người dùng đã cân
/// nhắc và loại (2026-08-19) — phải đếm toàn bảng mỗi lần ghi cộng bài toán race giữa 2 request
/// đồng thời, mua quá ít an toàn so với chi phí.</para>
/// </summary>
internal static class SuperAdminAccountGuard
{
    public static bool ContainsSuperAdmin(IEnumerable<string>? roles)
        => roles?.Any(r => string.Equals(r, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)) ?? false;

    /// <summary>
    /// Luật 1 + 2. <paramref name="targetUserId"/> null = đang TẠO user (chưa có id để so với
    /// người gọi); <paramref name="targetHasSuperAdmin"/> khi tạo luôn <c>false</c>.
    /// </summary>
    public static ErrorDescriptor? CheckRoleChange(
        ICurrentUser caller, Guid? targetUserId, bool targetHasSuperAdmin, IEnumerable<string>? requestedRoles)
    {
        var requestedHasSuperAdmin = ContainsSuperAdmin(requestedRoles);

        // Tư cách SuperAdmin không đổi → không phải hành vi cần đặc quyền, cho qua.
        // (Admin sửa email/tên của một SuperAdmin mà GIỮ NGUYÊN SuperAdmin vẫn làm được.)
        if (targetHasSuperAdmin == requestedHasSuperAdmin)
            return null;

        // Luật 2 phải đứng TRƯỚC luật 1: người gọi là SuperAdmin thì luật 1 cho qua, nên đặt
        // sau là không bao giờ chạy tới — đúng cái bẫy khiến ca "hạ quyền im lặng" tồn tại.
        if (targetHasSuperAdmin && !requestedHasSuperAdmin && IsSelf(caller, targetUserId))
            return UserErrors.SelfSuperAdminRemovalForbidden;

        return caller.IsInRole(Roles.SuperAdmin) ? null : UserErrors.SuperAdminRoleChangeForbidden;
    }

    /// <summary>Luật 3 + 4.</summary>
    public static ErrorDescriptor? CheckLock(ICurrentUser caller, Guid targetUserId, bool targetHasSuperAdmin)
    {
        if (IsSelf(caller, targetUserId))
            return UserErrors.SelfLockForbidden;

        if (targetHasSuperAdmin && !caller.IsInRole(Roles.SuperAdmin))
            return UserErrors.SuperAdminLockForbidden;

        return null;
    }

    /// <summary>
    /// Thiếu claim định danh (<c>UserId</c> null) → coi như KHÔNG phải chính mình, để rơi xuống
    /// luật theo role bên dưới thay vì cho qua — request kiểu đó vẫn bị luật 1/3 chặn.
    /// </summary>
    private static bool IsSelf(ICurrentUser caller, Guid? targetUserId)
        => targetUserId is { } id && caller.UserId == id;
}
