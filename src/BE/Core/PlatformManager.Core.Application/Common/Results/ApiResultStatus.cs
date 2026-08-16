using System.Text.Json.Serialization;

namespace PlatformManager.Core.Application.Common.Results;

/// <summary>4 nhóm, phục vụ FE quyết định cách phản ứng — không phải để phân loại lỗi
/// cho BE. Xem .claude/rules/api-controller.md §Envelope response.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApiResultStatus>))]
public enum ApiResultStatus
{
    SUCCESS,
    VALIDATION_ERROR,
    BUSINESS_ERROR,
    SYSTEM_ERROR,
}
