using System.Reflection;
using System.Text.RegularExpressions;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> (1) mọi <c>BusinessCode</c> theo đúng định dạng <c>MIEN.MA_LOI</c> chữ hoa;
/// (2) mọi <c>BusinessCode</c> là DUY NHẤT trên toàn hệ thống; (3) mọi giá trị
/// <see cref="ErrorCode"/> khác <c>Success</c> là một mã HTTP hợp lệ (400–599).
///
/// <para><b>Nền của luật</b> (doc/huong_dan/wiki-core/be/01-core-components.md:180):
/// <i>"Mã lỗi từ nay là hợp đồng công khai với FE. Đổi tên một mã đang dùng sẽ làm FE mất bản
/// dịch mà không có lỗi build ở cả hai phía."</i> Câu cuối là toàn bộ vấn đề: BE gõ chuỗi, FE tra
/// chuỗi, và chuỗi thì không có kiểu. Không phía nào biên dịch đỏ khi hai bên lệch nhau — người
/// dùng chỉ thấy một thông báo lỗi tiếng Anh lạ, hoặc một ô trống.</para>
///
/// <para><b>Vì sao ĐỊNH DẠNG đáng canh:</b> FE phân nhánh theo tiền tố miền (<c>AUTH.*</c>,
/// <c>USER.*</c>) và tra bản dịch theo mã đầy đủ. Một mã lệch khuôn (<c>userNotFound</c>,
/// <c>USER_NOT_FOUND</c> thiếu dấu chấm, <c>User.NotFound</c>) không sai về mặt biên dịch, chỉ
/// đơn giản không khớp bất kỳ nhánh nào ở phía kia. Định dạng là thứ duy nhất trong hợp đồng này
/// mà máy kiểm được.</para>
///
/// <para><b>Vì sao TRÙNG LẶP là loại tệ nhất:</b> hai catalog cùng khai
/// <c>"USER.VERSION_CONFLICT"</c> — cả hai đều biên dịch, cả hai đều chạy, nhưng FE chỉ có MỘT
/// bản dịch và MỘT nhánh xử lý. Người dùng ở luồng thứ hai nhận đúng câu hướng dẫn của luồng thứ
/// nhất, và vì câu đó nghe hợp lý nên không ai báo lỗi. Repo hôm nay đã có hai
/// <c>VersionConflict</c> ở hai catalog khác nhau (<c>PermissionErrors</c>, <c>UserErrors</c>) —
/// khác tiền tố nên hợp lệ, nhưng khoảng cách tới va chạm chỉ là một lần chép dán.</para>
///
/// <para><b>Vì sao ErrorCode phải là mã HTTP:</b> <see cref="ErrorCode"/> cố ý không có bảng map
/// thứ hai — giá trị enum CHÍNH LÀ status code, và <c>ApiControllerBase.HandleResult</c> ép thẳng
/// <c>(int)result.Code</c> vào <c>StatusCode(...)</c>. Thêm một member kiểu <c>= 4013</c> (gõ
/// nhầm, hoặc "mã nội bộ cho tiện") sẽ khiến ASP.NET Core trả một status vô nghĩa hoặc 500. Đây
/// là loại lỗi chỉ lộ ra ở đúng nhánh lỗi hiếm, tức là ở Production.</para>
///
/// <para><b>Quét CẢ HAI kiểu bản ghi lỗi</b> (mở rộng 2026-09-03): <see cref="ErrorDescriptor"/>
/// của tầng Application và <see cref="DomainError"/> của tầng Domain. Hai kiểu vì lý do TẦNG (xem
/// chú thích ở <c>DomainError</c>), nhưng chúng đổ ra CÙNG MỘT field <c>businessCode</c> trên
/// envelope — nên phải chung MỘT rổ khi kiểm khuôn và kiểm trùng. Tách rổ là để ngỏ đúng khả năng
/// một mã domain trùng y hệt một mã application mà không test nào thấy.</para>
///
/// <para><b>Đánh đổi:</b> reflection đọc GIÁ TRỊ thật của từng bản ghi sau khi biên dịch, nên nó
/// thấy đúng chuỗi mà API sẽ trả ra, kể cả khi mã được ghép từ hằng số. Giá phải trả: chỉ thấy
/// <c>static readonly</c> — một descriptor dựng thẳng bằng chuỗi trong thân handler sẽ lọt. Lỗ đó
/// KHÔNG còn để ngỏ: <see cref="ErrorCodeSourceTests"/> quét MÃ NGUỒN và cấm đúng khuôn
/// dựng-bằng-chuỗi-literal đó. Hai test bù nhau — reflection canh GIÁ TRỊ, quét nguồn canh CÁCH
/// DỰNG — và không cái nào một mình đủ.</para>
/// </summary>
public class ErrorCatalogTests
{
    /// <summary>
    /// <c>MIEN.MA_LOI</c> — mỗi đoạn bắt đầu bằng chữ cái hoa, chỉ chứa <c>A-Z 0-9 _</c>, và có ít
    /// nhất một dấu chấm phân tách. Cho phép nhiều hơn 2 đoạn (<c>USER.ROLE.FORBIDDEN</c>) vì đó
    /// là cách phân miền hẹp hơn, không phải lệch khuôn.
    ///
    /// <para><b>Neo <c>\A…\z</c> chứ KHÔNG <c>^…$</c>.</b> Trong .NET, <c>$</c> khớp cả ở vị trí
    /// ngay TRƯỚC một ký tự xuống dòng cuối chuỗi — nên <c>^…$</c> chấp nhận
    /// <c>"USER.NOT_FOUND\n"</c>. Một mã lỗi có ký tự thừa ở đuôi vẫn được gửi nguyên xi cho FE và
    /// vẫn không khớp bản dịch nào, tức đúng thứ luật này sinh ra để chặn. Chi tiết này do chính
    /// ca đối chứng <see cref="FormatPattern_Rejects_TheKnownWrongShapes"/> phát hiện
    /// (2026-09-02) — không có ca đối chứng thì lỗ hổng nằm im.</para>
    /// </summary>
    private static readonly Regex BusinessCodePattern =
        new(@"\A[A-Z][A-Z0-9_]*(\.[A-Z][A-Z0-9_]+)+\z", RegexOptions.Compiled);

