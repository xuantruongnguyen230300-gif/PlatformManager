using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <inheritdoc cref="IUserAdminService"/>
public sealed class UserAdminService(UserManager<AppUser> userManager) : IUserAdminService
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<PagedList<UserDto>> GetListAsync(int page, int pageSize, string? searchText, CancellationToken ct)
    {
        var query = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(u =>
                u.UserName!.Contains(term) || u.FullName.Contains(term) || (u.Email != null && u.Email.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = new List<UserDto>();
        foreach (var user in items)
            dtos.Add(await ToDtoAsync(user));

        return new PagedList<UserDto> { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<bool> UserNameExistsAsync(string userName, CancellationToken ct)
        => await userManager.FindByNameAsync(userName) is not null;

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        => await userManager.FindByEmailAsync(email) is not null;

    public async Task<CreateUserOutcome> CreateAsync(
        string userName, string? email, string fullName, string tempPassword,
        IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var user = new AppUser
        {
            Id = EntityId.New(),
            UserName = userName,
            Email = email,
            FullName = fullName,
            MustChangePassword = true, // áp dụng chung cho MỌI user do Admin tạo
            DateCreate = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
            return new CreateUserOutcome(false, null, [.. createResult.Errors.Select(e => e.Description)]);

        if (roles.Count > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, roles);
            if (!roleResult.Succeeded)
                return new CreateUserOutcome(false, user.Id, [.. roleResult.Errors.Select(e => e.Description)]);
        }

        return new CreateUserOutcome(true, user.Id, []);
    }

    public async Task<bool> UpdateAsync(Guid id, string? email, string fullName, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return false;

        // Tính TRƯỚC tập role thực sự đổi — quyết định có cần đổi con dấu hay không PHẢI dựa
        // trên diff này, không phải "có gọi UpdateAsync hay không" (xem
        // doc/huong_dan/wiki-core/be/02-identity-auth.md §"Quy tắc bắt buộc: đường ghi nào phải
        // đổi con dấu"). Chỉ sửa email/fullName (roles giữ nguyên) → toAdd/toRemove đều rỗng →
        // KHÔNG đổi con dấu, đá người ta ra vì bị sửa tên là thiệt hại không mua được gì.
        var currentRoles = await userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(roles).ToList();
        var toAdd = roles.Except(currentRoles).ToList();

        // Con dấu TRƯỚC, quyền SAU — hai lệnh ghi KHÔNG chung 1 transaction (UserManager tự
        // SaveChanges mỗi lần gọi). Hỏng ở bước sau chỉ đá người dùng ra (đăng nhập lại), không
        // để quyền mới có hiệu lực mà cookie cũ còn sống tới 14 ngày (hỏng theo hướng an toàn).
        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return false;
        }

        user.Email = email;
        user.FullName = fullName;
        user.DateUpdate = DateTimeOffset.UtcNow;

        // CÙNG instance user đã đổi con dấu ở trên — UpdateAsync tự làm mới ConcurrencyStamp
        // TRÊN CHÍNH instance đó; lấy lại instance cũ sẽ ra ConcurrencyFailure.
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return false;

        if (toRemove.Count > 0)
            await userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0)
            await userManager.AddToRolesAsync(user, toAdd);

        return true;
    }

    public async Task<bool> LockAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return false;

        // Con dấu TRƯỚC SetLockoutEndDateAsync — nếu không, SecurityStampValidator (đọc con dấu
        // mỗi 30 phút) không có gì để phát hiện, và phiên đang chạy của người bị khoá sống tiếp
        // vô thời hạn (xem doc/huong_dan/wiki-core/be/02-identity-auth.md).
        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
            return false;

        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        return result.Succeeded;
    }

    public async Task<bool> UnlockAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return false;

        var result = await userManager.SetLockoutEndDateAsync(user, null);
        return result.Succeeded;
    }

    private async Task<UserDto> ToDtoAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        return new UserDto(user.Id, user.UserName!, user.Email, user.FullName, [.. roles], isLocked, user.MustChangePassword, user.DateCreate);
    }
}
