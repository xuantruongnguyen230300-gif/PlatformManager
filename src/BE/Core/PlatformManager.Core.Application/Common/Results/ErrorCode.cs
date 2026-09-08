using System.Text.Json.Serialization;

namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Giá trị enum CHÍNH LÀ mã HTTP — không có bảng map thứ hai để lệch (xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §Envelope response). On-wire là TÊN member,
/// không phải số (JsonStringEnumConverter).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ErrorCode>))]
public enum ErrorCode
{
    Success = 0,
    ValidationError = 400,
    AuthenticationError = 401,
    AuthorizationError = 403,
    NotFound = 404,

    /// <summary>
    /// Route khớp nhưng HTTP verb không được endpoint hỗ trợ. CHỈ hạ tầng định tuyến sinh ra mã
    /// này (<c>ApiStatusCodeEnvelopeMiddleware</c>) — handler nghiệp vụ không bao giờ chọn nó, vì
    /// "sai verb" không phải một tình huống nghiệp vụ mà là client gọi sai địa chỉ.
    ///
    /// <para>Thêm 2026-09-04. Trước đó 405 rời khỏi BE với body RỖNG, tức nằm ngoài lời hứa
    /// "mọi response đi qua <c>IApiResult&lt;T&gt;</c>" của doc/contracts/auth.md.</para>
    /// </summary>
    MethodNotAllowed = 405,

    Conflict = 409,
    BusinessRuleError = 422,
    TooManyRequests = 429,
    SystemError = 500,
}
