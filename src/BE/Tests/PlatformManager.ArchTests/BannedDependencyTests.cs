using System.Xml.Linq;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> ba thứ đã bị loại bỏ bằng quyết định, không được lén quay lại —
/// (1) gói NuGet <c>*.InMemory</c> và <c>Moq</c> trong project test
/// (doc/huong_dan/wiki-core/be/04-testing-strategy.md:22,27);
/// (2) lời gọi <c>UseInMemoryDatabase</c> trong mã nguồn sản phẩm;
/// (3) lời gọi <c>EnableSensitiveDataLogging</c> trong mã nguồn sản phẩm.
///
/// <para><b>Lỗi thật thuộc loại "sửa cho chạy được rồi quên":</b> ai đó không bật được Docker
/// nên integration test không chạy; đường ra nhanh nhất là thêm
/// <c>Microsoft.EntityFrameworkCore.InMemory</c> và đổi provider. Test xanh trở lại ngay. Nhưng
/// provider InMemory <b>bỏ qua hoàn toàn</b> khoá ngoại, unique index và transaction thật — nghĩa
/// là toàn bộ lớp integration test, thứ tồn tại CHỈ để kiểm những ràng buộc đó, không còn kiểm gì
/// nữa. Cái giá không hiện ra lúc đó; nó hiện ra khi dữ liệu hỏng trên Production.</para>
///
/// <para><b>Và loại "bật tạm để debug rồi commit":</b> <c>EnableSensitiveDataLogging</c> đưa giá
/// trị tham số truy vấn vào log — mật khẩu, token, dữ liệu cá nhân. Log giữ 7 ngày, được thu về
/// máy chủ tập trung, và được nhiều người đọc hơn database. Một dòng bật tạm lúc 11 giờ đêm
/// không có gì phân biệt được với một dòng cố ý.</para>
///
/// <para><b>Vì sao cả hai đều cần test chứ không cần code review:</b> chúng đều là thay đổi
/// <b>một dòng</b>, đều làm gate <b>xanh hơn</b> chứ không đỏ hơn, và đều đi kèm một lý do chính
/// đáng tại thời điểm gõ. Không có thứ gì trong quy trình tự nhiên chặn được chúng.</para>
///
/// <para><b>Đánh đổi.</b> Nhóm (1) đọc csproj bằng <see cref="XDocument"/> chứ không bằng tìm
/// chuỗi: XML parser bỏ qua <c>&lt;!-- --&gt;</c> theo bản chất, nên nó miễn nhiễm với việc cả
/// hai chuỗi cấm ĐANG có mặt trong chú thích của
/// <c>PlatformManager.Core.IntegrationTests.csproj</c> và <c>PlatformManager.Core.UnitTests.csproj</c>
/// (hai chú thích ghi lại đúng lý do cấm — thứ nhất định phải giữ). Nhóm (2)(3) đọc <c>.cs</c>
/// qua <see cref="RepoSourceTree.ReadCodeWithoutComments"/> vì lý do y hệt. Giá phải trả: một lời
/// gọi dựng bằng reflection hay bằng ghép chuỗi sẽ lọt — nhưng đó không phải hình dạng của lỗi
/// này, lỗi này luôn được gõ thẳng.</para>
/// </summary>
public class BannedDependencyTests
{
    /// <summary>
    /// Gói bị cấm. Khớp theo id gói: <c>Moq</c> khớp đúng tên, <c>*.InMemory</c> khớp mọi gói kết
    /// thúc bằng <c>.InMemory</c> (nay là <c>Microsoft.EntityFrameworkCore.InMemory</c>, mai có
    /// thể là gói tương đương của provider khác).
    /// </summary>
    private static readonly string[] BannedPackageSuffixes = [".InMemory"];

    private static readonly string[] BannedPackageIds = ["Moq"];

