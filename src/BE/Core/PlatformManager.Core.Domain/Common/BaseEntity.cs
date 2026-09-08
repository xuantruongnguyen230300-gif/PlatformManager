namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Entity gốc dùng chung cho toàn bộ entity nghiệp vụ (KHÔNG dùng cho AppUser/AppRole —
/// đó là entity của ASP.NET Core Identity, tự quản lý vòng đời riêng).
///
/// <para><b>Id là <c>init</c></b> — sinh NGAY lúc khởi tạo bằng <see cref="EntityId.New"/>
/// (UUID v7, tuần tự theo thời gian nên không phân mảnh trang index B-tree). Không ai gán
/// lại Id sau khi object đã tồn tại, và không sinh Id ở interceptor lúc SaveChangesAsync —
/// làm vậy thì entity mang <c>Guid.Empty</c> suốt từ lúc tạo tới lúc lưu.</para>
///
/// <para><b>Setter public cho đúng 5 field audit</b> là chủ đích, không phải sơ suất:
/// AuditInterceptor (Infrastructure) phải ghi được chúng từ bên ngoài entity. Đánh đổi này
/// KHÔNG mở rộng sang field nghiệp vụ (luôn <c>private set</c> + mutate qua method có tên
/// nghiệp vụ) và KHÔNG áp cho <see cref="Id"/>.</para>
///
/// Hợp đồng đầy đủ: doc/huong_dan/quy-uoc/be-entity-domain.md mục "Base entity".
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Danh tính — sinh lúc khởi tạo, không gán lại từ ngoài.</summary>
    public Guid Id { get; init; } = EntityId.New();

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
