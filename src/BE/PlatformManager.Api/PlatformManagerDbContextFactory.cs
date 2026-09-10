using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PlatformManager.Api.Modules;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Api;

// ═════════════════════════════════════════════════════════════════════════════════════════════
// 🛑 DỪNG — KHÔNG CHẠY `dotnet ef migrations add` (đóng băng từ 2026-08-29)
// ═════════════════════════════════════════════════════════════════════════════════════════════
//
// ⚠️ ĐỌC TRƯỚC — TIỀN ĐỀ CỦA KHỐI NÀY KHÔNG CÒN ĐÚNG (đo lại 2026-09-04, CHƯA có người dùng
// duyệt gỡ đóng băng nên khối vẫn để nguyên):
//
//   Khối bên dưới nói snapshot "vẫn còn khai đủ 5 entity" của DtiWeekly. Đếm thật hôm nay:
//   `grep -c "Criteria\|business" Persistence/Migrations/*.cs` → 0/0/0. Snapshot hiện hành
//   (PlatformManagerDbContextModelSnapshot.cs) chỉ khai 8 entity, TOÀN BỘ là Core/Identity —
//   không có Criteria*, không có ImportJobs. Lý do: migration 20260831165117_InitialCreate được
//   tạo NGÀY 2026-08-31, tức SAU lần đóng băng, và nó là một baseline làm lại từ đầu. File
//   Persistence/Migrations/sql/0001_initial_baseline.sql cũng chỉ tạo schema "core".
//
//   Nghĩa là mối nguy "migrations add sinh DropTable cho 5 bảng" đã tự tiêu tan cùng lần
//   re-baseline đó — nhưng cảnh báo thì không ai gỡ. Một cảnh báo sai là cảnh báo sẽ bị bỏ qua.
//
//   KHÔNG tự ý xoá khối này: đóng băng là quyết định của người dùng (2026-08-29). Cần người dùng
//   xác nhận. Trước khi tin bất cứ điều gì ở đây, tự chạy lại lệnh grep phía trên.
//
// Lệnh đó HIỆN NAY sẽ sinh ra migration XOÁ 5 BẢNG dữ liệu thật:
//     business.Criteria, business.CriteriaAssessments, business.CriteriaEvidences,
//     business.CriteriaGroups, business.ImportJobs
//
// VÌ SAO: PlatformManagerDbContextModelSnapshot.cs vẫn còn khai đủ 5 entity trên (chúng thuộc
// module DtiWeekly đã bị gỡ khỏi solution), trong khi model thật mà factory này dựng ra chỉ còn
// Core. `migrations add` diff SNAPSHOT ↔ MODEL, thấy 5 bảng "biến mất khỏi model" và kết luận
// đúng theo thiết kế của nó: sinh DropTable cho cả 5.
//
// VÌ SAO NGUY HIỂM ĐẶC BIỆT: lỗi này KHÔNG gây lỗi biên dịch, KHÔNG test nào bắt được, và file
// migration sinh ra trông hoàn toàn bình thường. Nó chỉ lộ ra khi script đã chạy lên database có
// dữ liệu — lúc đó đã mất.
//
// QUYẾT ĐỊNH (người dùng chốt 2026-08-29): ĐÓNG BĂNG. Giữ nguyên 5 bảng trong database, chờ module
// DTI được xây lại. KHÔNG sửa snapshot, KHÔNG xoá migration cũ, KHÔNG "dọn cho sạch".
//
// CẦN ĐỔI SCHEMA CORE TRONG LÚC ĐÓNG BĂNG? Viết tay file .sql delta đặt cạnh các file có sẵn ở
// Core/PlatformManager.Core.Persistence/Migrations/sql/ (thư mục .sql Ở LẠI Core —
// nó là ARTIFACT Corebase ship cho dự án sau; chỉ các file .cs migration mới chuyển sang Api,
// xem MigrationsAssembly bên dưới) rồi thêm tên nó vào
// PostgresFixture.MigrationScripts (Tests/PlatformManager.Core.IntegrationTests/PostgresFixture.cs)
// — integration test dựng schema từ chính các file .sql đó, nên đường này vẫn được kiểm chứng.
//
// GỠ ĐÓNG BĂNG khi nào: module DTI (hoặc module kế thừa 5 bảng trên) quay lại solution VÀ đã được
// khai vào HostModuleRegistrars.Create(). Khi đó model lại chứa đủ 5 entity, diff trở về rỗng,
// và `migrations add` an toàn trở lại. Trình tự đúng: (1) thêm ProjectReference + registrar của
// tầng đó vào HostModuleRegistrars.Create(), (2) build, (3) `dotnet ef migrations add` và ĐỌC file
// sinh ra — phải KHÔNG có DropTable nào, (4) chỉ khi đó mới xoá khối cảnh báo này.
// ═════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Cho phép `dotnet ef` chạy được ngoài runtime DI, KHÔNG chạy Program.cs thật (tránh vô tình
/// kích hoạt seed/kết nối DB thật lúc chỉ đang sinh migration) — chỉ dùng lúc thiết kế
/// (migrations add/script), KHÔNG dùng để chạy app.
///
/// Đặt Ở ĐÂY (Api), KHÔNG đặt ở Core (Persistence/Infrastructure) — vì factory cần biết đủ assembly
/// của MỌI tầng đã đăng ký (để `dotnet ef migrations add` sinh migration đầy đủ, bao gồm cả bảng của
/// tầng nghiệp vụ) trong khi Core.* tuyệt đối không được phép biết tới bất kỳ
/// Modules.*.Infrastructure nào (xem doc/kien-truc-core-module.md §DbContext). Api là composition
/// root duy nhất thấy cả 2 bên nên đây là chỗ đúng cho factory này — khi thêm Module mới thì
/// KHÔNG phải sửa file này chút nào, chỉ thêm registrar vào <see cref="HostModuleRegistrars"/>.
/// </summary>
public class PlatformManagerDbContextFactory : IDesignTimeDbContextFactory<PlatformManagerDbContext>
{
    public PlatformManagerDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=platformmanager_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformManagerDbContext>();

