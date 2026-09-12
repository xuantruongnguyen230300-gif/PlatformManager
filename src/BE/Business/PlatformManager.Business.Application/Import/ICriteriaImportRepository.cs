using PlatformManager.Business.Domain.Entities;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.Application.Import;

/// <summary>Nhóm chỉ tiêu ở dạng tối thiểu mà đường nạp cần: khớp tên → lấy khoá.</summary>
public sealed record CriteriaGroupRef(Guid Id, string Name);

/// <summary>
/// Đọc/ghi dữ liệu DTI cho MỘT lượt nạp file.
///
/// <para><b>Mọi phép đọc đều theo LÔ, không theo dòng</b> — đó là toàn bộ lý do interface này tồn
/// tại thay vì dùng lại repository của lưới. Một file 62 dòng đi qua ba lời gọi
/// (<see cref="GetGroupsAsync"/> · <see cref="GetCriteriaByCodesAsync"/> ·
/// <see cref="GetAssessmentsInRangeAsync"/> + <see cref="GetLatestAssessmentsBeforeAsync"/>), số
/// truy vấn KHÔNG phụ thuộc số dòng. Tra từng dòng một là N+1 đúng nghĩa
/// (doc/huong_dan/quy-uoc/be-performance.md).</para>
///
/// <para><b>Entity trả về phải THEO DÕI ĐƯỢC</b> (trừ nguồn copy-forward): lượt nạp sửa thẳng lên
/// chúng rồi lưu một lần ở cuối. Đây là chỗ đường nạp khác hẳn đường đọc — lưới dùng
/// <c>AsNoTracking</c> ở mọi truy vấn.</para>
/// </summary>
public interface ICriteriaImportRepository
{
    /// <summary>
    /// Toàn bộ nhóm chưa xoá mềm. Danh mục ĐÓNG gồm 6 nhóm (§1.6) nên nạp hết một lần rẻ hơn tra
    /// theo tên từng dòng, và nó cũng là thứ cho phép so khớp tên không phân biệt hoa/thường mà
    /// không phải đẩy phép so đó xuống SQL.
    /// </summary>
    Task<IReadOnlyList<CriteriaGroupRef>> GetGroupsAsync(CancellationToken ct);

    /// <summary>
    /// Chỉ tiêu đang có, tra theo mã. So khớp mã là ORDINAL, phân biệt hoa/thường (§2) — mã chỉ
    /// gồm chữ số và dấu chấm nên không có gì để chuẩn hoá.
    /// </summary>
    Task<IReadOnlyList<CriteriaEntity>> GetCriteriaByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct);

    /// <summary>
    /// Bản ghi đánh giá ĐẠI DIỆN cho kỳ đích của từng chỉ tiêu — bản có <c>AssessmentDate</c> LỚN
    /// NHẤT nằm trong <paramref name="from"/>..<paramref name="to"/> (§5.2 + Q46).
    ///
    /// <para>Đây là "bước 2 mục 1" của luật ghi: kỳ đích ĐÃ CÓ bản ghi ⇒ cập nhật đúng bản ghi mà
    /// đường đọc sẽ đọc ra, và <c>AssessmentDate</c> của nó GIỮ NGUYÊN.</para>
    /// </summary>
    Task<IReadOnlyList<CriteriaAssessment>> GetAssessmentsInRangeAsync(
        IReadOnlyCollection<Guid> criteriaIds, DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>
    /// Bản ghi gần nhất TRƯỚC kỳ đích của từng chỉ tiêu — nguồn của phép copy-forward (§5.3 bước 2
    /// mục 2). Chỉ ĐỌC: lượt nạp không bao giờ sửa bản ghi của kỳ khác.
    /// </summary>
    /// <param name="before">Ngày đầu của kỳ đích; lấy bản ghi có <c>AssessmentDate</c> nhỏ hơn nó.</param>
    Task<IReadOnlyList<CriteriaAssessment>> GetLatestAssessmentsBeforeAsync(
        IReadOnlyCollection<Guid> criteriaIds, DateOnly before, CancellationToken ct);

    void AddCriteria(CriteriaEntity criteria);

    void AddAssessment(CriteriaAssessment assessment);
}
