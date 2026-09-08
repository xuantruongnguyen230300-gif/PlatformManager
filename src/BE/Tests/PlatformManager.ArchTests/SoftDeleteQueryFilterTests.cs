using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Infrastructure;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Soft-delete phải được gắn cho MỌI entity kế thừa <see cref="BaseEntity"/> bằng một vòng lặp
/// duy nhất trong <c>PlatformManagerDbContext.OnModelCreating</c> — KHÔNG khai lẻ ở từng
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> (xem doc/huong_dan/quy-uoc/be-entity-domain.md
/// §"Base entity").
///
/// Test này bắt đúng 3 cách hỏng mà compiler KHÔNG bắt được:
/// (1) thêm entity mới kế thừa BaseEntity nhưng vòng lặp không phủ tới nó;
/// (2) đảo thứ tự — gọi vòng lặp TRƯỚC <c>ApplyConfigurationsFromAssembly</c>, khi đó
///     <c>Model.GetEntityTypes()</c> còn rỗng nên không entity nào nhận được filter;
/// (3) cả một TẦNG biến mất khỏi model — registrar của nó không có mặt trong
///     <c>HostModuleRegistrars.Create()</c>, nên <c>PersistenceAssembly</c> của nó không bao giờ
///     thành <see cref="EfConfigurationAssembly"/> trong DI.
/// Cả 3 đều im lặng lúc chạy: dữ liệu đã xoá mềm lọt thẳng ra API, không có gì báo.
///
/// <para><b>Sửa 2026-08-28 (finding F6) — ca (3) trước đây được KHAI mà không được BẮT.</b> Bản
/// trước tự tay truyền danh sách assembly vào <see cref="CreateModelOnlyContext"/>, tức là bỏ
/// hẳn một tầng khỏi DI thì test vẫn xanh — nó kiểm một model dựng bằng
/// đường khác với đường mà ứng dụng thật dùng. Nay danh sách assembly lấy từ CHÍNH đường host đi:
/// <c>AddModules()</c> chạy trên <c>HostModuleRegistrars</c> (xem
/// <see cref="ConfigurationAssembliesFromDi"/>). Canary đã chạy
/// 2026-08-28 trên module DtiWeekly (đã xoá 2026-08-29): comment dòng
/// <c>AddSingleton(new EfConfigurationAssembly(...))</c> — dòng đó nay nằm ở
/// <c>Core.Infrastructure/Modules/ModuleRegistrationExtensions.cs</c> trong vòng lặp của
/// <c>AddModules</c>, không còn ở <c>DependencyInjection.cs</c> của từng module (seam
/// <c>IModuleRegistrar</c>, 2026-09-08) →
/// <see cref="EveryModuleRegistration_ContributesItsOwnConfigurationAssembly"/> và
/// <see cref="EveryBaseEntityDescendant_IsMappedIntoModel"/> đỏ; khôi phục → xanh. (Hai test
/// filter còn lại VẪN xanh trong ca đó, đúng như thiết kế: chúng chỉ nói về những entity CÓ
/// trong model — entity vắng mặt thì cũng vắng khỏi tập chúng kiểm. Đó chính là lý do ca (3)
/// cần một test riêng thay vì trông cậy vào chúng.)</para>
///
/// <para><b>2026-08-29:</b> module nghiệp vụ duy nhất đã bị xoá, nên hiện chỉ còn Core trong cả
/// <see cref="DomainAssemblies"/> lẫn <see cref="ConfigurationAssembliesFromDi"/>. Ca (3) vì thế
/// tạm thời không có gì để bắt. <b>Cập nhật 2026-09-08:</b> dựng tầng mới thì chỉ phải thêm
/// <see cref="DomainAssemblies"/> — vế còn lại tự đi theo, vì
/// <see cref="ConfigurationAssembliesFromDi"/> đọc thẳng danh sách registrar của host.</para>
///
/// KHÔNG cần Docker — chỉ dựng model EF (model building không mở kết nối) và đọc
/// <c>ServiceCollection</c> ở mức MÔ TẢ đăng ký (không build provider, không mở kết nối), nên
/// nằm ở ArchTests chứ không phải IntegrationTests.
/// </summary>
public class SoftDeleteQueryFilterTests
{
    /// <summary>
    /// Assembly chứa entity domain — nguồn để biết TẬP ĐẦY ĐỦ entity kế thừa
    /// <see cref="BaseEntity"/> phải có mặt trong model (khuôn giống
    /// <see cref="EntityEncapsulationTests"/>). Thêm module mới thì thêm assembly Domain của nó
    /// ở ĐÂY; phía model không phải sửa gì — nó tự lấy theo DI thật.
    /// </summary>
    private static readonly Assembly[] DomainAssemblies =
    [
        typeof(BaseEntity).Assembly,
    ];

    /// <summary>
    /// Sửa 2026-08-28 (finding F2). Bản TRƯỚC lấy danh sách entity từ chính
    /// <c>context.Model</c> rồi chỉ <c>Assert.NotEmpty</c> — nghĩa là nó hỏi model "anh có
    /// những ai" rồi kiểm đúng những người đó. Bỏ nguyên một module khỏi model vẫn XANH, vì
    /// entity vắng mặt thì cũng vắng luôn khỏi tập được kiểm. Nay tập kỳ vọng lấy bằng
    /// reflection từ assembly domain — nguồn ĐỘC LẬP với model — nên vắng mặt là đỏ.
    /// </summary>
    [Fact(DisplayName = "Mọi entity kế thừa BaseEntity đều được map vào model (không sót module)")]
    public void EveryBaseEntityDescendant_IsMappedIntoModel()
    {
        using var context = CreateModelOnlyContext();

        var declared = DeclaredBaseEntityTypes();

        // Chặn "pass rỗng" ở chính nguồn kỳ vọng: reflection trả về rỗng thì mọi assert vô nghĩa.
        Assert.NotEmpty(declared);

        var mapped = context.Model.GetEntityTypes().Select(entityType => entityType.ClrType).ToHashSet();

        var unmapped = declared
            .Where(type => !mapped.Contains(type))
            .Select(type => type.FullName!)
            .OrderBy(name => name)
            .ToList();

        Assert.True(unmapped.Count == 0,
            "Entity kế thừa BaseEntity nhưng KHÔNG có trong model EF (thiếu EfConfigurationAssembly " +
            "của module, hoặc thiếu IEntityTypeConfiguration<T>): " + string.Join(", ", unmapped));
    }

    [Fact(DisplayName = "Mọi entity BaseEntity map được đều có query filter soft-delete ĐẶT TÊN")]
    public void EveryBaseEntityDescendant_HasNamedSoftDeleteQueryFilter()
    {
        using var context = CreateModelOnlyContext();

        var filterable = FilterableBaseEntityTypes(context);
        Assert.NotEmpty(filterable);

        var missing = filterable
            .Where(entityType => entityType.GetDeclaredQueryFilters()
                .All(filter => filter.Key != PlatformManagerDbContext.SoftDeleteFilterKey))
            .Select(entityType => entityType.ClrType.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Entity kế thừa BaseEntity nhưng KHÔNG có global query filter khoá " +
            $"'{PlatformManagerDbContext.SoftDeleteFilterKey}': " + string.Join(", ", missing));
    }

    [Fact(DisplayName = "Query filter soft-delete phải lọc theo IsDeleted")]
    public void SoftDeleteFilter_FiltersOn_IsDeleted()
    {
        using var context = CreateModelOnlyContext();

        var wrong = FilterableBaseEntityTypes(context)
            .Where(entityType => !entityType.GetDeclaredQueryFilters().Any(filter =>
                filter.Key == PlatformManagerDbContext.SoftDeleteFilterKey
                && filter.Expression?.ToString().Contains(nameof(BaseEntity.IsDeleted)) == true))
            .Select(entityType => entityType.ClrType.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.True(wrong.Count == 0,
            "Query filter không lọc theo IsDeleted ở: " + string.Join(", ", wrong));
    }

    private static List<Type> DeclaredBaseEntityTypes() =>
        [.. DomainAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && typeof(BaseEntity).IsAssignableFrom(type))];

    /// <summary>
    /// Entity type ĐƯỢC PHÉP mang filter khai tại chính nó. Hai điều kiện loại trừ dưới đây là
    /// bản sao CÓ Ý THỨC của <c>PlatformManagerDbContext.ApplySoftDeleteQueryFilters</c> — thêm
    /// 2026-08-28 (finding F4). Vòng lặp bên đó bỏ qua chúng vì EF CẤM: kiểu con trong hệ thừa
    /// kế đã map (TPH/TPT) nhận filter từ gốc, owned type không truy vấn độc lập được. Bản
    /// TRƯỚC của test không có 2 điều kiện này, nên kiểu TPH hoặc owned type ĐẦU TIÊN thêm vào
    /// dự án sẽ làm test đỏ OAN — đỏ vì code đúng, đúng loại hỏng khiến người ta tắt test đi.
    /// </summary>
    private static List<Microsoft.EntityFrameworkCore.Metadata.IEntityType> FilterableBaseEntityTypes(
        DbContext context) =>
        [.. context.Model.GetEntityTypes()
            .Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => entityType.BaseType is null && !entityType.IsOwned())];

    /// <summary>
    /// Ca (3) khai ở docstring class, khẳng định TƯỜNG MINH: mỗi tầng phải nộp được assembly
    /// configuration của mình vào DI, qua <c>PersistenceAssembly</c> của registrar.
    ///
    /// <para><see cref="EveryBaseEntityDescendant_IsMappedIntoModel"/> cũng đỏ khi ca này hỏng,
    /// nhưng thông báo của nó nói về "entity thiếu trong model" — đọc lên không ai đoán ra nguyên
    /// nhân thật là một dòng <c>AddSingleton</c> bị gỡ. Hai test filter còn lại thì KHÔNG đỏ chút
    /// nào (entity vắng mặt cũng vắng khỏi tập chúng kiểm), nên ca (3) không thể phó thác cho
    /// chúng.</para>
    /// </summary>
    [Fact(DisplayName = "Mỗi tầng trong HostModuleRegistrars nộp EfConfigurationAssembly của mình vào DI")]
    public void EveryModuleRegistration_ContributesItsOwnConfigurationAssembly()
    {
        var registered = ConfigurationAssembliesFromDi();

        Assert.Contains(typeof(PlatformManagerDbContext).Assembly, registered);
        // Thêm tầng nghiệp vụ mới: KHÔNG phải sửa gì ở đây. Assembly của nó đi vào danh sách này
        // ngay khi registrar của nó có mặt trong HostModuleRegistrars — và việc "có mặt" đó do
        // ModuleRegistrarSeamTests canh. Muốn khẳng định đích danh thì thêm 1 dòng Assert.Contains.
        
    }

    /// <summary>
    /// Danh sách <see cref="EfConfigurationAssembly"/> ĐÚNG NHƯ ứng dụng thật sẽ nhận: chạy chính
    /// <c>AddModules()</c> trên danh sách tầng của host rồi ĐỌC MÔ TẢ đăng ký trong
    /// <c>ServiceCollection</c> — không <c>BuildServiceProvider()</c>, nên không có gì được khởi
    /// tạo và không có kết nối nào mở ra. <c>IConfiguration</c> rỗng là đủ: connection string chỉ
    /// đọc bên trong lambda của <c>AddDbContext</c> (chạy lúc resolve, không phải lúc đăng ký).
    ///
    /// <para>Thêm tầng mới thì KHÔNG phải sửa gì ở đây (từ 2026-09-08): danh sách đọc thẳng từ
    /// <c>HostModuleRegistrars</c>, nên tầng nào host nối vào thì phía model tự thấy.</para>
    ///
    /// <para><b>Chuyển thân hàm sang <see cref="EfModelProbe"/> 2026-09-03</b> khi
    /// <see cref="SchemaBoundaryTests"/> cần đúng model này: hai bản dựng model song song là hai
    /// nguồn sự thật, và bản không ai sửa sẽ lặng lẽ lệch khỏi đường mà ứng dụng thật đi — đúng
    /// hình dạng của finding F6 mà docstring class đang kể.</para>
    /// </summary>
    private static List<Assembly> ConfigurationAssembliesFromDi() => EfModelProbe.ConfigurationAssemblies();

    /// <summary>
    /// Chỉ dựng model, KHÔNG kết nối DB — connection string là placeholder hợp lệ về cú pháp,
    /// EF không mở kết nối khi chỉ đọc <c>Model</c>. Tập assembly lấy từ DI THẬT
    /// (<see cref="ConfigurationAssembliesFromDi"/>), KHÔNG liệt kê tay — liệt kê tay là đúng lý
    /// do ca (3) từng lọt (xem docstring class).
    /// </summary>
    private static PlatformManagerDbContext CreateModelOnlyContext() => EfModelProbe.CreateModelOnlyContext();
}
