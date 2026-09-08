using NSubstitute;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.UnitTests.Common;

/// <summary>
/// Ca 2 của cơ chế tham số (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.5): lỗi NGHIỆP VỤ
/// phải đẩy tham số ra envelope dưới dạng RỜI, không chỉ nằm trong câu đã ráp.
///
/// <para><b>Vì sao đi qua handler THẬT thay vì gọi thẳng <c>BaseResponse.Fail</c>:</b> chuỗi cần
/// chứng minh có bốn mắt xích và mắt nào đứt cũng không gây lỗi biên dịch — khuôn thông điệp phải
/// mang chỗ giữ ĐẶT TÊN, nơi gọi phải truyền ĐÚNG tên đó, <c>Fail</c> phải giữ lại bản rời, và
/// envelope phải mang được nó. Một test gọi thẳng <c>Fail</c> với tên tự chọn sẽ xanh kể cả khi
/// catalog vẫn còn <c>{0}</c> và nơi gọi vẫn truyền theo thứ tự.</para>
///
/// <para><b>Bất biến quan trọng nhất: MỘT nguồn, HAI đầu ra.</b> Câu fallback và
/// <c>messageParams</c> phải nói cùng một giá trị. Lệch nhau là kiểu hỏng tệ nhất của cơ chế này —
/// client tin bản rời, người phát triển đọc log tin câu fallback, và hai bên thấy hai sự thật khác
/// nhau về cùng một lỗi.</para>
/// </summary>
public class BusinessErrorMessageParamsTests
{
    private static ICurrentUser Admin()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        currentUser.Roles.Returns([Roles.Admin]);
        currentUser.IsInRole(Arg.Any<string>()).Returns(call => call.Arg<string>() == Roles.Admin);
        return currentUser;
    }

    [Fact(DisplayName = "Lỗi nghiệp vụ có tham số → messageParams mang bản RỜI, khoá đặt TÊN")]
    public async Task BusinessError_Carries_ItsParametersSeparately()
    {
        const string takenName = "nguyenvana";

        var service = Substitute.For<IUserAdminService>();
        service.UserNameExistsAsync(takenName, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new CreateUserHandler(service, Admin()).Handle(
            new CreateUserCommand(takenName, "a@example.com", "Nguyễn Văn A", "mat-khau-tam", [Roles.User]),
            CancellationToken.None);

        Assert.Equal("USER.DUPLICATE_USERNAME", result.BusinessCode);

        Assert.NotNull(result.MessageParams);
        Assert.Equal(takenName, result.MessageParams!["UserName"]);

        // Câu fallback vẫn phải ráp xong (dev-facing + dự phòng, §3) — và phải ráp từ CHÍNH bộ giá
        // trị vừa gửi đi. Còn nguyên chỗ giữ "{UserName}" nghĩa là tên tham số ở nơi gọi lệch với
        // tên trong khuôn thông điệp: không lỗi biên dịch, nhưng người dùng đọc được dấu ngoặc.
        Assert.NotNull(result.Message);
        Assert.Contains(takenName, result.Message);
        Assert.DoesNotContain("{", result.Message);
    }

    /// <summary>
    /// Ca ĐỐI CHỨNG — mã không có tham số thì trường phải VẮNG (null), không phải từ điển rỗng.
    /// Thiếu ca này, một bản sửa gán từ điển cho MỌI lỗi vẫn xanh, và envelope phình thêm một khoá
    /// vô nghĩa ở mọi phản hồi lỗi — đúng thứ khuôn <c>Retryable</c>/<c>Fields</c> tránh.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: mã KHÔNG có tham số → messageParams null (vắng mặt trên dây)")]
    public async Task ErrorWithoutParameters_LeavesTheFieldNull()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserDto?)null);

        var result = await new UnlockUserHandler(service)
            .Handle(new UnlockUserCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("USER.NOT_FOUND", result.BusinessCode);
        Assert.Null(result.MessageParams);
    }
}
