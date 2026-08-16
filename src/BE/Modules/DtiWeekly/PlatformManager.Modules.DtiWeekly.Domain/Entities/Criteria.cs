using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Modules.DtiWeekly.Domain.Entities;

/// <summary>
/// Danh mục chỉ tiêu chuyển đổi số (DTI) — tĩnh, không đổi theo kỳ báo cáo.
/// Biến thiên theo kỳ (tiến độ, điểm, trạng thái...) nằm ở CriteriaAssessment.
/// </summary>
public class Criteria : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid GroupId { get; private set; }
    public decimal MaxScore { get; private set; }

    private Criteria() { }

    public static Criteria Create(string code, string name, Guid groupId, decimal maxScore)
    {
        ValidateInvariants(code, name, groupId, maxScore);

        return new Criteria
        {
            Id = EntityId.New(),
            Code = code.Trim(),
            Name = name.Trim(),
            GroupId = groupId,
            MaxScore = maxScore,
        };
    }

    /// <summary>
    /// Cho phép đổi cả Code (business quyết định — xem spec/danh-muc-dti/business-rules.md
    /// mục 1.2: Code chỉ là dữ liệu hiển thị, FK CriteriaAssessment trỏ theo Id, không theo Code).
    /// </summary>
    public void UpdateDetails(string code, string name, Guid groupId, decimal maxScore)
    {
        ValidateInvariants(code, name, groupId, maxScore);

        Code = code.Trim();
        Name = name.Trim();
        GroupId = groupId;
        MaxScore = maxScore;
    }

    private static void ValidateInvariants(string code, string name, Guid groupId, decimal maxScore)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("CRITERIA_CODE_REQUIRED", "Mã chỉ tiêu không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("CRITERIA_NAME_REQUIRED", "Tên chỉ tiêu không được để trống.");
        if (groupId == Guid.Empty)
            throw new DomainException("CRITERIA_GROUP_REQUIRED", "Nhóm chỉ tiêu không được để trống.");
        if (maxScore <= 0)
            throw new DomainException("CRITERIA_MAX_SCORE_INVALID", "Điểm tối đa phải > 0.");
    }
}
