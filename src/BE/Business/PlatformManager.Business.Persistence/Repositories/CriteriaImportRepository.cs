using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>
/// Đọc/ghi dữ liệu DTI cho một lượt nạp file — DM-7.
///
/// <para><b>Đây là repository DUY NHẤT của cụm trả entity THEO DÕI ĐƯỢC</b> (trừ nguồn
/// copy-forward). Lưới và Dashboard đều <c>AsNoTracking</c> vì chúng chỉ đọc; lượt nạp thì sửa
/// thẳng lên entity rồi để handler lưu MỘT lần ở cuối (Q64). Đánh dấu khác biệt đó ngay ở đây để
/// không ai "dọn cho nhất quán" bằng cách thêm <c>AsNoTracking</c> — thêm vào là mọi thay đổi của
/// lượt nạp biến mất mà không có lỗi nào.</para>
/// </summary>
internal sealed class CriteriaImportRepository(PlatformManagerDbContext db) : ICriteriaImportRepository
{
    public async Task<IReadOnlyList<CriteriaGroupRef>> GetGroupsAsync(CancellationToken ct) =>
        await db.Set<CriteriaGroup>().AsNoTracking()
            .Select(group => new CriteriaGroupRef(group.Id, group.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Criteria>> GetCriteriaByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken ct)
    {
        if (codes.Count == 0)
            return [];

        // Tham số hoá bằng một danh sách (EF dịch thành `= ANY(@codes)` trên Npgsql) — một truy vấn
        // cho cả file, không phải một truy vấn cho mỗi dòng.
        var list = codes.ToList();

        return await db.Set<Criteria>()
            .Where(criteria => list.Contains(criteria.Code))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dựng bằng GỘP-LẤY-MAX rồi NỐI LẠI, cùng khuôn với <see cref="CriteriaGridRepository"/>:
    /// <c>GROUP BY … MAX(...)</c> + <c>JOIN</c> là thứ mọi phiên bản EF/Npgsql đều dịch được sang
    /// một câu SQL, còn <c>GroupBy().Select(g =&gt; g.First())</c> thì phụ thuộc khả năng sinh
    /// LATERAL của từng phiên bản. Ràng buộc unique partial trên cặp
    /// <c>(CriteriaId, AssessmentDate)</c> bảo đảm phép nối trả đúng MỘT dòng cho mỗi chỉ tiêu.
    /// </remarks>
    public async Task<IReadOnlyList<CriteriaAssessment>> GetAssessmentsInRangeAsync(
        IReadOnlyCollection<Guid> criteriaIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (criteriaIds.Count == 0)
            return [];

        var ids = criteriaIds.ToList();

        var inRange = db.Set<CriteriaAssessment>()
            .Where(a => ids.Contains(a.CriteriaId) && a.AssessmentDate >= from && a.AssessmentDate <= to);

        return await JoinLatestAsync(inRange, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <c>AsNoTracking</c> ở ĐÂY là đúng và cố ý: đây là nguồn ĐỌC của phép copy-forward. Lượt nạp
    /// không bao giờ sửa bản ghi của kỳ khác — theo dõi chúng chỉ mở cửa cho một lần
    /// <c>SaveChanges</c> vô tình ghi vào một kỳ đã chốt số liệu.
    /// </remarks>
    public async Task<IReadOnlyList<CriteriaAssessment>> GetLatestAssessmentsBeforeAsync(
        IReadOnlyCollection<Guid> criteriaIds, DateOnly before, CancellationToken ct)
    {
        if (criteriaIds.Count == 0)
            return [];

        var ids = criteriaIds.ToList();

        var earlier = db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(a => ids.Contains(a.CriteriaId) && a.AssessmentDate < before);

        return await JoinLatestAsync(earlier, ct);
    }

    public void AddCriteria(Criteria criteria) => db.Set<Criteria>().Add(criteria);

    public void AddAssessment(CriteriaAssessment assessment) => db.Set<CriteriaAssessment>().Add(assessment);

    /// <summary>
    /// Lấy đúng MỘT bản ghi cho mỗi <c>CriteriaId</c> — bản có <c>AssessmentDate</c> lớn nhất trong
    /// tập <paramref name="scope"/>. Soft-delete KHÔNG lọc ở đây: global query filter của
    /// <c>PlatformManagerDbContext</c> đã phủ mọi <c>BaseEntity</c>, và lặp lại là tạo nguồn sự
    /// thật thứ hai cho cùng một luật.
    /// </summary>
    private static async Task<IReadOnlyList<CriteriaAssessment>> JoinLatestAsync(
        IQueryable<CriteriaAssessment> scope, CancellationToken ct)
    {
        var latestDates = scope
            .GroupBy(a => a.CriteriaId)
            .Select(group => new { CriteriaId = group.Key, MaxDate = group.Max(a => a.AssessmentDate) });

        return await scope
            .Join(
                latestDates,
                assessment => new { assessment.CriteriaId, MaxDate = assessment.AssessmentDate },
                latest => new { latest.CriteriaId, latest.MaxDate },
                (assessment, _) => assessment)
            .ToListAsync(ct);
    }
}
