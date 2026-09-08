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
    /// <c>UserFormField</c> phía FE. DÙNG CHUNG cho cả hai đường: đó là cùng một hộp thoại, và các
    /// ô chỉ có ở chế độ thêm (<c>TempPassword</c>, <c>UserName</c>) thì đường sửa không bao giờ
    /// sinh ra mã tương ứng — khai thừa ở đây rẻ hơn nhiều so với hai preset lệch nhau.
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

        // CỐ Ý không có ở đây, để rơi về RecordKey: ConcurrencyFailure, DefaultError, InvalidToken,
        // UserLockoutNotEnabled, LoginAlreadyAssociated, RecoveryCodeRedemptionFailed,
        // UserAlreadyHasPassword. Chúng nói về bản ghi/trạng thái, không về một ô đang hiển thị.
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
