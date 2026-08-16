namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Entity gốc dùng chung cho toàn bộ entity nghiệp vụ (KHÔNG dùng cho AppUser/AppRole —
/// đó là entity của ASP.NET Core Identity, tự quản lý vòng đời riêng).
/// Setter public cho đúng 6 field kỹ thuật này là chủ đích, không phải sơ suất — xem
/// src/BE/.claude/rules/entity-domain.md mục "Base entity".
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public string? UserCreate { get; set; }
    public string? UserUpdate { get; set; }
    public DateTimeOffset? DateCreate { get; set; }
    public DateTimeOffset? DateUpdate { get; set; }
    public bool IsDelete { get; set; }
}
