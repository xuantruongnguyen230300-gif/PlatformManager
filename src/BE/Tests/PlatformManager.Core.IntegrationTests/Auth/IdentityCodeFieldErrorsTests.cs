using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PlatformManager.Core.Application.Common;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Auth;

/// <summary>
/// Quyết định người dùng 2026-09-05 (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2), đo
/// <b>trên dây</b>: mã lỗi Identity ra tới client qua <c>fieldErrors</c> kèm đúng ô nhập, và câu
/// <c>message</c> KHÔNG còn chứa mã nào.
///
/// <para><b>Vì sao cần bản HTTP dù đã có test ở tầng handler:</b> <c>fieldErrors</c> phải đi qua
/// serializer thật mới chứng minh được hai thứ mà handler không thấy — khoá dictionary giữ
/// PascalCase (nếu <c>DictionaryKeyPolicy</c> bị đổi thì FE tra <c>'NewPassword'</c> ra rỗng), và
/// trường này thật sự có mặt ở nhánh <b>lỗi nghiệp vụ</b> chứ không riêng nhánh 400. Trước
/// 2026-09-05 <c>ApiResult&lt;T&gt;.BusinessError</c> không có đường nào điền nó.</para>
///
/// <para><b>Vì sao đường đổi mật khẩu là chỗ đo tốt nhất:</b> nó là đường DUY NHẤT có thể ép
/// Identity từ chối bằng một request hợp lệ, không cần dàn cảnh — <c>MinimumLength(6)</c> của
/// FluentValidation cho một mật khẩu 8 ký tự đi qua, rồi
/// <c>IdentityOptions.Password.RequiredLength = 12</c> mới từ chối nó. Hai ngưỡng lệch nhau đó là
/// nợ đã ghi ở §8, và ở đây nó thành công cụ đo.</para>
///
/// <para>Mỗi test method có host RIÊNG (xUnit dựng instance mới cho từng method) — giữ nguyên lý
/// do đã ghi ở <see cref="SessionTerminationTests"/>: policy rate limit "login" đếm chung một khoá
/// "unknown-ip" trong cùng một host.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class IdentityCodeFieldErrorsTests : IAsyncLifetime
{
    /// <summary>8 ký tự: qua được <c>MinimumLength(6)</c> của validator, thua
    /// <c>RequiredLength = 12</c> của Identity ⇒ đúng một mã <c>PasswordTooShort</c>.</summary>
    private const string TooShortForIdentity = "Zq7#kLm2";

    private readonly WebApplicationFactory<Program> _factory;

    public IdentityCodeFieldErrorsTests(PostgresFixture fixture)
    {
        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    /// <summary>
    /// <b>Bẫy 1 của §11.2 trên dây.</b> <c>PasswordMismatch</c> nói về ô "mật khẩu hiện tại". Một
    /// bản cài đặt gom mọi mã của endpoint này về <c>NewPassword</c> vẫn trả 422 đúng, vẫn có
    /// <c>fieldErrors</c>, và chỉ sai đúng một chỗ: nó tô đỏ ô người dùng vừa gõ ĐÚNG.
    /// </summary>
    [Fact(DisplayName = "Sai mật khẩu HIỆN TẠI → fieldErrors.CurrentPassword = PasswordMismatch, KHÔNG phải NewPassword")]
    public async Task WrongCurrentPassword_MarksTheCurrentPasswordInput()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "fe-cur", Roles.User);

        var envelope = await ChangePasswordAsync(client, "Sai@123456789", "Moi@123456789");

        AssertChangePasswordFailedEnvelope(envelope);

        var fieldErrors = envelope.GetProperty("fieldErrors");
        Assert.Equal("PasswordMismatch", fieldErrors.GetProperty("CurrentPassword")[0].GetProperty("code").GetString());

        // Ô "mật khẩu mới" KHÔNG được mang lỗi này — nó là ô người dùng gõ đúng.
        Assert.False(fieldErrors.TryGetProperty("NewPassword", out _));
    }

    [Fact(DisplayName = "Mật khẩu MỚI không đạt chính sách → fieldErrors.NewPassword mang mã Identity, câu sạch mã")]
    public async Task WeakNewPassword_MarksTheNewPasswordInput()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "fe-new", Roles.User);

        var envelope = await ChangePasswordAsync(client, AdminApiTestClient.Password, TooShortForIdentity);

        AssertChangePasswordFailedEnvelope(envelope);

        var newPasswordErrors = envelope.GetProperty("fieldErrors").GetProperty("NewPassword")
            .EnumerateArray().Select(e => e.GetProperty("code").GetString()).ToList();

        // Contains chứ không so bằng phần tử [0]: bộ lọc mật khẩu phổ biến có thể thêm mã thứ hai
        // cho cùng ô, và test này không kiểm chính sách mật khẩu — nó kiểm ĐƯỜNG ĐI của mã.
        Assert.Contains("PasswordTooShort", newPasswordErrors);
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    private static async Task<JsonElement> ChangePasswordAsync(
        HttpClient client, string currentPassword, string newPassword)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new { currentPassword, newPassword });

        // 422 = ErrorCode.BusinessRuleError. 400 ở đây nghĩa là FluentValidation đã chặn TRƯỚC khi
        // Identity kịp nói gì — test sẽ đo nhầm nhánh validate và không chứng minh được gì về mã
        // Identity, nên phải đỏ kèm body để người đọc thấy ngay.
        if (response.StatusCode != HttpStatusCode.UnprocessableEntity)
            Assert.Fail($"Mong đợi 422, nhận {(int)response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    /// <summary>
    /// Phần chung của cả hai ca — và là khẳng định CHỐNG HỒI QUY thật sự của file này: câu người
    /// dùng đọc không được chứa mã nào. Ai đó quay lại khuôn <c>"…thất bại: {Reasons}"</c> +
    /// <c>string.Join("; ", errors)</c> sẽ làm đỏ ngay tại đây, kể cả khi họ điền CẢ
    /// <c>fieldErrors</c> cho đúng bài.
    /// </summary>
    private static void AssertChangePasswordFailedEnvelope(JsonElement envelope)
    {
        Assert.Equal("BUSINESS_ERROR", envelope.GetProperty("status").GetString());
        Assert.Equal("AUTH.CHANGE_PASSWORD_FAILED", envelope.GetProperty("businessCode").GetString());

        var message = envelope.GetProperty("message").GetString();
        Assert.Equal("Đổi mật khẩu thất bại.", message);
        Assert.DoesNotContain("Password", message);

        // messageParams phải VẮNG hẳn (không phải rỗng): câu này không còn tham số nào.
        Assert.False(envelope.TryGetProperty("messageParams", out _));

        // Khoá của fieldErrors giữ PascalCase — DictionaryKeyPolicy cố ý không set. Nếu ai đó bật
        // camelCase cho khoá dictionary thì FE tra 'NewPassword' ra rỗng mà không có gì báo.
        Assert.True(envelope.TryGetProperty("fieldErrors", out _));
    }
}
