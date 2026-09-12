using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Đường GHI TAY của Danh mục DTI — DM-3 (tạo) · DM-4 (dialog) · DM-5 (xoá) · DM-6 (sửa inline).
///
/// <para><b>Tách khỏi <c>ICriteriaGridRepository</c> có chủ đích:</b> lưới chỉ ĐỌC và
/// <c>AsNoTracking</c> ở mọi truy vấn; đường ghi cần entity THEO DÕI ĐƯỢC để sửa rồi lưu một lần ở
/// cuối. Gộp hai vai vào một interface là mời người sau thêm <c>AsNoTracking</c> "cho nhất quán",
/// và khi đó mọi thay đổi biến mất mà không có lỗi nào.</para>
///
/// <para><b>KHÔNG có <c>SaveChanges</c> ở đây</b> — handler own <c>SaveChanges</c>, đúng một lần, ở
/// cuối (doc/huong_dan/quy-uoc/be-cqrs-handler.md §Handler).</para>
/// </summary>
public interface ICriteriaWriteRepository
{
    /// <summary>Chỉ tiêu theo id, THEO DÕI ĐƯỢC. <c>null</c> nếu không có hoặc đã xoá mềm.</summary>
    Task<CriteriaEntity?> GetTrackedAsync(Guid criteriaId, CancellationToken ct);

    /// <summary>
    /// Chỉ tiêu mang <paramref name="code"/> trong tập CHƯA xoá mềm — dùng để kiểm trùng mã.
    /// So khớp ORDINAL, phân biệt hoa/thường (§2).
    ///
    /// <para>Trả về entity chứ không trả <c>bool</c>: đường sửa (DM-4) cần biết bản trùng đó có
    /// phải chính chỉ tiêu đang sửa không — đổi tên mà giữ nguyên mã thì KHÔNG phải trùng.</para>
    /// </summary>
    Task<CriteriaEntity?> FindByCodeAsync(string code, CancellationToken ct);

    Task<bool> GroupExistsAsync(Guid groupId, CancellationToken ct);

    /// <summary>Tên nhóm để dựng <c>CriteriaDto</c> của DM-3 mà không phải truy vấn lần hai.</summary>
    Task<string?> FindGroupNameAsync(Guid groupId, CancellationToken ct);

    /// <summary>Tài khoản có thật không — nguồn của <c>CRITERIA.OWNER_NOT_FOUND</c> (422).</summary>
    Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken ct);

    /// <summary>
    /// Bản ghi đánh giá ĐẠI DIỆN của kỳ đích (§5.2 — <c>AssessmentDate</c> lớn nhất trong khoảng),
    /// THEO DÕI ĐƯỢC. Đây là "bước 2 mục 1" của luật ghi.
    /// </summary>
    Task<CriteriaAssessment?> FindAssessmentInRangeAsync(
        Guid criteriaId, DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>
    /// Bản ghi gần nhất TRƯỚC kỳ đích — nguồn copy-forward. Chỉ ĐỌC: đường ghi không bao giờ sửa
    /// bản ghi của kỳ khác.
    /// </summary>
    Task<CriteriaAssessment?> FindLatestAssessmentBeforeAsync(
        Guid criteriaId, DateOnly before, CancellationToken ct);

    /// <summary>
    /// Chỉ tiêu này đã TỪNG có bản ghi đánh giá nào chưa — <b>mọi năm, mọi kỳ</b> (§5.5). Quyết
    /// định xoá cứng hay xoá mềm của DM-5 treo vào đúng câu hỏi này.
    ///
    /// <para>⚠️ Đếm cả bản ghi đã xoá mềm: "đã từng có lịch sử" là câu hỏi về QUÁ KHỨ, và một bản
    /// ghi bị xoá mềm vẫn là bằng chứng rằng chỉ tiêu này từng được đánh giá. Xoá cứng chỉ tiêu khi
    /// còn dòng đánh giá (dù đã xoá mềm) sẽ vấp FK <c>Restrict</c> — lỗi hạ tầng thay vì một quyết
    /// định nghiệp vụ.</para>
    /// </summary>
    Task<bool> HasAnyAssessmentAsync(Guid criteriaId, CancellationToken ct);

    void Add(CriteriaEntity criteria);

    void AddAssessment(CriteriaAssessment assessment);

    /// <summary>Xoá CỨNG — chỉ gọi khi <see cref="HasAnyAssessmentAsync"/> trả <c>false</c>.</summary>
    void Remove(CriteriaEntity criteria);

    /// <summary>
    /// Một dòng lưới của chỉ tiêu, đọc lại SAU khi ghi — response của DM-4 và DM-6 là cùng shape
    /// với một dòng của DM-2 để FE thay thẳng dòng trong lưới.
    /// </summary>
    /// <param name="period">
    /// KỲ ĐÍCH vừa ghi, không phải kỳ đang lọc trên màn. Nhờ vậy <c>assessmentPeriod</c> của phản
    /// hồi nói đúng nơi lời ghi vừa rơi vào — nghiệm thu Q26(c); thiếu nó thì FE không đổi được ô
    /// <c>Kỳ của số liệu</c> và Q31(b) hỏng trong im lặng.
    /// </param>
    Task<CriteriaRowDto?> FindRowAsync(Guid criteriaId, PeriodRange period, CancellationToken ct);
}
