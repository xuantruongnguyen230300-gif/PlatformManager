using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Configurations;

public class CriteriaConfiguration : IEntityTypeConfiguration<Criteria>
{
    public void Configure(EntityTypeBuilder<Criteria> builder)
    {
        builder.ToTable("Criteria", schema: "business");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnType("text").IsRequired();
        builder.Property(x => x.MaxScore).HasColumnType("decimal(6,2)");

        builder.Property(x => x.UserCreate).HasMaxLength(50);
        builder.Property(x => x.UserUpdate).HasMaxLength(50);

        builder.HasOne<CriteriaGroup>()
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique filtered — chỉ trong tập chưa xoá mềm (xem
        // spec/danh-muc-dti/business-rules.md mục 1.1).
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDelete\" = false")
            .HasDatabaseName("IX_Criteria_Code_Active");

        builder.HasQueryFilter(x => !x.IsDelete);
    }
}
