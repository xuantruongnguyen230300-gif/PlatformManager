using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace PlatformManager.Api.Common;

/// <summary>
/// Allowlist origin cho CORS — bind từ section <c>Cors</c>.
///
/// <para><b>Vì sao phải là <c>IOptions&lt;T&gt;</c> chứ không đọc thẳng <c>IConfiguration</c>:</b>
/// bản trước đọc <c>configuration.GetSection("Cors:AllowedOrigins").Get&lt;string[]&gt;() ?? []</c>
/// ngay trong <c>Program.cs</c>. Dấu <c>?? []</c> biến một cấu hình <b>thiếu</b> thành một allowlist
/// <b>rỗng</b>, mà allowlist rỗng chặn <b>mọi</b> origin — FE không gọi được một API nào, trong khi
/// <c>/health</c> vẫn xanh và deploy vẫn báo thành công. Xem
/// doc/huong_dan/quy-uoc/be-architecture.md §"Quyết định người dùng 2026-08-31 —
/// Cors:AllowedOrigins phải theo đúng khuôn này".</para>
///
/// <para><b>Ranh giới fail-fast:</b> <c>ValidateOnStart()</c> CHỈ được gắn ở Production (xem
/// <c>Program.cs</c>) — Development cố ý KHÔNG fail-fast, để một máy chưa cấu hình xong vẫn boot
/// được, và ở đó allowlist rỗng lộ ra ngay lần gọi API đầu tiên chứ không âm thầm.</para>
///
/// <para><b>Đặt giá trị ở đâu ở Development — máy mới phải TỰ TẠO (sửa 2026-09-08):</b> allowlist
/// dev là cấu hình <b>cục bộ của từng máy</b>, repo KHÔNG mang sẵn.
/// <c>appsettings.Development.json</c> bị <c>src/BE/.gitignore</c> (khuôn <c>appsettings.*.json</c>)
/// loại khỏi repo từ 2026-08-31 — kiểm bằng <c>git ls-files src/BE/PlatformManager.Api</c>, chỉ in
/// <c>appsettings.json</c>. Ai clone về phải tự tạo file đó (hoặc dùng User Secrets) với chuỗi kết
/// nối Postgres và origin dev của riêng mình; thiếu nó thì allowlist RỖNG và FE không gọi được API
/// nào. Vì sao không commit file đó: doc/huong_dan/quy-uoc/repo-artifact.md §1.
/// <i>Câu cũ ở đây nói Development "chạy được với cấu hình sẵn trong repo" — đúng cho tới
/// 2026-08-31, sai kể từ đó.</i></para>
///
/// <para><b>Đặt giá trị ở đâu ở Production:</b> biến môi trường
/// <c>Cors__AllowedOrigins__0</c>, <c>Cors__AllowedOrigins__1</c>, … (dấu <c>__</c> thay cho
/// <c>:</c>) hoặc secret store của nền tảng triển khai — KHÔNG commit domain thật vào repo.</para>
/// </summary>
public sealed class CorsPolicyOptions
{
    /// <summary>Tên section trong cấu hình.</summary>
    public const string SectionName = "Cors";

    /// <summary>Tên policy CORS duy nhất của app — dùng ở <c>app.UseCors(...)</c>.</summary>
    public const string PolicyName = "Default";

    /// <summary>
    /// Danh sách origin đầy đủ (scheme + host + port), ví dụ <c>https://app.example.com</c>.
    /// KHÔNG có dấu <c>/</c> ở cuối — <c>WithOrigins</c> so khớp chuỗi chính xác.
    /// </summary>
    [Required(ErrorMessage =
        "Thiếu Cors:AllowedOrigins. Production: đặt biến môi trường Cors__AllowedOrigins__0=https://<domain-FE>.")]
    [MinLength(1, ErrorMessage =
        "Cors:AllowedOrigins phải có ít nhất 1 origin. Allowlist rỗng chặn MỌI origin — FE sẽ không gọi được API nào. "
        + "Production: đặt biến môi trường Cors__AllowedOrigins__0=https://<domain-FE> (thêm __1, __2… nếu có nhiều origin).")]
    public string[] AllowedOrigins { get; init; } = [];
}

/// <summary>
/// Dựng policy CORS từ <see cref="CorsPolicyOptions"/>.
///
/// <para>Đi qua <c>IConfigureOptions</c> thay vì đọc cấu hình ngay trong <c>Program.cs</c> để
/// allowlist được phân giải <b>lười</b> qua DI — nhờ đó nó đi qua đúng đường
/// <c>ValidateDataAnnotations()</c>/<c>ValidateOnStart()</c>, không có đường tắt nào nhận giá trị
/// mặc định rỗng.</para>
/// </summary>
internal sealed class ConfigureDefaultCorsPolicy(IOptions<CorsPolicyOptions> options)
    : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions corsOptions)
        => corsOptions.AddPolicy(CorsPolicyOptions.PolicyName, policy => policy
            // AllowCredentials() + origin cụ thể — TUYỆT ĐỐI không AllowAnyOrigin() khi dùng
            // cookie (trình duyệt sẽ âm thầm bỏ qua cookie).
            .WithOrigins(options.Value.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
}
