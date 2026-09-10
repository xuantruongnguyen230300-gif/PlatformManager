using PlatformManager.Core.Application.Common;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Một permission-key của dự án và nhãn hiển thị của nó.
///
/// <para><b><c>DisplayName</c> là chữ HIỂN THỊ, và đó là nợ đã biết.</b> Theo hướng đã chốt "FE sở
/// hữu câu chữ", trường này đáng lẽ là một MÃ để FE tra bảng dịch, không phải câu tiếng Việt dựng
/// sẵn ở BE (cùng lý lẽ với <c>businessCode</c>, xem
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4). Lượt tách 2026-09-03 CỐ Ý không đụng vào
/// đó: đổi kiểu giá trị là đổi hợp đồng <c>GET /api/admin/permissions/resources</c>
/// (doc/contracts/permissions.md CONTRACT PERM-2) và kéo theo FE — hai việc khác nhau thì làm
/// thành hai lượt. Điều lượt này ĐÃ đạt được là chuyển câu tiếng Việt đó ra khỏi Core, nên khi
/// i18n tới thì chỉ còn một chỗ ở host phải sửa.</para>
/// </summary>
/// <param name="Key">Khoá gắn vào <c>[RequirePermission]</c> và lưu ở cột <c>ResourceKey</c> của
/// bảng <c>RolePermissions</c>. So khớp ORDINAL ở mọi nơi — khác hoa/thường là hai khoá khác nhau.</param>
/// <param name="DisplayName">Nhãn cho màn hình phân quyền. KHÔNG lưu DB.</param>
public sealed record ResourceKeyDefinition(string Key, string DisplayName)
{
    /// <summary>
    /// Vai được cấp key này ở LẦN SEED ĐẦU khi host không khai gì khác: <c>Admin</c> + <c>User</c>.
    ///
    /// <para><b>Đây là mặc định BẮT BUỘC, không phải một lựa chọn tiện tay.</b> Nó giữ nguyên hành
    /// vi seed có từ trước lượt mở rộng 2026-09-09 — mục tiêu đã ghi ở
    /// <c>CoreSeeder.SeedRolePermissionsAsync</c> là <i>"GIỮ NGUYÊN hành vi trước khi vá (mọi user
    /// thao tác được)"</i>, một quyết định di trú để việc bật deny-by-default không khoá mất người
    /// đang dùng hệ thống. Đổi giá trị này là đổi quyền của MỌI key hiện có cùng lúc.</para>
    ///
    /// <para><c>SuperAdmin</c> KHÔNG có mặt và không được khai: vai đó bỏ qua phép kiểm quyền ở
    /// <c>RequirePermissionFilter</c> (break-glass), nên mọi dòng seed cho nó đều là dòng chết.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultSeedRoles = [Roles.Admin, Roles.User];

    /// <summary>
    /// Vai được cấp key này khi chạy lệnh seed. Mặc định <see cref="DefaultSeedRoles"/>; host thu
    /// hẹp bằng cú pháp khởi tạo đối tượng, ví dụ <c>new("x.manage", "…") { SeedRoles = [Roles.Admin] }</c>.
    ///
    /// <para><b>Chỉ áp cho LẦN SEED, không phải luật phân quyền.</b> Sau khi seed, SuperAdmin cấp
    /// hay thu hồi tuỳ ý ở màn Phân quyền và <c>PUT</c> ghi đè toàn bộ ma trận — trường này không
    /// tham gia đường đó, và cũng không ngăn được gì ở đó.</para>
    ///
    /// <para><b>Vì sao là DỮ LIỆU ở host chứ không phải hằng số trong Core</b> (mở rộng 2026-09-09):
    /// có nghiệp vụ mà "cấp sẵn cho mọi người đăng nhập" là sai ngay từ lần seed đầu — lý lẽ "giữ
    /// nguyên hành vi cũ" không áp được cho một key MỚI, vì chưa có ai đang dùng để mà giữ. Hai lối
    /// còn lại đều hỏng ranh giới: hard-code danh sách trừ trong Core làm Core biết tên một nghiệp
    /// vụ (<c>CoreMustNotKnowBusinessNameTests</c> canh đúng điều đó), còn bỏ seed hẳn thì vai quản
    /// trị cũng không có quyền.</para>
    /// </summary>
    public IReadOnlyList<string> SeedRoles { get; init; } = DefaultSeedRoles;
}

