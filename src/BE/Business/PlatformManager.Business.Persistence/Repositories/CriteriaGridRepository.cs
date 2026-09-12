using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>
/// Đọc lưới Danh mục DTI (DM-2). Chỉ ĐỌC — <c>AsNoTracking</c> ở mọi truy vấn theo
/// doc/huong_dan/quy-uoc/be-performance.md.
///
/// <para><b>Ba truy vấn, không phải N+1.</b> (1) đếm tổng khớp bộ lọc; (2) lấy đúng một trang
/// chỉ tiêu đã sắp; (3) lấy bản ghi đánh giá ĐẠI DIỆN cho các chỉ tiêu của trang đó, cộng một
/// truy vấn tên người phụ trách khi trang có ô <c>Phụ trách</c>. Số truy vấn KHÔNG phụ thuộc số
/// dòng.</para>
/// </summary>
internal sealed class CriteriaGridRepository(PlatformManagerDbContext db) : ICriteriaGridRepository
{
    public async Task<CriteriaGridPage> GetPageAsync(
        CriteriaFilterSpec spec, int page, int pageSize, CancellationToken ct)
    {
        var filtered = CriteriaQueries.Filtered(db, spec);

        // Đếm số CHỈ TIÊU, không đếm bản ghi đánh giá — ở mọi giá trị period, lưới trả đúng
        // 1 dòng / 1 chỉ tiêu (DM-2 mục 5).
        var totalCount = await filtered.CountAsync(ct);

        var rows = await CriteriaQueries.Sorted(filtered)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                db.Set<CriteriaGroup>().AsNoTracking(),
                criteria => criteria.GroupId,
                group => group.Id,
                (criteria, group) => new CriteriaRowSource(
                    criteria.Id,
                    criteria.Code,
                    criteria.Name,
                    group.Id,
                    group.Code,
                    group.Name,
                    criteria.MaxScore))
            .ToListAsync(ct);

        if (rows.Count == 0)
            return new CriteriaGridPage([], totalCount);

        var criteriaIds = rows.Select(row => row.CriteriaId).ToList();
        var representative = await LoadRepresentativeAsync(criteriaIds, spec.Period, ct);
        var ownerNames = await LoadOwnerNamesAsync(representative.Values, ct);

        var items = rows
            .Select(row => CriteriaRowMapper.ToDto(row, representative.GetValueOrDefault(row.CriteriaId), ownerNames))
            .ToList();

        return new CriteriaGridPage(items, totalCount);
    }

    /// <summary>
    /// Bản ghi đánh giá ĐẠI DIỆN của từng chỉ tiêu trong kỳ (§5.2 — <c>AssessmentDate</c> lớn
    /// nhất nằm trong khoảng ngày của kỳ).
    ///
    /// <para><b>Dựng bằng gộp-lấy-max rồi nối lại</b> thay vì <c>GroupBy().Select(g =&gt; g.First())</c>:
    /// khuôn <c>GROUP BY … MAX(...)</c> + <c>JOIN</c> là thứ mọi phiên bản EF/Npgsql đều dịch
    /// được sang một câu SQL, còn khuôn kia phụ thuộc vào khả năng sinh LATERAL của từng phiên
    /// bản. Ràng buộc unique partial trên cặp (CriteriaId, AssessmentDate) bảo đảm phép nối trả
    /// đúng MỘT dòng cho mỗi chỉ tiêu.</para>
    /// </summary>
    private async Task<Dictionary<Guid, AssessmentSource>> LoadRepresentativeAsync(
        IReadOnlyList<Guid> criteriaIds, PeriodRange period, CancellationToken ct)
    {
        var from = period.Start;
        var to = period.End;

        var inRange = db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(a => criteriaIds.Contains(a.CriteriaId) && a.AssessmentDate >= from && a.AssessmentDate <= to);

        var latestDates = inRange
            .GroupBy(a => a.CriteriaId)
            .Select(group => new { CriteriaId = group.Key, MaxDate = group.Max(a => a.AssessmentDate) });

        var rows = await inRange
            .Join(
                latestDates,
                assessment => new { assessment.CriteriaId, MaxDate = assessment.AssessmentDate },
                latest => new { latest.CriteriaId, latest.MaxDate },
                (assessment, _) => new AssessmentSource(
                    assessment.Id,
                    assessment.CriteriaId,
                    assessment.AssessmentDate,
                    assessment.ProgressPercent,
                    assessment.SelfScore,
                    assessment.VerifiedScore,
                    assessment.Status,
                    assessment.OwnerId,
                    assessment.Deadline,
                    assessment.Note,
                    assessment.Version))
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.CriteriaId);
    }

    /// <summary>
    /// <c>ownerId</c> → <c>FullName</c>. Đọc thẳng bảng <c>core."AspNetUsers"</c> qua CÙNG một
    /// <c>DbContext</c> — đó chính là thứ quyết định "một DbContext cho hai schema" mua được
    /// (doc/cau-truc-database.md §5.3).
    /// </summary>
    private async Task<Dictionary<Guid, string>> LoadOwnerNamesAsync(
        IEnumerable<AssessmentSource> assessments, CancellationToken ct)
    {
        var ownerIds = assessments
            .Where(assessment => assessment.OwnerId is not null)
            .Select(assessment => assessment.OwnerId!.Value)
            .Distinct()
            .ToList();

        if (ownerIds.Count == 0)
            return [];

        return await db.Users.AsNoTracking()
            .Where(user => ownerIds.Contains(user.Id))
            .Select(user => new { user.Id, user.FullName })
            .ToDictionaryAsync(user => user.Id, user => user.FullName, ct);
    }

}
