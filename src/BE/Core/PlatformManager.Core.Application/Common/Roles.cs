namespace PlatformManager.Core.Application.Common;

/// <summary>3 role đã CHỐT — tên dùng thẳng làm AspNetRoles.Name (không FK cứng bằng Guid
/// ở tầng Application, chỉ so khớp theo tên). Xem doc/ke-hoach-xay-lai-corebase.md.</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = [SuperAdmin, Admin, User];
}
