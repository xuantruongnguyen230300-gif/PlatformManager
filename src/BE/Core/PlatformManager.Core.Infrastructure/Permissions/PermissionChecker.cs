using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Core.Infrastructure.Permissions;

/// <inheritdoc cref="IPermissionChecker"/>
public sealed class PermissionChecker(PlatformManagerDbContext db) : IPermissionChecker
{
    /// <summary>
    /// ĐÚNG 1 query (A0, doc/huong_dan/wiki-core/be/11-performance-caching.md §6.3) — trước đây
    /// tách làm 2: lấy roleIds theo tên rồi mới <c>AnyAsync</c> trên RolePermissions.
    ///
    /// Ngữ nghĩa từng nhánh (test trên Postgres thật ở PlatformManager.Core.IntegrationTests):
    /// <list type="bullet">
    /// <item>Role trong claim không tồn tại trong AspNetRoles → INNER JOIN không khớp → false.</item>
    /// <item>Role hợp lệ nhưng không được cấp ResourceKey → không khớp → false.</item>
    /// <item>Nhiều role, chỉ 1 role có quyền → khớp 1 dòng → true (EXISTS, không cần đếm).</item>
    /// <item>Dòng RolePermission MỒ CÔI (RoleId không còn trong AspNetRoles) → INNER JOIN loại
    ///   bỏ → false. Đây là lý do dùng INNER JOIN chứ không lọc thẳng trên RolePermissions.</item>
    /// <item><c>AspNetRoles.Name</c> NULL → <c>NULL IN (...)</c> ra NULL (không phải TRUE) trong
    ///   SQL → false. Deny-by-default vẫn giữ.</item>
    /// </list>
    /// Chỉ ĐỌC — EXISTS, không materialize entity nào nên không phát sinh change tracking.
    /// </summary>
    public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleNames, string resourceKey, CancellationToken ct)
        => (from rolePermission in db.RolePermissions
            join role in db.Roles on rolePermission.RoleId equals role.Id
            where rolePermission.ResourceKey == resourceKey && roleNames.Contains(role.Name!)
            select rolePermission.ResourceKey)
           .AnyAsync(ct);
}
