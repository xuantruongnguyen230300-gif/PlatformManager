using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Configurations;

public class CriteriaEvidenceConfiguration : IEntityTypeConfiguration<CriteriaEvidence>
{
    public void Configure(EntityTypeBuilder<CriteriaEvidence> builder)
    {
        builder.ToTable("CriteriaEvidences", schema: "business");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.OrderIndex).IsRequired();

        builder.Property(x => x.UserCreate).HasMaxLength(50);
        builder.Property(x => x.UserUpdate).HasMaxLength(50);

        builder.HasOne<CriteriaAssessment>()
            .WithMany()
            .HasForeignKey(x => x.CriteriaAssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDelete);
    }
}
