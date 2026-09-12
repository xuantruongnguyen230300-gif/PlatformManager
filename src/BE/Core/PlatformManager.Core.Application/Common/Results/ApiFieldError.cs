namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Một lỗi validate của MỘT field, dạng <b>mã + câu</b> — phần tử của
/// <c>IApiResult&lt;T&gt;.FieldErrors</c>.
///
/// <para><b>Vì sao tồn tại</b> (2026-09-03): trước đó nhánh 400 chỉ trả <c>Fields</c> =
/// <c>Dictionary&lt;string, string[]&gt;</c>, tức <b>chuỗi trần</b>. Client không có gì để tra
/// nên buộc phải hiển thị thẳng câu do BE sinh ra ⇒ "BE sở hữu câu chữ" trở thành mặc định
/// <b>do thiếu một field</b>, chứ không do ai quyết định như vậy. Xem
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(b).</para>
///
/// <para><b><see cref="Code"/> KHÔNG theo khuôn <c>MIEN.MA_LOI</c>, và đó là chủ đích.</b> Nó là
/// <c>ValidationFailure.ErrorCode</c> của FluentValidation — mặc định bằng TÊN VALIDATOR
/// (<c>NotEmptyValidator</c>, <c>EmailValidator</c>, <c>GreaterThanValidator</c>…). Đặt khoá mới
/// ở đây là dựng hệ mã thứ hai cho cùng một việc trong khi thư viện đã cấp sẵn một hệ ổn định,
/// có sẵn ở mọi rule, và không phải bảo trì. Hai hệ mã nằm ở hai field khác nhau
/// (<c>businessCode</c> = mã nghiệp vụ, <c>fieldErrors[].code</c> = mã lỗi ô nhập) nên chúng
/// không trộn vào nhau như ca đã trả giá ở §4(a) cùng file doc.</para>
///
/// <para><b><see cref="Message"/> vẫn còn ở đây</b> dù mục tiêu là client tra theo mã: nó là
/// <i>dev-facing + fallback</i>. Mã nào chưa có bản dịch thì client còn một câu đọc được thay vì
/// một ô trống — thiếu lối thoát đó, mỗi mã mới thêm ở BE là một chỗ giao diện hiển thị rỗng.</para>
///
/// <para>⚠️ <b>Lời hứa "còn một câu đọc được" chỉ giữ được ở nơi NGUỒN CÓ SẴN MỘT CÂU</b> — tức
/// lỗi FluentValidation, nơi <c>ValidationFailure.ErrorMessage</c> là một câu thật (thu hẹp
/// 2026-09-11 theo core-review). <b>Với mã Identity thì <c>Message</c> bằng đúng <c>Code</c>, và đó
/// là CHỦ ĐÍCH</b>: câu gốc của Identity là tiếng Anh, đẩy nó ra dây là mở lại đường rò chữ mà §4(c)
/// của doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md đã đóng — lý lẽ đầy đủ ở
/// <see cref="IdentityFieldErrors.Build"/>.</para>
///
/// <para>Không có lỗ hổng hiển thị nào từ việc đó: đường lùi của kênh FE không dừng ở
/// <c>message</c>. <c>ApiErrorMessageService.fieldMessage</c> tra bảng dịch theo <c>code</c> trước
/// (bậc 1), chỉ dùng <c>message</c> khi nó KHÁC <c>code</c> (bậc 2), và còn một câu chung ở bậc 3.
/// Mã Identity vì thế luôn rơi vào bậc 1 hoặc bậc 3, không bao giờ hiện ra dưới dạng mã trần.</para>
///
/// <para>Trước lượt thu hẹp này, đoạn trên và <see cref="IdentityFieldErrors"/> nói ngược nhau về
/// cùng một trường — một bên hứa "luôn có câu", một bên đặt <c>Message = Code</c>. Sửa ở đây rẻ hơn
/// refactor và <b>gỡ mâu thuẫn</b> thay vì để hai file cãi nhau (.claude/CLAUDE.md §5).</para>
/// </summary>
/// <param name="Code">Xem phần mô tả của record.</param>
/// <param name="Message">Xem phần mô tả của record.</param>
/// <param name="MessageParams">
/// Tham số RỜI của câu mà <paramref name="Code"/> trỏ tới — CÙNG trường, CÙNG kiểu, CÙNG cách đọc
/// với <c>IApiResult&lt;T&gt;.MessageParams</c> (thêm 2026-09-04,
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.1). Nguồn giá trị là
/// <c>ValidationFailure.FormattedMessagePlaceholderValues</c> của FluentValidation, ĐÃ lọc qua
/// <see cref="MessageParamPolicy"/>.
///
/// <para><b>Vì sao nằm ở đây chứ không gom hết về gốc envelope:</b> một lần submit hỏng nhiều
/// field thì mỗi field có bộ tham số RIÊNG. Form đổi mật khẩu có <c>NewPassword</c> và
/// <c>ConfirmPassword</c>; màn tạo người dùng có <c>UserName</c> và <c>Email</c> với hai giới hạn
/// độ dài khác nhau. Nhét chung một từ điển ở gốc thì hai field cùng fail một validator ghi đè
/// khoá của nhau — client vẫn dựng được câu, chỉ là câu của field này mang con số của field kia.
/// Hỏng im lặng, không test nào của client bắt được.</para>
///
/// <para>NULL ⇒ vắng mặt trên dây; mặc định null nên mọi nơi dựng cũ giữ nguyên hành vi.</para>
/// </param>
public sealed record ApiFieldError(
    string Code,
    string Message,
    Dictionary<string, string>? MessageParams = null)
{
    /// <summary>
    /// Mã dùng khi FluentValidation KHÔNG cấp mã nào — xảy ra thật với rule
    /// <c>Custom</c>/<c>CustomAsync</c> báo lỗi qua <c>context.AddFailure(prop, message)</c>:
    /// overload đó dựng <c>ValidationFailure</c> với <c>ErrorCode</c> null (repo đang có 3 chỗ
    /// như vậy trong <c>UpdatePermissionMatrixCommand</c> / <c>UpdateResourcePermissionMatrixCommand</c>).
    ///
    /// <para><b>Vì sao một hằng số chung thay vì đặt mã riêng cho từng chỗ:</b> ba thông điệp đó
    /// đang <b>ghép sẵn tham số vào câu</b> ("Thiếu 3 mục menu: A, B, C") nên dù có mã, client vẫn
    /// không tách lại được tham số để ráp vào câu của ngôn ngữ khác — đặt mã lúc này chỉ tạo cảm
    /// giác đã dịch được. Tách tham số ra khỏi câu là việc riêng, thuộc bước bọc chuỗi
    /// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §7 bước 10).</para>
    ///
    /// <para><b>Không để <see cref="Code"/> null</b> cho ca này: null buộc MỌI nơi đọc phải kiểm
    /// null cho một trường mà cả hợp đồng nói là luôn có, và cái giá đó trả ở phía client, nhân
    /// lên theo số nơi đọc.</para>
    /// </summary>
    public const string UnspecifiedCode = "UnspecifiedValidator";

    /// <summary>
    /// Mã cho lỗi <b>model binding</b> — sai kiểu, thiếu field bắt buộc của record vị trí, JSON
    /// hỏng. Khác <see cref="UnspecifiedCode"/> ở chỗ đây KHÔNG phải "validator không cấp mã" mà
    /// là "lỗi xảy ra TRƯỚC khi có validator nào chạy".
    ///
    /// <para>Tách riêng vì client cần phân biệt được hai ca: mã này nghĩa là <i>payload không
    /// đọc được</i> (thường là bug của client), còn mã validator nghĩa là <i>payload đọc được
    /// nhưng giá trị sai</i> (thường là người dùng nhập thiếu). Gộp chung thì một bug tích hợp
    /// hiện ra cho người dùng cuối y như một ô bỏ trống.</para>
    /// </summary>
    public const string ModelBindingCode = "ModelBindingValidator";
}
