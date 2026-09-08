using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Menu;

/// <summary>
/// <c>CoreSeeder</c> phải nhìn thấy menu ĐÃ XOÁ MỀM và HỒI SINH nó, không được chèn dòng mới.
///
/// <para><b>Vì sao ca này nguy hiểm hơn nó trông:</b> <c>UpsertMenuAsync</c> đọc qua global query
/// filter soft-delete thì dòng đã xoá mềm là vô hình, nên seeder rơi vào nhánh "chưa có thì
/// thêm". TRƯỚC bản vá 2026-08-28, <c>IX_SysMenus_Code</c> unique TOÀN BẢNG nên Postgres chặn bằng
/// <c>23505</c> — hỏng nhưng ồn ào, không ai bỏ sót. SAU bản vá đó (unique partial
/// <c>WHERE "IsDeleted" = false</c>) đúng lượt chèn ấy THÀNH CÔNG và để lại 2 dòng cùng
/// <c>Code</c>: sidebar hiện menu nhân đôi, còn <c>UpsertMenuRoleAsync</c> chỉ gắn role cho dòng
/// mới. Không exception, không log, không test nào khác báo.</para>
///
/// <para>(Sửa chú thích 2026-09-01: hai câu trên trước đây gọi bản vá này là "migration 0008" —
/// lịch sử migration đã baseline lại 2026-08-31, sáu file 0003–0008 gộp vào
/// <c>sql/0001_initial_baseline.sql</c>. Index thì không đổi.)</para>
///
/// <para><b>Vì sao phải là INTEGRATION test:</b> thứ làm ca này im lặng chính là index partial
/// trong Postgres. Trên model EF hay in-memory, lần chèn thứ hai không bị chặn bởi bất cứ thứ gì
/// nên test sẽ "xanh vì sai lý do" — nó không chứng minh được rằng DB thật đã ngừng chặn hộ.</para>
///
/// <para><b>Canary (đã chạy 2026-08-28):</b> bỏ <c>IgnoreQueryFilters()</c> khỏi
/// <c>CoreSeeder.UpsertMenuAsync</c> → test này đỏ ở assert "chỉ còn đúng 1 dòng" (thấy 2). Khôi
/// phục → xanh.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CoreSeederSoftDeletedMenuTests : IAsyncLifetime
{
    /// <summary>
    /// Menu do <b>host</b> khai — <c>PlatformManager.Api/Seeding/AppMenuSeedSource.cs</c>, chứ
    /// KHÔNG phải <c>CoreSeeder</c> (sửa chú thích 2026-09-02: dữ liệu menu đã tách khỏi
    /// <c>Core.Infrastructure</c> ra seam <c>ICoreMenuSeedSource</c> cùng ngày; nhãn/route/icon
    /// dưới đây là của DỰ ÁN này, Core không biết và không được biết mục nào tồn tại).
    ///
    /// <para>Chọn "trang-chu" có chủ đích: nó KHÔNG gắn <c>SysMenuRole</c> nào, nên test không
    /// đụng dữ liệu phân quyền mà các test khác trong cùng collection đang dùng. (Trước 2026-08-29
    /// dùng "dashboard" — menu đó do module nghiệp vụ sở hữu và đã bị xoá cùng module.)</para>
    ///
    /// <para>⚠️ Test này bám vào dữ liệu THẬT của host có chủ đích — thứ nó kiểm là nhánh hồi sinh
    /// chạy trên đúng dòng mà lệnh <c>--seed</c> sẽ chạm. Cơ chế seed đo trên nguồn GIẢ nằm ở
    /// <see cref="CoreSeederMenuSeedSourceTests"/>; đổi bảng menu ở host thì sửa 5 giá trị kỳ vọng
    /// dưới đây, đừng sửa bên đó.</para>
    /// </summary>
    private const string MenuCode = "trang-chu";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public CoreSeederSoftDeletedMenuTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName = "Xoá mềm menu rồi chạy lại CoreSeeder → HỒI SINH dòng cũ, KHÔNG nhân đôi Code")]
    public async Task ReSeed_AfterSoftDelete_RevivesRow_InsteadOfInsertingDuplicate()
    {
        // 1. Seed (idempotent — host đã chạy lúc boot, gọi lại để test không phụ thuộc thứ tự).
        await RunCoreSeederAsync();
        Assert.Equal(1, await CountByCodeIgnoringFiltersAsync());

        // 2. Xoá mềm + làm hỏng luôn phần định nghĩa. Dùng SQL thô vì mọi field nghiệp vụ của
        //    SysMenu là private set (đúng chủ đích) — và vì thứ cần dựng là TRẠNG THÁI DB, không
        //    phải hành vi của entity.
        await using (var db = CreateDbContext())
        {
            var affected = await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE core."SysMenus"
                SET "IsDeleted" = true, "Name" = 'ĐÃ XOÁ', "Route" = '/rac',
                    "Icon" = 'pi-trash', "DisplayOrder" = 99
                WHERE "Code" = {0}
                """,
                MenuCode);

            // Chặn "xanh rỗng": mã menu đổi tên mà không ai sửa test thì UPDATE không chạm dòng
            // nào, và mọi assert dưới vẫn đúng một cách vô nghĩa.
            Assert.Equal(1, affected);
        }

        // 3. Chạy lại seeder — đây là hành vi đang kiểm.
        await RunCoreSeederAsync();

        // 4. ĐÚNG MỘT dòng mang Code này, kể cả khi bỏ qua query filter.
        Assert.Equal(1, await CountByCodeIgnoringFiltersAsync());

        await using (var verify = CreateDbContext())
        {
            var revived = await verify.SysMenus
                .IgnoreQueryFilters()
                .SingleAsync(menu => menu.Code == MenuCode);

            Assert.False(revived.IsDeleted);

            // Hồi sinh phải đồng bộ LẠI định nghĩa, không chỉ bật cờ IsDeleted: bỏ phần cập nhật
            // đi thì menu sống lại với nội dung rác.
            Assert.Equal("Trang chủ", revived.Name);
            Assert.Equal("/trang-chu", revived.Route);
            Assert.Equal("pi-home", revived.Icon);
            Assert.Null(revived.ParentId);
            Assert.Equal(1, revived.DisplayOrder);
        }
    }

    private async Task RunCoreSeederAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<CoreSeeder>();
        await seeder.SeedAsync();
    }

    private async Task<int> CountByCodeIgnoringFiltersAsync()
    {
        await using var db = CreateDbContext();
        return await db.SysMenus.IgnoreQueryFilters().CountAsync(menu => menu.Code == MenuCode);
    }

    /// <summary>DbContext RIÊNG, ngoài scope của host — để phép đếm không đọc trúng change
    /// tracker của chính context mà seeder vừa dùng.</summary>
    private PlatformManagerDbContext CreateDbContext() => _fixture.CreateDbContext();
}
