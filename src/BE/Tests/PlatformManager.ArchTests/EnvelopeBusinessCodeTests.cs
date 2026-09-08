using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi envelope lỗi dựng bằng OBJECT INITIALIZER ngoài <c>ApiResult.cs</c> — tức có
/// gán <c>Status =</c> một cách tường minh — PHẢI gán cả <c>BusinessCode =</c>.
///
/// <para><b>Lỗi thật nó canh.</b> Đợt 2026-09-03 phải đi gắn <c>businessCode</c> cho 6 nhánh
/// envelope đã mọc lên thiếu nó (5 nhánh hạ tầng + nhánh model binding). Cả 6 mọc theo đúng một
/// cách: một nhánh mới trả JSON lỗi được viết tại chỗ, dựng <c>ApiResult</c> bằng tay, gán
/// <c>Status</c>/<c>Code</c>/<c>Message</c> — và không ai nhận ra thiếu <c>BusinessCode</c> vì
/// biên dịch sạch, response trông đúng, FE vẫn hiện được câu tiếng Việt. Không có gì ngăn nhánh
/// thứ 7 mọc lại y hệt. Hậu quả không lộ ra cho tới lúc i18n: <c>businessCode</c> là KHOÁ DỊCH
/// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a)), nên một nhánh thiếu nó là một nhánh
/// không bao giờ dịch được, và cũng không nhận diện được ở phía FE ngoài cách so chuỗi tiếng Việt.</para>
///
/// <para><b>Vì sao chỉ soi khối có <c>Status =</c>.</b> <c>ApiResultStatus.SUCCESS</c> là giá trị
/// 0, nên một initializer KHÔNG gán <c>Status</c> là envelope thành công — thứ cố ý không mang mã
/// nghiệp vụ. Bắt cả nhóm đó là đỏ oan, và đỏ oan là đường ngắn nhất tới việc luật bị nới.</para>
///
/// <para><b>Vì sao <c>ApiResult.cs</c> được miễn.</b> Đó là NƠI ĐẶT bốn factory
/// (<c>Success</c>/<c>BusinessError</c>/<c>ValidationError</c>/<c>SystemError</c>) — chúng dựng
/// envelope bằng <c>new()</c> theo định nghĩa, và <c>Success</c> thì đúng ra không có
/// <c>BusinessCode</c>. Miễn trừ theo TÊN FILE, cùng khuôn (và cùng giới hạn đã biết) với phép
/// miễn catalog <c>*Errors.cs</c> ở <see cref="ErrorCodeSourceTests"/>.</para>
///
/// <para><b>Luật này KHÔNG thay thế việc dùng factory.</b> Đường đúng vẫn là gọi
/// <c>ApiResult&lt;T&gt;.BusinessError(...)</c> — nó nhận <c>ErrorDescriptor</c> nên mã và câu đi
/// cùng nhau từ catalog. Luật ở đây chỉ chặn hậu quả TỆ NHẤT của việc không dùng factory, vì có
/// những chỗ buộc phải dựng tay (nhánh <c>OnRejected</c> của rate limiter cần gán thêm
/// <c>Retryable</c>/<c>TraceId</c> trước khi ghi thẳng ra response).</para>
///
/// <para><b>Đánh đổi:</b> phân tích VĂN BẢN, không phải Roslyn — cùng bản chất và cùng giới hạn đã
/// ghi ở <c>RepoSourceTree</c>. Nó không lần được một envelope dựng ở hàm khác rồi mới gán
/// <c>BusinessCode</c> sau. Đổi lại: chạy trong mili-giây, không cần dựng container.</para>
/// </summary>
public class EnvelopeBusinessCodeTests
{
    /// <summary>
    /// Hai khuôn mở đầu một object initializer của <c>ApiResult&lt;T&gt;</c>, bắt tới ĐÚNG dấu
    /// <c>{</c> mở khối (vị trí đó là đầu vào của bộ đếm ngoặc ở <see cref="BlockAt"/>):
    /// <list type="number">
    ///   <item>dạng nêu tên kiểu — <c>new ApiResult&lt;object&gt; {</c> (và biến thể có <c>()</c>);</item>
    ///   <item>dạng target-typed — <c>ApiResult&lt;object&gt; result = new() {</c>.</item>
    /// </list>
    /// Phải có cả hai vì lý do đã trả giá ở <see cref="ErrorCodeSourceTests"/>: bắt mỗi dạng thứ
    /// nhất thì dạng thứ hai lách qua, mà dạng thứ hai chính là khuôn quen tay khi chép dán.
    /// </summary>
    private static readonly Regex InitializerStart = new(
        @"new\s+ApiResult\s*<[^<>]*>\s*(?:\(\s*\))?\s*\{" +
        @"|ApiResult\s*<[^<>]*>\s+\w+\s*=\s*new\s*(?:\(\s*\))?\s*\{",
        RegexOptions.Compiled);

