using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;
using PlatformManager.Core.Application.Users;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;
using CriteriaFeature = PlatformManager.Modules.DtiWeekly.Application.Criteria;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// Cơ chế nhập liệu CHÍNH của tab "Đánh giá theo tuần" (xem
/// spec/danh-muc-dti/business-rules.md mục 2). Import GHI ĐÈ TOÀN BỘ field kể cả rỗng —
/// khác luồng sửa tay (partial-update qua UpdateCriteriaAssessmentCommand). Lỗi từng dòng
/// bắt riêng, không dừng cả batch.
/// </summary>
public sealed class CsvImportService(
    CriteriaFeature.ICriteriaRepository criteriaRepo,
    ICriteriaGroupRepository groupRepo,
    ICriteriaEvidenceRepository evidenceRepo,
    IAssessmentUpsertService upsertService,
    IUserLookupService userLookup,
    IUnitOfWork uow) : ICsvImportService
{
    private const string ColCode = "Mã";
    private const string ColName = "Chỉ tiêu";
    private const string ColGroup = "Nhóm";
    private const string ColMaxScore = "Điểm tối đa";
    private const string ColSelfScore = "Tự đánh giá";
    private const string ColVerifiedScore = "Thẩm định";
    private const string ColStatus = "Trạng thái";
    private const string ColOwner = "Phụ trách";
    private const string ColDeadline = "Hạn xử lý";
    private const string ColEvidence = "Minh chứng/Ghi chú";

    public async Task<ImportCsvResultDto> ImportAsync(Stream csvStream, CancellationToken ct)
    {
        var errors = new List<ImportRowErrorDto>();
        int totalRows = 0, successCount = 0, criteriaCreated = 0, groupsCreated = 0, ownersCreated = 0;

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var rowNumber = 1; // dòng 1 = header, dòng dữ liệu đầu tiên = dòng 2
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;
            totalRows++;
            var code = SafeGetField(csv, ColCode)?.Trim();

            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    throw new InvalidOperationException("Thiếu cột 'Mã'.");

                var (criteria, wasCreated, groupWasCreated) = await ResolveOrCreateCriteriaAsync(csv, code, ct);
                if (wasCreated) criteriaCreated++;
                if (groupWasCreated) groupsCreated++;

                Guid? ownerId = null;
                var ownerName = SafeGetField(csv, ColOwner)?.Trim();
                if (!string.IsNullOrWhiteSpace(ownerName))
                {
                    var (resolvedId, ownerWasCreated) = await userLookup.ResolveOrCreateByFullNameAsync(ownerName, ct);
                    ownerId = resolvedId;
                    if (ownerWasCreated) ownersCreated++;
                }

                var selfScore = ParseNullableDecimal(SafeGetField(csv, ColSelfScore));
                var verifiedScore = ParseNullableDecimal(SafeGetField(csv, ColVerifiedScore));
                var status = NormalizeEmpty(SafeGetField(csv, ColStatus));
                var deadline = ParseNullableDate(SafeGetField(csv, ColDeadline));

                var record = await upsertService.UpsertTodayAsync(criteria.Id, entity =>
                {
                    entity.UpdateSelfAssessment(selfScore, verifiedScore);
                    entity.UpdateStatus(status);
                    entity.AssignOwner(ownerId);
                    entity.SetDeadline(deadline);
                }, ct);

                await evidenceRepo.RemoveAllForAssessmentAsync(record.Id, ct);
                var evidenceContents = ParseEvidence(SafeGetField(csv, ColEvidence));
                if (evidenceContents.Count > 0)
                {
                    var evidenceEntities = evidenceContents
                        .Select((content, idx) => CriteriaEvidence.Create(record.Id, content, idx));
                    await evidenceRepo.AddRangeAsync(evidenceEntities, ct);
                }

                await uow.SaveChangesAsync(ct);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowErrorDto(rowNumber, code, ex.Message));
                uow.DiscardTrackedChanges();
            }
        }

        return new ImportCsvResultDto
        {
            TotalRows = totalRows,
            SuccessCount = successCount,
            ErrorCount = errors.Count,
            CriteriaCreatedCount = criteriaCreated,
            GroupsCreatedCount = groupsCreated,
            OwnersCreatedCount = ownersCreated,
            Errors = errors,
        };
    }

    private async Task<(Domain.Entities.Criteria Criteria, bool WasCreated, bool GroupWasCreated)> ResolveOrCreateCriteriaAsync(
        CsvReader csv, string code, CancellationToken ct)
    {
        var existing = await criteriaRepo.GetByCodeAsync(code, ct);
        if (existing is not null)
            return (existing, false, false);

        var name = SafeGetField(csv, ColName)?.Trim() ?? string.Empty;
        var groupName = SafeGetField(csv, ColGroup)?.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
            throw new InvalidOperationException($"Chỉ tiêu '{code}' chưa có trong danh mục — thiếu cột 'Nhóm' để tạo mới.");

        var maxScoreText = SafeGetField(csv, ColMaxScore);
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

    private static string? SafeGetField(CsvReader csv, string columnName)
        => csv.TryGetField<string>(columnName, out var value) ? value : null;

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
