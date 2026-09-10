using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Api.Permissions;

/// <summary>
/// Permission-key của CHÍNH dự án PlatformManager, dạng hằng số — cần <c>const</c> vì
/// <c>[RequirePermission(...)]</c> là attribute, và đối số attribute phải biết được lúc biên dịch.
///
/// <para><b>Vì sao ở host chứ không ở Core</b> (chuyển 2026-09-03): trước đó danh sách này là lớp
/// <c>ResourceKeys</c> nằm trong <c>Core.Application</c> — một danh mục ĐÓNG kèm nhãn tiếng Việt,
/// và docstring của nó ra chỉ thị cho module nghiệp vụ khai key vào trong Core. Cả tập key lẫn
/// nhãn đều là thứ riêng của một dự án, nên để chúng trong Core nghĩa là muốn tái dùng CoreBase
/// thì phải mổ vào Core. Cơ chế thì vẫn ở Core và host không đụng tới được: kiểm quyền
/// deny-by-default, ma trận, luật "PUT phải phủ đủ danh mục", và CƠ CHẾ seed (idempotent,
/// SuperAdmin không cần dòng nào, mặc định <see cref="ResourceKeyDefinition.DefaultSeedRoles"/>)
/// — xem <see cref="ICoreResourceKeySource"/>.</para>
///
/// <para><b>Sửa 2026-09-09:</b> dòng trên trước đây ghi <i>"seed cấp đủ danh mục cho Admin +
/// User"</i> là phần Core giữ. Hết đúng từ lượt mở rộng cùng ngày: VAI được cấp khi seed nay là
/// DỮ LIỆU của host (<see cref="ResourceKeyDefinition.SeedRoles"/>), Core chỉ giữ cơ chế và giá
/// trị MẶC ĐỊNH. Cùng câu sai đó tồn tại song song ở docstring của
/// <see cref="ICoreResourceKeySource"/> và đã sửa cùng lượt.</para>
///
/// <para><b>Thêm key mới:</b> thêm một <c>const</c> ở đây, thêm một dòng vào
/// <see cref="AppResourceKeySource.Definitions"/>, rồi gắn <c>[RequirePermission(...)]</c> lên
/// action. Thiếu bước thứ hai thì key không hiện trên màn hình phân quyền và không role nào cấp
/// được — endpoint sẽ 403 cho tất cả trừ SuperAdmin. Key nào KHÔNG được cấp sẵn cho mọi người
/// đăng nhập thì thu hẹp ngay tại dòng đó:
/// <c>new(Key, "Nhãn") { SeedRoles = [Roles.Admin] }</c>.</para>
/// </summary>
public static class AppResourceKeys
{
    /// <summary>
    /// ⚠️ <b>DI SẢN của module DtiWeekly đã gỡ 2026-08-29</b> — hôm nay (2026-09-03) KHÔNG endpoint
    /// nào mang <c>[RequirePermission(Import)]</c> trong mã sản phẩm; nơi duy nhất dùng nó là
    /// controller thăm dò của integration test. Giữ lại vì đường Import CSV/Excel đang được đưa
    /// lên Core thành năng lực dùng chung, và vì bỏ hẳn là một quyết định có hậu quả trên dữ liệu:
    /// <c>RolePermissions</c> ở database đã seed đang có dòng mang key này, xoá key khỏi danh mục
    /// biến chúng thành dòng mồ côi (xem <c>ResourcePermissionEndpointTests</c> — <c>PUT</c> ghi
    /// đè toàn bộ nên chúng cũng không tự dọn).
    /// </summary>
    public const string Import = "import.manage";
}

/// <summary>
/// Hiện thực seam <see cref="ICoreResourceKeySource"/> cho dự án này. Singleton — bảng hằng số,
/// không trạng thái (cùng khuôn với <c>AppMenuSeedSource</c> và <c>AppBootstrapAccountSource</c>).
/// </summary>
internal sealed class AppResourceKeySource : ICoreResourceKeySource
{
    /// <summary>
    /// Thứ tự khai là thứ tự dòng trên màn hình phân quyền — Core không sắp lại (xem
    /// <see cref="ICoreResourceKeySource.GetResourceKeys"/>).
    ///
    /// <para><b>Nhãn là câu tiếng Việt, và đó là nợ đã biết chứ không phải chỗ đặt sai:</b> theo
    /// hướng "FE sở hữu câu chữ" thì trường này nên là một MÃ để FE tra bảng dịch. Đổi nó là đổi
    /// hợp đồng PERM-2 và kéo theo FE, nên để thành một lượt riêng — xem
    /// <see cref="ResourceKeyDefinition"/>.</para>
    /// </summary>
    private static readonly ResourceKeyDefinition[] Definitions =
    [
        new(AppResourceKeys.Import, "Import CSV/Excel"),
    ];

    public IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys() => Definitions;
}
