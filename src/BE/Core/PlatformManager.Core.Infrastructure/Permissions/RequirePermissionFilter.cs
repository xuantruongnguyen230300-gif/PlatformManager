using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Core.Infrastructure.Permissions;

/// <summary>
/// Filter toàn cục — đối chiếu role hiện tại của user với RolePermission, trả 403 nếu không có
/// quyền. KHÔNG khai [RequirePermission] trên controller/action = không chặn thêm gì (giữ nguyên
/// [Authorize] fail-closed sẵn có ở ApiControllerBase — 2 cơ chế CỘNG DỒN, không thay thế). Đăng
/// ký DI qua AddPermissionInfrastructure() (PermissionInfrastructureExtensions.cs) — filter KHÔNG
/// tự thêm vào MVC options, Program.cs (composition root) tự gọi
/// options.Filters.Add&lt;RequirePermissionFilter&gt;(). Xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §"Phân quyền theo hành động — permission-key đầy đủ".
///
/// Truy vấn DB nằm sau <see cref="IPermissionChecker"/> chứ không inject thẳng DbContext — để
/// luồng quyết định ở đây test được không cần Postgres (xem PlatformManager.Core.UnitTests),
/// còn ngữ nghĩa truy vấn test riêng trên Postgres thật (PlatformManager.Core.IntegrationTests).
/// </summary>
public sealed class RequirePermissionFilter(IPermissionChecker permissionChecker) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .FirstOrDefault();
        if (attribute is null)
            return; // không khai [RequirePermission] = không chặn thêm, giữ nguyên [Authorize]

        var ct = context.HttpContext.RequestAborted;
        var user = context.HttpContext.User;

        var roleNames = user.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        if (roleNames.Count == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        // ── BREAK-GLASS CÓ CHỦ ĐÍCH (đã được người dùng duyệt 2026-08-19) ──────────────────
        // SuperAdmin đi qua MỌI [RequirePermission] mà KHÔNG cần dòng RolePermission nào.
        //
        // Vì sao là ngoại lệ được duyệt, không phải lỗ hổng ai đó cài vào:
        //  1. Tránh TỰ KHOÁ HỆ THỐNG. Màn ma trận phân quyền cho phép thu hồi quyền hàng loạt;
        //     nếu SuperAdmin cũng phải có RolePermission thì một lần thu nhầm là mất hẳn đường
        //     vào, chỉ còn cách sửa thẳng DB.
        //  2. (Lý do này đã HẾT HIỆU LỰC 2026-08-30 — giữ lại để thấy căn cứ đã đổi.) Trước đó:
        //     "CoreSeeder chỉ chạy khi IsDevelopment() nên môi trường thật không có dữ liệu seed".
        //     Nay seed production là lệnh `--seed` gọi chính CoreSeeder, nên vế đó không còn đúng.
        //     Quyết định GIỮ bypass không đổi — lý do 1 tự nó đã đủ. Xem
        //     doc/contracts/permissions.md §"Luật bypass SuperAdmin".
        //  3. Khớp lại với ý định đã ghi ở doc/contracts/permissions.md §"Luật bypass SuperAdmin"
        //     (file sql/0004_* đã gộp vào 0001_initial_baseline.sql khi baseline 2026-08-31)
        //     ("MỌI role, trừ SuperAdmin bypass") — trước 2026-08-19 tài liệu mô tả bypass nhưng
        //     code KHÔNG có, nay code và tài liệu thống nhất.
        //
        // HỆ QUẢ PHẢI BIẾT: quyền của SuperAdmin KHÔNG thu hồi được qua UI ma trận phân quyền —
        // gỡ quyền của SuperAdmin chỉ làm được bằng cách gỡ chính role SuperAdmin khỏi user.
        //
        // Đặt SAU bước kiểm claim rỗng là CỐ Ý: request chưa đăng nhập / không mang role nào vẫn
        // phải Forbid y như cũ, không được lọt qua đây.
        if (roleNames.Contains(Roles.SuperAdmin))
            return;

        // Deny-by-default: mọi đường không chứng minh được quyền đều ra ForbidResult.
        if (!await permissionChecker.HasPermissionAsync(roleNames, attribute.Key, ct))
            context.Result = new ForbidResult();
    }
}
