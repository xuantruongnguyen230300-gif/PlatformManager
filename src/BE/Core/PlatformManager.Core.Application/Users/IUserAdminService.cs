using PlatformManager.Core.Application.Common.Models;

namespace PlatformManager.Core.Application.Users;

/// <summary>Kết quả tạo user qua Identity — IdentityResult có thể trả nhiều lỗi cùng lúc
/// (trùng UserName, password không đủ mạnh...), không rút gọn về 1 ErrorDescriptor.</summary>
public sealed record CreateUserOutcome(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors);

/// <summary>
/// Kết quả CẬP NHẬT user — cùng khuôn <see cref="CreateUserOutcome"/> ngay trên (quyết định người
/// dùng 2026-09-05, doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.3).
///
/// <para><b>Vì sao không còn là <c>bool</c>:</b> <c>UserAdminService.UpdateCoreAsync</c> thoát bằng
/// <c>return false</c> ở NĂM chỗ, nên mã lỗi Identity — <c>ConcurrencyFailure</c>, lỗi gán role —
/// bị vứt TRƯỚC KHI tới được handler. Hệ quả đo được: handler không còn gì để nói nên phải bịa một
/// chuỗi tại chỗ gọi, và khi cập nhật hỏng thật thì KHÔNG AI biết vì sao, kể cả người vận hành đọc
/// log. <c>CreateAsync</c> ngay cạnh vốn đã trả outcome — <c>bool</c> trần là chỗ lạc loài.</para>
/// </summary>
/// <param name="Succeeded">Ghi thành công và đã commit.</param>
/// <param name="NotFound">
/// <b>KHÔNG phải lỗi Identity</b> — người dùng biến mất giữa lần đọc của handler và lần ghi (bị xoá
/// xen vào). Tách riêng là RÀNG BUỘC của §11.3: gộp nó vào <paramref name="Errors"/> rỗng sẽ làm một
/// bản ghi vừa bị xoá ra <c>USER.UPDATE_FAILED</c> kèm danh sách mã RỖNG — đúng lại chỗ trống mà
/// quyết định này sinh ra để lấp. Handler phân biệt được nên nó trả <c>USER.NOT_FOUND</c> (404).
/// </param>
/// <param name="Errors">
/// Mã <c>IdentityError.Code</c> (KHÔNG phải <c>Description</c> — xem §4(c)). <b>Bất biến:</b> khi
/// <paramref name="Succeeded"/> false và <paramref name="NotFound"/> false thì danh sách này KHÔNG
/// rỗng; <see cref="Rejected"/> cưỡng chế điều đó.
/// </param>
public sealed record UpdateUserOutcome(bool Succeeded, bool NotFound, IReadOnlyList<string> Errors)
{
    public static UpdateUserOutcome Success() => new(true, false, []);

    public static UpdateUserOutcome UserNotFound() => new(false, true, []);

    /// <summary>
    /// Identity từ chối. <paramref name="identityErrorCodes"/> rỗng ⇒ thay bằng
    /// <c>DefaultError</c> — mã CÓ THẬT của <c>IdentityErrorDescriber</c>, không phải mã ta bịa ra.
    ///
    /// <para><b>Vì sao phải chặn danh sách rỗng ở đây</b> chứ không tin nơi gọi: <c>IdentityResult</c>
    /// thất bại với <c>Errors</c> rỗng là chuyện thư viện cho phép, và nếu để lọt thì envelope ra
    /// một lỗi "cập nhật thất bại" không kèm nguyên nhân nào — tức đúng hiện trạng mà quyết định 2
    /// tồn tại để sửa, chỉ khác là lần này im lặng hơn.</para>
    /// </summary>
    public static UpdateUserOutcome Rejected(IReadOnlyList<string> identityErrorCodes)
        => new(false, false, identityErrorCodes.Count > 0 ? identityErrorCodes : ["DefaultError"]);
}

/// <summary>
/// Bộ lọc của grid "Quản trị người dùng" — null ở trường nào thì KHÔNG áp điều kiện đó.
///
/// Gói thành record thay vì thả thêm tham số vào <see cref="IUserAdminService.GetListAsync"/>:
/// <see cref="SearchText"/> và <see cref="Role"/> cùng kiểu <c>string?</c> và đứng cạnh nhau,
/// nên hoán vị nhầm hai đối số vẫn biên dịch được và chỉ lộ ra khi có người nhìn kết quả lọc.
/// </summary>
public sealed record UserListFilter(string? SearchText = null, string? Role = null, bool? IsLocked = null);

/// <summary>
/// Implement ở Infrastructure/Identity (dùng UserManager&lt;AppUser&gt;/RoleManager&lt;AppRole&gt;) —
/// Application không được biết tới kiểu AppUser/AppRole cụ thể (đó là kiểu Identity, thuộc
/// Infrastructure theo doc/huong_dan/quy-uoc/be-api-controller.md §Auth/Permission).
/// </summary>
public interface IUserAdminService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Lọc PHẢI chạy ở tầng truy vấn: <c>TotalCount</c> trả về là tổng SAU khi lọc,
    /// nếu lọc sau khi phân trang thì cả danh sách lẫn số trang đều sai.</summary>
    Task<PagedList<UserDto>> GetListAsync(int page, int pageSize, UserListFilter filter, CancellationToken ct);

    Task<bool> UserNameExistsAsync(string userName, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    /// <summary>Tạo user với mật khẩu tạm — MustChangePassword=true áp dụng chung cho MỌI
    /// user do Admin tạo (xem doc/ke-hoach-xay-lai-corebase.md).</summary>
    Task<CreateUserOutcome> CreateAsync(
        string userName, string? email, string fullName, string tempPassword,
        IReadOnlyCollection<string> roles, CancellationToken ct);

    Task<UpdateUserOutcome> UpdateAsync(Guid id, string? email, string fullName, IReadOnlyCollection<string> roles, CancellationToken ct);

    // LockAsync/UnlockAsync CỐ Ý vẫn trả bool trong đợt 2026-09-05 — chúng vứt IdentityResult.Errors
    // theo đúng khuôn UpdateAsync vừa bỏ, nhưng người dùng chốt phạm vi lần này là UpdateAsync
    // (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.3). Ghi ra để lần sau không ai tưởng đã
    // quét sạch; đây là ứng viên kế tiếp hiển nhiên nhất.
    Task<bool> LockAsync(Guid id, CancellationToken ct);

    Task<bool> UnlockAsync(Guid id, CancellationToken ct);
}
