using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence.Configurations;

/// <summary>PK ghép (SysMenuId, RoleId) — thuần bảng nối, không BaseEntity, xoá dòng = thu
/// hồi quyền (giống AspNetUserRoles).</summary>
public class SysMenuRoleConfiguration : IEntityTypeConfiguration<SysMenuRole>
{
    public void Configure(EntityTypeBuilder<SysMenuRole> builder)
    {
        builder.ToTable("SysMenuRoles");

        builder.HasKey(x => new { x.SysMenuId, x.RoleId });

        builder.HasOne<SysMenu>()
            .WithMany()
            .HasForeignKey(x => x.SysMenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
