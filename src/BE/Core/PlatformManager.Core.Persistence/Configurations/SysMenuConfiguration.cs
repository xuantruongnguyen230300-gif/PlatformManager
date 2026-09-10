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

        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);

        builder.HasOne<SysMenu>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique FILTERED — chỉ trong tập chưa xoá mềm, cùng khuôn với
        // IX_Criteria_Code_Active/IX_CriteriaGroups_Code_Active. Thêm filter 2026-08-28.
        //
        // ⚠️ THÌ DỰ PHÒNG, KHÔNG PHẢI SỰ CỐ ĐÃ XẢY RA. Hôm nay KHÔNG có controller nào ghi
        // SysMenu (không có MenusController; SysMenuRepository chỉ đọc) — đường ghi duy nhất là
        // CoreSeeder, nên chưa người dùng cuối nào gặp cảnh "mã đã tồn tại". Cái được mua là:
        // KHI có màn hình quản trị menu, unique toàn bảng sẽ chặn việc tạo lại đúng mã vừa xoá
        // mềm — dòng đã xoá vẫn chiếm chỗ trong index — trong khi màn hình không còn menu nào
        // mang mã ấy, và không có đường đi vòng nào ngoài xoá cứng dòng trong DB.
        //
        // Người hưởng lợi HÔM NAY là CoreSeeder, nhưng CÓ ĐIỀU KIỆN: filter này nới ràng buộc
        // ra, nên nó gỡ luôn tấm lưới 23505 vốn biến "seeder không thấy dòng đã xoá mềm" thành
        // một lỗi ồn ào. Vì vậy 0008 phải đi CÙNG bản sửa CoreSeeder.UpsertMenuAsync (đọc bằng
        // IgnoreQueryFilters + hồi sinh dòng cũ); thiếu bản sửa đó thì chính index này biến một
        // lỗi to tiếng thành menu nhân đôi im lặng. Xem CoreSeeder.UpsertMenuAsync và
        // Tests/PlatformManager.Core.IntegrationTests/Menu/CoreSeederSoftDeletedMenuTests.cs.
        //
        // Tên index GIỮ NGUYÊN "IX_SysMenus_Code" (không đổi thành "_Active" như 2 index kia) —
        // tên này đã có trong doc/cau-truc-database.md và trong DB thật; đổi tên chỉ để cho đẹp
        // sẽ tạo thêm một bước rename phải đồng bộ tay ở mọi môi trường mà không mua được gì.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_SysMenus_Code");
    }
}
