using PlatformManager.Core.Application.Bootstrap;

namespace PlatformManager.Api.Seeding;

/// <summary>
/// Email và tên hiển thị của 2 tài khoản bootstrap thuộc CHÍNH dự án PlatformManager — tên miền
/// nội bộ <c>@platformmanager.local</c> và nhãn tiếng Việt.
///
/// <para><b>Vì sao ở host chứ không ở Core</b> (chuyển 2026-09-02): cả tên miền lẫn ngôn ngữ đều
/// là thứ riêng của dự án này, và chắc chắn khác ở dự án thứ hai dựng trên cùng CoreBase. Để
/// chúng trong <c>Core.Persistence/CoreSeeder.cs</c> nghĩa là muốn tái dùng Core
/// thì phải mổ vào trong Core. Phần LUẬT thì vẫn ở Core và host không đụng tới được: đúng hai tài
/// khoản, mỗi tài khoản một vai, <c>MustChangePassword = true</c>, tên đăng nhập
/// <c>"SuperAdmin"</c>/<c>"Admin"</c> soi gương <c>Roles.*</c> — xem
/// <see cref="ICoreBootstrapAccountSource"/>.</para>
///
/// <para><b>Mật khẩu KHÔNG khai ở đây và không bao giờ được khai ở đây.</b> Nó đọc từ section
/// <c>Bootstrap</c> qua <c>BootstrapOptions</c> (User Secrets ở dev, biến môi trường ở
/// production). Seam này cố ý chỉ mang danh tính hiển thị, để không có chỗ nào trong repo cám dỗ
/// người ta đặt một mật khẩu vào cạnh email cho "đủ bộ".</para>
///
/// <para><b>Đổi email ở đây KHÔNG đổi được tài khoản đã seed.</b> <c>CoreSeeder</c> tìm tài khoản
/// theo TÊN ĐĂNG NHẬP; thấy đã tồn tại thì nó chỉ bảo đảm đúng role rồi thoát, không ghi đè
/// email/tên hiển thị. Sửa giá trị dưới đây rồi chạy lại <c>--seed</c> trên một database đã seed
/// sẽ không có tác dụng — muốn đổi thật thì sửa qua màn hình quản trị người dùng.</para>
/// </summary>
internal sealed class AppBootstrapAccountSource : ICoreBootstrapAccountSource
{
    /// <summary>
    /// Tài khoản break-glass. Tên hiển thị cố ý khác tài khoản vận hành ("hệ thống" ở cuối) để
    /// người nhìn danh sách người dùng phân biệt được hai tài khoản quản trị — ranh giới
    /// least-privilege 2026-08-24 chỉ có tác dụng nếu người thao tác nhận ra mình đang dùng cái nào.
    /// </summary>
    public BootstrapAccountProfile SuperAdmin { get; } =
        new("superadmin@platformmanager.local", "Quản trị viên hệ thống");

    /// <summary>Tài khoản vận hành hàng ngày.</summary>
    public BootstrapAccountProfile Admin { get; } =
        new("admin@platformmanager.local", "Quản trị viên");
}