        // Đây là NƠI DUY NHẤT trong repo khai MigrationsAssembly, và đủ: khi một
        // IDesignTimeDbContextFactory tồn tại, `dotnet ef` dùng NÓ và KHÔNG chạy Program.cs — nên
        // mọi lệnh `migrations add` / `migrations script` đều đi qua đúng options dựng ở đây.
        // Runtime (DependencyInjection.AddCoreModule) cố ý KHÔNG khai — xem giải thích tại đó.
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            // __EFMigrationsHistory → schema "core", PHẢI khớp với DependencyInjection.cs
            // (runtime) — xem comment ở đó.
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core");

            // ── Migration thuộc HOST, Core ship .sql (người dùng chốt 2026-09-04) ──────────────
            // Mặc định của EF là "assembly chứa DbContext" = PlatformManager.Core.Persistence.
            // Để mặc định đó thì Migrations/ + ModelSnapshot rơi vào Core, và ModelSnapshot là một
            // file TRẠNG THÁI DÙNG CHUNG: dự án thứ hai thêm bảng nghiệp vụ đầu tiên là EF GHI ĐÈ
            // snapshot của Core để nhét bảng đó vào. Từ đó hai bên cùng sở hữu một file, và lần
            // Core ship bản vá là lần merge hỏng — hoặc mất bảng dự án 2 khỏi snapshot (lượt
            // `migrations add` sau EF tưởng bảng chưa có và SINH LẠI CREATE TABLE lên dữ liệu
            // thật), hoặc mất thay đổi của Core.
            //
            // Nên: mỗi dự án giữ Migrations/ + ModelSnapshot RIÊNG trong project host của mình.
            // Corebase ship baseline .sql (Core/PlatformManager.Core.Persistence/Migrations/sql/) làm artifact
            // schema, KHÔNG ship file .cs migration nào.
            //
            // Vẫn là MỘT DbContext duy nhất ⇒ khoá ngoại core ↔ business còn nguyên. Đó là lý do
            // chọn đường này thay vì tách hai DbContext.
            //
            // Lấy tên assembly qua typeof(...) chứ không gõ chuỗi: đổi tên project thì lỗi biên
            // dịch, không phải lỗi lúc chạy `dotnet ef`.
            //
            // Cưỡng chế bằng máy: PlatformManager.ArchTests/MigrationsLocationTests.cs — file .cs
            // migration nằm dưới src/BE/Core là test đỏ.
            npgsql.MigrationsAssembly(typeof(PlatformManagerDbContextFactory).Assembly.GetName().Name);
        });

        // Danh sách assembly chứa IEntityTypeConfiguration<T> — đọc từ CHÍNH danh sách tầng mà
        // Program.cs dùng (HostModuleRegistrars), KHÔNG chép tay. Chép tay là nguồn sự thật thứ
        // hai cho cùng một câu hỏi, và bản không ai sửa sẽ lệch trong im lặng: `dotnet ef
        // migrations add` khi đó dựng model THIẾU bảng của tầng vắng mặt, rồi sinh migration
        // DropTable cho đúng những bảng đó — đúng tai nạn mà khối cảnh báo đầu file đang kể.
        var configurationAssemblies = HostModuleRegistrars.Create()
            .Select(registrar => new EfConfigurationAssembly(registrar.PersistenceAssembly))
            .ToList();

        return new PlatformManagerDbContext(optionsBuilder.Options, configurationAssemblies);
    }
}
