using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common.Results;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Common;

/// <summary>
/// Nhánh 400 của <see cref="GlobalExceptionHandler"/> — <b>chỗ duy nhất</b> lỗi validate biến
/// thành envelope, và tới 2026-09-03 là nhánh lỗi duy nhất rời khỏi BE mà không mang mã nào
/// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(b)).
///
/// <para><b>Vì sao gọi thẳng handler thay vì bắn HTTP như phần còn lại của project này:</b> ca
/// quan trọng nhất ở đây là <c>ValidationFailure.ErrorCode</c> <b>null</b> — thứ chỉ sinh ra từ
/// rule <c>Custom</c>/<c>CustomAsync</c>. Dựng ca đó qua HTTP nghĩa là phải seed đủ dữ liệu ma
/// trận quyền để một validator coverage chịu chạy, và khi đó test đỏ vì bất cứ lý do nào trong
/// chuỗi seed cũng trông y hệt như đỏ vì bộ dựng envelope hỏng. Handler không chạm DB, không cần
/// host, nên nó kiểm đúng một thứ.</para>
///
/// <para>Đổi lại, test này KHÔNG chứng minh được casing trên dây (khoá <c>fields</c> PascalCase
/// còn property thì camelCase) — phần đó do
/// <see cref="Users.UserListQueryValidationSeamTests"/> gánh, bằng JSON thật.</para>
///
/// <para>KHÔNG khai <c>[Collection]</c>: không dùng Postgres, và gắn vào collection dùng chung sẽ
/// buộc nó xếp hàng sau các test có container mà chẳng được gì.</para>
/// </summary>
public sealed class ValidationEnvelopeShapeTests
{
    [Fact(DisplayName = "Nhánh 400 mang businessCode VALIDATION.FAILED (trước 2026-09-03: không mã nào)")]
    public async Task ValidationBranch_Carries_ABusinessCode()
    {
        var result = await HandleAsync(new ValidationFailure("PageSize", "Số dòng mỗi trang phải từ 1 đến 200.")
        {
            ErrorCode = "InclusiveBetweenValidator",
        });

        Assert.Equal("VALIDATION_ERROR", result.GetProperty("status").GetString());
        Assert.Equal("ValidationError", result.GetProperty("code").GetString());

        // Khẳng định GIÁ TRỊ chứ không chỉ "có trường businessCode": mã này là thứ client tra để
        // biết đây là lỗi payload, và nó sẽ thành KHOÁ DỊCH ở bước i18n. Một chuỗi rỗng hay một
        // mã khác vẫn qua được phép kiểm "có mặt".
        Assert.Equal(ValidationErrors.Failed.BusinessCode, result.GetProperty("businessCode").GetString());
    }

    [Fact(DisplayName = "fieldErrors mang MÃ của FluentValidation, không phải khoá tự đặt")]
    public async Task FieldErrors_Carry_TheFluentValidationErrorCode()
    {
        var result = await HandleAsync(new ValidationFailure("PageSize", "Số dòng mỗi trang phải từ 1 đến 200.")
        {
            ErrorCode = "InclusiveBetweenValidator",
        });

        var pageSize = result.GetProperty("fieldErrors").GetProperty("PageSize");

        // "InclusiveBetweenValidator" là tên validator do FluentValidation tự đặt. Ghim nó ở đây
        // vì nó KHÔNG còn là chi tiết nội bộ của thư viện sau quyết định 2026-09-03: nó là khoá
        // client tra sang câu của ngôn ngữ đang chọn. Thư viện đổi tên (hoặc ai đó thêm
        // .WithErrorCode) là đổi một khoá đã phát tán ra bảng dịch vi + en + mã FE — phải thấy ở
        // đây, không phải thấy qua một ô trống trên giao diện tiếng Anh.
        Assert.Equal("InclusiveBetweenValidator", pageSize[0].GetProperty("code").GetString());
        Assert.Equal("Số dòng mỗi trang phải từ 1 đến 200.", pageSize[0].GetProperty("message").GetString());
    }

    /// <summary>
    /// Ca <c>ErrorCode</c> null — có thật trong repo: <c>context.AddFailure(prop, message)</c>
    /// của rule <c>Custom</c>/<c>CustomAsync</c> để lại <c>ErrorCode</c> null (3 chỗ trong
    /// <c>UpdatePermissionMatrixCommand</c>/<c>UpdateResourcePermissionMatrixCommand</c>).
    /// </summary>
    [Fact(DisplayName = "ErrorCode null (rule Custom) → mã dự phòng, KHÔNG phải null ra tới client")]
    public async Task FieldErrors_FallBack_WhenFluentValidationGivesNoCode()
    {
        var result = await HandleAsync(new ValidationFailure("Entries", "Thiếu 3 mục menu trong entries."));

        var code = result.GetProperty("fieldErrors").GetProperty("Entries")[0].GetProperty("code");

        Assert.Equal(JsonValueKind.String, code.ValueKind);
        Assert.Equal(ApiFieldError.UnspecifiedCode, code.GetString());
    }

