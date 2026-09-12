using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Persistence;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>
/// Đường GHI TAY của Danh mục DTI — DM-3 · DM-4 · DM-5 · DM-6.
///
/// <para><b>Đây là repository trả entity THEO DÕI ĐƯỢC</b> (trừ hai phép đọc đã đánh dấu). Lưới và
/// Dashboard đều <c>AsNoTracking</c> vì chúng chỉ đọc; đường ghi sửa thẳng lên entity rồi để handler
/// lưu một lần ở cuối. Thêm <c>AsNoTracking</c> vào đây "cho nhất quán" là làm mọi thay đổi biến
/// mất mà không có lỗi nào.</para>
/// </summary>
internal sealed class CriteriaWriteRepository(PlatformManagerDbContext db) : ICriteriaWriteRepository
{
    public async Task<CriteriaEntity?> GetTrackedAsync(Guid criteriaId, CancellationToken ct) =>
        await db.Set<CriteriaEntity>().FirstOrDefaultAsync(criteria => criteria.Id == criteriaId, ct);

    public async Task<CriteriaEntity?> FindByCodeAsync(string code, CancellationToken ct) =>
        // Global query filter đã lọc IsDeleted = false, nên phép kiểm trùng mã chỉ nhìn tập CHƯA
        // xoá mềm — khớp đúng unique index partial ở §1.4. Nhờ vậy xoá mềm một mã rồi tạo lại đúng
        // mã đó THÀNH CÔNG, thay vì nhận 409 cho một mã không ai còn thấy.
        await db.Set<CriteriaEntity>().FirstOrDefaultAsync(criteria => criteria.Code == code, ct);

    public async Task<bool> GroupExistsAsync(Guid groupId, CancellationToken ct) =>
        await db.Set<CriteriaGroup>().AsNoTracking().AnyAsync(group => group.Id == groupId, ct);

    public async Task<string?> FindGroupNameAsync(Guid groupId, CancellationToken ct) =>
        await db.Set<CriteriaGroup>().AsNoTracking()
            .Where(group => group.Id == groupId)
            .Select(group => group.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken ct) =>
        await db.Users.AsNoTracking().AnyAsync(user => user.Id == ownerId, ct);

    public async Task<CriteriaAssessment?> FindAssessmentInRangeAsync(
        Guid criteriaId, DateOnly from, DateOnly to, CancellationToken ct) =>
        await db.Set<CriteriaAssessment>()
            .Where(a => a.CriteriaId == criteriaId && a.AssessmentDate >= from && a.AssessmentDate <= to)
            // Bản ĐẠI DIỆN của kỳ = AssessmentDate lớn nhất trong khoảng (§5.2 + Q46). Phải khớp
            // đúng luật mà đường ĐỌC dùng, nếu không lời ghi sẽ sửa một bản ghi khác bản mà người
            // dùng đang nhìn.
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    /// <remarks><c>AsNoTracking</c> ở đây là ĐÚNG và cố ý: đây là nguồn ĐỌC của copy-forward, và
    /// đường ghi không bao giờ sửa bản ghi của kỳ khác.</remarks>
    public async Task<CriteriaAssessment?> FindLatestAssessmentBeforeAsync(
        Guid criteriaId, DateOnly before, CancellationToken ct) =>
        await db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(a => a.CriteriaId == criteriaId && a.AssessmentDate < before)
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// <c>IgnoreQueryFilters</c> có chủ đích — đếm CẢ bản ghi đã xoá mềm. "Đã từng có lịch sử" là
    /// câu hỏi về QUÁ KHỨ, và một bản ghi bị xoá mềm vẫn là bằng chứng rằng chỉ tiêu này từng được
    /// đánh giá. Bỏ qua chúng thì DM-5 sẽ chọn xoá CỨNG một chỉ tiêu còn dòng đánh giá trong bảng,
    /// và FK <c>Restrict</c> ném ra một lỗi hạ tầng thay cho một quyết định nghiệp vụ.
    /// </remarks>
    public async Task<bool> HasAnyAssessmentAsync(Guid criteriaId, CancellationToken ct) =>
        await db.Set<CriteriaAssessment>().AsNoTracking().IgnoreQueryFilters()
            .AnyAsync(a => a.CriteriaId == criteriaId, ct);

    public void Add(CriteriaEntity criteria) => db.Set<CriteriaEntity>().Add(criteria);

    public void AddAssessment(CriteriaAssessment assessment) => db.Set<CriteriaAssessment>().Add(assessment);

    public void Remove(CriteriaEntity criteria) => db.Set<CriteriaEntity>().Remove(criteria);

    public async Task<CriteriaRowDto?> FindRowAsync(
        Guid criteriaId, PeriodRange period, CancellationToken ct)
    {
        var row = await db.Set<CriteriaEntity>().AsNoTracking()
            .Where(criteria => criteria.Id == criteriaId)
            .Join(
                db.Set<CriteriaGroup>().AsNoTracking(),
                criteria => criteria.GroupId,
                group => group.Id,
                (criteria, group) => new CriteriaRowSource(
                    criteria.Id, criteria.Code, criteria.Name,
                    group.Id, group.Code, group.Name, criteria.MaxScore))
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return null;

        var from = period.Start;
        var to = period.End;

        var assessment = await db.Set<CriteriaAssessment>().AsNoTracking()
            .Where(a => a.CriteriaId == criteriaId && a.AssessmentDate >= from && a.AssessmentDate <= to)
            .OrderByDescending(a => a.AssessmentDate)
            .Select(a => new AssessmentSource(
                a.Id, a.CriteriaId, a.AssessmentDate, a.ProgressPercent, a.SelfScore, a.VerifiedScore,
                a.Status, a.OwnerId, a.Deadline, a.Note, a.Version))
            .FirstOrDefaultAsync(ct);

        var ownerNames = assessment?.OwnerId is { } ownerId
            ? await db.Users.AsNoTracking()
                .Where(user => user.Id == ownerId)
                .Select(user => new { user.Id, user.FullName })
                .ToDictionaryAsync(user => user.Id, user => user.FullName, ct)
            : [];

        // ĐÚNG bộ dựng mà lưới dùng — xem CriteriaRowMapper cho lý do nó phải là một chỗ.
        return CriteriaRowMapper.ToDto(row, assessment, ownerNames);
    }
}
