using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi chuỗi có thể ra tới field <c>businessCode</c> của envelope đều phải xuất phát
/// từ một CATALOG đã khai — không được gõ thẳng tại chỗ dùng. Hai đường lách được canh ở đây:
/// <list type="number">
///   <item><b>T1</b> — dựng <c>DomainException</c>/<c>ConflictException</c> bằng chuỗi literal.</item>
///   <item><b>T2</b> — dựng <c>ErrorDescriptor</c> bằng chuỗi literal ở file KHÔNG phải catalog.</item>
/// </list>
///
/// <para><b>Vì sao cần quét MÃ NGUỒN dù đã có <see cref="ErrorCatalogTests"/>:</b> test kia đọc
/// GIÁ TRỊ qua reflection và chỉ thấy field <c>public static readonly</c>. Một mã gõ thẳng vào
/// thân hàm không phải là field nào cả, nên nó vô hình với reflection — và XML doc của test đó
/// từng tự thừa nhận đúng lỗ này. Hệ quả thật đã đo ngày 2026-09-03: 6 mã domain lệch khuôn
/// (<c>SYS_MENU_CODE_REQUIRED</c>, không dấu chấm) sống song song với hệ <c>MIEN.MA_LOI</c> trong
/// CÙNG một field, trong khi <see cref="ErrorCatalogTests"/> vẫn xanh và ca đối chứng của nó còn
/// liệt đúng khuôn đó là "phải từ chối".</para>
///
/// <para><b>T1 không thừa dù trình biên dịch đã chặn.</b> Sau 2026-09-03 hai exception chỉ nhận
/// <c>DomainError</c>, nên một chuỗi ở tham số đầu là lỗi biên dịch. Luật này canh đường QUAY LẠI:
/// ai đó thêm một overload <c>(string, string)</c> "cho tương thích" thì compiler im lặng ngay, và
/// không còn gì chặn nữa. Test rẻ, đường tái phát thì có thật — repo này đã trả giá đúng một lần
/// cho việc để mã tự do đi thẳng ra envelope.</para>
///
/// <para><b>Đánh đổi và giới hạn đã biết</b> (cùng bản chất với ghi chú ở <c>RepoSourceTree</c>):
/// đây là phân tích VĂN BẢN, không phải Roslyn. Nó không hiểu <c>using alias</c>, không lần được
/// một hằng số chuỗi khai nơi khác rồi truyền vào, và miễn trừ catalog bằng TÊN FILE
/// (<c>*Errors.cs</c>) chứ không bằng cấu trúc — đặt tên file như vậy rồi viết gì trong đó cũng
/// qua được T2. Đổi lại: chạy trong mili-giây, thấy được cả nhánh điều kiện, không cần dựng
/// container. Khi refactor làm test đỏ oan thì sửa bộ dò ở đây, ĐỪNG nới luật cho xanh.</para>
///
/// <para><b>Phạm vi quét</b> là <c>RepoSourceTree.ProductSourceFiles()</c> — hôm nay là Core + Api,
/// đúng tập project sản phẩm đang tồn tại. Khi tầng <c>Business.*</c> ra đời, nó phải được thêm
/// vào chính hàm đó (một chỗ, dùng chung cho mọi test quét nguồn); đừng chép một danh sách thư mục
/// thứ hai vào file này.</para>
/// </summary>
public class ErrorCodeSourceTests
{
    /// <summary>
    /// <c>new DomainException("…</c> / <c>new ConflictException("…</c> — chỉ bắt chuỗi ở THAM SỐ
    /// ĐẦU, vì đó là vị trí của mã lỗi. Tham số sau là đối số điền vào khuôn thông điệp, được phép
    /// là literal.
    /// </summary>
    private static readonly Regex DomainExceptionLiteral =
        new(@"new\s+(?:Domain|Conflict)Exception\s*\(\s*""", RegexOptions.Compiled);

    /// <summary>
    /// Hai khuôn dựng <c>ErrorDescriptor</c> với mã literal: dạng nêu tên kiểu
    /// (<c>new ErrorDescriptor("…</c>) và dạng target-typed (<c>ErrorDescriptor x = new("…</c>).
    /// Phải có cả hai — bắt mỗi dạng thứ nhất thì dạng thứ hai lách qua, mà chính các catalog hiện
    /// có đều viết theo dạng thứ hai nên đó là khuôn quen tay nhất.
    ///
    /// <para><b>Nới 2026-09-03 sau khi một lượt kiểm độc lập chứng minh bộ dò CHẾT.</b> Bản trước
    /// chỉ nêu tên <c>ErrorDescriptor</c>, nên <c>new DomainException(new DomainError("SYS_MENU_CODE_REQUIRED", …))</c>
    /// viết thẳng tại chỗ ném đi lọt cả ba luật cùng lúc: T1 đòi dấu nháy ngay sau <c>(</c> (ở đây
    /// là <c>new</c>), T2 không biết kiểu <c>DomainError</c>, còn <c>ErrorCatalogTests</c> chỉ soi
    /// field nên không thấy thứ dựng trong thân hàm. Tái hiện thật: đưa lại đúng mã mà đợt dọn vừa
    /// gỡ ⇒ biên dịch sạch, 49/49 vẫn xanh. Thêm <c>=>?</c> cho khuôn property
    /// (<c>public static DomainError X => new("…</c>) vì nó cũng lọt cùng cách.</para>
    /// </summary>
    private static readonly Regex ErrorDescriptorLiteral =
        new(@"new\s+(?:ErrorDescriptor|DomainError)\s*\(\s*""|(?:ErrorDescriptor|DomainError)\s+\w+\s*=>?\s*new\s*\(\s*""", RegexOptions.Compiled);

