using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence;

/// <summary>
/// Seed danh mục TĨNH (CriteriaGroup/Criteria) từ doc/ERD/example_db_ver1.csv — DML, idempotent,
/// chỉ được gọi khi IsDevelopment() (xem Program.cs), CHẠY SAU CoreSeeder (cần role/user Core đã
/// tồn tại nếu sau này seed liên quan). KHÔNG seed CriteriaAssessment/CriteriaEvidence — đó là
/// việc của luồng Import CSV thật qua POST /api/import/csv (xem
/// Modules.DtiWeekly.Application/Assessments/CsvImportService.cs).
/// </summary>
public sealed class DtiWeeklySeeder(PlatformManagerDbContext db, ILogger<DtiWeeklySeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default) => await SeedCriteriaCatalogAsync(ct);

    private async Task SeedCriteriaCatalogAsync(CancellationToken ct)
    {
        if (await db.Set<CriteriaGroup>().IgnoreQueryFilters().AnyAsync(ct))
            return; // đã seed rồi — idempotent

        var csvPath = ResolveSampleCsvPath();
        if (csvPath is null)
        {
            logger.LogWarning("Không tìm thấy doc/ERD/example_db_ver1.csv — bỏ qua seed danh mục DTI.");
            return;
        }

        using var reader = new StreamReader(csvPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        var groupsByName = new Dictionary<string, CriteriaGroup>(StringComparer.OrdinalIgnoreCase);
        var groupDisplayOrder = 0;

        while (csv.Read())
        {
            var code = csv.TryGetField<string>("Mã", out var codeValue) ? codeValue?.Trim() : null;
            var name = csv.TryGetField<string>("Chỉ tiêu", out var nameValue) ? nameValue?.Trim() : null;
            var groupName = csv.TryGetField<string>("Nhóm", out var groupValue) ? groupValue?.Trim() : null;
            var maxScoreText = csv.TryGetField<string>("Điểm tối đa", out var maxScoreValue) ? maxScoreValue : null;

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(groupName)
                || !decimal.TryParse(maxScoreText, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxScore)
                || maxScore <= 0)
            {
                continue;
            }

            if (!groupsByName.TryGetValue(groupName, out var group))
            {
                groupDisplayOrder++;
                group = CriteriaGroup.Create(groupDisplayOrder.ToString(CultureInfo.InvariantCulture), groupName, groupDisplayOrder - 1);
                groupsByName[groupName] = group;
                await db.Set<CriteriaGroup>().AddAsync(group, ct);
            }

            var criteria = Criteria.Create(code, name ?? code, group.Id, maxScore);
            await db.Set<Criteria>().AddAsync(criteria, ct);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Đã seed {GroupCount} nhóm / {CriteriaCount} chỉ tiêu từ {CsvPath}.",
            groupsByName.Count, await db.Set<Criteria>().CountAsync(ct), csvPath);
    }

    /// <summary>Tìm doc/ERD/example_db_ver1.csv đi lên từ thư mục output (bin/) tới gốc repo.</summary>
    private static string? ResolveSampleCsvPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "doc", "ERD", "example_db_ver1.csv");
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }
}
