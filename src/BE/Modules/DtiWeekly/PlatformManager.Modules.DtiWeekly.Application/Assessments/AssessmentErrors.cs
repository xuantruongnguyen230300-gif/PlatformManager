using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

public static class AssessmentErrors
{
    public static readonly ErrorDescriptor CriteriaNotFound = new(
        "CRITERIA_ASSESSMENT.CRITERIA_NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy chỉ tiêu.");

    public static readonly ErrorDescriptor NotEditableOutsideLivePeriod = new(
        "CRITERIA_ASSESSMENT.NOT_EDITABLE_OUTSIDE_LIVE_PERIOD", ErrorCode.BusinessRuleError,
        "Không thể sửa khi đang xem dữ liệu lịch sử — quay lại 'Tất cả' của năm hiện tại để chỉnh sửa.");

    public static readonly ErrorDescriptor FileRequired = new(
        "IMPORT.FILE_REQUIRED", ErrorCode.ValidationError, "Vui lòng chọn file CSV để import.");

    public static readonly ErrorDescriptor FileTooLarge = new(
        "IMPORT.FILE_TOO_LARGE", ErrorCode.ValidationError, "File vượt quá giới hạn 20MB.");
}
