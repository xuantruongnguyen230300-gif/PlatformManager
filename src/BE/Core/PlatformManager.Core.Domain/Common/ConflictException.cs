namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Ném khi phát hiện xung đột trạng thái của chính resource (trùng giá trị unique,
/// đang bị ràng buộc không cho thao tác). Application layer dịch thành IApiResult lỗi
/// ErrorCode.Conflict (409) — xem doc/huong_dan/quy-uoc/be-api-controller.md §Error → HTTP status mapping.
///
/// <para><b>Cũng chỉ nhận <see cref="DomainError"/></b> (đổi 2026-09-03). Hôm nay chưa có chỗ nào
/// ném nó, nhưng <c>ExceptionHandlingBehavior</c> đẩy <c>Code</c> của nó ra CÙNG field
/// <c>businessCode</c> với <see cref="DomainException"/> — để nguyên chữ ký chuỗi tự do là chừa lại
/// đúng cái lỗ vừa bịt, và lần ném đầu tiên sẽ chui qua. Sửa lúc còn 0 nơi dùng là rẻ nhất.</para>
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(DomainError error, params object[] args)
        : base(Render(error, args))
    {
        Error = error;
    }

    public DomainError Error { get; }

    /// <summary>Cùng lý do như <see cref="DomainException"/> — xem chú thích ở đó.</summary>
    private static string Render(DomainError error, object[] args)
        => args.Length == 0 ? error.MessageTemplate : string.Format(error.MessageTemplate, args);
}