    [Fact(DisplayName = "Mọi ErrorDescriptor.BusinessCode đúng định dạng MIEN.MA_LOI")]
    public void EveryBusinessCode_Matches_TheContractFormat()
    {
        var descriptors = DeclaredCodes();

        // Chặn "pass rỗng": reflection trả rỗng thì "không mã nào sai định dạng" là câu nói suông.
        Assert.True(descriptors.Count > 0,
            "Không tìm thấy ErrorDescriptor/DomainError nào khai `public static readonly` trong các assembly " +
            "sản phẩm ⇒ test này không đo gì. Nguyên nhân thường gặp: assembly PlatformManager.Core.Application " +
            "không nạp được (kiểm ProductAssemblies.All), hoặc catalog đã đổi sang khuôn khác (property thay vì " +
            "field) — khi đó sửa DeclaredCodes, ĐỪNG xoá test.");

        var malformed = descriptors
            .Where(item => !BusinessCodePattern.IsMatch(item.BusinessCode))
            .Select(item => $"{item.Origin} = \"{item.BusinessCode}\"")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(malformed.Count == 0,
            "BusinessCode lệch khuôn hợp đồng với FE: " + string.Join("; ", malformed) + ". Khuôn bắt buộc " +
            "là MIEN.MA_LOI viết HOA, ngăn bằng dấu chấm, chỉ dùng A-Z 0-9 _ (ví dụ \"USER.NOT_FOUND\", " +
            "\"AUTH.INVALID_CREDENTIALS\"). Vì sao khắt khe: FE phân nhánh theo tiền tố miền và tra bản " +
            "dịch theo mã đầy đủ — mã lệch khuôn không khớp nhánh nào, và KHÔNG phía nào có lỗi biên dịch. " +
            "Xem doc/huong_dan/wiki-core/be/01-core-components.md §\"Hệ quả cho BE\".");
    }

