using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Api.Common;

/// <summary>
/// Bản cài <see cref="ICurrentUser"/> THỨ HAI — dùng khi mã chạy NGOÀI một HTTP request (worker
/// Hangfire, lệnh <c>--seed</c>). Đọc danh tính đã được
/// <see cref="BackgroundJobIdentityFilter"/> chụp lúc enqueue thay vì đọc <c>HttpContext</c>, thứ
/// không tồn tại ở đó (chốt Q35, xem spec/danh-muc-dti/business-rules.md §5.6).
///
/// <para>Đặt cạnh <see cref="HttpContextCurrentUser"/> vì cùng một lý do: hai bản cài của cùng một
/// seam, phụ thuộc vào ngữ cảnh chạy — thứ chỉ composition root được biết. Đây là hạ tầng DÙNG
/// CHUNG của host, phục vụ mọi job nền, không thuộc riêng feature nào.</para>
///
/// <para><b>Không có danh tính là chuyện bình thường, không phải lỗi.</b> Khi không ai kích hoạt
/// job (recurring job, job của hệ thống), mọi thứ trả về <c>null</c>/rỗng và
/// <c>AuditInterceptor</c> ghi <c>"system"</c> — đúng hành vi trước Q35, không ném lỗi.</para>
/// </summary>
public sealed class BackgroundJobCurrentUser(IBackgroundJobIdentityAccessor identityAccessor) : ICurrentUser
{
    private BackgroundJobIdentity? Identity => identityAccessor.Current;

    public bool IsAuthenticated => Identity is not null;

    public Guid? UserId => Identity?.UserId;

    public string? UserName => Identity?.UserName;

    /// <summary>
    /// LUÔN rỗng — vai trò cố ý không đi theo job. Lý do đầy đủ ở
    /// <see cref="BackgroundJobIdentity"/>: ảnh chụp vai trò lúc enqueue sẽ giữ hiệu lực cho quyền
    /// đã bị thu hồi. Job cần quyết định phân quyền thì đọc quyền hiện tại từ DB theo
    /// <see cref="UserId"/>.
    /// </summary>
    public IReadOnlyCollection<string> Roles => [];

    /// <inheritdoc cref="Roles"/>
    public bool IsInRole(string role) => false;
}
