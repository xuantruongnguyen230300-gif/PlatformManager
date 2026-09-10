using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Luật <c>*.Application ⇏ *.Persistence/*.Infrastructure</c> cho MỌI tầng Application — khai ở
/// doc/huong_dan/quy-uoc/be-architecture.md §"Project layout &amp; dependency direction", thêm test
/// 2026-09-10 theo finding của core-reviewer ngay sau lượt tách <c>Core.Persistence</c>.
///
/// <para><b>Lỗ hổng được bịt.</b> Trước file này luật chỉ có ở mức assembly, chỉ cho
/// <c>Core.Application</c> (<see cref="LayerDependencyTests"/>), và điều kiện chỉ nhận
/// <c>.Infrastructure</c>. <c>Core.Application → Core.Persistence</c> bị chặn nhờ VÒNG THAM CHIẾU
/// (<c>Core.Persistence</c> reference <c>Core.Application</c>) — hệ quả phụ, không phải luật, cùng
/// nhận định ở <see cref="PersistenceLayerBoundaryTests"/>. Còn <c>Business.Application →
/// Core.Persistence</c> thì không vòng nào chặn: tầng nghiệp vụ inject thẳng <c>DbContext</c> mà mọi
/// cổng vẫn xanh.</para>
///
/// <para>Hai mức, cùng khuôn với <see cref="ApiLayerBoundaryTests"/>: mức assembly bắt "code đã gọi
/// sang", mức văn bản <c>.csproj</c> bắt bước đầu tiên — thêm <c>&lt;ProjectReference&gt;</c> khi chưa
/// có dòng code nào dùng, thứ <c>GetReferencedAssemblies()</c> không thấy. Kèm một ca đối chứng chạy
/// CHÍNH bộ dò <c>.csproj</c> trên một <c>Business.Application</c> dựng tay: đó đúng là ca không có
/// vòng tham chiếu nào che, và hôm nay chưa có project thật nào để đo nó.</para>
/// </summary>
public class ApplicationLayerBoundaryTests
{
    private const string ApplicationSuffix = ".Application";

    /// <summary>So bằng đoạn tên, cùng lý do với marker của <see cref="ApiLayerBoundaryTests"/>.</summary>
    private static readonly string[] ForbiddenLayerMarkers = [".Persistence", ".Infrastructure"];

    /// <summary>Dẫn xuất từ <see cref="ProductAssemblies.All"/> — không liệt kê tay lần thứ hai.</summary>
    private static List<Assembly> ApplicationAssemblies() =>
        [.. ProductAssemblies.All.Where(assembly =>
            assembly.GetName().Name!.EndsWith(ApplicationSuffix, StringComparison.Ordinal))];

    [Fact(DisplayName = "Mọi assembly *.Application không reference *.Persistence/*.Infrastructure hay gói hạ tầng")]
    public void EveryApplication_MustNotReference_PersistenceOrInfrastructure()
    {
        var assemblies = ApplicationAssemblies();

        // Chặn "xanh mà không đo gì": Core.Application vắng khỏi ProductAssemblies.All ⇒ tập rỗng.
        Assert.True(assemblies.Count > 0,
            "Không có assembly *.Application nào trong ProductAssemblies.All ⇒ luật này xanh mà không đo gì. " +
            "Khai assembly Application vào ProductAssemblies.All (xem nhãn 🛑 tại đó).");

        var violations = assemblies
            .SelectMany(assembly => LayerDependencyTests.ForbiddenReferencesOf(assembly)
                .Select(name => $"{assembly.GetName().Name} -> {name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            "Assembly *.Application reference Persistence/Infrastructure/gói hạ tầng (KHÔNG được phép): " +
            string.Join(", ", violations) +
            ". Application chỉ nói chuyện với dữ liệu qua interface khai ở chính nó (repository, IUnitOfWork) — " +
            "không inject DbContext. Xem doc/huong_dan/quy-uoc/be-architecture.md.");
    }

    [Fact(DisplayName = "csproj của *.Application không khai ProjectReference tới *.Persistence/*.Infrastructure")]
    public void ApplicationProjects_MustNotDeclare_ProjectReference_To_PersistenceOrInfrastructure()
    {
        var projectFiles = RepoSourceTree.ProjectFiles()
            .Where(path => Path.GetFileNameWithoutExtension(path).EndsWith(ApplicationSuffix, StringComparison.Ordinal))
            .ToList();

        // Chặn "xanh mà không đo gì" (1/2): tập file rỗng ⇒ "không ai vi phạm".
        Assert.True(projectFiles.Count > 0,
            $"Không tìm thấy .csproj nào của tầng *.Application dưới '{RepoSourceTree.BackendRoot}' ⇒ luật này " +
            "không đo gì. Xem RepoSourceTree.LocateBackendRoot.");

        var violations = new List<string>();
        var parsedReferences = 0;

        foreach (var projectFile in projectFiles)
        {
            var (parsed, forbidden) = ScanProjectReferences(File.ReadAllText(projectFile));
            parsedReferences += parsed;
            violations.AddRange(forbidden.Select(include => $"{RepoSourceTree.Relative(projectFile)} -> {include}"));
        }

        // Chặn "xanh mà không đo gì" (2/2): mọi *.Application reference ít nhất Domain của tầng nó, nên
        // 0 reference đọc được nghĩa là bộ dò hỏng chứ không phải "sạch".
        Assert.True(parsedReferences > 0,
            "Không đọc được ProjectReference nào từ csproj của tầng *.Application ⇒ bộ dò đã hỏng. Sửa regex, " +
            "ĐỪNG nới luật cho xanh.");

        Assert.True(violations.Count == 0,
            "csproj của tầng *.Application khai ProjectReference tới Persistence/Infrastructure (KHÔNG được phép, " +
            "kể cả khi chưa có dòng code nào dùng tới): " + string.Join(", ", violations) + ".");
    }

    /// <summary>
    /// Ca đối chứng: chạy CHÍNH <see cref="ScanProjectReferences"/> mà luật thật dùng, trên một
    /// <c>Business.Application</c> dựng tay. Bộ dò phải bắt cạnh sang <c>Core.Persistence</c>, bỏ qua
    /// cạnh hợp lệ sang <c>Core.Application</c>/<c>Business.Domain</c>, và không đếm reference nằm trong
    /// chú thích XML.
    /// </summary>
    [Fact(DisplayName = "Bộ dò csproj bắt Business.Application → Core.Persistence (ca không vòng tham chiếu nào che)")]
    public void Detector_Flags_BusinessApplication_To_CorePersistence()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <!-- <ProjectReference Include="..\..\Core\PlatformManager.Core.Infrastructure\PlatformManager.Core.Infrastructure.csproj" /> -->
              <ItemGroup>
                <ProjectReference Include="..\..\Core\PlatformManager.Core.Application\PlatformManager.Core.Application.csproj" />
                <ProjectReference Include="..\PlatformManager.Business.Domain\PlatformManager.Business.Domain.csproj" />
                <ProjectReference Include="..\..\Core\PlatformManager.Core.Persistence\PlatformManager.Core.Persistence.csproj" />
              </ItemGroup>
            </Project>
            """;

        var (parsed, forbidden) = ScanProjectReferences(csproj);

        Assert.Equal(3, parsed);
        var only = Assert.Single(forbidden);
        Assert.EndsWith("PlatformManager.Core.Persistence.csproj", only, StringComparison.Ordinal);
    }

    /// <summary>
    /// Bộ dò THUẦN trên văn bản csproj — tách ra để ca đối chứng chạy đúng hàm luật thật chạy. Regex
    /// dùng lại của <see cref="ApiLayerBoundaryTests"/>, không chép.
    /// </summary>
    internal static (int Parsed, List<string> Forbidden) ScanProjectReferences(string csprojText)
    {
        var xml = ApiLayerBoundaryTests.XmlComment.Replace(csprojText, string.Empty);
        var parsed = 0;
        var forbidden = new List<string>();

        foreach (Match match in ApiLayerBoundaryTests.ProjectReferenceInclude.Matches(xml))
        {
            parsed++;
            var include = match.Groups[1].Value;
            var target = Path.GetFileNameWithoutExtension(include.Replace('\\', '/').Split('/')[^1]);

            if (ForbiddenLayerMarkers.Any(marker => target.Contains(marker, StringComparison.Ordinal)))
                forbidden.Add(include);
        }

        return (parsed, forbidden);
    }
}
