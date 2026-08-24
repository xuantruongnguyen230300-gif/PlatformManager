namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Seam tra quyền theo (role hiện tại × ResourceKey) — tách khỏi <c>RequirePermissionFilter</c>
/// để phần LUỒNG QUYẾT ĐỊNH của filter (claim rỗng → Forbid, SuperAdmin → break-glass, không có
/// quyền → Forbid) test được mà KHÔNG cần Postgres, còn phần NGỮ NGHĨA TRUY VẤN (join
/// AspNetRoles × RolePermissions, dòng mồ côi, <c>Name</c> NULL) vẫn phải test trên Postgres thật
/// (Testcontainers) vì nó là hành vi của SQL chứ không phải của C#.
///
/// Xem doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Kiểm thử phân quyền" — chia đôi như
/// vậy là có chủ đích: test bằng LINQ-to-Objects trên <c>List&lt;T&gt;</c> chỉ kiểm được phép tập
/// hợp, KHÔNG kiểm được EF còn dịch đúng sang SQL sau khi nâng version, nên không thay thế được
/// nhóm test chạy trên DB thật.
///
/// KHÔNG cache — quyền thu hồi phải có hiệu lực NGAY, xem
/// doc/huong_dan/wiki-core/be/11-performance-caching.md §6.2 quyết định #5.
/// </summary>
public interface IPermissionChecker
{
    /// <param name="roleNames">Tên role lấy từ claim của user hiện tại (đã loại trùng, KHÔNG rỗng).</param>
    /// <param name="resourceKey">Key khai trong <see cref="RequirePermissionAttribute"/> của endpoint.</param>
    /// <returns><c>true</c> nếu CÓ ÍT NHẤT 1 role trong <paramref name="roleNames"/> được cấp
    /// <paramref name="resourceKey"/>. Deny-by-default: không chứng minh được thì trả <c>false</c>.</returns>
    Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleNames, string resourceKey, CancellationToken ct);
}
