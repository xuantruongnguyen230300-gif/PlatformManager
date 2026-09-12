namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Ô nhập mà một mã lỗi Identity NÓI VỀ — khái niệm, chưa phải tên field trên form.
///
/// <para><b>Vì sao tách hai tầng</b> (mã → chỗ này → tên field): quy tắc "mã nào nói về cái gì" là
/// thuộc tính của <b>Identity</b> nên nó giống nhau ở mọi đường; còn "chỗ đó tên là gì trên form"
/// là thuộc tính của <b>từng form</b> (ô mật khẩu mới tên <c>NewPassword</c> ở màn đổi mật khẩu
/// nhưng tên <c>TempPassword</c> ở màn tạo người dùng). Gộp làm một sẽ buộc mỗi đường chép lại
/// toàn bộ bảng mã — và ba bản chép thì chúng lệch nhau, đúng ràng buộc số 2 của
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2 cấm.</para>
/// </summary>
public enum IdentityFieldSlot
{
    /// <summary>Không thuộc ô nhập nào — nói về BẢN GHI (<c>ConcurrencyFailure</c>) hoặc về một
    /// trạng thái nội bộ. Bẫy 2 của §11.2.</summary>
    Record = 0,

    /// <summary>Mật khẩu người dùng vừa ĐẶT (mọi mã độ mạnh mật khẩu).</summary>
    NewPassword,

    /// <summary>Mật khẩu HIỆN TẠI người dùng vừa gõ để tự xác thực (<c>PasswordMismatch</c>).</summary>
    CurrentPassword,

    UserName,

    Email,

    Roles,
}

/// <summary>
/// Tên field THẬT của một form, theo từng <see cref="IdentityFieldSlot"/>. <c>null</c> ⇒ form này
/// không có ô đó ⇒ mã rơi về khoá bản ghi (<see cref="IdentityFieldErrors.RecordKey"/>).
///
/// <para><b>Tên phải khớp TÊN PROPERTY C# của command, PascalCase</b> — đó chính là bộ khoá mà FE
/// bind lỗi lên form (cùng quy ước với <c>Fields</c>, xem <c>NormalizeField</c> ở
/// <c>GlobalExceptionHandler</c>). Hai preset dưới đây khai CẠNH NHAU có chủ đích: chúng là chỗ
/// duy nhất hai form được phép khác nhau, nên khác biệt nào cũng đọc thấy ngay tại đây thay vì
/// phải đi lục ba file lệnh.</para>
/// </summary>
public sealed record IdentityFormFields(
    string? NewPassword = null,
    string? CurrentPassword = null,
    string? UserName = null,
    string? Email = null,
    string? Roles = null)
{
    /// <summary>Màn "Đổi mật khẩu" — khớp <c>ChangePasswordCommand</c> và union
    /// <c>ChangePasswordField</c> phía FE.</summary>
    public static readonly IdentityFormFields ChangePassword = new(
        NewPassword: "NewPassword",
        CurrentPassword: "CurrentPassword");

    /// <summary>
    /// Màn "Thêm/Sửa người dùng" — khớp <c>CreateUserCommand</c>/<c>UpdateUserCommand</c> và union
    /// <c>UserFormField</c> phía FE. DÙNG CHUNG cho cả hai đường: đó là cùng một hộp thoại, và khai
    /// thừa vài ô rẻ hơn nhiều so với hai preset lệch nhau.
    ///
    /// <para>⚠️ <b>Hạ giọng 2026-09-11.</b> Bản trước khẳng định <i>"các ô chỉ có ở chế độ thêm
    /// (<c>TempPassword</c>, <c>UserName</c>) thì đường sửa KHÔNG BAO GIỜ sinh ra mã tương ứng"</i>.
    /// Không đúng về lý thuyết: <c>userManager.UpdateAsync</c> chạy lại <c>IUserValidator</c>, nên
    /// <c>InvalidUserName</c>/<c>DuplicateUserName</c> tới được đây từ đường SỬA — và khi đó mã gắn
    /// vào một ô <b>không tồn tại</b> trên dialog ở chế độ sửa, tức lỗi TÀNG HÌNH, đúng hình dạng ca
    /// <c>CommonPassword</c> ngày 2026-09-11 (câu lỗi có thật nhưng không hiện ở đâu).</para>
    ///
    /// <para>Chưa đổi gì vì hôm nay đường sửa không cho đổi <c>UserName</c>, nên ca đó không tới
    /// được từ giao diện. Ghi ra để nó là rủi ro CÓ KHAI: ngày nào dialog cho sửa tên đăng nhập thì
    /// đây là chỗ phải xem lại trước.</para>
    /// </summary>
    public static readonly IdentityFormFields UserForm = new(
        NewPassword: "TempPassword",
        UserName: "UserName",
        Email: "Email",
        Roles: "Roles");

    internal string? For(IdentityFieldSlot slot) => slot switch
    {
        IdentityFieldSlot.NewPassword => NewPassword,
        IdentityFieldSlot.CurrentPassword => CurrentPassword,
        IdentityFieldSlot.UserName => UserName,
        IdentityFieldSlot.Email => Email,
        IdentityFieldSlot.Roles => Roles,
        _ => null,
    };
}

