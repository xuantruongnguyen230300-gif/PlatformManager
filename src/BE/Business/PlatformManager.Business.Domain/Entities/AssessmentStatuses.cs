namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Bốn giá trị <c>Trạng thái</c>, lưu NGUYÊN VĂN tiếng Việt —
/// spec/danh-muc-dti/business-rules.md §4. Người dùng CHỌN TAY; hệ thống không tự tính, không
/// tự đổi, và không suy từ điểm.
///
/// <para><b>So khớp (luật 4 của §4):</b> cắt khoảng trắng đầu/cuối, KHÔNG phân biệt hoa/thường,
/// CÓ phân biệt dấu tiếng Việt. <c>"đang thực hiện"</c> khớp; <c>"Dang thuc hien"</c> KHÔNG —
/// bỏ dấu ở đây sẽ làm <c>Chưa thực hiện</c> và <c>Chua thuc hien</c> cùng khớp, mà đó là hai
/// chuỗi người dùng gõ với ý định khác nhau. Đây là chỗ Q47 (tìm kiếm không dấu) CỐ Ý không áp.</para>
/// </summary>
public static class AssessmentStatuses
{
    public const string NotStarted = "Chưa thực hiện";
    public const string InProgress = "Đang thực hiện";
    public const string NeedsEvidence = "Cần bổ sung minh chứng";
    public const string Done = "Hoàn thành";

    public static readonly string[] All = [NotStarted, InProgress, NeedsEvidence, Done];

    /// <summary>
    /// Quy một chuỗi người dùng nhập về đúng một trong 4 giá trị chuẩn, theo luật so khớp ở
    /// docstring của lớp. Trả <c>false</c> nếu không khớp giá trị nào — nơi gọi quyết định mã lỗi
    /// (400 ở đường đọc/ghi tay, lỗi DÒNG ở đường import).
    /// </summary>
    public static bool TryResolve(string? value, out string resolved)
    {
        resolved = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        foreach (var candidate in All)
        {
            // OrdinalIgnoreCase: không phân biệt hoa/thường nhưng KHÔNG đụng tới dấu — đúng luật 4.
            if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                resolved = candidate;
                return true;
            }
        }

        return false;
    }
}
