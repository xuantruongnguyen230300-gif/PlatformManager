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
}
