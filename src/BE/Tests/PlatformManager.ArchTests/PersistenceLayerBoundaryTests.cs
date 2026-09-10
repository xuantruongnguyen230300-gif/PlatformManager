using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Chiều phụ thuộc của tầng <c>*.Persistence</c> — thêm 2026-09-10 cùng lượt tách
/// <c>PlatformManager.Core.Persistence</c> khỏi <c>Core.Infrastructure</c> (doc/kien-truc-core-module.md
/// §"tách Core.Persistence khỏi Core.Infrastructure").
///
/// <para><b>Luật:</b> một assembly <c>*.Persistence</c> KHÔNG được reference <c>*.Infrastructure</c>
/// hay <c>*.Api</c> (kể cả host <c>PlatformManager.Api</c>). Đồ thị đã khai ở §"Nguyên tắc phụ thuộc
/// bắt buộc": <c>Core.Persistence → Core.Application, Core.Domain</c> và
/// <c>Business.Persistence → Core.Persistence, Business.Application, Business.Domain</c> — reference
/// sang một <c>*.Persistence</c> khác vì thế được phép.</para>
///
/// <para><b>Vì sao cần test dù trình biên dịch đã chặn một phần.</b> <c>Persistence → Infrastructure</c>
/// hôm nay là vòng tròn (Infrastructure reference Persistence) nên không build được — nhưng đó là
/// hệ quả phụ của MỘT cạnh khác, không phải luật: gỡ cạnh ngược kia là vòng tròn biến mất cùng lúc
/// với lưới chặn. Còn <c>Persistence → Api</c> thì không có vòng tròn nào chặn cả.</para>
///
/// <para>Hai mức, cùng khuôn và cùng lý do với <see cref="ApiLayerBoundaryTests"/>: mức assembly bắt
/// "code đã gọi sang", mức văn bản <c>.csproj</c> bắt bước đầu tiên — thêm
/// <c>&lt;ProjectReference&gt;</c> "cho tiện" khi chưa có dòng code nào dùng tới, thứ
/// <c>GetReferencedAssemblies()</c> không thấy.</para>
/// </summary>
public class PersistenceLayerBoundaryTests
{
    private const string PersistenceSuffix = ".Persistence";

    /// <summary>Chỉ xét assembly/project CỦA REPO — gói ngoài không mang tiền tố này.</summary>
    private const string ProductPrefix = "PlatformManager.";

    private static bool IsForbiddenTarget(string name) =>
        name.StartsWith(ProductPrefix, StringComparison.Ordinal)
        && (name.Contains(".Infrastructure", StringComparison.Ordinal)
            || name.EndsWith(".Api", StringComparison.Ordinal));

    /// <summary>Dẫn xuất từ <see cref="ProductAssemblies.All"/> — không liệt kê tay lần thứ hai.</summary>
    private static List<Assembly> PersistenceAssemblies() =>
        [.. ProductAssemblies.All.Where(assembly =>
            assembly.GetName().Name!.EndsWith(PersistenceSuffix, StringComparison.Ordinal))];

    [Fact(DisplayName = "Assembly *.Persistence không reference *.Infrastructure/*.Api")]
    public void Persistence_MustNotReference_InfrastructureOrApi()
    {
        var assemblies = PersistenceAssemblies();

        // Chặn "xanh mà không đo gì": Core.Persistence vắng khỏi ProductAssemblies.All ⇒ tập rỗng.
        Assert.True(assemblies.Count > 0,
            "Không có assembly *.Persistence nào trong ProductAssemblies.All ⇒ luật này xanh mà không đo gì. " +
            "Khai assembly Persistence vào ProductAssemblies.All (xem nhãn 🛑 tại đó).");

        var violations = assemblies
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name!)
                .Where(IsForbiddenTarget)
                .Select(name => $"{assembly.GetName().Name} -> {name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            "Assembly *.Persistence reference thẳng Infrastructure/Api (KHÔNG được phép): " +
            string.Join(", ", violations) +
            ". Persistence chỉ được thấy Application/Domain (và Core.Persistence, nếu là tầng nghiệp vụ). " +
            "Cần một dịch vụ của Infrastructure thì khai interface ở *.Application — xem " +
            "doc/kien-truc-core-module.md muc \"Nguyên tắc phụ thuộc bắt buộc\".");
    }

    [Fact(DisplayName = "csproj của *.Persistence không khai ProjectReference tới *.Infrastructure/*.Api")]
    public void PersistenceProjects_MustNotDeclare_ProjectReference_To_InfrastructureOrApi()
    {
        var projectFiles = RepoSourceTree.ProjectFiles()
            .Where(path => Path.GetFileNameWithoutExtension(path).EndsWith(PersistenceSuffix, StringComparison.Ordinal))
            .ToList();

        // Chặn "xanh mà không đo gì" (1/2): tập file rỗng ⇒ "không ai vi phạm".
        Assert.True(projectFiles.Count > 0,
            $"Không tìm thấy .csproj nào của tầng *.Persistence dưới '{RepoSourceTree.BackendRoot}' ⇒ luật này " +
            "không đo gì. Xem RepoSourceTree.LocateBackendRoot.");

        var violations = new List<string>();
        var parsedReferences = 0;

        foreach (var projectFile in projectFiles)
        {
            // Cắt chú thích XML trước: csproj của Core.Persistence cố ý VIẾT RA "*.Infrastructure"/"*.Api"
            // trong khối chú thích cấm.
            var xml = ApiLayerBoundaryTests.XmlComment.Replace(File.ReadAllText(projectFile), string.Empty);

            foreach (Match match in ApiLayerBoundaryTests.ProjectReferenceInclude.Matches(xml))
            {
                parsedReferences++;
                var include = match.Groups[1].Value;
                var target = Path.GetFileNameWithoutExtension(include.Replace('\\', '/').Split('/')[^1]);

                if (IsForbiddenTarget(target))
                    violations.Add($"{RepoSourceTree.Relative(projectFile)} -> {include}");
            }
        }

        // Chặn "xanh mà không đo gì" (2/2): mọi *.Persistence đều reference ít nhất Application của
        // tầng nó, nên 0 reference đọc được nghĩa là bộ dò hỏng chứ không phải "sạch".
        Assert.True(parsedReferences > 0,
            "Không đọc được ProjectReference nào từ csproj của tầng *.Persistence ⇒ bộ dò đã hỏng. Sửa regex, " +
            "ĐỪNG nới luật cho xanh.");

        Assert.True(violations.Count == 0,
            "csproj của tầng *.Persistence khai ProjectReference tới Infrastructure/Api (KHÔNG được phép, kể cả " +
            "khi chưa có dòng code nào dùng tới): " + string.Join(", ", violations) + ".");
    }
}
