using System.Globalization;
using PlatformManager.Core.Application.Import;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Ánh xạ 11 cột của file nạp — spec/danh-muc-dti/business-rules.md §6.2.
///
/// <para><b>Khớp header theo TÊN CỘT, không theo vị trí.</b> So khớp: cắt khoảng trắng, KHÔNG phân
/// biệt hoa/thường, CÓ phân biệt dấu. Khớp theo vị trí thì một cột thừa chèn vào giữa file sẽ đẩy
/// mọi cột sau đó sang một ô — và kiểu hỏng đó không có triệu chứng nào ngoài dữ liệu sai.</para>
///
/// <para><b>11 cột này ánh xạ 1-1 với 11 cột đầu của FILE XUẤT</b> (DB-4 xuất 12 cột = 11 cột này
/// + <c>Tiến độ %</c>). Đó là ràng buộc thiết kế, không phải trùng hợp: file tải về phải nạp ngược
/// lại được. Đổi tên một hằng số ở đây mà không đổi bên export là phá round-trip.</para>
/// </summary>
public static class ImportColumns
{
    public const string Code = "Mã";
    public const string Name = "Chỉ tiêu";
    public const string Group = "Nhóm";
    public const string MaxScore = "Điểm tối đa";
    public const string SelfScore = "Tự đánh giá";
    public const string VerifiedScore = "Thẩm định";

    /// <summary>
    /// ⚠️ <b>ĐỌC VÀO RỒI VỨT — không đối chiếu, không báo lỗi khi lệch, không đòi phải có mặt.</b>
    /// Đây là trường TÍNH (§3.1), không phải dữ liệu.
    ///
    /// <para>Sau Q25 luật này còn quan trọng hơn trước: cột <c>Chênh lệch</c> trong file gốc của BA
    /// tính theo chiều CŨ, tức ngược dấu với thứ hệ thống tính ra ở <b>27/62 dòng</b>. Một đường
    /// import có đối chiếu sẽ từ chối gần nửa file gốc và đổ lỗi cho dữ liệu của BA — trong khi nó
    /// đúng theo quy ước của họ. Đọc vào rồi vứt là cách duy nhất để hai quy ước cùng tồn tại.</para>
    ///
    /// <para>Hằng số này tồn tại để tên cột được KHAI, không phải để được dùng: nó là bằng chứng
    /// rằng cột thứ 7 đã được cân nhắc chứ không bị bỏ sót. Không nơi nào đọc giá trị của nó.</para>
    /// </summary>
    public const string Diff = "Chênh lệch";

    public const string Status = "Trạng thái";
    public const string Owner = "Phụ trách";
    public const string Deadline = "Hạn xử lý";
    public const string Note = "Minh chứng/Ghi chú";

    /// <summary>
    /// Hai cột mà thiếu header là file KHÔNG DÙNG ĐƯỢC, không phải "mọi dòng đều lỗi":
    /// <see cref="Code"/> (không có nó thì không định danh được dòng nào) và <see cref="Group"/>
    /// (bắt buộc ở MỌI dòng theo §6.2, nên thiếu header sẽ sinh đúng 62 lỗi giống hệt nhau —
    /// một danh sách lỗi như vậy không nói cho người dùng biết điều gì mà một câu không nói được).
    ///
    /// <para><see cref="Name"/> và <see cref="MaxScore"/> CỐ Ý không nằm ở đây: §6.2 khai chúng bắt
    /// buộc "(khi tạo mới)". Một file chỉ cập nhật điểm cho các chỉ tiêu đã có là ca dùng hợp lệ.</para>
    /// </summary>
    public static readonly string[] RequiredHeaders = [Code, Group];

