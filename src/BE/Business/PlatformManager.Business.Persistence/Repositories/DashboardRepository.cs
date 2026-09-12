using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>
/// Đọc dữ liệu thô cho Dashboard (DB-1, DB-3). Chỉ đọc — <c>AsNoTracking</c> ở mọi truy vấn.
///
/// <para><b>KHÔNG cache, có chủ đích</b> (Q70): dữ liệu đằng sau các endpoint này đổi ngay sau
/// mỗi lần sửa inline và mỗi lần import, mà cả hai đều là thao tác người dùng làm rồi QUAY LẠI
/// XEM NGAY. Một bản cache dù chỉ vài chục giây cũng biến "số tôi vừa nhập đi đâu mất" thành một
/// báo lỗi không tái hiện được. Ngưỡng xem lại: có SỐ ĐO của một truy vấn chậm thật.</para>
/// </summary>
internal sealed class DashboardRepository(PlatformManagerDbContext db) : IDashboardRepository
{
    public async Task<IReadOnlyList<CriteriaFact>> GetCriteriaAsync(CancellationToken ct) =>
        await db.Set<Criteria>().AsNoTracking()
            .Join(
                db.Set<CriteriaGroup>().AsNoTracking(),
                criteria => criteria.GroupId,
                group => group.Id,
                (criteria, group) => new CriteriaFact(
                    criteria.Id,
                    criteria.Code,
                    criteria.CodeSortKey,
                    criteria.Name,
                    group.Id,
                    group.Code,
                    group.Name,
                    group.DisplayOrder,
                    criteria.MaxScore))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssessmentFact>> GetAssessmentFactsAsync(
        DateOnly from, DateOnly to, CancellationToken ct) =>
        await db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(assessment => assessment.AssessmentDate >= from && assessment.AssessmentDate <= to)
            // Phép nối này KHÔNG thừa dù cả hai bảng đã có global query filter soft-delete: nó
            // loại bản ghi đánh giá của chỉ tiêu ĐÃ XOÁ MỀM. Chỉ tiêu đã xoá mềm phải biến khỏi
            // MỌI phép tổng hợp, kể cả khi xem lại kỳ cũ mà lúc đó nó còn sống (§5.5) — nếu
            // không, tổng của một kỳ sẽ đổi tuỳ thời điểm người ta mở màn hình lên xem.
            .Join(
                db.Set<Criteria>().AsNoTracking(),
                assessment => assessment.CriteriaId,
                criteria => criteria.Id,
                (assessment, _) => new AssessmentFact(
                    assessment.CriteriaId,
                    assessment.AssessmentDate,
                    assessment.ProgressPercent,
                    assessment.Status))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetYearsWithDataAsync(CancellationToken ct) =>
        await db.Set<CriteriaAssessment>().AsNoTracking()
            .Join(
                db.Set<Criteria>().AsNoTracking(),
                assessment => assessment.CriteriaId,
                criteria => criteria.Id,
                (assessment, _) => assessment.AssessmentDate.Year)
            .Distinct()
            .OrderBy(year => year)
            .ToListAsync(ct);

