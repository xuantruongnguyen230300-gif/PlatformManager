namespace PlatformManager.Api.Entities;

/// <summary>
/// Base entity dùng chung cho mọi entity nghiệp vụ (trừ <see cref="AppUser"/>
/// — bảng lookup thường, không có audit/soft-delete).
/// Setter là <c>protected</c> — mutation qua method nghiệp vụ hoặc gán trực
/// tiếp trong Services (demo đơn giản, không dùng factory method/CQRS).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
