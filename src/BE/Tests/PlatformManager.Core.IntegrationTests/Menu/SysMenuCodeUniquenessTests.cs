using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlatformManager.Core.Domain.Entities;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Menu;

/// <summary>
/// <c>IX_SysMenus_Code</c> phải là unique <b>PARTIAL</b> (<c>WHERE "IsDeleted" = false</c>) —
/// sửa 2026-08-28.
///
/// <para>(Sửa chú thích 2026-09-01: trước đây dòng trên trỏ "migration 0008". Lịch sử migration đã
/// được baseline lại 2026-08-31 — sáu file 0003–0008 gộp vào <c>sql/0001_initial_baseline.sql</c>,
/// xem <c>PostgresFixture.MigrationScripts</c> và doc/cau-truc-database.md §5.2. Bản thân index thì
/// không đổi.)</para>
///
/// Vấn đề gốc: unique TOÀN BẢNG đếm cả dòng đã xoá mềm, nên "xoá menu rồi tạo lại đúng mã cũ" bị
/// Postgres chặn trong khi màn hình không còn menu nào mang mã ấy. Không có đường đi vòng nào cho
/// người dùng ngoài xoá cứng dòng trong DB.
///
/// Vì sao phải là INTEGRATION test chứ không phải ArchTest: thứ đang kiểm là hành vi của chính
/// index trong Postgres. Kiểm trên model EF chỉ khẳng định được chuỗi <c>HasFilter</c> đã được
/// khai — không khẳng định được rằng file .sql người dùng chạy tay đã thật sự tạo ra index đó
/// (đúng lý do PostgresFixture dựng schema từ .sql thay vì từ model EF).
/// </summary>
[Collection(PostgresCollection.Name)]
public class SysMenuCodeUniquenessTests(PostgresFixture fixture)
{
    private static string NewCode() => $"it-code-{Guid.NewGuid():N}";

    [Fact(DisplayName = "Xoá mềm menu rồi tạo lại CÙNG Code → THÀNH CÔNG")]
    public async Task SoftDeletedCode_CanBeReused()
    {
        var code = NewCode();

        Guid firstId;
        await using (var db = fixture.CreateDbContext())
        {
            var first = SysMenu.Create(code, "Menu gốc", "/it-goc", null, null, 1);
            db.SysMenus.Add(first);
            await db.SaveChangesAsync();
            firstId = first.Id;
        }

        await using (var db = fixture.CreateDbContext())
        {
            var first = await db.SysMenus.SingleAsync(menu => menu.Id == firstId);
            first.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        Guid secondId;
        await using (var db = fixture.CreateDbContext())
        {
            var again = SysMenu.Create(code, "Menu tạo lại", "/it-tao-lai", null, null, 2);
            db.SysMenus.Add(again);

            // KHÔNG bọc try/catch: nếu index còn thiếu filter thì DbUpdateException ném ra ở
            // đây chính là kết quả cần thấy, kèm nguyên thông báo của Postgres nói rõ index nào
            // đã chặn. Nuốt nó đi rồi Assert.False sẽ mất đúng thông tin đó.
            await db.SaveChangesAsync();
            secondId = again.Id;
        }

        await using (var verify = fixture.CreateDbContext())
        {
            // Dòng cũ vẫn nằm trong bảng — nếu nó bị xoá CỨNG ở đâu đó thì test trên vẫn xanh
            // nhưng vì lý do hoàn toàn khác, và index có filter hay không cũng không còn quan hệ.
            Assert.Equal(2, await verify.SysMenus.IgnoreQueryFilters().CountAsync(menu => menu.Code == code));

            // Qua query filter chỉ còn thấy bản mới.
            var visible = await verify.SysMenus.SingleAsync(menu => menu.Code == code);
            Assert.Equal(secondId, visible.Id);
        }
    }

    /// <summary>
    /// Chiều NGƯỢC — bắt buộc phải có. Thiếu ca này thì test trên vẫn xanh kể cả khi ai đó xoá
    /// hẳn index unique đi (tái dùng mã lúc nào cũng "thành công"), tức là 0008 sẽ được coi là
    /// đúng ngay cả khi nó phá mất ràng buộc mà nó chỉ định nới lỏng một phần.
    /// </summary>
    [Fact(DisplayName = "Hai menu CÙNG Code, cùng CHƯA xoá → vẫn bị chặn (23505)")]
    public async Task ActiveDuplicateCode_IsStillRejected()
    {
        var code = NewCode();

        await using (var db = fixture.CreateDbContext())
        {
            db.SysMenus.Add(SysMenu.Create(code, "Menu thứ nhất", "/it-1", null, null, 1));
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateDbContext())
        {
            db.SysMenus.Add(SysMenu.Create(code, "Menu thứ hai", "/it-2", null, null, 2));

            var ex = await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());

            // Khẳng định đích danh "vi phạm unique" (SQLSTATE 23505) chứ không chỉ "có ném lỗi":
            // một lỗi NOT NULL hay FK cũng là DbUpdateException và sẽ làm test xanh nhầm.
            var postgres = Assert.IsType<PostgresException>(ex.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("IX_SysMenus_Code", postgres.ConstraintName);
        }
    }
}
