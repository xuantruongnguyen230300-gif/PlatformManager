using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Api.Modules;
using PlatformManager.Core.Application.Modules;
using PlatformManager.Core.Infrastructure.Modules;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.ArchTests;

/// <summary>
/// Hai nguồn sự thật dùng chung cho nhóm ArchTest "sự TỒN TẠI / sự ĐƯỢC NỐI VÀO" (thêm
/// 2026-09-01): <see cref="ProductAssemblies"/> (reflection — biết CÓ những thành phần nào) và
/// <see cref="RepoSourceTree"/> (văn bản mã nguồn — biết chúng có được NỐI VÀO hay không).
///
/// <para>Tách 2 nguồn là điều kiện để test không rỗng: nếu cả tập kỳ vọng lẫn tập thực tế đều lấy
/// từ cùng một nơi thì test chỉ hỏi một danh sách "anh có ai" rồi kiểm đúng những người đó — đúng
/// khuôn 2 bất biến "xanh mà không đo gì" đã tìm ra ở
/// <see cref="SoftDeleteQueryFilterTests"/> (finding F2) và <see cref="EntityEncapsulationTests"/>
/// (finding F7).</para>
/// </summary>
internal static class ProductAssemblies
{
    /// <summary>
    /// Mọi assembly SẢN PHẨM (không gồm assembly test). Thêm module nghiệp vụ mới thì thêm bộ 3
    /// assembly của nó vào đây — 4 test trong nhóm này tự phủ module mới.
    ///
    /// <para><c>PlatformManager.Api</c> có mặt qua <see cref="Api.Common.CorsPolicyOptions"/> —
    /// một kiểu <c>public</c> bất kỳ của assembly đó là đủ để nạp nó. Middleware trong
    /// <c>Api/Common</c> là <c>internal</c>, nhưng <c>Assembly.GetTypes()</c> vẫn thấy chúng
    /// (reflection không bị chặn bởi accessibility), nên KHÔNG cần <c>InternalsVisibleTo</c>.</para>
    /// </summary>
    public static readonly Assembly[] All =
    [
        typeof(Core.Domain.Common.BaseEntity).Assembly,
        typeof(Core.Application.DependencyInjection).Assembly,
        typeof(Core.Infrastructure.DependencyInjection).Assembly,
        typeof(Api.Common.CorsPolicyOptions).Assembly,
    ];

    /// <summary>
    /// <c>GetTypes()</c> ném <see cref="ReflectionTypeLoadException"/> khi một dependency không
    /// nạp được, và khi đó nó vẫn trả về các kiểu đã nạp thành công trong <c>ex.Types</c>. Nuốt
    /// ngoại lệ ở đây là CÓ Ý THỨC nhưng KHÔNG âm thầm: kiểu nạp hụt biến mất khỏi tập kỳ vọng ⇒
    /// test có thể xanh oan. Vì vậy mọi test dùng hàm này đều phải kèm một <c>Assert.NotEmpty</c>
    /// trên tập kỳ vọng của nó.
    /// </summary>
    public static IEnumerable<Type> LoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Select(type => type!);
        }
    }

    public static IEnumerable<Type> AllLoadableTypes() => All.SelectMany(LoadableTypes);
}

/// <summary>
/// Composition root THẬT dựng ở mức MÔ TẢ đăng ký — chạy đúng lời gọi mà <c>Program.cs</c> chạy
/// (<c>AddModules</c> trên danh sách tầng của host), KHÔNG <c>BuildServiceProvider()</c> nên
/// không khởi tạo gì và không mở kết nối nào (cùng khuôn với
/// <see cref="SoftDeleteQueryFilterTests"/>).
///
/// <para><b>Danh sách tầng lấy THẲNG từ host (2026-09-08), không chép lại.</b> Trước đó chỗ này
/// gọi <c>AddCoreModule(configuration)</c> và mang một chú thích "thêm module mới thì thêm
/// .AddXxxModule() ở đây" — tức một danh sách tầng THỨ HAI, do tay người giữ, nằm trong bộ test
/// canh chính việc "không sót tầng nào". Danh sách phụ đó chỉ lệch được theo một chiều: quên
/// thêm ⇒ mọi test đọc composition root này im lặng bỏ qua tầng mới, và chúng vẫn XANH.</para>
/// </summary>
internal static class CompositionRoot
{
    /// <summary>Danh sách tầng của host — nguồn sự thật DUY NHẤT, xem <see cref="HostModuleRegistrars"/>.</summary>
    public static IReadOnlyList<IModuleRegistrar> Registrars() => HostModuleRegistrars.Create();

