using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <inheritdoc cref="IUserAdminService"/>
/// <param name="currentUser">
/// Khai <c>= null</c> để nơi dựng service BẰNG TAY (integration test đường đọc danh sách) không
/// phải biết tới phụ thuộc chỉ dùng cho đường GHI. DI thật LUÔN truyền vào — container built-in
/// chỉ dùng giá trị mặc định khi dịch vụ không được đăng ký, mà <c>ICurrentUser</c> thì có đăng ký
/// (Program.cs). Null ⇒ audit ghi "system", đúng như mọi đường ghi không có phiên đăng nhập.
/// </param>
public sealed class UserAdminService(
    UserManager<AppUser> userManager,
    PlatformManagerDbContext db,
    ICurrentUser? currentUser = null) : IUserAdminService
{
    /// <summary>
    /// Ai đang thao tác — dùng cho <c>AppUser.CreatedBy/UpdatedBy</c>. PHẢI là người THAO TÁC, chứ
    /// không phải người bị sửa (phép nghiệm thu số 4 của quyết định 2026-08-31). "system" khi
    /// không có phiên đăng nhập nào (seeder, job nền) — cùng quy ước với AuditInterceptor, để hai
    /// đường ghi audit không nói hai thứ tiếng khác nhau.
    /// </summary>
    private string ActorName =>
        currentUser is { IsAuthenticated: true } ? currentUser.UserName ?? "system" : "system";

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<PagedList<UserDto>> GetListAsync(int page, int pageSize, UserListFilter filter, CancellationToken ct)
    {
        // db.Users chứ không phải userManager.Users: bên dưới cần ghép với db.UserRoles/db.Roles
        // trong CÙNG một cây biểu thức để EF dịch trọn gói sang SQL.
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(u =>
                u.UserName!.Contains(term) || u.FullName.Contains(term) || (u.Email != null && u.Email.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            // So theo NormalizedName: đó là cột Identity đánh index duy nhất (RoleNameIndex) và
            // là cách chính Identity tra role, nên không phụ thuộc casing thật đã seed vào cột
            // Name. Casing của tham số vào vẫn bị GetUsersListValidator chặn từ trước.
            var normalizedRole = userManager.KeyNormalizer.NormalizeName(filter.Role.Trim());

            // Subquery id -> EF dịch thành IN/EXISTS. KHÔNG dùng UserManager.GetUsersInRoleAsync:
            // hàm đó trả List<AppUser> (kéo toàn bộ user của role về bộ nhớ) nên lọc xong thì
            // TotalCount và phân trang phải tính lại phía ứng dụng — sai đúng cái CONTRACT USER-6
            // yêu cầu tránh.
            var userIdsInRole = db.UserRoles
                .Where(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == normalizedRole))
                .Select(ur => ur.UserId);

            query = query.Where(u => userIdsInRole.Contains(u.Id));
        }

        if (filter.IsLocked is { } isLocked)
        {
            // Cùng ĐỊNH NGHĨA "đang bị khoá" với ToDtoAsync (LockoutEnd còn hiệu lực trong tương
            // lai). Lệch định nghĩa ở hai chỗ này nghĩa là một dòng lọt bộ lọc "Đã khoá" nhưng
            // hiển thị badge "Đang hoạt động" — không có gì báo lỗi.
            var now = DateTimeOffset.UtcNow;
            query = isLocked
                ? query.Where(u => u.LockoutEnd != null && u.LockoutEnd > now)
                : query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now);
        }

        // Đếm SAU khi áp hết điều kiện, TRƯỚC khi Skip/Take — totalCount là tổng số dòng khớp
        // trong DB, không phải số dòng còn lại trên trang hiện tại.
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        // Role của CẢ TRANG lấy bằng ĐÚNG MỘT round-trip. Bản trước (tới 2026-08-29) chạy
        // `foreach (var user in items) dtos.Add(await ToDtoAsync(user))`, mà ToDtoAsync gọi
        // UserManager.GetRolesAsync → mỗi dòng một lần đi DB: pageSize=20 thành 21 query. Đó là
        // N+1, bị cấm thẳng ở doc/huong_dan/quy-uoc/be-performance.md §"Khi viết repository/query
        // mới" ("Không await trong vòng lặp"). Chi phí ở đây KHÔNG tự giới hạn: nó tăng tuyến
        // tính theo pageSize, nên nó cộng hưởng với BE-3 (trần pageSize) — sửa một mình mục kia
        // vẫn để lại 200 round-trip cho một request.
        var userIds = items.Select(u => u.Id).ToList();
        var roleNamesByUserId = (await (
                from userRole in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync(ct))
            .Where(x => x.Name is not null)
            .GroupBy(x => x.UserId)
            // Sắp xếp tên role để kết quả xác định giữa các lần chạy — thứ tự do DB trả về
            // không có bảo đảm nào, và UserDto.Roles đi thẳng ra FE.
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)[.. g.Select(x => x.Name!).Order()]);

        var dtos = items
            .Select(u => ToDto(u, roleNamesByUserId.GetValueOrDefault(u.Id, [])))
            .ToList();

        return new PagedList<UserDto> { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<bool> UserNameExistsAsync(string userName, CancellationToken ct)
        => await userManager.FindByNameAsync(userName) is not null;

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        => await userManager.FindByEmailAsync(email) is not null;

    /// <summary>
    /// <para>Cùng khuôn transaction + execution strategy như <see cref="UpdateAsync"/> — đọc
    /// docstring ở đó cho lý do đầy đủ.</para>
    ///
    /// <para><b>Vì sao đường TẠO cũng cần transaction</b> (bổ sung 2026-08-31, quyết định người
    /// dùng): <c>CreateAsync</c> thành công rồi <c>AddToRolesAsync</c> hỏng sẽ để lại một tài
    /// khoản <b>không role nào</b> trong khi API báo thất bại. Người thao tác thử lại thì vấp
    /// "tên đăng nhập đã tồn tại" — một thông điệp đúng về kỹ thuật và vô nghĩa với người đọc,
    /// và tài khoản mồ côi kia phải dọn tay trong DB.</para>
    /// </summary>
    public async Task<CreateUserOutcome> CreateAsync(
        string userName, string? email, string fullName, string tempPassword,
        IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
            await CreateCoreAsync(userName, email, fullName, tempPassword, roles, ct));
    }

    private async Task<CreateUserOutcome> CreateCoreAsync(
        string userName, string? email, string fullName, string tempPassword,
        IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var user = new AppUser
        {
            Id = EntityId.New(),
            UserName = userName,
            Email = email,
            FullName = fullName,
            MustChangePassword = true, // áp dụng chung cho MỌI user do Admin tạo
            DateCreate = DateTimeOffset.UtcNow,
            // Ghi TAY vì AppUser không kế thừa BaseEntity nên AuditInterceptor không chạm tới —
            // xem docstring AppUser.CreatedBy. UpdatedBy set luôn bằng CreatedBy để không phải
            // viết `UpdatedBy ?? CreatedBy` ở mọi nơi hiển thị "sửa lần cuối" (cùng quy ước với
            // AuditInterceptor).
            CreatedBy = ActorName,
            UpdatedBy = ActorName,
        };

        var createResult = await userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
            // Lấy `.Code` chứ KHÔNG phải `.Description` (sửa 2026-09-03) — cùng lý do, cùng đường ra
            // như IdentityService.ChangePasswordAsync: Description là chuỗi tiếng Anh của Identity
            // (không có bản dịch để bật) và nó đi thẳng ra envelope qua CreateUserCommand. Code
            // ("DuplicateUserName", "PasswordTooShort"…) là mã ổn định, FE tra sang câu của ngôn
            // ngữ đang chọn. Xem doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(c).
            return new CreateUserOutcome(false, null, [.. createResult.Errors.Select(e => e.Code)]);

        if (roles.Count > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, roles);
            if (!roleResult.Succeeded)
                // Lấy `.Code`, cùng lý do nhánh trên — hai nhánh dùng CHUNG một đường ra
                // (CreateUserOutcome.Errors), bịt một nhánh là bịt nửa đường rò.
                return new CreateUserOutcome(false, user.Id, [.. roleResult.Errors.Select(e => e.Code)]);
        }

        await transaction.CommitAsync(ct);
        return new CreateUserOutcome(true, user.Id, []);
    }

    /// <summary>
    /// <para><b>Vì sao thân hàm nằm trong <c>ExecuteAsync</c> của execution strategy:</b>
    /// <c>EnableRetryOnFailure</c> đã bật ở <c>DependencyInjection.AddCoreModule</c>. Khi có
    /// chiến lược thử lại, EF <b>từ chối</b> transaction do người dùng tự mở
    /// (<c>BeginTransactionAsync</c>) và ném <see cref="InvalidOperationException"/> — vì nó
    /// không biết phải chạy lại từ đâu nếu nửa chừng transaction gặp lỗi tạm thời.</para>
    ///
    /// <para>Cái bẫy ở chỗ nó <b>không lộ ra lúc biên dịch</b>: code build sạch, và mọi lệnh
    /// <c>PUT /api/users/{id}</c> hỏng lúc chạy. Bọc lại như dưới đây là cách EF chỉ định —
    /// nó biến "toàn bộ khối này" thành đơn vị được thử lại.</para>
    ///
    /// <para>⚠️ <c>ChangeTracker.Clear()</c> vứt bỏ <b>mọi</b> thay đổi đang chờ của toàn bộ
    /// request-scope, không riêng của hàm này. Hôm nay vô hại vì không đường ghi nào gom nhiều lệnh
    /// rồi mới gọi <c>IUnitOfWork.SaveChangesAsync</c> chung scope với nó. Thành lỗi im lặng ngay
    /// khi có — nên nếu thêm handler kiểu đó, thu hẹp lại thành <c>Detached</c> đúng entity cần.</para>
    ///
    /// <para><b>Vì sao <c>FindByIdAsync</c> nằm BÊN TRONG lambda, và vì sao phải
    /// <c>ChangeTracker.Clear()</c>:</b> mỗi lần thử lại phải bắt đầu từ dữ liệu sạch. Để lời
    /// đọc ở ngoài thì lần thử thứ hai dùng lại đúng instance đã bị sửa dở của lần đầu —
    /// hoặc ghi đè bằng dữ liệu cũ, hoặc vấp <c>ConcurrencyFailure</c> không giải thích được.</para>
    /// </summary>
    public async Task<UpdateUserOutcome> UpdateAsync(Guid id, string? email, string fullName, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => await UpdateCoreAsync(id, email, fullName, roles, ct));
    }

    /// <summary>
    /// Mọi điểm thoát trả <see cref="UpdateUserOutcome"/> mang MÃ THẬT (đổi 2026-09-05, quyết định
    /// người dùng — doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.3). Bản trước trả
    /// <c>bool</c> và <c>return false</c> ở năm chỗ: bốn lần vứt thẳng <c>IdentityResult.Errors</c>
    /// (nơi <c>ConcurrencyFailure</c> và lỗi gán role sinh ra), một lần lẫn ca "không tìm thấy" vào
    /// cùng một giá trị. Hai loại thất bại hoàn toàn khác nhau ra cùng một câu, và không ai — kể cả
    /// người vận hành đọc log — biết được cái nào đã xảy ra.
    ///
    /// <para>Lấy <c>.Code</c> chứ KHÔNG phải <c>.Description</c> ở mọi nhánh, cùng lý do và cùng
    /// đường ra như <see cref="CreateCoreAsync"/>: <c>Description</c> là chuỗi tiếng Anh của
    /// Identity, không có bản dịch để bật, và nó đi thẳng ra envelope (§4(c)).</para>
    /// </summary>
    private async Task<UpdateUserOutcome> UpdateCoreAsync(Guid id, string? email, string fullName, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        db.ChangeTracker.Clear(); // mỗi lần thử lại bắt đầu từ dữ liệu sạch — xem docstring UpdateAsync

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            // KHÔNG phải lỗi Identity — người dùng bị xoá xen giữa lần đọc của handler và lần ghi
            // này. Tách riêng để handler trả 404 thay vì "cập nhật thất bại" kèm danh sách mã rỗng.
            return UpdateUserOutcome.UserNotFound();

        // Tính TRƯỚC tập role thực sự đổi — quyết định có cần đổi con dấu hay không PHẢI dựa
        // trên diff này, không phải "có gọi UpdateAsync hay không" (xem
        // doc/huong_dan/wiki-core/be/02-identity-auth.md §"Quy tắc bắt buộc: đường ghi nào phải
        // đổi con dấu"). Chỉ sửa email/fullName (roles giữ nguyên) → toAdd/toRemove đều rỗng →
        // KHÔNG đổi con dấu, đá người ta ra vì bị sửa tên là thiệt hại không mua được gì.
        var currentRoles = await userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(roles).ToList();
        var toAdd = roles.Except(currentRoles).ToList();

        // MỘT transaction cho CẢ BỐN lệnh ghi (đổi 2026-08-31, quyết định người dùng 2026-08-30 —
        // doc/contracts/users.md §"...ghi quyền phải kiểm kết quả, và phải nằm trong transaction").
        // UserManager tự SaveChanges mỗi lần gọi, nên nếu không có transaction thì hỏng giữa chừng
        // để lại trạng thái nửa vời: nặng nhất là gỡ role xong mà thêm role hỏng — người dùng còn
        // ÍT quyền hơn lúc đầu, có thể là không role nào, trong khi API vẫn trả 200.
        //
        // Transaction lấy từ CHÍNH DbContext mà Identity store dùng (cùng instance scoped), nên nó
        // phủ luôn các lệnh ghi do UserManager phát ra. Hoàn tác phủ CẢ con dấu bảo mật: không ai
        // bị đăng xuất vì một thao tác của người khác thất bại.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Thứ tự GIỮ NGUYÊN "con dấu TRƯỚC, quyền SAU" — transaction làm mọi thứ all-or-nothing
        // nên thứ tự không còn là lá chắn duy nhất, nhưng nó vẫn là thứ tự an toàn hơn nếu sau này
        // có ai gỡ transaction ra: xem doc/huong_dan/wiki-core/be/02-identity-auth.md
        // §"Thứ tự: con dấu TRƯỚC, quyền/khoá SAU".
        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                // dispose transaction mà không commit = rollback
                return UpdateUserOutcome.Rejected([.. stampResult.Errors.Select(e => e.Code)]);
        }

        user.Email = email;
        user.FullName = fullName;
        user.DateUpdate = DateTimeOffset.UtcNow;
        user.UpdatedBy = ActorName; // người THAO TÁC, không phải người bị sửa

        // CÙNG instance user đã đổi con dấu ở trên — UpdateAsync tự làm mới ConcurrencyStamp
        // TRÊN CHÍNH instance đó; lấy lại instance cũ sẽ ra ConcurrencyFailure.
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            // Nhánh sinh ra ConcurrencyFailure — mã đó nay đi được tới tận envelope thay vì tan
            // thành cùng một `false` với "gán role hỏng".
            return UpdateUserOutcome.Rejected([.. updateResult.Errors.Select(e => e.Code)]);

        // KIỂM .Succeeded cho CẢ HAI lệnh ghi role — trước 2026-08-31 hai kết quả này bị bỏ qua và
        // hàm luôn trả true, nên "gỡ role thành công + thêm role thất bại" vẫn ra HTTP 200 và FE
        // hiện toast "Đã lưu" cho một tài khoản vừa bị mất quyền.
        if (toRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return UpdateUserOutcome.Rejected([.. removeResult.Errors.Select(e => e.Code)]);
        }

        if (toAdd.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
                return UpdateUserOutcome.Rejected([.. addResult.Errors.Select(e => e.Code)]);
        }

        await transaction.CommitAsync(ct);
        return UpdateUserOutcome.Success();
    }

    /// <summary>
    /// Hai lệnh ghi (đổi con dấu, đặt lockout) nên cùng khuôn transaction + execution strategy như
    /// <see cref="UpdateAsync"/> — đọc docstring ở đó cho lý do đầy đủ.
    ///
    /// <para><b>Vì sao đường này cũng cần</b> (bổ sung 2026-09-01 sau core-review): hỏng giữa chừng
    /// để lại trạng thái nửa vời <b>ngược hướng an toàn</b> — con dấu đã đổi nên mọi phiên của người
    /// đó bị chấm dứt, nhưng tài khoản <b>chưa bị khoá</b> nên họ đăng nhập lại được ngay. Người
    /// thao tác thấy lỗi và tin là chưa khoá; thực tế đã gây tác dụng phụ.</para>
    /// </summary>
    public async Task<bool> LockAsync(Guid id, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => await LockCoreAsync(id, ct));
    }

    private async Task<bool> LockCoreAsync(Guid id, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return false;

        // Con dấu TRƯỚC SetLockoutEndDateAsync — nếu không, SecurityStampValidator (đọc con dấu
        // mỗi 30 phút) không có gì để phát hiện, và phiên đang chạy của người bị khoá sống tiếp
        // vô thời hạn (xem doc/huong_dan/wiki-core/be/02-identity-auth.md).
        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
            return false;

        // Audit GHI TAY — xem chú thích ở UnlockAsync.
        user.DateUpdate = DateTimeOffset.UtcNow;
        user.UpdatedBy = ActorName;

        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
            return false; // dispose transaction mà không commit = rollback, con dấu cũng lùi lại

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> UnlockAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return false;

        // Audit GHI TAY, không tự động (bổ sung 2026-08-28, finding F8). AppUser KHÔNG kế thừa
        // BaseEntity — Identity tự quản vòng đời entity của nó — nên nó nằm NGOÀI tầm với của
        // AuditInterceptor, thứ chỉ chạm entity BaseEntity. Khoá/mở khoá tài khoản là đường ghi
        // đủ nhạy cảm để phải có dấu vết; trước bản sửa này chỉ UpdateAsync/CreateAsync ghi audit,
        // còn 2 đường lock/unlock im lặng: cột "DateUpdate" đứng yên trong khi quyền truy cập của
        // người dùng vừa bị đảo. Tên cột giữ nguyên "DateCreate"/"DateUpdate" — AppUser CỐ Ý không
        // đổi theo quy ước BaseEntity, xem header
        // Persistence/Migrations/sql/0001_initial_baseline.sql (gộp từ 0007 khi baseline 2026-08-31).
        user.DateUpdate = DateTimeOffset.UtcNow;
        user.UpdatedBy = ActorName;

        var result = await userManager.SetLockoutEndDateAsync(user, null);
        return result.Succeeded;
    }

    /// <summary>
    /// Đường MỘT user (GetByIdAsync). Giữ nguyên GetRolesAsync ở đây là cố ý: 1 user thì đúng 1
    /// round-trip, không có N+1 để tránh — xem chú thích trong GetListAsync.
    /// </summary>
    private async Task<UserDto> ToDtoAsync(AppUser user)
        => ToDto(user, [.. await userManager.GetRolesAsync(user)]);

    /// <summary>
    /// Phần dựng DTO thuần (không chạm DB) — dùng chung cho cả đường 1 user lẫn đường cả trang,
    /// để định nghĩa <c>IsLocked</c> chỉ tồn tại ở MỘT chỗ. Định nghĩa này còn phải khớp với bộ
    /// lọc <c>IsLocked</c> ở GetListAsync; lệch nhau thì một dòng lọt bộ lọc "Đã khoá" lại hiển
    /// thị badge "Đang hoạt động".
    /// </summary>
    private static UserDto ToDto(AppUser user, IReadOnlyList<string> roles)
    {
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        // Version = ConcurrencyStamp CÓ SẴN của Identity — không thêm cột, không migration. Identity
        // tự làm mới nó ở mọi lệnh ghi đi qua UserManager.UpdateAsync, nên nó đổi đúng khi và chỉ
        // khi bản ghi user đã bị ghi lại. Xem doc/contracts/users.md quyết định 3.
        return new UserDto(
            user.Id, user.UserName!, user.Email, user.FullName, roles, isLocked,
            user.MustChangePassword, user.DateCreate, user.ConcurrencyStamp);
    }
}
