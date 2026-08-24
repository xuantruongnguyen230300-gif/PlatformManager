using Hangfire.Dashboard;
using PlatformManager.Core.Application.Common;

namespace PlatformManager.Api.Common;

/// <summary>
/// Hangfire Dashboard ("/hangfire") KHÔNG có auth mặc định — để mở nguyên là lộ toàn bộ
/// job/data (kể cả nội dung ImportJob đang chạy) ra ngoài. Chỉ Roles.SuperAdmin được xem, cùng
/// mẫu PermissionsController. Bắt buộc, không phải tuỳ chọn — xem
/// doc/huong_dan/wiki-core/be/07-observability.md §"Hangfire Dashboard".
/// </summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(Roles.SuperAdmin);
    }
}
