namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Mã cho các nhánh lỗi <b>hạ tầng</b> — những nhánh dựng envelope ngoài handler: middleware,
/// exception handler toàn cục, và các sự kiện của cookie authentication.
///
/// <para><b>Vì sao tồn tại</b> (2026-09-03): luật "mọi envelope lỗi rời khỏi BE đều mang mã máy
/// đọc được" (doc/huong_dan/quy-uoc/be-api-controller.md) không có ngoại lệ cho nhánh hạ tầng.
/// Trước ngày này có <b>5</b> nhánh dựng <c>ApiResult</c> bằng tay với <c>BusinessCode</c> để
/// trống — hai lượt kiểm độc lập cùng xếp đó là phát hiện số một. Nhánh nặng nhất là 403 phân
/// quyền: <c>RequirePermissionFilter</c> trả <c>ForbidResult()</c> nên MỌI lần từ chối quyền đều
/// đi qua đó, tức toàn bộ 403 phân quyền ra tới FE không có gì để tra.</para>
///
/// <para><b>Đặt cạnh <see cref="ValidationErrors"/>/<see cref="RateLimitErrors"/></b> chứ không
/// cạnh middleware sinh ra chúng, cùng một lý do: đây là lỗi dùng chung cho MỌI endpoint, không
/// thuộc riêng feature nào. Tên file kết thúc <c>Errors.cs</c> là điều kiện để luật T2 của
/// <c>PlatformManager.ArchTests/ErrorCodeSourceTests</c> coi đây là catalog hợp lệ.</para>
///
/// <para><b>401 KHÔNG nằm ở đây.</b> Nó dùng lại <c>AuthErrors.NotAuthenticated</c>
/// (<c>AUTH.NOT_AUTHENTICATED</c>) — chuỗi "Chưa đăng nhập." vốn đã khai ở đó, và bản gõ tay
/// trong <c>Program.cs</c> là nguồn thứ hai cho cùng một sự thật, đúng khuôn
/// <c>.claude/CLAUDE.md</c> §5 cấm.</para>
/// </summary>
public static class InfrastructureErrors
{
    /// <summary>Thiếu/sai token chống giả mạo trên request ghi. 403 chứ không 500: request bị TỪ
    /// CHỐI có chủ đích, không phải lỗi hệ thống.</summary>
    public static readonly ErrorDescriptor CsrfRejected = new(
        "AUTH.CSRF_REJECTED",
        ErrorCode.AuthorizationError,
        "Yêu cầu bị từ chối — thiếu hoặc sai token chống giả mạo (CSRF).");

    /// <summary>Header <c>Origin</c> không nằm trong danh sách cho phép — lớp chặn CSRF thứ nhất,
    /// độc lập với token.</summary>
    public static readonly ErrorDescriptor OriginRejected = new(
        "AUTH.ORIGIN_REJECTED",
        ErrorCode.AuthorizationError,
        "Yêu cầu bị từ chối: nguồn gửi (Origin) không được phép.");

    /// <summary>Đã đăng nhập nhưng không đủ quyền. Đường ra của MỌI lần
    /// <c>RequirePermissionFilter</c> trả <c>ForbidResult()</c>.</summary>
    public static readonly ErrorDescriptor Forbidden = new(
        "AUTH.FORBIDDEN",
        ErrorCode.AuthorizationError,
        "Không có quyền truy cập.");

    /// <summary>
    /// Không có endpoint nào khớp đường dẫn. Miền <c>ROUTE</c> chứ không phải miền của thực thể:
    /// đây là "không có địa chỉ này", KHÁC hẳn <c>USER.NOT_FOUND</c> ("địa chỉ đúng, bản ghi không
    /// tồn tại"). Hai ca đều là 404 nên <c>code</c> không phân biệt được chúng — <c>businessCode</c>
    /// là thứ duy nhất phân biệt, và FE cần phân biệt: một bên là bug của chính FE (gọi sai URL),
    /// một bên là dữ liệu đã bị xoá.
    /// </summary>
    public static readonly ErrorDescriptor RouteNotFound = new(
        "ROUTE.NOT_FOUND",
        ErrorCode.NotFound,
        "Không tìm thấy tài nguyên được yêu cầu.");

    /// <summary>Đường dẫn khớp nhưng HTTP verb không được hỗ trợ. Response giữ nguyên header
    /// <c>Allow</c> do hạ tầng định tuyến đặt — envelope chỉ thêm phần thân, không thay thế nó.</summary>
    public static readonly ErrorDescriptor MethodNotAllowed = new(
        "ROUTE.METHOD_NOT_ALLOWED",
        ErrorCode.MethodNotAllowed,
        "Phương thức HTTP không được hỗ trợ cho tài nguyên này.");

    /// <summary>Lỗi không mong đợi (bug/hạ tầng). Câu giữ nguyên bản cũ có chủ đích: mục tiêu đợt
    /// này là <b>thêm mã</b>, không đổi câu — và câu này cố ý không nói gì về nguyên nhân, chi
    /// tiết ở lại log kèm <c>TraceId</c>.</summary>
    public static readonly ErrorDescriptor Unexpected = new(
        "SYSTEM.UNEXPECTED",
        ErrorCode.SystemError,
        "Đã có lỗi xảy ra.");
}
