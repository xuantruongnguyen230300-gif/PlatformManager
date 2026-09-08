using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi class <c>*Middleware</c> trong <c>PlatformManager.Api/Common</c> phải được nối
/// vào pipeline ở <c>Program.cs</c>.
///
/// <para><b>Lỗi thật nó bắt:</b> viết xong một middleware rồi quên nối. Không có lỗi biên dịch,
/// không có cảnh báo, không test nào đỏ — middleware chỉ đơn giản KHÔNG BAO GIỜ CHẠY. Với 3
/// middleware hiện có, hậu quả của việc "quên nối" đều là hỏng âm thầm và đều thuộc loại nguy
/// hiểm: log mất traceId (không tra được sự cố),
/// <c>LoginUserNameRateLimitMiddleware</c> không đọc tên đăng nhập (hàng rào brute-force thứ hai
/// phân vùng sai), <c>OriginValidationMiddleware</c> không kiểm Origin (mất hẳn lớp CSRF thứ
/// nhất).</para>
///
/// <para><b>Vì sao đây là loại luật ArchTest cũ không có.</b> 13 ArchTest trước 2026-09-01 đều
/// kiểm HÌNH DẠNG (ranh giới module, hướng phụ thuộc, đóng gói entity, thứ tự pipeline, bộ lọc
/// xoá mềm). Không cái nào kiểm SỰ ĐƯỢC NỐI VÀO — nên chiều "code có thành phần Y, ai cho phép Y
/// tồn tại / Y có được dùng không" hoàn toàn mù.</para>
///
/// <para><b>Đánh đổi:</b> phân tích tĩnh văn bản, xem
/// <see cref="RepoSourceTree"/> để biết vì sao không dựng container. Tóm tắt: pipeline sau khi
/// build là chuỗi delegate đóng, không liệt kê được middleware đã nối.</para>
/// </summary>
public class MiddlewareWiringTests
{
    /// <summary>
    /// Namespace bị ràng buộc. Middleware sống ở nơi khác (module nghiệp vụ sau này) thì thêm
    /// namespace vào đây — test tự phủ.
    /// </summary>
    private static readonly string[] MiddlewareNamespaces = ["PlatformManager.Api.Common"];

    [Fact(DisplayName = "Mọi *Middleware trong Api/Common đều được nối vào pipeline ở Program.cs")]
    public void EveryMiddlewareClass_IsWiredInto_ProgramPipeline()
    {
        var middlewares = DeclaredMiddlewareTypes();

        // Chặn "pass rỗng" ở chính nguồn kỳ vọng: reflection trả rỗng thì mọi assert dưới vô nghĩa.
        // Assembly Api KHÔNG được reference tường minh ở đâu trong code test này ngoài
        // ProductAssemblies — dòng dưới là thứ duy nhất chứng minh nó thật sự nạp được.
        Assert.True(middlewares.Count > 0,
            "Không tìm thấy class *Middleware nào trong " + string.Join(", ", MiddlewareNamespaces) +
            " ⇒ test này không đo gì. Nguyên nhân thường gặp: assembly PlatformManager.Api không nạp được " +
            "(kiểm ProductAssemblies.All), hoặc middleware đã chuyển sang namespace khác — khi đó thêm " +
            "namespace mới vào MiddlewareNamespaces.");

        var program = RepoSourceTree.ReadCodeWithoutComments(RepoSourceTree.ProgramCsPath);
        Assert.False(string.IsNullOrWhiteSpace(program),
            $"Đọc được Program.cs rỗng tại '{RepoSourceTree.ProgramCsPath}' — bộ dò không có gì để soi.");

        var notWired = middlewares
            .Where(type => !IsWiredIntoPipeline(type.Name, program))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(notWired.Count == 0,
            "Middleware TỒN TẠI nhưng KHÔNG được nối vào pipeline ⇒ nó không bao giờ chạy, và không " +
            "có gì báo: " + string.Join(", ", notWired) + ". Cách sửa: thêm `app.UseXxx();` vào phần " +
            "Pipeline của PlatformManager.Api/Program.cs (đúng vị trí thứ tự — xem chú thích quanh đó), " +
            "trong đó UseXxx là extension method `public static IApplicationBuilder UseXxx(this " +
            "IApplicationBuilder app) => app.UseMiddleware<Tên>();` khai cạnh chính class middleware. " +
            "Nếu middleware này CỐ Ý chưa dùng thì xoá nó đi — code chết không được ở lại giả làm hàng rào.");
    }

    /// <summary>
    /// Đối chứng — chứng minh bộ dò biết nói KHÔNG, chứ không phải trả <c>true</c> cho mọi thứ.
    /// Không có ca này thì test trên có thể xanh vĩnh viễn vì một lỗi trong
    /// <see cref="IsWiredIntoPipeline"/>, và không ai biết.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò phải trả FALSE cho một middleware không hề tồn tại")]
    public void WiringDetector_Reports_NotWired_For_NonExistentMiddleware()
    {
        var program = RepoSourceTree.ReadCodeWithoutComments(RepoSourceTree.ProgramCsPath);

        Assert.False(IsWiredIntoPipeline("NeverWiredProbeMiddleware", program),
            "Bộ dò báo 'đã nối' cho một middleware không tồn tại ⇒ nó đang trả true vô điều kiện, " +
            "và test EveryMiddlewareClass_IsWiredInto_ProgramPipeline đang xanh mà không đo gì.");
    }

