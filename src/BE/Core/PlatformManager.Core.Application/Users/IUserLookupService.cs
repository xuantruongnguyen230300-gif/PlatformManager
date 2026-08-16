namespace PlatformManager.Core.Application.Users;

/// <summary>
/// Chỉ dùng riêng cho CsvImportService resolve cột "Phụ trách" (tên tự do) → AppUser —
/// KHÁC IUserAdminService (màn Quản trị người dùng, tạo user đầy đủ UserName/Email/mật
/// khẩu tạm). Xem spec/danh-muc-dti/business-rules.md mục 2.2 câu #16.
/// </summary>
public interface IUserLookupService
{
    /// <summary>
    /// Khớp chính xác AppUser.FullName (trim, không phân biệt hoa/thường):
    /// - Đúng 1 user khớp → trả Id đó, WasCreated=false.
    /// - Không ai khớp → tự tạo AppUser mới (chỉ có FullName, UserName/mật khẩu tự sinh vô
    ///   danh, MustChangePassword=true — tài khoản này chỉ là nhãn "phụ trách", không kỳ
    ///   vọng đăng nhập được cho tới khi Admin cấp lại thông tin qua màn Quản trị người dùng).
    /// - Trùng ≥2 user cùng tên (ambiguous) → trả null, WasCreated=false — KHÔNG tự đoán.
    /// </summary>
    Task<(Guid? OwnerId, bool WasCreated)> ResolveOrCreateByFullNameAsync(string fullName, CancellationToken ct);
}
