using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Bootstrap;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Seed role/2 tài khoản bootstrap (<c>SuperAdmin</c> + <c>Admin</c>, mỗi tài khoản MỘT
/// role)/SysMenu/SysMenuRole — DML, idempotent ("chưa có thì thêm"), chỉ được gọi khi
/// lệnh `--seed` (xem SeedCommand.cs). KHÔNG seed dữ liệu đặc thù Module nào — mỗi module tự có
/// seeder riêng cho dữ liệu nghiệp vụ của nó. Gọi CoreSeeder TRƯỚC mọi ModuleSeeder khác (Module có thể cần role/user đã
/// tồn tại — xem doc/kien-truc-core-module.md). Mật khẩu 2 tài khoản đọc từ
/// <see cref="BootstrapOptions"/> (fail-fast — xem DependencyInjection.AddCoreModule), KHÔNG
/// hardcode — xem BootstrapOptions cho điều kiện cấu hình và nơi đặt giá trị.
///
/// <para><b>DỮ LIỆU menu KHÔNG nằm ở đây</b> (tách 2026-09-02): danh sách mục menu do host khai
/// qua <see cref="ICoreMenuSeedSource"/>. Nhãn/route/icon là thứ riêng của từng dự án, còn lớp
/// này giữ đúng phần cơ chế — xem <see cref="SeedMenuAsync"/>.</para>
///
/// <para><b>DANH MỤC permission-key cũng KHÔNG nằm ở đây</b> (tách 2026-09-03): host khai qua
/// <see cref="ICoreResourceKeySource"/>. Phần cơ chế ở lại: cấp ĐỦ danh mục cho Admin + User,
/// idempotent, và SuperAdmin không cần dòng nào — xem <see cref="SeedRolePermissionsAsync"/>.</para>
///
/// <para><b>EMAIL và TÊN HIỂN THỊ của 2 tài khoản bootstrap cũng KHÔNG nằm ở đây</b> (tách
/// 2026-09-02): host khai qua <see cref="ICoreBootstrapAccountSource"/>. Lớp này vẫn giữ trọn
/// phần luật — đúng HAI tài khoản, mỗi tài khoản MỘT vai, <c>MustChangePassword = true</c>, tên
/// đăng nhập soi gương <c>Roles.*</c> — xem <see cref="SeedBootstrapUserAsync"/>.</para>
/// </summary>
public sealed class CoreSeeder(
    PlatformManagerDbContext db,
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IOptions<BootstrapOptions> bootstrapOptions,
    ICoreBootstrapAccountSource bootstrapAccountSource,
    ICoreMenuSeedSource menuSeedSource,
    ICoreResourceKeySource resourceKeySource,
    ILogger<CoreSeeder> logger)
{
    private const string SuperAdminUserName = "SuperAdmin";
    private const string AdminUserName = "Admin";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedRolePermissionsAsync(ct);
        await SeedBootstrapUserAsync(ct);
        await SeedMenuAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        foreach (var roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            await roleManager.CreateAsync(new AppRole(roleName) { Id = EntityId.New() });
        }
    }

    /// <summary>
    /// `RequirePermissionFilter` là deny-by-default — bảng `RolePermissions` rỗng nghĩa là MỌI
    /// role (trừ SuperAdmin bypass) bị 403 ở MỌI endpoint ngay khi
    /// [RequirePermission] gắn lên controller. Cấp đủ danh mục key do host khai
    /// (<see cref="ICoreResourceKeySource"/>, tách 2026-09-03) cho
    /// Admin + User để GIỮ NGUYÊN hành vi trước khi vá (mọi user thao tác được) — xem
    /// doc/contracts/permissions.md §"Rủi ro rollout" + doc/huong_dan/wiki-core/be/
    /// 13-core-data-migration.md. SuperAdmin KHÔNG cần dòng nào ở đây (break-glass ở
    /// RequirePermissionFilter). Chạy qua lệnh `--seed`, KHÔNG chạy lúc app khởi động — và lệnh
    /// đó là ĐƯỜNG CHÍNH cho production, nó gọi thẳng method này nên phủ luôn bảng RolePermissions.
    /// scripts/seed-role-permissions.sql chỉ còn dành cho ca chỉ có quyền truy cập DB mà không chạy
    /// được binary (quyết định 2026-08-30, xem 13-core-data-migration.md).
    /// </summary>
    private async Task SeedRolePermissionsAsync(CancellationToken ct)
    {
        foreach (var roleName in new[] { Roles.Admin, Roles.User })
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue; // Roles.* là hằng số, SeedRolesAsync đã chạy trước — nhánh phòng thủ thuần // SeedRolesAsync chạy trước nên bình thường không xảy ra — phòng thủ nếu thứ tự đổi

            foreach (var resourceKey in resourceKeySource.Keys())
            {
                var exists = await db.RolePermissions
                    .AnyAsync(x => x.RoleId == role.Id && x.ResourceKey == resourceKey, ct);
                if (!exists)
                    await db.RolePermissions.AddAsync(RolePermission.Create(role.Id, resourceKey), ct);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>2 tài khoản RIÊNG BIỆT, mỗi tài khoản MỘT role — SuperAdmin thuần break-glass,
    /// Admin thuần vận hành hàng ngày. Khác thiết kế trước đó (1 tài khoản gộp cả 2 role) — quyết
    /// định người dùng 2026-08-24, xem doc/huong_dan/wiki-core/be/13-core-data-migration.md
    /// §"✅ Quyết định người dùng 2026-08-24 — tách tài khoản bootstrap SuperAdmin/Admin" (lý do:
    /// least-privilege, SuperAdmin là break-glass không thu hồi được qua UI). Cũng xem
    /// doc/huong_dan/wiki-core/be/13-core-data-migration.md ("mỗi tài khoản MỘT role") và docstring
    /// BootstrapOptions ("2 tài khoản quản trị").</summary>
    private async Task SeedBootstrapUserAsync(CancellationToken ct)
    {
        // Email + tên hiển thị đến từ HOST (tách 2026-09-02, xem ICoreBootstrapAccountSource).
        // Số lượng, vai và tên đăng nhập thì KHÔNG — chúng nằm ngay dưới đây, trong Core, vì đó là
        // quyết định least-privilege 2026-08-24 chứ không phải dữ liệu riêng của một dự án.
        var superAdmin = bootstrapAccountSource.SuperAdmin;
        var admin = bootstrapAccountSource.Admin;

        GuardAgainstInvalidProfiles(superAdmin, admin);

        await SeedBootstrapAccountAsync(
            SuperAdminUserName, superAdmin, bootstrapOptions.Value.SuperAdminPassword, Roles.SuperAdmin, ct);

        await SeedBootstrapAccountAsync(
            AdminUserName, admin, bootstrapOptions.Value.AdminPassword, Roles.Admin, ct);
    }

    /// <summary>
    /// Kiểm CẢ HAI hồ sơ TRƯỚC khi tạo tài khoản nào. Kiểm từng cái ngay trước lúc dùng thì lượt
    /// seed đầu có thể tạo xong SuperAdmin rồi mới ném vì Admin khai hụt — để lại một hệ thống có
    /// tài khoản break-glass mà không có tài khoản vận hành, tức đúng trạng thái nửa vời mà người
    /// chạy lệnh không đọc ra được từ mã thoát.
    ///
    /// <para><b>Vì sao phải kiểm gì đó ở đây, khi trước lượt tách chẳng cần kiểm:</b> bốn giá trị
    /// này từng là hằng số trong chính file này nên compiler và code review bảo vệ. Nay chúng là
    /// chuỗi TỰ DO do host khai, và cả ba ca hỏng bên dưới đều <b>không</b> làm Identity hay
    /// Postgres ồn lên một tiếng nào.</para>
    ///
    /// <para><b>Email rỗng:</b> Identity không bắt buộc email, nên chuỗi rỗng đi thẳng xuống cột
    /// <c>Email</c> và lệnh seed vẫn thoát 0. Kết quả là tài khoản quản trị duy nhất của hệ thống
    /// không liên hệ được, mà phát hiện ra thì nó đã nằm trong DB thật.</para>
    ///
    /// <para><b>Tên hiển thị rỗng:</b> <c>AppUser.FullName</c> mặc định là chuỗi rỗng, nên "host
    /// khai hụt" và "host cố ý khai rỗng" nhìn giống hệt nhau ở mọi màn hình quản trị.</para>
    ///
    /// <para><b>Hai tài khoản chung một email:</b> <c>RequireUniqueEmail = false</c>
    /// (DependencyInjection.cs) nên KHÔNG có gì chặn — không Identity, không index. Hậu quả không
    /// rơi vào seeder mà rơi ra chỗ khác: <c>UserAdminService.EmailExistsAsync</c>
    /// (UserAdminService.cs:116) trả về một trong hai dòng mà không nói là dòng nào, và người vận
    /// hành liên hệ hoặc khôi phục theo email không còn phân biệt được tài khoản break-glass với
    /// tài khoản dùng hằng ngày. Ranh giới least-privilege 2026-08-24 vẫn đúng trong bảng
    /// <c>AspNetUserRoles</c> nhưng đã bị xoá nhoà ở lớp con người — nơi nó thật sự được thi hành.</para>
    /// </summary>
    private static void GuardAgainstInvalidProfiles(
        BootstrapAccountProfile superAdmin, BootstrapAccountProfile admin)
    {
        GuardAgainstBlankFields(nameof(ICoreBootstrapAccountSource.SuperAdmin), superAdmin);
        GuardAgainstBlankFields(nameof(ICoreBootstrapAccountSource.Admin), admin);

        // OrdinalIgnoreCase: Identity chuẩn hoá email về chữ hoa trước khi ghi NormalizedEmail, nên
        // hai chuỗi chỉ khác nhau ở hoa/thường là CÙNG một email ở mọi phép tra sau này.
        if (!string.Equals(superAdmin.Email, admin.Email, StringComparison.OrdinalIgnoreCase))
            return;

        throw new InvalidOperationException(
            $"{nameof(ICoreBootstrapAccountSource)} khai cùng một email '{superAdmin.Email}' cho cả hai tài " +
            "khoản bootstrap. Hai tài khoản này cố ý TÁCH RỜI (SuperAdmin break-glass, Admin vận hành hàng " +
            "ngày) — dùng chung email làm mất khả năng phân biệt chúng khi liên hệ hoặc khôi phục.");
    }

    /// <summary>
    /// Nêu ĐÍCH DANH property nào của seam: một thông điệp "email rỗng" không nói được phải sửa
    /// dòng nào trong hai dòng của host.
    /// </summary>
    private static void GuardAgainstBlankFields(string propertyName, BootstrapAccountProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new InvalidOperationException(
                $"{nameof(ICoreBootstrapAccountSource)}.{propertyName} khai Email rỗng. Không gì chặn giá trị " +
                "này ở tầng dưới — tài khoản vẫn được tạo và lệnh seed vẫn báo thành công.");
        }

        if (string.IsNullOrWhiteSpace(profile.FullName))
        {
            throw new InvalidOperationException(
                $"{nameof(ICoreBootstrapAccountSource)}.{propertyName} khai FullName rỗng. Đây là tên hiển thị " +
                "trên mọi màn hình quản trị, để rỗng thì không ai nhận ra tài khoản này là gì.");
        }
    }

    private async Task SeedBootstrapAccountAsync(
        string userName, BootstrapAccountProfile profile, string password, string role, CancellationToken ct)
    {
        // Tài khoản đã có ⇒ vẫn phải BẢO ĐẢM ĐÚNG ROLE rồi mới thoát.
        //
        // Trước 2026-09-01 nhánh này `return` ngay. Kết hợp với việc kết quả AddToRolesAsync bị bỏ
        // qua ở cuối hàm, nó tạo ra một trạng thái KHÔNG TỰ HỒI PHỤC: lần seed đầu tạo được user
        // nhưng gán role hỏng ⇒ tài khoản SuperAdmin tồn tại mà không role nào; chạy lại `--seed`
        // thấy user đã có nên thoát ngay, vĩnh viễn không gắn lại. Không ai đăng nhập quản trị
        // được, và không có đường sửa từ trong ứng dụng — đúng kịch bản mà
        // doc/huong_dan/wiki-core/be/13-core-data-migration.md §"Khoảng trống" mô tả.
        //
        // Idempotent theo nghĩa HỒI PHỤC, không chỉ theo nghĩa "không nhân bản".
        var existing = await userManager.FindByNameAsync(userName);
        if (existing is not null)
        {
            if (await userManager.IsInRoleAsync(existing, role))
                return;

            logger.LogWarning(
                "Tài khoản bootstrap '{UserName}' đã tồn tại nhưng THIẾU role '{Role}' — gắn lại.",
                userName, role);

            var repair = await userManager.AddToRolesAsync(existing, [role]);
            if (!repair.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không gắn lại được role '{role}' cho tài khoản bootstrap '{userName}': " +
                    string.Join("; ", repair.Errors.Select(e => e.Description)));
            }

            return;
        }

        var user = new AppUser
        {
            Id = EntityId.New(),
            UserName = userName,
            // Email/FullName là DỮ LIỆU CỦA HOST — Core không kiểm định dạng email, chỉ chặn rỗng
            // và chặn trùng (xem GuardAgainstInvalidProfiles). Gói thành một record thay vì hai
            // tham số string liền nhau cũng là để không hoán vị nhầm chúng ở nơi gọi.
            Email = profile.Email,
            FullName = profile.FullName,
            MustChangePassword = true,
            DateCreate = DateTimeOffset.UtcNow,
            // "system": seeder chạy KHÔNG có phiên đăng nhập nào, đúng quy ước của AuditInterceptor
            // cho đường ghi không có người thao tác (xem AppUser.CreatedBy — cột này ghi tay vì
            // AppUser không kế thừa BaseEntity).
            CreatedBy = "system",
            UpdatedBy = "system",
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed tài khoản bootstrap '{userName}' thất bại: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // KIỂM .Succeeded — trước 2026-09-01 kết quả này bị bỏ qua, và lệnh `--seed` vẫn thoát 0.
        // Người vận hành đọc "thành công" trong khi tài khoản quản trị duy nhất không có quyền gì.
        // NÉM thay vì log: đây là bootstrap, hỏng nửa chừng phải dừng ồn ào để người chạy biết mà
        // xử lý, không phải ghi một dòng log rồi báo thành công.
        var roleResult = await userManager.AddToRolesAsync(user, [role]);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Tạo được tài khoản bootstrap '{userName}' nhưng KHÔNG gắn được role '{role}': " +
                string.Join("; ", roleResult.Errors.Select(e => e.Description)) +
                ". Tài khoản đang không có quyền nào — chạy lại lệnh seed để gắn lại.");
        }
    }

    /// <summary>
    /// Cơ chế seed menu — <b>không chứa mục menu nào</b>. Danh sách đến từ
    /// <see cref="ICoreMenuSeedSource"/> do host đăng ký (tách 2026-09-02, xem docstring của seam
    /// đó cho lý do đầy đủ): nhãn, route và icon là dữ liệu riêng từng dự án, còn thứ tự
    /// cha-trước-con, ánh xạ <c>ParentCode → Id</c>, upsert/hồi sinh và gán role là cơ chế dùng
    /// chung — chỉ phần sau thuộc về Core.
    ///
    /// <para>Menu NGHIỆP VỤ do seeder của chính module đóng góp, KHÔNG đi qua seam này — đúng
    /// ranh giới Core/Module (doc/kien-truc-core-module.md).</para>
    /// </summary>
    private async Task SeedMenuAsync(CancellationToken ct)
    {
        var items = menuSeedSource.GetMenuItems();

        if (items.Count == 0)
        {
            // KHÔNG ném: "dự án này không có mục menu Core nào" là lựa chọn hợp lệ của host, Core
            // không có thẩm quyền phủ quyết. Nhưng nó cũng là hệ quả điển hình của một nguồn seed
            // viết hụt, và một sidebar trống thì chẳng để lại dấu vết nào để lần ra — nên phải ồn.
            logger.LogWarning(
                "{Seam} không trả mục menu nào — sẽ không có dòng SysMenus nào được seed.",
                nameof(ICoreMenuSeedSource));
            return;
        }

        GuardAgainstDuplicateCodes(items);
        await GuardAgainstUnknownRolesAsync(items, ct);

        // Mục gốc TRƯỚC, rồi phần còn lại theo đúng thứ tự host khai: con cần Id của cha, mà Id
        // chỉ tồn tại sau khi cha đã được upsert (xem MenuSeedItem.ParentCode — host không biết Id).
        var ordered = items
            .Where(item => item.ParentCode is null)
            .Concat(items.Where(item => item.ParentCode is not null));

        var idByCode = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var seeded = new List<(MenuSeedItem Item, Guid MenuId)>(items.Count);

        foreach (var item in ordered)
        {
            var menu = await UpsertMenuAsync(
                item.Code,
                item.Name,
                item.Route,
                item.Icon,
                ResolveParentId(item, idByCode),
                item.DisplayOrder,
                ct);

            idByCode[item.Code] = menu.Id;
            seeded.Add((item, menu.Id));
        }

        await db.SaveChangesAsync(ct);

        // Mục khai Roles rỗng ⇒ vòng lặp trong UpsertMenuRoleAsync không chạy ⇒ không dòng
        // SysMenuRole nào ⇒ mở cho mọi user đã đăng nhập (xem MenuSeedItem.Roles).
        foreach (var (item, menuId) in seeded)
            await UpsertMenuRoleAsync(menuId, item.Roles, ct);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// <c>Code</c> là danh tính của mục menu (mọi nhánh upsert tìm dòng cũ theo nó), nên hai mục
    /// cùng mã là hai định nghĩa tranh nhau MỘT dòng. Không chặn ở đây thì lần seed đầu chèn 2
    /// dòng cùng mã và vỡ ở <c>IX_SysMenus_Code</c> với một <c>23505</c> không chỉ ra được chỗ
    /// khai sai — còn lần seed sau lại "thành công" vì cả hai đều tìm thấy dòng cũ.
    /// </summary>
    private static void GuardAgainstDuplicateCodes(IReadOnlyCollection<MenuSeedItem> items)
    {
        var duplicates = items
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count == 0)
            return;

        throw new InvalidOperationException(
            $"{nameof(ICoreMenuSeedSource)} khai trùng mã menu: {string.Join(", ", duplicates)}. " +
            "Mỗi mã chỉ được xuất hiện một lần — nó là danh tính của mục menu, không phải nhãn.");
    }

    /// <summary>
    /// Ánh xạ <c>ParentCode → Id</c> trên các mục ĐÃ upsert trong chính lượt seed này. Mã cha chưa
    /// có trong bản đồ là lỗi khai báo của host, không phải trạng thái dữ liệu — nên ném thay vì
    /// lặng lẽ hạ mục đó xuống thành mục gốc (một menu mồ côi trồi lên đầu sidebar là thứ rất khó
    /// truy ngược về nguyên nhân).
    /// </summary>
    private static Guid? ResolveParentId(MenuSeedItem item, IReadOnlyDictionary<string, Guid> idByCode)
    {
        if (item.ParentCode is null)
            return null;

        if (idByCode.TryGetValue(item.ParentCode, out var parentId))
            return parentId;

        throw new InvalidOperationException(
            $"Mục menu '{item.Code}' khai ParentCode '{item.ParentCode}' nhưng chưa mục nào được xử lý " +
            $"mang mã ấy. Cha phải nằm trong CÙNG danh sách mà {nameof(ICoreMenuSeedSource)} trả về. Mục " +
            "gốc luôn được xử lý trước, nên chỉ menu lồng sâu hơn 1 cấp mới cần tự xếp cha đứng trước con.");
    }

    /// <summary>
    /// <b><c>IgnoreQueryFilters()</c> là BẮT BUỘC, không phải tuỳ chọn.</b> Đọc qua global query
    /// filter soft-delete thì menu ĐÃ XOÁ MỀM là vô hình với seeder, nên nhánh "chưa có thì thêm"
    /// sẽ chèn một dòng MỚI cùng <c>Code</c>. Kể từ migration 0008, <c>IX_SysMenus_Code</c> chỉ
    /// unique trong tập chưa xoá mềm — Postgres KHÔNG chặn lần chèn đó nữa, nên hỏng xảy ra trong
    /// IM LẶNG: bảng có 2 dòng cùng <c>Code</c> (1 đã xoá + 1 mới), sidebar hiện menu nhân đôi, và
    /// <see cref="UpsertMenuRoleAsync"/> chỉ gắn role cho dòng mới. (Trước 0008, unique toàn bảng
    /// biến đúng ca này thành <c>23505</c> — hỏng nhưng ồn ào.) Mọi seeder của module nghiệp vụ
    /// phải đọc theo đúng cách này vì cùng một lý do.
    ///
    /// <para>Thấy dòng đã xoá mềm thì <b>hồi sinh</b> (<see cref="SysMenu.ReviveWith"/>) chứ không
    /// chèn mới: menu là metadata do CHÍNH seeder sở hữu, mã menu là danh tính nghiệp vụ của nó,
    /// và mọi <c>SysMenuRole</c> đang trỏ vào <c>Id</c> cũ vẫn còn nguyên giá trị.</para>
    ///
    /// <para>Canh bằng <c>Tests/PlatformManager.Core.IntegrationTests/Menu/
    /// CoreSeederSoftDeletedMenuTests.cs</c>.</para>
    /// </summary>
    /// <summary>
    /// Mọi tên role trong danh sách seed phải tồn tại — kiểm TRƯỚC khi ghi bất cứ dòng nào.
    ///
    /// <para><b>Vì sao ném chứ không bỏ qua:</b> trước khi tách seam, tên role đến từ hằng
    /// <c>Roles.*</c> nên compiler bảo vệ. Nay danh sách menu do HOST cung cấp qua
    /// <see cref="ICoreMenuSeedSource"/>, tức tên role là chuỗi tự do. Hậu quả của một lỗi gõ
    /// KHÔNG hiển nhiên: mục menu thiếu mọi dòng <c>SysMenuRole</c> nghĩa là <b>mở cho MỌI user
    /// đã đăng nhập</b> (xem <see cref="MenuSeedItem.Roles"/>), chứ không phải "không ai thấy".
    /// Một typo làm menu quản trị hiện ra cho tất cả, im lặng.</para>
    ///
    /// <para><b>Vì sao kiểm ở ĐÂY chứ không trong <c>UpsertMenuRoleAsync</c>:</b> vòng lặp gán
    /// role chạy SAU một <c>SaveChangesAsync</c> đã commit cả cây menu. Ném ở đó sẽ để lại menu
    /// đã ghi xuống DB mà thiếu dòng role — đúng trạng thái "mở cho mọi người" mà guard này sinh
    /// ra để ngăn, lần này đã nằm trong bảng. Kiểm trước mọi lệnh ghi thì hỏng là hỏng sạch.</para>
    /// </summary>
    private async Task GuardAgainstUnknownRolesAsync(
        IReadOnlyCollection<MenuSeedItem> items, CancellationToken ct)
    {
        var unknown = new List<string>();

        foreach (var name in items.SelectMany(i => i.Roles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (await roleManager.FindByNameAsync(name) is null)
                unknown.Add(name);
        }

        if (unknown.Count == 0)
            return;

        throw new InvalidOperationException(
            $"Menu seed khai {unknown.Count} tên role không tồn tại: {string.Join(", ", unknown)}. " +
            $"Role hợp lệ: {string.Join(", ", Roles.All)}. Sửa ICoreMenuSeedSource của host — " +
            "bỏ qua lời gán này sẽ khiến mục menu mở cho MỌI user đã đăng nhập.");
    }

    private async Task<SysMenu> UpsertMenuAsync(
        string code, string name, string? route, string? icon, Guid? parentId, int displayOrder, CancellationToken ct)
    {
        var existing = await db.SysMenus
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Code == code, ct);

        if (existing is not null)
        {
            if (existing.IsDeleted)
                existing.ReviveWith(name, route, icon, parentId, displayOrder);

            return existing;
        }

        var menu = SysMenu.Create(code, name, route, icon, parentId, displayOrder);
        await db.SysMenus.AddAsync(menu, ct);
        return menu;
    }

    private async Task UpsertMenuRoleAsync(Guid sysMenuId, IReadOnlyCollection<string> roleNames, CancellationToken ct)
    {
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            var exists = await db.SysMenuRoles.AnyAsync(x => x.SysMenuId == sysMenuId && x.RoleId == role.Id, ct);
            if (!exists)
                await db.SysMenuRoles.AddAsync(SysMenuRole.Create(sysMenuId, role.Id), ct);
        }
    }
}
