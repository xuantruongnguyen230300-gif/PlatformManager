using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Entities;

namespace PlatformManager.Api.Data;

/// <summary>
/// Seed 6 CriteriaGroup (tĩnh, theo đúng tên xuất hiện trong doc/ERD/example_db_ver1.csv
/// cột "Nhóm" — PHẢI khớp chính xác để CsvImportService resolve được nhóm khi import lại)
/// và 62 Criteria đọc trực tiếp từ chính file CSV mẫu đó (tránh chép tay có thể sai sót).
/// KHÔNG seed CriteriaAssessment nào — bắt đầu trống, người dùng Import CSV thật hoặc
/// nhập tay để test (xem yêu cầu gốc).
/// </summary>
public static class DbSeeder
{
    // Thứ tự xuất hiện đầu tiên trong DTI_ITEMS/CSV — xem doc/ERD/ERD.md mục 1.
    private static readonly (string Code, string Name)[] Groups =
    [
        ("1", "Hạ tầng và Nền tảng số"),
        ("2", "Nhân lực số"),
        ("3", "An toàn thông tin, an ninh mạng"),
        ("4", "Hoạt động chính quyền số"),
        ("5", "Hoạt động Kinh tế số"),
        ("6", "Hoạt động Xã hội số"),
    ];

    public static async Task SeedAsync(AppDbContext db, string contentRootPath, ILogger logger, CancellationToken ct = default)
    {
        if (!await db.CriteriaGroups.IgnoreQueryFilters().AnyAsync(ct))
        {
            for (var i = 0; i < Groups.Length; i++)
            {
                db.CriteriaGroups.Add(new CriteriaGroup
                {
                    Id = Guid.NewGuid(),
                    Code = Groups[i].Code,
                    Name = Groups[i].Name,
                    DisplayOrder = i + 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
            }
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} CriteriaGroups.", Groups.Length);
        }

        if (await db.Criteria.IgnoreQueryFilters().AnyAsync(ct))
            return; // đã seed rồi

        var csvPath = FindCsvPath(contentRootPath);
        if (csvPath is null)
        {
            logger.LogWarning("Không tìm thấy doc/ERD/example_db_ver1.csv để seed Criteria — bỏ qua seed 62 chỉ tiêu.");
            return;
        }

        var groupByName = (await db.CriteriaGroups.AsNoTracking().ToListAsync(ct))
            .ToDictionary(g => g.Name.Trim(), g => g, StringComparer.OrdinalIgnoreCase);

        using var streamReader = new StreamReader(csvPath, System.Text.Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(streamReader, config);
        if (!await csv.ReadAsync()) return;
        csv.ReadHeader();

        var seeded = 0;
        while (await csv.ReadAsync())
        {
            var code = csv.GetField("Mã")?.Trim();
            var name = csv.GetField("Chỉ tiêu")?.Trim();
            var groupName = csv.GetField("Nhóm")?.Trim();
            var maxScoreRaw = csv.GetField("Điểm tối đa");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(groupName)) continue;
            if (!groupByName.TryGetValue(groupName, out var group))
            {
                logger.LogWarning("Seed: bỏ qua mã {Code} vì nhóm '{Group}' không khớp CriteriaGroup nào.", code, groupName);
                continue;
            }
            if (!decimal.TryParse(maxScoreRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxScore) || maxScore <= 0)
            {
                logger.LogWarning("Seed: bỏ qua mã {Code} vì 'Điểm tối đa' không hợp lệ.", code);
                continue;
            }

            db.Criteria.Add(new Criteria
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = string.IsNullOrWhiteSpace(name) ? code : name,
                GroupId = group.Id,
                MaxScore = maxScore,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            seeded++;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} Criteria từ {Path}.", seeded, csvPath);
    }

    private static string? FindCsvPath(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, "doc", "ERD", "example_db_ver1.csv");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
