using System.Reflection;
using System.Text.RegularExpressions;
using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Bắt bằng máy — không dựa vào review người — luật kiến trúc quan trọng nhất của dự án (xem
/// doc/huong_dan/quy-uoc/be-architecture.md §"Project layout & dependency direction",
/// doc/kien-truc-core-module.md). Cố tình chạy test này FAIL trước khi có (thêm
/// EntityFrameworkCore vào Core.Domain.csproj tạm thời) để xác nhận nó thật sự bắt được lỗi.
/// </summary>
public class LayerDependencyTests
{
    /// <summary>
    /// Hạ tầng cụ thể mà TẦNG APPLICATION (Core lẫn mọi Module) không được reference. Khai
    /// MỘT chỗ và dùng chung cho cả 2 test bên dưới — trước 2026-08-28 danh sách bị chép tay 2
    /// lần, và Hangfire không có trong bản nào nên `BackgroundJob.Enqueue` gọi thẳng từ
    /// StartImportCommandHandler lọt qua: test XANH VÌ MÙ, không phải vì code đúng.
    ///
    /// ⚠️ GIỚI HẠN đã đo (2026-08-28): GetReferencedAssemblies() chỉ thấy assembly THẬT SỰ
    /// ĐƯỢC DÙNG — Roslyn lược bỏ khỏi manifest mọi reference không có code nào chạm tới. Vì
    /// vậy test này bắt "code gọi thẳng hạ tầng" (đúng thứ vi phạm layer), CHỨ KHÔNG bắt
    /// "csproj còn PackageReference thừa". Xác nhận bằng canary: thêm lại
    /// PackageReference Hangfire.Core mà không có lời gọi nào → test vẫn xanh; thêm lại cả
    /// lời gọi BackgroundJob.Enqueue → test đỏ và nêu đích danh Hangfire.Core.
    /// </summary>
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        // Enqueue job nền PHẢI đi qua seam IBackgroundJobScheduler (Core.Application), hiện
        // thực Hangfire nằm ở Core.Infrastructure — xem
        // doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Command chạy lâu → job nền".
        "Hangfire",
        // ── Thêm 2026-09-09 (finding F3) ────────────────────────────────────────────────────
        // Đọc file import PHẢI đi qua seam IImportFileReader (Core.Application); hai thư viện
        // dưới đây chỉ được có mặt ở Core.Infrastructure — xem
        // doc/huong_dan/wiki-core/be/15-import-export.md §2.
        //
        // Vì sao thêm chứ không sửa chú thích cho khớp: PlatformManager.Core.Infrastructure.csproj
        // (khối PackageReference của Import engine) TUYÊN BỐ "LayerDependencyTests cưỡng chế" —
        // nhưng trước dòng này mảng không có NPOI lẫn CsvHelper, tức máy không cưỡng chế gì. Hôm
        // nay Core.Application thật sự sạch nên chưa có vi phạm; đó chính là lúc rẻ nhất để đóng
        // lỗ hổng, và đúng khuôn bài học 2026-09-08 ở .claude/CLAUDE.md §8 (luật tuyên bố được
        // cưỡng chế bằng máy mà máy không chặn).
        "NPOI",
        "CsvHelper",
    ];

    internal static List<string> ForbiddenReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => ForbiddenAssemblyPrefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal))
                        // Bất kỳ project *.Persistence/*.Infrastructure nào (.Persistence thêm 2026-09-10, xem ApplicationLayerBoundaryTests).
                        || n.Contains(".Infrastructure", StringComparison.Ordinal) || n.Contains(".Persistence", StringComparison.Ordinal))
            .ToList();

    [Fact]
    public void Core_Domain_Assembly_MustHave_ZeroPackageReference()
    {
        var referenced = typeof(BaseEntity).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => !n.StartsWith("System", StringComparison.Ordinal)
                        && !n.StartsWith("netstandard", StringComparison.Ordinal)
                        && !n.StartsWith("mscorlib", StringComparison.Ordinal))
            .ToList();

        Assert.True(referenced.Count == 0, $"Core.Domain đang reference (phải zero): {string.Join(", ", referenced)}");
    }

    [Fact]
    public void Core_Application_MustNotDependOn_EntityFrameworkCore_Or_AspNetCore_Or_AnyInfrastructure()
    {
        var forbidden = ForbiddenReferencesOf(typeof(PlatformManager.Core.Application.DependencyInjection).Assembly);

        Assert.True(forbidden.Count == 0, $"Core.Application đang reference (không được phép): {string.Join(", ", forbidden)}");
    }

    // Module nghiệp vụ (DtiWeekly) đã bị xoá 2026-08-29 để xây lại, nên 2 test tương ứng
    // (Modules_*_Domain_MustOnlyDependOn_CoreDomain / Modules_*_Application_MustNotDependOn_*)
    // đã gỡ theo — chúng chỉ định danh được khi có assembly thật. Dựng module mới thì thêm lại
    // theo đúng khuôn 2 test Core_* ở trên.

    // ────────────────────────────────────────────────────────────────────────────────────────
    // Ngoại lệ đã khai của luật "*.Application ⇏ IConfiguration" — 2 luật dưới đây GIỮ CHO NÓ
    // KHÔNG LAN RA. Điều kiện và giới hạn của ngoại lệ:
    // doc/huong_dan/quy-uoc/be-architecture.md §"Ngoại lệ DUY NHẤT của luật *.Application ⇏
    // IConfiguration". Tóm tắt: Core.Application được KHAI KIỂU IConfiguration trong chữ ký
    // IModuleRegistrar.RegisterServices (chạy lúc composition, mỗi tầng tự bind IOptions<T> của
    // mình) nhưng KHÔNG được ĐỌC giá trị cấu hình nào.
    // ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Gói cấu hình DUY NHẤT mà Core.Application được phép reference.</summary>
    private const string AllowedConfigurationAssembly = "Microsoft.Extensions.Configuration.Abstractions";

    /// <summary>File DUY NHẤT của Core.Application được phép nhắc tới <c>IConfiguration</c>.</summary>
    private static readonly string RegistrarContractFile =
        Path.Combine("PlatformManager.Core.Application", "Modules", "IModuleRegistrar.cs");

    /// <summary>Danh hiệu <c>IConfiguration</c>, <c>IConfigurationSection</c>, <c>IConfigurationRoot</c>…</summary>
    private static readonly Regex IConfigurationIdentifier =
        new(@"\bIConfiguration[A-Za-z0-9_]*\b", RegexOptions.Compiled);

    /// <summary>
    /// <b>Luật (khai 2026-09-08):</b> trong số các gói cấu hình, <c>Core.Application</c> chỉ được
    /// reference <b>abstraction</b> — không provider (<c>Configuration.Json</c>,
    /// <c>Configuration.EnvironmentVariables</c>), không binder (<c>Configuration.Binder</c>,
    /// <c>Microsoft.Extensions.Options.ConfigurationExtensions</c>).
    ///
    /// <para>Vì sao vạch đúng ở ranh giới abstraction/binder: chỉ khai kiểu trong một chữ ký thì
    /// abstraction là đủ. Cần tới binder nghĩa là có ai đó đang <b>đọc giá trị</b>
    /// (<c>.Get&lt;T&gt;()</c>, <c>.GetValue&lt;T&gt;()</c>) — đúng thứ luật gốc cấm, và nó bỏ qua
    /// toàn bộ fail-fast của <c>IOptions&lt;T&gt;</c> + <c>ValidateOnStart()</c>.</para>
    ///
    /// <para>⚠️ Chịu CHUNG giới hạn đã đo của <see cref="ForbiddenAssemblyPrefixes"/>:
    /// <c>GetReferencedAssemblies()</c> chỉ thấy assembly thật sự có code chạm tới, nên luật này
    /// bắt "đã dùng", không bắt "csproj còn PackageReference thừa". Và nó KHÔNG bắt được một
    /// handler nhận <c>IConfiguration</c> rồi đọc bằng indexer — ca đó không sinh tham chiếu mới
    /// nào. Đó là lý do luật thứ hai bên dưới tồn tại.</para>
    /// </summary>
    [Fact(DisplayName = "Core.Application chỉ reference abstraction cấu hình, không provider/binder nào")]
    public void Core_Application_MustNotReference_ConfigurationPackages_Beyond_Abstractions()
    {
        var referenced = typeof(PlatformManager.Core.Application.DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Chặn "xanh mà không đo gì": assembly nạp hụt ⇒ danh sách rỗng ⇒ không vi phạm nào.
        Assert.True(referenced.Count > 0,
            "Không đọc được reference nào của Core.Application ⇒ luật này không đo gì.");

        var forbidden = referenced
            .Where(name => name.Contains("Configuration", StringComparison.Ordinal)
                           && !string.Equals(name, AllowedConfigurationAssembly, StringComparison.Ordinal))
            .ToList();

        Assert.True(forbidden.Count == 0,
            "Core.Application đang reference gói cấu hình ngoài abstraction: " + string.Join(", ", forbidden) +
            ". Ngoại lệ đã khai chỉ cho phép KHAI KIỂU IConfiguration trong chữ ký " +
            "IModuleRegistrar.RegisterServices, không cho phép đọc giá trị cấu hình. Cấu hình cần đọc thì " +
            "bind thành IOptions<T> ở composition root — xem doc/huong_dan/quy-uoc/be-architecture.md " +
            "muc \"Cấu hình — fail-fast validation\".");
    }

    /// <summary>
    /// <b>Luật (khai 2026-09-08):</b> trong mã nguồn <c>Core.Application</c>, danh hiệu
    /// <c>IConfiguration*</c> chỉ được xuất hiện ở <see cref="RegistrarContractFile"/>.
    ///
    /// <para><b>Đây mới là luật chặn rủi ro tiền lệ.</b> Cách phá dễ nhất KHÔNG tạo tham chiếu
    /// package mới: một handler khai một tham số <c>IConfiguration</c> rồi đọc giá trị qua indexer
    /// — biên dịch sạch, luật assembly ở trên xanh, và ranh giới đã mất. Cùng khuôn phân tích tĩnh
    /// văn bản mã nguồn với <see cref="CoreMustNotKnowBusinessNameTests"/>; giá phải trả và cách
    /// nó có thể sai đã ghi ở <see cref="RepoSourceTree"/>.</para>
    ///
    /// <para><b>Giới hạn đã biết:</b> luật quét danh hiệu <c>IConfiguration</c>, không quét mọi
    /// đường vòng có thể nghĩ ra (ví dụ nhận <c>IServiceProvider</c> rồi tự resolve). Nó bịt lối
    /// đi thẳng và rẻ nhất, không tuyên bố bịt hết.</para>
    /// </summary>
    [Fact(DisplayName = "Chỉ IModuleRegistrar.cs được nhắc IConfiguration trong Core.Application")]
    public void CoreApplicationSource_MustNotMention_IConfiguration_OutsideRegistrarContract()
    {
        var applicationDirectorySegment =
            $"{Path.DirectorySeparatorChar}PlatformManager.Core.Application{Path.DirectorySeparatorChar}";

        var files = RepoSourceTree.CoreSourceFiles()
            .Where(path => path.Contains(applicationDirectorySegment, StringComparison.Ordinal))
            .ToList();

        // Chặn "xanh mà không đo gì" (1/2): đường dẫn sai ⇒ tập file rỗng ⇒ "không ai vi phạm".
        Assert.True(files.Count > 0,
            $"Không đọc được file .cs nào của Core.Application dưới '{RepoSourceTree.CoreDirectory}' ⇒ luật này " +
            "xanh vĩnh viễn mà không đo gì. Xem RepoSourceTree.LocateBackendRoot.");

        var mentions = new List<string>();
        var contractFileMentions = 0;

        foreach (var file in files)
        {
            var code = RepoSourceTree.ReadCodeWithoutComments(file);
            var hits = IConfigurationIdentifier.Matches(code).Count;
            if (hits == 0)
                continue;

            if (file.EndsWith(RegistrarContractFile, StringComparison.Ordinal))
                contractFileMentions += hits;
            else
                mentions.Add($"{RepoSourceTree.Relative(file)} ({hits} lần)");
        }

        // Chặn "xanh mà không đo gì" (2/2): bộ dò hỏng ⇒ 0 hit ở khắp nơi ⇒ xanh. Ngoại lệ đã khai
        // là thứ DUY NHẤT được phép khớp, nên nó cũng là mẫu chứng minh bộ dò còn hoạt động.
        Assert.True(contractFileMentions > 0,
            "Không thấy danh hiệu IConfiguration trong Core/PlatformManager.Core.Application/Modules/" +
            "IModuleRegistrar.cs — hoặc bộ dò đã hỏng (và luật này đang xanh mà không đo gì), hoặc hợp đồng " +
            "IModuleRegistrar đã bỏ tham số IConfiguration. Nếu là vế sau: gỡ luôn muc \"Ngoại lệ DUY NHẤT\" ở " +
            "doc/huong_dan/quy-uoc/be-architecture.md rồi xoá 2 luật này — ngoại lệ không còn được dùng thì " +
            "không được để nó nằm lại như một giấy phép ngỏ.");

        Assert.True(mentions.Count == 0,
            "IConfiguration xuất hiện trong Core.Application ngoài chữ ký IModuleRegistrar.RegisterServices: " +
            string.Join("; ", mentions) +
            ". Ngoại lệ đã khai KHÔNG nới luật thành \"được phép nếu có lý do chính đáng\" — nó miễn trừ đúng " +
            "MỘT chữ ký. Cần cấu hình ở tầng Application thì khai IOptions<T> và bind ở composition root.");
    }
}
