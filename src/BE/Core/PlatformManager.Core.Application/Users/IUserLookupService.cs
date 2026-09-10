namespace PlatformManager.Core.Application.Users;

/// <summary>
/// Seam <b>CHỈ TRA CỨU</b>: tên tự do (ví dụ cột "Phụ trách" của một file import) → <c>AppUser.Id</c>.
/// Không khớp ai thì trả <c>null</c>, KHÔNG tạo gì — khác hẳn <c>IUserAdminService</c> (màn Quản trị
/// người dùng, tạo user đầy đủ UserName/Email/mật khẩu tạm).
///
/// <para>⚠️ Hôm nay interface này chưa có nơi dùng thật trong mã sản phẩm: đường import từng dùng nó
/// đã bị gỡ cùng module DtiWeekly 2026-08-29.</para>
///
/// <para><b>Vì sao KHÔNG còn nhánh tự tạo user</b> (sửa 2026-09-09): bản trước tạo một
/// <c>AppUser</c> "nhãn phụ trách" khi không khớp ai. Điều đó ngược luật nghiệp vụ đã chốt —
/// <c>spec/danh-muc-dti/business-rules.md</c> §6.3 quy định <i>"`Phụ trách` không khớp
/// `AppUser.FullName` nào → <b>KHÔNG lỗi</b> — `OwnerId` để trống"</i> — và hậu quả thật là nạp một
/// file có cột "Phụ trách" đầy dữ liệu sẽ đẻ ra hàng chục tài khoản không ai đăng nhập được, nằm
/// lại vĩnh viễn trong bảng người dùng.</para>
/// </summary>
public interface IUserLookupService
{
    /// <summary>
    /// Khớp chính xác <c>AppUser.FullName</c> (trim, không phân biệt hoa/thường):
    /// <list type="bullet">
    /// <item>Đúng 1 user khớp → trả <c>Id</c> của user đó.</item>
    /// <item>Không ai khớp → <c>null</c>. Đây là kết quả BÌNH THƯỜNG, không phải lỗi — người phụ
    /// trách chưa có tài khoản là chuyện thường; bên gọi để trống trường tham chiếu.</item>
    /// <item>Trùng ≥2 user cùng tên (ambiguous) → <c>null</c> — KHÔNG tự đoán.</item>
    /// </list>
    /// Hai nhánh sau cùng trả <c>null</c> có chủ đích: bên gọi xử lý chúng y như nhau (bỏ trống),
    /// nên phân biệt chúng ở kiểu trả về chỉ tạo một nhánh không ai dùng.
    /// </summary>
    Task<Guid?> ResolveByFullNameAsync(string fullName, CancellationToken ct);
}