/// <summary>
/// Seam "danh mục permission-key của dự án" — <b>host cung cấp, Core tiêu thụ</b>. Cùng ranh giới
/// và cùng khuôn với <c>ICoreMenuSeedSource</c> / <c>ICoreBootstrapAccountSource</c>: <b>Core giữ
/// CƠ CHẾ, dự án cung cấp DỮ LIỆU</b> (doc/kien-truc-core-module.md).
///
/// <para><b>Vì sao tồn tại</b> (tách 2026-09-03): trước đó Core có lớp <c>ResourceKeys</c> mang
/// một danh sách ĐÓNG các key, một bảng nhãn tiếng Việt, và một docstring ra chỉ thị "module
/// nghiệp vụ mới khai key của mình Ở ĐÂY". Tức là Core chủ động bảo dự án ghi dữ liệu vào trong
/// Core — chiều phụ thuộc ngược hẳn với ranh giới đã chốt, và là thứ khiến CoreBase không cắm
/// được vào dự án thứ hai nếu không mổ lại. Ba đường dùng nó (ma trận GET, validator của PUT,
/// seeder) đều là CƠ CHẾ và ở lại Core; chỉ danh sách key + nhãn đi ra host.</para>
///
/// <para><b>Cái gì KHÔNG đi qua seam này</b> — phần Core giữ vì nó là cơ chế, không phải dữ liệu:
/// phép kiểm quyền deny-by-default và đường bỏ qua của SuperAdmin (<c>RequirePermissionFilter</c>);
/// luật "PUT là ghi đè toàn bộ nên payload phải phủ đủ danh mục"; CƠ CHẾ seed (idempotent,
/// SuperAdmin không cần dòng nào, mặc định <see cref="ResourceKeyDefinition.DefaultSeedRoles"/>);
/// và tập vai (<c>Roles.All</c>) — vai là khái niệm của Core, không phải của dự án.</para>
///
/// <para><b>Sửa 2026-09-09:</b> dòng trên trước đây ghi <i>"việc seed cấp đủ danh mục cho Admin +
/// User"</i> là phần Core giữ. Nay VAI được cấp khi seed đi qua seam này
/// (<see cref="ResourceKeyDefinition.SeedRoles"/>), vì có nghiệp vụ mà cấp sẵn cho mọi người đăng
/// nhập là sai ngay lần seed đầu. Phần ở lại Core là cơ chế và MẶC ĐỊNH — host không khai gì thì
/// hành vi vẫn y hệt trước.</para>
///
/// <para><b>Phải đăng ký ở host; Core cố ý KHÔNG có hiện thực mặc định.</b> Cùng lý do đã ghi ở
/// <c>ICoreMenuSeedSource</c>: một bản mặc định trả danh sách rỗng sẽ biến "quên đăng ký" thành
/// một màn hình phân quyền trống rỗng và một bảng <c>RolePermissions</c> không dòng nào — mà
/// <c>RequirePermissionFilter</c> thì deny-by-default, nên hậu quả thật là mọi endpoint có
/// <c>[RequirePermission]</c> trả 403 cho tất cả trừ SuperAdmin, không kèm lỗi nào giải thích.
/// Thiếu đăng ký ⇒ DI không phân giải được ⇒ hỏng ngay và hỏng ồn ào.</para>
/// </summary>
public interface ICoreResourceKeySource
{
    /// <summary>
    /// Toàn bộ permission-key của dự án. Thứ tự khai được giữ nguyên ra tới response của
    /// <c>GET /api/admin/permissions/resources</c> — đó là thứ tự các dòng trên màn hình phân
    /// quyền, nên sắp xếp có chủ đích ở host chứ Core không sắp lại.
    ///
    /// <para>Đồng bộ có chủ đích — đây là bảng hằng số của một dự án, không phải thứ đọc từ I/O
    /// (cùng lý do đã ghi ở <c>ICoreMenuSeedSource.GetMenuItems</c>).</para>
    /// </summary>
    IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys();
}

