using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Infrastructure.Persistence.Configurations;

public class SysMenuConfiguration : IEntityTypeConfiguration<SysMenu>
{
    public void Configure(EntityTypeBuilder<SysMenu> builder)
    {
        builder.ToTable("SysMenus");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Route).HasMaxLength(200);
        builder.Property(x => x.Icon).HasMaxLength(50);

        builder.Property(x => x.UserCreate).HasMaxLength(50);
        builder.Property(x => x.UserUpdate).HasMaxLength(50);

        builder.HasOne<SysMenu>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_SysMenus_Code");

        builder.HasQueryFilter(x => !x.IsDelete);
    }
}
