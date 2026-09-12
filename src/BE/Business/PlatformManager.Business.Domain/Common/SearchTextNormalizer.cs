using System.Globalization;
using System.Text;

namespace PlatformManager.Business.Domain.Common;

/// <summary>
/// Chuẩn hoá một chuỗi tiếng Việt về dạng dùng để TÌM KIẾM không phân biệt hoa/thường và
/// không phân biệt dấu — Q47, spec/danh-muc-dti/business-rules.md §1.2.
///
/// <para><b>Một hàm, hai nơi gọi.</b> Entity gọi nó ở mọi chỗ gán <c>Name</c> để ghi cột
/// <c>NameNormalized</c>; handler gọi nó để chuẩn hoá từ khoá <c>search</c> trước khi so.
/// Hai bên PHẢI dùng đúng một hàm — hai bản chuẩn hoá lệch nhau một ký tự là tìm không ra
/// mà không có lỗi nào.</para>
///
/// <para><b>Vì sao đặt ở Business.Domain chứ không ở Core:</b> đây là luật của một nghiệp vụ
/// (cột <c>NameNormalized</c> chỉ tồn tại vì DM-2 tìm theo tên), và <c>Core.Common</c> —
/// chỗ hợp lệ cho utility thuần — chưa là project riêng. Đưa nó vào <c>Core.Domain</c> là
/// đúng thứ mà doc/kien-truc-core-module.md §"Quyết định BE" cảnh báo: utility bò dần vào
/// Domain vì không có chỗ đặt hợp lệ.</para>
/// </summary>
public static class SearchTextNormalizer
{
    /// <summary>
    /// Cắt khoảng trắng đầu/cuối → chữ thường (invariant) → bỏ dấu → <c>đ</c>/<c>Đ</c> thành
    /// <c>d</c>. <c>null</c>/rỗng/toàn khoảng trắng trả về chuỗi rỗng.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lowered = value.Trim().ToLowerInvariant();

        // NFD tách ký tự có dấu thành ký tự gốc + dấu tổ hợp, nên bỏ mọi NonSpacingMark là bỏ dấu.
        var decomposed = lowered.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            // ⚠️ 'đ' (U+0111) là một CHỮ CÁI RIÊNG của bảng chữ cái, không phải 'd' cộng dấu —
            // NFD KHÔNG tách được nó. Bỏ bước này thì gõ "dao tao" không ra "Đào tạo", và không
            // có lỗi nào báo. Đây là cái bẫy được nêu đích danh ở §1.2 của file luật.
            if (ch is 'đ' or 'Đ')
            {
                builder.Append('d');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        // Ghép lại về NFC: chuỗi kết quả chỉ còn ASCII cho tiếng Việt, nhưng chuẩn hoá lại để
        // ký tự ngoài bảng chữ cái Việt (nếu có) không ở dạng phân rã nửa vời.
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
