using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Modules;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Modules;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật — seam <c>IModuleRegistrar</c> (CHỐT 2026-08-23, thi công 2026-09-08):</b> một tầng
/// nghiệp vụ phải cắm được đủ BA đường vào nền tảng — dịch vụ vào DI, cấu hình EF vào model,
/// controller vào MVC — mà KHÔNG sửa một dòng nào trong <c>PlatformManager.Core.*</c>.
///
/// <para><b>Vì sao luật này cần test chứ không cần đọc code.</b> Ba đường trên hỏng theo cùng một
/// kiểu: im lặng. Registrar không được nối vào danh sách thì dịch vụ của nó vắng mặt (lỗi chỉ nổ ở
/// request đầu tiên chạm tới), bảng của nó biến khỏi model (migration sinh ra thiếu bảng, hoặc tệ
/// hơn là sinh <c>DropTable</c>), controller của nó không được định tuyến (404 cho một endpoint có
/// thật trong mã nguồn). Không cái nào gây lỗi biên dịch.</para>
///
/// <para><b>Và vì sao phải có registrar GIẢ.</b> Hôm nay chỉ có Core hiện thực seam này, mà Core
/// khai <c>ApiAssembly = null</c> — nghĩa là đường thứ ba KHÔNG có dữ liệu thật để chạy. Một luật
/// chỉ quan sát Core sẽ xanh vĩnh viễn mà chưa từng chứng minh điều nó tuyên bố. Registrar giả
/// bên dưới đóng đúng vai tầng nghiệp vụ đầu tiên: nó khai đủ ba thứ và đi qua đúng hai hàm mà
/// <c>Program.cs</c> gọi (<see cref="ModuleRegistrationExtensions.AddModules"/> và
/// <see cref="ModuleRegistrationExtensions.AddModuleApplicationParts"/>), không phải bản mô phỏng
/// của chúng.</para>
///
/// <para>KHÔNG cần Docker: cả ba phép đo dừng ở mức MÔ TẢ (đọc <c>ServiceCollection</c>, dựng
/// model EF, đọc <c>ApplicationPartManager</c>) — không <c>BuildServiceProvider()</c>, không mở
/// kết nối nào.</para>
/// </summary>
public class ModuleRegistrarSeamTests
{
    // ── Đường 0: tầng có thật thì phải được NỐI VÀO ──────────────────────