/// <summary>
/// Đổi danh sách <b>mã lỗi ASP.NET Core Identity</b> thành <c>fieldErrors</c> của envelope —
/// quyết định người dùng 2026-09-05, doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2.
///
/// <para><b>Vì sao đây là chỗ ĐÚNG NGỮ NGHĨA chứ không phải mẹo kỹ thuật:</b>
/// <c>PasswordTooShort</c> nói rằng <b>giá trị người dùng vừa gõ vào ô mật khẩu</b> không đạt — nó
/// vốn LÀ lỗi của một ô nhập. Trước 2026-09-05 nó bị <c>string.Join("; ", …)</c> nối vào giữa một
/// câu tiếng Việt ("Đổi mật khẩu thất bại: PasswordTooShort; PasswordRequiresDigit"), tức vừa
/// không dịch được vừa không tô đỏ được ô nào. Nối danh sách vào giữa câu là thứ VỠ khi đổi ngôn
/// ngữ: trật tự từ và cách nối danh sách mỗi ngôn ngữ một khác.</para>
///
/// <para><b>Không tạo họ khoá mới cho FE:</b> <see cref="ApiFieldError.Code"/> từ trước tới nay đã
/// mang mã PascalCase không có dấu chấm (<c>NotEmptyValidator</c>, <c>EmailValidator</c> — mã của
/// FluentValidation). Mã Identity có ĐÚNG hình dạng đó và đi vào ĐÚNG trường đó.</para>
/// </summary>
public static class IdentityFieldErrors
{
    /// <summary>
    /// Khoá cho lỗi KHÔNG thuộc ô nhập nào — fallback bắt buộc của bẫy 2, §11.2. Chốt 2026-09-05.
    ///
    /// <para><b>Vì sao vẫn đi ra dây thay vì bỏ đi:</b> <c>ConcurrencyFailure</c> nói về bản ghi
    /// chứ không về một ô, nhưng nó là thông tin CHẨN ĐOÁN — vứt nó đi là quay lại đúng hiện trạng
    /// mà quyết định 2 (§11.3) tồn tại để sửa: cập nhật hỏng mà không ai biết vì sao, kể cả người
    /// vận hành đọc log.</para>
    ///
    /// <para><b>Vì sao bắt đầu bằng <c>$</c>:</b> ký tự này KHÔNG mở đầu được một định danh C#, nên
    /// khoá này không bao giờ đụng tên property nào do <c>NormalizeField</c> sinh ra. Chọn một từ
    /// tiếng Anh thường (<c>"Record"</c>, <c>"General"</c>) thì ngày có ai đó đặt một property tên
    /// như vậy, lỗi mức bản ghi sẽ ghi đè lỗi của một ô thật — hỏng im lặng, không có gì báo. Phía
    /// FE, union tên ô là kiểu literal đóng nên khoá lạ đơn giản không bind vào ô nào; người dùng
    /// vẫn đọc câu ở <c>message</c>.</para>
    /// </summary>
    public const string RecordKey = "$record";

