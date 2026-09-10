using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <c>Api_MustNotReference_PersistenceOrInfrastructure_Directly</c> — rule thứ ba trong danh sách
/// "ArchTest cần có" của doc/kien-truc-core-module.md, viết 2026-09-09.
///
/// <para><b>Vì sao viết ĐƯỢC bây giờ, sau khi bị hoãn.</b> Ô trạng thái của rule này từng ghi
/// <c>📐 ĐÍCH ĐẾN</c> với lý do <i>"không có assembly *.Api theo tầng, chỉ có host
/// PlatformManager.Api, và nó PHẢI reference Infrastructure — nó là composition root"</i>. Lý do
/// đó hết đúng khi <c>PlatformManager.Core.Api</c> được dựng (Q8, 2026-09-09): đó là một assembly
/// <c>*.Api</c> KHÔNG phải host, nên tập quét nay có phần tử thật và luật đo được thật. Trước lần
/// này ranh giới của <c>Core.Api</c> chỉ được canh bằng một khối chú thích trong
/// <c>PlatformManager.Core.Api.csproj</c> — chú thích không làm test nào đỏ.</para>
///
/// <para><b>Phạm vi cố ý hẹp: chỉ cấm <c>*.Persistence</c> và <c>*.Infrastructure</c>.</b> Reference
/// từ một <c>*.Api</c> sang một <c>*.Api</c> khác KHÔNG bị cấm — đó là cạnh
/// <c>Business.Api → Core.Api</c> đã khai hợp lệ ở doc/kien-truc-core-module.md §"Nguyên tắc phụ
/// thuộc bắt buộc" (Business.Api kế thừa <c>ApiControllerBase</c>) và ở
/// doc/huong_dan/quy-uoc/be-architecture.md. Cấm nhầm cạnh đó sẽ làm tầng nghiệp vụ đầu tiên
/// không dựng được.</para>
///
/// <para><b>Host <c>PlatformManager.Api</c> được loại trừ, và đó không phải lỗ hổng.</b> Host là
/// composition root — nó BẮT BUỘC thấy Infrastructure để nối DI, EF và design-time factory. Việc
/// nó là project DUY NHẤT thấy cả hai tầng thuộc về một luật khác
/// (<c>OnlyHostApi_MustReference_BothUnits</c>, vẫn <c>📐</c> cho tới khi <c>Business.*</c> tồn tại).</para>
/// </summary>
public class ApiLayerBoundaryTests
{
    /// <summary>Assembly host — composition root, được phép thấy mọi tầng.</summary>
    private const string HostApiAssemblyName = "PlatformManager.Api";

    private const string ApiAssemblySuffix = ".Api";

    /// <summary>
    /// Hai tầng mà controller không được chạm thẳng. So bằng <b>đoạn tên</b> chứ không bằng tiền
    /// tố: tên đầy đủ là <c>PlatformManager.Core.Persistence</c> /
    /// <c>PlatformManager.Business.Infrastructure</c>… nên chỉ có phần giữa là bất biến. Cùng khuôn
    /// với <c>n.Contains(".Infrastructure")</c> đang dùng ở
    /// <see cref="LayerDependencyTests"/>.
    /// </summary>
    private static readonly string[] ForbiddenLayerMarkers = [".Persistence", ".Infrastructure"];

    /// <summary>
    /// Mọi assembly <c>*.Api</c> của sản phẩm TRỪ host. Dẫn xuất từ
    /// <see cref="ProductAssemblies.All"/> thay vì liệt kê tay — một tầng <c>*.Api</c> mới chỉ phải
    /// khai ở đúng MỘT chỗ, và chỗ đó đã mang nhãn 🛑 cảnh báo hậu quả của việc quên.
    /// </summary>
    private static List<Assembly> TieredApiAssemblies() =>
        [.. ProductAssemblies.All.Where(assembly =>
        {
            var name = assembly.GetName().Name!;
            return name.EndsWith(ApiAssemblySuffix, StringComparison.Ordinal)
                   && !string.Equals(name, HostApiAssemblyName, StringComparison.Ordinal);
        })];

    [Fact(DisplayName = "Assembly *.Api theo tầng không reference *.Persistence/*.Infrastructure")]
    public void Api_MustNotReference_PersistenceOrInfrastructure_Directly()
    {
        var apiAssemblies = TieredApiAssemblies();

        // Chặn "xanh mà không đo gì": đây là chính khuyết tật mà rule này bị hoãn để tránh. Nếu
        // mọi assembly *.Api biến mất khỏi ProductAssemblies.All (hoặc project bị gộp lại vào
        // host), tập quét về rỗng và luật sẽ xanh vĩnh viễn mà không kiểm một cạnh nào.
        Assert.True(apiAssemblies.Count > 0,
            "Không có assembly *.Api nào ngoài host trong ProductAssemblies.All ⇒ luật này xanh mà " +
            "không đo gì. Hoặc project *.Api theo tầng đã bị gỡ, hoặc nó chưa được khai vào " +
            "ProductAssemblies.All (xem nhãn 🛑 tại đó). Đừng để nguyên trạng thái này.");

        var violations = new List<string>();

        foreach (var apiAssembly in apiAssemblies)
        {
            var forbidden = apiAssembly.GetReferencedAssemblies()
                .Select(reference => reference.Name!)
                .Where(name => ForbiddenLayerMarkers.Any(marker =>
                    name.Contains(marker, StringComparison.Ordinal)));

            violations.AddRange(forbidden.Select(name => $"{apiAssembly.GetName().Name} -> {name}"));
        }

        Assert.True(violations.Count == 0,
            "Assembly *.Api reference thẳng Persistence/Infrastructure (KHÔNG được phép): " +
            string.Join(", ", violations) +
            ". Controller chỉ nói chuyện qua MediatR — không tự inject repository/DbContext. Xem " +
            "doc/kien-truc-core-module.md muc \"Nguyên tắc phụ thuộc bắt buộc\".");
    }

