using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>Catalog lỗi bất biến của <see cref="CriteriaAssessment"/>.</summary>
public static class CriteriaAssessmentDomainErrors
{
    public static readonly DomainError CriteriaRequired = new(
        "CRITERIA_ASSESSMENT.CRITERIA_REQUIRED", "Bản ghi đánh giá phải thuộc về một chỉ tiêu.");

    /// <summary>
    /// 4 giá trị hợp lệ ở §4 của file luật. Giá trị ngoài tập đó bị TỪ CHỐI, không âm thầm bỏ
    /// qua và không tự map gần đúng — luật 3 của §4.
    /// </summary>
    public static readonly DomainError StatusInvalid = new(
        "CRITERIA_ASSESSMENT.STATUS_INVALID", "Trạng thái không thuộc 4 giá trị hợp lệ.");
}
