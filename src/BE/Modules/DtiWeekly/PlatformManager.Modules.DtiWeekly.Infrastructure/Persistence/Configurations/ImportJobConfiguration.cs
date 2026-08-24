using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs", schema: "business");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResultJson).HasColumnType("text");
        builder.Property(x => x.ErrorMessage).HasColumnType("text");

        builder.Property(x => x.UserCreate).HasMaxLength(50);
        builder.Property(x => x.UserUpdate).HasMaxLength(50);

        builder.HasIndex(x => x.Status);

        builder.HasQueryFilter(x => !x.IsDelete);
    }
}
