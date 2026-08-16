namespace PlatformManager.Core.Application.Common.Models;

/// <summary>
/// Kết quả phân trang — luôn bọc trong IApiResult&lt;PagedList&lt;T&gt;&gt;, không trả trần
/// (xem .claude/rules/cqrs-handler.md §Grid / danh sách).
/// </summary>
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
