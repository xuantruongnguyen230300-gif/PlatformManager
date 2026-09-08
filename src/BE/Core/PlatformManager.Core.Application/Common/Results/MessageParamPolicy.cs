using System.Globalization;

namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Bộ lọc + bộ đổi-chuỗi DUY NHẤT cho <c>messageParams</c> — tham số rời của câu thông điệp mà
/// envelope mang theo mã lỗi (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10).
///
/// <para><b>Vì sao phải có ALLOWLIST chứ không chuyển tiếp cả từ điển</b> (§10.4, ràng buộc BẮT
/// BUỘC): <c>ValidationFailure.FormattedMessagePlaceholderValues</c> của FluentValidation LUÔN chứa
/// khoá <c>PropertyValue</c> = <b>chính giá trị người dùng vừa gõ</b> — thư viện nạp khoá này cho
/// mọi failure, không phụ thuộc câu lỗi có dùng tới nó hay không. Đổ thẳng từ điển đó ra response
/// nghĩa là rule độ dài của MẬT KHẨU MỚI fail ⇒ mật khẩu vừa nhập đi ra HTTP response, vào log
/// trình duyệt, vào mọi telemetry của client. Danh sách CHO PHÉP (chứ không phải danh sách CẤM) là
/// khác biệt quyết định: thư viện thêm khoá mới ở bản sau thì khoá đó mặc định KHÔNG ra ngoài.</para>
///
/// <para><b>Vì sao giá trị là chuỗi chứ không phải kiểu JSON nguyên bản:</b> từ điển gốc mang
/// <c>object</c>. Serialize nguyên trạng là mở sẵn một đường rò cho bất kỳ thứ gì rơi vào đó (một
/// entity, một DTO nội bộ) — không đoán trước được nó sẽ mang gì. Đổi sang chuỗi ngay tại biên là
/// chỗ duy nhất chặn được điều đó bằng cấu trúc, không bằng kỷ luật người viết.</para>
///
/// <para><b>Cái giá đã biết và chấp nhận</b> (§10.4 ý 3): client KHÔNG định dạng lại số/ngày theo
/// locale được vì nhận về đã là chuỗi. Ở quy mô này tham số hầu hết là số nguyên nhỏ (độ dài tối
/// thiểu) và tên riêng — định dạng theo locale không đổi gì.</para>
/// </summary>
public static class MessageParamPolicy
{
    /// <summary>
    /// Khoá KHÔNG BAO GIỜ được ra ngoài, khai thành hằng số để test đối chứng và phần còn lại của
    /// hệ nói về cùng một chuỗi thay vì mỗi nơi gõ lại.
    /// </summary>
    public const string UserSuppliedValueKey = "PropertyValue";

    /// <summary>
    /// Khoá chỗ giữ của FluentValidation được phép ra tới client. Đối chiếu nhị phân với
    /// <c>FluentValidation 12.0.0</c> ngày 2026-09-04 (cách kiểm: §10.3 của file doc nêu trên) —
    /// mọi khoá dưới đây đều có thật trong dll, không phải chép từ tài liệu.
    ///
    /// <para><b>Tiêu chí vào danh sách:</b> khoá mô tả <b>chính sách</b> (ngưỡng, giới hạn, mốc so
    /// sánh) hoặc <b>vị trí</b> của lỗi — thứ client cần để dựng lại câu ở ngôn ngữ đang chọn. Khoá
    /// mô tả <b>dữ liệu người dùng vừa nhập</b> thì không, và đó là lý do
    /// <see cref="UserSuppliedValueKey"/> vắng mặt ở đây. <c>RegularExpression</c> cũng cố ý vắng:
    /// nó phơi mẫu kiểm nội bộ ra ngoài mà không giúp dựng câu cho người dùng cuối.</para>
    /// </summary>
    private static readonly HashSet<string> AllowedValidationKeys = new(StringComparer.Ordinal)
    {
        "PropertyName",
        "PropertyPath",
        "CollectionIndex",
        "ComparisonValue",
        "ComparisonProperty",
        "MinLength",
        "MaxLength",
        "TotalLength",
        "From",
        "To",
        "Digits",
        "ExpectedPrecision",
        "ExpectedScale",
        "ActualScale",
    };

    /// <summary>
    /// Lọc từ điển chỗ giữ của một <c>ValidationFailure</c> qua allowlist rồi đổi giá trị sang
    /// chuỗi. Trả <b>null</b> khi không còn khoá nào — null ⇒ trường vắng mặt trên dây, đúng khuôn
    /// <c>Retryable</c>/<c>Fields</c> đang dùng; một từ điển RỖNG sẽ làm envelope phình thêm một
    /// khoá vô nghĩa cho mọi lỗi.
    /// </summary>
    public static Dictionary<string, string>? FromValidationPlaceholders(
        IReadOnlyDictionary<string, object>? placeholders)
    {
        if (placeholders is null || placeholders.Count == 0)
            return null;

        Dictionary<string, string>? kept = null;

        foreach (var pair in placeholders)
        {
            if (!AllowedValidationKeys.Contains(pair.Key))
                continue;

            kept ??= new Dictionary<string, string>(StringComparer.Ordinal);
            kept[pair.Key] = Stringify(pair.Value);
        }

        return kept;
    }

    /// <summary>
    /// Đổi một giá trị tham số sang chuỗi bằng văn hoá <b>invariant</b> (§10.4 ý 2).
    ///
    /// <para><b>Vì sao tường minh invariant dù BE cố ý không bật hạ tầng bản địa hoá nào</b> (§5.1):
    /// đây là dữ liệu đi ra HỢP ĐỒNG API, và một hợp đồng phụ thuộc vào cấu hình tiến trình là hợp
    /// đồng đổi nghĩa khi có người đổi cấu hình. Với <c>vi-VN</c>, dấu thập phân là dấu phẩy —
    /// client tự parse lại con số sẽ ra kết quả khác mà không có gì báo. Tường minh ở đây khiến hai
    /// mục doc đó không thể mâu thuẫn nhau, và đi đúng tiền lệ đã có ở <c>Program.cs</c> (giá trị
    /// <c>Retry-After</c> cũng đổi chuỗi bằng văn hoá invariant tường minh).</para>
    /// </summary>
    public static string Stringify(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
