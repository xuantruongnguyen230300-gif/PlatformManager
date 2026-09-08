using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Permissions;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// Endpoint THĂM DÒ, chỉ tồn tại trong assembly test — nạp vào host qua <c>AssemblyPart</c>
/// (xem <see cref="RequirePermissionSeamTests"/>). Không nằm trong sản phẩm, không route nào của
/// người dùng chạm tới.
///
/// <para><b>Vì sao thăm dò thay vì dùng endpoint nghiệp vụ thật.</b> Bản trước của
/// <see cref="RequirePermissionSeamTests"/> gọi <c>GET /api/criteria</c> của module DtiWeekly.
/// Thứ nó cần chứng minh là <c>options.Filters.Add&lt;RequirePermissionFilter&gt;()</c> +
/// <c>AddPermissionInfrastructure()</c> ở <c>Program.cs</c> — hoàn toàn thuộc Core — nhưng nó lại
/// mượn một endpoint của module để chứng minh. Khi module đó bị xoá (2026-08-29), test đỏ dù
/// KHÔNG có gì trong Core thay đổi: đỏ vì lý do sai. Endpoint thăm dò gỡ đúng sự phụ thuộc đó, và
/// giữ test sống độc lập với việc có/không có module nghiệp vụ nào.</para>
///
/// <para>Body trả về không quan trọng — test chỉ đọc status code (403 trước khi cấp quyền, 200
/// sau khi cấp).</para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/_test/permission-probe")]
public sealed class PermissionSeamProbeController : ControllerBase
{
    [HttpGet]
    [RequirePermission(AppResourceKeys.Import)]
    public IActionResult Probe() => Ok(new { probe = true });
}