    /// <summary>
    /// Ký tự file XUẤT ghi vào ô trống (<c>spec/dashboard-dti/business-rules.md</c> §4.3). Đường
    /// nạp phải BỎ QUA nó y như bỏ qua ô trống — nếu không, vòng
    /// <i>export → sửa trong Excel → import</i> sẽ ghi chính chữ <c>—</c> vào database.
    ///
    /// <para>Nhận cả EM DASH (U+2014, thứ file xuất ghi) lẫn EN DASH (U+2013, thứ Excel hay tự đổi
    /// khi người dùng gõ tay): hai ký tự nhìn gần như nhau và người sửa file không phân biệt được.</para>
    /// </summary>
    private static readonly string[] EmptyMarkers = ["—", "–"];

    /// <summary>Ô rỗng, hoặc ô chỉ chứa dấu gạch "không có dữ liệu" của file xuất.</summary>
    public static bool IsBlank(ImportCellValue cell) => Text(cell) is null;

    /// <summary>
    /// Nội dung ô ở dạng chữ, đã cắt khoảng trắng; <c>null</c> khi ô rỗng hoặc mang dấu gạch
    /// "không có dữ liệu".
    /// </summary>
    public static string? Text(ImportCellValue cell)
    {
        if (cell.IsEmpty)
            return null;

        var text = cell.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;

        return Array.IndexOf(EmptyMarkers, text) >= 0 ? null : text;
    }

    /// <summary>
    /// Số của một ô. Trả <c>false</c> KÈM <c>value = null</c> khi ô rỗng — nơi gọi phân biệt "ô
    /// trống" với "ô có nội dung nhưng không đọc ra số" bằng <see cref="IsBlank"/>.
    ///
    /// <para><b>Hỏi kiểu thật TRƯỚC, parse chuỗi sau.</b> Ô Excel mang kiểu số đi qua seam nguyên
    /// kiểu (<see cref="ImportCellValue.AsNumber"/>); đường parse chuỗi là đường DUY NHẤT cho CSV,
    /// và nó dùng <see cref="CultureInfo.InvariantCulture"/> vì <see cref="ImportCellValue"/> bảo
    /// đảm mọi chuỗi của nó là invariant round-trip.</para>
    /// </summary>
    public static bool TryDecimal(ImportCellValue cell, out decimal? value)
    {
        value = null;

        if (IsBlank(cell))
            return false;

        if (cell.AsNumber is { } number)
        {
            value = (decimal)number;
            return true;
        }

        var text = Text(cell)!;

        // Người sửa file tay ở máy vi-VN gõ dấu phẩy thập phân. Chấp nhận cả hai dấu là dung thứ
        // đúng chỗ: nó không tạo ra nhập nhằng nào, vì ô của các cột số này không bao giờ là một
        // danh sách. Thử invariant trước để "1,5" không bị đọc thành 15 ở máy có dấu phân nhóm.
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ngày của một ô. Cùng khuôn với <see cref="TryDecimal"/>: hỏi kiểu thật trước
    /// (<see cref="ImportCellValue.AsDate"/>), rồi mới parse chuỗi.
    ///
    /// <para>Chuỗi được thử theo ĐÚNG BA khuôn, không dùng parse "đoán theo locale": ISO round-trip
    /// (thứ seam sinh ra), <c>yyyy-MM-dd</c>, và <c>dd/MM/yyyy</c> (thứ người dùng Việt gõ tay, và
    /// cũng là khuôn file xuất ghi ra). Parse theo locale máy chạy là đúng lỗi §2c mà seam sinh ra
    /// để chặn — <c>03/04/2026</c> sẽ là hai ngày khác nhau trên hai máy.</para>
    /// </summary>
    public static bool TryDate(ImportCellValue cell, out DateOnly? value)
    {
        value = null;

        if (IsBlank(cell))
            return false;

        if (cell.AsDate is { } date)
        {
            value = DateOnly.FromDateTime(date);
            return true;
        }

        var text = Text(cell)!;

        if (DateTime.TryParseExact(
                text,
                ["O", "yyyy-MM-dd", "dd/MM/yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            value = DateOnly.FromDateTime(parsed);
            return true;
        }

        return false;
    }
}
