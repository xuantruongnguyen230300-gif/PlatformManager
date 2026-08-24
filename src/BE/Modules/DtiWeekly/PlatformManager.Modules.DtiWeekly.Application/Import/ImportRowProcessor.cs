using System.Globalization;
using System.Text;
using PlatformManager.Core.Application.Users;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;
using CriteriaFeature = PlatformManager.Modules.DtiWeekly.Application.Criteria;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>
/// PORT NGUYÊN VẸN từ CsvImportService.ImportAsync/ResolveOrCreateCriteriaAsync cũ — không đổi
/// 1 business rule nào (thứ tự resolve Criteria → owner → parse score/status/deadline →
/// upsert-trong-ngày → ghi đè evidence), chỉ đổi input từ CsvReader sang
/// IReadOnlyDictionary&lt;string,string?&gt; đã chuẩn hoá bởi IImportFileReader.
/// </summary>
public sealed class ImportRowProcessor(
    CriteriaFeature.ICriteriaRepository criteriaRepo,
    ICriteriaGroupRepository groupRepo,
    ICriteriaEvidenceRepository evidenceRepo,
    IAssessmentUpsertService upsertService,
    IUserLookupService userLookup) : IImportRowProcessor
{
    public async Task<ImportRowOutcome> ProcessRowAsync(IReadOnlyDictionary<string, string?> row, CancellationToken ct)
    {
        var code = GetField(row, ImportColumnNames.Code)?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Thiếu cột 'Mã'.");

        var (criteria, wasCreated, groupWasCreated) = await ResolveOrCreateCriteriaAsync(row, code, ct);

        Guid? ownerId = null;
        var ownerWasCreated = false;
        var ownerName = GetField(row, ImportColumnNames.Owner)?.Trim();
        if (!string.IsNullOrWhiteSpace(ownerName))
        {
            var (resolvedId, created) = await userLookup.ResolveOrCreateByFullNameAsync(ownerName, ct);
            ownerId = resolvedId;
            ownerWasCreated = created;
        }

        var selfScore = ParseNullableDecimal(GetField(row, ImportColumnNames.SelfScore));
        var verifiedScore = ParseNullableDecimal(GetField(row, ImportColumnNames.VerifiedScore));
        var status = NormalizeEmpty(GetField(row, ImportColumnNames.Status));
        var deadline = ParseNullableDate(GetField(row, ImportColumnNames.Deadline));

        var record = await upsertService.UpsertTodayAsync(criteria.Id, entity =>
        {
            entity.UpdateSelfAssessment(selfScore, verifiedScore);
            entity.UpdateStatus(status);
            entity.AssignOwner(ownerId);
            entity.SetDeadline(deadline);
        }, ct);

        await evidenceRepo.RemoveAllForAssessmentAsync(record.Id, ct);
        var evidenceContents = ParseEvidence(GetField(row, ImportColumnNames.Evidence));
        if (evidenceContents.Count > 0)
        {
            var evidenceEntities = evidenceContents
                .Select((content, idx) => CriteriaEvidence.Create(record.Id, content, idx));
            await evidenceRepo.AddRangeAsync(evidenceEntities, ct);
        }

        return new ImportRowOutcome(wasCreated, groupWasCreated, ownerWasCreated);
    }

    private async Task<(Domain.Entities.Criteria Criteria, bool WasCreated, bool GroupWasCreated)> ResolveOrCreateCriteriaAsync(
        IReadOnlyDictionary<string, string?> row, string code, CancellationToken ct)
    {
        var existing = await criteriaRepo.GetByCodeAsync(code, ct);
        if (existing is not null)
            return (existing, false, false);

        var name = GetField(row, ImportColumnNames.Name)?.Trim() ?? string.Empty;
        var groupName = GetField(row, ImportColumnNames.Group)?.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
            throw new InvalidOperationException($"Chỉ tiêu '{code}' chưa có trong danh mục — thiếu cột 'Nhóm' để tạo mới.");

        var maxScoreText = GetField(row, ImportColumnNames.MaxScore);
        if (!decimal.TryParse(maxScoreText, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxScore) || maxScore <= 0)
            throw new InvalidOperationException($"Chỉ tiêu '{code}' chưa có trong danh mục — 'Điểm tối đa' không hợp lệ (phải là số > 0).");

        var groupWasCreated = false;
        var group = await groupRepo.GetByNameAsync(groupName, ct);
        if (group is null)
        {
            var allGroups = await groupRepo.GetAllAsync(ct);
            var nextNumericCode = allGroups
                .Select(g => int.TryParse(g.Code, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            group = CriteriaGroup.Create(nextNumericCode.ToString(CultureInfo.InvariantCulture), groupName, allGroups.Count);
            await groupRepo.AddAsync(group, ct);
            groupWasCreated = true;
        }

        var criteria = Domain.Entities.Criteria.Create(code, name, group.Id, maxScore);
        await criteriaRepo.AddAsync(criteria, ct);

        return (criteria, true, groupWasCreated);
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> row, string columnName)
        => row.TryGetValue(columnName, out var value) ? value : null;

    private static string? NormalizeEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? ParseNullableDecimal(string? value)
        => !string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d
            : null;

    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    private static DateOnly? ParseNullableDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null; // định dạng không nhận diện được — bỏ qua, không chặn cả dòng import
    }

    /// <summary>Dòng bắt đầu bằng "*" = 1 minh chứng mới; dòng không bắt đầu bằng "*" = nối
    /// tiếp (thêm dấu cách) vào minh chứng trước đó — xem doc/ERD/ERD.md entity 5.</summary>
    private static List<string> ParseEvidence(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var lines = raw.Replace("\r\n", "\n").Split('\n');
        var items = new List<string>();
        var current = new StringBuilder();
        var hasCurrent = false;

        foreach (var rawLine in lines)
        {
            var trimmedStart = rawLine.TrimStart();
            if (trimmedStart.StartsWith('*'))
            {
                if (hasCurrent)
                    items.Add(current.ToString().Trim());

                current.Clear();
                current.Append(trimmedStart.TrimStart('*').Trim());
                hasCurrent = true;
            }
            else if (hasCurrent)
            {
                var continuation = rawLine.Trim();
                if (continuation.Length > 0)
                    current.Append(' ').Append(continuation);
            }
        }

        if (hasCurrent)
            items.Add(current.ToString().Trim());

        return items.Where(i => i.Length > 0).ToList();
    }
}
