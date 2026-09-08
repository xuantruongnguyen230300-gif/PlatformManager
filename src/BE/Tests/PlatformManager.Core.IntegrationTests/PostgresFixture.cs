using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PlatformManager.Api.Modules;
using PlatformManager.Core.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace PlatformManager.Core.IntegrationTests;

/// <summary>
/// Postgres THẬT trong container, schema dựng từ CHÍNH các file
/// <c>src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/*.sql</c> mà
/// người dùng chạy tay lên DB thật — KHÔNG dùng <c>EnsureCreated()</c> từ model EF.
///
/// Vì sao chọn nguồn .sql (quyết định 2026-08-19): như vậy test kiểm luôn tính đúng của chính
/// file .sql sẽ được áp lên production. Dựng schema từ model EF sẽ test trên một schema KHÁC
/// schema thật — mà đợt tối ưu A2 vừa rồi cho thấy khác biệt schema (index) đúng là thứ đáng
/// quan tâm. Thêm file .sql mới (0008...) thì PHẢI thêm tên vào <see cref="MigrationScripts"/>,
/// nếu không schema test sẽ lệch schema thật đúng cái điều đang muốn tránh.
///
/// Sửa 2026-08-24: đường dẫn TRƯỚC trỏ <c>doc/ERD/migrations</c> — thư mục đó đã xoá 2026-08-23
/// khi hợp nhất nguồn schema về <c>doc/cau-truc-database.md</c>/<c>.sql</c>, khiến
/// <see cref="FindRepositoryRoot"/> luôn ném <see cref="DirectoryNotFoundException"/> và
/// KHÔNG bộ integration test nào chạy được (phát hiện khi build+test thật lần đầu, không phải lý
/// thuyết). Nguồn .sql THẬT nằm trong cây mã nguồn, không phải một thư mục tài liệu riêng — trỏ
/// về đó.
///
/// Sửa 2026-09-04: câu trước nói .sql "nằm cạnh chính migration C# tương ứng". Không còn đúng —
/// theo chốt "migration thuộc host, Core ship .sql", các file .cs migration đã chuyển sang
/// <c>src/BE/PlatformManager.Api/Persistence/Migrations/</c>, còn thư mục <c>sql/</c> Ở LẠI Core
/// vì nó là ARTIFACT schema mà Corebase ship cho dự án sau. Đường dẫn trong
/// <see cref="MigrationsSqlDirectorySegments"/> vì thế KHÔNG đổi, và 104 test của bộ này không bị
/// ảnh hưởng: chúng dựng schema từ .sql chứ không chạy migration C# bao giờ.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// <para>Thứ tự CÓ Ý NGHĨA nếu có nhiều file — mỗi script là DELTA, chạy đúng thứ tự sinh ra.</para>
    ///
    /// <para><b>Rút còn MỘT file ngày 2026-08-31</b> (baseline lại lịch sử migration — xem
    /// <c>doc/cau-truc-database.md</c> §5.2). Sáu file 0003–0008 cũ đã xoá cùng lượt: chúng dựng
    /// 5 bảng <c>business.*</c> của module DtiWeekly đã bị gỡ, và snapshot EF khi đó lệch khỏi
    /// model tới mức lệnh sinh migration kế tiếp sẽ đẻ ra 5 lệnh <c>DropTable</c>.</para>
    /// </summary>
    private static readonly string[] MigrationScripts =
    [
        "0001_initial_baseline.sql",
    ];

    private PostgreSqlContainer? _container;

    public string ConnectionString => _container is null
        ? throw new InvalidOperationException($"{nameof(PostgresFixture)} chưa khởi tạo xong.")
        : _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        try
        {
            // Build() NẰM TRONG try có chủ đích: Testcontainers phân giải Docker endpoint ngay
            // lúc build, nên nếu để ở field initializer thì lỗi "không có Docker" ném ra TRƯỚC
            // khi vào được try — người chạy chỉ thấy thông báo thô của thư viện, không thấy
            // hướng dẫn của repo.
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("platformmanager_it")
                .Build();

            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            // KHÔNG Skip: skip im lặng = "xanh giả", đúng rủi ro đã nêu khi chọn hạ tầng test.
            // Fail rõ ràng, kèm cách xử lý, để người chạy không phải đoán.
            throw new InvalidOperationException(
                "Không khởi động được Postgres container cho integration test. " +
                "Bộ test này CẦN Docker đang chạy (Docker Desktop trên Windows/macOS, docker daemon trên Linux) — " +
                "xem doc/huong_dan/wiki-core/be/04-testing-strategy.md §\"Yêu cầu môi trường\". " +
                "Bật Docker rồi chạy lại `dotnet test`. Nếu chỉ muốn chạy phần không cần Docker: " +
                "`dotnet test Tests/PlatformManager.ArchTests` và `dotnet test Tests/PlatformManager.Core.UnitTests`.",
                ex);
        }

        await ApplyMigrationScriptsAsync(ConnectionString);
        await SeedCoreAsync();
    }

    /// <summary>
    /// Chạy <see cref="CoreSeeder"/> lên database vừa dựng schema — role, 2 tài khoản bootstrap,
    /// <c>SysMenus</c>/<c>SysMenuRoles</c>, <c>RolePermissions</c> mặc định.
    ///
    /// <para><b>Vì sao việc này chuyển vào fixture (2026-09-01):</b> trước đó seed do CHÍNH HOST
    /// làm — <c>Program.cs</c> có khối <c>if (app.Environment.IsDevelopment()) { … SeedAsync() }</c>,
    /// và <see cref="IntegrationTestHostEnvironment"/> đặt <c>ASPNETCORE_ENVIRONMENT=Development</c>
    /// nên mọi <c>WebApplicationFactory</c> dựng lên đều seed hộ. Khối đó đã bị GỠ khỏi
    /// <c>Program.cs</c> (seed nay là lệnh riêng <c>--seed</c>, xem <c>SeedCommand.cs</c>), nên
    /// database test sẽ TRỐNG: không role, không tài khoản, <c>SysMenus</c> rỗng. Hệ quả không
    /// phải là "vài test đỏ" mà là đỏ vì lý do sai — ví dụ ma trận PERM-1 rỗng thì mọi payload đều
    /// "phủ đủ" và test coverage validator xanh một cách vô nghĩa.</para>
    ///
    /// <para><b>Vì sao đặt ở fixture chứ không ở từng test class:</b> database là của
    /// collection, nên trạng thái nền cũng phải là của collection. Rải lời gọi ra từng class vừa
    /// lặp, vừa để ngỏ ca "class quên gọi" — đúng lớp lỗi mà
    /// <see cref="IntegrationTestHostEnvironment"/> sinh ra để đóng cho phần cấu hình.</para>
    ///
    /// <para>Public vì test class nào ghi đè lên dữ liệu seed (hai ma trận phân quyền) phải gọi
    /// lại được để đưa bảng về nền. Seeder idempotent nên gọi thừa vô hại.</para>
    /// </summary>
    public async Task SeedCoreAsync()
    {
        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(ConnectionString);

        // Dựng host thật rồi bỏ đi ngay: CoreSeeder cần UserManager/RoleManager, tức cần đúng
        // đồ hình DI của Program.cs. Dựng tay một ServiceProvider rút gọn ở đây sẽ là bản sao thứ
        // hai của đồ hình đó, và bản sao sẽ lệch.
        await using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CoreSeeder>().SeedAsync();
    }

    /// <summary>
    /// Dựng thêm một database <b>TRỐNG</b> trong CÙNG container: đủ schema (chạy đúng bộ
    /// <see cref="MigrationScripts"/> như database chính) nhưng KHÔNG có một dòng dữ liệu seed nào.
    /// Trả về connection string trỏ tới nó.
    ///
    /// <para><b>Vì sao cần</b> (thêm 2026-09-01): bất biến "seeder KHÔNG chạy lúc host khởi động"
    /// chỉ chứng minh được trên một database <b>chưa từng</b> được seed — đúng tình huống thật của
    /// lần triển khai Production đầu tiên. Đo trên database dùng chung của collection thì không phân
    /// biệt nổi "host không ghi gì" với "host ghi lại đúng thứ fixture đã seed".</para>
    ///
    /// <para>Cố ý KHÔNG đụng tới database chính: mọi test khác trong collection vẫn thấy nguyên
    /// trạng thái nền do <see cref="SeedCoreAsync"/> dựng. Container là của collection nên database
    /// phụ này chết cùng container.</para>
    /// </summary>
    public async Task<string> CreateEmptyDatabaseAsync(string databaseName)
    {
        await using (var adminConnection = new NpgsqlConnection(ConnectionString))
        {
            await adminConnection.OpenAsync();

            // FORCE: cắt mọi kết nối còn sót của lần chạy trước trong cùng phiên container. Không có
            // nó, DROP treo vô hạn nếu một pool Npgsql còn giữ kết nối.
            await using (var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE);", adminConnection))
            {
                await drop.ExecuteNonQueryAsync();
            }

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", adminConnection);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = databaseName,
        }.ConnectionString;

        await ApplyMigrationScriptsAsync(connectionString);
        return connectionString;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>DbContext mới hoàn toàn mỗi lần gọi — cố ý, để test "thu hồi quyền có hiệu lực
    /// NGAY" không thể vô tình pass nhờ change tracker của một context đang sống.</summary>
    public PlatformManagerDbContext CreateDbContext()
    {
        // KHÔNG khai MigrationsAssembly ở đây, có chủ đích: fixture này dựng schema bằng
        // ApplyMigrationScriptsAsync (chạy file .sql), không gọi Migrate() lần nào — nên tuỳ chọn
        // đó sẽ không được đọc tới. Thêm vào chỉ để "cho giống chỗ khác" là buộc project test
        // phải biết assembly nào giữ migration, tức thêm một chỗ nữa phải sửa khi đổi host, đổi
        // lấy đúng zero hành vi. MigrationsHistoryTable thì GIỮ: bảng lịch sử có thật trong .sql
        // (0001_initial_baseline.sql tạo core."__EFMigrationsHistory").
        var options = new DbContextOptionsBuilder<PlatformManagerDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"))
            .Options;

        // Danh sách assembly chứa IEntityTypeConfiguration<T> đọc từ CHÍNH danh sách tầng của host
        // (HostModuleRegistrars), KHÔNG chép tay — cùng khuôn với PlatformManagerDbContextFactory.
        // Sửa 2026-09-08: bản trước tự dựng đúng MỘT EfConfigurationAssembly của Core. Khi tầng
        // nghiệp vụ đầu tiên xuất hiện, model dựng ở đây sẽ THIẾU entity của tầng đó mà không có gì
        // báo — cùng khuôn finding F6 mà bộ test này đã trả giá một lần (xem ArchTestSourceAccess.cs
        // §EfModelProbe). Và không ArchTest nào canh được chỗ này: luật "registrar phải được nối vào
        // host" cố ý bỏ qua cây Tests/.
        var configurationAssemblies = HostModuleRegistrars.Create()
            .Select(registrar => new EfConfigurationAssembly(registrar.PersistenceAssembly))
            .ToList();

        return new PlatformManagerDbContext(options, configurationAssemblies);
    }

    private static readonly string[] MigrationsSqlDirectorySegments =
        ["src", "BE", "Core", "PlatformManager.Core.Infrastructure", "Persistence", "Migrations", "sql"];

    private static async Task ApplyMigrationScriptsAsync(string connectionString)
    {
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), Path.Combine(MigrationsSqlDirectorySegments));

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var scriptName in MigrationScripts)
        {
            var path = Path.Combine(migrationsDirectory, scriptName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Thiếu script schema '{scriptName}' tại '{migrationsDirectory}'. Integration test dựng " +
                    "schema từ chính file .sql của repo — đổi tên/di chuyển file thì phải cập nhật " +
                    $"{nameof(PostgresFixture)}.{nameof(MigrationScripts)}.", path);

            await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(path), connection)
            {
                CommandTimeout = 300,
            };
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Đi ngược từ thư mục assembly cho tới khi thấy thư mục sql/ của migration — không
    /// hardcode số cấp "../../../.." (đổi TargetFramework/cấu hình build là gãy).</summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, Path.Combine(MigrationsSqlDirectorySegments))))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Không tìm thấy gốc repo (thư mục chứa {string.Join('/', MigrationsSqlDirectorySegments)}) " +
            $"khi đi ngược từ '{AppContext.BaseDirectory}'.");
    }
}

/// <summary>Một container dùng chung cho toàn bộ integration test — khởi động container tốn
/// hàng chục giây, không dựng lại cho từng class. Các class trong collection này chạy TUẦN TỰ,
/// nên test được phép ghi vào cùng 1 database.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