    [Fact(DisplayName = "Mọi ErrorDescriptor.BusinessCode là DUY NHẤT trên toàn hệ thống")]
    public void EveryBusinessCode_IsUnique()
    {
        var descriptors = DeclaredCodes();

        Assert.True(descriptors.Count > 0,
            "Không tìm thấy bản ghi lỗi nào ⇒ test này không đo gì. Xem thông điệp của " +
            "EveryBusinessCode_Matches_TheContractFormat.");

        // Cùng phép nhóm với DuplicateCodes, nhưng giữ bản riêng ở đây vì nó kèm VỊ TRÍ KHAI BÁO:
        // thông điệp lỗi phải chỉ được CẢ HAI chỗ thì người sửa mới biết đổi cái nào.
        var duplicates = descriptors
            .GroupBy(item => item.BusinessCode, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"\"{group.Key}\" khai ở {string.Join(" và ", group.Select(item => item.Origin).Order(StringComparer.Ordinal))}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(duplicates.Count == 0,
            "BusinessCode bị khai trùng: " + string.Join("; ", duplicates) + ". Hai descriptor cùng mã đều " +
            "biên dịch được và đều chạy, nhưng FE chỉ có MỘT bản dịch và MỘT nhánh xử lý cho mã đó — luồng " +
            "thứ hai sẽ nhận đúng câu hướng dẫn của luồng thứ nhất, nghe hợp lý nên không ai báo lỗi. Cách " +
            "sửa: đổi tiền tố miền cho khác nhau (ví dụ \"PERMISSION.VERSION_CONFLICT\" vs " +
            "\"USER.VERSION_CONFLICT\"), rồi BÁO CHO PHÍA FE — mã lỗi là hợp đồng công khai, thêm/đổi mã " +
            "cần một mục bản dịch tương ứng bên kia và không có gì cưỡng chế việc đó.");
    }

