using PlatformManager.Core.Application.Common.Models;

namespace PlatformManager.Core.Application.Users;

/// <summary>Kết quả tạo user qua Identity — IdentityResult có thể trả nhiều lỗi cùng lúc
/// (trùng UserName, password không đủ mạnh...), không rút gọn về 1 ErrorDescriptor.</summary>
public sealed record CreateUserOutcome(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors);

/// <summary>
/// Implement ở Infrastructure/Identity (dùng UserManager&lt;AppUser&gt;/RoleManager&lt;AppRole&gt;) —
/// Application không được biết tới kiểu AppUser/AppRole cụ thể (đó là kiểu Identity, thuộc
/// Infrastructure theo .claude/rules/api-controller.md §Auth/Permission).
/// </summary>
public interface IUserAdminService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedList<UserDto>> GetListAsync(int page, int pageSize, string? searchText, CancellationToken ct);

    Task<bool> UserNameExistsAsync(string userName, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    /// <summary>Tạo user với mật khẩu tạm — MustChangePassword=true áp dụng chung cho MỌI
    /// user do Admin tạo (xem doc/ke-hoach-xay-lai-corebase.md).</summary>
    Task<CreateUserOutcome> CreateAsync(
        string userName, string? email, string fullName, string tempPassword,
        IReadOnlyCollection<string> roles, CancellationToken ct);

    Task<bool> UpdateAsync(Guid id, string? email, string fullName, IReadOnlyCollection<string> roles, CancellationToken ct);

    Task<bool> LockAsync(Guid id, CancellationToken ct);

    Task<bool> UnlockAsync(Guid id, CancellationToken ct);
}
