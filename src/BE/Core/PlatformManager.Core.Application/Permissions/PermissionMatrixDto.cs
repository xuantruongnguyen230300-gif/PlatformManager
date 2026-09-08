namespace PlatformManager.Core.Application.Permissions;

public sealed record PermissionMatrixRowDto(
    Guid SysMenuId, string SysMenuCode, string SysMenuName, Guid? ParentId, IReadOnlyList<string> AssignedRoles);

/// <summary>
/// <paramref name="Version"/> (thêm 2026-08-31) là token phiên bản của toàn bộ ma trận — FE giữ
/// nguyên chuỗi nhận được và gửi lại y hệt trong <c>PUT</c>; KHÔNG tự sinh, KHÔNG diễn giải nội
/// dung. Xem <see cref="MatrixVersion"/> và doc/contracts/permissions.md.
/// </summary>
public sealed record PermissionMatrixDto(
    IReadOnlyList<string> Roles, IReadOnlyList<PermissionMatrixRowDto> Rows, string Version);

public sealed record PermissionMatrixEntryDto(Guid SysMenuId, IReadOnlyCollection<string> Roles);
