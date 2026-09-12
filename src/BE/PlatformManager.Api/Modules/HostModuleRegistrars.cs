using PlatformManager.Core.Application.Modules;
using PlatformManager.Core.Infrastructure.Modules;

namespace PlatformManager.Api.Modules;

/// <summary>
/// Danh sách tầng của DỰ ÁN NÀY — nguồn sự thật DUY NHẤT cho câu hỏi "ứng dụng gồm những tầng
/// nào". Host là nơi duy nhất được biết cả Core lẫn tầng nghiệp vụ (xem
/// doc/kien-truc-core-module.md §Nguyên tắc phụ thuộc), nên danh sách chỉ có thể sống ở đây.
///
/// <para><b>Thêm một tầng nghiệp vụ = thêm MỘT dòng vào đây cho BA đường nối</b> (DI, cấu hình EF,
/// controller) — cả ba tự đi theo, vì cả ba đều đọc từ chính danh sách này:
/// <c>Program.cs</c> (đường DI + EF qua <c>AddModules</c>, đường controller qua
/// <c>AddModuleApplicationParts</c>) và <see cref="PlatformManagerDbContextFactory"/>
/// (design-time, cần biết đủ assembly Persistence để sinh migration).</para>
///
/// <para><b>Ngoại lệ đã biết — SEED.</b> Tầng nào cần seed dữ liệu khởi tạo thì vẫn phải sửa thêm
/// <c>Common/SeedCommand.cs</c>: seeder của tầng chạy SAU <c>CoreSeeder</c> (tầng nghiệp vụ có thể
/// cần role/user của Core đã tồn tại), và thứ tự đó không suy ra được từ danh sách này. Lời hứa lớn
/// vẫn giữ nguyên: <c>SeedCommand</c> là file của HOST, không phải <c>Core.*</c> — thêm tầng mới
/// vẫn KHÔNG phải sửa một dòng nào trong Core.</para>
///
/// <para><b>Vì sao là hàm chứ không phải hằng số:</b> <c>CoreModuleRegistrar</c> nhận
/// <c>requireBootstrapOptions</c>, thứ chỉ biết được sau khi đọc tham số dòng lệnh. Caller nào
/// không quan tâm (design-time factory, ArchTest) gọi không tham số và nhận mặc định.</para>
/// </summary>
public static class HostModuleRegistrars
{
    public static IReadOnlyList<IModuleRegistrar> Create(bool requireBootstrapOptions = false) =>
    [
        // Core LUÔN đứng đầu — tầng nghiệp vụ có thể cần dịch vụ/role/user của Core đã có mặt.
        new CoreModuleRegistrar(requireBootstrapOptions),

        // Tầng nghiệp vụ (thêm 2026-09-10, vòng 1 của cụm DTI). Thêm đúng MỘT dòng ở đây đã nối
        // đủ BA đường: dịch vụ vào DI, cấu hình EF của 4 entity vào model, controller vào bảng
        // route — Program.cs và PlatformManagerDbContextFactory không phải sửa một dòng nào.
        new BusinessModuleRegistrar(),

        // Thêm tầng nghiệp vụ mới: thêm registrar của nó vào ngay dưới dòng này. KHÔNG sửa
        // Program.cs, KHÔNG sửa PlatformManagerDbContextFactory, KHÔNG sửa gì trong Core.
        // ArchTest ModuleRegistrarSeamTests đỏ nếu có hiện thực IModuleRegistrar nào trong cây
        // mã nguồn mà không có mặt ở đây.
    ];
}
