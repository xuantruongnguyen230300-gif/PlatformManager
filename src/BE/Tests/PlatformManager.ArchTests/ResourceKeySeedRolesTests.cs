using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Business.Application.Permissions;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Canh <b>GIÁ TRỊ</b> của ngoại lệ Q36: key ghi dữ liệu DTI được seed cho <c>Admin</c> và
/// <b>CHỈ</b> <c>Admin</c> — vai <c>User</c> KHÔNG được cấp sẵn
/// (spec/danh-muc-dti/business-rules.md §6.5).
///
/// <para><b>Vì sao cần test này dù đường đi đã có test</b> (finding F3, 2026-09-10):
/// <c>CoreSeederRolePermissionSeedTests</c> chứng minh <b>CƠ CHẾ</b> — rằng
/// <c>SeedRoles</c> được tôn trọng — nhưng nó làm vậy bằng một key TỔNG HỢP dựng trong test, nên
/// nó không đọc khai báo THẬT của host. Đổi
/// <c>SeedRoles = [Roles.Admin]</c> thành <c>[Roles.Admin, Roles.User]</c> ở
/// <c>AppResourceKeySource</c> sẽ vi phạm Q36 mà bộ test đó vẫn xanh. Thêm nữa, nó nằm trong
/// <c>Core.IntegrationTests</c> — bộ cần Docker, tức trên máy không có Docker thì nó không chạy
/// lần nào.</para>
///
/// <para><b>Vì sao đây là thứ ĐÁNG canh bằng máy:</b> mặc định của seam là
/// <c>[Admin, User]</c> và mặc định đó là một GIÁ TRỊ HỢP LỆ — nên quên mệnh đề
/// <c>SeedRoles</c>, hoặc một lượt refactor "cho nhất quán" gỡ nó đi, sẽ mở quyền ghi cho toàn bộ
/// người đăng nhập mà KHÔNG có triệu chứng nào. Chính docstring ở
/// <c>AppResourceKeySource</c> tự khai rằng nghiệm thu của nó là một thao tác BẰNG TAY sau khi
/// seed; test này biến thao tác đó thành thứ chạy mỗi lần.</para>
///
/// <para>KHÔNG cần Docker: đọc danh mục ở mức MÔ TẢ đăng ký, không build provider, không chạm DB.</para>
/// </summary>
public class ResourceKeySeedRolesTests
{
    [Fact(DisplayName = "Q36 — key ghi dữ liệu DTI chỉ seed cho Admin, KHÔNG có User")]
    public void DtiManageKey_IsSeeded_ForAdminOnly()
    {
        var catalog = HostResourceKeyCatalog();

        // Chặn "xanh mà không đo gì" #1: danh mục rỗng thì mọi assert bên dưới vô nghĩa.
        Assert.True(catalog.Count > 0,
            "Danh mục permission-key của host rỗng ⇒ luật này không đo gì. Nguyên nhân thường là " +
            "ICoreResourceKeySource không còn được đăng ký trong composition root — xem " +
            "CompositionRoot.CoreServices().");

        var dti = catalog.SingleOrDefault(definition =>
            string.Equals(definition.Key, BusinessResourceKeys.DtiManage, StringComparison.Ordinal));

        // Chặn "xanh mà không đo gì" #2: key vắng mặt thì SingleOrDefault trả null, và một
        // Assert.Equal trên null sẽ báo lỗi khó hiểu. Nói thẳng hậu quả thay vì để người đọc đoán.
        Assert.True(dti is not null,
            $"Key '{BusinessResourceKeys.DtiManage}' KHÔNG có trong danh mục của host ⇒ nó không hiện " +
            "trên màn Phân quyền, không role nào cấp được, và mọi endpoint ghi DTI trả 403 cho tất cả " +
            "trừ SuperAdmin. Thêm một dòng vào AppResourceKeySource.Definitions (bước 2 của §6.5).");

        Assert.Equal([Roles.Admin], dti!.SeedRoles);
    }

