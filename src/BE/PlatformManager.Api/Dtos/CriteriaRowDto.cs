namespace PlatformManager.Api.Dtos;

/// <summary>
/// 1 dòng grid "Danh mục DTI" (GET /api/criteria). Ở mode week/month: 1 dòng/
/// Criteria, giá trị resolve tại đúng kỳ đang xem (hoặc null = "—" nếu chỉ
/// tiêu không có record trong kỳ đó — KHÔNG carry-forward). Ở mode all:
/// 1 dòng/CriteriaAssessment record thực tế (có thể nhiều dòng/1 chỉ tiêu
/// trong năm) — xem doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm)".
/// </summary>
public class CriteriaRowDto
{
    public Guid CriteriaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }

    /// <summary>Id của CriteriaAssessment resolve được cho dòng này — null nếu không có dữ liệu ở kỳ đang xem.</summary>
    public Guid? AssessmentId { get; set; }
    public decimal? ProgressPercent { get; set; }
    public decimal? SelfScore { get; set; }
    public decimal? VerifiedScore { get; set; }
    /// <summary>Chênh lệch = SelfScore - VerifiedScore (cùng 1 record) — tính sẵn, không lưu cột riêng.</summary>
    public decimal? Diff { get; set; }
    public string? Status { get; set; }
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateOnly? Deadline { get; set; }
    public string? Note { get; set; }
    /// <summary>Ngày (phần date của CreatedAt) của record resolve được — "kỳ" của dòng này.</summary>
    public DateOnly? AssessmentDate { get; set; }
    public List<CriteriaEvidenceDto> Evidences { get; set; } = [];

    /// <summary>true chỉ khi đang xem "Tất cả" của NĂM HIỆN TẠI (server tự tính) — xem
    /// spec/danh-muc-dti/business-rules.md mục 2.4.</summary>
    public bool IsEditable { get; set; }
}