    /// <summary>
    /// Luật quan trọng nhất của file này: <b>viết xong một registrar mà quên nối vào host thì
    /// ĐỎ.</b>
    ///
    /// <para>Hai nguồn ĐỘC LẬP, đúng khuôn đã dùng cho <see cref="MiddlewareWiringTests"/>: kỳ
    /// vọng đọc từ VĂN BẢN mã nguồn (có những hiện thực nào trong cây), thực tế đọc từ DANH SÁCH
    /// CỦA HOST (những hiện thực nào thật sự được nối). Lấy cả hai từ cùng một chỗ thì test chỉ
    /// hỏi một danh sách "anh có ai" rồi kiểm đúng những người đó.</para>
    ///
    /// <para>Kỳ vọng quét TOÀN cây <c>src/BE</c> (trừ <c>Tests/</c>) chứ không quét theo danh sách
    /// thư mục biết trước — tầng nghiệp vụ tương lai nằm ở thư mục hôm nay chưa có tên, và một
    /// lưới hẹp hơn ranh giới sẽ không thấy nó.</para>
    /// </summary>
    [Fact(DisplayName = "Mọi hiện thực IModuleRegistrar trong cây mã nguồn đều được nối vào host")]
    public void EveryRegistrarDeclaredInSource_IsWiredIntoHostList()
    {
        var files = RepoSourceTree.AllProductSourceFilesInTree();

        Assert.True(files.Count > 0,
            $"Không đọc được file .cs sản phẩm nào dưới '{RepoSourceTree.BackendRoot}' ⇒ test này không " +
            "đo gì. Xem thông điệp của RepoSourceTree.LocateBackendRoot.");

        var declared = files
            .SelectMany(file => RegistrarNamesIn(RepoSourceTree.ReadCodeWithoutComments(file)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Chặn "xanh mà không đo gì": hôm nay CoreModuleRegistrar chắc chắn có mặt trong cây.
        // Tập rỗng nghĩa là bộ dò hỏng, không phải "không ai vi phạm".
        Assert.True(declared.Count > 0,
            "Không tìm thấy hiện thực IModuleRegistrar nào trong mã nguồn sản phẩm ⇒ bộ dò đang hỏng " +
            "(CoreModuleRegistrar chắc chắn có thật). Sửa RegistrarDeclarationPattern, ĐỪNG nới luật.");

        var wired = CompositionRoot.Registrars()
            .Select(registrar => registrar.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var notWired = declared.Except(wired, StringComparer.Ordinal).ToList();

        Assert.True(notWired.Count == 0,
            "Tầng có hiện thực IModuleRegistrar nhưng KHÔNG được nối vào danh sách của host: " +
            string.Join(", ", notWired) + ". Hệ quả nếu để nguyên: dịch vụ của tầng đó vắng khỏi DI, " +
            "bảng của nó vắng khỏi model EF (migration sinh ra sẽ THIẾU chúng), controller của nó không " +
            "được định tuyến — cả ba đều im lặng. Cách sửa: thêm registrar vào " +
            "PlatformManager.Api/Modules/HostModuleRegistrars.Create().");

        var wiredButNotDeclared = wired.Except(declared, StringComparer.Ordinal).ToList();

        Assert.True(wiredButNotDeclared.Count == 0,
            "Host nối một registrar mà bộ dò không tìm thấy trong mã nguồn: " +
            string.Join(", ", wiredButNotDeclared) + ". Đây là dấu hiệu BỘ DÒ hỏng (đổi khuôn khai báo?), " +
            "và một bộ dò mù thì luật ở trên xanh mà không đo gì. Sửa RegistrarDeclarationPattern.");
    }

    /// <summary>
    /// Đối chứng — bộ dò phải BÁO đúng lớp hiện thực và KHÔNG báo cho lớp khác, cho chính khai báo
    /// interface, hay cho một lần nhắc tên trong biểu thức. Không có ca này, một lỗi trong
    /// <see cref="RegistrarDeclarationPattern"/> sẽ làm luật trên xanh mãi mãi.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò nhận đúng lớp hiện thực IModuleRegistrar, bỏ qua phần còn lại")]
    public void Detector_Recognizes_OnlyImplementationDeclarations()
    {
        const string probe = """
            public interface IModuleRegistrar { }
            public sealed class ShopModuleRegistrar(bool flag = false) : IModuleRegistrar { }
            public sealed class ReportRegistrar : BaseThing, IModuleRegistrar { }
            public sealed class NotARegistrar : IDisposable { }
            var name = typeof(IModuleRegistrar).Name;
            """;

        string[] expected = ["ReportRegistrar", "ShopModuleRegistrar"];

        Assert.Equal(
            expected.OrderBy(name => name, StringComparer.Ordinal),
            RegistrarNamesIn(RepoSourceTree.RemoveComments(probe)).OrderBy(name => name, StringComparer.Ordinal));
    }

    // ── Đường 1: RegisterServices → DI ───────────────────────────────────

    [Fact(DisplayName = "Đường 1 — RegisterServices của mọi tầng đi tới DI, kể cả tầng thêm sau")]
    public void RegisterServices_OfEveryRegistrar_ReachesDi()
    {
        var services = ComposeWithProbeModule();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ProbeModuleMarker));

        // Dịch vụ của Core vẫn còn nguyên trên cùng đường đó — seam không thay thế Core, nó CHỞ Core.
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(CoreSeeder));

        // Registrar tự nó cũng vào DI: đó là cách đọc lại được "ứng dụng gồm những tầng nào" mà
        // không cần một danh sách thứ hai.
        var registrars = services
            .Where(descriptor => descriptor.ServiceType == typeof(IModuleRegistrar))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<IModuleRegistrar>()
            .Select(registrar => registrar.ModuleName)
            .ToList();

        Assert.Contains(ProbeModuleRegistrar.Name, registrars);
        Assert.Contains("Core", registrars);
    }

    // ── Đường 2: PersistenceAssembly → model EF ──────────────────────────

