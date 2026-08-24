namespace PlatformManager.Core.Application.Permissions;

public sealed record ResourcePermissionRowDto(
    string ResourceKey, string ResourceName, IReadOnlyList<string> AssignedRoles);

public sealed record ResourcePermissionMatrixDto(
    IReadOnlyList<string> Roles, IReadOnlyList<ResourcePermissionRowDto> Rows);

public sealed record ResourcePermissionEntryDto(string ResourceKey, IReadOnlyCollection<string> Roles);
