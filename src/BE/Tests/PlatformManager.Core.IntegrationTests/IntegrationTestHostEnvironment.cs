namespace PlatformManager.Core.IntegrationTests;

/// <summary>
/// Cấu hình bắt buộc để host thật (<c>WebApplicationFactory&lt;Program&gt;</c>) khởi động được
/// trong test. Gọi ở <b>constructor của test class</b>, TRƯỚC khi dựng factory.
///
/// <para><b>Vì sao phải đặt bằng BIẾN MÔI TRƯỜNG, không phải <c>ConfigureAppConfiguration</c>:</b>
/// <c>Program.cs</c> đọc <c>ConnectionStrings:Default</c> NGAY tại
/// <c>AddHangfire(...UseNpgsqlConnection(...))</c> — giá trị bị "đông cứng" vào closure lúc đăng
/// ký DI, nên callback của factory chạy quá muộn.</para>
///
/// <para><b>Vì sao gom vào một chỗ:</b> trước đây mỗi test class tự set
/// <c>ConnectionStrings__Default</c> — 5 bản sao. Khi <c>BootstrapOptions</c> được thêm vào
/// (2026-08-22) với <c>ValidateOnStart()</c>, cả 5 chỗ đều phải sửa, và bỏ sót một chỗ nghĩa là
/// test đó đỏ với <c>OptionsValidationException</c> — một lỗi chẳng liên quan gì tới thứ nó kiểm.
/// Thêm cấu hình bắt buộc mới thì sửa DUY NHẤT ở đây.</para>
/// </summary>
internal static class IntegrationTestHostEnvironment
{
    /// <summary>
    /// Mật khẩu bootstrap DÙNG RIÊNG CHO TEST — cố ý đặt tường minh tại đây thay vì đọc User
    /// Secrets của máy đang chạy.
    ///
    /// <para>Nếu test dựa vào User Secrets, bộ test chỉ chạy được trên máy đã <c>dotnet
    /// user-secrets set</c> — máy mới hoặc CI sẽ đỏ vì thiếu cấu hình, chứ không phải vì code
    /// sai. Test phải tự đủ, không phụ thuộc trạng thái máy.</para>
    ///
    /// <para>Đây KHÔNG phải mật khẩu thật của bất kỳ môi trường nào: nó chỉ tồn tại trong
    /// database tạm do Testcontainers dựng lên rồi xoá sau mỗi lần chạy.</para>
    /// </summary>
    private const string TestBootstrapPassword = "IntegrationTest@123";

    /// <summary>Đặt toàn bộ cấu hình bắt buộc cho host test. Idempotent.</summary>
    public static void Configure(string connectionString)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);

        // Development: đúng môi trường WebApplicationFactory tự đặt, và là điều kiện để
        // DtiWeeklySeeder (danh mục demo) chạy. Đặt tường minh để không phụ thuộc thứ tự áp
        // cấu hình của factory.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // BootstrapOptions có ValidateOnStart ⇒ thiếu 2 giá trị này thì host KHÔNG boot được và
        // MỌI integration test đỏ với OptionsValidationException.
        Environment.SetEnvironmentVariable("Bootstrap__SuperAdminPassword", TestBootstrapPassword);
        Environment.SetEnvironmentVariable("Bootstrap__AdminPassword", TestBootstrapPassword);
    }
}
