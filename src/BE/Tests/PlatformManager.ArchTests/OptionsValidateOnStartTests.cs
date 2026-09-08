using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi class <c>*Options</c> có ít nhất một thuộc tính gắn <c>[Required]</c> phải
/// được <c>ValidateOnStart()</c> ở đâu đó trong code sản phẩm.
///
/// <para><b>Lỗi thật nó bắt — đã trả giá 2026-08-31:</b> <c>Cors:AllowedOrigins</c>. Cấu hình
/// thiếu ở Production biến thành allowlist RỖNG, mà allowlist rỗng chặn MỌI origin. Không một
/// lời gọi API nào của FE tới được app, trong khi <c>/health</c> vẫn xanh và deploy vẫn báo thành
/// công. <c>[Required]</c> mà không có <c>ValidateOnStart()</c> nghĩa là cấu hình thiếu chỉ lộ ra
/// ở REQUEST ĐẦU TIÊN — tức là sau khi đã deploy xong và đã báo thành công.</para>
///
/// <para><b>Đánh đổi: phân tích tĩnh, KHÔNG dựng container.</b> Lý do quyết định nằm ở chính
/// hình dạng của luật: cả 2 chỗ <c>ValidateOnStart()</c> trong repo đều nằm trong nhánh
/// <c>if</c> — CORS chỉ ở Production, Bootstrap chỉ trên đường <c>--seed</c>. Dựng
/// <c>ServiceCollection</c> một lần sẽ chạy đúng MỘT nhánh, nên sẽ báo thiếu cho những Options
/// được canh ở nhánh kia. Ngoài ra <c>Program.cs</c> là top-level statements: không có hàm nào
/// gọi lại được, và đăng ký CORS chỉ tồn tại ở đó. Xem thêm <see cref="RepoSourceTree"/>.</para>
///
/// <para><b>Giới hạn phải biết (đừng nhầm test này với bảo đảm runtime):</b> nó khẳng định "có
/// một đường code gắn <c>ValidateOnStart()</c> cho Options này", KHÔNG khẳng định "đường đó chạy
/// trong mọi môi trường". Ví dụ hôm nay: <c>SmtpOptions</c> được
/// <c>AddNotificationInfrastructure()</c> gắn <c>ValidateOnStart()</c> vô điều kiện, nhưng không
/// dòng nào trong <c>Program.cs</c> gọi hàm đó (cố ý — xem chú thích Notification ở Program.cs),
/// nên trên thực tế nó chưa bao giờ chạy. Canh "nhánh nào chạy ở môi trường nào" là việc của
/// integration test chạy host thật (xem <c>Production/ProductionHostTests.cs</c>), không phải của
/// ArchTest.</para>
/// </summary>
public class OptionsValidateOnStartTests
{
    [Fact(DisplayName = "Mọi *Options có [Required] đều được ValidateOnStart() ở đâu đó")]
    public void EveryRequiredOptions_HasA_ValidateOnStart_CodePath()
    {
        var declared = DeclaredRequiredOptionsTypes();

        // Chặn "pass rỗng" ở nguồn kỳ vọng — không có Options nào thì mọi assert dưới vô nghĩa.
        Assert.True(declared.Count > 0,
            "Không tìm thấy class *Options nào có [Required] trong assembly sản phẩm ⇒ test này không đo " +
            "gì. Nhiều khả năng ProductAssemblies.All thiếu assembly (đặc biệt PlatformManager.Api — nơi " +
            "CorsPolicyOptions sống) hoặc một assembly không nạp được.");

        var sources = CompositionSources();
        Assert.True(sources.Count > 0,
            $"Không đọc được file .cs sản phẩm nào dưới '{RepoSourceTree.BackendRoot}' ⇒ bộ dò không có gì " +
            "để soi và sẽ báo 'thiếu ValidateOnStart' cho tất cả. Xem RepoSourceTree.");

        var unvalidated = declared
            .Where(type => !HasValidateOnStartPath(type.Name, sources))
            .Select(type => $"{type.FullName} (thuộc tính [Required]: {string.Join(", ", RequiredPropertyNames(type))})")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(unvalidated.Count == 0,
            "Options có [Required] nhưng KHÔNG có đường nào gọi ValidateOnStart() ⇒ cấu hình thiếu chỉ " +
            "lộ ra ở request đầu tiên, sau khi deploy đã báo thành công (đúng lỗi Cors:AllowedOrigins " +
            "2026-08-31): " + string.Join(", ", unvalidated) + ". Cách sửa: ở nơi đăng ký, viết " +
            "`services.AddOptions<TênOptions>().Bind(configuration.GetSection(...)).ValidateDataAnnotations()" +
            ".ValidateOnStart();` — xem Core/PlatformManager.Core.Infrastructure/" +
            "NotificationInfrastructureExtensions.cs làm mẫu. Nếu fail-fast CỐ Ý chỉ bật ở một môi trường, " +
            "vẫn phải gọi ValidateOnStart() trong nhánh if đó (khuôn CorsPolicyOptions ở Program.cs) — test " +
            "này chấp nhận nhánh có điều kiện. Nếu thuộc tính KHÔNG thật sự bắt buộc thì gỡ [Required] đi, " +
            "đừng để một ràng buộc chỉ có hiệu lực lúc đã quá muộn.");
    }

