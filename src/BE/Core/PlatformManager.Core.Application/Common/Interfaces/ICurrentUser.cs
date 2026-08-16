namespace PlatformManager.Core.Application.Common.Interfaces;

/// <summary>
/// Abstraction thuần Application cho user hiện tại — implement thật
/// (HttpContextCurrentUser, đọc claims) nằm ở PlatformManager.Api (composition root),
/// vì phụ thuộc HttpContext là khái niệm ASP.NET Core, Application không được biết tới.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);
}
