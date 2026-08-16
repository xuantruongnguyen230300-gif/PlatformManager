using Microsoft.AspNetCore.Identity;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <summary>3 role đã CHỐT: SuperAdmin/Admin/User (xem PlatformManager.Core.Application.Common.Roles).</summary>
public class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}
