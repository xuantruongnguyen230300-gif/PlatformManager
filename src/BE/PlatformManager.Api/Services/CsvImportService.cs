using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Data;
using PlatformManager.Api.Dtos;
using PlatformManager.Api.Entities;

namespace PlatformManager.Api.Services;

/// <summary>
/// Import file CSV theo mẫu doc/ERD/example_db_ver1.csv — mapping cột theo
/// spec/danh-muc-dti/business-rules.md mục 2.2. Mỗi dòng đi qua
/// <see cref="AssessmentUpsertService"/> (upsert-trong-ngày + copy-forward).
/// </summary>
public class CsvImportService(AppDbContext db, AssessmentUpsertService upsertService)
{
    public async Task<CsvImportResultDto> ImportAsync(Stream csvStream, CancellationToken ct)
    {
        var result = new CsvImportResultDto();

        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            DetectDelimiter = false,
        };
        using var csv = new CsvReader(reader, csvConfig);

        if (!await csv.ReadAsync())
            return result;
        csv.ReadHeader();

        var groups = await db.CriteriaGroups.AsNoTracking().ToListAsync(ct);
        var groupByName = groups.ToDictionary(g => g.Name.Trim(), g => g, StringComparer.OrdinalIgnoreCase);

        var criteriaList = await db.Criteria.ToListAsync(ct); // tracked — cần cho case tạo mới
        var criteriaByCode = criteriaList.ToDictionary(c => c.Code.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

        var users = await db.AppUsers.AsNoTracking().ToListAsync(ct);
        var userByName = users
            .GroupBy(u => u.FullName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count() == 1 ? g.First() : null, StringComparer.OrdinalIgnoreCase);

        var rowNumber = 1; // dòng 1 = header
        while (await csv.ReadAsync())
        {
            rowNumber++;
            result.TotalRows++;
            try
            {
                var code = csv.GetField("Mã")?.Trim();
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new CsvImportRowErrorDto { RowNumber = rowNumber, Message = "Thiếu cột 'Mã'." });
                    result.ErrorCount++;
                    continue;
                }

                var name = csv.GetField("Chỉ tiêu")?.Trim();
                var groupName = csv.GetField("Nhóm")?.Trim();
                var maxScoreRaw = csv.GetField("Điểm tối đa");
                var selfScoreRaw = csv.GetField("Tự đánh giá");
                var verifiedScoreRaw = csv.GetField("Thẩm định");
                var status = csv.GetField("Trạng thái")?.Trim();
                var ownerRaw = csv.GetField("Phụ trách")?.Trim();
                var deadlineRaw = csv.GetField("Hạn xử lý")?.Trim();
                var evidenceRaw = csv.GetField("Minh chứng/Ghi chú");

                if (!criteriaByCode.TryGetValue(code, out var criteria))
                {
                    // Code lạ -> tự động tạo Criteria mới (đã chốt, xem business-rules mục 2.2 câu #5)
                    if (string.IsNullOrWhiteSpace(groupName) || !groupByName.TryGetValue(groupName, out var group))
                    {
                        result.Errors.Add(new CsvImportRowErrorDto
                        {
                            RowNumber = rowNumber,
                            Code = code,
                            Message = $"Nhóm '{groupName}' không khớp CriteriaGroup nào đã seed — không tự tạo nhóm mới (Code lạ bị bỏ qua).",
                        });
                        result.ErrorCount++;
                        continue;
                    }
                    if (!TryParseDecimal(maxScoreRaw, out var maxScore) || maxScore <= 0)
                    {
                        result.Errors.Add(new CsvImportRowErrorDto
                        {
                            RowNumber = rowNumber,
                            Code = code,
                            Message = "'Điểm tối đa' không hợp lệ (phải > 0) khi tạo chỉ tiêu mới.",
                        });
                        result.ErrorCount++;
                        continue;
                    }

                    criteria = new Criteria
                    {
                        Id = Guid.NewGuid(),
                        Code = code,
                        Name = string.IsNullOrWhiteSpace(name) ? code : name,
                        GroupId = group.Id,
                        MaxScore = maxScore,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };
                    db.Criteria.Add(criteria);
                    criteriaByCode[code] = criteria;
                    result.CriteriaCreatedCount++;
                    // Flush ngay để các query trực tiếp DB bên trong AssessmentUpsertService (baseline == null
                    // -> seed ProgressPercent) nhìn thấy Criteria vừa tạo.
                    await db.SaveChangesAsync(ct);
                }

                var selfScore = TryParseDecimal(selfScoreRaw, out var ss) ? ss : (decimal?)null;
                var verifiedScore = TryParseDecimal(verifiedScoreRaw, out var vs) ? vs : (decimal?)null;

                Guid? ownerId = null;
                if (!string.IsNullOrWhiteSpace(ownerRaw))
                {
                    // Giả định đã tự quyết định (không chặn): match chính xác theo AppUsers.FullName;
                    // không khớp hoặc trùng tên nhiều user -> để OwnerId=null, KHÔNG tự tạo AppUser mới.
                    if (userByName.TryGetValue(ownerRaw, out var matchedUser) && matchedUser != null)
                        ownerId = matchedUser.Id;
                }

                DateOnly? deadline = DateOnly.TryParse(deadlineRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
                var evidenceContents = ParseEvidenceLines(evidenceRaw);

                // Import GHI ĐÈ TOÀN BỘ 5 field (kể cả rỗng) — đã chốt, xem business-rules mục 2.2.
                var assessment = await upsertService.UpsertTodayAsync(criteria.Id, a =>
                {
                    a.SelfScore = selfScore;
                    a.VerifiedScore = verifiedScore;
                    a.Status = status;
                    a.OwnerId = ownerId;
                    a.Deadline = deadline;
                }, ct);

                // CriteriaEvidence KHÔNG copy-forward — ghi đè danh sách minh chứng của đúng record hôm nay.
                var oldEvidences = await db.CriteriaEvidences
                    .Where(e => e.CriteriaAssessmentId == assessment.Id && !e.IsDeleted)
                    .ToListAsync(ct);
                db.CriteriaEvidences.RemoveRange(oldEvidences);
                for (var i = 0; i < evidenceContents.Count; i++)
                {
                    db.CriteriaEvidences.Add(new CriteriaEvidence
                    {
                        Id = Guid.NewGuid(),
                        CriteriaAssessmentId = assessment.Id,
                        Content = evidenceContents[i],
                        OrderIndex = i,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                }
                await db.SaveChangesAsync(ct);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new CsvImportRowErrorDto { RowNumber = rowNumber, Message = ex.Message });
                result.ErrorCount++;
            }
        }

        return result;
    }

    private static bool TryParseDecimal(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return decimal.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Tách cột "Minh chứng/Ghi chú" thành các minh chứng theo dòng bắt đầu bằng "*".
    /// Dòng KHÔNG bắt đầu bằng "*" (do nội dung dài tự xuống dòng) được nối tiếp vào
    /// minh chứng gần nhất — xem doc/ERD/ERD.md mục 5 "Lưu ý khi import CSV".
    /// </summary>
    private static List<string> ParseEvidenceLines(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>();
        var current = new StringBuilder();
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith('*'))
            {
                if (current.Length > 0) result.Add(current.ToString().Trim());
                current.Clear();
                current.Append(line.TrimStart('*').Trim());
            }
            else if (line.Length > 0)
            {
                if (current.Length > 0) current.Append(' ');
                current.Append(line);
            }
        }
        if (current.Length > 0) result.Add(current.ToString().Trim());
        return result.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }
}
