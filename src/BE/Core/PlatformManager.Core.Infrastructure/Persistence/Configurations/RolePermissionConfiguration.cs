using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence.Configurations;

/// <summary>PK ghép (RoleId, ResourceKey) — thuần bảng nối, không BaseEntity, xoá dòng = thu hồi
/// quyền (giống SysMenuRoleConfiguration).</summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.Property(x => x.ResourceKey).HasMaxLength(100);

        builder.HasKey(x => new { x.RoleId, x.ResourceKey });

        // Q2 (.claude/rules/performance.md) — PK ghép dẫn đầu bằng "RoleId" nên KHÔNG seek được
        // cho predicate nóng nhất của hệ thống: RequirePermissionFilter lọc trước hết theo
        // ResourceKey (= key của endpoint) rồi mới ghép RoleId. Index này dẫn đầu ĐÚNG cột được
        // lọc, và phủ luôn cột join → cho phép index-only scan, không phải chạm heap.
        // Lưu ý khi đọc EXPLAIN: ở cỡ dữ liệu hiện tại (6 dòng) Postgres vẫn chọn Seq Scan vì
        // bảng chỉ có 1 page — đó là lựa chọn ĐÚNG của planner, không phải index vô dụng. Index
        // có giá trị khi số (role × resource key) tăng lên; chi phí giữ nó gần bằng 0.
        builder.HasIndex(x => new { x.ResourceKey, x.RoleId })
            .HasDatabaseName("IX_RolePermissions_ResourceKey_RoleId");

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
