using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> mọi class kế thừa <see cref="SaveChangesInterceptor"/> trong
/// <c>PlatformManager.Core.Persistence</c> phải xuất hiện trong một lời gọi
/// <c>AddInterceptors(...)</c> — tức phải được NỐI VÀO <c>DbContext</c>, không chỉ được đăng ký
/// trong DI container.
///
/// <para><b>Lỗi chưa ai canh:</b> xoá đúng một dòng
/// <c>options.AddInterceptors(sp.GetRequiredService&lt;AuditInterceptor&gt;())</c> ở
/// <c>Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs</c> thì <b>mọi</b> entity
/// kế thừa <c>BaseEntity</c> mất sạch <c>CreatedBy/CreatedAt/UpdatedBy/UpdatedAt</c> — vĩnh
/// viễn và im lặng. Không lỗi biên dịch (class vẫn tồn tại, vẫn còn
/// <c>services.AddScoped&lt;AuditInterceptor&gt;()</c> nên DI vẫn phân giải được), không ngoại
/// lệ lúc chạy, chỉ là các cột audit thôi được điền.</para>
///
/// <para><b>Đã kiểm: không test nào đỏ trong ca đó.</b> <c>UserAdminAuditTests</c> nghe tên thì
/// tưởng phủ, nhưng nó chỉ phủ <c>AppUser</c> — mà <c>AppUser</c> cố ý KHÔNG kế thừa
/// <c>BaseEntity</c> nên nằm ngoài tầm với của interceptor và mọi đường ghi của nó điền audit
/// bằng tay. Nghĩa là bài test duy nhất mang chữ "Audit" trong tên lại là bài test duy nhất
/// KHÔNG chứng minh gì về interceptor.</para>
///
/// <para><b>Vì sao "đăng ký DI" không đủ:</b> <c>AddScoped&lt;AuditInterceptor&gt;()</c> chỉ nói
/// "dựng được đối tượng này khi có ai xin". EF Core không xin — nó chỉ gọi những interceptor đã
/// được đưa vào <c>DbContextOptions</c>. Hai việc trông giống nhau trong code, cách nhau 43
/// dòng, và chỉ việc thứ hai mới có tác dụng. Vì vậy bộ dò ở đây soi RIÊNG đối số của
/// <c>AddInterceptors(...)</c>, KHÔNG chấp nhận việc chỉ nhắc tên ở chỗ khác.</para>
///
/// <para><b>⚠️ Bắt buộc cắt chú thích.</b> Tên <c>AuditInterceptor</c> xuất hiện trong văn xuôi ở
/// nhiều file (<c>BaseEntity</c>, <c>AppUser</c>, <c>IdentityService</c>, <c>UserAdminService</c>,
/// <c>CoreSeeder</c>, <c>SysMenuRoleRepository</c>…). Một bộ dò cả tin — chỉ tìm chuỗi trong toàn
/// file — sẽ báo "đã nối" mãi mãi, kể cả sau khi dòng nối thật đã bị xoá. Nên mọi lần đọc file ở
/// đây đều đi qua <see cref="RepoSourceTree.ReadCodeWithoutComments"/>, và có ca đối chứng chứng
/// minh việc đó thật sự có tác dụng.</para>
///
/// <para><b>Đánh đổi:</b> cùng khuôn với <see cref="MiddlewareWiringTests"/> — phân tích tĩnh văn
/// bản thay vì dựng <c>DbContext</c> thật. Lý do đầy đủ ở <see cref="RepoSourceTree"/>; riêng ở
/// đây còn một lý do nữa: đọc được interceptor đã nối từ một <c>DbContextOptions</c> đã dựng đòi
/// hỏi phải mở kết nối Postgres, tức rơi khỏi ArchTests sang IntegrationTests và phải có Docker.</para>
/// </summary>
public class InterceptorWiringTests
{
    [Fact(DisplayName = "Mọi SaveChangesInterceptor đều được nối vào DbContext qua AddInterceptors(...)")]
    public void EveryInterceptor_IsWiredInto_DbContextOptions()
    {
        var interceptors = DeclaredInterceptorTypes();

        // Chặn "pass rỗng": reflection trả rỗng thì mọi assert dưới vô nghĩa.
        Assert.True(interceptors.Count > 0,
            "Không tìm thấy class nào kế thừa SaveChangesInterceptor trong các assembly sản phẩm ⇒ test này " +
            "không đo gì. Nguyên nhân thường gặp: assembly PlatformManager.Core.Persistence không nạp được " +
            "(kiểm ProductAssemblies.All), hoặc interceptor đã chuyển sang một base khác " +
            "(IInterceptor/IDbCommandInterceptor) — khi đó mở rộng DeclaredInterceptorTypes, ĐỪNG xoá test.");

        var wired = WiredInterceptorArguments();
        Assert.True(wired.Count > 0,
            "Không tìm thấy lời gọi AddInterceptors(...) nào trong mã nguồn sản phẩm (sau khi đã cắt chú " +
            "thích) ⇒ hoặc không interceptor nào được nối, hoặc bộ dò đã hỏng. Nếu repo cố ý không dùng " +
            "interceptor nữa thì xoá luôn class interceptor — code chết không được ở lại giả làm hàng rào.");

        var notWired = interceptors
            .Where(type => !wired.Any(args => args.Contains(type.Name, StringComparison.Ordinal)))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(notWired.Count == 0,
            "Interceptor TỒN TẠI nhưng KHÔNG được nối vào DbContext ⇒ nó không bao giờ chạy, và không có gì " +
            "báo: " + string.Join(", ", notWired) + ". Với AuditInterceptor, hậu quả là MỌI entity kế thừa " +
            "BaseEntity mất CreatedBy/CreatedAt/UpdatedBy/UpdatedAt — im lặng, không hồi tố được. Cách sửa: " +
            "trong AddDbContext<PlatformManagerDbContext>((sp, options) => …) ở " +
            "Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs, thêm " +
            "`options.AddInterceptors(sp.GetRequiredService<Tên>());` (và giữ nguyên " +
            "`services.AddScoped<Tên>();` phía trên — ĐĂNG KÝ DI và NỐI VÀO DbContext là hai việc khác nhau, " +
            "thiếu việc thứ hai thì interceptor im lặng không chạy).");
    }