    /// <summary>
    /// Bảng mã Identity → ô nhập. <b>ALLOWLIST</b>, cùng lý do với <c>MessageParamPolicy</c>: mã
    /// không có trong bảng rơi về <see cref="IdentityFieldSlot.Record"/>. Bản Identity sau thêm mã
    /// mới thì mã đó mặc định KHÔNG bị gắn nhầm vào một ô — chỉ mất chỗ tô đỏ, không tô sai chỗ.
    ///
    /// <para>🛑 <b>ALLOWLIST nghĩa là MỌI mã chưa khai ở đây đều rơi về <c>$record</c>, và bảng này
    /// KHÔNG phủ hết bộ mã của Identity.</b> <c>IdentityErrorDescriber</c> có khoảng hơn hai chục
    /// mã, còn bảng dưới chỉ khai những mã đã có đường đi tới một ô thật. Con số đó không chép ra
    /// đây — đếm bằng
    /// <c>typeof(IdentityErrorDescriber).GetMethods()</c> nếu cần.</para>
    ///
    /// <para><b>Ai thêm một <c>IPasswordValidator</c> / một nguồn lỗi Identity mới thì phải thêm
    /// một dòng vào đây</b> — nếu không, lỗi của nó hiện ở khối lỗi chung cuối form thay vì ngay
    /// dưới ô người dùng đang gõ. Đây không phải giả thuyết: ngày <b>2026-09-11</b> người dùng đặt
    /// mật khẩu <c>qwerty123456</c>, <c>.AddTop10000PasswordValidator&lt;AppUser&gt;()</c>
    /// (đăng ký ở <c>Core.Infrastructure/DependencyInjection.cs</c>) trả mã <c>CommonPassword</c>,
    /// mã đó không có trong bảng ⇒ rơi về <c>$record</c> ⇒ <b>màn hình không hiện lý do nào</b>.
    /// Chính sách mật khẩu không sai; thiếu đúng một dòng ở bảng này.</para>
    ///
    /// <para>⚠️ <b>Ánh xạ theo MÃ, KHÔNG theo ENDPOINT</b> (bẫy 1, §11.2). Phản xạ "đường đổi mật
    /// khẩu ⇒ mọi mã về ô mật khẩu mới" là SAI: <c>userManager.ChangePasswordAsync</c> trả
    /// <c>PasswordMismatch</c> khi <b>mật khẩu HIỆN TẠI</b> nhập sai, nên quy nó về ô mật khẩu mới
    /// là tô đỏ đúng cái ô người dùng đã gõ đúng.</para>
    ///
    /// <para>Tên mã chép từ <c>IdentityErrorDescriber</c> — chúng là giá trị <c>IdentityError.Code</c>
    /// do chính lớp đó đặt, ổn định qua các bản.</para>
    /// </summary>
    private static readonly Dictionary<string, IdentityFieldSlot> SlotByCode = new(StringComparer.Ordinal)
    {
        // Độ mạnh mật khẩu — nói về giá trị vừa đặt.
        //
        // CommonPassword KHÔNG do Identity lõi sinh ra — nó đến từ gói CommonPasswordsValidator
        // (đăng ký bằng .AddTop10000PasswordValidator<AppUser>() ở
        // Core.Infrastructure/DependencyInjection.cs), nhưng đi ra qua CÙNG một
        // IdentityResult.Errors[].Code nên thuộc về đúng bảng này. Thêm 2026-09-11 sau khi mật
        // khẩu `qwerty123456` bị từ chối mà màn hình không hiện lý do nào.
        //
        // ⚠️ ĐỪNG gọi gói này là "PasswordPwned" (bản trước của chú thích gọi sai như vậy): họ
        // validator mang tên Pwned GỌI API HIBP RA INTERNET, đúng thứ mà DependencyInjection.cs
        // khai rõ là đã CỐ Ý TRÁNH — bộ lọc đang dùng hoạt động HOÀN TOÀN OFFLINE bằng danh sách
        // nhúng. Một cái tên sai ở đây dạy người đọc sau rằng đường đặt mật khẩu có gọi ra ngoài.
        //
        // Hệ quả cho bộ test: mã này KHÔNG có trong IdentityErrorDescriber, nên lưới phản chiếu
        // (EveryDescriberCode_IsClassified) không thấy nó. Lưới cho nhóm mã kiểu này là test tích
        // hợp đi qua HTTP thật — xem IdentityCodeFieldErrorsTests.
        ["CommonPassword"] = IdentityFieldSlot.NewPassword,

        ["PasswordTooShort"] = IdentityFieldSlot.NewPassword,
        ["PasswordRequiresDigit"] = IdentityFieldSlot.NewPassword,
        ["PasswordRequiresLower"] = IdentityFieldSlot.NewPassword,
        ["PasswordRequiresUpper"] = IdentityFieldSlot.NewPassword,
        ["PasswordRequiresNonAlphanumeric"] = IdentityFieldSlot.NewPassword,
        ["PasswordRequiresUniqueChars"] = IdentityFieldSlot.NewPassword,

        // Bẫy 1 — mã DUY NHẤT nói về mật khẩu HIỆN TẠI.
        ["PasswordMismatch"] = IdentityFieldSlot.CurrentPassword,

        ["InvalidUserName"] = IdentityFieldSlot.UserName,
        ["DuplicateUserName"] = IdentityFieldSlot.UserName,

        ["InvalidEmail"] = IdentityFieldSlot.Email,
        ["DuplicateEmail"] = IdentityFieldSlot.Email,

        ["InvalidRoleName"] = IdentityFieldSlot.Roles,
        ["DuplicateRoleName"] = IdentityFieldSlot.Roles,
        ["UserAlreadyInRole"] = IdentityFieldSlot.Roles,
        ["UserNotInRole"] = IdentityFieldSlot.Roles,

        // Mã CỐ Ý không có ở đây được khai tường minh ở IntentionallyRecord ngay dưới — KHÔNG phải
        // trong một chú thích, vì một chú thích thì không test nào đọc được.
    };

