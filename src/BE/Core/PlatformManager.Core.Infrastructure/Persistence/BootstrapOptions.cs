using System.ComponentModel.DataAnnotations;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Mật khẩu ban đầu của 2 tài khoản quản trị do <see cref="CoreSeeder"/> tạo.
///
/// <para><b>KHÔNG có giá trị mặc định, và KHÔNG được đặt giá trị mặc định.</b> Bind từ section
/// <c>Bootstrap</c> qua <c>ValidateDataAnnotations().ValidateOnStart()</c> — thiếu thì app KHÔNG
/// khởi động được. Đó là chủ đích: một tài khoản quản trị với mật khẩu ai cũng đoán được còn tệ
/// hơn hẳn một app từ chối khởi động.</para>
///
/// <para><b>Đặt giá trị ở đâu — không bao giờ trong repo:</b></para>
/// <list type="table">
///   <item>
///     <term>Development</term>
///     <description>
///       User Secrets — file nằm ở <c>%APPDATA%\Microsoft\UserSecrets\platformmanager-api\secrets.json</c>,
///       NGOÀI thư mục repo nên không bao giờ bị commit:
///       <code>
///       dotnet user-secrets set "Bootstrap:SuperAdminPassword" "..." --project src/BE/PlatformManager.Api
///       dotnet user-secrets set "Bootstrap:AdminPassword" "..." --project src/BE/PlatformManager.Api
///       </code>
///     </description>
///   </item>
///   <item>
///     <term>Production</term>
///     <description>
///       Biến môi trường (dấu <c>__</c> thay cho <c>:</c>):
///       <c>Bootstrap__SuperAdminPassword</c>, <c>Bootstrap__AdminPassword</c> — hoặc secret store
///       của nền tảng triển khai (Key Vault, Secrets Manager, Docker/K8s secret).
///     </description>
///   </item>
/// </list>
///
/// <para>⚠️ <b>Đừng đặt giá trị giả cho "qua được validation".</b> <c>appsettings.json</c> từng chứa
/// một section <c>Smtp</c> với <c>localhost:25</c> đặt vào đúng để <c>ValidateOnStart()</c> không
/// chặn app — cấu hình giả kiểu đó VẪN qua validation rồi thất bại âm thầm đúng lúc cần dùng. Ở
/// đây hậu quả nặng hơn nhiều: nó tạo ra tài khoản quản trị thật với mật khẩu rác.</para>
/// </summary>
public sealed class BootstrapOptions
{
    /// <summary>Tên section trong cấu hình.</summary>
    public const string SectionName = "Bootstrap";

    /// <summary>Mật khẩu ban đầu của tài khoản <c>SuperAdmin</c>.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage =
        "Thiếu Bootstrap:SuperAdminPassword. Dev: dotnet user-secrets set. Production: biến môi trường Bootstrap__SuperAdminPassword.")]
    [MinLength(6, ErrorMessage = "Bootstrap:SuperAdminPassword phải dài ít nhất 6 ký tự (khớp Identity Password.RequiredLength).")]
    public string SuperAdminPassword { get; init; } = default!;

    /// <summary>Mật khẩu ban đầu của tài khoản <c>Admin</c>.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage =
        "Thiếu Bootstrap:AdminPassword. Dev: dotnet user-secrets set. Production: biến môi trường Bootstrap__AdminPassword.")]
    [MinLength(6, ErrorMessage = "Bootstrap:AdminPassword phải dài ít nhất 6 ký tự (khớp Identity Password.RequiredLength).")]
    public string AdminPassword { get; init; } = default!;
}