    [Fact(DisplayName = "T1: không dựng DomainException/ConflictException bằng chuỗi mã literal")]
    public void DomainException_IsNever_BuiltFrom_AStringLiteral()
    {
        var files = RepoSourceTree.ProductSourceFiles();

        Assert.True(files.Count > 0,
            "Không quét được file .cs sản phẩm nào ⇒ test này không đo gì. Xem thông điệp của " +
            "RepoSourceTree.LocateBackendRoot: bộ dò chỉ chạy đúng khi thư mục output nằm trong cây repo.");

        var violations = ViolationsIn(files, DomainExceptionLiteral);

        Assert.True(violations.Count == 0,
            "Có chỗ dựng DomainException/ConflictException bằng chuỗi mã literal: " +
            string.Join("; ", violations) + ". Mã lỗi phải khai trong catalog {Entity}Errors.cs dưới dạng " +
            "DomainError rồi truyền vào (ví dụ `throw new DomainException(SysMenuErrors.CodeRequired)`). " +
            "Vì sao: chuỗi gõ tại chỗ ném đi THẲNG ra field businessCode của envelope mà không qua catalog " +
            "nào, nên ErrorCatalogTests không kiểm được khuôn lẫn tính duy nhất của nó — và businessCode sẽ " +
            "là KHOÁ DỊCH khi i18n vào (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a)).");
    }

    [Fact(DisplayName = "T2: không dựng ErrorDescriptor bằng chuỗi mã literal ngoài file catalog")]
    public void ErrorDescriptor_IsNever_BuiltFrom_AStringLiteral_OutsideACatalog()
    {
        var files = RepoSourceTree.ProductSourceFiles().Where(path => !IsCatalogFile(path)).ToList();

        Assert.True(files.Count > 0,
            "Không còn file .cs sản phẩm nào ngoài catalog để quét ⇒ test này không đo gì. Nếu phép lọc " +
            "IsCatalogFile đã nuốt sạch danh sách thì bộ dò đang hỏng, ĐỪNG nới luật.");

        var violations = ViolationsIn(files, ErrorDescriptorLiteral);

        Assert.True(violations.Count == 0,
            "Có chỗ dựng ErrorDescriptor bằng chuỗi mã literal ngoài catalog: " +
            string.Join("; ", violations) + ". Chuyển mã đó vào một file {Entity}Errors.cs dưới dạng " +
            "`public static readonly ErrorDescriptor` rồi tham chiếu tới (doc/huong_dan/quy-uoc/be-cqrs-handler.md " +
            "§ErrorDescriptor). Descriptor dựng trong thân hàm KHÔNG hiện ra với reflection, nên " +
            "ErrorCatalogTests sẽ xanh trong khi một mã lệch khuôn hoặc trùng mã khác vẫn ra tới FE.");
    }

    /// <summary>
    /// Đối chứng T1 — chứng minh bộ dò biết nói CÓ với vi phạm, nói KHÔNG với code đúng, và không
    /// bị chú thích đánh lừa. Trên một repo đang sạch, nhánh "có vi phạm" không bao giờ chạy, nên
    /// nó hỏng được mà không ai biết: một test luôn xanh không phân biệt được với một test chết.
    /// </summary>
    [Fact(DisplayName = "Đối chứng T1: bộ dò bắt được chuỗi literal, bỏ qua catalog và chú thích")]
    public void DomainExceptionDetector_Catches_TheKnownViolation()
    {
        Assert.True(DetectsIn("throw new DomainException(\"SYS_MENU.CODE_REQUIRED\", \"Mã menu trống.\");", DomainExceptionLiteral),
            "Bộ dò KHÔNG bắt được khuôn vi phạm kinh điển (chuỗi mã ở tham số đầu) ⇒ T1 đang xanh mà không đo gì.");

        Assert.True(DetectsIn("throw new ConflictException(\"SYS_MENU.DUPLICATE_CODE\", \"Trùng mã.\");", DomainExceptionLiteral),
            "Bộ dò bắt DomainException nhưng bỏ sót ConflictException ⇒ nửa còn lại của cùng một lỗ vẫn mở.");

        Assert.False(DetectsIn("throw new DomainException(SysMenuErrors.CodeRequired);", DomainExceptionLiteral),
            "Bộ dò báo vi phạm cho code ĐÚNG khuôn ⇒ nó quá chặt và sẽ đỏ oan, buộc người sau nới luật.");

        Assert.False(DetectsIn("throw new DomainException(SysMenuErrors.NameRequired, \"tham so\");", DomainExceptionLiteral),
            "Bộ dò nhầm ĐỐI SỐ của khuôn thông điệp thành mã lỗi ⇒ nó cấm cả việc truyền tham số hợp lệ.");

        Assert.False(DetectsIn("// throw new DomainException(\"SYS_MENU.CODE_REQUIRED\", \"x\");", DomainExceptionLiteral),
            "Bộ dò tính cả CHÚ THÍCH là vi phạm ⇒ repo này chú thích rất dày và nhắc thẳng khuôn sai trong " +
            "văn xuôi, nên nó sẽ đỏ ở những chỗ chỉ NÓI VỀ vi phạm. Phép cắt chú thích dùng chung là " +
            "RepoSourceTree.RemoveComments.");
    }

