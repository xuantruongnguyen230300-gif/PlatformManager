using Hangfire.Client;
using Hangfire.Server;
using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Api.Common;

/// <summary>
/// Mang danh tính người dùng qua ranh giới HTTP request → worker Hangfire (chốt Q35, xem
/// spec/danh-muc-dti/business-rules.md §5.6). Hai nửa của CÙNG một cơ chế nên ở cùng một class:
///
/// <list type="number">
///   <item><b>Phía client</b> (<see cref="IClientFilter"/>, chạy lúc enqueue — còn trong HTTP
///   request): hỏi <see cref="ICurrentUser"/> rồi cất tên đăng nhập vào <i>job parameter</i>.</item>
///   <item><b>Phía server</b> (<see cref="IServerFilter"/>, chạy trong worker — KHÔNG có
///   <c>HttpContext</c>): đọc lại job parameter và đặt vào
///   <see cref="IBackgroundJobIdentityAccessor"/> để <see cref="BackgroundJobCurrentUser"/> thấy.</item>
/// </list>
///
/// <para><b>Vì sao filter chứ không mở rộng <c>IBackgroundJobScheduler</c>:</b> filter chạy cho MỌI
/// job đi qua Hangfire, kể cả job viết sau này. Mở rộng chữ ký seam thì mỗi nơi enqueue phải NHỚ
/// truyền danh tính, và nơi nào quên sẽ lặng lẽ ghi <c>"system"</c> trở lại — bug quay về mà không
/// có gì canh. Lý do đầy đủ + lịch sử lật quyết định: spec/danh-muc-dti/business-rules.md §5.6.</para>
///
/// <para><b>KHÔNG được ném lỗi khi không có ai để chụp.</b> Job enqueue ngoài HTTP request (lệnh
/// <c>--seed</c>, recurring job) là ca hợp lệ: không stash gì cả, phía server đọc ra <c>null</c>,
/// và <c>AuditInterceptor</c> rơi về <c>"system"</c> đúng như trước. Đây là điều kiện nghiệm thu
/// "ca âm 2" của Q35.</para>
/// </summary>
public sealed class BackgroundJobIdentityFilter(
    IHttpContextAccessor httpContextAccessor,
    IBackgroundJobIdentityAccessor identityAccessor) : IClientFilter, IServerFilter
{
    /// <summary>
    /// Tên job parameter. Có tiền tố riêng để không đụng các tham số Hangfire tự đặt
    /// (<c>RetryCount</c>, <c>CurrentCulture</c>…) — Hangfire không có namespace cho chỗ này.
    /// </summary>
    internal const string UserNameParameter = "PlatformManagerUserName";

    /// <inheritdoc cref="UserNameParameter"/>
    internal const string UserIdParameter = "PlatformManagerUserId";

    public void OnCreating(CreatingContext context)
    {
        var identity = CaptureFrom(ResolveRequestUser());
        if (identity is null)
        {
            // Không ai đang đăng nhập (job của hệ thống, hoặc enqueue ngoài HTTP request).
            // Không stash gì — KHÔNG ném lỗi.
            return;
        }

        if (identity.UserName is not null)
        {
            context.SetJobParameter(UserNameParameter, identity.UserName);
        }

        if (identity.UserId is { } userId)
        {
            context.SetJobParameter(UserIdParameter, userId);
        }
    }

    public void OnCreated(CreatedContext context)
    {
        // Không có việc gì phải làm sau khi job đã được tạo — job parameter đã ghi ở OnCreating.
    }

    public void OnPerforming(PerformingContext context)
        => identityAccessor.Current = RestoreFrom(
            context.GetJobParameter<string>(UserNameParameter),
            context.GetJobParameter<Guid?>(UserIdParameter));

    public void OnPerformed(PerformedContext context)
        // Dọn sạch: worker tái dùng luồng cho job kế tiếp, để sót danh tính là gán nhầm người
        // cho một job hoàn toàn khác.
        => identityAccessor.Current = null;

    /// <summary>
    /// Nửa "chụp" — tách khỏi <see cref="CreatingContext"/> để kiểm được bằng unit test mà không
    /// phải dựng nguyên bộ máy Hangfire (storage + connection + BackgroundJob). Public chứ không
    /// internal chỉ vì lý do đó — assembly host này không có InternalsVisibleTo, và thêm một cơ
    /// chế mới cho hai hàm thuần thì đắt hơn là mở chúng ra.
    /// </summary>
    /// <returns><c>null</c> khi không có ai để chụp — chưa đăng nhập, hoặc không nằm trong HTTP
    /// request, hoặc phiên không mang cả tên đăng nhập lẫn khoá tài khoản.</returns>
    public static BackgroundJobIdentity? CaptureFrom(ICurrentUser? user)
    {
        if (user is not { IsAuthenticated: true })
        {
            return null;
        }

        var userName = string.IsNullOrWhiteSpace(user.UserName) ? null : user.UserName;
        return userName is null && user.UserId is null ? null : new BackgroundJobIdentity(user.UserId, userName);
    }

    /// <summary>Nửa "đọc lại" — đối xứng với <see cref="CaptureFrom"/>, cùng lý do tách.</summary>
    /// <returns><c>null</c> khi job không mang danh tính nào ⇒ <c>AuditInterceptor</c> ghi
    /// <c>"system"</c> như cũ.</returns>
    public static BackgroundJobIdentity? RestoreFrom(string? userName, Guid? userId)
        => userName is null && userId is null ? null : new BackgroundJobIdentity(userId, userName);

    /// <summary>
    /// Lấy <see cref="ICurrentUser"/> của ĐÚNG request đang enqueue. Phải đi qua
    /// <c>RequestServices</c> chứ không inject thẳng: filter Hangfire là singleton, không phân giải
    /// được dịch vụ scoped từ container gốc.
    ///
    /// <para>Dùng <c>GetService</c> (không phải <c>GetRequiredService</c>) có chủ đích: ở đây một
    /// đăng ký thiếu chỉ làm mất tên người ghi audit, không đáng để làm hỏng cả lượt enqueue.</para>
    /// </summary>
    private ICurrentUser? ResolveRequestUser()
        => httpContextAccessor.HttpContext?.RequestServices?.GetService<ICurrentUser>();
}