    private static readonly Regex AssignsStatus = new(@"\bStatus\s*=", RegexOptions.Compiled);
    private static readonly Regex AssignsBusinessCode = new(@"\bBusinessCode\s*=", RegexOptions.Compiled);

    [Fact(DisplayName = "Envelope dựng tay có Status = thì phải có BusinessCode = (ngoài ApiResult.cs)")]
    public void HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode()
    {
        var files = RepoSourceTree.ProductSourceFiles().Where(path => !IsEnvelopeFactoryFile(path)).ToList();

        Assert.True(files.Count > 0,
            "Không còn file .cs sản phẩm nào ngoài ApiResult.cs để quét ⇒ test này không đo gì. Nếu phép " +
            "lọc IsEnvelopeFactoryFile đã nuốt sạch danh sách thì bộ dò đang hỏng, ĐỪNG nới luật.");

        var violations = new List<string>();

        foreach (var path in files)
        {
            var original = File.ReadAllText(path);

            foreach (var line in ViolationLines(original))
                violations.Add($"{RepoSourceTree.Relative(path)}:{line}");
        }

        violations.Sort(StringComparer.Ordinal);

        Assert.True(violations.Count == 0,
            "Envelope lỗi dựng bằng object initializer nhưng KHÔNG gán BusinessCode: " +
            string.Join("; ", violations) + ". Cách sửa ưu tiên: gọi ApiResult<T>.BusinessError/" +
            "ValidationError/SystemError với một ErrorDescriptor lấy từ catalog {Entity}Errors.cs. Nếu buộc " +
            "phải dựng tay (cần gán thêm Retryable/TraceId trước khi ghi thẳng ra response), vẫn phải gán " +
            "BusinessCode = <Catalog>.<Mã>.BusinessCode. Vì sao: businessCode là KHOÁ DỊCH của i18n " +
            "(doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a)) và là thứ DUY NHẤT FE bind lỗi được mà " +
            "không phải so chuỗi tiếng Việt.");
    }

