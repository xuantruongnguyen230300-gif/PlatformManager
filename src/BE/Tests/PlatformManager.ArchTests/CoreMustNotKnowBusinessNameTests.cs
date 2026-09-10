using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật — <c>Core_MustNotKnowBusinessName</c>, [CHỐT 2026-08-23]</b> (doc/kien-truc-core-module.md
/// §"ArchTest cần có"): không assembly <c>PlatformManager.Core.*</c> nào được chứa <b>string
/// literal</b> mang tên tầng nghiệp vụ (<c>"Business"</c>, <c>"DtiWeekly"</c>…). Mọi thứ Core cần
/// từ tầng nghiệp vụ phải đi qua <c>IModuleRegistrar</c>.
///
/// <para><b>Lỗi thật nó bắt.</b> <c>CoreModuleBoundaryTests.Core_MustNotReference_AnyModulesAssembly</c>
/// canh chiều <i>tham chiếu assembly</i> — nhưng cách phá ranh giới rẻ nhất và phổ biến nhất KHÔNG
/// tạo tham chiếu nào: một chuỗi. <c>ApplyConfigurationsFromAssembly(Assembly.Load("PlatformManager.Business.Infrastructure"))</c>,
/// một <c>switch</c> trên <c>"DtiWeekly"</c> để chọn nhánh, một tên schema/hàng đợi/thư mục ghép
/// bằng tay. Tất cả đều biên dịch sạch, không sinh tham chiếu, không test nào khác đỏ — và đều
/// biến Corebase thành thứ không cắm được vào dự án thứ hai nếu không mổ lại.</para>
///
/// <para><b>Hiện trạng khi viết (2026-09-01): Core đang SẠCH.</b> Test này vì thế xanh ngay từ
/// lượt đầu — mục đích của nó là <b>khoá lại</b> trạng thái sạch đó, không phải phát hiện vi phạm
/// đang có. Đúng vì vậy mà 3 ca đối chứng bên dưới là phần bắt buộc, không phải phần trang trí:
/// một test "canh hồi quy" mà bộ dò của nó hỏng thì sẽ xanh vĩnh viễn và không ai biết.</para>
///
/// <para><b>Đánh đổi: phân tích TĨNH văn bản mã nguồn.</b> Reflection không đọc được string
/// literal (chúng là <c>ldstr</c> trong IL, không có trong metadata mà <c>System.Reflection</c>
/// phơi ra), nên đường duy nhất không cần thư viện đọc IL là đọc mã nguồn — dùng lại đúng hạ tầng
/// của <see cref="RepoSourceTree"/>. Giá phải trả và cách nó có thể sai: xem
/// <see cref="ExtractStringLiterals"/>.</para>
/// </summary>
public class CoreMustNotKnowBusinessNameTests
{
    /// <summary>
    /// Tên tầng/module nghiệp vụ bị cấm, so khớp SAU khi chuẩn hoá (xem <see cref="Normalize"/>):
    /// hạ chữ thường và bỏ mọi ký tự không phải chữ-số. Nhờ đó <c>"DTI Weekly"</c>,
    /// <c>"dti-weekly"</c>, <c>"Dti_Weekly"</c> và <c>"PlatformManager.Business.Application"</c>
    /// đều bị bắt bằng cùng 2 mục dưới đây.
    ///
    /// <para>Thêm khối nghiệp vụ mới (dự án thứ hai đặt tên khác) thì thêm tên đã chuẩn hoá vào
    /// đây — test tự phủ.</para>
    /// </summary>
    private static readonly string[] ForbiddenNames = ["business", "dtiweekly"];

    /// <summary>
    /// <b>Miễn trừ DUY NHẤT, tường minh — schema Postgres <c>business</c>.</b>
    ///
    /// <para>Vì sao đây là ngoại lệ có chủ đích chứ không phải lỗ hổng: 1 database duy nhất chia
    /// 2 schema <c>core</c> / <c>business</c> là quyết định đã chốt (doc/cau-truc-database.md §1).
    /// Tên một schema SQL không phải là Core "biết tên" tầng nghiệp vụ để rẽ nhánh theo nó.</para>
    ///
    /// <para><b>Thu hẹp 2026-09-04 — lý do CHÍNH của miễn trừ này đã biến mất.</b> Lập luận trước
    /// là: <c>PlatformManagerDbContext</c> sống ở <c>Core.Infrastructure</c> nên khi khối
    /// <c>Business.*</c> dựng lại, EF sẽ TỰ SINH vào <c>Core.Infrastructure/Persistence/Migrations/</c>
    /// các dòng <c>b.ToTable("…", "business")</c> và
    /// <c>migrationBuilder.EnsureSchema(name: "business")</c> — tức Core bị ép chứa literal đó.
    /// Theo chốt "migration thuộc host, Core ship .sql" (2026-09-04), EF nay sinh vào
    /// <c>PlatformManager.Api/Persistence/Migrations/</c>, KHÔNG phải Core — luật này không quét
    /// tới đó, nên tình huống ấy không còn phát sinh.</para>
    ///
    /// <para>Vẫn GIỮ miễn trừ, có cân nhắc: hằng số tên schema vẫn có thể xuất hiện hợp lệ trong
    /// Core (một <c>ToTable(..., "business")</c> ở cấu hình entity dùng chung, hay hằng số tên
    /// schema đặt ở Core cho tầng nghiệp vụ dùng lại). Gỡ đi thì luật đỏ oan đúng vào ngày khối
    /// nghiệp vụ quay lại — thời điểm tệ nhất để phải tranh luận về một ArchTest. Nó hẹp tới mức
    /// vô hại, và ca đối chứng
    /// <see cref="Detector_Exempts_LowercasePostgresSchema_ButNotOtherCasings"/> khoá đúng độ hẹp
    /// đó.</para>
    ///
    /// <para><b>Miễn trừ hẹp có chủ ý:</b> chỉ literal <b>đúng bằng</b> <c>business</c>, phân biệt
    /// hoa thường. <c>"Business"</c>, <c>"business.CriteriaAssessments"</c>,
    /// <c>"PlatformManager.Business.Infrastructure"</c> đều KHÔNG được miễn — chúng không phải tên
    /// schema. Nới miễn trừ này (ví dụ đổi sang "chứa chữ business") là mở đúng cái lỗ mà luật
    /// sinh ra để bịt.</para>
    ///
    /// <para>Hôm nay (2026-09-01) chưa literal nào trong Core khớp miễn trừ này — nó chưa được
    /// dùng lần nào. Ca đối chứng <see cref="Detector_Exempts_LowercasePostgresSchema_ButNotOtherCasings"/>
    /// là thứ duy nhất chứng minh nó hoạt động đúng như mô tả.</para>
    /// </summary>
    private const string PostgresBusinessSchema = "business";

    [Fact(DisplayName = "Không file .cs nào của Core chứa string literal mang tên tầng nghiệp vụ")]
    public void CoreSource_MustNotContain_BusinessNameStringLiteral()
    {
        var files = RepoSourceTree.CoreSourceFiles();

        // ── Chặn "xanh mà không đo gì" (1/3): đường dẫn sai ⇒ tập file rỗng ⇒ "không ai vi phạm".
        Assert.True(files.Count > 0,
            $"Không đọc được file .cs nào dưới '{RepoSourceTree.CoreDirectory}' ⇒ test này không đo gì và " +
            "sẽ xanh vĩnh viễn. Xem RepoSourceTree.LocateBackendRoot: nó đi ngược từ thư mục output để tìm " +
            "PlatformManager.slnx, nên chỉ chạy đúng khi test chạy tại chỗ trong cây repo.");

        // ── Chặn "xanh mà không đo gì" (2/3): quét đủ CẢ 3 project Core, không phải mỗi cái đầu.
        var missingProjects = ExpectedCoreProjects
            .Where(project => !files.Any(file => file.Contains(
                $"{Path.DirectorySeparatorChar}{project}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            .ToList();

        Assert.True(missingProjects.Count == 0,
            "Không có file .cs nào thuộc project Core sau đây lọt vào tập quét: " +
            string.Join(", ", missingProjects) + ". Hoặc project đã đổi tên/đổi chỗ (thì cập nhật " +
            "ExpectedCoreProjects), hoặc bộ liệt kê file đang sót — trong cả hai trường hợp luật này đang " +
            "KHÔNG canh phần Core đó.");

        var findings = new List<string>();
        var literalCount = 0;
        var anchorFound = false;

        foreach (var file in files)
        {
            var code = RepoSourceTree.ReadCodeWithoutComments(file);
            AssertLexerDidNotLoseTrack(file, code);

            foreach (var literal in ExtractStringLiterals(code))
            {
                literalCount++;
                anchorFound |= string.Equals(literal.Value, NonEmptinessAnchor, StringComparison.Ordinal);

                var hit = MatchedForbiddenName(literal.Value);
                if (hit is not null)
                    findings.Add($"{Path.GetRelativePath(RepoSourceTree.BackendRoot, file)}:{literal.Line} " +
                                 $"chứa \"{literal.Value}\" (khớp tên cấm '{hit}')");
            }
        }

        // ── Chặn "xanh mà không đo gì" (3/3): đọc được file KHÔNG có nghĩa là bóc được literal.
        // Mỏ neo là literal của HasDefaultSchema("core") ở PlatformManagerDbContext — một quyết
        // định đã chốt, nên nó ổn định; nếu nó biến mất thì hoặc bộ bóc literal hỏng, hoặc schema
        // đã đổi tên và mỏ neo cần cập nhật. Cả hai đều đáng dừng lại xem.
        Assert.True(literalCount > 0 && anchorFound,
            $"Quét {files.Count} file Core nhưng bóc ra {literalCount} string literal và " +
            $"{(anchorFound ? "" : "KHÔNG ")}thấy mỏ neo \"{NonEmptinessAnchor}\" ⇒ không tin được là bộ dò " +
            "đang thật sự soi vào literal. Kiểm ExtractStringLiterals trước khi tin kết quả xanh của test này.");

        Assert.True(findings.Count == 0,
            "Assembly Core chứa string literal mang tên tầng nghiệp vụ — Corebase mất tính cắm được vào dự " +
            "án thứ hai (luật Core_MustNotKnowBusinessName, CHỐT 2026-08-23, doc/kien-truc-core-module.md): " +
            string.Join(" | ", findings) + ". Cách sửa: bỏ chuỗi đó đi và để tầng nghiệp vụ TỰ khai báo phần " +
            "của mình qua IModuleRegistrar (khuôn đã có: mỗi tầng tự đăng ký assembly *.Persistence của " +
            "mình qua DI, PlatformManagerDbContext.OnModelCreating chỉ lặp trên danh sách đã đăng ký — " +
            "KHÔNG hardcode tên assembly nào). Nếu chuỗi là tên schema Postgres thì nó phải đúng chữ thường " +
            $"\"{PostgresBusinessSchema}\" (miễn trừ tường minh, xem PostgresBusinessSchema trong file này). " +
            "ĐỪNG nới luật cho xanh — làm thế là xoá đúng thứ luật này sinh ra để giữ.");
    }

    /// <summary>
    /// Mọi project Core phải có mặt trong tập quét — dựng project Core mới thì thêm vào đây.
    ///
    /// <para>🛑 <b>DANH SÁCH LIỆT KÊ TAY.</b> Nó là bộ chặn "xanh mà không đo gì" (2/3) của luật
    /// bên trên, nên bản thân nó mục ruỗng thì chính bộ chặn ngừng chặn: một project Core mới
    /// không khai ở đây vẫn được QUÉT (tập file lấy theo thư mục <c>Core/</c>), nhưng nếu bộ liệt
    /// kê file bỗng sót nó thì không còn gì báo động. <c>PlatformManager.Core.Api</c> thêm
    /// 2026-09-09 cùng project (Q8).</para>
    /// </summary>
    private static readonly string[] ExpectedCoreProjects =
    [
        "PlatformManager.Core.Domain",
        "PlatformManager.Core.Application",
        "PlatformManager.Core.Infrastructure",
        "PlatformManager.Core.Api",
    ];

    /// <summary>Literal chắc chắn có thật trong Core: <c>modelBuilder.HasDefaultSchema("core")</c>.</summary>
    private const string NonEmptinessAnchor = "core";

    // ── Đối chứng: chứng minh bộ dò biết nói KHÔNG ───────────────────────

    /// <summary>
    /// Đối chứng 1 — bộ dò phải BÁO khi tên tầng nghiệp vụ nằm trong chuỗi thật.
    /// Không có ca này, một lỗi trong <see cref="MatchedForbiddenName"/> (ví dụ mảng
    /// <see cref="ForbiddenNames"/> lỡ tay bị làm rỗng) sẽ làm test chính xanh mãi mãi.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò BÁO chuỗi thật chứa tên tầng nghiệp vụ")]
    public void Detector_Reports_RealStringLiteralsNamingBusinessLayer()
    {
        const string probe = """
            var configuration = Assembly.Load("PlatformManager.Business.Infrastructure");
            var module = "DtiWeekly";
            var spaced = "DTI Weekly";
            var kebab = "dti-weekly";
            """;

        var hits = ScanProbe(probe);

        string[] expected =
            ["DTI Weekly", "PlatformManager.Business.Infrastructure", "dti-weekly", "DtiWeekly"];

        Assert.Equal(
            expected.OrderBy(x => x, StringComparer.Ordinal),
            hits.OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// Đối chứng 2 — bộ dò KHÔNG được báo cho chú thích và cho tên định danh.
    ///
    /// <para>Đây là ca quyết định test này có dùng được hay không. Repo nhắc chữ
    /// <c>Business</c>/<c>DTI Weekly</c> dày đặc trong văn xuôi (riêng
    /// <c>PlatformManagerDbContext.OnModelCreating</c> đã có một đoạn giải thích schema
    /// <c>"business"</c>), và <c>ErrorCode.BusinessRuleError</c> — một thành viên enum của CHÍNH
    /// Core — xuất hiện ở nhiều <c>ErrorDescriptor</c>. Một bộ dò cả tin kiểu <c>grep</c> sẽ đỏ
    /// ngay trên cả hai, và test sẽ bị gỡ vì phiền chứ không vì sai.</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò KHÔNG báo cho chú thích, và không báo cho tên định danh")]
    public void Detector_Ignores_Comments_And_Identifiers()
    {
        const string probe = """
            // Entity nghiệp vụ (DTI Weekly...) khai tường minh schema "business" ngay tại ToTable().
            /* Business / DtiWeekly chỉ được nhắc trong chú thích khối này, không phải chuỗi. */
            public static readonly ErrorDescriptor CreateFailed = new(
                "USER.CREATE_FAILED", ErrorCode.BusinessRuleError, "Tạo người dùng thất bại: {0}");
            var registrar = typeof(IModuleRegistrar).Name; // Business đi qua đây
            """;

        Assert.Empty(ScanProbe(probe));
    }

    /// <summary>
    /// Đối chứng 3 — miễn trừ schema Postgres đúng phạm vi đã khai: chữ thường thì tha, mọi dạng
    /// khác thì vẫn báo. Xem <see cref="PostgresBusinessSchema"/> để biết vì sao miễn trừ này tồn
    /// tại và vì sao nó hẹp.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: miễn trừ CHỈ áp cho schema \"business\" chữ thường")]
    public void Detector_Exempts_LowercasePostgresSchema_ButNotOtherCasings()
    {
        const string generatedByEf = """
            migrationBuilder.EnsureSchema(name: "business");
            b.ToTable("CriteriaAssessments", "business");
            """;

        Assert.Empty(ScanProbe(generatedByEf));

        const string notTheSchema = """
            b.ToTable("CriteriaAssessments", "Business");
            var qualified = "business.CriteriaAssessments";
            """;

        string[] expected = ["Business", "business.CriteriaAssessments"];

        Assert.Equal(
            expected.OrderBy(x => x, StringComparer.Ordinal),
            ScanProbe(notTheSchema).OrderBy(x => x, StringComparer.Ordinal));
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Chạy ĐÚNG đường mà test chính chạy — cắt chú thích bằng <see cref="RepoSourceTree.RemoveComments"/>
    /// (cùng một bộ cắt, không phải bản sao), bóc literal, rồi lọc. Nhờ vậy 3 ca đối chứng ở trên
    /// chứng minh được về bộ dò thật, chứ không phải về một bản mô phỏng của nó.
    /// </summary>
    private static List<string> ScanProbe(string probeSource)
    {
        var code = RepoSourceTree.RemoveComments(probeSource);

        return [.. ExtractStringLiterals(code)
            .Where(literal => MatchedForbiddenName(literal.Value) is not null)
            .Select(literal => literal.Value)];
    }

    private static string? MatchedForbiddenName(string literalValue)
    {
        if (string.Equals(literalValue, PostgresBusinessSchema, StringComparison.Ordinal))
            return null;

        var normalized = Normalize(literalValue);
        return ForbiddenNames.FirstOrDefault(name => normalized.Contains(name, StringComparison.Ordinal));
    }

    /// <summary>Hạ chữ thường và bỏ mọi ký tự không phải chữ-số, để mọi cách viết cùng quy về một dạng.</summary>
    private static string Normalize(string value) =>
        new([.. value.ToLowerInvariant().Where(char.IsLetterOrDigit)]);

    private readonly record struct SourceLiteral(string Value, int Line);

    /// <summary>
    /// Bóc string literal ra khỏi mã nguồn ĐÃ CẮT CHÚ THÍCH.
    ///
    /// <para><b>Vì sao phải bóc literal chứ không grep cả dòng.</b> Grep theo dòng cho kết quả sai
    /// theo cả hai chiều. Sai dương: dòng
    /// <c>new("USER.CREATE_FAILED", ErrorCode.BusinessRuleError, "Tạo người dùng thất bại: {0}")</c>
    /// khiến mọi mẫu kiểu <c>"…business…"</c> khớp — vì đoạn nằm GIỮA hai literal cũng nằm giữa hai
    /// dấu nháy. Sai âm: tên cấm nằm trong literal trải trên dòng đã bị cắt chú thích thì trượt.</para>
    ///
    /// <para><b>Giới hạn đã biết, khai để không ai tin nhầm.</b> Đây là bộ dò theo regex chứ không
    /// phải lexer C#: (a) nó không hiểu chuỗi nội suy có nháy lồng bên trong (<c>$"{d["k"]}"</c>) —
    /// hôm nay Core không có ca nào; (b) nó không hiểu chuỗi thô <c>"""…"""</c> — Core cũng chưa
    /// dùng; (c) nó thừa hưởng giới hạn đã ghi ở <see cref="RepoSourceTree.RemoveComments"/> (dãy
    /// <c>//</c> BÊN TRONG chuỗi bị hiểu là chú thích), điều duy nhất khiến một chuỗi bị cắt cụt và
    /// tên cấm ở phần đuôi bị bỏ sót. Cả 3 giới hạn đều làm test SÓT chứ không làm nó đỏ oan, nên
    /// chúng không được phép âm thầm — <see cref="AssertLexerDidNotLoseTrack"/> canh đúng chỗ đó:
    /// mọi dấu nháy còn lại sau khi đã bóc hết literal là dấu hiệu bộ dò lạc, và test đỏ để có
    /// người xem, thay vì xanh vì không thấy gì.</para>
    /// </summary>
    private static IEnumerable<SourceLiteral> ExtractStringLiterals(string codeWithoutComments)
    {
        foreach (Match match in LiteralPattern.Matches(codeWithoutComments))
        {
            if (match.Groups["char"].Success)
                continue;

            var value = match.Groups["verbatim"].Success
                ? match.Value[2..^1].Replace("\"\"", "\"", StringComparison.Ordinal)
                : match.Value[1..^1];

            yield return new SourceLiteral(value, LineOf(codeWithoutComments, match.Index));
        }
    }

    /// <summary>
    /// Chuỗi verbatim <c>@"…"</c>, chuỗi thường/nội suy <c>"…"</c> (không vắt dòng — đúng luật C#),
    /// và ký tự <c>'…'</c>. Ký tự phải khớp để <c>'"'</c> không bị hiểu là mở chuỗi; nó bị bỏ qua
    /// khi bóc giá trị.
    /// </summary>
    private static readonly Regex LiteralPattern = new(
        """
        (?<verbatim>@"(?:[^"]|"")*?"(?!"))|(?<regular>"(?:\\.|[^"\\\r\n])*")|(?<char>'(?:\\.|[^'\\\r\n])')
        """,
        RegexOptions.Compiled);

    /// <summary>
    /// Sau khi xoá mọi literal đã bóc, trong mã nguồn không được còn dấu nháy kép nào. Còn nghĩa là
    /// bộ dò đã lạc (chuỗi thô, nháy lồng trong nội suy, hoặc chuỗi bị chú thích cắt cụt) — và một
    /// bộ dò lạc thì im lặng bỏ sót, đúng kiểu hỏng mà test này không được phép mắc.
    /// </summary>
    private static void AssertLexerDidNotLoseTrack(string file, string codeWithoutComments)
    {
        var residue = LiteralPattern.Replace(codeWithoutComments, match => new string(' ', match.Length));
        var strayIndex = residue.IndexOf('"');

        if (strayIndex < 0)
            return;

        Assert.Fail(
            $"Còn dấu nháy kép lạc ở {Path.GetRelativePath(RepoSourceTree.BackendRoot, file)}:" +
            $"{LineOf(codeWithoutComments, strayIndex)} sau khi đã bóc hết string " +
            "literal ⇒ bộ dò không còn bám đúng ranh giới chuỗi, nên từ điểm đó trở đi nó sẽ bỏ sót vi " +
            "phạm trong im lặng. Nguyên nhân thường gặp (xem ExtractStringLiterals §Giới hạn): file mới " +
            "dùng chuỗi thô \"\"\"…\"\"\", hoặc chuỗi nội suy có nháy lồng, hoặc một chuỗi chứa dãy // bị " +
            "RepoSourceTree.RemoveComments cắt cụt. Cách sửa: mở rộng LiteralPattern cho đúng dạng mới — " +
            "ĐỪNG tắt kiểm tra này.");
    }

    private static int LineOf(string code, int index) =>
        code.AsSpan(0, index).Count('\n') + 1;
}
