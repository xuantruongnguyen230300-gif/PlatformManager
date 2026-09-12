using Microsoft.Extensions.Options;
// Hai using dưới đây chỉ phục vụ nameof trong thông điệp lỗi — cố ý, để tên seam trong thông báo
// gãy lúc biên dịch nếu interface bị đổi tên, thay vì âm thầm chỉ người vận hành đi tìm một cái tên
// không còn tồn tại.
using PlatformManager.Core.Application.Bootstrap;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Business.Persistence;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Api.Common;

/// <summary>
/// Lệnh seed dữ liệu Core chạy <b>một lần</b> rồi thoát — KHÔNG mở cổng, KHÔNG phục vụ request.
///
/// <para><b>Vì sao tách khỏi đường khởi động API</b> (chốt 2026-08-30, xem
/// doc/huong_dan/wiki-core/be/13-core-data-migration.md §"bootstrap Production bằng lệnh riêng"):
/// trước đây toàn bộ <c>CoreSeeder.SeedAsync()</c> nằm sau hàng rào
/// <c>app.Environment.IsDevelopment()</c>. Hệ quả trên một database Production mới: không role,
/// không tài khoản, <c>SysMenus</c> rỗng ⇒ <b>không ai đăng nhập được</b>, và cũng không có đường
/// tạo tài khoản đầu tiên (tạo người dùng là chức năng đòi đăng nhập).</para>
///
/// <para>Vấn đề chưa bao giờ là <c>CoreSeeder</c> — mà là <b>seed lúc app khởi động</b>. Một lệnh
/// riêng tách được hai thứ đó: tiến trình API phục vụ thật <b>không bao giờ</b> ghi dữ liệu seed.</para>
///
/// <para><b>Cách chạy:</b>
/// <code>dotnet run --project src/BE/PlatformManager.Api -- --seed</code>
/// (Production: chạy chính binary đã publish với tham số <c>--seed</c>, sau khi đã áp schema.)</para>
///
/// <para><b>KHÔNG BAO GIỜ tự chạy migration/DDL</b> ở đây — schema áp bằng cách người dùng tự chạy
/// tay file <c>.sql</c> sinh từ <c>dotnet ef migrations script</c>, xem
/// doc/ke-hoach-xay-lai-corebase.md. Lệnh này chỉ ghi DML.</para>
/// </summary>
internal static class SeedCommand
{
    /// <summary>Tham số dòng lệnh kích hoạt chế độ seed.</summary>
    public const string ArgumentName = "--seed";

    /// <summary>Có tham số <c>--seed</c> trên dòng lệnh hay không.</summary>
    public static bool IsRequested(string[] args)
        => args.Any(arg => string.Equals(arg, ArgumentName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Chạy seed rồi trả mã thoát (0 = thành công, 1 = thất bại).
    ///
    /// <para><b>KHÔNG</b> nuốt lỗi như đường khởi động cũ. Đường cũ bọc try/catch rồi chỉ ghi
    /// <c>LogWarning</c> vì app còn phải tiếp tục phục vụ; ở đây seed CHÍNH LÀ việc duy nhất của
    /// tiến trình, nên thất bại phải hiện ra ở mã thoát để người vận hành (hoặc script triển khai)
    /// biết mà dừng lại.</para>
    /// </summary>
    public static async Task<int> RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Phân giải BootstrapOptions TRƯỚC khi ghi bất cứ thứ gì — đây là bước fail-fast thật
            // của chế độ seed. `ValidateOnStart()` khai trong AddCoreModule chỉ chạy khi HOST khởi
            // động, mà tiến trình seed cố ý KHÔNG khởi động host; nên nếu không gọi ở đây, thiếu
            // mật khẩu sẽ chỉ lộ ra giữa chừng — sau khi role và menu đã được ghi.
            _ = scope.ServiceProvider.GetRequiredService<IOptions<BootstrapOptions>>().Value;

            var seeder = scope.ServiceProvider.GetRequiredService<CoreSeeder>();
            await seeder.SeedAsync();

            // Seeder của tầng NGHIỆP VỤ chạy SAU CoreSeeder — thứ tự đó không suy ra được từ
            // HostModuleRegistrars (ngoại lệ đã ghi trong docstring của lớp đó), nên nó phải khai
            // tường minh ở đây. Hôm nay nó seed 6 nhóm chỉ tiêu của
            // spec/danh-muc-dti/business-rules.md §1.6 — danh mục ĐÓNG mà import không tự tạo
            // được, nên phải có sẵn TRƯỚC lần import đầu tiên.
            var businessSeeder = scope.ServiceProvider.GetRequiredService<BusinessSeeder>();
            await businessSeeder.SeedAsync();

            // Cả hai seeder kiểm tra tồn tại trước khi ghi ⇒ chạy lại nhiều lần không nhân đôi
            // dòng nào (nghiệm thu #3 của 13-core-data-migration.md, và nghiệm thu §1.6).
            logger.LogInformation("Seed dữ liệu Core + nghiệp vụ hoàn tất. Tiến trình thoát, KHÔNG mở cổng.");
            return 0;
        }
        catch (Exception ex)
        {
            // Mục (4) có mặt vì ba mục đầu đều nói về MÔI TRƯỜNG (schema, chuỗi kết nối, secret),
            // trong khi một nguyên nhân thật sự xảy ra lại nằm ở CODE của host: CoreSeeder nhận hai
            // seam qua constructor và Core cố ý không có hiện thực mặc định (xem docstring của
            // ICoreBootstrapAccountSource), nên quên một dòng AddSingleton là DI ném ngay tại
            // GetRequiredService<CoreSeeder>() — trước khi chạm database. Thiếu mục này, người vận
            // hành đi kiểm cả ba mục trên, thấy cả ba đều đúng, và không còn chỗ nào để nhìn tiếp.
            //
            // Tên seam viết bằng nameof để thông điệp không nói sai khi interface bị đổi tên — đúng
            // khuôn các thông báo lỗi trong CoreSeeder.
            logger.LogCritical(
                ex,
                "Seed dữ liệu THẤT BẠI. Kiểm tra: (1) đã chạy tay file schema .sql lên Postgres chưa, "
                + "(2) ConnectionStrings:Default trỏ đúng database chưa, (3) đã đặt Bootstrap:SuperAdminPassword "
                + "và Bootstrap:AdminPassword chưa, (4) nếu lỗi kèm theo là 'Unable to resolve service for type' "
                + $"kèm {nameof(ICoreMenuSeedSource)} hoặc {nameof(ICoreBootstrapAccountSource)} thì KHÔNG phải lỗi "
                + "database/cấu hình: Program.cs của host thiếu dòng "
                + $"builder.Services.AddSingleton<{nameof(ICoreMenuSeedSource)}, ...>() hoặc "
                + $"AddSingleton<{nameof(ICoreBootstrapAccountSource)}, ...>() — hai dòng này đứng ngay sau "
                + "AddCoreModule(...).");
            return 1;
        }
    }
}
