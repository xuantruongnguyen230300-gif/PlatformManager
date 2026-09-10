using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật — <c>Migrations_BelongToTheHost</c>, [CHỐT 2026-09-04]</b>: không file <c>.cs</c> nào
/// dưới <c>src/BE/Core/</c> được là mã migration EF (lớp <c>Migration</c>, lớp
/// <c>ModelSnapshot</c>, thuộc tính <c>[Migration(...)]</c>, tham số <c>MigrationBuilder</c>).
/// Chúng thuộc project HOST — hôm nay là <c>PlatformManager.Api</c>. Riêng thư mục
/// <c>Core/PlatformManager.Core.Persistence/Migrations/sql/</c> thì Ở LẠI Core: artifact schema Corebase ship.
///
/// <para><b>Lỗi thật nó ngăn — mất bảng của dự án thứ hai.</b> EF không đọc database để biết cần
/// sinh gì; nó so model hiện tại với <c>ModelSnapshot</c>, một file TRẠNG THÁI ghi đè được. Nếu
/// snapshot nằm trong một project Core thì lượt <c>migrations add</c> đầu tiên của dự án thứ
/// hai sẽ GHI ĐÈ nó để nhét bảng nghiệp vụ của dự án đó vào. Từ đó Core và dự án 2 cùng sở hữu
/// một file trạng thái, và lần Core ship bản vá là lần merge hỏng: mất bảng dự án 2 khỏi snapshot
/// (lượt sinh migration kế tiếp EF tưởng bảng chưa tồn tại và SINH LẠI <c>CREATE TABLE</c> lên
/// dữ liệu thật), hoặc mất thay đổi của Core. Cả hai đều KHÔNG gây lỗi biên dịch và chỉ lộ ra khi
/// script đã chạy lên database có dữ liệu.</para>
///
/// <para><b>Vì sao phải là TEST chứ không phải một câu trong tài liệu.</b> Chỗ đặt migration do
/// một cấu hình quyết định (<c>MigrationsAssembly</c> ở
/// <c>PlatformManager.Api/PlatformManagerDbContextFactory.cs</c>) cộng một cờ dòng lệnh
/// (<c>--project</c> ở <c>src/BE/scripts/db.ps1</c>). Chạy <c>dotnet ef</c> tay với cờ khác — hoặc
/// gõ đúng khuôn cũ đã quen tay suốt nhiều tháng — là migration lại rơi vào Core, biên dịch sạch,
/// không test nào khác đỏ. Đúng thứ vừa gỡ, quay lại trong im lặng.</para>
///
/// <para><b>Đánh đổi: phân tích TĨNH văn bản mã nguồn</b> (cùng khuôn với
/// <see cref="ErrorCodeSourceTests"/> và <see cref="CoreMustNotKnowBusinessNameTests"/>) — dùng
/// lại <see cref="RepoSourceTree"/>, kể cả phép cắt chú thích. Reflection không dùng được ở đây vì
/// câu hỏi là "file NẰM Ở ĐÂU", một tính chất của cây thư mục chứ không của metadata.</para>
/// </summary>
public class MigrationsLocationTests
{
    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Ba dấu vết của mã migration EF sinh ra, đủ độc lập để mất một cái vẫn còn hai:
    /// <list type="number">
    ///   <item><c>class X : Migration</c> / <c>class X : ModelSnapshot</c> — lớp cơ sở.</item>
    ///   <item><c>[Migration("20260831165117_InitialCreate")]</c> — thuộc tính trên file Designer.</item>
    ///   <item><c>MigrationBuilder migrationBuilder</c> — tham số của <c>Up</c>/<c>Down</c>.</item>
    /// </list>
    ///
    /// <para><b>Vì sao dò theo NỘI DUNG chứ không theo tên thư mục.</b> Một luật "cấm thư mục
    /// <c>Persistence/Migrations</c> trong Core" bị lách chỉ bằng <c>--output-dir</c> khác, mà
    /// người lách thì không biết mình đang lách. Ba dấu vết trên do chính EF sinh ra nên đổi được
    /// tên thư mục chứ không đổi được chúng.</para>
    ///
    /// <para><b>Chỗ dễ sai và đã canh:</b> mẫu này KHÔNG được khớp
    /// <c>MigrationsHistoryTable(...)</c> hay <c>MigrationsAssembly(...)</c> — hai lời gọi CẤU
    /// HÌNH sống hợp lệ trong Core (<c>DependencyInjection.cs</c>) và trong Api. Vì thế mẫu 1 đòi
    /// từ khoá <c>class</c>, mẫu 2 đòi dấu <c>[</c> mở thuộc tính, mẫu 3 đòi <c>MigrationBuilder</c>
    /// đứng liền một định danh. Ca đối chứng
    /// <see cref="Detector_Catches_RealMigrationShapes_ButIgnores_CommentsAndConfigCalls"/> khoá
    /// đúng ranh giới này.</para>
    /// </summary>
    private static readonly Regex[] MigrationSourceMarkers =
    [
        new(@"\bclass\s+\w+\s*:\s*(?:Migration|ModelSnapshot)\b", RegexOptions.Compiled),
        new(@"\[\s*Migration\s*\(", RegexOptions.Compiled),
        new(@"\bMigrationBuilder\s+\w+", RegexOptions.Compiled),
    ];

