using System.Security.Claims;
using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Api.Common;

/// <summary>
/// Implement ICurrentUser đọc từ ClaimsPrincipal của cookie session hiện tại — đặt ở Api
/// (composition root) vì phụ thuộc HttpContext, một khái niệm ASP.NET Core mà Application
/// không được biết tới (xem .claude/rules/api-controller.md §Auth/Permission).
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name) ?? User?.Identity?.Name;

    public IReadOnlyCollection<string> Roles
        => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
