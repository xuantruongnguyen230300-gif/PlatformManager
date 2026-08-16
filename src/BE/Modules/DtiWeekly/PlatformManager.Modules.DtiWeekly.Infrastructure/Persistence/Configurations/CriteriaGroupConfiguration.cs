using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Configurations;

public class CriteriaGroupConfiguration : IEntityTypeConfiguration<CriteriaGroup>
{
    public void Configure(EntityTypeBuilder<CriteriaGroup> builder)
    {
        builder.ToTable("CriteriaGroups", schema: "business");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();

        builder.Property(x => x.UserCreate).HasMaxLength(50);
        builder.Property(x => x.UserUpdate).HasMaxLength(50);

        // Unique filtered — chỉ trong tập chưa xoá mềm, cho phép tái dùng Code sau khi xoá
        // (xem spec/danh-muc-dti/business-rules.md mục 1.1).
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDelete\" = false")
            .HasDatabaseName("IX_CriteriaGroups_Code_Active");

        builder.HasQueryFilter(x => !x.IsDelete);
    }
}
