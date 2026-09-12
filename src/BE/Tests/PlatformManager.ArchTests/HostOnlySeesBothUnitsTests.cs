using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <c>OnlyHostApi_MustReference_BothUnits</c> — rule cuối trong danh sách "ArchTest cần có" của
/// doc/kien-truc-core-module.md, viết 2026-09-10 CÙNG LƯỢT dựng <c>Business.*</c> đầu tiên.
///
/// <para><b>Luật:</b> <c>PlatformManager.Api</c> (host) là project DUY NHẤT được phép reference
/// cả <c>Core.*</c> lẫn <c>Business.*</c> cùng lúc. Mọi project sản phẩm khác chỉ thuộc về ĐÚNG
/// MỘT tầng — <c>Business.*</c> được phép nhìn sang <c>Core.*</c> (chiều hợp lệ), nhưng không
/// <c>Core.*</c> nào được nhìn sang <c>Business.*</c>.</para>
///
/// <para><b>Vì sao viết ĐƯỢC bây giờ, và vì sao không viết sớm hơn.</b> Ô trạng thái của rule
/// này ghi <c>📐 ĐÍCH ĐẾN</c> kèm một câu điều kiện: <i>"viết bây giờ sẽ là test RỖNG — chúng
/// quét một tập không có phần tử nào, nên xanh vĩnh viễn mà không đo gì"</i>, và dặn viết nó cùng
/// lượt dựng <c>Business.*</c> kèm khẳng định <i>"tập quét được khác rỗng"</i>. Đó chính là
/// <see cref="Assert.True(bool, string)"/> đầu mỗi test dưới đây — bỏ chúng đi là biến luật này
/// trở lại thành thứ nó vừa thoát ra.</para>
///
/// <para><b>Đo trên VĂN BẢN <c>.csproj</c>, không trên assembly.</b> Khác với
/// <see cref="ApiLayerBoundaryTests"/> (đo cả hai mức), rule này CHỈ cần mức <c>.csproj</c> —
/// <c>GetReferencedAssemblies()</c> chỉ thấy assembly thật sự có code chạm tới, nên một project
/// <c>Core.*</c> khai <c>ProjectReference</c> tới <c>Business.*</c> mà chưa gọi gì sẽ vô hình ở
/// mức assembly. Mà "thêm reference cho tiện" chính là bước ĐẦU TIÊN của mọi ca phá ranh giới —
/// đúng kết luận đã rút ra từ canary 2026-09-09.</para>
///
/// <para>Rule này KHÔNG thay <c>CoreModuleBoundaryTests</c>: cái kia quét ở mức assembly và canh
/// chiều <c>Core → Modules</c>; cái này canh thêm mệnh đề <i>"DUY NHẤT host thấy cả hai"</i>, tức
/// bắt được cả một project <c>Tests</c>-lookalike hay một project mới nào đó đứng giữa hai tầng.</para>
/// </summary>
public class HostOnlySeesBothUnitsTests
{
    private const string HostProjectFileName = "PlatformManager.Api.csproj";

    /// <summary>Đọc thuộc tính <c>Include</c> của mọi <c>&lt;ProjectReference&gt;</c>.</summary>
    private static readonly Regex ProjectReferencePattern =
        new(@"<ProjectReference\s+Include\s*=\s*""([^""]+)""", RegexOptions.Compiled);

