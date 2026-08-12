using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Entities;

namespace PlatformManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<CriteriaGroup> CriteriaGroups => Set<CriteriaGroup>();
    public DbSet<Criteria> Criteria => Set<Criteria>();
    public DbSet<CriteriaAssessment> CriteriaAssessments => Set<CriteriaAssessment>();
    public DbSet<CriteriaEvidence> CriteriaEvidences => Set<CriteriaEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== AppUser =====
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("AppUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        });

        // ===== CriteriaGroup =====
        modelBuilder.Entity<CriteriaGroup>(e =>
        {
            e.ToTable("CriteriaGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ===== Criteria =====
        modelBuilder.Entity<Criteria>(e =>
        {
            e.ToTable("Criteria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.MaxScore).HasColumnType("decimal(6,2)");
            e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasOne(x => x.Group)
                .WithMany(g => g.Criteria)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ===== CriteriaAssessment =====
        modelBuilder.Entity<CriteriaAssessment>(e =>
        {
            e.ToTable("CriteriaAssessments");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProgressPercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.SelfScore).HasColumnType("decimal(6,2)");
            e.Property(x => x.VerifiedScore).HasColumnType("decimal(6,2)");
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Note);
            e.HasOne(x => x.Criteria)
                .WithMany(c => c.Assessments)
                .HasForeignKey(x => x.CriteriaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            // Unique (CriteriaId, CAST(CreatedAt AS date)) filtered WHERE IsDeleted=false
            // — EF Core HasIndex không diễn đạt được CAST trực tiếp, tạo bằng raw SQL
            // trong migration (xem Migrations/*_AddUniqueCriteriaAssessmentPerDay.cs).
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ===== CriteriaEvidence =====
        modelBuilder.Entity<CriteriaEvidence>(e =>
        {
            e.ToTable("CriteriaEvidences");
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).IsRequired();
            e.HasOne(x => x.CriteriaAssessment)
                .WithMany(a => a.Evidences)
                .HasForeignKey(x => x.CriteriaAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
