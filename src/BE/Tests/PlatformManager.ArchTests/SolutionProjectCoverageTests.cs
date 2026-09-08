using System.Xml.Linq;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi <c>*.csproj</c> có trên đĩa trong cây backend phải được khai trong
/// <c>PlatformManager.slnx</c>, và ngược lại mọi mục khai trong solution phải trỏ tới một file có
/// thật.
///
/// <para><b>Lỗi ĐÃ XẢY RA THẬT trong repo này:</b> 2 project test tồn tại trên đĩa nhưng không
/// được khai trong solution. <c>dotnet test PlatformManager.slnx</c> khi đó bỏ qua chúng <b>trong
/// im lặng</b> — không cảnh báo, không mã lỗi, không dòng log nào. Gate xanh trong khi <b>59
/// test không hề chạy</b>. Đây là dạng hỏng tệ nhất của một bộ test: nó không mất đi, nó chỉ
/// thôi chạy, và cái bảng "tất cả đã xanh" vẫn hiện đúng như cũ.</para>
///
/// <para><b>Vì sao đây là luật kiến trúc chứ không phải chuyện vặt về file:</b> solution là thứ
/// định nghĩa "cái gì được biên dịch và được chạy". Một project ngoài solution vẫn build được
/// khi có ai đó gọi thẳng nó, nên nó trông vẫn sống — nhưng nó đã rơi ra khỏi mọi quy trình tự
/// động. Số project ít, người ta tin là mình nhớ hết, và chính vì tin nên không ai kiểm.</para>
///
/// <para><b>⚠️ GIỚI HẠN — test này KHÔNG tự bảo vệ được chính nó.</b> Nếu
/// <c>PlatformManager.ArchTests</c> bị gỡ khỏi <c>.slnx</c> thì nó cũng không được chạy nữa, và
/// khi đó không còn gì báo động cả. Nó bảo vệ 6 project CÒN LẠI, không bảo vệ mục thứ 7 là bản
/// thân nó. Không có cách nào vá lỗ này từ bên trong bộ test — chỉ một cổng bên ngoài (script
/// kiểm repo) mới làm được. Ghi ở đây để không ai hiểu nhầm phạm vi.</para>
///
/// <para><b>Đánh đổi:</b> đọc <c>.slnx</c> như XML (định dạng solution mới, đã là XML thật — khác
/// <c>.sln</c> cũ phải parse bằng regex). Nguồn kỳ vọng (đĩa) và nguồn thực tế (solution) hoàn
/// toàn độc lập nhau, nên test không thể "tự hỏi mình rồi tự trả lời". Giá phải trả: nếu sau này
/// repo cố ý giữ một project ngoài solution (ví dụ mẫu code, sandbox), test sẽ đỏ — khi đó thêm
/// một allowlist tường minh ở đây, ĐỪNG xoá test.</para>
/// </summary>
public class SolutionProjectCoverageTests
{
    [Fact(DisplayName = "Mọi *.csproj trên đĩa đều được khai trong PlatformManager.slnx")]
    public void EveryProjectOnDisk_IsDeclared_InSolution()
    {
        var onDisk = ProjectsOnDisk();
        var inSolution = ProjectsInSolution();

        // Chặn "pass rỗng" ở CẢ HAI nguồn — sai đường dẫn ở bất kỳ nguồn nào cũng biến phép so
        // sánh tập thành phép so sánh rỗng-với-rỗng, tức xanh vĩnh viễn.
        Assert.True(onDisk.Count > 0,
            $"Không tìm thấy *.csproj nào dưới '{RepoSourceTree.BackendRoot}' ⇒ test này không đo gì. " +
            "Xem thông điệp của RepoSourceTree.LocateBackendRoot: rất có thể thư mục output đã bị chép " +
            "ra ngoài cây repo.");
        Assert.True(inSolution.Count > 0,
            $"Không đọc được mục <Project Path=\"…\"/> nào trong '{RepoSourceTree.SolutionPath}' ⇒ test này " +
            "không đo gì. Nếu định dạng .slnx đã đổi (đổi tên phần tử/thuộc tính) thì sửa bộ đọc " +
            "ProjectsInSolution ở đây — ĐỪNG xoá test.");

        var missing = onDisk
            .Where(path => !inSolution.Contains(path))
            .Select(RepoSourceTree.Relative)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            "Project TỒN TẠI trên đĩa nhưng KHÔNG được khai trong PlatformManager.slnx: " +
            string.Join(", ", missing) + ". Hệ quả: `dotnet build`/`dotnet test` trên solution bỏ qua " +
            "nó TRONG IM LẶNG — nếu đó là project test thì toàn bộ test bên trong ngừng chạy mà gate vẫn " +
            "xanh (đã xảy ra thật với 2 project test / 59 test). Cách sửa: thêm " +
            "`<Project Path=\"…/Tên.csproj\" />` vào PlatformManager.slnx (đặt trong <Folder Name=\"/Tests/\"> " +
            "nếu là project test). Nếu project này CỐ Ý nằm ngoài solution thì xoá hẳn nó đi, hoặc thêm " +
            "allowlist tường minh trong SolutionProjectCoverageTests kèm lý do.");
    }

    [Fact(DisplayName = "Mọi mục Project trong PlatformManager.slnx đều trỏ tới file có thật")]
    public void EveryProjectInSolution_PointsTo_ExistingFile()
    {
        var inSolution = ProjectsInSolution();

        Assert.True(inSolution.Count > 0,
            $"Không đọc được mục <Project Path=\"…\"/> nào trong '{RepoSourceTree.SolutionPath}' ⇒ test này " +
            "không đo gì. Xem thông điệp của EveryProjectOnDisk_IsDeclared_InSolution.");

        var dangling = inSolution
            .Where(path => !File.Exists(path))
            .Select(RepoSourceTree.Relative)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(dangling.Count == 0,
            "PlatformManager.slnx khai project không tồn tại trên đĩa: " + string.Join(", ", dangling) +
            ". `dotnet build` trên solution sẽ đỏ với một thông điệp khó lần. Cách sửa: xoá mục " +
            "<Project Path=\"…\"/> tương ứng khỏi PlatformManager.slnx, hoặc khôi phục file nếu nó bị xoá " +
            "nhầm (đường di chuyển project thường quên bước cập nhật solution).");
    }

    /// <summary>
    /// Đối chứng — chứng minh phép so sánh tập biết nói KHÔNG. Không có ca này thì một lỗi trong
    /// khâu chuẩn hoá đường dẫn (ví dụ trả về chuỗi rỗng cho mọi mục) sẽ làm test chính xanh
    /// vĩnh viễn, và không ai biết.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bỏ một project khỏi tập solution thì phải bị báo là THIẾU")]
    public void Comparison_Detects_AProjectRemovedFromTheSolutionSet()
    {
        var onDisk = ProjectsOnDisk();
        var inSolution = ProjectsInSolution();

        Assert.True(onDisk.Count > 0 && inSolution.Count > 0,
            "Một trong hai nguồn rỗng ⇒ ca đối chứng này không chứng minh được gì. Xem thông điệp của " +
            "EveryProjectOnDisk_IsDeclared_InSolution.");

        // Nạn nhân phải là project ĐANG được khai — bỏ một mục vốn đã thiếu thì chẳng chứng minh
        // được gì. Đo theo ĐỘ LỆCH (before → after) chứ không theo con số tuyệt đối, để ca đối
        // chứng này vẫn nói đúng một điều duy nhất ngay cả khi repo đang có sẵn vi phạm thật —
        // nếu không, một vi phạm thật sẽ làm ĐỎ HAI test và thông điệp thứ hai đổ lỗi nhầm cho bộ dò.
        var victim = onDisk
            .Where(inSolution.Contains)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        Assert.True(victim is not null,
            "Không project nào trên đĩa đang được khai trong solution ⇒ không có gì để bỏ ra, và ca đối " +
            "chứng này không chứng minh được gì. Xem thông điệp của EveryProjectOnDisk_IsDeclared_InSolution.");

        var missingBefore = onDisk.Where(path => !inSolution.Contains(path)).ToList();

        var mutilated = new HashSet<string>(inSolution, StringComparer.OrdinalIgnoreCase);
        mutilated.Remove(victim!);
        var missingAfter = onDisk.Where(path => !mutilated.Contains(path)).ToList();

        Assert.True(
            missingAfter.Count == missingBefore.Count + 1
            && missingAfter.Contains(victim!, StringComparer.OrdinalIgnoreCase)
            && !missingBefore.Contains(victim!, StringComparer.OrdinalIgnoreCase),
            "Bỏ một project khỏi tập solution mà phép so sánh KHÔNG báo thêm đúng mục đó " +
            $"('{RepoSourceTree.Relative(victim!)}') ⇒ bộ so sánh đang hỏng (thường là do chuẩn hoá đường " +
            "dẫn hai bên không cùng dạng), và EveryProjectOnDisk_IsDeclared_InSolution đang xanh mà không " +
            "đo gì.");
    }

    // ── Hai nguồn độc lập ────────────────────────────────────────────────

    /// <summary>Nguồn KỲ VỌNG: hệ thống tệp.</summary>
    private static List<string> ProjectsOnDisk() => [.. RepoSourceTree.ProjectFiles()];

    /// <summary>
    /// Nguồn THỰC TẾ: văn bản <c>.slnx</c>. <c>Descendants</c> chứ không <c>Elements</c> vì
    /// project nằm lồng trong <c>&lt;Folder&gt;</c> (thư mục ảo của solution) — chỉ đọc cấp 1 sẽ
    /// bỏ sót đúng nhóm <c>/Tests/</c>, tức bỏ sót đúng ca đã gây sự cố.
    /// </summary>
    private static HashSet<string> ProjectsInSolution()
    {
        if (!File.Exists(RepoSourceTree.SolutionPath))
        {
            throw new FileNotFoundException(
                $"Không thấy '{RepoSourceTree.SolutionPath}'. Solution là nguồn thực tế của test này — " +
                "không có nó thì không kết luận được gì.", RepoSourceTree.SolutionPath);
        }

        var solution = XDocument.Load(RepoSourceTree.SolutionPath);

        // OrdinalIgnoreCase: đường dẫn viết trong .slnx giữ nguyên cách gõ tay của người sửa file,
        // còn đường dẫn từ đĩa giữ cách hệ thống tệp lưu. Trên Windows hai bên có thể lệch hoa
        // thường mà vẫn là cùng một file — so sánh phân biệt hoa thường sẽ báo THIẾU giả.
        return new HashSet<string>(
            solution.Descendants("Project")
                .Select(element => (string?)element.Attribute("Path"))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(Path.Combine(
                    RepoSourceTree.BackendRoot,
                    path!.Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar)))),
            StringComparer.OrdinalIgnoreCase);
    }
}
