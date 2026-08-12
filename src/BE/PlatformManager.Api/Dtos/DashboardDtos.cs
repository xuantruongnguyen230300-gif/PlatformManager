namespace PlatformManager.Api.Dtos;

public class DashboardResponseDto
{
    public string Mode { get; set; } = string.Empty; // week|month|year
    public string PeriodLabel { get; set; } = string.Empty;
    public DashboardKpiDto Kpi { get; set; } = new();
    public List<DashboardGroupProgressDto> Groups { get; set; } = [];
    public List<DashboardTrendPointDto> Trend { get; set; } = [];
    public List<DashboardTableRowDto> Table { get; set; } = [];
}

public class DashboardKpiDto
{
    /// <summary>Tiến độ chung của kỳ đang xem — null = "chưa có dữ liệu".</summary>
    public decimal? OverallProgress { get; set; }
    /// <summary>Delta so với kỳ liền trước — null nếu 1 trong 2 kỳ thiếu dữ liệu.</summary>
    public decimal? Delta { get; set; }
    public string? PreviousPeriodLabel { get; set; }
    public int Up { get; set; }
    public int Flat { get; set; }
    public int Down { get; set; }
    public int Done { get; set; }
    public int TotalCriteria { get; set; }
}

public class DashboardGroupProgressDto
{
    public Guid GroupId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    /// <summary>null = "chưa có dữ liệu" cho nhóm này trong kỳ đang xem.</summary>
    public decimal? Progress { get; set; }
}

public class DashboardTrendPointDto
{
    public string Label { get; set; } = string.Empty; // vd "2026-W07" hoặc "Th.3"
    public decimal? Value { get; set; }
}

public class DashboardTableRowDto
{
    public Guid CriteriaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal? PreviousValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? Delta { get; set; }
    /// <summary>Badge tự tính (KHÔNG phải field Status lưu DB) — "Hoàn thành"|"Không tăng"|"Đang thực hiện"|"Chưa có dữ liệu".</summary>
    public string Badge { get; set; } = string.Empty;
}

public class ReportResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
}

/// <summary>
/// Danh sách năm/kỳ-tuần/kỳ-tháng có dữ liệu — phục vụ dropdown lọc ở cả 2 màn
/// (Dashboard, Danh mục DTI). KHÔNG có trong endpoint list gốc của yêu cầu ban
/// đầu — bổ sung vì cả 2 màn đều cần bộ lọc Năm/Tuần/Tháng (đã chốt ở
/// doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm)") mà không có cách nào suy ra phía
/// FE nếu không có endpoint này.
/// </summary>
public class PeriodOptionsDto
{
    /// <summary>Mọi năm có ít nhất 1 CriteriaAssessment, luôn kèm năm hiện tại dù chưa có data.</summary>
    public List<int> Years { get; set; } = [];
    /// <summary>Chỉ trả khi query có Year — các kỳ-tuần có dữ liệu trong năm đó, sort giảm dần.</summary>
    public List<PeriodOptionDto> WeeksInYear { get; set; } = [];
    /// <summary>Chỉ trả khi query có Year — các kỳ-tháng có dữ liệu trong năm đó, sort giảm dần.</summary>
    public List<PeriodOptionDto> MonthsInYear { get; set; } = [];
}

public class PeriodOptionDto
{
    /// <summary>Giá trị dùng cho query param `period` của GET /api/criteria — "YYYY-Www" hoặc "YYYY-MM".</summary>
    public string Value { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal? OverallProgress { get; set; }
}
