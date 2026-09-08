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

        // 256 chứ KHÔNG phải 50 như CreatedBy/UpdatedBy của BaseEntity: hai cột này lưu
        // ICurrentUser.UserName, mà UserName của Identity dài tới 256. Lấy 50 cho "đồng bộ" nghĩa
        // là một người thao tác có tên đăng nhập dài hơn 50 sẽ làm cả lệnh ghi hỏng ở tầng DB
        // (22001) — đúng thứ cột audit không được phép gây ra.
        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.UpdatedBy).HasMaxLength(256);
    }
}