    internal static readonly Regex ProjectReferenceInclude =
        new(@"<ProjectReference\s+[^>]*Include\s*=\s*""([^""]+)""", RegexOptions.Compiled);

    internal static readonly Regex XmlComment =
        new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Luật bổ sung ở mức VĂN BẢN <c>.csproj</c> — bịt đúng lỗ hổng đã đo của luật assembly ở trên.
    ///
    /// <para><c>GetReferencedAssemblies()</c> chỉ thấy assembly thật sự có code chạm tới: Roslyn
    /// lược khỏi manifest mọi reference không dùng (đo 2026-08-28, ghi ở
    /// <see cref="LayerDependencyTests"/>). Nghĩa là thêm
    /// <c>&lt;ProjectReference&gt;</c> tới <c>Core.Infrastructure</c> "cho tiện" mà chưa gọi gì —
    /// đúng bước ĐẦU TIÊN của mọi ca phá ranh giới — vẫn để luật trên xanh. Luật này bắt ngay bước
    /// đó, vì nó đọc thứ người viết gõ ra chứ không đọc thứ trình biên dịch giữ lại.</para>
    /// </summary>
    [Fact(DisplayName = "csproj của *.Api theo tầng không khai ProjectReference tới Persistence/Infrastructure")]
    public void ApiProjects_MustNotDeclare_ProjectReference_To_PersistenceOrInfrastructure()
    {
        var apiProjectFiles = RepoSourceTree.ProjectFiles()
            .Where(path =>
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                return fileName.EndsWith(ApiAssemblySuffix, StringComparison.Ordinal)
                       && !string.Equals(fileName, HostApiAssemblyName, StringComparison.Ordinal);
            })
            .ToList();

        // Chặn "xanh mà không đo gì" (1/2): tập file rỗng ⇒ "không ai vi phạm".
        Assert.True(apiProjectFiles.Count > 0,
            $"Không tìm thấy .csproj nào của tầng *.Api (ngoài host) dưới '{RepoSourceTree.BackendRoot}' ⇒ " +
            "luật này không đo gì. Xem RepoSourceTree.LocateBackendRoot.");

        var violations = new List<string>();
        var parsedReferences = 0;

        foreach (var projectFile in apiProjectFiles)
        {
            // Cắt chú thích XML trước: PlatformManager.Core.Api.csproj cố ý VIẾT RA cụm
            // "*.Persistence / *.Infrastructure" trong khối chú thích cấm — không cắt thì luật
            // báo lỗi cho chính dòng văn xuôi nói rằng nó bị cấm.
            var xml = XmlComment.Replace(File.ReadAllText(projectFile), string.Empty);

            foreach (Match match in ProjectReferenceInclude.Matches(xml))
            {
                parsedReferences++;
                var include = match.Groups[1].Value;

                if (ForbiddenLayerMarkers.Any(marker => include.Contains(marker, StringComparison.Ordinal)))
                    violations.Add($"{RepoSourceTree.Relative(projectFile)} -> {include}");
            }
        }

        // Chặn "xanh mà không đo gì" (2/2): bộ dò hỏng (đổi khuôn khai ProjectReference, hoặc regex
        // sai) ⇒ 0 reference đọc được ở khắp nơi ⇒ xanh. Mọi *.Api đều phải reference ít nhất
        // Application của tầng nó, nên con số này không thể bằng 0 một cách hợp lệ.
        Assert.True(parsedReferences > 0,
            "Không đọc được ProjectReference nào từ csproj của tầng *.Api ⇒ bộ dò đã hỏng và luật này " +
            "đang xanh mà không đo gì. Sửa regex ProjectReferenceInclude, ĐỪNG nới luật cho xanh.");

        Assert.True(violations.Count == 0,
            "csproj của tầng *.Api khai ProjectReference tới Persistence/Infrastructure " +
            "(KHÔNG được phép, kể cả khi chưa có dòng code nào dùng tới): " +
            string.Join(", ", violations) +
            ". Reference sang một *.Api khác thì được phép (Business.Api → Core.Api để kế thừa " +
            "ApiControllerBase) — chỉ hai tầng Persistence/Infrastructure bị cấm.");
    }
}
