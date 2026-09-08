using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Lỗi nghiệp vụ của hai ma trận phân quyền (PERM-1 menu × role, PERM-2 resource × role) — khai
/// tập trung thay vì string literal rải rác, xem doc/huong_dan/quy-uoc/be-cqrs-handler.md
/// §ErrorDescriptor.
/// </summary>
public static class PermissionErrors
{
    /// <summary>
    /// <c>version</c> trong body không khớp trạng thái DB tại thời điểm ghi — nghĩa là có người
    /// khác đã lưu ma trận sau khi client tải nó về. <b>409</b> (<see cref="ErrorCode.Conflict"/>
    /// → status <c>BUSINESS_ERROR</c>), và handler KHÔNG ghi gì cả.
    ///
    /// <para>Thiếu hẳn <c>version</c> cũng rơi vào đây, KHÔNG phải 400 — quyết định người dùng
    /// 2026-08-30 (doc/contracts/permissions.md §Lỗi). Lý do: với một lệnh ghi đè toàn bộ, client
    /// không gửi token không phân biệt được với client gửi token đã cũ; cả hai đều là "ghi mà
    /// không biết mình đang ghi đè lên cái gì".</para>
    ///
    /// <para><c>Retryable = false</c>: thử lại nguyên payload cũ sẽ hỏng y hệt — người dùng phải
    /// tải lại ma trận rồi sửa trên nền mới.</para>
    /// </summary>
    public static readonly ErrorDescriptor VersionConflict = new(
        "PERMISSION.VERSION_CONFLICT", ErrorCode.Conflict,
        "Ma trận phân quyền đã được người khác thay đổi kể từ lúc bạn mở màn hình. " +
        "Hãy tải lại trang rồi thực hiện lại thay đổi — thay đổi vừa rồi CHƯA được lưu.");
}
