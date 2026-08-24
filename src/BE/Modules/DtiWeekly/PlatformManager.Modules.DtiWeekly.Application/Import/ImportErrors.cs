using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>BusinessCode khớp CONTRACT DM-7 (doc/contracts/danh-muc-dti.md) — chỉ
/// IMPORT.FILE_EMPTY được contract liệt kê tường minh cho bước 1; FileTooLarge/
/// FileFormatUnsupported là validate bổ sung bắt buộc theo yêu cầu (giữ ngưỡng 20MB cũ + hỗ trợ
/// thêm .xlsx/.xls) nhưng chưa có business code riêng trong contract — đặt tên theo đúng quy ước
/// "{ENTITY}.{ERROR}" chung của dự án.</summary>
public static class ImportErrors
{
    public static readonly ErrorDescriptor FileEmpty = new(
        "IMPORT.FILE_EMPTY", ErrorCode.ValidationError, "Vui lòng chọn file để import.");

    public static readonly ErrorDescriptor FileTooLarge = new(
        "IMPORT.FILE_TOO_LARGE", ErrorCode.ValidationError, "File vượt quá giới hạn 20MB.");

    public static readonly ErrorDescriptor FileFormatUnsupported = new(
        "IMPORT.FILE_FORMAT_UNSUPPORTED", ErrorCode.ValidationError, "Chỉ hỗ trợ file .csv, .xlsx, .xls.");

    public static readonly ErrorDescriptor JobNotFound = new(
        "IMPORT.JOB_NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy job import.");
}
