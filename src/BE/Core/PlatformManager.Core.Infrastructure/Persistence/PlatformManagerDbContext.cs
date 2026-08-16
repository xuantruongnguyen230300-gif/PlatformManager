using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence;

/// <summary>
/// Nơi Identity + entity Core (SysMenu/SysMenuRole) + soft-delete hội tụ — DÙNG CHUNG cho mọi
/// module (1 Postgres duy nhất, xem doc/kien-truc-core-module.md §DbContext). Kế thừa
/// IdentityDbContext&lt;AppUser,AppRole,Guid&gt; — 7 bảng chuẩn Identity tự sinh qua migration,
/// không tự vẽ tay (xem .claude/rules/api-controller.md §Auth/Permission).
///
/// KHÔNG khai DbSet&lt;T&gt; cho entity của bất kỳ Module nào (Core.Infrastructure không được
/// ProjectReference tới Modules.*.Domain) — Module tự gọi Set&lt;T&gt;() trực tiếp trên
/// PlatformManagerDbContext trong repository của mình (vẫn cùng 1 instance DbContext, chỉ khác
/// cách truy cập). Model của entity thuộc Module nào do CHÍNH Module đó đăng ký qua
/// <see cref="EfConfigurationAssembly"/> (mỗi Module tự thêm assembly của mình vào DI khi gọi
/// AddXxxModule(), xem Core.Infrastructure/DependencyInjection.cs và
/// Modules.DtiWeekly.Infrastructure/DependencyInjection.cs).
/// </summary>
public class PlatformManagerDbContext(
    DbContextOptions<PlatformManagerDbContext> options,
    IEnumerable<EfConfigurationAssembly> configurationAssemblies)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    private readonly IReadOnlyList<System.Reflection.Assembly> _configurationAssemblies =
        [.. configurationAssemblies.Select(x => x.Assembly).Distinct()];

    public DbSet<SysMenu> SysMenus => Set<SysMenu>();
    public DbSet<SysMenuRole> SysMenuRoles => Set<SysMenuRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mọi entity KHÔNG khai schema riêng (Identity 7 bảng + SysMenu/SysMenuRole) tự vào
        // "core". Entity nghiệp vụ (DTI Weekly...) khai tường minh schema "business" ngay tại
        // ToTable() trong IEntityTypeConfiguration<T> của mình — ranh giới schema Postgres khớp
        // đúng ranh giới Core/Business đã có ở tầng code (xem doc/kien-truc-core-module.md).
        modelBuilder.HasDefaultSchema("core");

        base.OnModelCreating(modelBuilder); // Identity map trước — configuration của ta override phần cần thiết (vd ValueGeneratedNever)

        // Mỗi assembly (Core.Infrastructure của chính nó + assembly của từng Module đã đăng ký)
        // tự sở hữu IEntityTypeConfiguration<T> của entity mình — KHÔNG hardcode tên assembly
        // Module cụ thể ở đây (xem doc/kien-truc-core-module.md §DbContext).
        foreach (var assembly in _configurationAssemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
