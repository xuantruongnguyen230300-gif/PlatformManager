namespace PlatformManager.Core.Application.Permissions;

public sealed record ResourcePermissionRowDto(
    string ResourceKey, string ResourceName, IReadOnlyList<string> AssignedRoles);

/// <summary><paramref name="Version"/>: xem <see cref="PermissionMatrixDto"/> — cùng cơ chế, tính
/// trên bảng <c>RolePermissions</c> thay vì <c>SysMenuRoles</c>.</summary>
public sealed record ResourcePermissionMatrixDto(
    IReadOnlyList<string> Roles, IReadOnlyList<ResourcePermissionRowDto> Rows, string Version);

public sealed record ResourcePermissionEntryDto(string ResourceKey, IReadOnlyCollection<string> Roles);