    /// <summary>
    /// Đối chứng thứ hai — chứng minh nhánh GIÁN TIẾP (extension method) thật sự phân giải được.
    /// Khuôn của repo là <c>app.UseTraceIdLogEnrichment()</c>, KHÔNG phải
    /// <c>app.UseMiddleware&lt;TraceIdLogEnrichmentMiddleware&gt;()</c>; nếu bộ phân giải gián tiếp
    /// hỏng, test chính sẽ ĐỎ (chứ không xanh oan) — nhưng thông điệp của nó sẽ đổ lỗi cho code sản
    /// phẩm. Ca này tách nguyên nhân ra: nó đỏ ⇒ lỗi ở bộ dò, không phải ở Program.cs.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò phân giải được đường gián tiếp qua extension method")]
    public void WiringDetector_Resolves_Indirect_ExtensionMethods()
    {
        var middlewares = DeclaredMiddlewareTypes();
        Assert.True(middlewares.Count > 0,
            "Không tìm thấy class *Middleware nào — trên một tập rỗng, ca đối chứng này không chứng minh " +
            "được gì. Xem thông điệp của EveryMiddlewareClass_IsWiredInto_ProgramPipeline.");

        var resolvedIndirectly = middlewares
            .Where(type => ExtensionMethodsInvoking(type.Name).Count > 0)
            .Select(type => type.Name)
            .ToList();

        Assert.True(resolvedIndirectly.Count > 0,
            "Không middleware nào phân giải được ra extension method `UseXxx() => app.UseMiddleware<...>()`. " +
            "Hoặc repo đã đổi khuôn nối pipeline (thì sửa ExtensionMethodsInvoking cho khớp), hoặc bộ dò " +
            "đang hỏng — trong cả hai trường hợp, ĐỪNG nới luật cho xanh.");
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Nguồn kỳ vọng: REFLECTION trên assembly Api (độc lập hoàn toàn với văn bản Program.cs).
    /// Loại static class để <c>*MiddlewareExtensions</c> không lọt vào — chúng kết thúc bằng
    /// "Extensions" nên vốn đã không khớp, nhưng lọc thêm cho chắc.
    /// </summary>
    private static List<Type> DeclaredMiddlewareTypes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .Where(type => type.IsClass
                        && !type.IsAbstract
                        && type.Namespace is not null
                        && MiddlewareNamespaces.Contains(type.Namespace)
                        && type.Name.EndsWith("Middleware", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];

    private static bool IsWiredIntoPipeline(string middlewareTypeName, string programCode)
    {
        // Đường TRỰC TIẾP: app.UseMiddleware<Tên>() ngay trong Program.cs.
        if (programCode.Contains($"UseMiddleware<{middlewareTypeName}>", StringComparison.Ordinal))
            return true;

        // Đường GIÁN TIẾP (khuôn repo đang dùng): Program.cs gọi extension method, extension
        // method mới gọi UseMiddleware<Tên>.
        return ExtensionMethodsInvoking(middlewareTypeName)
            .Any(method => Regex.IsMatch(programCode, $@"\.\s*{Regex.Escape(method)}\s*\("));
    }

    /// <summary>
    /// Tìm tên các extension method trên <c>IApplicationBuilder</c> có gọi
    /// <c>UseMiddleware&lt;middlewareTypeName&gt;</c>.
    ///
    /// <para>Cách làm: định vị lời gọi <c>UseMiddleware&lt;T&gt;</c> trong từng file nguồn của Api,
    /// rồi lùi về khai báo <c>… IApplicationBuilder Tên(this IApplicationBuilder …)</c> GẦN NHẤT ở
    /// phía trước. Đây là phép xấp xỉ "phương thức bao quanh" — đủ đúng cho khuôn một-dòng
    /// (<c>=> app.UseMiddleware&lt;T&gt;();</c>) mà cả 3 middleware đang dùng, và cố ý KHÔNG cố
    /// phân tích khối lệnh lồng nhau: làm thế là viết lại parser C#, và một parser tự chế sai âm
    /// thầm còn tệ hơn không có test.</para>
    /// </summary>
    private static List<string> ExtensionMethodsInvoking(string middlewareTypeName)
    {
        var names = new List<string>();
        var callPattern = new Regex(
            $@"UseMiddleware\s*<\s*{Regex.Escape(middlewareTypeName)}\s*>", RegexOptions.Compiled);

        foreach (var file in RepoSourceTree.ApiSourceFiles())
        {
            var code = RepoSourceTree.ReadCodeWithoutComments(file);

            foreach (Match call in callPattern.Matches(code))
            {
                var enclosing = ExtensionMethodDeclarations
                    .Matches(code[..call.Index])
                    .LastOrDefault();

                if (enclosing is not null)
                    names.Add(enclosing.Groups["name"].Value);
            }
        }

        return [.. names.Distinct(StringComparer.Ordinal)];
    }

    private static readonly Regex ExtensionMethodDeclarations = new(
        @"IApplicationBuilder\s+(?<name>\w+)\s*\(\s*this\s+IApplicationBuilder",
        RegexOptions.Compiled);
}