    /// <summary>
    /// Mã Identity <b>CỐ Ý</b> rơi về <see cref="RecordKey"/>: chúng nói về bản ghi hoặc một trạng
    /// thái nội bộ, không về một ô đang hiển thị trên form.
    ///
    /// <para><b>Vì sao là một tập PUBLIC chứ không phải một dòng chú thích</b> (đổi 2026-09-11):
    /// danh sách này là nửa còn lại của <see cref="SlotByCode"/> — cộng hai cái lại phải ra TRỌN BỘ
    /// mã của <c>IdentityErrorDescriber</c>, và đó là bất biến DUY NHẤT bắt được ca "mã mới xuất
    /// hiện mà không ai khai". Một chú thích không kiểm được bằng máy; chép danh sách sang file test
    /// thì thành hai bản của cùng một danh sách, đúng thứ .claude/CLAUDE.md §5 cấm.</para>
    ///
    /// <para>Bề mặt công khai tăng thêm đúng một tập mô tả một <b>quyết định phân loại</b>, không
    /// phải một chi tiết cài đặt — nó trả lời <i>"mã này cố ý không thuộc ô nào"</i>, câu hỏi mà
    /// người bảo trì cần trả lời được từ ngoài.</para>
    /// </summary>
    public static readonly IReadOnlySet<string> IntentionallyRecord = new HashSet<string>(StringComparer.Ordinal)
    {
        "ConcurrencyFailure",
        "DefaultError",
        "InvalidToken",
        "UserLockoutNotEnabled",
        "LoginAlreadyAssociated",
        "RecoveryCodeRedemptionFailed",
        "UserAlreadyHasPassword",
    };

    /// <summary>Ô nhập mà <paramref name="identityErrorCode"/> nói tới — công khai để test đối
    /// chứng khẳng định được bẫy 1 mà không phải dựng cả một envelope.</summary>
    public static IdentityFieldSlot SlotFor(string identityErrorCode)
        => SlotByCode.GetValueOrDefault(identityErrorCode, IdentityFieldSlot.Record);

