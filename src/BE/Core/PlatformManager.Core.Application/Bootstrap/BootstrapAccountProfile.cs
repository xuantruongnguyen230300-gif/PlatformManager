namespace PlatformManager.Core.Application.Bootstrap;

/// <summary>
/// Phần danh tính hiển thị của MỘT tài khoản bootstrap do <b>host</b> khai qua
/// <see cref="ICoreBootstrapAccountSource"/> — dữ liệu thuần, không hành vi.
///
/// <para>Bản ghi này cố ý KHÔNG mang tên đăng nhập, vai, cờ đổi mật khẩu hay mật khẩu: cả bốn thứ
/// đó là cơ chế của Core và host không được quyền đổi — xem
/// <see cref="ICoreBootstrapAccountSource"/> §"Cái gì KHÔNG đi qua seam này".</para>
/// </summary>
/// <param name="Email">
/// Email của tài khoản. <b>Quy ước đặt tên, không phải validate cứng</b> — Core không kiểm định
/// dạng, giữ nguyên hành vi có từ trước lượt tách (Identity cũng không: xem
/// <c>options.User.RequireUniqueEmail = false</c> ở <c>DependencyInjection</c>).
///
/// <para>Hai điều kiện DUY NHẤT mà seeder ép: khác rỗng, và hai tài khoản không được dùng chung
/// một email — xem <c>CoreSeeder.GuardAgainstInvalidProfiles</c> cho hậu quả của từng ca.</para>
/// </param>
/// <param name="FullName">
/// Tên hiển thị. Đây là thứ người dùng thật sự đọc trên mọi màn hình quản trị, nên chuỗi rỗng ở
/// đây không phải "chưa điền" mà là một tài khoản quản trị không nhận diện được — seeder chặn.
/// </param>
public sealed record BootstrapAccountProfile(string Email, string FullName);
