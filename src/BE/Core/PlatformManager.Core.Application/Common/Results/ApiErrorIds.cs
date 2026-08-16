namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Nguồn DUY NHẤT map ErrorCode → ApiResultStatus — Status không bao giờ gán tay ở
/// nơi khác (xem .claude/rules/api-controller.md §Envelope response).
/// </summary>
public static class ApiErrorIds
{
    public static ApiResultStatus StatusForCode(ErrorCode code) => code switch
    {
        ErrorCode.ValidationError => ApiResultStatus.VALIDATION_ERROR,
        ErrorCode.SystemError => ApiResultStatus.SYSTEM_ERROR,
        _ => ApiResultStatus.BUSINESS_ERROR,
    };
}