    [Fact(DisplayName = "Mọi giá trị ErrorCode khác Success đều là mã HTTP hợp lệ (400–599)")]
    public void EveryErrorCode_Is_AValidHttpStatus()
    {
        var members = Enum.GetValues<ErrorCode>();

        Assert.True(members.Length > 1,
            "Enum ErrorCode chỉ có tối đa một member ⇒ test này không đo gì. Nếu enum đã bị thay bằng cơ chế " +
            "khác thì sửa test cho khớp cơ chế mới, ĐỪNG xoá luật: ApiControllerBase.HandleResult vẫn ép " +
            "thẳng giá trị này vào StatusCode(...).");

        var invalid = members
            .Where(code => code != ErrorCode.Success)
            .Where(code => (int)code is < 400 or > 599)
            .Select(code => $"{code} = {(int)code}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();

        Assert.True(invalid.Count == 0,
            "ErrorCode có member không phải mã HTTP lỗi hợp lệ: " + string.Join(", ", invalid) + ". " +
            "ApiControllerBase.HandleResult ép THẲNG (int)result.Code vào StatusCode(...) — không có bảng " +
            "map thứ hai để chặn. Giá trị ngoài 400–599 sẽ thành một status vô nghĩa trên dây hoặc bị hạ " +
            "tầng biến thành 500, và chỉ lộ ra ở đúng nhánh lỗi hiếm. Cách sửa: chọn một mã HTTP có sẵn " +
            "đúng ngữ nghĩa (400/401/403/404/409/422/429/500); cần phân biệt hẹp hơn thì dùng " +
            "ErrorDescriptor.BusinessCode — đó mới là chỗ dành cho mã nghiệp vụ chi tiết.");
    }

    /// <summary>
    /// Đối chứng — chứng minh regex định dạng biết nói KHÔNG. Không có ca này thì một regex viết
    /// hỏng (ví dụ thiếu neo <c>^…$</c>, hoặc lỡ tay thành <c>.*</c>) sẽ nhận mọi chuỗi, và
    /// <see cref="EveryBusinessCode_Matches_TheContractFormat"/> xanh vĩnh viễn.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: regex định dạng phải từ chối các khuôn sai đã lường trước")]
    public void FormatPattern_Rejects_TheKnownWrongShapes()
    {
        string[] mustReject =
        [
            "userNotFound",          // camelCase — khuôn quen tay nhất khi gõ vội
            "USER_NOT_FOUND",        // thiếu dấu chấm ⇒ FE không tách được miền
            "User.NotFound",         // PascalCase
            "USER.",                 // đoạn sau rỗng
            ".NOT_FOUND",            // đoạn đầu rỗng
            "USER.NOT FOUND",        // có dấu cách
            "USER.NOT-FOUND",        // gạch nối thay vì gạch dưới
            "1USER.NOT_FOUND",       // bắt đầu bằng chữ số
            "USER.NOT_FOUND\n",      // ký tự thừa cuối chuỗi — bẫy kinh điển khi regex thiếu neo $
        ];

        foreach (var wrong in mustReject)
        {
            Assert.False(BusinessCodePattern.IsMatch(wrong),
                $"Regex định dạng CHẤP NHẬN chuỗi sai khuôn \"{wrong.Replace("\n", "\\n")}\" ⇒ nó đang quá " +
                "lỏng, và EveryBusinessCode_Matches_TheContractFormat đang xanh mà không đo gì.");
        }

        string[] mustAccept = ["USER.NOT_FOUND", "AUTH.INVALID_CREDENTIALS", "RATE_LIMIT.TOO_MANY_REQUESTS", "USER.ROLE.FORBIDDEN"];

        foreach (var right in mustAccept)
        {
            Assert.True(BusinessCodePattern.IsMatch(right),
                $"Regex định dạng TỪ CHỐI chuỗi đúng khuôn \"{right}\" ⇒ nó đang quá chặt và sẽ báo đỏ oan " +
                "cho mã hợp lệ. Sửa regex, đừng đổi mã lỗi đang là hợp đồng với FE.");
        }
    }

    /// <summary>
    /// Đối chứng thứ hai — chứng minh phép dò TRÙNG LẶP biết nói KHÔNG. Trên một repo đang sạch,
    /// nhánh "có trùng" không bao giờ chạy, nên nó có thể hỏng mà không ai biết.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: phép dò trùng lặp phải bắt được một mã bị khai hai lần")]
    public void DuplicateDetection_Catches_ACodeDeclaredTwice()
    {
        var descriptors = DeclaredCodes();
        Assert.True(descriptors.Count > 0,
            "Không tìm thấy bản ghi lỗi nào ⇒ ca đối chứng này không chứng minh được gì.");

        // Nhân đôi một descriptor thật — đúng hình dạng của lỗi: ai đó chép một catalog cũ sang
        // module mới và quên đổi tiền tố miền.
        //
        // Đo theo ĐỘ LỆCH (before → after) chứ không theo con số tuyệt đối: nếu repo đang có sẵn
        // một mã trùng thật, phép đo tuyệt đối sẽ làm ĐỎ HAI test và thông điệp thứ hai đổ lỗi
        // nhầm cho bộ dò, che mất vi phạm thật. Cùng lý do đã ghi ở
        // SolutionProjectCoverageTests.Comparison_Detects_AProjectRemovedFromTheSolutionSet.
        var victim = descriptors[0].BusinessCode;
        var duplicatedBefore = DuplicateCodes(descriptors);
        var duplicatedAfter = DuplicateCodes([.. descriptors, descriptors[0]]);

        Assert.True(
            !duplicatedBefore.Contains(victim, StringComparer.Ordinal)
            && duplicatedAfter.Contains(victim, StringComparer.Ordinal)
            && duplicatedAfter.Count == duplicatedBefore.Count + 1,
            $"Khai trùng mã \"{victim}\" mà phép dò KHÔNG báo thêm đúng mã đó ⇒ bộ dò đang hỏng, và " +
            "EveryBusinessCode_IsUnique đang xanh mà không đo gì.");
    }

    /// <summary>
    /// Chặn "mù một nửa": hai kiểu bản ghi lỗi phải ĐỀU có mặt trong rổ. Tập <see cref="DomainError"/>
    /// rỗng nghĩa là 6 mã domain đã di trú (2026-09-03) bị gỡ khỏi catalog, hoặc bộ dò không nhận ra
    /// kiểu đó nữa — và khi đó 2 test khuôn/trùng ở trên vẫn XANH trong khi nửa domain của hợp đồng
    /// không được kiểm gì. Đúng khuôn "xanh vì mù" đã tìm ra ở
    /// <see cref="SoftDeleteQueryFilterTests"/> (finding F2).
    /// </summary>
    [Fact(DisplayName = "Cả hai kiểu bản ghi lỗi (ErrorDescriptor + DomainError) đều có mặt trong rổ")]
    public void BothRecordKinds_Are_Covered()
    {
        var byKind = DeclaredCodes()
            .GroupBy(item => item.Kind, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.True(
            byKind.GetValueOrDefault(nameof(ErrorDescriptor)) > 0
            && byKind.GetValueOrDefault(nameof(DomainError)) > 0,
            "Rổ mã lỗi thiếu hẳn một KIỂU bản ghi (đếm được: " +
            string.Join(", ", byKind.Select(pair => $"{pair.Key}={pair.Value}").Order(StringComparer.Ordinal)) +
            "). Cả hai kiểu đều đổ ra field businessCode của envelope, nên thiếu kiểu nào là nửa hợp đồng đó " +
            "không được kiểm khuôn lẫn kiểm trùng. Nếu tầng Domain thật sự không còn mã lỗi nào thì sửa test " +
            "này KÈM lý do; đừng lặng lẽ để nó rỗng.");
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Một mã đã khai, kèm VỊ TRÍ và KIỂU bản ghi — cả hai đều vào thông điệp lỗi để người sửa biết
    /// phải mở file nào.
    /// </summary>
    private sealed record DeclaredCode(string Origin, string Kind, string BusinessCode);

    /// <summary>Tập <c>BusinessCode</c> xuất hiện nhiều hơn một lần trong danh sách đưa vào.</summary>
    private static List<string> DuplicateCodes(IEnumerable<DeclaredCode> declared) =>
        [.. declared
            .GroupBy(item => item.BusinessCode, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)];

    /// <summary>
    /// Mọi <c>public static readonly</c> kiểu <see cref="ErrorDescriptor"/> HOẶC
    /// <see cref="DomainError"/> trong các assembly sản phẩm. Quét CẢ 4 assembly (không chỉ
    /// Application) để module nghiệp vụ sau này tự động được phủ — luật "mã lỗi là hợp đồng công
    /// khai" không phụ thuộc vào việc catalog nằm ở tầng nào.
    /// </summary>
    private static List<DeclaredCode> DeclaredCodes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .SelectMany(type => type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(field => field.IsInitOnly && IsErrorRecord(field.FieldType))
                .Select(field => new
                {
                    Origin = $"{type.Name}.{field.Name}",
                    Kind = field.FieldType.Name,
                    Code = BusinessCodeOf(field.GetValue(null)),
                })
                // Nới 2026-09-03: đọc CẢ static property. Một lượt kiểm độc lập chứng minh khai
                // bằng property (`public static DomainError X => new("…")`) đi lọt hoàn toàn —
                // không phải field nên reflection không thấy, mà nằm trong catalog nên T2 cũng bỏ
                // qua. Chỉ nhận property KHÔNG có tham số và ĐỌC ĐƯỢC; getter ném thì bỏ qua thay
                // vì làm đỏ cả bộ test vì một lý do không liên quan tới mã lỗi.
                .Concat(type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(prop => prop.CanRead
                        && prop.GetIndexParameters().Length == 0
                        && IsErrorRecord(prop.PropertyType))
                    .Select(prop => new
                    {
                        Origin = $"{type.Name}.{prop.Name}",
                        Kind = prop.PropertyType.Name,
                        Code = TryReadCode(prop),
                    })))
            .Where(item => item.Code is not null)
            .Select(item => new DeclaredCode(item.Origin, item.Kind, item.Code!))
            .OrderBy(item => item.Origin, StringComparer.Ordinal)];

    /// <summary>
    /// Lọc theo KIỂU FIELD trước, rồi mới <c>GetValue</c> — thứ tự này bắt buộc, không phải để chạy
    /// nhanh. Gọi <c>GetValue</c> trên field khai trong một kiểu generic MỞ (ví dụ cache tĩnh của
    /// <c>ApiResult&lt;T&gt;</c>) ném <c>InvalidOperationException</c> "Late bound operations cannot be
    /// performed on fields with types for which Type.ContainsGenericParameters is true" — tức là
    /// 3 test của file này chết vì hạ tầng, chứ không phải vì phát hiện vi phạm nào. Đã xảy ra thật
    /// 2026-09-03 khi mở rộng bộ dò sang <see cref="DomainError"/> và lỡ bỏ phép lọc kiểu.
    /// </summary>
    private static bool IsErrorRecord(Type type) =>
        type == typeof(ErrorDescriptor) || type == typeof(DomainError);

    /// <summary>
    /// Rút <c>BusinessCode</c> ra khỏi một bản ghi lỗi bất kể nó thuộc kiểu nào; <c>null</c> nghĩa là
    /// field đó không phải bản ghi lỗi. Viết dạng switch để thêm kiểu thứ ba (nếu có) là thêm đúng
    /// một nhánh, thay vì mọc thêm một bộ dò song song — và nhớ thêm nhánh tương ứng ở
    /// <see cref="IsErrorRecord"/>, vì phép lọc kiểu mới là thứ quyết định field nào được đọc.
    /// </summary>
    /// <summary>
    /// Đọc mã từ một static property, nuốt exception của getter. Property có thể ném (phụ thuộc
    /// cấu hình chưa nạp lúc chạy test) — khi đó nó KHÔNG phải một mã lỗi khai báo được, và làm
    /// đỏ cả bộ test vì lý do đó là báo sai chỗ.
    /// </summary>
    private static string? TryReadCode(System.Reflection.PropertyInfo prop)
    {
        try { return BusinessCodeOf(prop.GetValue(null)); }
        catch { return null; }
    }

    private static string? BusinessCodeOf(object? value) => value switch
    {
        ErrorDescriptor descriptor => descriptor.BusinessCode,
        DomainError error => error.BusinessCode,
        _ => null,
    };
}
