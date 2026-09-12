using System.Reflection;
using NSubstitute;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.UnitTests.Common;

/// <summary>
/// Quyết định người dùng 2026-09-05 (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11): mã lỗi
/// Identity đi ra <c>fieldErrors</c> kèm ĐÚNG ô nhập nó nói tới, và <b>KHÔNG</b> bị nối chuỗi vào
/// giữa câu tiếng Việt.
///
/// <para><b>Khẳng định chống hồi quy quan trọng nhất trong file này</b> là
/// <c>Assert.DoesNotContain(&lt;mã&gt;, result.Message)</c>: nó ĐỎ ngay lần đầu có ai đó quay lại
/// khuôn <c>"…thất bại: {Reasons}"</c> + <c>string.Join("; ", errors)</c>. Nếu chỉ khẳng định
/// "fieldErrors có mã" thì một bản sửa điền CẢ HAI chỗ vẫn xanh, và câu ghép tiếng Anh vẫn hiện
/// nguyên cho người dùng — tức thứ quyết định này tồn tại để dọn vẫn còn.</para>
///
/// <para>Đi qua HANDLER thật (không gọi thẳng <c>IdentityFieldErrors.Build</c>) vì chuỗi cần chứng
/// minh có bốn mắt xích và đứt mắt nào cũng KHÔNG gây lỗi biên dịch: khuôn thông điệp phải sạch chỗ
/// giữ, nơi gọi phải gom mã, <c>BaseResponse.Fail</c> phải giữ lại được <c>fieldErrors</c>, và
/// envelope phải mang nó ra. Ba ca thuần <see cref="IdentityFieldErrors"/> ở cuối file là ca ĐỐI
/// CHỨNG cho bảng ánh xạ, không thay thế bốn mắt xích trên.</para>
/// </summary>
public class IdentityFieldErrorsTests
{
    private static readonly Guid CallerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TargetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ICurrentUser Admin()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(CallerId);
        currentUser.Roles.Returns([Roles.Admin]);
        currentUser.IsInRole(Arg.Any<string>()).Returns(call => call.Arg<string>() == Roles.Admin);
        return currentUser;
    }

    private static IUserAdminService UserServiceWithTarget()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(
            new UserDto(TargetId, "target", "target@example.com", "Người dùng đích", [Roles.User], false, false, null));
        return service;
    }

    // ---------- Đường đổi mật khẩu ----------

    /// <summary>
    /// <b>Bẫy 1 của §11.2</b>, và là lý do file này tồn tại: <c>PasswordMismatch</c> nghĩa là MẬT
    /// KHẨU HIỆN TẠI nhập sai. Ánh xạ theo ENDPOINT ("đường đổi mật khẩu ⇒ tất cả về NewPassword")
    /// biên dịch sạch, trông hợp lý, và tô đỏ đúng cái ô người dùng vừa gõ ĐÚNG.
    /// </summary>
    [Fact(DisplayName = "Đổi mật khẩu hỏng → mỗi mã Identity về đúng ô; PasswordMismatch KHÔNG rơi vào NewPassword")]
    public async Task ChangePassword_RoutesEachIdentityCode_ToTheInputItTalksAbout()
    {
        var identityService = Substitute.For<IIdentityService>();
        identityService.ChangePasswordAsync(CallerId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ChangePasswordResult(false, NotFound: false, ["PasswordMismatch", "PasswordTooShort"]));

        var result = await new ChangePasswordHandler(Admin(), identityService)
            .Handle(new ChangePasswordCommand("sai-mat-khau", "ngan"), CancellationToken.None);

        Assert.Equal("AUTH.CHANGE_PASSWORD_FAILED", result.BusinessCode);

        Assert.NotNull(result.FieldErrors);
        Assert.Equal("PasswordMismatch", result.FieldErrors!["CurrentPassword"][0].Code);
        Assert.Equal("PasswordTooShort", result.FieldErrors["NewPassword"][0].Code);

        // Ô người dùng gõ ĐÚNG không được mang lỗi của ô kia.
        Assert.DoesNotContain(
            result.FieldErrors["NewPassword"], error => error.Code == "PasswordMismatch");
    }

    [Fact(DisplayName = "Đổi mật khẩu hỏng → câu KHÔNG chứa mã nào, messageParams VẮNG (chống nối chuỗi quay lại)")]
    public async Task ChangePassword_KeepsIdentityCodes_OutOfTheSentence()
    {
        var identityService = Substitute.For<IIdentityService>();
        identityService.ChangePasswordAsync(CallerId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ChangePasswordResult(false, NotFound: false, ["PasswordTooShort", "PasswordRequiresDigit"]));

        var result = await new ChangePasswordHandler(Admin(), identityService)
            .Handle(new ChangePasswordCommand("mat-khau-cu", "mat-khau-moi"), CancellationToken.None);

        Assert.Equal("Đổi mật khẩu thất bại.", result.Message);

        // Ba khẳng định dưới đây là BA đường quay lại khác nhau của cùng một lỗi, nên phải khẳng
        // định cả ba: nối thẳng vào câu; nối qua chỗ giữ {Reasons}; và để sót dấu ngoặc chưa ráp.
        Assert.DoesNotContain("PasswordTooShort", result.Message);
        Assert.DoesNotContain("PasswordRequiresDigit", result.Message);
        Assert.Null(result.MessageParams);
        Assert.DoesNotContain("{", result.Message);
    }

    /// <summary>
    /// <b>Bẫy 3 của §11.2.</b> Nhánh "không tìm thấy" từng trả
    /// <c>new ChangePasswordResult(false, ["Không tìm thấy người dùng."])</c> — một CÂU nằm trong
    /// danh sách mã. Sau quyết định 1 câu đó sẽ thành <c>fieldErrors[].code</c>, tức một khoá bảng
    /// dịch là một câu tiếng Việt.
    /// </summary>
    [Fact(DisplayName = "Bẫy 3: người dùng không còn → USER.NOT_FOUND, KHÔNG có câu tiếng Việt nằm trong danh sách mã")]
    public async Task ChangePassword_WhenUserVanished_DoesNotSmuggleASentenceIntoTheCodeList()
    {
        var identityService = Substitute.For<IIdentityService>();
        identityService.ChangePasswordAsync(CallerId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ChangePasswordResult(false, NotFound: true, []));

        var result = await new ChangePasswordHandler(Admin(), identityService)
            .Handle(new ChangePasswordCommand("mat-khau-cu", "mat-khau-moi"), CancellationToken.None);

        Assert.Equal("USER.NOT_FOUND", result.BusinessCode);
        Assert.Equal(ErrorCode.NotFound, result.Code);

        // Không có fieldErrors nào: "tài khoản không còn" không nói về một ô nhập nào cả — và
        // tuyệt đối không được biến thành một mã có nội dung là câu tiếng Việt.
        Assert.Null(result.FieldErrors);
    }

    // ---------- Đường tạo người dùng ----------

    [Fact(DisplayName = "Tạo người dùng hỏng → DuplicateUserName về ô UserName, câu sạch mã")]
    public async Task CreateUser_RoutesIdentityCodes_ToTheUserForm()
    {
        var service = Substitute.For<IUserAdminService>();
        service.CreateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new CreateUserOutcome(false, null, ["DuplicateUserName", "PasswordTooShort"]));

        var result = await new CreateUserHandler(service, Admin()).Handle(
            new CreateUserCommand("nguyenvana", "a@example.com", "Nguyễn Văn A", "matkhau", [Roles.User]),
            CancellationToken.None);

        Assert.Equal("USER.CREATE_FAILED", result.BusinessCode);
        Assert.Equal("Tạo người dùng thất bại.", result.Message);
        Assert.DoesNotContain("DuplicateUserName", result.Message);

        Assert.NotNull(result.FieldErrors);
        Assert.Equal("DuplicateUserName", result.FieldErrors!["UserName"][0].Code);

        // Ô mật khẩu của màn TẠO tên là TempPassword, không phải NewPassword — cùng một slot khái
        // niệm, hai tên field khác nhau. Đây là thứ hai preset của IdentityFormFields canh giữ.
        Assert.Equal("PasswordTooShort", result.FieldErrors["TempPassword"][0].Code);
    }

    // ---------- Đường sửa người dùng (quyết định 2) ----------

    [Fact(DisplayName = "Sửa người dùng hỏng → mã THẬT của Identity ra tới envelope, không còn câu bịa")]
    public async Task UpdateUser_SurfacesTheRealIdentityCode_InsteadOfAnInventedSentence()
    {
        var service = UserServiceWithTarget();
        service.UpdateAsync(default, default, default!, default!, default)
            .ReturnsForAnyArgs(UpdateUserOutcome.Rejected(["ConcurrencyFailure"]));

        var result = await new UpdateUserHandler(service, Admin()).Handle(
            new UpdateUserCommand(TargetId, "a@example.com", "Người dùng đích", [Roles.User]),
            CancellationToken.None);

        Assert.Equal("USER.UPDATE_FAILED", result.BusinessCode);
        Assert.Equal("Cập nhật người dùng thất bại.", result.Message);

        // Câu bịa cũ ("Cập nhật người dùng thất bại: không lưu được thay đổi") nói hai lần cùng một
        // điều. Khẳng định này đỏ ngay nếu ai đó viết lại nó thay vì gỡ đi.
        Assert.DoesNotContain("không lưu được", result.Message);
        Assert.Null(result.MessageParams);

        // ConcurrencyFailure nói về BẢN GHI, không về một ô ⇒ khoá dự phòng (bẫy 2 của §11.2).
        Assert.NotNull(result.FieldErrors);
        Assert.Equal("ConcurrencyFailure", result.FieldErrors![IdentityFieldErrors.RecordKey][0].Code);
    }

    /// <summary>
    /// Ràng buộc của §11.3: nhánh <c>FindByIdAsync</c> null KHÔNG phải lỗi Identity. Gộp nó vào
    /// danh sách mã sẽ cho ra <c>USER.UPDATE_FAILED</c> kèm danh sách RỖNG — đúng lại chỗ trống mà
    /// quyết định 2 sinh ra để lấp.
    /// </summary>
    [Fact(DisplayName = "Sửa người dùng: bị xoá xen giữa đọc và ghi → USER.NOT_FOUND, không phải UPDATE_FAILED rỗng")]
    public async Task UpdateUser_WhenRecordVanishedMidRequest_IsNotReportedAsAnIdentityRejection()
    {
        var service = UserServiceWithTarget();
        service.UpdateAsync(default, default, default!, default!, default)
            .ReturnsForAnyArgs(UpdateUserOutcome.UserNotFound());

        var result = await new UpdateUserHandler(service, Admin()).Handle(
            new UpdateUserCommand(TargetId, "a@example.com", "Người dùng đích", [Roles.User]),
            CancellationToken.None);

        Assert.Equal("USER.NOT_FOUND", result.BusinessCode);
        Assert.Equal(ErrorCode.NotFound, result.Code);
        Assert.Null(result.FieldErrors);
    }

    /// <summary>
    /// Bất biến của <see cref="UpdateUserOutcome.Rejected"/>: thất bại thì LUÔN có ít nhất một mã.
    /// Thiếu chặn này, một <c>IdentityResult</c> hỏng với <c>Errors</c> rỗng ra tới envelope thành
    /// một lỗi không kèm nguyên nhân nào — im lặng hơn cả hiện trạng đang sửa.
    /// </summary>
    [Fact(DisplayName = "Outcome từ chối mà không mã nào → thay bằng DefaultError, KHÔNG để danh sách rỗng")]
    public void RejectedOutcome_NeverCarries_AnEmptyCodeList()
    {
        var outcome = UpdateUserOutcome.Rejected([]);

        Assert.False(outcome.Succeeded);
        Assert.False(outcome.NotFound);
        Assert.Equal("DefaultError", Assert.Single(outcome.Errors));
    }

    // ---------- Ca đối chứng cho bảng ánh xạ ----------

    [Theory(DisplayName = "Đối chứng bảng ánh xạ: mã nào nói về ô nào")]
    [InlineData("PasswordMismatch", IdentityFieldSlot.CurrentPassword)]
    [InlineData("PasswordTooShort", IdentityFieldSlot.NewPassword)]
    // CommonPassword — mã của .AddTop10000PasswordValidator<AppUser>(), thêm 2026-09-11. Trước đó
    // nó rơi về $record và màn hình KHÔNG hiện lý do nào; xem ca hồi quy riêng ở cuối file.
    [InlineData("CommonPassword", IdentityFieldSlot.NewPassword)]
    [InlineData("PasswordRequiresDigit", IdentityFieldSlot.NewPassword)]
    [InlineData("DuplicateUserName", IdentityFieldSlot.UserName)]
    [InlineData("DuplicateEmail", IdentityFieldSlot.Email)]
    [InlineData("UserAlreadyInRole", IdentityFieldSlot.Roles)]
    [InlineData("ConcurrencyFailure", IdentityFieldSlot.Record)]
    public void SlotFor_MapsEachKnownCode_ToItsInput(string code, IdentityFieldSlot expected)
        => Assert.Equal(expected, IdentityFieldErrors.SlotFor(code));

    /// <summary>
    /// ALLOWLIST — mã lạ (bản Identity sau thêm mã mới) rơi về khoá bản ghi. Ca ngược lại
    /// (denylist) sẽ gắn mã chưa biết vào một ô bất kỳ: mất chỗ tô đỏ còn chấp nhận được, tô SAI ô
    /// thì không.
    /// </summary>
    [Fact(DisplayName = "Mã Identity chưa biết → rơi về khoá bản ghi, KHÔNG gắn bừa vào một ô")]
    public void UnknownIdentityCode_FallsBackToTheRecordKey()
    {
        Assert.Equal(IdentityFieldSlot.Record, IdentityFieldErrors.SlotFor("MotMaHoanToanMoi"));

        var built = IdentityFieldErrors.Build(["MotMaHoanToanMoi"], IdentityFormFields.UserForm);

        Assert.NotNull(built);
        Assert.Equal("MotMaHoanToanMoi", built![IdentityFieldErrors.RecordKey][0].Code);
    }

    /// <summary>
    /// Khoá dự phòng phải KHÔNG dựng được thành một định danh C#, nếu không nó có thể trùng tên
    /// một property thật và lỗi mức bản ghi sẽ ghi đè lỗi của một ô — hỏng im lặng.
    /// </summary>
    [Fact(DisplayName = "Khoá dự phòng không thể trùng tên property C# nào")]
    public void RecordKey_CannotCollide_WithAnyCSharpPropertyName()
        => Assert.False(char.IsLetter(IdentityFieldErrors.RecordKey[0]) || IdentityFieldErrors.RecordKey[0] == '_');

    /// <summary>Không mã nào ⇒ trường VẮNG trên dây, không phải từ điển rỗng — cùng khuôn
    /// <c>Retryable</c>/<c>Fields</c>/<c>MessageParams</c>.</summary>
    [Fact(DisplayName = "Đối chứng: không mã nào → fieldErrors null (vắng mặt trên dây)")]
    public void Build_ReturnsNull_WhenThereIsNoCode()
        => Assert.Null(IdentityFieldErrors.Build([], IdentityFormFields.ChangePassword));

    // ---------- Hồi quy 2026-09-11: mật khẩu phổ biến ----------

    /// <summary>
    /// 🔴 <b>Ca thật, không phải ca giả định.</b> Người dùng tạo tài khoản với mật khẩu
    /// <c>qwerty123456</c> → Identity từ chối bằng mã <c>CommonPassword</c> (validator của
    /// <c>.AddTop10000PasswordValidator&lt;AppUser&gt;()</c>), mã đó KHÔNG có trong allowlist nên
    /// rơi về <c>$record</c>, và <b>màn hình không hiện lý do nào</b>.
    ///
    /// <para>Khẳng định <c>DoesNotContain(RecordKey)</c> mới là khẳng định chống hồi quy: chỉ kiểm
    /// "có khoá TempPassword" thì một bản sửa gắn mã vào CẢ HAI chỗ vẫn xanh, trong khi khối lỗi
    /// chung cuối form vẫn hiện thừa một dòng.</para>
    /// </summary>
    [Fact(DisplayName = "Hồi quy: CommonPassword về ô mật khẩu, KHÔNG rơi về $record")]
    public void CommonPassword_LandsOnThePasswordInput_NotTheRecordKey()
    {
        Assert.Equal(IdentityFieldSlot.NewPassword, IdentityFieldErrors.SlotFor("CommonPassword"));

        // Màn Thêm/Sửa người dùng — ô mật khẩu ở form này tên `TempPassword`.
        var userForm = IdentityFieldErrors.Build(["CommonPassword"], IdentityFormFields.UserForm);

        Assert.NotNull(userForm);
        Assert.Equal("CommonPassword", userForm!["TempPassword"][0].Code);
        Assert.DoesNotContain(IdentityFieldErrors.RecordKey, userForm.Keys);

        // Cùng mã, form khác, ô khác tên — đây là toàn bộ lý do hai tầng (mã → slot → tên field)
        // tồn tại; gộp một tầng thì mỗi form phải chép lại cả bảng mã.
        var changePassword = IdentityFieldErrors.Build(["CommonPassword"], IdentityFormFields.ChangePassword);

        Assert.NotNull(changePassword);
        Assert.Equal("CommonPassword", changePassword!["NewPassword"][0].Code);
        Assert.DoesNotContain(IdentityFieldErrors.RecordKey, changePassword.Keys);
    }

    /// <summary>
    /// Lưới chặn HỒI QUY cho các mã họ <c>Password*</c> <b>ĐÃ KHAI</b>: không mã nào trong số đó
    /// được phép tuột về <c>$record</c>. <c>PasswordMismatch</c> là ngoại lệ có chủ đích (bẫy 1):
    /// nó nói về mật khẩu HIỆN TẠI — vẫn là một ô mật khẩu.
    ///
    /// <para>🛑 <b>Test này KHÔNG bắt được mã MỚI, và nói riêng nó KHÔNG bắt được ca
    /// 2026-09-11.</b> Bản đầu của docstring này khẳng định ngược lại (*"lẽ ra đã bắt được trước
    /// khi người dùng gặp"*) — sai theo hai đường, và <c>core-reviewer</c> bắt được:</para>
    /// <list type="number">
    ///   <item><c>CommonPassword</c> <b>không</b> bắt đầu bằng <c>Password</c> — mã gây ra sự cố
    ///   nằm NGOÀI đúng cái họ mà lưới này nhận là mình phủ.</item>
    ///   <item>Danh sách dưới đây là <c>[InlineData]</c> gõ tay, không phải phép liệt kê từ nguồn.
    ///   Nó chỉ khẳng định được những mã đã có người gõ vào file test — mà ai gõ được tên mã vào
    ///   đây thì đã gõ được dòng vào <c>SlotByCode</c> rồi.</item>
    /// </list>
    ///
    /// <para>Hai lưới thật sự bắt được "dòng còn thiếu" nằm ở chỗ khác, và chúng bù nhau:
    /// <see cref="EveryDescriberCode_IsClassified"/> phủ mã của Identity lõi bằng PHẢN CHIẾU; còn
    /// mã của validator bên thứ ba (không có describer để phản chiếu) thì do
    /// <c>IdentityCodeFieldErrorsTests</c> phủ bằng một request HTTP thật, không cần biết tên mã.</para>
    /// </summary>
    [Theory(DisplayName = "Mọi mã họ Password* đều về MỘT ô mật khẩu, không về $record")]
    [InlineData("PasswordTooShort")]
    [InlineData("PasswordRequiresDigit")]
    [InlineData("PasswordRequiresLower")]
    [InlineData("PasswordRequiresUpper")]
    [InlineData("PasswordRequiresNonAlphanumeric")]
    [InlineData("PasswordRequiresUniqueChars")]
    [InlineData("PasswordMismatch")]
    public void EveryPasswordCode_LandsOnAPasswordInput(string code)
    {
        var slot = IdentityFieldErrors.SlotFor(code);

        Assert.True(
            slot is IdentityFieldSlot.NewPassword or IdentityFieldSlot.CurrentPassword,
            $"Mã '{code}' nói về một ô mật khẩu nhưng đang về slot {slot}. Rơi về " +
            $"{nameof(IdentityFieldSlot.Record)} nghĩa là câu lỗi hiện ở khối lỗi chung cuối form " +
            "thay vì ngay dưới ô người dùng đang gõ — đúng ca đã xảy ra với CommonPassword ngày " +
            "2026-09-11. Thêm một dòng vào IdentityFieldErrors.SlotByCode.");
    }

    /// <summary>
    /// 🔴 <b>Lưới DUY NHẤT trong file này bind vào NGUỒN thay vì vào một danh sách gõ tay.</b>
    /// Tên method khai trên <c>IdentityErrorDescriber</c> CHÍNH LÀ mã lỗi mà Identity sinh ra, nên
    /// phản chiếu lớp đó cho ra trọn bộ mã của bản .NET đang dùng — không phải bộ mã mà người viết
    /// test nhớ được.
    ///
    /// <para>Khẳng định: mỗi mã hoặc <b>có ô</b> (<c>SlotFor != Record</c>), hoặc nằm trong
    /// <see cref="IdentityFieldErrors.IntentionallyRecord"/> — tức đã được PHÂN LOẠI tường minh.
    /// Không có cửa thứ ba. Hôm nay xanh với trọn bộ describer; nó chỉ ĐỎ khi .NET thêm một mã mới
    /// mà chưa ai quyết định mã đó thuộc ô nào.</para>
    ///
    /// <para><b>Vì sao <c>IntentionallyRecord</c> phải public:</b> nếu chép danh sách loại trừ sang
    /// đây thì có HAI bản của cùng một danh sách, và bản ở test sẽ lệch ngay lần đầu ai đó sửa bản
    /// kia — test vẫn xanh, và nó xanh vì mù. Đọc từ chính nguồn là điều kiện để lưới này có nghĩa.</para>
    ///
    /// <para>⚠️ Lưới này KHÔNG phủ mã của validator bên thứ ba: <c>CommonPassword</c> đến từ gói
    /// <c>CommonPasswordsValidator</c> và không có mặt trong describer. Ca đó do
    /// <c>IdentityCodeFieldErrorsTests</c> phủ bằng HTTP thật.</para>
    /// </summary>
    [Fact(DisplayName = "Mọi mã của IdentityErrorDescriber đều ĐÃ ĐƯỢC PHÂN LOẠI (có ô, hoặc cố ý về $record)")]
    public void EveryDescriberCode_IsClassified()
    {
        var codes = typeof(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.ReturnType == typeof(Microsoft.AspNetCore.Identity.IdentityError))
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Chặn "xanh mà không đo gì": phản chiếu trả rỗng (đổi API, đổi kiểu trả về) thì mọi khẳng
        // định dưới đây vô nghĩa. Không chép CON SỐ vào đây — nó là thứ đếm được bằng lệnh.
        Assert.True(codes.Count > 0,
            "Không phản chiếu được mã nào từ IdentityErrorDescriber ⇒ lưới này không đo gì. " +
            "Nguyên nhân thường gặp: bản .NET mới đổi chữ ký (method không còn trả IdentityError).");

        var unclassified = codes
            .Where(code => IdentityFieldErrors.SlotFor(code) == IdentityFieldSlot.Record
                           && !IdentityFieldErrors.IntentionallyRecord.Contains(code))
            .ToList();

        Assert.True(unclassified.Count == 0,
            "Mã Identity CHƯA ĐƯỢC PHÂN LOẠI: " + string.Join(", ", unclassified) + ". " +
            "Mỗi mã phải hoặc có một dòng trong IdentityFieldErrors.SlotByCode (nói về một ô nhập), " +
            "hoặc được khai tường minh trong IdentityFieldErrors.IntentionallyRecord (nói về bản " +
            "ghi/trạng thái). Mặc định rơi về $record nghĩa là câu lỗi hiện ở khối lỗi chung cuối " +
            "form thay vì dưới ô người dùng đang gõ — im lặng, đúng ca 2026-09-11.");
    }

    /// <summary>
    /// Đối chứng cho lưới ngay trên: hai tập phải RỜI NHAU. Một mã vừa có ô vừa nằm trong danh sách
    /// "cố ý về bản ghi" nghĩa là hai chỗ nói ngược nhau, và lưới kia vẫn xanh vì nó chỉ hỏi
    /// "có ít nhất một trong hai".
    /// </summary>
    [Fact(DisplayName = "Đối chứng: SlotByCode và IntentionallyRecord không có mã chung")]
    public void ClassificationSets_DoNotOverlap()
    {
        var overlap = IdentityFieldErrors.IntentionallyRecord
            .Where(code => IdentityFieldErrors.SlotFor(code) != IdentityFieldSlot.Record)
            .ToList();

        Assert.True(overlap.Count == 0,
            "Mã vừa được gán một ô vừa khai là 'cố ý về bản ghi': " + string.Join(", ", overlap));
    }
}
