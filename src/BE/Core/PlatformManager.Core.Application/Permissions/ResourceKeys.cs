namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Permission-key đầy đủ cho hành động ghi dữ liệu nghiệp vụ — KHÁC với SysMenu (chỉ điều khiển
/// menu nhìn thấy được, không chặn gọi API trực tiếp). Mỗi key gắn 1 [RequirePermission] trên
/// controller/action; RolePermission map role nào được cấp key nào. Thô (không phân biệt
/// View/Create/Delete) theo đúng bản rút gọn đã CHỐT ở
/// .claude/rules/api-controller.md §"Phân quyền theo hành động — permission-key đầy đủ".
/// </summary>
public static class ResourceKeys
{
    public const string Criteria = "criteria.manage";
    public const string CriteriaGroups = "criteria-groups.manage";
    public const string Import = "import.manage";

    public static readonly string[] All = [Criteria, CriteriaGroups, Import];

    /// <summary>Tên hiển thị tiếng Việt cho GET /api/admin/permissions/resources — map TĨNH,
    /// KHÔNG lưu trong DB (RolePermission chỉ lưu ResourceKey). Xem
    /// doc/contracts/permissions.md CONTRACT PERM-2.</summary>
    public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        [Criteria] = "Quản lý chỉ tiêu",
        [CriteriaGroups] = "Quản lý nhóm chỉ tiêu",
        [Import] = "Import CSV/Excel",
    };
}
