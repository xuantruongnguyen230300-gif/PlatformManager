namespace PlatformManager.Business.Application.Criteria;

/// <summary>Một trang của lưới chỉ tiêu, kèm tổng số dòng KHỚP BỘ LỌC.</summary>
/// <param name="TotalCount">
/// Đếm số CHỈ TIÊU khớp bộ lọc, không đếm số bản ghi đánh giá — ở mọi giá trị <c>period</c>, lưới
/// trả đúng 1 dòng / 1 chỉ tiêu (doc/contracts/danh-muc-dti.md DM-2 mục 5).
/// </param>
public sealed record CriteriaGridPage(IReadOnlyList<CriteriaRowDto> Items, int TotalCount);

/// <summary>
/// Đọc lưới Danh mục DTI. Hiện thực dùng EF sống ở <c>Business.Persistence</c>.
/// </summary>
public interface ICriteriaGridRepository
{
    Task<CriteriaGridPage> GetPageAsync(CriteriaFilterSpec spec, int page, int pageSize, CancellationToken ct);
}