    /// <summary>
    /// Đối chứng — chứng minh phép đo trên PHÂN BIỆT được hai giá trị, chứ không phải luôn đúng.
    /// Không có ca này, một lỗi trong <see cref="HostResourceKeyCatalog"/> (ví dụ trả về một danh
    /// mục dựng tay) sẽ làm luật trên xanh vĩnh viễn.
    ///
    /// <para>Key <c>import.manage</c> là di sản KHÔNG khai <c>SeedRoles</c>, nên nó phải nhận
    /// đúng mặc định <c>[Admin, User]</c> — tức hai key trong CÙNG một danh mục cho hai kết quả
    /// khác nhau, và đó chính là thứ Q36 gọi là "ngoại lệ".</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: key không khai SeedRoles vẫn nhận mặc định [Admin, User]")]
    public void KeyWithoutExplicitSeedRoles_Falls_BackToDefault()
    {
        var catalog = HostResourceKeyCatalog();

        var legacy = catalog.SingleOrDefault(definition =>
            string.Equals(definition.Key, "import.manage", StringComparison.Ordinal));

        Assert.True(legacy is not null,
            "Key di sản 'import.manage' đã biến khỏi danh mục ⇒ ca đối chứng này không còn đo gì. " +
            "Nếu việc gỡ nó là CÓ CHỦ ĐÍCH thì thay ca đối chứng bằng một key khác không khai " +
            "SeedRoles, ĐỪNG xoá ca này — mất nó là mất bằng chứng rằng phép đo ở trên phân biệt " +
            "được hai giá trị.");

        Assert.Equal(ResourceKeyDefinition.DefaultSeedRoles, legacy!.SeedRoles);

        // Và mặc định đó KHÁC giá trị của ngoại lệ Q36 — nếu hai bên bằng nhau thì luật trên
        // không chứng minh được điều gì.
        Assert.NotEqual<IReadOnlyList<string>>([Roles.Admin], legacy.SeedRoles);
    }

    /// <summary>
    /// Danh mục THẬT mà host cấp cho Core, lấy qua đúng composition root của ứng dụng — không
    /// dựng tay, không <c>new AppResourceKeySource()</c>. Lấy tay thì test chỉ chứng minh về một
    /// đối tượng nó tự tạo, còn dòng đăng ký trong <c>Program.cs</c> có thể đã trỏ đi nơi khác.
    /// </summary>
    private static IReadOnlyList<ResourceKeyDefinition> HostResourceKeyCatalog()
    {
        var descriptor = CompositionRoot.CoreServices()
            .LastOrDefault(item => item.ServiceType == typeof(ICoreResourceKeySource));

        // AddModules không đăng ký seam này (host tự khai bằng AddSingleton ngay sau đó), nên
        // đường DI ở đây có thể rỗng. Khi đó đọc thẳng hiện thực của host — vẫn là VĂN BẢN khai
        // báo thật, chỉ khác cách với tới.
        var source = descriptor?.ImplementationInstance as ICoreResourceKeySource
            ?? (ICoreResourceKeySource)Activator.CreateInstance(HostResourceKeySourceType())!;

        return [.. source.GetResourceKeys()];
    }

    /// <summary>
    /// <c>AppResourceKeySource</c> là <c>internal</c> ở project host. Reflection thấy được kiểu
    /// internal, nên KHÔNG cần <c>InternalsVisibleTo</c> — cùng lý do đã ghi ở
    /// <see cref="ProductAssemblies"/> cho các middleware internal của host.
    /// </summary>
    private static Type HostResourceKeySourceType()
    {
        var hostAssembly = typeof(Api.Common.CorsPolicyOptions).Assembly;

        return hostAssembly.LoadableTypes().Single(type =>
            typeof(ICoreResourceKeySource).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false });
    }
}