    /// <summary>
    /// Đối chứng — chứng minh bộ dò biết nói KHÔNG. Không có ca này thì một lỗi trong
    /// <see cref="HasValidateOnStartPath"/> (ví dụ chỉ cần file chứa chữ "ValidateOnStart" là cho
    /// qua — mà chú thích trong repo nhắc chữ đó ở 9 chỗ) sẽ khiến test chính xanh vĩnh viễn.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò phải trả FALSE cho một Options không hề được AddOptions")]
    public void Detector_Reports_Missing_For_UnregisteredOptions()
    {
        var sources = CompositionSources();
        Assert.True(sources.Count > 0,
            $"Không đọc được file .cs sản phẩm nào dưới '{RepoSourceTree.BackendRoot}' — trên một tập rỗng, " +
            "ca đối chứng này đúng một cách hiển nhiên và không chứng minh được gì.");

        Assert.False(HasValidateOnStartPath("NeverRegisteredProbeOptions", sources),
            "Bộ dò báo 'đã ValidateOnStart' cho một Options không tồn tại ⇒ nó đang trả true vô điều " +
            "kiện, và test EveryRequiredOptions_HasA_ValidateOnStart_CodePath đang xanh mà không đo gì.");
    }

    /// <summary>
    /// Đối chứng thứ hai — chứng minh bộ dò xử lý được CẢ HAI khuôn gọi có thật trong repo:
    /// chuỗi liền mạch (<c>AddOptions&lt;T&gt;()….ValidateOnStart();</c>) và chuỗi gán vào biến
    /// rồi <c>ValidateOnStart()</c> ở một nhánh <c>if</c> phía sau. Nếu chỉ nhận ra khuôn thứ
    /// nhất, test chính sẽ đỏ và đổ lỗi nhầm cho code sản phẩm; ca này tách nguyên nhân ra.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò nhận cả chuỗi liền mạch lẫn chuỗi gán vào biến rồi validate sau")]
    public void Detector_Handles_Both_Registration_Shapes()
    {
        const string inlineChain = """
            services.AddOptions<AlphaOptions>()
                .Bind(configuration.GetSection("Alpha"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            """;

        const string deferredViaVariable = """
            var betaBuilder = services.AddOptions<BetaOptions>()
                .Bind(configuration.GetSection("Beta"))
                .ValidateDataAnnotations();
            if (builder.Environment.IsProduction())
            {
                betaBuilder.ValidateOnStart();
            }
            """;

        // Khuôn thứ ba: đăng ký mà KHÔNG bao giờ validate — phải bị bắt.
        const string neverValidated = """
            services.AddOptions<GammaOptions>()
                .Bind(configuration.GetSection("Gamma"))
                .ValidateDataAnnotations();
            """;

        Assert.True(HasValidateOnStartPath("AlphaOptions", [inlineChain]));
        Assert.True(HasValidateOnStartPath("BetaOptions", [deferredViaVariable]));
        Assert.False(HasValidateOnStartPath("GammaOptions", [neverValidated]));
    }

    // ── Hai nguồn ────────────────────────────────────────────────────────

    /// <summary>Nguồn kỳ vọng: REFLECTION trên assembly sản phẩm, độc lập với văn bản mã nguồn.</summary>
    private static List<Type> DeclaredRequiredOptionsTypes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .Where(type => type.IsClass
                        && !type.IsAbstract
                        && type.Name.EndsWith("Options", StringComparison.Ordinal)
                        && RequiredPropertyNames(type).Count > 0)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];

    private static List<string> RequiredPropertyNames(Type type) =>
        [.. type.GetProperties()
            .Where(property => property.IsDefined(typeof(RequiredAttribute), inherit: true))
            .Select(property => property.Name)];

    private static List<string> CompositionSources() =>
        [.. RepoSourceTree.ProductSourceFiles().Select(RepoSourceTree.ReadCodeWithoutComments)];

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm một đường code gắn <c>ValidateOnStart()</c> cho <paramref name="optionsTypeName"/>.
    ///
    /// <para>Phạm vi cố ý HẸP: chỉ xét câu lệnh chứa <c>AddOptions&lt;T&gt;</c> (cắt theo dấu
    /// <c>;</c>), rồi mới xét biến nhận kết quả của chính câu lệnh đó. KHÔNG bao giờ chấp nhận
    /// "file này có chứa chữ ValidateOnStart" — chú thích trong repo nhắc chữ đó ở rất nhiều chỗ,
    /// và một bộ dò cả tin sẽ cho qua mọi thứ.</para>
    /// </summary>
    private static bool HasValidateOnStartPath(string optionsTypeName, IReadOnlyList<string> sources)
    {
        var registration = new Regex(
            $@"AddOptions\s*<\s*{Regex.Escape(optionsTypeName)}\s*>", RegexOptions.Compiled);

        foreach (var code in sources)
        {
            foreach (Match match in registration.Matches(code))
            {
                var statement = StatementAround(code, match.Index);

                // Khuôn 1 — chuỗi liền mạch kết thúc bằng .ValidateOnStart();
                if (statement.Contains("ValidateOnStart", StringComparison.Ordinal))
                    return true;

                // Khuôn 2 — chuỗi gán vào biến, ValidateOnStart() gọi sau (thường trong nhánh if).
                var assigned = AssignedVariable.Match(statement);
                if (assigned.Success
                    && Regex.IsMatch(code, $@"\b{Regex.Escape(assigned.Groups["name"].Value)}\s*\.\s*ValidateOnStart\s*\("))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Cắt lấy câu lệnh chứa vị trí <paramref name="index"/>: từ sau dấu ngắt câu lệnh gần nhất
    /// phía trước (<c>;</c> <c>{</c> <c>}</c>) tới dấu <c>;</c> gần nhất phía sau.
    /// </summary>
    private static string StatementAround(string code, int index)
    {
        var start = code.LastIndexOfAny([';', '{', '}'], index) + 1;
        var end = code.IndexOf(';', index);

        return end < 0 ? code[start..] : code[start..(end + 1)];
    }

    private static readonly Regex AssignedVariable = new(
        @"(?:var|OptionsBuilder\s*<[^>]*>)\s+(?<name>\w+)\s*=", RegexOptions.Compiled);
}
