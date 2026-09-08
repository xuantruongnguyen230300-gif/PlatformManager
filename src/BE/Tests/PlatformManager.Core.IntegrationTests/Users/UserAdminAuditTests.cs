using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.IntegrationTests.Auth;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Users;

/// <summary>
/// Khoá/mở khoá tài khoản phải để lại DẤU VẾT ở cột audit <c>DateUpdate</c> (finding F8, sửa
/// 2026-08-28).
///
/// Vì sao dễ sót: <see cref="AppUser"/> KHÔNG kế thừa <c>BaseEntity</c> — Identity tự quản vòng
/// đời entity của nó — nên nó nằm NGOÀI tầm với của <c>AuditInterceptor</c>, thứ chỉ duyệt
/// <c>ChangeTracker.Entries&lt;BaseEntity&gt;()</c>. Mọi đường ghi chạm AppUser phải tự ghi audit
/// bằng tay; <c>CreateAsync</c>/<c>UpdateAsync</c> có làm, còn
/// <c>LockAsync</c>/<c>UnlockAsync</c> thì quên. Không có gì báo: không lỗi biên dịch, không
/// ngoại lệ lúc chạy, chỉ là quyền truy cập của một người vừa bị đảo mà cột "sửa lần cuối" đứng
/// yên.
///
/// Test gọi thẳng <see cref="IUserAdminService"/> chứ không qua HTTP: <c>SuperAdminAccountGuard</c>
/// nằm ở handler và cần một người dùng đang đăng nhập, trong khi thứ đang kiểm là tầng ghi.
/// Đường HTTP của lock/unlock đã có bộ test riêng ở <c>Auth/</c>.
///
/// <para><b>Bổ sung 2026-09-01 — cột <c>UpdatedBy</c>.</b> <c>AspNetUsers</c> vừa có thêm
/// <c>CreatedBy</c>/<c>UpdatedBy</c> (schema baseline 2026-08-31). Chúng nằm trong CÙNG lớp rủi ro
/// với <c>DateUpdate</c> và vì đúng một lý do: <see cref="AppUser"/> không kế thừa
/// <c>BaseEntity</c> nên <c>AuditInterceptor</c> không đụng tới, mọi đường ghi phải tự điền tay.
/// Thêm cột mà không thêm khẳng định nghĩa là có thêm một cột nữa im lặng đứng yên.</para>
///
/// <para>Không có phiên đăng nhập nào ở đường gọi này (test gọi từ DI scope), nên giá trị kỳ vọng
/// là <c>"system"</c> — đúng quy ước <c>UserAdminService.ActorName</c> dùng chung với
/// <c>AuditInterceptor</c>.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserAdminAuditTests : IDisposable
{
    private const string Password = "Test@123456789";

    private readonly PostgresFixture _fixture;
    private readonly SessionTerminationFactory _factory;

    public UserAdminAuditTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new SessionTerminationFactory();
    }

    public void Dispose() => _factory.Dispose();

    [Fact(DisplayName = "LockAsync → DateUpdate được ghi mới")]
    public async Task LockAsync_WritesDateUpdate()
    {
        var userId = await CreateUserAsync();

        // Mốc so sánh lấy TRƯỚC lời gọi, không phải giá trị cũ trong DB: user vừa tạo có
        // DateUpdate = null, nên so "khác null" thôi sẽ vẫn xanh nếu ai đó ghi một hằng số bất kỳ.
        var before = DateTimeOffset.UtcNow;

        Assert.True(await LockAsync(userId));

        var (dateUpdate, updatedBy) = await ReadAuditAsync(userId);
        Assert.NotNull(dateUpdate);
        Assert.True(dateUpdate >= before,
            $"DateUpdate ({dateUpdate:O}) phải >= mốc trước khi khoá ({before:O}).");

        // Cột mới 2026-08-31 — "system" vì không có phiên đăng nhập nào ở đường gọi này.
        Assert.Equal("system", updatedBy);
    }

    [Fact(DisplayName = "UnlockAsync → DateUpdate được ghi mới (khác lần khoá)")]
    public async Task UnlockAsync_WritesDateUpdate()
    {
        var userId = await CreateUserAsync();

        Assert.True(await LockAsync(userId));
        var (afterLock, _) = await ReadAuditAsync(userId);
        Assert.NotNull(afterLock);

        Assert.True(await UnlockAsync(userId));
        var (afterUnlock, updatedBy) = await ReadAuditAsync(userId);

        Assert.NotNull(afterUnlock);
        Assert.Equal("system", updatedBy);
        Assert.True(afterUnlock > afterLock,
            $"DateUpdate sau mở khoá ({afterUnlock:O}) phải MỚI HƠN sau khoá ({afterLock:O}) — " +
            "bằng nhau nghĩa là đường mở khoá không ghi audit, chỉ thừa hưởng dấu vết của lần khoá.");
    }

    private async Task<bool> LockAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserAdminService>()
            .LockAsync(userId, CancellationToken.None);
    }

    private async Task<bool> UnlockAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserAdminService>()
            .UnlockAsync(userId, CancellationToken.None);
    }

    private async Task<Guid> CreateUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var userName = $"it-audit-{Guid.NewGuid():N}"[..24];
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = $"{userName}@it.local",
            FullName = userName,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        return user.Id;
    }

    private async Task<(DateTimeOffset? DateUpdate, string? UpdatedBy)> ReadAuditAsync(Guid userId)
    {
        await using var db = _fixture.CreateDbContext();
        var row = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.DateUpdate, u.UpdatedBy })
            .FirstAsync();

        return (row.DateUpdate, row.UpdatedBy);
    }
}