    /// <summary>
    /// Bảng chi tiết 9 cột. Dùng CHUNG <see cref="CriteriaQueries"/> với DM-2 — hai đường chỉ
    /// khác phép <c>Select</c> (Q68).
    /// </summary>
    public async Task<IReadOnlyList<DashboardTableRowDto>> GetTableAsync(
        CriteriaFilterSpec spec, CancellationToken ct)
    {
        var from = spec.Period.Start;
        var to = spec.Period.End;

        var rows = await CriteriaQueries.Sorted(CriteriaQueries.Filtered(db, spec))
            .Join(
                db.Set<CriteriaGroup>().AsNoTracking(),
                criteria => criteria.GroupId,
                group => group.Id,
                (criteria, group) => new { Criteria = criteria, Group = group })
            .ToListAsync(ct);

        if (rows.Count == 0)
            return [];

        var criteriaIds = rows.Select(row => row.Criteria.Id).ToList();

        // Cùng khuôn "gộp-lấy-max rồi nối lại" với CriteriaGridRepository — lý do đầy đủ ghi ở đó.
        var inRange = db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(a => criteriaIds.Contains(a.CriteriaId) && a.AssessmentDate >= from && a.AssessmentDate <= to);

        var latestDates = inRange
            .GroupBy(a => a.CriteriaId)
            .Select(group => new { CriteriaId = group.Key, MaxDate = group.Max(a => a.AssessmentDate) });

        var representative = await inRange
            .Join(
                latestDates,
                assessment => new { assessment.CriteriaId, MaxDate = assessment.AssessmentDate },
                latest => new { latest.CriteriaId, latest.MaxDate },
                (assessment, _) => new
                {
                    assessment.CriteriaId,
                    assessment.SelfScore,
                    assessment.VerifiedScore,
                    assessment.Status,
                    assessment.Note,
                })
            .ToDictionaryAsync(assessment => assessment.CriteriaId, ct);

        return
        [
            .. rows.Select(row =>
            {
                var assessment = representative.GetValueOrDefault(row.Criteria.Id);

                return new DashboardTableRowDto(
                    row.Criteria.Id,
                    row.Criteria.Code,
                    row.Criteria.Name,
                    row.Group.Id,
                    row.Group.Code,
                    row.Group.Name,
                    row.Criteria.MaxScore,
                    assessment?.SelfScore,
                    assessment?.VerifiedScore,
                    DashboardCalculator.Diff(assessment?.SelfScore, assessment?.VerifiedScore),
                    assessment?.Status,
                    assessment?.Note);
            }),
        ];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Đi qua ĐÚNG <c>CriteriaQueries.Filtered</c>/<c>Sorted</c> mà <see cref="GetTableAsync"/>
    /// dùng — một chỗ lọc, hai phép <c>Select</c> (Q68). Viết lại điều kiện lọc ở đây là mở đúng
    /// khe hở mà kiểm "số dòng file = số phần tử table" sinh ra để bịt.
    /// </remarks>
    public async Task<IReadOnlyList<DashboardExportRow>> GetExportRowsAsync(
        CriteriaFilterSpec spec, CancellationToken ct)
    {
        var from = spec.Period.Start;
        var to = spec.Period.End;

        var rows = await CriteriaQueries.Sorted(CriteriaQueries.Filtered(db, spec))
            .Join(
                db.Set<CriteriaGroup>().AsNoTracking(),
                criteria => criteria.GroupId,
                group => group.Id,
                (criteria, group) => new { Criteria = criteria, Group = group })
            .ToListAsync(ct);

        if (rows.Count == 0)
            return [];

        var criteriaIds = rows.Select(row => row.Criteria.Id).ToList();

        // Cùng khuôn "gộp-lấy-max rồi nối lại" với GetTableAsync — và cùng luật chọn bản đại diện
        // (§5.2 + Q46). Hai đường KHÁC nhau ở bộ trường chiếu ra, KHÔNG ở luật.
        var inRange = db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(a => criteriaIds.Contains(a.CriteriaId) && a.AssessmentDate >= from && a.AssessmentDate <= to);

        var latestDates = inRange
            .GroupBy(a => a.CriteriaId)
            .Select(group => new { CriteriaId = group.Key, MaxDate = group.Max(a => a.AssessmentDate) });

        var representative = await inRange
            .Join(
                latestDates,
                assessment => new { assessment.CriteriaId, MaxDate = assessment.AssessmentDate },
                latest => new { latest.CriteriaId, latest.MaxDate },
                (assessment, _) => new
                {
                    assessment.CriteriaId,
                    assessment.SelfScore,
                    assessment.VerifiedScore,
                    assessment.Status,
                    assessment.OwnerId,
                    assessment.Deadline,
                    assessment.ProgressPercent,
                    assessment.Note,
                })
            .ToDictionaryAsync(assessment => assessment.CriteriaId, ct);

        // Tên người phụ trách: MỘT truy vấn cho cả file, không phải một truy vấn cho mỗi dòng. Đây
        // đúng là chi phí mà Q68 không muốn bắt DB-1 gánh — màn hình không có cột nào hiện nó.
        var ownerIds = representative.Values
            .Where(assessment => assessment.OwnerId is not null)
            .Select(assessment => assessment.OwnerId!.Value)
            .Distinct()
            .ToList();

        var ownerNames = ownerIds.Count == 0
            ? []
            : await db.Users.AsNoTracking()
                .Where(user => ownerIds.Contains(user.Id))
                .Select(user => new { user.Id, user.FullName })
                .ToDictionaryAsync(user => user.Id, user => user.FullName, ct);

        return
        [
            .. rows.Select(row =>
            {
                var assessment = representative.GetValueOrDefault(row.Criteria.Id);

                return new DashboardExportRow(
                    row.Criteria.Code,
                    row.Criteria.Name,

                    // Cột `Nhóm` ghi TÊN TRẦN (Q55), KHÔNG phải dạng `Code. Name` của màn hình:
                    // import khớp nhóm theo đúng chuỗi tên, nên một ô "1. Hạ tầng và Nền tảng số"
                    // nạp ngược lại sẽ không khớp nhóm nào — gãy đúng round-trip mà 12 cột bảo vệ.
                    row.Group.Name,

                    row.Criteria.MaxScore,
                    assessment?.SelfScore,
                    assessment?.VerifiedScore,
                    DashboardCalculator.Diff(assessment?.SelfScore, assessment?.VerifiedScore),
                    assessment?.Status,
                    assessment?.OwnerId is { } ownerId ? ownerNames.GetValueOrDefault(ownerId) : null,
                    assessment?.Deadline,
                    assessment?.ProgressPercent,
                    assessment?.Note);
            }),
        ];
    }

    public async Task<string?> FindGroupLabelAsync(Guid groupId, CancellationToken ct) =>
        await db.Set<CriteriaGroup>().AsNoTracking()
            .Where(group => group.Id == groupId)
            // `Code. Name` — dạng HIỂN THỊ của Q42, đúng cho một dòng mô tả bộ lọc.
            .Select(group => group.Code + ". " + group.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<string?> FindUserFullNameAsync(Guid userId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.FullName)
            .FirstOrDefaultAsync(ct);
}
