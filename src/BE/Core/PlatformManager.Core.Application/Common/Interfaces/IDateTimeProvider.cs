namespace PlatformManager.Core.Application.Common.Interfaces;

/// <summary>
/// Seam cho "thời điểm hiện tại" — AssessmentUpsertService/AggregationService dùng đây
/// thay vì gọi thẳng DateTimeOffset.UtcNow, để logic "kỳ = ngày hôm nay" test được.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
