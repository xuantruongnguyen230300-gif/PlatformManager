using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Common;

/// <summary>
/// Dispatcher mỏng, 1 chỗ map HTTP — mọi controller kế thừa cái này thay vì ControllerBase
/// trực tiếp. KHÔNG controller nào tự try-catch/tự hardcode status code (xem
/// .claude/rules/api-controller.md §Dispatcher).
///
/// [Authorize] đặt Ở ĐÂY — fail-closed theo mặc định (mọi action của mọi controller con yêu
/// cầu đã đăng nhập trừ khi tường minh [AllowAnonymous]), KHÔNG phải opt-in (mỗi controller tự
/// khai [Authorize] riêng — dễ quên, đã xảy ra thật với AuthController.Logout trước khi sửa).
/// Controller cần role cụ thể vẫn khai thêm [Authorize(Roles = "...")] ở class/action — 2
/// attribute cộng dồn (AND), không thay thế. Xem
/// doc/huong_dan/wiki-core/be/trien-khai/05-p4-hosting-api.md §4.1.
/// </summary>
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(IApiResult<T> result)
    {
        result.TraceId ??= HttpContext.TraceIdentifier;
        var status = result.Code == ErrorCode.Success ? StatusCodes.Status200OK : (int)result.Code;
        return StatusCode(status, result);
    }
}
