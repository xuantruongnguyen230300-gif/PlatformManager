using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.IntegrationTests.Auth;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Users;

/// <summary>
/// <see cref="IUserLookupService"/> là seam CHỈ TRA CỨU — ba nhánh, và cả ba đều phải chạy trên
/// Postgres thật: phép so <c>ToLower()</c> ở nhánh khớp tên là thứ EF Core dịch sang SQL, nên
/// một bài test LINQ-to-Objects sẽ xanh kể cả khi bản dịch SQL hỏng.
///
/// <para><b>Nhánh "không ai khớp" là nhánh quan trọng nhất của bộ này</b> (sửa 2026-09-09): bản
/// trước của service TỰ TẠO một <c>AppUser</c> mới ở đúng nhánh đó. Nó ngược
/// <c>spec/danh-muc-dti/business-rules.md</c> §6.3 (<i>"`Phụ trách` không khớp `AppUser.FullName`
/// nào → KHÔNG lỗi — `OwnerId` để trống"</i>), và hậu quả không lộ ra ở đường trả về: hàm vẫn trả
/// một Guid hợp lệ, chỉ có bảng người dùng lặng lẽ phình thêm một tài khoản không ai đăng nhập
/// được cho mỗi tên lạ trong file import. Vì vậy test dưới đây khẳng định CẢ HAI: trả
/// <c>null</c>, và <b>số dòng người dùng không đổi</b>.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserLookupServiceTests : IDisposable
{
    private const string Password = "Test@123456789";

    private readonly PostgresFixture _fixture;
    private readonly SessionTerminationFactory _factory;

    public UserLookupServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new SessionTerminationFactory();
    }

    public void Dispose() => _factory.Dispose();

    [Fact(DisplayName = "Đúng 1 user khớp FullName → trả Id user đó")]
    public async Task SingleMatch_ReturnsThatId()
    {
        var fullName = NewFullName();
        var expected = await CreateUserAsync(fullName);

        Assert.Equal(expected, await ResolveAsync(fullName));
    }

    [Fact(DisplayName = "Khớp bỏ qua hoa/thường và khoảng trắng thừa hai đầu")]
    public async Task SingleMatch_IsCaseInsensitiveAndTrimmed()
    {
        var fullName = NewFullName();
        var expected = await CreateUserAsync(fullName);

        Assert.Equal(expected, await ResolveAsync($"   {fullName.ToUpperInvariant()}  "));
    }

    [Fact(DisplayName = "Không ai khớp → null, và KHÔNG tạo user mới")]
    public async Task NoMatch_ReturnsNull_AndCreatesNothing()
    {
        var before = await CountUsersAsync();

        Assert.Null(await ResolveAsync(NewFullName()));

        Assert.Equal(before, await CountUsersAsync());
    }

    [Fact(DisplayName = "Trùng ≥2 user cùng tên → null, KHÔNG tự đoán")]
    public async Task AmbiguousMatch_ReturnsNull()
    {
        var fullName = NewFullName();
        await CreateUserAsync(fullName);
        await CreateUserAsync(fullName);

        Assert.Null(await ResolveAsync(fullName));
    }

    private static string NewFullName() => $"Nguyễn Tra Cứu {Guid.NewGuid():N}";

    private async Task<Guid?> ResolveAsync(string fullName)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserLookupService>()
            .ResolveByFullNameAsync(fullName, CancellationToken.None);
    }

    private async Task<int> CountUsersAsync()
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Users.AsNoTracking().CountAsync();
    }

    private async Task<Guid> CreateUserAsync(string fullName)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var userName = $"it-lookup-{Guid.NewGuid():N}"[..24];
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = $"{userName}@it.local",
            FullName = fullName,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        return user.Id;
    }
}
