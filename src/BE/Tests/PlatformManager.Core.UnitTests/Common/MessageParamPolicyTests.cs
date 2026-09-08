using FluentValidation;
using PlatformManager.Core.Application.Common.Results;
using Xunit;

namespace PlatformManager.Core.UnitTests.Common;

/// <summary>
/// Canh <b>ràng buộc bảo mật</b> của cơ chế <c>messageParams</c>
/// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.4): chỉ khoá trong allowlist mới rời khỏi
/// BE, và <c>PropertyValue</c> — giá trị người dùng vừa gõ — KHÔNG BAO GIỜ nằm trong đó.
///
/// <para><b>Lỗi thật nó canh, không phải rủi ro lý thuyết.</b> FluentValidation nạp khoá
/// <c>PropertyValue</c> vào <c>FormattedMessagePlaceholderValues</c> cho MỌI failure, bất kể câu
/// lỗi có dùng tới nó hay không. Repo có màn đổi mật khẩu đang kiểm ràng buộc độ dài, nên một bản
/// sửa "chuyển tiếp cho gọn" sẽ đẩy mật khẩu mới vừa nhập ra HTTP response ở đúng lần nhập hỏng —
/// rồi vào log trình duyệt và mọi telemetry phía client. Không có gì đỏ, không có gì báo: response
/// vẫn hợp lệ, giao diện vẫn chạy đúng.</para>
///
/// <para><b>Vì sao test đứng ở tầng lớp lọc chứ không ở tầng HTTP:</b> đây là bất biến của CHÍNH
/// phép lọc. Kiểm qua HTTP thì nó chỉ đúng cho những đường đã có test HTTP, còn đường thứ hai dùng
/// lại lớp này vẫn hở. Shape trên dây do
/// <c>PlatformManager.Core.IntegrationTests.Common.ValidationEnvelopeShapeTests</c> gánh.</para>
/// </summary>
public class MessageParamPolicyTests
{
    /// <summary>
    /// Dựng từ điển chỗ giữ bằng CHÍNH FluentValidation, không viết tay: thứ cần chứng minh là
    /// "cái thư viện thật sự nạp vào thì bị chặn", và một từ điển viết tay chỉ chứng minh được về
    /// giả định của người viết test.
    /// </summary>
    private sealed class Probe
    {
        public string NewPassword { get; init; } = string.Empty;
    }

    private sealed class ProbeValidator : AbstractValidator<Probe>
    {
        public ProbeValidator() => RuleFor(x => x.NewPassword).MinimumLength(12);
    }

    private static IReadOnlyDictionary<string, object> PlaceholdersFromRealValidator(string typedValue)
    {
        var result = new ProbeValidator().Validate(new Probe { NewPassword = typedValue });

        Assert.False(result.IsValid,
            "Rule dựng ca thử không còn fail ⇒ test này không đo gì. Sửa dữ liệu thử, ĐỪNG bỏ test.");

        return result.Errors[0].FormattedMessagePlaceholderValues;
    }

    [Fact(DisplayName = "PropertyValue (giá trị người dùng vừa gõ) KHÔNG BAO GIỜ ra tới messageParams")]
    public void UserTypedValue_NeverLeaves_TheServer()
    {
        // Ngắn hơn ngưỡng của rule để nó FAIL — giá trị này chính là thứ không được ra khỏi BE.
        const string secret = "mat-khau";
        var placeholders = PlaceholdersFromRealValidator(secret);

        // Đối chứng cho chính ca thử: nếu thư viện thôi nạp khoá này thì phép lọc bên dưới không
        // còn chặn cái gì, và test sẽ xanh vì không có gì để chặn — tệ hơn là đỏ.
        Assert.True(placeholders.ContainsKey(MessageParamPolicy.UserSuppliedValueKey),
            "FluentValidation không còn nạp PropertyValue ⇒ ca thử này đã hết tác dụng. Kiểm lại bản " +
            "thư viện trước khi kết luận allowlist là thừa.");
        Assert.Equal(secret, placeholders[MessageParamPolicy.UserSuppliedValueKey]);

        var kept = MessageParamPolicy.FromValidationPlaceholders(placeholders);

        Assert.NotNull(kept);
        Assert.False(kept!.ContainsKey(MessageParamPolicy.UserSuppliedValueKey),
            "PropertyValue lọt qua allowlist ⇒ giá trị người dùng vừa gõ (mật khẩu mới, số điện thoại…) " +
            "đi thẳng ra HTTP response. Xem doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.4.");
        Assert.DoesNotContain(secret, kept.Values);
    }

    [Fact(DisplayName = "Khoá chính sách (MinLength) ĐƯỢC chuyển tiếp — allowlist không chặn hết")]
    public void PolicyKeys_DoPass_TheAllowlist()
    {
        var kept = MessageParamPolicy.FromValidationPlaceholders(PlaceholdersFromRealValidator("ngan"));

        Assert.NotNull(kept);

        // Chính là con số §10.6 nói tới: câu người dùng đọc phải dựng TỪ CON SỐ BE GỬI. Chặn nốt
        // khoá này thì allowlist "an toàn tuyệt đối" mà vô dụng — và ai đó sẽ gỡ nó ra.
        Assert.Equal("12", kept!["MinLength"]);
    }

    [Fact(DisplayName = "Không khoá nào qua được allowlist ⇒ NULL (vắng mặt trên dây), không phải từ điển rỗng")]
    public void NothingAllowed_Yields_Null()
    {
        var onlyForbidden = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [MessageParamPolicy.UserSuppliedValueKey] = "mat-khau-that",
        };

        Assert.Null(MessageParamPolicy.FromValidationPlaceholders(onlyForbidden));
        Assert.Null(MessageParamPolicy.FromValidationPlaceholders(null));
        Assert.Null(MessageParamPolicy.FromValidationPlaceholders(new Dictionary<string, object>()));
    }

    /// <summary>
    /// Số phải ra chuỗi theo văn hoá invariant. Với <c>vi-VN</c>, dấu thập phân là dấu phẩy — một
    /// hợp đồng API đổi nghĩa theo cấu hình tiến trình là hợp đồng không ai kiểm được.
    /// </summary>
    [Fact(DisplayName = "Giá trị đổi sang chuỗi bằng văn hoá invariant, không theo cấu hình tiến trình")]
    public void Values_Are_Stringified_Invariantly()
    {
        Assert.Equal("12", MessageParamPolicy.Stringify(12));
        Assert.Equal("1.5", MessageParamPolicy.Stringify(1.5m));
        Assert.Equal(string.Empty, MessageParamPolicy.Stringify(null));
        Assert.Equal("abc", MessageParamPolicy.Stringify("abc"));
    }
}