    /// <summary>Đối chứng T2 — cùng lý do như đối chứng T1.</summary>
    [Fact(DisplayName = "Đối chứng T2: bộ dò bắt cả dạng nêu tên kiểu lẫn dạng target-typed")]
    public void ErrorDescriptorDetector_Catches_BothConstructionShapes()
    {
        Assert.True(DetectsIn("var e = new ErrorDescriptor(\"USER.NOT_FOUND\", ErrorCode.NotFound, \"x\");", ErrorDescriptorLiteral),
            "Bộ dò KHÔNG bắt dạng `new ErrorDescriptor(\"…` ⇒ T2 đang xanh mà không đo gì.");

        Assert.True(DetectsIn("ErrorDescriptor e = new(\"USER.NOT_FOUND\", ErrorCode.NotFound, \"x\");", ErrorDescriptorLiteral),
            "Bộ dò KHÔNG bắt dạng target-typed `= new(\"…` ⇒ lách qua T2 bằng đúng khuôn mà mọi catalog " +
            "hiện có đang viết, tức khuôn quen tay nhất khi chép dán.");

        Assert.False(DetectsIn("var e = new ErrorDescriptor(error.BusinessCode, errorCode, error.MessageTemplate);", ErrorDescriptorLiteral),
            "Bộ dò báo vi phạm cho ExceptionHandlingBehavior — chỗ ghép mã ĐÃ CÓ từ catalog với mã HTTP do " +
            "loại exception quyết định. Chỗ đó không đặt ra mã mới; cấm nó là cấm nhầm.");

        Assert.False(DetectsIn("/* var e = new ErrorDescriptor(\"USER.NOT_FOUND\", ErrorCode.NotFound, \"x\"); */", ErrorDescriptorLiteral),
            "Bộ dò tính cả chú thích khối là vi phạm ⇒ xem lý do ở đối chứng T1.");
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// File catalog nhận diện bằng ĐUÔI TÊN <c>Errors.cs</c> — đúng quy ước
    /// <c>{Entity}Errors.cs</c> của doc/huong_dan/quy-uoc/be-cqrs-handler.md §ErrorDescriptor.
    /// </summary>
    private static bool IsCatalogFile(string path) =>
        Path.GetFileName(path).EndsWith("Errors.cs", StringComparison.Ordinal);

    /// <summary>
    /// Vi phạm dạng <c>đường/dẫn.cs:dòng</c>. Số dòng tính trên văn bản GỐC chứ không trên bản đã
    /// cắt chú thích — làm được vì <c>RemoveComments</c> thay chú thích bằng dấu cách CÙNG ĐỘ DÀI,
    /// nên chỉ số ký tự của hai bản trùng nhau. Không có tính chất đó thì một chú thích khối nhiều
    /// dòng sẽ làm mọi số dòng phía sau lệch, và thông điệp lỗi chỉ sai chỗ một cách khó chịu.
    /// </summary>
    private static List<string> ViolationsIn(IEnumerable<string> files, Regex pattern)
    {
        var found = new List<string>();

        foreach (var path in files)
        {
            var original = File.ReadAllText(path);

            foreach (Match match in pattern.Matches(RepoSourceTree.RemoveComments(original)))
                found.Add($"{RepoSourceTree.Relative(path)}:{LineOf(original, match.Index)}");
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static int LineOf(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;

    /// <summary>
    /// Dùng bởi các ca đối chứng: chạy CHÍNH bộ cắt chú thích mà bộ dò thật dùng
    /// (<c>RepoSourceTree.RemoveComments</c>). Đối chứng chạy trên một bộ cắt khác thì nó không
    /// chứng minh được gì về bộ đang dùng thật — lý do đã ghi tại chính hàm đó.
    /// </summary>
    private static bool DetectsIn(string code, Regex pattern) =>
        pattern.IsMatch(RepoSourceTree.RemoveComments(code));
}