    [Fact(DisplayName = "Chỉ host PlatformManager.Api được reference cả Core.* lẫn Business.*")]
    public void OnlyHostApi_MustReference_BothUnits()
    {
        var projects = ProductProjectFiles();

        Assert.True(projects.Count > 0,
            $"Không tìm thấy *.csproj sản phẩm nào dưới '{RepoSourceTree.BackendRoot}' ⇒ luật này không " +
            "đo gì. Xem thông điệp của RepoSourceTree.LocateBackendRoot.");

        // Khẳng định "tập quét khác rỗng" mà ô 📐 của doc/kien-truc-core-module.md yêu cầu kèm
        // theo khi viết rule này: phải CÓ THẬT project Business.* thì mệnh đề "chỉ host thấy cả
        // hai" mới có gì để đo. Không có nó, luật xanh vĩnh viễn trên một tập một phần tử.
        Assert.True(
            projects.Any(path => Path.GetFileName(path).Contains(".Business.", StringComparison.Ordinal)),
            "Không có project PlatformManager.Business.* nào trên đĩa ⇒ luật này xanh mà không đo gì. " +
            "Nếu tầng nghiệp vụ vừa bị gỡ thì gỡ luôn luật này (và ghi lại lý do); nếu nó chỉ bị đổi " +
            "tên thì sửa bộ dò ở đây, ĐỪNG nới luật.");

        var violations = new List<string>();

        foreach (var project in projects)
        {
            if (string.Equals(Path.GetFileName(project), HostProjectFileName, StringComparison.Ordinal))
                continue;

            var references = ReferencedProjectNames(project);
            var seesCore = references.Any(name => name.Contains(".Core.", StringComparison.Ordinal));
            var seesBusiness = references.Any(name => name.Contains(".Business.", StringComparison.Ordinal));

            // Business.* THẤY Core.* là cạnh HỢP LỆ và bắt buộc (Business.Domain → Core.Domain…).
            // Điều bị cấm là một project KHÔNG thuộc tầng nghiệp vụ mà lại thấy cả hai — nó nghĩa
            // là có một composition root thứ hai, hoặc Core đang nhìn sang nghiệp vụ.
            var isBusinessProject = Path.GetFileName(project).Contains(".Business.", StringComparison.Ordinal);

            if (seesCore && seesBusiness && !isBusinessProject)
                violations.Add(RepoSourceTree.Relative(project));
        }

        Assert.True(violations.Count == 0,
            "Project KHÁC host reference cả Core.* lẫn Business.*: " + string.Join(", ", violations) +
            ". Host PlatformManager.Api là composition root DUY NHẤT được thấy cả hai tầng " +
            "(doc/kien-truc-core-module.md §\"Nguyên tắc phụ thuộc bắt buộc\"). Một project thứ hai " +
            "thấy cả hai nghĩa là ranh giới Core↔Business không còn chỗ nào cưỡng chế được, và lúc " +
            "tách CoreBase sang dự án sau sẽ không biết cắt ở đâu.");
    }

    [Fact(DisplayName = "Không project Core.* nào reference Business.* ở mức văn bản .csproj")]
    public void CoreProjects_MustNotReference_Business_AtCsprojLevel()
    {
        var coreProjects = ProductProjectFiles()
            .Where(path => Path.GetFileName(path).Contains(".Core.", StringComparison.Ordinal))
            .ToList();

        Assert.True(coreProjects.Count > 0,
            "Không tìm thấy project PlatformManager.Core.* nào ⇒ luật này không đo gì.");

        var violations = coreProjects
            .Where(project => ReferencedProjectNames(project)
                .Any(name => name.Contains(".Business.", StringComparison.Ordinal)))
            .Select(RepoSourceTree.Relative)
            .ToList();

        Assert.True(violations.Count == 0,
            "Project Core.* khai ProjectReference tới Business.*: " + string.Join(", ", violations) +
            ". Core KHÔNG được biết về nghiệp vụ — đây là ranh giới quyết định việc CoreBase còn " +
            "tách sang dự án thứ hai được hay không. Luật ở mức assembly (CoreModuleBoundaryTests) " +
            "không bắt được ca này khi reference đã thêm mà chưa gọi gì.");
    }

    /// <summary>
    /// Mọi <c>.csproj</c> SẢN PHẨM — bỏ <c>Tests/</c>. Project test được phép thấy toàn bộ để kiểm
    /// tra chéo (khối chú thích ở <c>PlatformManager.ArchTests.csproj</c> khai rõ ngoại lệ đó),
    /// nên đưa chúng vào tập quét sẽ làm luật đỏ oan ngay lượt chạy đầu.
    /// </summary>
    private static List<string> ProductProjectFiles() =>
        [.. RepoSourceTree.ProjectFiles().Where(path =>
            !path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];

    private static List<string> ReferencedProjectNames(string projectFile) =>
        [.. ProjectReferencePattern.Matches(File.ReadAllText(projectFile))
            .Select(match => Path.GetFileName(match.Groups[1].Value.Replace('\\', '/')))];
}
