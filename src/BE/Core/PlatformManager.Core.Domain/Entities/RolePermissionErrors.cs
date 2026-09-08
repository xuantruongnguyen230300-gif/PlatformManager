using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>Catalog lỗi của <see cref="RolePermission"/>.</summary>
public static class RolePermissionErrors
{
    public static readonly DomainError RoleRequired = new(
        "ROLE_PERMISSION.ROLE_REQUIRED", "RoleId không được để trống.");

    public static readonly DomainError ResourceKeyRequired = new(
        "ROLE_PERMISSION.RESOURCE_KEY_REQUIRED", "ResourceKey không được để trống.");
}