    /// <summary>
    /// Công tắc bị cấm trong mã nguồn sản phẩm. Khớp theo TÊN PHƯƠNG THỨC, nên bắt được cả
    /// <c>optionsBuilder.UseInMemoryDatabase(...)</c> lẫn <c>options.EnableSensitiveDataLogging()</c>
    /// bất kể viết trên biến nào.
    /// </summary>
    private static readonly (string Call, string Why)[] BannedCalls =
    [
        ("UseInMemoryDatabase",
            "provider InMemory BỎ QUA khoá ngoại, unique index và transaction thật — mọi thứ mà lớp " +
            "integration test tồn tại để kiểm. Dùng Testcontainers.PostgreSql (Postgres thật trong " +
            "container) cho integration test, và mock/NSubstitute cho unit test. Xem " +
            "doc/huong_dan/wiki-core/be/04-testing-strategy.md §\"Gotcha đáng nhớ nhất\"."),
        ("EnableSensitiveDataLogging",
            "nó đẩy GIÁ TRỊ tham số truy vấn vào log — mật khẩu, token, dữ liệu cá nhân — và log được " +
            "giữ nhiều ngày, thu về máy chủ tập trung, nhiều người đọc được hơn database. Cần xem tham số " +
            "khi gỡ lỗi thì bật TẠM ở máy mình và KHÔNG commit; muốn lâu dài thì bọc trong nhánh chỉ chạy " +
            "ở Development kèm chú thích lý do, và cân nhắc rất kỹ."),
    ];

    [Fact(DisplayName = "Không project nào tham chiếu gói đã bị luật loại bỏ (*.InMemory, Moq)")]
    public void NoProject_References_BannedPackages()
    {
        var projects = RepoSourceTree.ProjectFiles();

        Assert.True(projects.Count > 0,
            $"Không tìm thấy *.csproj nào dưới '{RepoSourceTree.BackendRoot}' ⇒ test này không đo gì. " +
            "Xem thông điệp của SolutionProjectCoverageTests.");

        var references = projects
            .SelectMany(path => PackageIdsIn(path).Select(id => (Project: path, Id: id)))
            .ToList();

        // Chặn "pass rỗng" ở tầng thứ hai: đọc được file nhưng không rút ra được gói nào (đổi định
        // dạng, đổi tên phần tử) cũng cho kết quả xanh y hệt như "sạch".
        Assert.True(references.Count > 0,
            "Đọc được csproj nhưng không rút ra được PackageReference nào ⇒ bộ đọc đang hỏng và test này " +
            "xanh mà không đo gì. Nguyên nhân có thể: repo đã chuyển sang Central Package Management " +
            "(Directory.Packages.props) — khi đó bổ sung file đó vào PackageIdsIn, ĐỪNG xoá test.");

        var violations = references
            .Where(reference => IsBanned(reference.Id))
            .Select(reference => $"{RepoSourceTree.Relative(reference.Project)} → {reference.Id}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            "Gói đã bị luật loại bỏ đang được tham chiếu: " + string.Join("; ", violations) + ". " +
            "Với *.InMemory: provider này bỏ qua FK/unique index/transaction, nên test dùng nó XANH mà " +
            "không chứng minh được gì về hành vi thật trên Postgres — dùng Testcontainers.PostgreSql thay " +
            "thế (xem PlatformManager.Core.IntegrationTests.csproj). Với Moq: repo dùng NSubstitute, một " +
            "thư viện mock thứ hai làm hai nửa bộ test viết theo hai lối. Cả hai theo " +
            "doc/huong_dan/wiki-core/be/04-testing-strategy.md. Docker không chạy được thì SỬA Docker — " +
            "đừng đổi provider.");
    }

    [Fact(DisplayName = "Mã nguồn sản phẩm không gọi UseInMemoryDatabase / EnableSensitiveDataLogging")]
    public void NoProductSource_Calls_BannedSwitches()
    {
        var sources = RepoSourceTree.ProductSourceFiles();

        Assert.True(sources.Count > 0,
            "Không tìm thấy file .cs sản phẩm nào ⇒ test này không đo gì. Xem thông điệp của " +
            "RepoSourceTree.LocateBackendRoot.");

        // Chặn "pass rỗng" ở tầng nội dung: đọc ra toàn chuỗi rỗng (sai encoding, sai đường dẫn)
        // cũng cho kết quả xanh y hệt như "sạch".
        var code = sources.ToDictionary(path => path, RepoSourceTree.ReadCodeWithoutComments);
        Assert.True(code.Values.Any(text => text.Contains("UseNpgsql", StringComparison.Ordinal)),
            "Không file sản phẩm nào chứa 'UseNpgsql' sau khi cắt chú thích ⇒ bộ đọc đang trả về nội dung " +
            "rỗng/sai, và test này xanh mà không đo gì. (UseNpgsql là mốc neo: nó chắc chắn có mặt trong " +
            "cấu hình DbContext — mất nó nghĩa là mất luôn khả năng thấy UseInMemoryDatabase nếu ai đó " +
            "thay nó bằng InMemory.)");

        foreach (var (call, why) in BannedCalls)
        {
            var hits = code
                .Where(entry => entry.Value.Contains(call, StringComparison.Ordinal))
                .Select(entry => RepoSourceTree.Relative(entry.Key))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.True(hits.Count == 0,
                $"Mã nguồn sản phẩm gọi `{call}` tại: " + string.Join(", ", hits) + $". Vì sao cấm: {why} " +
                "(Nhắc tên trong CHÚ THÍCH thì không bị tính — bộ dò đã cắt chú thích trước khi soi. " +
                "Nếu bạn thấy test đỏ mà chỗ đó đúng là chú thích, thì phép cắt đang hỏng, hãy sửa " +
                "RepoSourceTree.RemoveComments chứ đừng nới luật.)");
        }
    }