    [Fact(DisplayName = "Đường 2 — PersistenceAssembly của mọi tầng đi tới model EF (kèm soft-delete của Core)")]
    public void PersistenceAssembly_OfEveryRegistrar_ReachesEfModel()
    {
        var configurationAssemblies = ComposeWithProbeModule()
            .Where(descriptor => descriptor.ServiceType == typeof(EfConfigurationAssembly))
            .Select(descriptor => (EfConfigurationAssembly)descriptor.ImplementationInstance!)
            .ToList();

        var options = new DbContextOptionsBuilder<PlatformManagerDbContext>()
            .UseNpgsql("Host=localhost;Database=arch_tests_module_seam;Username=none;Password=none")
            // ⚠️ BẮT BUỘC, và lý do KHÔNG hiển nhiên — đã bắt được thật lúc thi công 2026-09-08.
            // EF cache MODEL theo KIỂU DbContext (ModelCacheKey mặc định = kiểu context + designTime)
            // bên trong service provider nội bộ DÙNG CHUNG, không theo chuỗi kết nối. Nên context
            // dựng ở đây và context dựng bởi EfModelProbe (SoftDeleteQueryFilterTests /
            // SchemaBoundaryTests) TÁI DÙNG CÙNG MỘT model — cái nào chạy trước thì thắng. Hệ quả
            // đo được: chạy cả bộ thì test này ĐỎ (nhận model không có entity của tầng giả), chạy
            // riêng thì XANH; và chiều ngược lại còn tệ hơn — nếu test này chạy trước, entity của
            // tầng GIẢ sẽ rò sang model mà SchemaBoundaryTests chấm.
            //
            // Tắt cache service provider ⇒ context này dựng provider nội bộ RIÊNG ⇒ model riêng.
            // Đây là cách ly hai chiều: không nhận model của ai, không để lại model cho ai.
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new PlatformManagerDbContext(options, configurationAssemblies);

        var probe = context.Model.FindEntityType(typeof(ProbeModuleEntity));

        Assert.True(probe is not null,
            "Entity của tầng thêm sau KHÔNG vào được model EF qua IModuleRegistrar.PersistenceAssembly ⇒ " +
            "tầng nghiệp vụ đầu tiên sẽ phải sửa vào trong Core để có bảng của mình. Xem " +
            "ModuleRegistrationExtensions.AddModules và PlatformManagerDbContext.OnModelCreating. " +
            "Assembly đã nộp: " + string.Join(", ", configurationAssemblies.Select(a => a.Assembly.GetName().Name)) +
            ". Entity trong model: " + string.Join(", ", context.Model.GetEntityTypes().Select(e => e.ClrType.Name)));

        // Entity Core vẫn ở trong CÙNG model đó — một DbContext dùng chung, đúng chốt ở
        // doc/kien-truc-core-module.md §DbContext.
        Assert.True(context.Model.FindEntityType(typeof(SysMenu)) is not null,
            "Entity Core biến mất khỏi model ⇒ CoreModuleRegistrar.PersistenceAssembly không tới nơi.");

        Assert.Equal(PlatformManagerDbContext.BusinessSchema, probe!.GetSchema());

        // CƠ CHẾ của Core (soft-delete khai một chỗ) tự phủ lên entity của tầng mới mà tầng đó
        // không phải khai gì — đây là thứ làm seam có giá trị, không chỉ là một đường ống.
        Assert.Contains(probe.GetDeclaredQueryFilters(),
            filter => filter.Key == PlatformManagerDbContext.SoftDeleteFilterKey);
    }

    // ── Đường 3: ApiAssembly → ApplicationPart ───────────────────────────

    [Fact(DisplayName = "Đường 3 — ApiAssembly của mọi tầng thành ApplicationPart, null thì bỏ qua")]
    public void ApiAssembly_OfEveryRegistrar_BecomesApplicationPart()
    {
        var mvc = new ServiceCollection()
            .AddControllers()
            .AddModuleApplicationParts(Registrars());

        var assemblies = mvc.PartManager.ApplicationParts
            .OfType<AssemblyPart>()
            .Select(part => part.Assembly)
            .ToList();

        Assert.Contains(typeof(ProbeModuleRegistrar).Assembly, assemblies);

        // Đối chứng trong cùng phép đo: CoreModuleRegistrar khai ApiAssembly = null, và nhánh null
        // phải là BỎ QUA chứ không phải nạp nhầm assembly nào khác của tầng đó.
        Assert.DoesNotContain(typeof(PlatformManagerDbContext).Assembly, assemblies);
    }

    // ── Guard của cơ chế ─────────────────────────────────────────────────

