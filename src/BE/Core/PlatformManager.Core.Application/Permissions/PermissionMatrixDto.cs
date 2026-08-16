namespace PlatformManager.Core.Application.Permissions;

public sealed record PermissionMatrixRowDto(
    Guid SysMenuId, string SysMenuCode, string SysMenuName, Guid? ParentId, IReadOnlyList<string> AssignedRoles);

public sealed record PermissionMatrixDto(IReadOnlyList<string> Roles, IReadOnlyList<PermissionMatrixRowDto> Rows);

public sealed record PermissionMatrixEntryDto(Guid SysMenuId, IReadOnlyCollection<string> Roles);