    public static IServiceCollection CoreServices()
    {
        var configuration = new ConfigurationBuilder().Build();

        return new ServiceCollection().AddModules(configuration, Registrars());
    }
}

/// <summary>
/// Định vị CÂY MÃ NGUỒN backend lúc chạy test, và cắt chú thích trước khi phân tích tĩnh.
///
/// <para><b>Đánh đổi đã chọn (2026-09-01).</b> Hai luật "middleware có được nối vào pipeline
/// không" và "options có được <c>ValidateOnStart()</c> không" đều nằm ở <c>Program.cs</c> —
/// top-level statements, KHÔNG có hàm nào gọi lại được từ test. Hai đường khả dĩ:</para>
/// <list type="number">
///   <item><b>Dựng container thật</b> (<c>WebApplicationFactory&lt;Program&gt;</c>): chạy đúng
///   thứ ứng dụng chạy, nhưng (a) pipeline ASP.NET Core sau khi build là một chuỗi delegate
///   ĐÓNG — không liệt kê được middleware nào đã nối; (b) phải boot cả host (Hangfire +
///   Postgres) nên rơi khỏi ArchTests sang IntegrationTests; (c) mọi nhánh <c>if</c> theo môi
///   trường (CORS chỉ <c>ValidateOnStart</c> ở Production, Bootstrap chỉ ở đường <c>--seed</c>)
///   sẽ KHÔNG chạy trong lần boot đó ⇒ bỏ sót đúng những chỗ cần canh.</item>
///   <item><b>Đọc mã nguồn như văn bản</b> (đường đã chọn): thấy được cả nhánh điều kiện, chạy
///   trong mili-giây, không cần Docker. Giá phải trả: GIÒN khi refactor — đổi khuôn gọi (ví dụ
///   gói việc nối pipeline vào một hàm nhận tên middleware bằng chuỗi) sẽ làm test đỏ dù code
///   đúng. Khi đó sửa bộ dò ở đây, ĐỪNG nới luật cho xanh.</item>
/// </list>
/// </summary>
internal static class RepoSourceTree
{
    private static readonly Lazy<string> BackendRootLazy = new(LocateBackendRoot);

    /// <summary>Thư mục chứa <c>PlatformManager.slnx</c> (hôm nay là <c>src/BE</c>).</summary>
    public static string BackendRoot => BackendRootLazy.Value;

    /// <summary>
    /// Đường dẫn tuyệt đối tới <c>PlatformManager.slnx</c>. Thêm 2026-09-02 cho
    /// <see cref="SolutionProjectCoverageTests"/> — luật duy nhất trong bộ này lấy nguồn kỳ vọng từ
    /// HỆ THỐNG TỆP (project có trên đĩa) và nguồn thực tế từ VĂN BẢN solution.
    /// </summary>
    public static string SolutionPath => Path.Combine(BackendRoot, "PlatformManager.slnx");

    /// <summary>
    /// Mọi file <c>*.csproj</c> trong cây backend, bỏ <c>obj/</c> và <c>bin/</c> — cần lọc vì
    /// NuGet sinh <c>obj/…/*.csproj.nuget.*</c> và một số công cụ chép csproj vào <c>obj/</c>.
    /// Trả về đường dẫn TUYỆT ĐỐI đã chuẩn hoá, để so sánh tập được với danh sách trong
    /// <c>.slnx</c>.
    /// </summary>
    public static IReadOnlyList<string> ProjectFiles() =>
        !Directory.Exists(BackendRoot)
            ? []
            : [.. Directory.EnumerateFiles(BackendRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !IsUnder(path, "obj") && !IsUnder(path, "bin"))
                .Select(Path.GetFullPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)];

    /// <summary>Đường dẫn hiển thị gọn (tương đối so với gốc backend) cho thông điệp lỗi.</summary>
    public static string Relative(string absolutePath) =>
        Path.GetRelativePath(BackendRoot, absolutePath).Replace('\\', '/');

    public static string ApiDirectory => Path.Combine(BackendRoot, "PlatformManager.Api");

    public static string ProgramCsPath => Path.Combine(ApiDirectory, "Program.cs");

