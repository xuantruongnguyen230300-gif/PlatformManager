using System.Text.Json;

namespace PlatformManager.Api.Common;

/// <summary>
/// Đọc trước tên đăng nhập trong thân request <c>POST /api/auth/login</c> và cất vào
/// <see cref="HttpContext.Items"/> để hàng rào rate limit thứ hai (phân vùng theo TÊN ĐĂNG NHẬP)
/// dùng được.
///
/// <para><b>Vì sao phải có middleware này thay vì đọc thẳng trong hàm chọn phân vùng:</b> hàm chọn
/// phân vùng của <c>RateLimiter</c> chạy <b>đồng bộ</b> và chạy <b>trước</b> khi ASP.NET đọc thân
/// request, trong khi tên đăng nhập nằm trong thân JSON. Nên phải bật đệm thân request, đọc tên,
/// cất lại, và <b>trả con trỏ thân request về 0</b> — quên bước cuối thì handler đọc được thân
/// RỖNG và mọi lần đăng nhập trả 400 "UserName không được rỗng". Xem
/// doc/huong_dan/wiki-core/be/09-security-beyond-auth.md §"Chính sách mật khẩu".</para>
///
/// <para><b>Chỉ đệm đúng một đường</b> (<c>POST /api/auth/login</c>) — đệm thân MỌI request là trả
/// phí bộ nhớ/sao chép cho toàn bộ lưu lượng chỉ để phục vụ một endpoint.</para>
///
/// <para><b>Đặt ở đâu:</b> ngay TRƯỚC <c>app.UseRateLimiter()</c>. Đặt sau thì
/// <c>HttpContext.Items</c> còn rỗng lúc rate limiter chọn phân vùng, hàng rào rơi hết về phân
/// vùng dùng chung — hỏng âm thầm, không lỗi biên dịch.</para>
/// </summary>
internal sealed class LoginUserNameRateLimitMiddleware(RequestDelegate next)
{
    /// <summary>Khoá trong <see cref="HttpContext.Items"/>. Nội bộ host, không lộ ra ngoài.</summary>
    private const string UserNameItemKey = "RateLimit:LoginUserName";

    /// <summary>Đường đăng nhập — hàng rào theo tên CHỈ áp cho đúng đường này.</summary>
    private static readonly PathString LoginPath = new("/api/auth/login");

    /// <summary>
    /// Trần đệm thân request. Thân đăng nhập thật chỉ vài trăm byte; vượt ngưỡng này là lưu lượng
    /// bất thường ⇒ KHÔNG đệm, KHÔNG đọc, và request rơi về phân vùng dùng chung
    /// <see cref="UnparsedPartitionKey"/> thay vì thoát khỏi hàng rào.
    /// </summary>
    private const int MaxBufferedBodyBytes = 32 * 1024;

    /// <summary>Chặn tên đăng nhập dài bất thường làm khoá phân vùng phình to.</summary>
    private const int MaxUserNameLength = 128;

    /// <summary>
    /// Phân vùng dùng chung cho request đăng nhập KHÔNG đọc được tên (thân không phải JSON, thiếu
    /// trường, hoặc quá lớn).
    ///
    /// <para>Cố ý KHÔNG cho những request này đi thẳng qua: nếu chúng được miễn, kẻ tấn công chỉ
    /// cần gửi thân dị dạng là thoát hàng rào. Chúng dùng chung một xô — và vì thân dị dạng thì
    /// validator trả 400 chứ không bao giờ đăng nhập được, việc xô này bị đốt cạn không chặn
    /// nhầm ai đang đăng nhập thật.</para>
    /// </summary>
    public const string UnparsedPartitionKey = "login-user:__unparsed__";

    /// <summary>Tiền tố khoá phân vùng — tách hẳn không gian khoá với các limiter khác trong chuỗi.</summary>
    private const string PartitionKeyPrefix = "login-user:";

    /// <summary>Request này có phải lượt đăng nhập không.</summary>
    public static bool IsLoginRequest(HttpContext context)
        => HttpMethods.IsPost(context.Request.Method)
           && context.Request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Khoá phân vùng theo tên đăng nhập đã chuẩn hoá. Chuẩn hoá về chữ thường là bắt buộc:
    /// <c>SuperAdmin</c> và <c>superadmin</c> đăng nhập vào CÙNG một tài khoản (Identity so khớp
    /// theo <c>NormalizedUserName</c>), nên nếu không chuẩn hoá thì đổi hoa/thường là có ngay một
    /// xô mới — hàng rào thành vô nghĩa.
    /// </summary>
    public static string ResolvePartitionKey(HttpContext context)
        => context.Items.TryGetValue(UserNameItemKey, out var value) && value is string userName
            ? PartitionKeyPrefix + userName
            : UnparsedPartitionKey;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsLoginRequest(context))
        {
            await CaptureUserNameAsync(context);
        }

        await next(context);
    }

    private static async Task CaptureUserNameAsync(HttpContext context)
    {
        var request = context.Request;

        // Không đệm khi thân khai độ dài vượt trần. ContentLength null (chunked) VẪN được đệm —
        // EnableBuffering có bufferLimit riêng lo phần đó.
        //
        // CỐ Ý không lọc theo Content-Type: thân không phải JSON rơi vào catch (JsonException) bên
        // dưới và về xô "__unparsed__" — vẫn bị tính lượt. Lọc theo Content-Type sẽ biến việc khai
        // sai header thành đường THOÁT khỏi hàng rào, đúng thứ hàng rào này sinh ra để chặn.
        // (Sửa chú thích 2026-09-01: bản trước nói "không phải JSON thì không đệm" — mô tả một
        // hàng rào code không có.)
        if (request.ContentLength > MaxBufferedBodyBytes)
        {
            return;
        }

        request.EnableBuffering(bufferThreshold: 8 * 1024, bufferLimit: MaxBufferedBodyBytes);

        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: context.RequestAborted);

            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                return;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                // So khớp KHÔNG phân biệt hoa thường: envelope đi dây là camelCase ("userName"),
                // nhưng client khác có thể gửi "UserName" — cùng một trường với System.Text.Json
                // (PropertyNameCaseInsensitive của MVC), nên ở đây phải nhận cả hai.
                if (!string.Equals(property.Name, "userName", StringComparison.OrdinalIgnoreCase)
                    || property.Value.ValueKind is not JsonValueKind.String)
                {
                    continue;
                }

                var userName = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(userName) && userName.Length <= MaxUserNameLength)
                {
                    context.Items[UserNameItemKey] = userName.Trim().ToLowerInvariant();
                }

                break;
            }
        }
        catch (JsonException)
        {
            // Thân không phải JSON hợp lệ — không phải việc của middleware này. Rơi về phân vùng
            // dùng chung; MVC/FluentValidation sẽ trả 400 cho request đó.
        }
        catch (IOException)
        {
            // Vượt bufferLimit hoặc kết nối đứt giữa chừng. Cùng cách xử lý.
        }
        finally
        {
            // ⚠️ BƯỚC KHÔNG ĐƯỢC QUÊN — trả con trỏ về 0 để model binder đọc lại được thân request.
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }
    }
}

/// <summary>Đăng ký <see cref="LoginUserNameRateLimitMiddleware"/> vào pipeline.</summary>
internal static class LoginUserNameRateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseLoginUserNameCapture(this IApplicationBuilder app)
        => app.UseMiddleware<LoginUserNameRateLimitMiddleware>();
}