/// <summary>
/// Phần dùng chung của ba đường tiêu thụ <see cref="ICoreResourceKeySource"/> (ma trận GET,
/// validator của PUT, seeder).
/// </summary>
public static class ResourceKeySourceExtensions
{
    /// <summary>
    /// Danh mục đã kiểm tính duy nhất, giữ NGUYÊN thứ tự host khai.
    ///
    /// <para><b>Vì sao ném thay vì lặng lẽ gộp trùng.</b> Bản Core cũ giữ nhãn trong một
    /// <c>Dictionary</c> khởi tạo tĩnh, nên một key khai hai lần là lỗi ngay lần dùng đầu — tính
    /// chất đó mất đi khi danh sách chuyển sang host, và mất im lặng: key trùng sẽ thành hai dòng
    /// giống hệt trên màn hình phân quyền, còn <c>PUT</c> thì bị chính validator chống trùng của
    /// nó từ chối vĩnh viễn vì payload buộc phải phủ đủ danh mục. Ném ở đây trả lại đúng mức ồn
    /// ào cũ.</para>
    /// </summary>
    public static IReadOnlyList<ResourceKeyDefinition> Catalog(this ICoreResourceKeySource source)
    {
        var definitions = source.GetResourceKeys().ToList();

        var duplicates = definitions
            .GroupBy(definition => definition.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Host khai trùng permission-key: {string.Join(", ", duplicates)}. Mỗi key chỉ được " +
                $"xuất hiện đúng một lần trong {nameof(ICoreResourceKeySource)}.{nameof(ICoreResourceKeySource.GetResourceKeys)}().");

        foreach (var definition in definitions)
            GuardSeedRoles(definition);

        return definitions;
    }

    /// <summary>
    /// Kiểm <see cref="ResourceKeyDefinition.SeedRoles"/> của MỘT key. Ba ca sai dưới đây đều hỏng
    /// IM LẶNG nếu để lọt — seed vẫn thoát 0, chỉ có ma trận quyền khác thứ người khai định làm.
    ///
    /// <para><b>Rỗng:</b> không vai nào được cấp ⇒ mọi endpoint mang key đó trả 403 cho tất cả trừ
    /// SuperAdmin, không kèm lỗi nào giải thích — đúng cách hỏng mà docstring của
    /// <see cref="ICoreResourceKeySource"/> mô tả cho ca quên đăng ký seam.</para>
    ///
    /// <para><b>Tên vai lạ:</b> chuỗi tự do, nên một lỗi gõ (<c>"Admn"</c>) không sai kiểu ở đâu cả;
    /// seeder chỉ đơn giản không tìm thấy vai đó và bỏ qua. Hướng hỏng là "hụt quyền", nên triệu
    /// chứng đến từ người dùng chứ không từ log.</para>
    ///
    /// <para><b>Khai <c>SuperAdmin</c>:</b> vai đó bỏ qua phép kiểm quyền ở
    /// <c>RequirePermissionFilter</c>, nên dòng seed cho nó vừa vô tác dụng vừa gây hiểu nhầm khi
    /// đọc bảng <c>RolePermissions</c> — người đọc sẽ tưởng quyền của SuperAdmin đến từ những dòng
    /// đó và có thể thu hồi được bằng cách xoá chúng.</para>
    /// </summary>
    private static void GuardSeedRoles(ResourceKeyDefinition definition)
    {
        if (definition.SeedRoles.Count == 0)
            throw new InvalidOperationException(
                $"Permission-key '{definition.Key}' khai {nameof(ResourceKeyDefinition.SeedRoles)} RỖNG. " +
                $"Không vai nào được cấp key này khi seed, nên mọi endpoint mang nó sẽ trả 403 cho tất cả " +
                $"trừ SuperAdmin. Khai ít nhất một vai, hoặc bỏ hẳn {nameof(ResourceKeyDefinition.SeedRoles)} " +
                $"để dùng mặc định ({string.Join(" + ", ResourceKeyDefinition.DefaultSeedRoles)}).");

        foreach (var roleName in definition.SeedRoles)
        {
            if (string.Equals(roleName, Roles.SuperAdmin, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Permission-key '{definition.Key}' khai vai '{Roles.SuperAdmin}' trong " +
                    $"{nameof(ResourceKeyDefinition.SeedRoles)}. Vai đó bỏ qua phép kiểm quyền (break-glass), " +
                    $"nên dòng seed cho nó không có tác dụng gì và chỉ làm bảng RolePermissions gây hiểu nhầm. " +
                    $"Bỏ '{Roles.SuperAdmin}' khỏi danh sách.");

            if (!Roles.All.Contains(roleName, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    $"Permission-key '{definition.Key}' khai vai không tồn tại: '{roleName}'. " +
                    $"Vai hợp lệ: {string.Join(", ", Roles.All)}. Seeder sẽ lặng lẽ bỏ qua tên vai lạ, " +
                    $"nên hậu quả là vai định cấp KHÔNG có quyền mà không có lỗi nào báo.");
        }
    }

    /// <summary>Chỉ tập khoá, giữ nguyên thứ tự khai.</summary>
    public static IReadOnlyList<string> Keys(this ICoreResourceKeySource source) =>
        [.. source.Catalog().Select(definition => definition.Key)];
}
