using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <inheritdoc cref="IUserLookupService"/>
public sealed class UserLookupService(UserManager<AppUser> userManager) : IUserLookupService
{
    public async Task<(Guid? OwnerId, bool WasCreated)> ResolveOrCreateByFullNameAsync(string fullName, CancellationToken ct)
    {
        var trimmed = fullName.Trim();
        var matches = await userManager.Users
            .Where(u => u.FullName.ToLower() == trimmed.ToLower())
            .ToListAsync(ct);

        if (matches.Count == 1)
            return (matches[0].Id, false);

        if (matches.Count > 1)
            return (null, false); // trùng nhiều người — KHÔNG tự đoán

        // Chưa từng có -> tự tạo user "nhãn phụ trách", mật khẩu ngẫu nhiên không ai biết,
        // chỉ dùng được sau khi Admin cấp lại thông tin qua màn Quản trị người dùng.
        var userName = await GenerateUniqueUserNameAsync(trimmed, ct);
        var user = new AppUser
        {
            Id = EntityId.New(),
            UserName = userName,
            FullName = trimmed,
            Email = null,
            MustChangePassword = true,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var tempPassword = $"Tmp!{Guid.NewGuid():N}9a";
        var result = await userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            return (null, false);

        await userManager.AddToRoleAsync(user, Roles.User);

        return (user.Id, true);
    }

    private async Task<string> GenerateUniqueUserNameAsync(string fullName, CancellationToken ct)
    {
        var baseName = new string([.. fullName.Where(char.IsLetterOrDigit)]).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "user";

        var candidate = baseName;
        var suffix = 1;
        while (await userManager.FindByNameAsync(candidate) is not null)
        {
            suffix++;
            candidate = $"{baseName}{suffix}";
        }

        return candidate;
    }
}
