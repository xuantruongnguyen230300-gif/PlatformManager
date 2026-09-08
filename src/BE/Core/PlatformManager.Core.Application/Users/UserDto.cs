namespace PlatformManager.Core.Application.Users;

/// <param name="Version">
/// Token chống ghi đè lẫn nhau — chính là <c>ConcurrencyStamp</c> mà ASP.NET Identity ĐÃ có sẵn
/// trên <c>AspNetUsers</c> (không thêm cột, không cần migration). FE giữ nguyên chuỗi này rồi gửi
/// lại trong <c>PUT /api/users/{id}</c>; lệch ⇒ 409. Xem doc/contracts/users.md
/// §"Quyết định người dùng 2026-08-30" quyết định 3.
///
/// <para><c>= null</c> là để 3 bộ unit test đang dựng <c>UserDto</c> bằng 8 đối số vẫn biên dịch,
/// KHÔNG phải để trường này tuỳ chọn ở đường thật: <c>UserAdminService</c> luôn điền.</para>
/// </param>
public sealed record UserDto(
    Guid Id,
    string UserName,
    string? Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsLocked,
    bool MustChangePassword,
    DateTimeOffset? DateCreate,
    string? Version = null);