    /// <summary>Mọi file <c>.cs</c> sản phẩm (Core + Api), bỏ <c>obj/</c> và <c>bin/</c>.</summary>
    public static IReadOnlyList<string> ProductSourceFiles() =>
    [
        .. CoreSourceFiles(),
        .. CSharpFilesUnder(ApiDirectory),
    ];

    /// <summary>Thư mục chứa 3 project <c>PlatformManager.Core.*</c>.</summary>
    public static string CoreDirectory => Path.Combine(BackendRoot, "Core");

    /// <summary>
    /// Mọi file <c>.cs</c> của RIÊNG tầng Core. Dùng bởi
    /// <see cref="CoreMustNotKnowBusinessNameTests"/> và <see cref="MigrationsLocationTests"/>.
    ///
    /// <para><b>Sửa 2026-09-04.</b> Câu trước ghi "gồm cả <c>Persistence/Migrations</c> — code sinh
    /// tự động vẫn là code nằm trong assembly Core". Nửa đầu không còn đúng: theo chốt "migration
    /// thuộc host, Core ship .sql", các file <c>.cs</c> migration đã chuyển sang
    /// <c>PlatformManager.Api/Persistence/Migrations/</c>, dưới Core chỉ còn thư mục <c>sql/</c>
    /// (không phải <c>.cs</c> nên không lọt vào hàm này). Nửa sau vẫn là nguyên tắc đúng: code EF
    /// sinh ra KHÔNG được miễn luật — nay nó chịu luật với tư cách file của Api. Chính
    /// <see cref="MigrationsLocationTests"/> canh việc nó không quay lại đây.</para>
    /// </summary>
    public static IReadOnlyList<string> CoreSourceFiles() => CSharpFilesUnder(CoreDirectory);

    public static IReadOnlyList<string> ApiSourceFiles() => CSharpFilesUnder(ApiDirectory);

    /// <summary>
    /// MỌI file <c>.cs</c> sản phẩm trong toàn cây backend — không giới hạn ở <c>Core/</c> và
    /// <c>PlatformManager.Api/</c> như <see cref="ProductSourceFiles"/>. Bỏ <c>Tests/</c>,
    /// <c>obj/</c>, <c>bin/</c>.
    ///
    /// <para>Thêm 2026-09-08 cho <see cref="ModuleRegistrarSeamTests"/>. Cần bề rộng này vì luật
    /// nó canh nói về thứ CHƯA TỒN TẠI: một tầng nghiệp vụ dựng sau này sẽ nằm ở thư mục mà hôm
    /// nay chưa có tên. Quét theo danh sách thư mục biết trước là quét hẹp hơn ranh giới, và
    /// một tầng mới đặt ngoài danh sách đó sẽ vô hình đúng lúc luật cần thấy nó nhất.</para>
    /// </summary>
    public static IReadOnlyList<string> AllProductSourceFilesInTree() =>
        !Directory.Exists(BackendRoot)
            ? []
            : [.. Directory.EnumerateFiles(BackendRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsUnder(path, "obj") && !IsUnder(path, "bin") && !IsUnder(path, "Tests"))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Đọc file và thay MỌI chú thích bằng dấu cách CÙNG ĐỘ DÀI — giữ nguyên chỉ số ký tự để các
    /// phép cắt câu lệnh theo <c>;</c> bên dưới vẫn đúng.
    ///
    /// <para>Bắt buộc phải cắt chú thích: repo này chú thích rất dày và nhắc thẳng tên
    /// <c>ValidateOnStart</c> / tên middleware trong văn xuôi — không cắt thì bộ dò sẽ báo "đã
    /// nối" cho một dòng chỉ NÓI VỀ việc nối.</para>
    ///
    /// <para>Giới hạn đã biết: regex này không phân biệt <c>//</c> trong chuỗi (ví dụ
    /// <c>https://…</c>) với chú thích thật. Hệ quả duy nhất là phần đuôi chuỗi đó bị xoá — chỉ
    /// sai nếu chuỗi chứa dấu <c>;</c>, chưa xảy ra trong repo. Cần đúng tuyệt đối thì phải nhấc
    /// sang Roslyn, giá không xứng với 4 luật này.</para>
    /// </summary>
    public static string ReadCodeWithoutComments(string path) =>
        RemoveComments(File.ReadAllText(path));

    /// <summary>
    /// Cùng phép cắt chú thích như <see cref="ReadCodeWithoutComments"/> nhưng nhận thẳng văn bản.
    /// Tách ra 2026-09-01 để các ca ĐỐI CHỨNG chạy được trên mẫu văn bản dựng trong test mà KHÔNG
    /// phải viết bộ cắt chú thích thứ hai — đối chứng chạy trên một bộ cắt khác thì nó không
    /// chứng minh gì về bộ cắt đang dùng thật.
    /// </summary>
    public static string RemoveComments(string code) =>
        CommentPattern.Replace(code, match => new string(' ', match.Length));

    private static readonly Regex CommentPattern = new(
        @"/\*.*?\*/|//[^\r\n]*", RegexOptions.Singleline | RegexOptions.Compiled);

    private static List<string> CSharpFilesUnder(string root) =>
        !Directory.Exists(root)
            ? []
            : [.. Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsUnder(path, "obj") && !IsUnder(path, "bin"))];