    /// <summary>
    /// Ca 1 của cơ chế tham số (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.3): tham số của
    /// lỗi validate đi ra dưới dạng RỜI, lấy thẳng từ từ điển FluentValidation vốn đã dựng sẵn.
    ///
    /// <para><b>Chạy validator THẬT thay vì tự dựng <c>ValidationFailure</c>:</b> thứ đang chốt là
    /// "cái thư viện thật sự nạp thì đi ra đúng như mong đợi". Một failure viết tay chỉ chứng minh
    /// được về giả định của người viết test — và giả định sai chính là chỗ hỏng: từ điển thật còn
    /// mang <c>PropertyValue</c>, thứ KHÔNG được ra ngoài.</para>
    /// </summary>
    [Fact(DisplayName = "fieldErrors mang messageParams, và TUYỆT ĐỐI không mang giá trị người dùng vừa gõ")]
    public async Task FieldErrors_Carry_MessageParams_ButNeverTheTypedValue()
    {
        // Chuỗi đủ lạ để tìm được trong JSON thô — nó đóng vai "mật khẩu mới" của ca thật.
        const string typed = "bimat-9";

        var failures = new ProbeValidator().Validate(new Probe { NewPassword = typed }).Errors;
        Assert.NotEmpty(failures);

        var result = await HandleExceptionAsync(new ValidationException(failures));
        var messageParams = result.GetProperty("fieldErrors").GetProperty("NewPassword")[0]
            .GetProperty("messageParams");

        // Con số chính sách do BE sở hữu (§10.6): client dựng câu TỪ con số này, không từ hằng số
        // của chính nó — hai bản sao của một chính sách đã trôi thật một lần.
        Assert.Equal("12", messageParams.GetProperty("MinLength").GetString());

        Assert.False(messageParams.TryGetProperty(MessageParamPolicy.UserSuppliedValueKey, out _),
            "PropertyValue ra tới envelope ⇒ giá trị người dùng vừa gõ (mật khẩu mới) nằm trong HTTP " +
            "response. Xem doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.4.");

        // Kiểm trên JSON THÔ chứ không chỉ trên khoá đã biết: chuỗi bí mật có thể lọt ra qua một
        // khoá khác mà allowlist chưa nghĩ tới, và phép kiểm theo tên khoá sẽ không thấy.
        Assert.DoesNotContain(typed, result.GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Ca ĐỐI CHỨNG cho ca trên: failure không có chỗ giữ nào qua được allowlist thì trường phải
    /// NULL (⇒ vắng mặt trên dây), không phải một object rỗng. Thiếu ca này, một bản sửa gán từ
    /// điển cho MỌI lỗi vẫn xanh và client phải viết thêm nhánh xử lý cho một thứ vô nghĩa.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: không tham số nào qua allowlist → messageParams null, không phải {}")]
    public async Task FieldErrors_LeaveMessageParamsNull_WhenNothingPasses()
    {
        var result = await HandleAsync(new ValidationFailure("Entries", "Thiếu 3 mục menu trong entries."));

        var messageParams = result.GetProperty("fieldErrors").GetProperty("Entries")[0]
            .GetProperty("messageParams");

        Assert.Equal(JsonValueKind.Null, messageParams.ValueKind);
    }

    /// <summary>
    /// Bất biến của giai đoạn DI TRÚ SONG SONG (bước 11 của §7 chưa chạy): hai trường phải mô tả
    /// CÙNG một tập lỗi. Mất bất biến này thì client đọc trường mới thấy ít lỗi hơn client đọc
    /// trường cũ — và không có gì báo, vì mỗi trường đọc riêng vẫn hợp lệ.
    /// </summary>
    [Fact(DisplayName = "fields và fieldErrors mô tả CÙNG tập lỗi (bất biến của di trú song song)")]
    public async Task Fields_And_FieldErrors_StayInSync()
    {
        var result = await HandleAsync(
            new ValidationFailure("Page", "Số trang phải từ 1 trở lên.") { ErrorCode = "GreaterThanOrEqualValidator" },
            new ValidationFailure("PageSize", "Số dòng mỗi trang phải từ 1 đến 200.") { ErrorCode = "InclusiveBetweenValidator" },
            new ValidationFailure("PageSize", "Lỗi thứ hai trên cùng một field.") { ErrorCode = "PredicateValidator" });

        var fields = result.GetProperty("fields");
        var fieldErrors = result.GetProperty("fieldErrors");

        var fieldKeys = fields.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
        var errorKeys = fieldErrors.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(fieldKeys, errorKeys);

        foreach (var key in fieldKeys)
        {
            var messages = fields.GetProperty(key).EnumerateArray().Select(m => m.GetString()).ToList();
            var richer = fieldErrors.GetProperty(key).EnumerateArray()
                .Select(m => m.GetProperty("message").GetString()).ToList();

            // So cả THỨ TỰ, không chỉ nội dung: hai trường sinh ra từ cùng một phép gộp nên thứ tự
            // phải trùng. Lệch thứ tự là dấu hiệu ai đó đã tách thành hai phép gộp độc lập.
            Assert.Equal(messages, richer);
        }
    }

    /// <summary>
    /// Ca ĐỐI CHỨNG: nhánh không phải validate không được mọc <c>fieldErrors</c>. Thiếu ca này,
    /// một bản sửa gán trường mới cho MỌI envelope vẫn xanh — và client sẽ thấy một dictionary
    /// rỗng ở chỗ đáng lẽ không có gì, tức phải viết thêm nhánh xử lý cho một thứ vô nghĩa.
    ///
    /// <para><b>Kiểm "giá trị null" chứ không kiểm "vắng mặt", và lý do đáng ghi lại vì bản đầu
    /// của test này đã ĐỎ đúng ở đây (2026-09-03):</b> việc lược bỏ thuộc tính null là do
    /// <c>DefaultIgnoreCondition = WhenWritingNull</c> mà Program.cs cấu hình cho <c>Http.Json</c>,
    /// KHÔNG phải mặc định của <c>System.Text.Json</c>. Test này chạy handler ngoài host nên
    /// <c>WriteAsJsonAsync</c> không phân giải được tuỳ chọn đó và ghi ra cả thuộc tính null.</para>
    ///
    /// <para>Vì vậy khẳng định "vắng mặt TRÊN DÂY" phải do một test chạy host thật gánh, và nó có
    /// thật: <see cref="RateLimiting.GlobalRateLimitTests"/> kiểm <c>fields</c> lẫn
    /// <c>fieldErrors</c> đều không xuất hiện trong envelope 429. Dựng lại cấu hình JSON của
    /// Program.cs vào đây để test "đúng hơn" sẽ tạo bản sao thứ hai của cấu hình đó — bản sao ấy
    /// vẫn xanh sau khi bản thật đổi, tức test sẽ nói dối đúng lúc cần nó nói thật.</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: nhánh 500 để fields/fieldErrors NULL (vắng mặt trên dây: xem GlobalRateLimitTests)")]
    public async Task NonValidationBranch_HasNeitherField()
    {
        var result = await HandleExceptionAsync(new InvalidOperationException("bug giả lập"));

        Assert.Equal("SYSTEM_ERROR", result.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("fields").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("fieldErrors").ValueKind);
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>
    /// Đối tượng + validator dựng ca "rule độ dài của mật khẩu mới fail" — ca có thật trên màn đổi
    /// mật khẩu, và là ca khiến allowlist tồn tại.
    /// </summary>
    private sealed class Probe
    {
        public string NewPassword { get; init; } = string.Empty;
    }

    private sealed class ProbeValidator : AbstractValidator<Probe>
    {
        public ProbeValidator() => RuleFor(x => x.NewPassword).MinimumLength(12);
    }

    private static Task<JsonElement> HandleAsync(params ValidationFailure[] failures)
        => HandleExceptionAsync(new ValidationException(failures));

    /// <summary>
    /// Chạy handler thật trên một <see cref="DefaultHttpContext"/> và đọc lại JSON nó ghi ra.
    ///
    /// <para>Đọc JSON chứ không đọc object trả về: thứ đang chốt là <b>hợp đồng trên dây</b>
    /// (trường nào có mặt, tên trường ra sao). Một khẳng định trên <c>ApiResult&lt;object&gt;</c>
    /// vẫn xanh kể cả khi trường mới bị serializer bỏ qua.</para>
    ///
    /// <para><b>Cái mà test này KHÔNG chứng minh:</b> <c>WriteAsJsonAsync</c> lấy tuỳ chọn JSON từ
    /// DI của request, mà ở đây không có DI — nên nó chạy với mặc định của
    /// <c>System.Text.Json</c> (camelCase, nhưng <b>không</b> lược thuộc tính null), KHÔNG phải
    /// cấu hình <c>Http.Json</c> của Program.cs. Vì vậy mọi khẳng định về việc trường nào bị lược
    /// bỏ phải đọc ở test chạy host thật — xem ghi chú tại
    /// <see cref="NonValidationBranch_HasNeitherField"/>. Casing thì trùng nhau ở cả hai đường nên
    /// đọc <c>"PageSize"</c> (khoá dictionary) cạnh <c>"code"</c> (property) là đúng cho cả hai.</para>
    /// </summary>
    private static async Task<JsonElement> HandleExceptionAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;

        var handled = await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled, "Handler từ chối xử lý ⇒ response thật sẽ là trang lỗi mặc định, không phải envelope.");

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        return document.RootElement.Clone();
    }
}