    /// <summary>
    /// Đối chứng 1 — chứng minh bộ dò biết nói KHÔNG cho một cái tên không hề được nối.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò phải trả FALSE cho một interceptor không hề tồn tại")]
    public void WiringDetector_Reports_NotWired_For_NonExistentInterceptor()
    {
        var wired = WiredInterceptorArguments();

        Assert.DoesNotContain(wired,
            args => args.Contains("NeverWiredProbeInterceptor", StringComparison.Ordinal));
    }

    /// <summary>
    /// Đối chứng 2 — chứng minh việc CẮT CHÚ THÍCH thật sự có tác dụng, trên chính bộ cắt mà test
    /// chính đang dùng (<see cref="RepoSourceTree.RemoveComments"/>), chứ không phải trên một bản
    /// thứ hai viết riêng cho test.
    ///
    /// <para>Đây là ca đối chứng quan trọng nhất của file này: repo nhắc tên
    /// <c>AuditInterceptor</c> ở hàng chục dòng chú thích, nên nếu phép cắt hỏng, test chính sẽ
    /// xanh MÃI MÃI kể cả sau khi dòng nối thật bị xoá — đúng kịch bản mà test này sinh ra để
    /// ngăn.</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: nhắc tên interceptor trong CHÚ THÍCH không được tính là đã nối")]
    public void WiringDetector_Ignores_MentionsInsideComments()
    {
        const string mentionedOnlyInComments = """
            // options.AddInterceptors(sp.GetRequiredService<GhostInterceptor>());
            /* Trước đây ta nối GhostInterceptor bằng AddInterceptors(new GhostInterceptor()). */
            services.AddScoped<GhostInterceptor>();
            """;

        const string actuallyWired = """
            options.AddInterceptors(sp.GetRequiredService<GhostInterceptor>());
            """;

        Assert.Empty(InterceptorArgumentsIn(RepoSourceTree.RemoveComments(mentionedOnlyInComments)));

        var wired = InterceptorArgumentsIn(RepoSourceTree.RemoveComments(actuallyWired));
        Assert.Contains(wired, args => args.Contains("GhostInterceptor", StringComparison.Ordinal));
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>Nguồn KỲ VỌNG: reflection trên assembly sản phẩm — độc lập với văn bản mã nguồn.</summary>
    private static List<Type> DeclaredInterceptorTypes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .Where(type => type.IsClass
                        && !type.IsAbstract
                        && typeof(SaveChangesInterceptor).IsAssignableFrom(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];

    /// <summary>
    /// Nguồn THỰC TẾ: nội dung đối số của MỌI lời gọi <c>AddInterceptors(...)</c> trong mã nguồn
    /// sản phẩm (Core + Api), đã cắt chú thích.
    /// </summary>
    private static List<string> WiredInterceptorArguments()
    {
        var arguments = new List<string>();

        foreach (var file in RepoSourceTree.ProductSourceFiles())
            arguments.AddRange(InterceptorArgumentsIn(RepoSourceTree.ReadCodeWithoutComments(file)));

        return arguments;
    }

    /// <summary>
    /// Trích phần trong ngoặc của từng <c>AddInterceptors(</c> bằng phép ĐẾM NGOẶC CÂN BẰNG, chứ
    /// không bằng regex "tới dấu <c>)</c> đầu tiên".
    ///
    /// <para>Bắt buộc phải cân bằng ngoặc vì khuôn thật của repo có ngoặc lồng:
    /// <c>AddInterceptors(sp.GetRequiredService&lt;AuditInterceptor&gt;())</c> — regex ngây thơ sẽ
    /// cắt ở dấu <c>)</c> của <c>GetRequiredService()</c> và bỏ mất phần còn lại. Với dạng nhiều
    /// đối số <c>AddInterceptors(a, b)</c> thì cả hai đều nằm trong đoạn trích, đúng ý.</para>
    ///
    /// <para>Giới hạn đã biết: không phân biệt <c>(</c> nằm trong chuỗi ký tự. Chưa xảy ra trong
    /// repo, và cần đúng tuyệt đối thì phải nhấc sang Roslyn — giá không xứng với một luật.</para>
    /// </summary>
    private static List<string> InterceptorArgumentsIn(string code)
    {
        const string marker = "AddInterceptors";
        var results = new List<string>();

        for (var index = code.IndexOf(marker, StringComparison.Ordinal);
             index >= 0;
             index = code.IndexOf(marker, index + marker.Length, StringComparison.Ordinal))
        {
            var open = index + marker.Length;
            while (open < code.Length && char.IsWhiteSpace(code[open])) open++;
            if (open >= code.Length || code[open] != '(') continue;

            var depth = 0;
            for (var cursor = open; cursor < code.Length; cursor++)
            {
                if (code[cursor] == '(') depth++;
                else if (code[cursor] == ')' && --depth == 0)
                {
                    results.Add(code[(open + 1)..cursor]);
                    break;
                }
            }
        }

        return results;
    }
}
