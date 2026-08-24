using Microsoft.EntityFrameworkCore;
using Npgsql;
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
/// quan tâm. Thêm file .sql mới (0007...) thì PHẢI thêm tên vào <see cref="MigrationScripts"/>,
/// nếu không schema test sẽ lệch schema thật đúng cái điều đang muốn tránh.
///
/// Sửa 2026-08-24: đường dẫn TRƯỚC trỏ <c>doc/ERD/migrations</c> — thư mục đó đã xoá 2026-08-23
/// khi hợp nhất nguồn schema về <c>doc/cau-truc-database.md</c>/<c>.sql</c>, khiến
/// <see cref="FindRepositoryRoot"/> luôn ném <see cref="DirectoryNotFoundException"/> và
/// KHÔNG bộ integration test nào chạy được (phát hiện khi build+test thật lần đầu, không phải lý
/// thuyết). Nguồn .sql THẬT của từng migration nằm cạnh chính migration C# tương ứng, không phải
/// một thư mục tài liệu riêng — trỏ về đó.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>Thứ tự CÓ Ý NGHĨA — mỗi script là DELTA, phải chạy đúng thứ tự sinh ra.</summary>
    private static readonly string[] MigrationScripts =
    [
        "0003_corebase_v2.sql",
        "0004_role_permission_import_job.sql",
        "0005_role_permission_resource_key_index.sql",
        "0006_criteria_assessment_row_version.sql",
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

        await ApplyMigrationScriptsAsync();
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
        var options = new DbContextOptionsBuilder<PlatformManagerDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"))
            .Options;

        return new PlatformManagerDbContext(
            options,
            [new EfConfigurationAssembly(typeof(PlatformManagerDbContext).Assembly)]);
    }

    private static readonly string[] MigrationsSqlDirectorySegments =
        ["src", "BE", "Core", "PlatformManager.Core.Infrastructure", "Persistence", "Migrations", "sql"];

    private async Task ApplyMigrationScriptsAsync()
    {
        var migrationsDirectory = Path.Combine(FindRepositoryRoot(), Path.Combine(MigrationsSqlDirectorySegments));

        await using var connection = new NpgsqlConnection(ConnectionString);
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
