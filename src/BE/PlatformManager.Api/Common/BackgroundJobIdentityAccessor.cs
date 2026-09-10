using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Api.Common;

/// <summary>
/// Danh tính của người đã KÍCH HOẠT một job nền — chụp lại lúc enqueue (còn trong HTTP request,
/// lúc <see cref="ICurrentUser"/> còn trả đúng người) và đọc lại lúc worker chạy job.
///
/// <para><b>Cố ý KHÔNG mang <c>Roles</c>.</b> Đây là ảnh chụp tại thời điểm enqueue; một job chạy
/// sau đó vài phút mà quyết định phân quyền theo ảnh chụp này thì quyền vừa bị thu hồi vẫn còn
/// hiệu lực — đúng rủi ro mà doc/huong_dan/wiki-core/be/11-performance-caching.md §Cache nêu cho
/// dữ liệu phân quyền. Job nào cần quyết định phân quyền phải đọc quyền HIỆN TẠI từ DB theo
/// <see cref="UserId"/>. Vì vậy <see cref="BackgroundJobCurrentUser.Roles"/> trả rỗng và
/// <see cref="BackgroundJobCurrentUser.IsInRole"/> trả <c>false</c> — từ chối theo mặc định.</para>
/// </summary>
/// <param name="UserId">Khoá chính của tài khoản — ổn định kể cả khi đổi tên đăng nhập.</param>
/// <param name="UserName">Tên đăng nhập — chính là giá trị <c>AuditInterceptor</c> ghi vào
/// <c>CreatedBy</c>/<c>UpdatedBy</c>.</param>
public sealed record BackgroundJobIdentity(Guid? UserId, string? UserName);

/// <summary>
/// Chỗ chứa danh tính job nền cho luồng đang chạy. Cùng khuôn với <c>IHttpContextAccessor</c>:
/// một singleton bọc <see cref="AsyncLocal{T}"/>, ghi bởi hạ tầng (ở đây là
/// <see cref="BackgroundJobIdentityFilter"/> phía server) và chỉ ĐỌC bởi phần còn lại.
/// </summary>
public interface IBackgroundJobIdentityAccessor
{
    /// <summary>
    /// <c>null</c> nghĩa là "không ai" — job không do người dùng kích hoạt, hoặc mã đang chạy
    /// ngoài mọi job nền. Đây là trạng thái BÌNH THƯỜNG, không phải lỗi.
    /// </summary>
    BackgroundJobIdentity? Current { get; set; }
}

/// <inheritdoc cref="IBackgroundJobIdentityAccessor"/>
/// <remarks>
/// Dùng <see cref="AsyncLocal{T}"/> chứ không phải trường thường: một Hangfire server chạy nhiều
/// worker song song trong cùng tiến trình, nên giá trị phải bám theo LUỒNG CHẠY của từng job. Đăng
/// ký SINGLETON (xem Program.cs) — trạng thái nằm trong <see cref="AsyncLocal{T}"/> chứ không nằm
/// ở instance, còn scope DI của job thì được tạo sau khi filter đã ghi.
///
/// <para>Trường instance (không static) để mỗi lần dựng trong unit test là một chỗ chứa riêng —
/// static sẽ làm các test chạy song song trong cùng assembly giẫm lên nhau.</para>
/// </remarks>
public sealed class BackgroundJobIdentityAccessor : IBackgroundJobIdentityAccessor
{
    private readonly AsyncLocal<BackgroundJobIdentity?> _current = new();

    public BackgroundJobIdentity? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
