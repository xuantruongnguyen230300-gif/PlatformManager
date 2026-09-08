using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Nơi Identity + entity Core (SysMenu/SysMenuRole) + soft-delete hội tụ — DÙNG CHUNG cho mọi
/// module (1 Postgres duy nhất, xem doc/kien-truc-core-module.md §DbContext). Kế thừa
/// IdentityDbContext&lt;AppUser,AppRole,Guid&gt; — 7 bảng chuẩn Identity tự sinh qua migration,
/// không tự vẽ tay (xem doc/huong_dan/quy-uoc/be-api-controller.md §Auth/Permission).
///
/// KHÔNG khai DbSet&lt;T&gt; cho entity của bất kỳ Module nào (Core.Infrastructure không được
/// ProjectReference tới Modules.*.Domain) — Module tự gọi Set&lt;T&gt;() trực tiếp trên
/// PlatformManagerDbContext trong repository của mình (vẫn cùng 1 instance DbContext, chỉ khác
/// cách truy cập). Model của entity thuộc tầng nào do CHÍNH tầng đó khai, qua
/// <c>IModuleRegistrar.PersistenceAssembly</c> → <see cref="EfConfigurationAssembly"/> →
/// danh sách dưới đây (xem Core.Infrastructure/Modules/ModuleRegistrationExtensions.cs).
///
/// Soft-delete: xem <see cref="ApplySoftDeleteQueryFilters"/> — khai MỘT CHỖ, không khai lẻ ở
/// từng IEntityTypeConfiguration&lt;T&gt;.
/// </summary>
public class PlatformManagerDbContext(
    DbContextOptions<PlatformManagerDbContext> options,
    IEnumerable<EfConfigurationAssembly> configurationAssemblies)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    /// <summary>Khoá của global query filter soft-delete. Là hằng số công khai vì ArchTest tra
    /// đúng khoá này — hard-code chuỗi ở hai nơi thì đổi tên một nơi là test mù im lặng.</summary>
    public const string SoftDeleteFilterKey = "SoftDelete";

    /// <summary>
    /// Schema mặc định của mọi bảng Core. Là hằng số công khai vì ArchTest
    /// <c>SchemaBoundaryTests</c> đối chiếu đúng giá trị này — chuỗi gõ ở hai nơi thì đổi một nơi
    /// là test mù im lặng (cùng lý do đã ghi cho <see cref="SoftDeleteFilterKey"/>).
    /// </summary>
    public const string CoreSchema = "core";

    /// <summary>
    /// Schema BẮT BUỘC của mọi bảng nghiệp vụ. Hai schema nằm trong CÙNG MỘT database — quyết định
    /// người dùng 2026-09-03 giữ nguyên hiện trạng, lý do: còn khoá ngoại và giao dịch chung giữa
    /// hai bên nên tách database sẽ mất cả hai thứ đó.
    ///
    /// <para>Hằng số này chưa có nơi dùng trong mã sản phẩm (khối <c>Business.*</c> chưa dựng lại).
    /// Nó tồn tại để cấu hình entity nghiệp vụ ĐẦU TIÊN viết
    /// <c>ToTable("…", PlatformManagerDbContext.BusinessSchema)</c> thay vì gõ lại chuỗi — và để
    /// ArchTest lấy kỳ vọng từ cùng một nguồn với mã sản phẩm.</para>
    /// </summary>
    public const string BusinessSchema = "business";

    private readonly IReadOnlyList<System.Reflection.Assembly> _configurationAssemblies =
        [.. configurationAssemblies.Select(x => x.Assembly).Distinct()];

    public DbSet<SysMenu> SysMenus => Set<SysMenu>();
    public DbSet<SysMenuRole> SysMenuRoles => Set<SysMenuRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mọi entity KHÔNG khai schema riêng (Identity 7 bảng + SysMenu/SysMenuRole) tự vào
        // "core". Entity nghiệp vụ (DTI Weekly...) khai tường minh schema "business" ngay tại
        // ToTable() trong IEntityTypeConfiguration<T> của mình — ranh giới schema Postgres khớp
        // đúng ranh giới Core/Business đã có ở tầng code (xem doc/kien-truc-core-module.md).
        modelBuilder.HasDefaultSchema(CoreSchema);

        base.OnModelCreating(modelBuilder); // Identity map trước — configuration của ta override phần cần thiết (vd ValueGeneratedNever)

        // Mỗi assembly (Core.Infrastructure của chính nó + assembly của từng Module đã đăng ký)
        // tự sở hữu IEntityTypeConfiguration<T> của entity mình — KHÔNG hardcode tên assembly
        // Module cụ thể ở đây (xem doc/kien-truc-core-module.md §DbContext).
        foreach (var assembly in _configurationAssemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Soft-delete khai MỘT CHỖ cho MỌI entity kế thừa <see cref="BaseEntity"/> — entity mới
    /// được bảo vệ ngay khi vừa thêm, không phụ thuộc việc người viết có nhớ khai
    /// <c>HasQueryFilter</c> trong <c>IEntityTypeConfiguration&lt;T&gt;</c> hay không. Khai lẻ
    /// từng config (cách làm TRƯỚC 2026-08-27) thì entity nào quên khai là mất filter im lặng —
    /// không lỗi biên dịch, không test nào báo, dữ liệu đã xoá mềm lọt thẳng ra API. Hợp đồng:
    /// doc/huong_dan/quy-uoc/be-entity-domain.md §"Base entity".
    ///
    /// <para><b>PHẢI gọi SAU <c>ApplyConfigurationsFromAssembly</c></b> vì <c>Model.GetEntityTypes()</c>
    /// chưa thấy entity nào cho tới khi config khai <c>ToTable</c>/<c>HasKey</c> — chạy trước thì
    /// vòng lặp chạy trên tập rỗng và KHÔNG entity nào nhận được filter.</para>
    ///
    /// <para><b>Filter ĐẶT TÊN (<see cref="SoftDeleteFilterKey"/>), không phải ẩn danh</b> — sửa
    /// 2026-08-28 theo finding F3. Overload <c>HasQueryFilter(LambdaExpression)</c> dùng TRƯỚC đó
    /// ghi vào filter ẩn danh duy nhất của entity, mà vòng lặp này chạy SAU MỌI
    /// <c>IEntityTypeConfiguration&lt;T&gt;</c> — nên mọi filter nghiệp vụ khai ẩn danh trong config
    /// (vd "chỉ thấy bản ghi thuộc đơn vị của mình") sẽ bị GHI ĐÈ IM LẶNG: không lỗi biên dịch,
    /// không test nào báo, dữ liệu ngoài phạm vi lọt thẳng ra API. Với khoá tên, soft-delete và
    /// filter nghiệp vụ CÙNG TỒN TẠI (EF nối bằng AND) thay vì thay thế nhau.</para>
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // BaseType != null = kiểu con trong 1 hệ thừa kế đã map (TPH/TPT) — filter khai ở
            // gốc đã phủ; khai lại ở kiểu con là lỗi cấu hình EF. IsOwned() = owned type, không
            // truy vấn độc lập được nên không nhận filter riêng.
            if (entityType.BaseType is not null || entityType.IsOwned())
                continue;

            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            // e => !e.IsDeleted — dựng bằng Expression vì ClrType chỉ biết lúc chạy.
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(SoftDeleteFilterKey, Expression.Lambda(body, parameter));
        }
    }
}
