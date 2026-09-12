using PlatformManager.Business.Domain.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.Business.UnitTests.Domain;

/// <summary>
/// Hai cột DẪN XUẤT của <see cref="Criteria"/> (<c>NameNormalized</c> — Q47, <c>CodeSortKey</c> —
/// Q53) do CHÍNH entity ghi. Luật ở spec/danh-muc-dti/business-rules.md §1.2.
///
/// <para>Đây là bất biến đắt nhất của entity này: nó hỏng IM LẶNG. Cột lệch không gây lỗi biên
/// dịch, không lỗi lúc chạy — chỉ là gõ từ khoá đúng mà không ra dòng nào, hoặc lưới sắp sai thứ
/// tự. Vì vậy phải có test đi thẳng vào từng nhánh, không chỉ test đường hạnh phúc.</para>
/// </summary>
public class CriteriaDerivedColumnTests
{
    private static readonly Guid AnyGroup = Guid.CreateVersion7();

    [Theory]
    // Cái bẫy được nêu đích danh ở §1.2: 'đ' (U+0111) là CHỮ CÁI RIÊNG, không phải 'd' cộng dấu,
    // nên tách NFD KHÔNG gỡ được nó. Bỏ sót bước này thì gõ "dao tao" không ra "Đào tạo".
    [InlineData("Đào tạo nhân lực số", "dao tao nhan luc so")]
    [InlineData("ĐỔI MỚI SÁNG TẠO", "doi moi sang tao")]
    [InlineData("Hạ tầng và Nền tảng số", "ha tang va nen tang so")]
    [InlineData("  An toàn thông tin  ", "an toan thong tin")]
    [InlineData("Hoạt động Kinh tế số", "hoat dong kinh te so")]
    public void Rename_Writes_NormalizedName(string name, string expected)
    {
        var criteria = Criteria.Create("1.1", name, AnyGroup, 10m);

        Assert.Equal(expected, criteria.NameNormalized);
    }

    /// <summary>
    /// Từ khoá tìm kiếm phải đi qua ĐÚNG hàm chuẩn hoá mà entity dùng. Hai bản chuẩn hoá lệch
    /// nhau một ký tự là tìm không ra mà không có lỗi nào — nên ca này so hai đầu với nhau, không
    /// so với một chuỗi chép tay.
    /// </summary>
    [Fact]
    public void SearchKeyword_And_StoredColumn_Use_TheSameNormalizer()
    {
        var criteria = Criteria.Create("2.1", "Đào tạo kỹ năng số", AnyGroup, 20m);

        Assert.Contains(SearchTextNormalizer.Normalize("ĐÀO TẠO"), criteria.NameNormalized);
        Assert.Contains(SearchTextNormalizer.Normalize("dao tao"), criteria.NameNormalized);
        Assert.Contains(SearchTextNormalizer.Normalize("  Kỹ Năng  "), criteria.NameNormalized);
    }

    [Theory]
    [InlineData("4.2", "0004.0002")]
    [InlineData("4.10", "0004.0010")]
    [InlineData("4.22.11", "0004.0022.0011")]
    [InlineData("1", "0001")]
    public void SetCode_Writes_SortKey(string code, string expected)
    {
        var criteria = Criteria.Create(code, "x", AnyGroup, 10m);

        Assert.Equal(expected, criteria.CodeSortKey);
    }

    /// <summary>
    /// Phép thử THẬT của Q53: so CHUỖI trên khoá phải cho ra thứ tự TỰ NHIÊN. So chuỗi trên chính
    /// <c>Code</c> thì cho kết quả ngược — đó là toàn bộ lý do cột này tồn tại.
    /// </summary>
    [Fact]
    public void SortKey_Orders_CodesNaturally()
    {
        string[] codes = ["4.22.11", "4.2", "4.10", "4.22.2", "4.1"];

        var bySortKey = codes
            .Select(code => Criteria.Create(code, "x", AnyGroup, 10m))
            .OrderBy(criteria => criteria.CodeSortKey, StringComparer.Ordinal)
            .ThenBy(criteria => criteria.Code, StringComparer.Ordinal)
            .Select(criteria => criteria.Code)
            .ToArray();

        Assert.Equal(["4.1", "4.2", "4.10", "4.22.2", "4.22.11"], bySortKey);

        // Ca đối chứng: sắp theo chính Code cho kết quả KHÁC. Không có khẳng định này thì test
        // trên vẫn xanh kể cả khi CodeSortKey bị thay bằng chính Code.
        var byRawCode = codes.OrderBy(code => code, StringComparer.Ordinal).ToArray();
        Assert.NotEqual(byRawCode, bySortKey);
    }

