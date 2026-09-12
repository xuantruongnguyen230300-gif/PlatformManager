using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Business.Application.Permissions;

/// <summary>
/// Trả lời đúng MỘT câu hỏi: <i>"người gọi request này có được GHI dữ liệu DTI không"</i> —
/// nguồn của cờ <c>canWrite</c> ở DM-2 (doc/contracts/danh-muc-dti.md DM-2 mục 3).
///
/// <para><b>Vì sao là một seam riêng chứ không gọi thẳng <c>IPermissionChecker</c> trong handler:</b>
/// câu trả lời gồm HAI vế — break-glass của <c>SuperAdmin</c> và phép tra <c>dti.manage</c> — và
/// vế thứ nhất rất dễ quên. Quên nó thì một <c>SuperAdmin</c> nhận <c>canWrite = false</c> trong
/// khi mọi endpoint ghi vẫn cho họ qua (<c>RequirePermissionFilter</c> bypass), tức FE ẩn nút của
/// đúng người có toàn quyền. Gói hai vế vào một chỗ để handler thứ hai (và vòng 2) không dựng lại
/// một nửa.</para>
/// </summary>
public interface IDtiWriteAccess
{
    Task<bool> CanWriteAsync(CancellationToken ct);
}

/// <inheritdoc />
internal sealed class DtiWriteAccess(ICurrentUser currentUser, IPermissionChecker permissionChecker)
    : IDtiWriteAccess
{
    public async Task<bool> CanWriteAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return false;

        // Break-glass: SuperAdmin đi qua MỌI [RequirePermission] mà không cần dòng RolePermission
        // nào (RequirePermissionFilter, quyết định người dùng 2026-08-19). Cờ canWrite phải nói
        // đúng sự thật đó, nếu không FE ẩn nút của người vẫn ghi được.
        if (currentUser.IsInRole(Roles.SuperAdmin))
            return true;

        var roles = currentUser.Roles;
        if (roles.Count == 0)
            return false;

        // KHÔNG cache — Core cố ý không cache phép kiểm quyền để việc thu hồi có hiệu lực ngay ở
        // request kế tiếp (IPermissionChecker). Tính lại mỗi request là lý do canWrite không có
        // cửa sổ lệch, khác hẳn một payload cached-at-bootstrap như GET /api/auth/me.
        return await permissionChecker.HasPermissionAsync(roles, BusinessResourceKeys.DtiManage, ct);
    }
}
