namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Ném từ Domain khi vi phạm invariant nghiệp vụ (factory method / mutation method).
/// Application layer bắt và dịch thành IApiResult lỗi (ErrorCode.BusinessRuleError, 422)
/// qua ErrorDescriptor tương ứng — xem .claude/rules/entity-domain.md và cqrs-handler.md.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