    /// <summary>
    /// Đối chứng — chứng minh CẢ HAI bộ dò biết nói KHÔNG, và quan trọng hơn: chứng minh chúng
    /// nói KHÔNG đúng chỗ. Cả hai chuỗi cấm hôm nay đang nằm trong chú thích của repo, nên nếu bộ
    /// dò không phân biệt được chú thích với code thì test chính sẽ ĐỎ OAN — và người sửa gần như
    /// chắc chắn sẽ xoá luôn hai chú thích ghi lại lý do cấm, tức xoá đúng phần tri thức đáng giữ
    /// nhất.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò bắt được vi phạm THẬT nhưng bỏ qua chú thích")]
    public void Detectors_Catch_RealViolations_ButIgnore_Comments()
    {
        // (1) Gói — XDocument bỏ qua XComment theo bản chất, không cần cắt chú thích thủ công.
        const string csprojWithCommentOnly = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <!-- NSubstitute chứ KHÔNG Moq; và TUYỆT ĐỐI không Microsoft.EntityFrameworkCore.InMemory. -->
                <PackageReference Include="NSubstitute" Version="5.3.0" />
              </ItemGroup>
            </Project>
            """;

        const string csprojWithRealReference = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.11" />
              </ItemGroup>
            </Project>
            """;

        Assert.DoesNotContain(PackageIdsInXml(csprojWithCommentOnly), IsBanned);
        Assert.Contains(PackageIdsInXml(csprojWithRealReference), IsBanned);

        // (2)(3) Công tắc — dựa vào RemoveComments, chính bộ cắt mà test chính dùng.
        const string codeWithCommentOnly = """
            // TUYỆT ĐỐI không UseInMemoryDatabase, và không EnableSensitiveDataLogging.
            options.UseNpgsql(connectionString);
            """;

        const string codeWithRealCalls = """
            options.UseInMemoryDatabase("test");
            options.EnableSensitiveDataLogging();
            """;

        var cleanedComment = RepoSourceTree.RemoveComments(codeWithCommentOnly);
        var cleanedReal = RepoSourceTree.RemoveComments(codeWithRealCalls);

        foreach (var (call, _) in BannedCalls)
        {
            Assert.False(cleanedComment.Contains(call, StringComparison.Ordinal),
                $"Bộ dò tính `{call}` nằm trong chú thích là vi phạm ⇒ NoProductSource_Calls_BannedSwitches " +
                "sẽ đỏ oan, và người sửa sẽ xoá mất chú thích ghi lý do cấm. Sửa RepoSourceTree.RemoveComments.");
            Assert.True(cleanedReal.Contains(call, StringComparison.Ordinal),
                $"Bộ dò KHÔNG thấy `{call}` khi nó là lời gọi thật ⇒ " +
                "NoProductSource_Calls_BannedSwitches đang xanh mà không đo gì.");
        }
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    private static bool IsBanned(string packageId) =>
        BannedPackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase)
        || BannedPackageSuffixes.Any(suffix => packageId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> PackageIdsIn(string csprojPath) =>
        PackageIdsInXml(File.ReadAllText(csprojPath));

    private static List<string> PackageIdsInXml(string csprojXml) =>
        [.. XDocument.Parse(csprojXml)
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())];
}