    /// <summary>
    /// Đối chứng — trên một repo đang sạch, nhánh "có vi phạm" của test trên không bao giờ chạy,
    /// nên nó hỏng được mà không ai biết. Ca này chạy CHÍNH <see cref="ViolationLines"/> mà test
    /// thật chạy, trên các mẫu dựng tay.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò bắt cả 2 khuôn dựng, bỏ qua envelope đúng, chú thích và khối lồng")]
    public void Detector_Catches_TheKnownViolationShapes()
    {
        const string missing = """
            var result = new ApiResult<object>
            {
                Status = ApiResultStatus.BUSINESS_ERROR,
                Code = ErrorCode.TooManyRequests,
                Message = "Quá nhiều yêu cầu.",
            };
            """;
        Assert.True(ViolationLines(missing).Count == 1,
            "Bộ dò KHÔNG bắt được khuôn vi phạm kinh điển (Status có, BusinessCode không) ⇒ luật này đang " +
            "xanh mà không đo gì, và nhánh envelope thứ 7 sẽ mọc lại y như 6 nhánh đợt 2026-09-03.");

        const string targetTyped = """
            ApiResult<object> result = new()
            {
                Status = ApiResultStatus.SYSTEM_ERROR,
                Message = "Lỗi hệ thống.",
            };
            """;
        Assert.True(ViolationLines(targetTyped).Count == 1,
            "Bộ dò bỏ sót dạng target-typed `= new() {` ⇒ lách qua bằng đúng khuôn quen tay nhất khi chép dán.");

        const string compliant = """
            var result = new ApiResult<object>
            {
                Status = ApiResultStatus.BUSINESS_ERROR,
                BusinessCode = RateLimitErrors.TooManyRequests.BusinessCode,
                Message = RateLimitErrors.TooManyRequests.MessageTemplate,
            };
            """;
        Assert.Empty(ViolationLines(compliant));

        // Envelope THÀNH CÔNG cố ý không mang mã nghiệp vụ. Không gán Status ⇒ SUCCESS (giá trị 0
        // của enum), nên khối này nằm ngoài phạm vi luật — bắt nó là đỏ oan.
        Assert.Empty(ViolationLines("""
            var result = new ApiResult<string> { Data = "xong" };
            """));

        // Khối LỒNG: nếu bộ đếm ngoặc dừng ở dấu `}` đầu tiên thì nó cắt mất phần đuôi và báo vi
        // phạm cho một envelope hợp lệ — đúng loại đỏ oan khiến người ta tắt test đi.
        Assert.Empty(ViolationLines("""
            var result = new ApiResult<object>
            {
                Status = ApiResultStatus.VALIDATION_ERROR,
                Fields = new Dictionary<string, string[]> { ["ten"] = ["Bắt buộc."] },
                BusinessCode = ValidationErrors.Invalid.BusinessCode,
            };
            """));

        Assert.Empty(ViolationLines("""
            // var result = new ApiResult<object> { Status = ApiResultStatus.SYSTEM_ERROR };
            """));
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>ApiResult.cs</c> — nơi ĐẶT các factory, miễn trừ theo tên file. Khai riêng thành hàm để
    /// thông điệp lỗi và docstring trỏ về đúng một chỗ.
    /// </summary>
    private static bool IsEnvelopeFactoryFile(string path) =>
        string.Equals(Path.GetFileName(path), "ApiResult.cs", StringComparison.Ordinal);

    /// <summary>
    /// Số dòng (tính trên văn bản GỐC) của mỗi khối vi phạm. Chú thích bị cắt bằng
    /// <c>RepoSourceTree.RemoveComments</c> — bộ cắt đó thay chú thích bằng dấu cách CÙNG ĐỘ DÀI
    /// nên chỉ số ký tự của hai bản trùng nhau, và số dòng vẫn đúng.
    /// </summary>
    private static List<int> ViolationLines(string original)
    {
        var code = RepoSourceTree.RemoveComments(original);
        var lines = new List<int>();

        foreach (Match match in InitializerStart.Matches(code))
        {
            var block = BlockAt(code, match.Index + match.Length - 1);

            if (AssignsStatus.IsMatch(block) && !AssignsBusinessCode.IsMatch(block))
                lines.Add(LineOf(original, match.Index));
        }

        return lines;
    }

    /// <summary>
    /// Văn bản từ dấu <c>{</c> tại <paramref name="openBraceIndex"/> tới dấu <c>}</c> ĐÓNG NÓ, đếm
    /// ngoặc lồng. Phải đếm chứ không được lấy tới <c>}</c> đầu tiên: initializer của envelope
    /// validate có <c>Fields = new() { … }</c> lồng bên trong, và cắt sớm sẽ nuốt mất phần khai
    /// <c>BusinessCode</c> đứng sau ⇒ báo vi phạm cho code đúng.
    ///
    /// <para>Không thấy dấu đóng (mã đang dở, hoặc bộ cắt chú thích lệch) thì trả phần còn lại của
    /// file — thà quét thừa còn hơn im lặng bỏ qua một khối.</para>
    /// </summary>
    private static string BlockAt(string code, int openBraceIndex)
    {
        var depth = 0;

        for (var i = openBraceIndex; i < code.Length; i++)
        {
            if (code[i] == '{')
                depth++;
            else if (code[i] == '}' && --depth == 0)
                return code[openBraceIndex..(i + 1)];
        }

        return code[openBraceIndex..];
    }

    private static int LineOf(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;
}