    /// <summary>
    /// Trần độ dài khoá (49) suy từ hai ràng buộc của §2, không từ dữ liệu mẫu: tổng mã ≤ 20 ký
    /// tự và mỗi đoạn ≤ 4 chữ số ⇒ tối đa 10 đoạn ⇒ khoá dài nhất 5×10 − 1.
    /// </summary>
    [Fact]
    public void SortKey_NeverExceeds_ColumnLength()
    {
        // 10 đoạn, đúng số đoạn tối đa mà một mã 20 ký tự chứa được.
        var criteria = Criteria.Create("1.2.3.4.5.6.7.8.9.1", "x", AnyGroup, 10m);

        Assert.Equal(CriteriaCode.SortKeyMaxLength, criteria.CodeSortKey.Length);
    }

    [Theory]
    [InlineData("4..2")]      // đoạn rỗng
    [InlineData(".4.2")]      // dấu chấm ở đầu
    [InlineData("4.2.")]      // dấu chấm ở cuối
    [InlineData("4.2a")]      // chữ cái
    [InlineData("abc")]
    public void SetCode_Rejects_MalformedCode(string code)
    {
        var error = Assert.Throws<DomainException>(() => Criteria.Create(code, "x", AnyGroup, 10m));

        Assert.Equal("CRITERIA.CODE_FORMAT_INVALID", error.Error.BusinessCode);
    }

    /// <summary>Q58 — mỗi đoạn tối đa 4 chữ số, kiểm ở MỌI đường ghi mã.</summary>
    [Fact]
    public void SetCode_Rejects_SegmentLongerThanFourDigits()
    {
        var error = Assert.Throws<DomainException>(() => Criteria.Create("4.12345", "x", AnyGroup, 10m));

        Assert.Equal("CRITERIA.CODE_SEGMENT_TOO_LONG", error.Error.BusinessCode);
    }

    /// <summary>
    /// Mã ba cấp phải HỢP LỆ. Dữ liệu thật của BA có 11 mã dạng này (4.22.1 … 4.22.11); một regex
    /// kiểu <c>^\d+\.\d+$</c> sẽ từ chối đúng 11 chỉ tiêu hợp lệ (§2).
    /// </summary>
    [Fact]
    public void SetCode_Accepts_ThreeLevelCode()
    {
        var criteria = Criteria.Create("4.22.11", "x", AnyGroup, 10m);

        Assert.Equal("4.22.11", criteria.Code);
    }

    /// <summary>
    /// Trạng thái so KHÔNG phân biệt hoa/thường nhưng CÓ phân biệt dấu (§4 luật 4). Bỏ dấu ở đây
    /// sẽ làm "Chưa thực hiện" và "Chua thuc hien" cùng khớp — hai chuỗi người dùng gõ với ý định
    /// khác nhau. Đây là chỗ Q47 CỐ Ý không áp.
    /// </summary>
    [Theory]
    [InlineData("đang thực hiện", true)]
    [InlineData("ĐANG THỰC HIỆN", true)]
    [InlineData("  Hoàn thành  ", true)]
    [InlineData("Dang thuc hien", false)]
    [InlineData("Chua thuc hien", false)]
    [InlineData("Đã xong", false)]
    public void AssessmentStatus_MatchesCaseInsensitively_ButDiacriticSensitively(string input, bool expected)
    {
        Assert.Equal(expected, AssessmentStatuses.TryResolve(input, out _));
    }

    /// <summary>Ngoài miền 0..100 thì KẸP về biên, KHÔNG báo lỗi (§3.2 — bảo vệ chiều sâu).</summary>
    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(70, 70)]
    [InlineData(140, 100)]
    public void ProgressPercent_IsClamped_NotRejected(int input, int expected)
    {
        var assessment = CriteriaAssessment.Create(Guid.CreateVersion7(), new DateOnly(2026, 8, 12));

        assessment.SetProgressPercent(input);

        Assert.Equal(expected, assessment.ProgressPercent);
    }

    /// <summary><c>TargetWeekEnd</c> phải là Chủ nhật — lưới an toàn thứ nhất của §1.5.</summary>
    [Fact]
    public void ImportJob_Rejects_TargetWeekEnd_ThatIsNotSunday()
    {
        var error = Assert.Throws<DomainException>(
            () => ImportJob.Create("a.csv", "csv", "/tmp/a.csv", new DateOnly(2026, 8, 12)));

        Assert.Equal("IMPORT_JOB.TARGET_WEEK_END_INVALID", error.Error.BusinessCode);

        // Ca dương: 16/08/2026 là Chủ nhật, kết tuần ISO 33.
        var job = ImportJob.Create("a.csv", "csv", "/tmp/a.csv", new DateOnly(2026, 8, 16));
        Assert.Equal(ImportJobStatuses.Pending, job.Status);
    }
}
