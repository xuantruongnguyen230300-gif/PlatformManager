namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Thay cho magic string — nguồn lỗi nghiệp vụ mong đợi. Khai báo tập trung cạnh
/// handler (vd Application/Criteria/CriteriaErrors.cs), không rải rác string literal
/// trong code. Xem doc/huong_dan/quy-uoc/be-cqrs-handler.md §ErrorDescriptor.
/// </summary>
public sealed record ErrorDescriptor(
    string BusinessCode,
    ErrorCode ErrorCode,
    string MessageTemplate,
    bool Retryable = false);
