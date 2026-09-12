namespace PlatformManager.Business.Domain.Common;

/// <summary>
/// Luật của MÃ chỉ tiêu (<c>Criteria.Code</c>) — spec/danh-muc-dti/business-rules.md §2:
/// khuôn hợp lệ (Q65), trần độ dài tổng và trần chữ số mỗi đoạn (Q58), và phép dựng khoá sắp
/// xếp tự nhiên <c>CodeSortKey</c> (Q53).
///
/// <para><b>Một hàm kiểm, ba đường ghi.</b> Tạo (DM-3), sửa (DM-4) và import (DM-7) dùng CHUNG
/// <see cref="IsWellFormed"/> / <see cref="HasSegmentTooLong"/>; chúng chỉ khác nhau ở MÃ LỖI
/// trả về, không khác ở luật. Vòng 1 chưa có đường ghi nào — nhưng entity đã ép luật ngay tại
/// <see cref="Entities.Criteria.Create"/>, nên không đường nào lách được.</para>
/// </summary>
public static class CriteriaCode
{
    /// <summary>Trần độ dài toàn bộ mã — §2.</summary>
    public const int MaxLength = 20;

    /// <summary>Trần số chữ số của MỘT đoạn — Q58, §2.</summary>
    public const int MaxSegmentDigits = 4;

    /// <summary>
    /// Độ dài cột <c>CodeSortKey</c>. Suy từ hai ràng buộc trên, KHÔNG từ dữ liệu mẫu:
    /// mã <c>n</c> đoạn cần ít nhất <c>2n − 1</c> ký tự ⇒ tối đa 10 đoạn; khoá của <c>n</c> đoạn
    /// dài <c>5n − 1</c> ⇒ dài nhất 49. Phép tính đầy đủ ở §1.2 của file luật.
    /// </summary>
    public const int SortKeyMaxLength = 49;

    /// <summary>
    /// Khuôn hợp lệ (Q65): CHỈ chữ số, ngăn bằng dấu chấm ĐƠN, không đoạn rỗng, không dấu chấm
    /// ở đầu hoặc cuối. Không giới hạn số cấp — dữ liệu thật có mã ba cấp (<c>4.22.11</c>), nên
    /// một regex kiểu <c>^\d+\.\d+$</c> sẽ từ chối đúng những chỉ tiêu hợp lệ (§2).
    /// </summary>
    public static bool IsWellFormed(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var trimmed = code.Trim();
        if (trimmed.Length > MaxLength)
            return false;

        foreach (var segment in trimmed.Split('.'))
        {
            if (segment.Length == 0)
                return false;

            foreach (var ch in segment)
            {
                if (ch is < '0' or > '9')
                    return false;
            }
        }

        return true;
    }

    /// <summary>Có đoạn nào vượt <see cref="MaxSegmentDigits"/> chữ số không (Q58).</summary>
    public static bool HasSegmentTooLong(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        foreach (var segment in code.Trim().Split('.'))
        {
            if (segment.Length > MaxSegmentDigits)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Dựng khoá sắp xếp tự nhiên (Q53): đệm <c>0</c> bên trái từng đoạn cho đủ
    /// <see cref="MaxSegmentDigits"/> chữ số rồi nối lại bằng dấu chấm —
    /// <c>4.2</c> → <c>0004.0002</c>, <c>4.22.11</c> → <c>0004.0022.0011</c>. Nhờ vậy
    /// <c>4.2 &lt; 4.10 &lt; 4.22.11</c> đúng khi so CHUỖI theo byte (cột khai
    /// <c>COLLATE "C"</c>, xem cấu hình EF).
    ///
    /// <para>Mã sai khuôn (không lọt qua <see cref="IsWellFormed"/>) không bao giờ tới được đây
    /// qua đường ghi — nhưng nếu tới thì đoạn không phải toàn chữ số được giữ NGUYÊN VĂN thay vì
    /// ném: khoá sắp xếp là dữ liệu hiển thị, không phải chỗ cưỡng chế luật.</para>
    /// </summary>
    public static string BuildSortKey(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var segments = code.Trim().Split('.');
        var padded = new string[segments.Length];

        for (var i = 0; i < segments.Length; i++)
        {
            padded[i] = segments[i].Length < MaxSegmentDigits
                ? segments[i].PadLeft(MaxSegmentDigits, '0')
                : segments[i];
        }

        var key = string.Join('.', padded);

        // Lưới an toàn cho cột string(49): mã sai khuôn (đoạn quá dài) có thể sinh khoá dài hơn
        // trần. Cắt còn hơn ném ở tầng lưu trữ — luật khuôn mã đã có chỗ cưỡng chế riêng.
        return key.Length <= SortKeyMaxLength ? key : key[..SortKeyMaxLength];
    }
}
