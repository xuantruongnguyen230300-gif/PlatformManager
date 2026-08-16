namespace PlatformManager.Core.Application.Users;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string? Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsLocked,
    bool MustChangePassword,
    DateTimeOffset? DateCreate);
