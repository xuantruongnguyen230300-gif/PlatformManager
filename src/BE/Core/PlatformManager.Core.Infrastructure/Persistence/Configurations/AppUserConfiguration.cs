using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformManager.Core.Infrastructure.Identity;

namespace PlatformManager.Core.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Guid PK luôn client-generated (EntityId.New()/Guid.NewGuid() gán tay TRƯỚC khi
        // AddAsync) — ValueGeneratedNever tránh EF hiểu nhầm key-đã-set = "đã tồn tại" khi
        // entity được thêm qua graph fixup thay vì DbSet.Add trực tiếp.
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.MustChangePassword).HasDefaultValue(true);
    }
}
