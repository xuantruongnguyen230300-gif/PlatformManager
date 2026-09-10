using System.Globalization;

namespace PlatformManager.Core.Application.Import;

/// <summary>
/// Giá trị MỘT ô của file import, ở dạng TRUNG TÍNH định dạng: bên đọc không biết cột này là gì,
/// bên xử lý không biết file gốc là CSV hay Excel.
///
/// <para><b>Vì sao không phải <c>string?</c> — đây là chỗ lệch có chủ đích so với chữ ký ở
/// doc/huong_dan/wiki-core/be/15-import-export.md §2.</b> Chữ ký in trong tài liệu đó là
/// <c>IReadOnlyDictionary&lt;string, string?&gt;</c>, nhưng CHÍNH file đó, mục §2c, lại yêu cầu
/// <i>"giữ kiểu ngày thật qua seam thay vì chuyển thành chuỗi"</i> — hai câu không cùng đúng được.
/// Bản sửa này giữ nguyên phần còn lại của hình dạng cũ (dictionary theo tên cột) và chỉ đổi kiểu
/// giá trị, vì lỗi §2c chính là lỗi mà chữ ký <c>string?</c> gây ra: ô ngày trong <c>.xlsx</c> bị
/// ép về <c>dd/MM/yyyy</c> rồi bên kia parse ngược — vừa mất giờ/phút, vừa dính locale máy chạy.</para>
///
/// <para><b><see cref="Text"/> luôn dùng được, kể cả với ô kiểu.</b> CSV không có kiểu nên mọi ô
/// của nó là chữ; bên xử lý vì thế vẫn phải biết tự parse. Điểm khác là chuỗi ở đây luôn theo
/// <see cref="CultureInfo.InvariantCulture"/> và round-trip được, nên parse lại không mất gì.</para>
/// </summary>
public readonly record struct ImportCellValue
{
    private ImportCellValue(string? text, object? value)
    {
        Text = text;
        Value = value;
    }

    /// <summary>Ô trống — cả <see cref="Text"/> lẫn <see cref="Value"/> đều <c>null</c>.</summary>
    public static ImportCellValue Empty => default;

    /// <summary>
    /// Dạng chữ của ô. Với ô kiểu (ngày/số/luận lý) đây là chuỗi INVARIANT round-trip, KHÔNG phải
    /// chuỗi đã định dạng theo locale — xem docstring của kiểu.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Giá trị nguyên kiểu: <see cref="string"/>, <see cref="System.DateTime"/>,
    /// <see cref="double"/>, <see cref="bool"/>, hoặc <c>null</c>. Bên xử lý nên hỏi qua
    /// <see cref="AsDate"/>/<see cref="AsNumber"/>/<see cref="AsBoolean"/> trước, rồi mới lùi về
    /// parse <see cref="Text"/> — đường lùi ấy là đường DUY NHẤT cho file CSV.
    /// </summary>
    public object? Value { get; }

    /// <summary>Ô không có nội dung gì (không tồn tại, blank, hoặc chuỗi rỗng).</summary>
    public bool IsEmpty => Value is null && string.IsNullOrEmpty(Text);

    /// <summary>Ngày thật nếu ô mang kiểu ngày; <c>null</c> với mọi kiểu khác (kể cả CSV).</summary>
    public DateTime? AsDate => Value as DateTime?;

    /// <summary>Số thật nếu ô mang kiểu số; <c>null</c> với mọi kiểu khác (kể cả CSV).</summary>
    public double? AsNumber => Value as double?;

    /// <summary>Luận lý thật nếu ô mang kiểu luận lý; <c>null</c> với mọi kiểu khác.</summary>
    public bool? AsBoolean => Value as bool?;

    /// <summary>Ô chữ. Chuỗi rỗng quy về <see cref="Empty"/> — "có ô nhưng không có gì trong đó"
    /// và "không có ô" là cùng một chuyện với bên xử lý.</summary>
    public static ImportCellValue FromText(string? text) =>
        string.IsNullOrEmpty(text) ? Empty : new(text, text);

    /// <summary>
    /// Ô ngày. <see cref="Text"/> ghi theo <c>"O"</c> (ISO 8601 round-trip) chứ không theo
    /// <c>dd/MM/yyyy</c> — đó chính là lỗi §2c của
    /// doc/huong_dan/wiki-core/be/15-import-export.md.
    /// </summary>
    public static ImportCellValue FromDate(DateTime value) =>
        new(value.ToString("O", CultureInfo.InvariantCulture), value);

    /// <summary><c>"R"</c> = round-trip: parse lại ra đúng <see cref="double"/> ban đầu.</summary>
    public static ImportCellValue FromNumber(double value) =>
        new(value.ToString("R", CultureInfo.InvariantCulture), value);

    /// <summary>Chữ thường cố định, không theo locale (<c>bool.ToString()</c> trả "True"/"False").</summary>
    public static ImportCellValue FromBoolean(bool value) =>
        new(value ? "true" : "false", value);

    /// <summary>Trả <see cref="Text"/> để nội suy chuỗi/log không phải viết <c>?? string.Empty</c>.</summary>
    public override string ToString() => Text ?? string.Empty;
}