    private static bool LooksLikeMigrationSource(string codeWithoutComments) =>
        MigrationSourceMarkers.Any(marker => marker.IsMatch(codeWithoutComments));

    // ── Luật ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Không file .cs migration nào nằm trong src/BE/Core")]
    public void CoreProjects_MustNotContain_EfMigrationSourceFiles()
    {
        var files = RepoSourceTree.CoreSourceFiles();

        // Chặn "xanh mà không đo gì": đường dẫn sai ⇒ tập rỗng ⇒ "không ai vi phạm".
        Assert.True(files.Count > 0,
            $"Không đọc được file .cs nào dưới '{RepoSourceTree.CoreDirectory}' ⇒ luật này không đo gì và " +
            "sẽ xanh vĩnh viễn. Xem RepoSourceTree.LocateBackendRoot: nó đi ngược từ thư mục output để tìm " +
            "PlatformManager.slnx, nên chỉ đúng khi test chạy tại chỗ trong cây repo.");

        var violations = files
            .Where(file => LooksLikeMigrationSource(RepoSourceTree.ReadCodeWithoutComments(file)))
            .Select(RepoSourceTree.Relative)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            "Mã migration EF đang nằm trong tầng Core: " + string.Join(", ", violations) + ". " +
            "Migration + ModelSnapshot thuộc PROJECT HOST (PlatformManager.Api/Persistence/Migrations/), " +
            "chốt 2026-09-04 — vì ModelSnapshot là file TRẠNG THÁI dùng chung: để ở Core thì dự án thứ hai " +
            "sẽ ghi đè nó khi thêm bảng nghiệp vụ đầu tiên, và lần Core ship bản vá là lần merge hỏng (mất " +
            "bảng của dự án 2 ⇒ lượt sinh migration sau EF SINH LẠI CREATE TABLE lên dữ liệu thật). " +
            "Nguyên nhân thường gặp: chạy `dotnet ef migrations add` với --project trỏ Core. Cách sửa: dùng " +
            "src/BE/scripts/db.ps1 -AddMigration (đã trỏ sẵn --project PlatformManager.Api), rồi chuyển file " +
            "vừa sinh nhầm sang PlatformManager.Api/Persistence/Migrations/ và đổi namespace thành " +
            "PlatformManager.Api.Persistence.Migrations. ĐỪNG nới luật cho xanh.");
    }

    /// <summary>
    /// Nửa còn lại của luật, và là thứ khiến nó không thể "xanh nhờ xoá sạch": migration phải TỒN
    /// TẠI ở host. Không có kiểm này, cách rẻ nhất để làm test trên xanh là xoá hết migration đi —
    /// mất luôn khả năng sinh file .sql delta cho lần đổi schema kế tiếp.
    /// </summary>
    [Fact(DisplayName = "Project host GIỮ migration + đúng MỘT ModelSnapshot")]
    public void Host_MustOwn_TheMigrationsAndSnapshot()
    {
        var apiFiles = RepoSourceTree.ApiSourceFiles();

        Assert.True(apiFiles.Count > 0,
            $"Không đọc được file .cs nào dưới '{RepoSourceTree.ApiDirectory}' ⇒ luật này không đo gì.");

        var code = apiFiles.ToDictionary(path => path, RepoSourceTree.ReadCodeWithoutComments);

        var migrationClasses = code
            .Where(entry => MigrationClassPattern.IsMatch(entry.Value))
            .Select(entry => RepoSourceTree.Relative(entry.Key))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(migrationClasses.Count > 0,
            "Project host không còn file migration EF nào (`class X : Migration`). Hoặc chúng đã bị xoá, " +
            "hoặc lượt `migrations add` gần nhất ghi ra chỗ khác. Cả hai đều nghĩa là lần đổi schema kế " +
            "tiếp không sinh nổi file .sql delta — xem src/BE/scripts/db.ps1 -ScriptOutput.");

        var snapshots = code
            .Where(entry => SnapshotClassPattern.IsMatch(entry.Value))
            .Select(entry => RepoSourceTree.Relative(entry.Key))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(snapshots.Count == 1,
            $"Project host có {snapshots.Count} file ModelSnapshot ({string.Join(", ", snapshots)}), phải có " +
            "ĐÚNG 1. Không có ⇒ lượt `migrations add` kế tiếp coi database là trống và sinh lại CREATE TABLE " +
            "cho mọi bảng. Nhiều hơn 1 ⇒ EF chọn cái nào là chuyện của thứ tự nạp kiểu, tức không xác định.");

        // Neo phía cấu hình: chỗ đặt migration chỉ đúng chừng nào MigrationsAssembly còn trỏ về host.
        // Không có neo này, ai đó gỡ MigrationsAssembly đi thì mọi test ở đây vẫn xanh cho tới lượt
        // `migrations add` kế tiếp — lượt sẽ lại ghi vào Core.
        Assert.Contains(code.Values, text => text.Contains("MigrationsAssembly", StringComparison.Ordinal));
    }

    /// <summary>
    /// Nửa "Core ship .sql" của chốt 2026-09-04. <c>PostgresFixture</c> cũng ném khi thiếu file
    /// này, nhưng chỉ trong bộ integration test — mà bộ đó cần Docker, nên trên máy không có Docker
    /// việc xoá nhầm artifact schema sẽ không ai thấy. Kiểm ở đây chạy trong mili-giây.
    /// </summary>
    [Fact(DisplayName = "Thư mục .sql artifact vẫn Ở LẠI Core")]
    public void Core_MustStillShip_TheBaselineSqlArtifact()
    {
        var sqlDirectory = Path.Combine(
            RepoSourceTree.CoreDirectory,
            "PlatformManager.Core.Persistence", "Migrations", "sql");

        Assert.True(Directory.Exists(sqlDirectory),
            $"Không còn thư mục '{sqlDirectory}'. File .sql là ARTIFACT SCHEMA mà Corebase ship cho dự án " +
            "sau (chốt 2026-09-04: migration .cs thuộc host, .sql thuộc Core) — chỉ file .cs mới chuyển sang " +
            "PlatformManager.Api. Đây cũng là nguồn schema mà PostgresFixture dựng database test từ đó.");

        var scripts = Directory.EnumerateFiles(sqlDirectory, "*.sql", SearchOption.TopDirectoryOnly).ToList();

        Assert.True(scripts.Count > 0,
            $"Thư mục '{sqlDirectory}' còn đó nhưng rỗng ⇒ không còn schema nào để áp lên database, và " +
            "PostgresFixture.MigrationScripts đang trỏ vào hư không.");
    }

    private static readonly Regex MigrationClassPattern =
        new(@"\bclass\s+\w+\s*:\s*Migration\b", RegexOptions.Compiled);

    private static readonly Regex SnapshotClassPattern =
        new(@"\bclass\s+\w+\s*:\s*ModelSnapshot\b", RegexOptions.Compiled);

    // ── Đối chứng: chứng minh bộ dò biết nói CÓ và biết nói KHÔNG ─────────

    /// <summary>
    /// Trên một repo đang sạch, nhánh "có vi phạm" của luật chính KHÔNG BAO GIỜ chạy — nó hỏng
    /// được mà không ai biết, và một test luôn xanh thì không phân biệt được với một test chết.
    ///
    /// <para>Ba nửa của ca này đều bắt nguồn từ tình huống có thật trong repo, không phải giả
    /// định: (a) văn bản EF sinh ra thật, chép từ file đã chuyển; (b) chú thích — chính
    /// <c>Core.Infrastructure/DependencyInjection.cs</c> hôm nay có một đoạn giải thích dài nhắc
    /// thẳng "Migrations/ + ModelSnapshot", nên bộ dò không cắt chú thích sẽ báo Core vi phạm
    /// NGAY; (c) lời gọi cấu hình <c>MigrationsHistoryTable</c>/<c>MigrationsAssembly</c> — sống
    /// hợp lệ ở cả Core lẫn Api, cấm nhầm là đỏ oan và người sau sẽ nới luật cho xanh.</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò bắt mã migration THẬT, bỏ qua chú thích và lời gọi cấu hình")]
    public void Detector_Catches_RealMigrationShapes_ButIgnores_CommentsAndConfigCalls()
    {
        // (a) Ba dấu vết thật, mỗi cái phải tự đứng một mình được.
        const string migrationClass = """
            public partial class InitialCreate : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.EnsureSchema(name: "core");
                }
            }
            """;

        const string designerAttribute = """
            [DbContext(typeof(PlatformManagerDbContext))]
            [Migration("20260831165117_InitialCreate")]
            partial class InitialCreate
            {
            }
            """;

        const string snapshotClass = """
            partial class PlatformManagerDbContextModelSnapshot : ModelSnapshot
            {
            }
            """;

        Assert.True(Detects(migrationClass), "Bộ dò trượt `class X : Migration` ⇒ luật chính xanh mà không đo gì.");
        Assert.True(Detects(designerAttribute), "Bộ dò trượt `[Migration(\"…\")]` ⇒ file .Designer.cs lọt vào Core mà không ai biết.");
        Assert.True(Detects(snapshotClass), "Bộ dò trượt `class X : ModelSnapshot` ⇒ trượt ĐÚNG file nguy hiểm nhất (file trạng thái dùng chung).");

        // Từng dấu vết một, để mất một mẫu vẫn lộ ra chứ không núp sau hai mẫu kia.
        Assert.True(MigrationSourceMarkers[0].IsMatch("public partial class InitialCreate : Migration"), "Mẫu 1 (lớp cơ sở) hỏng.");
        Assert.True(MigrationSourceMarkers[1].IsMatch("[Migration(\"20260831165117_InitialCreate\")]"), "Mẫu 2 (thuộc tính) hỏng.");
        Assert.True(MigrationSourceMarkers[2].IsMatch("protected override void Down(MigrationBuilder migrationBuilder)"), "Mẫu 3 (tham số MigrationBuilder) hỏng.");

        // (b) Chú thích — dùng ĐÚNG bộ cắt mà luật chính dùng, không phải một bản mô phỏng.
        const string commentsOnly = """
            // Chốt 2026-09-04: Migrations/ + ModelSnapshot rơi vào Core là hỏng — chúng thuộc host.
            /* class Foo : Migration và [Migration("x")] chỉ được NHẮC trong chú thích khối này,
               kể cả MigrationBuilder migrationBuilder. */
            services.AddScoped<AuditInterceptor>();
            """;

        Assert.False(Detects(commentsOnly),
            "Bộ dò tính CHÚ THÍCH là mã migration ⇒ Core.Infrastructure/DependencyInjection.cs sẽ đỏ oan vì " +
            "đoạn giải thích nằm ngay tại chỗ, và người sửa gần như chắc chắn xoá đoạn ghi lại lý do — tức " +
            "xoá đúng phần đáng giữ nhất. Phép cắt dùng chung là RepoSourceTree.RemoveComments.");

        // (c) Cấu hình hợp lệ — phải sống được ở CẢ Core lẫn Api.
        const string configurationCalls = """
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core");
                npgsql.MigrationsAssembly(typeof(PlatformManagerDbContextFactory).Assembly.GetName().Name);
            });
            var pending = context.Database.GetPendingMigrations();
            """;

        Assert.False(Detects(configurationCalls),
            "Bộ dò báo vi phạm cho lời gọi CẤU HÌNH (MigrationsHistoryTable/MigrationsAssembly/" +
            "GetPendingMigrations) ⇒ nó cấm luôn thứ Core được phép làm, và luật sẽ bị gỡ vì phiền chứ " +
            "không vì sai.");
    }

    private static bool Detects(string probeSource) =>
        LooksLikeMigrationSource(RepoSourceTree.RemoveComments(probeSource));
}
