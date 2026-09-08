namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Mã nghiệp vụ của nhánh <b>lỗi validate</b> (HTTP 400) — nhánh này dựng envelope ở
/// <c>PlatformManager.Api/Common/GlobalExceptionHandler.cs</c>, KHÔNG đi qua handler nào, nên nó
/// là nhánh lỗi <b>đầu tiên</b> được gắn mã trong đợt 2026-09-03.
///
/// <para><b>Sửa cùng ngày:</b> bản trước của câu này viết "nhánh lỗi <i>duy nhất</i> từng rời khỏi
/// BE mà không mang mã nào" — SAI, và là loại sai khiến người sau tin việc đã đóng trọn vẹn rồi
/// không đi tìm tiếp. Hai lượt kiểm độc lập cùng chỉ ra còn <b>5</b> nhánh nữa: CSRF 403, 500 không
/// mong đợi, 403 Origin, 401 chưa đăng nhập, 403 thiếu quyền. Cả 5 nay dùng
/// <see cref="InfrastructureErrors"/> (riêng 401 dùng lại <c>AuthErrors.NotAuthenticated</c>).
/// Nhánh thứ 7 — model binding — dựng ở <c>Common/ModelBindingProblemFactory.cs</c> và dùng lại
/// chính descriptor của file này.</para>
///
/// <para><b>Vì sao phải có mã dù đã có <c>fields</c>:</b> luật "mọi envelope lỗi rời khỏi BE đều
/// mang mã máy đọc được" (doc/huong_dan/quy-uoc/be-api-controller.md §"Lỗi validation cũng phải
/// mang mã") không có ngoại lệ cho 400. Không có mã thì client không phân biệt được "payload sai"
/// với bất kỳ lỗi nào khác trừ khi đọc <c>status</c> — và <c>status</c> là phân loại thô, không
/// phải định danh.</para>
///
/// <para><b>Đặt cạnh <see cref="ErrorCode"/>/<see cref="ApiResult{T}"/></b> chứ không cạnh một
/// handler, cùng lý do với <see cref="RateLimitErrors"/>: đây là lỗi HẠ TẦNG dùng chung cho MỌI
/// endpoint có validator, không thuộc riêng feature nào.</para>
///
/// <para>Tên file kết thúc bằng <c>Errors.cs</c> là điều kiện để luật T2 của
/// <c>PlatformManager.ArchTests/ErrorCodeSourceTests</c> coi đây là catalog hợp lệ — đổi tên file
/// sẽ làm test đó đỏ, và đó là ý đồ.</para>
/// </summary>
public static class ValidationErrors
{
    /// <summary>
    /// <c>MessageTemplate</c> giữ nguyên đúng câu mà nhánh này vẫn trả từ trước
    /// ("Dữ liệu không hợp lệ.") — mục tiêu của đợt sửa là <b>thêm mã</b>, không phải đổi câu.
    /// Đổi câu cùng lúc sẽ trộn hai thay đổi vào một, và câu là thứ đang có test bám vào.
    /// </summary>
    public static readonly ErrorDescriptor Failed = new(
        "VALIDATION.FAILED",
        ErrorCode.ValidationError,
        "Dữ liệu không hợp lệ.");
}
