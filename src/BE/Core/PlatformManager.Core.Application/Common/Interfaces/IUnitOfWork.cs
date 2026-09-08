namespace PlatformManager.Core.Application.Common.Interfaces;

/// <summary>
/// Handler own SaveChanges, đúng một lần, ở cuối — repository KHÔNG BAO GIỜ tự gọi
/// SaveChanges (xem doc/huong_dan/quy-uoc/be-cqrs-handler.md §Handler).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Xoá toàn bộ entity đang được change-tracker theo dõi mà CHƯA SaveChanges —
    /// dùng khi 1 dòng trong vòng lặp import lỗi giữa chừng, tránh rò rỉ entity chưa lưu của
    /// dòng lỗi sang lần SaveChanges của dòng kế tiếp — ca điển hình là import theo dòng.</summary>
    void DiscardTrackedChanges();
}