    [Fact(DisplayName = "AddModules từ chối hai tầng trùng ModuleName")]
    public void AddModules_Rejects_DuplicateModuleName()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddModules(
                configuration,
                [new ProbeModuleRegistrar(), new ProbeModuleRegistrar()]));

        Assert.Contains(ProbeModuleRegistrar.Name, exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AddModules từ chối danh sách tầng rỗng")]
    public void AddModules_Rejects_EmptyRegistrarList()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddModules(configuration, []));
    }

    // ── Hạ tầng của test ─────────────────────────────────────────────────

    /// <summary>
    /// Danh sách tầng THẬT của host + một tầng giả đóng vai tầng nghiệp vụ đầu tiên. Dùng đúng
    /// <see cref="CompositionRoot.Registrars"/> chứ không dựng lại Core bằng tay: nếu Core rời khỏi
    /// danh sách của host thì mọi test ở đây phải đỏ theo.
    /// </summary>
    private static List<IModuleRegistrar> Registrars() =>
        [.. CompositionRoot.Registrars(), new ProbeModuleRegistrar()];

    private static IServiceCollection ComposeWithProbeModule() =>
        new ServiceCollection().AddModules(new ConfigurationBuilder().Build(), Registrars());

    /// <summary>
    /// Khai báo lớp hiện thực <c>IModuleRegistrar</c>. <c>[^{;]</c> không vượt được qua dấu mở
    /// thân lớp hay dấu kết câu, nên nó không thể nối một khai báo lớp ở chỗ này với một lần nhắc
    /// tên interface ở chỗ khác. Khai báo <c>interface</c> không khớp (không có trong danh sách
    /// từ khoá), nên chính file khai hợp đồng cũng không bị tính là hiện thực.
    /// </summary>
    private static readonly Regex RegistrarDeclarationPattern = new(
        @"\b(?:class|record|struct)\s+(?<name>\w+)[^{;]*?:\s*[^{;]*?\bIModuleRegistrar\b",
        RegexOptions.Compiled);

    private static IEnumerable<string> RegistrarNamesIn(string codeWithoutComments) =>
        RegistrarDeclarationPattern.Matches(codeWithoutComments)
            .Select(match => match.Groups["name"].Value);

    /// <summary>
    /// Tầng nghiệp vụ GIẢ — sống trong assembly test, không tạo project nào. Khai đủ ba thứ mà
    /// một tầng thật khai, nên nó chạy đúng ba đường mà tầng thật sẽ chạy.
    /// </summary>
    private sealed class ProbeModuleRegistrar : IModuleRegistrar
    {
        public const string Name = "ArchTestProbe";

        public string ModuleName => Name;

        public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
            services.AddSingleton<ProbeModuleMarker>();

        public Assembly PersistenceAssembly => typeof(ProbeModuleRegistrar).Assembly;

        public Assembly? ApiAssembly => typeof(ProbeModuleRegistrar).Assembly;
    }

    private sealed class ProbeModuleMarker;
}

/// <summary>
/// Entity của tầng giả. <c>public</c> vì <c>ApplyConfigurationsFromAssembly</c> quét kiểu khai
/// trong assembly — giữ cùng hình dạng với entity thật (kế thừa <see cref="BaseEntity"/>, field
/// nghiệp vụ <c>private set</c>) để phép đo nói về entity thật chứ về một thứ khác.
///
/// <para>KHÔNG lọt vào các luật khác: <c>EntityEncapsulationTests</c> và
/// <c>SoftDeleteQueryFilterTests</c> chỉ quét assembly <c>Core.Domain</c>, còn model của ứng dụng
/// thật (<c>EfModelProbe</c>) chỉ nhận assembly do host nộp — assembly test không có trong đó.</para>
/// </summary>
public sealed class ProbeModuleEntity : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
}

/// <summary>
/// Cấu hình EF của entity tầng giả — khai schema nghiệp vụ TƯỜNG MINH, đúng thứ mà
/// <c>SchemaBoundaryTests</c> đòi ở entity của tầng nghiệp vụ thật.
/// </summary>
public sealed class ProbeModuleEntityConfiguration : IEntityTypeConfiguration<ProbeModuleEntity>
{
    public void Configure(EntityTypeBuilder<ProbeModuleEntity> builder)
    {
        builder.ToTable("ArchTestProbeEntities", PlatformManagerDbContext.BusinessSchema);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(64).IsRequired();
    }
}
