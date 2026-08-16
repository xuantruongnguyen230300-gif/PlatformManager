namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Điểm sinh Id duy nhất cho toàn hệ thống — không factory method nào gọi
/// Guid.NewGuid() trực tiếp, luôn qua đây. Seam một dòng: đổi chiến lược sinh Id
/// (vd UUIDv7 tuần tự) chỉ cần sửa một chỗ.
/// </summary>
public static class EntityId
{
    public static Guid New() => Guid.NewGuid();
}
