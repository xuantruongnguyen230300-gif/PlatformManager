namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Điểm sinh Id duy nhất cho toàn hệ thống — không factory method nào gọi
/// Guid.NewGuid() trực tiếp, luôn qua đây. Seam một dòng: đổi chiến lược sinh Id
/// chỉ cần sửa một chỗ.
///
/// <para>Từ 2026-08-28 dùng <c>Guid.CreateVersion7()</c> (UUID v7, .NET 9+) thay cho
/// <c>Guid.NewGuid()</c> (v4 ngẫu nhiên hoàn toàn): v7 tăng dần theo thời gian nên khoá
/// chính không làm phân mảnh trang index B-tree — vấn đề thật ở bảng ghi nhiều. Xem
/// doc/huong_dan/quy-uoc/be-entity-domain.md mục "Base entity".</para>
/// </summary>
public static class EntityId
{
    public static Guid New() => Guid.CreateVersion7();
}
