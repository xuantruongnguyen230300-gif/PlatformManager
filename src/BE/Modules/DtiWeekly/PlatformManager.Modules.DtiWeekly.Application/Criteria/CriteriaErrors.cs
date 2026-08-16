using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

public static class CriteriaErrors
{
    public static readonly ErrorDescriptor NotFound = new(
        "CRITERIA.NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy chỉ tiêu.");

    public static readonly ErrorDescriptor DuplicateCode = new(
        "CRITERIA.DUPLICATE_CODE", ErrorCode.Conflict, "Mã chỉ tiêu '{0}' đã tồn tại.");

    public static readonly ErrorDescriptor GroupNotFound = new(
        "CRITERIA.GROUP_NOT_FOUND", ErrorCode.BusinessRuleError, "Nhóm chỉ tiêu không tồn tại hoặc đã bị xoá.");
}