    private static bool IsUnder(string path, string folder) =>
        path.Contains($"{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string LocateBackendRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PlatformManager.slnx")))
                return dir.FullName;
        }

        throw new InvalidOperationException(
            "Không tìm thấy gốc backend (thư mục chứa PlatformManager.slnx) khi đi ngược từ " +
            $"'{AppContext.BaseDirectory}'. MiddlewareWiringTests và OptionsValidateOnStartTests phân tích " +
            "TĨNH mã nguồn thật, nên chỉ chạy được khi thư mục output nằm trong cây repo. Cách chạy đúng: " +
            "`dotnet test src/BE/Tests/PlatformManager.ArchTests` tại chỗ — đừng copy thư mục bin/ đi nơi khác rồi chạy.");
    }
}

/// <summary>
/// Dựng MODEL EF ở mức mô tả — không mở kết nối, không cần Docker — từ ĐÚNG tập
/// <see cref="EfConfigurationAssembly"/> mà DI thật nộp vào.
///
/// <para><b>Vì sao tách ra đây (2026-09-03):</b> <see cref="SchemaBoundaryTests"/> cần đúng model
/// mà <see cref="SoftDeleteQueryFilterTests"/> đã dựng. Chép lời gọi <c>UseNpgsql</c> + danh sách
/// assembly sang file thứ hai là dựng model bằng một ĐƯỜNG KHÁC với đường ứng dụng dùng — đúng
/// cách hỏng mà finding F6 đã trả giá một lần: bỏ một tầng khỏi DI mà test vẫn
/// xanh vì nó tự tay truyền danh sách assembly vào.</para>
/// </summary>
internal static class EfModelProbe
{
    /// <summary>
    /// Danh sách <see cref="EfConfigurationAssembly"/> ĐÚNG NHƯ ứng dụng thật nhận: chạy chính
    /// <c>AddModules()</c> trên danh sách tầng của host (qua <see cref="CompositionRoot.CoreServices"/>)
    /// rồi ĐỌC MÔ TẢ đăng ký — không <c>BuildServiceProvider()</c>, nên không khởi tạo gì và không
    /// mở kết nối nào.
    /// </summary>
    public static List<Assembly> ConfigurationAssemblies() =>
        [.. CompositionRoot.CoreServices()
            .Where(descriptor => descriptor.ServiceType == typeof(EfConfigurationAssembly))
            .Select(descriptor => ((EfConfigurationAssembly)descriptor.ImplementationInstance!).Assembly)
            .Distinct()];

    /// <summary>
    /// Chỉ dựng model, KHÔNG kết nối DB — connection string là placeholder hợp lệ về cú pháp, EF
    /// không mở kết nối khi chỉ đọc <c>Model</c>. Provider PHẢI là Npgsql (không phải InMemory):
    /// schema là khái niệm của provider quan hệ, InMemory bỏ qua nó nên model dựng bằng InMemory
    /// sẽ trả <c>GetSchema() == null</c> cho mọi entity và biến luật schema thành vô nghĩa.
    /// </summary>
    public static PlatformManagerDbContext CreateModelOnlyContext()
    {
        var options = new DbContextOptionsBuilder<PlatformManagerDbContext>()
            .UseNpgsql("Host=localhost;Database=arch_tests_model_only;Username=none;Password=none")
            .Options;

        return new PlatformManagerDbContext(
            options,
            [.. ConfigurationAssemblies().Select(assembly => new EfConfigurationAssembly(assembly))]);
    }
}
