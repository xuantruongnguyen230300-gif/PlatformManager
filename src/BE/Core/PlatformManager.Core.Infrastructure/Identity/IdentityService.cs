using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <inheritdoc cref="IIdentityService"/>
/// <remarks>
/// <c>db</c> nhận vào CHỈ để mở transaction tường minh cho <see cref="ChangePasswordAsync"/> —
/// cùng instance scoped mà Identity store dùng, nên transaction phủ luôn lệnh do
/// <c>UserManager</c> phát ra. Không truy vấn gì qua nó ở đây.
/// </remarks>
public sealed class IdentityService(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    PlatformManagerDbContext db)
    : IIdentityService
{
    public async Task<LoginOutcome> SignInAsync(string userName, string password, bool rememberMe, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            // Băm GIẢ trước khi trả lỗi — xem BurnPasswordHashingTime. Bỏ bước này thì nhánh
            // "không có tài khoản" trả về gần như tức thì trong khi nhánh "có tài khoản, sai mật
            // khẩu" chạy đủ PBKDF2, và chênh lệch đó liệt kê được HÀNG LOẠT tài khoản có thật mà
            // không cần đoán đúng mật khẩu lần nào.
            BurnPasswordHashingTime(password);
            return new LoginOutcome(false, false, null);
        }

        // SuperAdmin MIỄN khoá theo username (quyết định người dùng 2026-08-20) — xem
        // doc/huong_dan/wiki-core/be/09-security-beyond-auth.md
        // §"Ngoại lệ đã có thật: `SuperAdmin` KHÔNG bị khoá".
        // lockoutOnFailure=true cho phép BẤT KỲ AI, kể cả chưa đăng nhập, khoá 15 phút MỘT tài
        // khoản bất kỳ bằng 5 lần đoán sai — với break-glass SuperAdmin đó là đường tự-DoS quản
        // trị viên cuối cùng ra khỏi hệ thống. SuperAdminAccountGuard KHÔNG chạm được đường này
        // (nằm TRƯỚC lúc đăng nhập, chưa có ai để mà guard). CHỈ ảnh hưởng việc ĐẾM lần sai
        // (AccessFailedCount) — không nới độ mạnh kiểm mật khẩu, không bỏ qua bước xác thực nào.
        var isSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);

        var checkResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: !isSuperAdmin);
        if (checkResult.IsLockedOut)
            return new LoginOutcome(false, true, null);

        if (!checkResult.Succeeded)
            return new LoginOutcome(false, false, null);

        // isPersistent = rememberMe (2026-08-31). Trước đó khai cứng `true`, nên MỌI lần đăng nhập
        // đều để lại cookie sống nhiều ngày và tự gia hạn: đóng trình duyệt không đăng xuất, và
        // trên máy dùng chung người tiếp theo mở trình duyệt là đang ở trong phiên của người trước.
        // Xem doc/contracts/auth.md §POST /api/auth/login.
        await signInManager.SignInAsync(user, isPersistent: rememberMe);

        return new LoginOutcome(true, false, await BuildUserInfoAsync(user));
    }

    public async Task SignOutAsync(CancellationToken ct) => await signInManager.SignOutAsync();

    public async Task<CurrentUserInfo?> GetUserInfoAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildUserInfoAsync(user);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            // Cờ NotFound, KHÔNG phải một câu nhét vào danh sách Errors (sửa 2026-09-05, bẫy 3 của
            // doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2). Danh sách đó nay là danh sách
            // MÃ đi thẳng ra fieldErrors[].code — một câu tiếng Việt nằm trong đó sẽ trở thành một
            // khoá bảng dịch bằng tiếng Việt.
            return new ChangePasswordResult(false, NotFound: true, []);

        // Hai lệnh ghi trong MỘT transaction (bổ sung 2026-09-01 sau core-review). Hỏng ở lệnh thứ
        // hai để lại trạng thái nửa vời đặc biệt khó chịu: mật khẩu ĐÃ đổi nhưng MustChangePassword
        // vẫn true ⇒ người dùng bị ép đổi lại, và màn đổi mật khẩu đòi "mật khẩu hiện tại" mà họ
        // vừa thay — họ không có cách nào tự thoát.
        //
        // RefreshSignInAsync ở cuối hàm CỐ Ý nằm NGOÀI transaction: nó ghi cookie, không ghi DB.
        var strategy = db.Database.CreateExecutionStrategy();
        var outcome = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
                // Lấy `.Code` chứ KHÔNG phải `.Description` (sửa 2026-09-03): Description là chuỗi
                // TIẾNG ANH của ASP.NET Core Identity và không có bản dịch nào để bật —
                // dotnet/aspnetcore có 97 file .resx nhưng 0 file .xlf. Chuỗi này đi thẳng ra
                // envelope cho người dùng cuối, nên nó là một đường rò tiếng Anh ra giao diện
                // tiếng Việt. Code ("PasswordTooShort", "PasswordMismatch"…) VỐN ĐÃ là mã ổn
                // định, tức đúng thứ FE tra được sang câu của ngôn ngữ đang chọn — xem
                // doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(c).
                //
                // Nợ đã biết và đã chấp nhận (§8 cùng file): mất tham số RequiredLength trong câu
                // "Passwords must be at least N characters" ⇒ FE hiển thị câu chung, không con số.
                // Phương án giữ con số là dựng IdentityErrorDescriber với 22 override — đắt hơn
                // nhiều lần giá trị của một con số, và dựng lại bộ chữ thứ hai ở BE cho đúng kênh
                // mà FE đã sở hữu câu chữ.
                return new ChangePasswordResult(false, NotFound: false, [.. result.Errors.Select(e => e.Code)]);

            user.MustChangePassword = false;
        user.DateUpdate = DateTimeOffset.UtcNow;
        // Tự đổi mật khẩu ⇒ người thao tác CHÍNH LÀ chủ tài khoản. Ghi tay vì AppUser nằm ngoài
        // tầm với của AuditInterceptor (xem docstring AppUser.CreatedBy).
            user.UpdatedBy = user.UserName;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                // Lấy `.Code`, cùng lý do như nhánh trên — đường ra của hai nhánh này là MỘT
                // (ChangePasswordResult.Errors → ChangePasswordCommand → envelope), nên sửa một
                // nhánh mà để nhánh kia là bịt nửa đường rò.
                return new ChangePasswordResult(false, NotFound: false, [.. updateResult.Errors.Select(e => e.Code)]);

            await transaction.CommitAsync(ct);
            return new ChangePasswordResult(true, NotFound: false, []);
        });

        if (!outcome.Succeeded)
            return outcome;

        // UserManager.ChangePasswordAsync ở trên đã TỰ đổi SecurityStamp bên trong (hành vi có
        // sẵn của Identity — đúng bảo mật, giết mọi phiên cũ, nhưng giết CẢ phiên đang gọi).
        // BẮT BUỘC cấp lại cookie mang con dấu MỚI ở đây, gọi SAU CÙNG (sau khi MustChangePassword
        // đã ghi ổn định) — thiếu bước này, người vừa đổi mật khẩu bị chính SecurityStampValidator
        // đá ra ~30 phút sau, không rõ lý do. Các phiên KHÁC của cùng người vẫn mang con dấu cũ
        // ⇒ vẫn bị chấm dứt — ĐÚNG chuẩn bảo mật, không "sửa" nốt phần này. Xem
        // doc/huong_dan/wiki-core/be/02-identity-auth.md §"Cạm bẫy: ChangePasswordAsync TỰ đổi
        // con dấu".
        await signInManager.RefreshSignInAsync(user);

        return new ChangePasswordResult(true, NotFound: false, []);
    }

    /// <summary>
    /// Tiêu một lượt băm mật khẩu để nhánh "không tìm thấy tài khoản" tốn thời gian tương đương
    /// nhánh "có tài khoản, sai mật khẩu". Cách xử lý chuẩn, có trong tài liệu của ASP.NET Core
    /// Identity; quyết định người dùng 2026-08-30, xem
    /// doc/huong_dan/wiki-core/be/09-security-beyond-auth.md §"Liệt kê tài khoản qua đường đăng
    /// nhập". Chính nhờ bước này mà quyết định GIỮ <c>AUTH.LOCKED_OUT</c> tách biệt với
    /// <c>AUTH.INVALID_CREDENTIALS</c> mới đứng vững: sau khi vá đường thời gian, đường thông điệp
    /// không còn liệt kê hàng loạt được nữa (tốn 5 lần đoán sai cho MỖI tài khoản, và khoá luôn
    /// tài khoản đó lại).
    ///
    /// <para><b>Băm giả sinh MỘT LẦN cho cả tiến trình</b> và dùng lại: <c>HashPassword</c> cũng
    /// tốn đúng một lượt PBKDF2, không lý gì trả cái giá đó ở mọi request. Lấy từ
    /// <c>userManager.PasswordHasher</c> chứ không hardcode chuỗi hash: chỉ như vậy số vòng lặp
    /// mới đúng bằng số vòng đang cấu hình — hardcode một chuỗi sinh bằng tham số khác chính là
    /// cách tạo ra một chênh lệch thời gian MỚI trong khi tưởng đã vá.</para>
    ///
    /// <para>Đua giữa hai luồng ở lần gọi đầu là vô hại: hai chuỗi hash khác nhau đều dùng được,
    /// chỉ một cái ở lại.</para>
    /// </summary>
    private void BurnPasswordHashingTime(string password)
    {
        var hasher = userManager.PasswordHasher;
        _dummyPasswordHash ??= hasher.HashPassword(TimingProbeUser, DummyPassword);
        _ = hasher.VerifyHashedPassword(TimingProbeUser, _dummyPasswordHash, password);
    }

    private static readonly AppUser TimingProbeUser = new() { UserName = "__timing_probe__" };

    /// <summary>Giá trị không bao giờ dùng để đăng nhập — chỉ để sinh một chuỗi hash hợp lệ có
    /// cùng tham số với hash thật.</summary>
    private const string DummyPassword = "__timing_probe_password__";

    private static string? _dummyPasswordHash;

    private async Task<CurrentUserInfo> BuildUserInfoAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserInfo(user.Id, user.UserName!, user.Email, user.FullName, [.. roles], user.MustChangePassword);
    }
}
