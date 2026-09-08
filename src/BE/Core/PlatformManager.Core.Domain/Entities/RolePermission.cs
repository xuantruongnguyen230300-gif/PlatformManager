using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Bảng nối nhiều-nhiều Role↔ResourceKey. Đây là ma trận "role được thao tác resource nào"
/// (permission-key đầy đủ) — KHÁC
/// với SysMenuRole (chỉ điều khiển menu nhìn thấy được, không chặn gọi API trực tiếp). Quy ước:
/// role KHÔNG có dòng nào ở đây cho 1 ResourceKey = KHÔNG được thao tác resource đó
/// (deny-by-default — ngược với SysMenuRole, xem RequirePermissionFilter +
/// doc/contracts/permissions.md CONTRACT PERM-2 §"Rủi ro rollout"). ResourceKey phải khớp một mục
/// trong danh mục do HOST khai (ICoreResourceKeySource, tách 2026-09-03) — KHÔNG lưu tên hiển thị
/// ở đây, nhãn đi kèm khoá ở phía host và không lưu DB. RoleId trỏ AspNetRoles.Id (Identity, thuộc Infrastructure) — chỉ
/// Guid FK thuần, giống cách SysMenuRole.RoleId không cần biết kiểu AppRole cụ thể.
///
/// <para><b>KẾ THỪA BaseEntity từ 2026-08-31</b> — cùng quyết định, cùng lý do và cùng hệ quả về
/// khoá như <see cref="SysMenuRole"/>: PK là <see cref="BaseEntity.Id"/>, tính duy nhất của cặp
/// (RoleId, ResourceKey) giữ bằng unique index LỌC theo <c>IsDeleted = false</c> (xem
/// RolePermissionConfiguration). Nguồn: doc/huong_dan/quy-uoc/be-entity-domain.md
/// §"Quyết định người dùng 2026-08-31".</para>
///
/// <para><b>Deny-by-default KHÔNG đổi nghĩa sau khi có soft-delete:</b> dòng đã xoá mềm là vô
/// hình với mọi truy vấn đọc (global query filter), nên "role không có dòng ĐANG SỐNG cho key
/// này" vẫn là từ chối. Dòng đã xoá mềm chỉ để tra lại lịch sử, KHÔNG cấp quyền cho ai.</para>
/// </summary>
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; private set; }
    public string ResourceKey { get; private set; } = string.Empty;

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, string resourceKey)
    {
        if (roleId == Guid.Empty)
            throw new DomainException(RolePermissionErrors.RoleRequired);
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new DomainException(RolePermissionErrors.ResourceKeyRequired);

        return new RolePermission { RoleId = roleId, ResourceKey = resourceKey };
    }
}
