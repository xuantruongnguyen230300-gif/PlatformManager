using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Application.Storage;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Infrastructure.Common;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Import;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Core.Infrastructure.Persistence.Interceptors;
using PlatformManager.Core.Infrastructure.Persistence.Repositories;
using PlatformManager.Core.Infrastructure.Storage;

namespace PlatformManager.Core.Infrastructure;

/// <summary>Composition duy nhất của Core module. KHÔNG gọi thẳng từ host: đường vào chính
/// thức là <see cref="Modules.CoreModuleRegistrar"/> đi qua
/// <c>ModuleRegistrationExtensions.AddModules</c> — cùng một đường với mọi tầng nghiệp vụ, và
/// Core luôn đứng ĐẦU danh sách registrar (tầng nghiệp vụ có thể cần role/user Core đã tồn tại
/// lúc seed — xem doc/kien-truc-core-module.md §IModuleRegistrar).</summary>
public static class DependencyInjection
{
    /// <param name="requireBootstrapOptions">
    /// <c>true</c> ⇒ <see cref="BootstrapOptions"/> bị bắt buộc và validate NGAY LÚC KHỞI ĐỘNG.
    ///
    /// <para>Chỉ đường chạy <b>lệnh seed</b> (<c>--seed</c>) mới truyền <c>true</c>. Trước
    /// 2026-08-31 <c>ValidateOnStart()</c> là KHÔNG ĐIỀU KIỆN, nên tiến trình API ở Production bắt
    /// buộc phải có <c>Bootstrap__SuperAdminPassword</c>/<c>Bootstrap__AdminPassword</c> mới khởi
    /// động nổi — hai secret mà nó không bao giờ đọc tới. Người vận hành đặt secret, thấy app lên,
    /// và kết luận nhầm rằng đã bootstrap xong (xem
    /// doc/huong_dan/wiki-core/be/13-core-data-migration.md §"bootstrap Production bằng lệnh riêng"
    /// quyết định 2).</para>
    ///
    /// <para>Mặc định <c>false</c> để mọi caller khác (test kiến trúc dựng
    /// <c>ServiceCollection</c> chỉ để đọc mô tả đăng ký) không phải khai gì thêm.</para>
    /// </param>
    public static IServiceCollection AddCoreModule(
        this IServiceCollection services,
        IConfiguration configuration,
        bool requireBootstrapOptions = false)
    {
        services.AddCoreApplication();

        services.AddScoped<AuditInterceptor>();

        // ── EfConfigurationAssembly KHÔNG đăng ký ở đây nữa (chuyển 2026-09-08) ──────────────
        // Trước đó dòng `services.AddSingleton(new EfConfigurationAssembly(...))` đứng ngay đây.
        // Nay assembly Persistence của Core đi vào DI qua ĐÚNG đường mà mọi tầng khác đi:
        // CoreModuleRegistrar.PersistenceAssembly → ModuleRegistrationExtensions.AddModules.
        // Giữ cả hai đường thì Core có một lối riêng còn tầng nghiệp vụ có lối khác — và lối
        // của tầng nghiệp vụ sẽ không được chạy lần nào cho tới khi tầng đó tồn tại.
        //
        // ⚠️ HỆ QUẢ: gọi thẳng AddCoreModule() mà không đi qua AddModules() thì model EF KHÔNG
        // có cấu hình entity nào. Đường vào chính thức là AddModules(configuration, registrars).

        services.AddDbContext<PlatformManagerDbContext>((sp, options) =>
        {
            // __EFMigrationsHistory đặt trong schema "core" (không phải "public" mặc định của
            // Postgres) — cùng chủ trương với HasDefaultSchema("core") ở
            // PlatformManagerDbContext.OnModelCreating: "public" không chứa bảng nào của app.
            // PHẢI khớp với PlatformManagerDbContextFactory.cs (design-time, sinh migration
            // script) — lệch nhau sẽ khiến runtime và migration script tra __EFMigrationsHistory
            // ở 2 schema khác nhau.
            options.UseNpgsql(configuration.GetConnectionString("Default"), npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core");

                // ── CỐ Ý KHÔNG khai MigrationsAssembly ở đây (người dùng chốt 2026-09-04) ──────
                // Chốt "migration thuộc host, Core ship .sql" đặt Migrations/ + ModelSnapshot vào
                // project host (PlatformManager.Api). Khai MigrationsAssembly ở ĐÂY đồng nghĩa
                // Core.Infrastructure phải gọi tên assembly host — hoặc bằng chuỗi
                // "PlatformManager.Api", hoặc bằng một tham số mới xuyên qua AddCoreModule. Cả hai
                // đều là thứ Corebase mang theo sang dự án thứ hai rồi trỏ SAI, và không ArchTest
                // nào hôm nay bắt được (CoreMustNotKnowBusinessNameTests chỉ cấm "business"/
                // "dtiweekly", không cấm tên host).
                //
                // Bỏ trống là ĐỦ và ĐÚNG vì runtime KHÔNG chạy migration: không chỗ nào gọi
                // Database.Migrate()/GetPendingMigrations() (tự kiểm:
                // `grep -rn "Database.Migrate\|GetPendingMigrations" src/BE --include=*.cs`),
                // `dotnet ef database update` bị settings.json chặn bằng máy, và schema được áp
                // bằng file .sql chạy tay. Nơi DUY NHẤT cần MigrationsAssembly là design-time —
                // PlatformManagerDbContextFactory (Api), nơi `dotnet ef` thật sự đọc options.
                //
                // Nếu sau này CÓ đường ghi chạy migration lúc chạy: đừng gõ tên host vào đây —
                // thêm một seam để host tự nộp assembly của nó (khuôn có sẵn:
                // EfConfigurationAssembly ngay dưới đây).

                // Thử lại khi lỗi TẠM THỜI (chốt 2026-08-31, xem
                // doc/huong_dan/quy-uoc/be-performance.md §"Cấu hình kết nối DB"). Không có nó thì
                // một nhịp chớp mạng giữa app và Postgres thành 500 cho người dùng — chạy sau proxy
                // và trên máy dùng chung thì chuyện này xảy ra thật.
                //
                // ⚠️ BẪY: chiến lược thử lại KHÔNG bọc được transaction do CODE TỰ MỞ
                // (BeginTransaction/BeginTransactionAsync) và sẽ ném InvalidOperationException
                // "The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not
                // support user-initiated transactions". Nó KHÔNG lộ ra lúc biên dịch — chỉ nổ lúc
                // chạy đúng đường ghi đó. Mọi đường ghi dùng transaction tường minh (vd
                // Identity/UserAdminService.UpdateAsync) PHẢI bọc bằng execution strategy thủ công:
                //     var strategy = dbContext.Database.CreateExecutionStrategy();
                //     await strategy.ExecuteAsync(async () => { ...BeginTransactionAsync... });
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Khai TƯỜNG MINH thay vì để mặc định 30 giây của Npgsql: một truy vấn chậm giữ một
                // kết nối suốt 30 giây, dồn lại thì cạn pool và app đứng cho MỌI người. 30 giây ở
                // đây là con số đã cân nhắc, không phải mặc định trôi vào — hạ xuống khi có số đo
                // thật về p99 của truy vấn nặng nhất.
                npgsql.CommandTimeout(30);
            });
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                // Chính sách mật khẩu — chốt 2026-08-31, xem
                // doc/huong_dan/wiki-core/be/09-security-beyond-auth.md §"Chính sách mật khẩu".
                // Bối cảnh đổi: hệ thống nay công khai ra Internet, nên chú thích "[ĐƠN GIẢN HOÁ]
                // demo/nội bộ" của bản trước không còn đúng tiền đề.
                //
                // 12 ký tự đưa không gian mật khẩu lên ~9×10^16 — ngoài tầm dò vét. (NIST SP
                // 800-63B Rev 4 đòi 15 cho hệ thống chỉ dùng mật khẩu; 12 là LỆCH CHUẨN CÓ CÂN
                // NHẮC, người dùng chốt 2026-08-31 sau khi biết dữ kiện đó — đừng mở lại thảo luận
                // này từ đầu.)
                options.Password.RequiredLength = 12;

                // ⚠️ Bốn luật thành phần dưới đây để `false` là CHỦ ĐÍCH, KHÔNG phải bỏ sót — đừng
                // "sửa" thành true. NIST SP 800-63B: độ dài quan trọng hơn độ phức tạp; bắt buộc
                // thành phần chỉ đẻ ra "Matkhau1!" — dễ đoán hơn một cụm 12 chữ thường.
                // Thứ THAY THẾ cho luật thành phần là bộ lọc mật khẩu phổ biến ngay dưới khối này.
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Email chỉ là quy ước đặt tên, không bắt buộc duy nhất/không bắt buộc có —
                // xem doc/ke-hoach-xay-lai-corebase.md.
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<PlatformManagerDbContext>()
            .AddDefaultTokenProviders()
            // Từ chối mật khẩu nằm trong danh sách 10.000 mật khẩu phổ biến nhất — đây là thứ NIST
            // khuyến nghị THAY CHO luật thành phần, và nó đóng đúng đường tấn công thật: không ai
            // dò vét 12 ký tự, người ta thử 10.000 mật khẩu phổ biến.
            //
            // Gói CommonPasswordsValidator (Andrew Lock, MIT) nhúng sẵn danh sách trong assembly —
            // hoạt động HOÀN TOÀN OFFLINE, không gọi API nào ra Internet lúc chạy (một dịch vụ
            // ngoài nằm trên đường đặt mật khẩu là thêm một điểm hỏng và một đường rò dữ liệu).
            // Đây là một IPasswordValidator<AppUser> bình thường, cộng dồn với PasswordOptions ở
            // trên chứ không thay thế.
            .AddTop10000PasswordValidator<AppUser>();

        // Mật khẩu bootstrap của tài khoản SuperAdmin/Admin do CoreSeeder tạo — KHÔNG hardcode,
        // fail-fast nếu thiếu cấu hình (xem BootstrapOptions.cs cho lý do đầy đủ + nơi đặt giá
        // trị theo từng môi trường, và be-architecture.md §"Cấu hình — fail-fast validation").
        var bootstrapOptions = services.AddOptions<BootstrapOptions>()
            .Bind(configuration.GetSection(BootstrapOptions.SectionName))
            .ValidateDataAnnotations();

        // ValidateOnStart CHỈ trên đường chạy lệnh seed (xem tham số requireBootstrapOptions).
        // Hai mật khẩu quản trị chỉ dùng ĐÚNG MỘT LẦN trong đời hệ thống — không có lý do gì để
        // chúng nằm thường trực trên máy production sau đó, và bắt tiến trình API đòi chúng tạo ra
        // hiểu lầm "đặt được secret nghĩa là đã bootstrap".
        if (requireBootstrapOptions)
        {
            bootstrapOptions.ValidateOnStart();
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<ISysMenuRepository, SysMenuRepository>();
        services.AddScoped<ISysMenuRoleRepository, SysMenuRoleRepository>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IUserLookupService, UserLookupService>();

        services.AddScoped<CoreSeeder>();

        // Import engine — 3 reader đăng ký thành MỘT TẬP, selector hỏi CanRead từng cái. Đăng ký
        // qua tập chứ không qua switch theo enum định dạng là điều kiện để thêm định dạng thứ tư
        // mà không sửa dòng nào ở Core (doc/huong_dan/wiki-core/be/15-import-export.md §2).
        // Singleton: cả ba đều không trạng thái, không giữ tài nguyên nào giữa hai lần đọc.
        services.AddSingleton<IImportFileReader, CsvImportFileReader>();
        services.AddSingleton<IImportFileReader, XlsxImportFileReader>();
        services.AddSingleton<IImportFileReader, XlsImportFileReader>();
        services.AddSingleton<IImportFileReaderSelector, ImportFileReaderSelector>();

        // Trần dung lượng file import. ValidateOnStart KHÔNG điều kiện: section vắng mặt thì mặc
        // định 10 MB vẫn hợp lệ, nên thứ duy nhất bị chặn ở đây là một giá trị SAI đã khai (0/âm) —
        // và một trần bằng 0 từ chối mọi file mà không gợi được về nguyên nhân.
        services.AddOptions<ImportOptions>()
            .Bind(configuration.GetSection(ImportOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Kho file runtime (upload/export). Singleton: không trạng thái, chỉ giữ đường dẫn gốc đã
        // phân giải một lần.
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // ValidateOnStart KHÔNG điều kiện, nhưng LUẬT thì có: StorageOptionsValidator chỉ bắt buộc
        // khai RootPath ở Production (xem docstring của nó). Gọi ValidateOnStart vô điều kiện để
        // luật đó chạy trên MỌI môi trường — ở Development nó vẫn bắt được một đường dẫn tương đối
        // khai nhầm, thứ sẽ trỏ vào ba chỗ khác nhau tuỳ cách chạy tiến trình.
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
