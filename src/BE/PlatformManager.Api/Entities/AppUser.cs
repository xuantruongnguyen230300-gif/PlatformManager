namespace PlatformManager.Api.Entities;

/// <summary>
/// Bảng lookup người dùng đơn giản — KHÔNG phải ASP.NET Core Identity (không
/// password/login field nào). Chỉ dùng làm điểm neo cho
/// <see cref="CriteriaAssessment.OwnerId"/> ("Phụ trách") ở bản demo này.
/// Không kế thừa <see cref="BaseEntity"/> — không cần audit/soft-delete cho
/// danh sách người phụ trách tĩnh.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
}