    /// <summary>
    /// Gom danh sách mã Identity về <c>fieldErrors</c> theo bộ tên field của MỘT form.
    ///
    /// <para>Trả <b>null</b> khi không có mã nào — null ⇒ trường vắng mặt trên dây, đúng khuôn
    /// <c>Retryable</c>/<c>Fields</c>/<c>MessageParams</c>. Từ điển RỖNG sẽ làm envelope phình thêm
    /// một khoá vô nghĩa.</para>
    ///
    /// <para><b><see cref="ApiFieldError.Message"/> nhận CHÍNH mã đó, không phải một câu tiếng
    /// Việt.</b> Trường này là <i>dev-facing + fallback</i> (§3: với kênh có FE, FE sở hữu câu chữ).
    /// Viết sẵn một câu tiếng Việt cho từng mã Identity ở BE là dựng <b>bộ chữ thứ hai</b> cho đúng
    /// cái kênh FE đã sở hữu — chính thứ §5.2 từ chối khi không bật <c>LanguageManager</c> tiếng
    /// Việt của FluentValidation. Câu thật đến từ bảng dịch của FE, tra bằng mã này.</para>
    ///
    /// <para>🛑 <b><c>Message == Code</c> là CỐ Ý KHÔNG LÀM, không phải "chưa kịp sửa"</b> (đổi
    /// nhãn 2026-09-11 theo core-review). Nhãn cũ ở đây là <i>"giới hạn đã biết, chưa sửa"</i>, và
    /// nhãn đó nguy hiểm: nó nói lý do duy nhất là CHI PHÍ, nên người đọc sau kết luận
    /// <i>"chi phí thì có ngày trả được"</i>, mở một lượt riêng, và <b>tái lập một đường rò tiếng
    /// Anh</b>.</para>
    ///
    /// <para><b>Lý do THỨ NHẤT — i18n, và đây mới là lý do chặn:</b> <c>IdentityError.Description</c>
    /// là câu <b>TIẾNG ANH</b> (<c>CommonPassword</c> ⇒ <i>"The password you chose is too
    /// common."</i>). Đẩy nó ra <c>ApiFieldError.Message</c> là mở lại đúng đường rò mà §4(c) của
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md đóng ngày 2026-09-03, và trái §3 + §5.2. Hai
    /// hệ chữ cho cùng một lỗi thì client không có cách nào biết hệ nào thắng.</para>
    ///
    /// <para><b>Lý do THỨ HAI — chi phí:</b> câu gốc bị bỏ từ rất sớm, mọi nơi dựng danh sách mã đều
    /// làm <c>.Select(e =&gt; e.Code)</c>. Đếm bằng lệnh thay vì tin một con số chép tay:
    /// <c>grep -rn "Errors.Select(e =&gt; e.Code)" src/BE/Core --include=*.cs</c>. Giữ được câu thì
    /// phải đổi kiểu ba record kết quả (<c>ChangePasswordResult</c>, <c>CreateUserOutcome</c>,
    /// <c>UpdateUserOutcome</c>) cùng mọi nơi dựng và mọi handler tiêu thụ.</para>
    ///
    /// <para><b>Mâu thuẫn thật thì nằm ở CHỖ KHÁC, và đã gỡ:</b> lời hứa bị vỡ là lời hứa của
    /// <see cref="ApiFieldError.Message"/> (<i>"client còn một câu đọc được"</i>), không phải của
    /// hàm này. Lời hứa đó đã được thu hẹp tại chính <see cref="ApiFieldError"/> cho đúng phạm vi
    /// nó giữ được. Hai file nay nói cùng một điều.</para>
    /// </summary>
    public static Dictionary<string, ApiFieldError[]>? Build(
        IReadOnlyList<string> identityErrorCodes,
        IdentityFormFields formFields)
    {
        if (identityErrorCodes.Count == 0)
            return null;

        return identityErrorCodes
            .GroupBy(code => formFields.For(SlotFor(code)) ?? RecordKey, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(code => new ApiFieldError(code, code)).ToArray(),
                StringComparer.Ordinal);
    }
}
