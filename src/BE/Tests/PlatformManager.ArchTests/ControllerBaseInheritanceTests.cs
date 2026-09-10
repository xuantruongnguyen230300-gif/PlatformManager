using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Core.Api;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật:</b> (1) mọi controller phải kế thừa <see cref="ApiControllerBase"/>, không kế thừa
/// thẳng <see cref="ControllerBase"/>; (2) mọi <c>[AllowAnonymous]</c> phải nằm trong allowlist
/// tường minh dưới đây.
///
/// <para><b>Lỗi ĐÃ XẢY RA THẬT trong repo này:</b> <c>AuthController.Logout</c> từng thiếu CẢ
/// <c>[Authorize]</c> lẫn <c>[AllowAnonymous]</c> nên <b>vô tình public</b> — endpoint đăng xuất
/// gọi được bởi người chưa đăng nhập. Sự cố đó là lý do <c>[Authorize]</c> được đưa lên
/// <see cref="ApiControllerBase"/> (fail-closed mặc định) thay vì để mỗi controller tự khai
/// (fail-open). Xem docstring của <see cref="ApiControllerBase"/>.</para>
///
/// <para><b>Vì sao vẫn cần test dù đã fail-closed:</b> cơ chế fail-closed chỉ có tác dụng với
/// controller CÓ kế thừa base. Một controller mới viết <c>: ControllerBase</c> — đúng khuôn mọi
/// hướng dẫn ASP.NET Core trên mạng, đúng thứ IDE gợi ý, biên dịch sạch — sẽ rơi thẳng về trạng
/// thái fail-open cũ. Và controller mới, theo định nghĩa, là controller <b>chưa có integration
/// test nào</b>: không có gì khác đang canh nó.</para>
///
/// <para><b>Vì sao allowlist chứ không phải "đếm cho vui":</b> <c>[AllowAnonymous]</c> là công
/// tắc GHI ĐÈ — nó thắng mọi <c>[Authorize]</c> ở cấp trên, kể cả cái ở base class. Nó là đường
/// duy nhất mở một endpoint ra Internet, nên nó phải là quyết định CÓ NGƯỜI NHÌN. Allowlist biến
/// việc thêm một attribute (thao tác 1 dòng, review dễ lướt qua) thành việc phải sửa một file
/// test kèm lý do (thao tác không lướt qua được).</para>
///
/// <para><b>Đánh đổi:</b> reflection trên assembly đã biên dịch — thấy đúng thứ runtime thấy, kể
/// cả controller kế thừa qua nhiều tầng hoặc sinh tự động, và không giòn với cách gõ code. Giá
/// phải trả: chỉ thấy được attribute ĐẶT TRỰC TIẾP trên class/method, không thấy quyền suy ra từ
/// convention hay filter đăng ký toàn cục (<c>AddControllers(o =&gt; o.Filters.Add(...))</c>) —
/// những đường đó phải canh bằng integration test, không canh được từ đây.</para>
/// </summary>
public class ControllerBaseInheritanceTests
{
    /// <summary>
    /// Danh sách endpoint được phép mở cho người CHƯA đăng nhập, dạng <c>Controller.Action</c>
    /// (hoặc chỉ <c>Controller</c> nếu attribute đặt ở cấp class).
    ///
    /// <para><b>Thêm một dòng vào đây là quyết định bảo mật, không phải sửa test cho xanh.</b>
    /// Kèm lý do ngay cạnh, và trả lời được: endpoint này lộ ra dữ liệu gì cho người lạ, và nó có
    /// hàng rào chống lạm dụng nào (rate limit) chưa.</para>
    /// </summary>
    private static readonly HashSet<string> AnonymousAllowlist = new(StringComparer.Ordinal)
    {
        // Đăng nhập: theo định nghĩa phải gọi được khi chưa có phiên. Có hàng rào riêng —
        // [EnableRateLimiting("login")] cộng dồn với GlobalLimiter.
        "AuthController.Login",
    };

    [Fact(DisplayName = "Mọi controller đều kế thừa ApiControllerBase (không kế thừa thẳng ControllerBase)")]
    public void EveryController_Inherits_ApiControllerBase()
    {
        var controllers = ConcreteControllerTypes();

        // Chặn "pass rỗng": assembly Api không nạp được ⇒ tập rỗng ⇒ xanh vĩnh viễn.
        Assert.True(controllers.Count > 0,
            "Không tìm thấy controller cụ thể nào trong các assembly sản phẩm ⇒ test này không đo gì. " +
            "Nguyên nhân thường gặp: assembly PlatformManager.Api không nạp được (kiểm ProductAssemblies.All), " +
            "hoặc ProjectReference tới PlatformManager.Api đã bị gỡ khỏi PlatformManager.ArchTests.csproj.");

        var rogue = controllers
            .Where(type => !typeof(ApiControllerBase).IsAssignableFrom(type))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(rogue.Count == 0,
            "Controller kế thừa THẲNG ControllerBase, bỏ qua ApiControllerBase: " +
            string.Join(", ", rogue) + ". Hệ quả: (a) mất [Authorize] mặc định ⇒ mọi action của nó " +
            "PUBLIC mà không có lỗi biên dịch nào — đúng sự cố AuthController.Logout đã xảy ra; " +
            "(b) mất HandleResult ⇒ controller tự map status code, envelope trả về lệch chuẩn. " +
            "Cách sửa: đổi `: ControllerBase` thành `: ApiControllerBase` (using PlatformManager.Core.Api) " +
            "và trả kết quả qua HandleResult(await mediator.Send(...)). Xem " +
            "doc/huong_dan/quy-uoc/be-api-controller.md §Dispatcher.");
    }

    [Fact(DisplayName = "Mọi [AllowAnonymous] đều nằm trong allowlist tường minh")]
    public void EveryAllowAnonymous_IsOn_TheAllowlist()
    {
        var controllers = ConcreteControllerTypes();
        Assert.True(controllers.Count > 0,
            "Không tìm thấy controller cụ thể nào ⇒ test này không đo gì. Xem thông điệp của " +
            "EveryController_Inherits_ApiControllerBase.");

        var anonymous = AnonymousEndpoints(controllers);

        // Chặn "pass rỗng" ở chiều ngược lại: hôm nay repo CÓ đúng 1 chỗ [AllowAnonymous] thật.
        // Tập rỗng ở đây nghĩa là bộ dò attribute hỏng (nạp nhầm kiểu AllowAnonymousAttribute từ
        // assembly khác chẳng hạn), chứ không phải repo bỗng dưng sạch.
        Assert.True(anonymous.Count > 0,
            "Không phát hiện [AllowAnonymous] ở đâu cả. Repo phải có ÍT NHẤT một chỗ (đăng nhập), nên " +
            "tập rỗng nghĩa là bộ dò attribute đang hỏng — và khi đó test này xanh mà không đo gì. " +
            "Nếu endpoint đăng nhập thật sự đã đổi cơ chế (không còn [AllowAnonymous]) thì cập nhật " +
            "AnonymousAllowlist và cả assert này.");

        var unexpected = anonymous.Except(AnonymousAllowlist, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal).ToList();
        var stale = AnonymousAllowlist.Except(anonymous, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal).ToList();

        Assert.True(unexpected.Count == 0,
            "[AllowAnonymous] xuất hiện ở chỗ CHƯA ĐƯỢC DUYỆT: " + string.Join(", ", unexpected) +
            ". Attribute này GHI ĐÈ [Authorize] của ApiControllerBase, nên mỗi chỗ như vậy là một " +
            "endpoint mở ra Internet. Cách sửa: hoặc bỏ [AllowAnonymous] đi (mặc định fail-closed đã " +
            "đúng), hoặc thêm dòng tương ứng vào AnonymousAllowlist trong file này KÈM LÝ DO và kèm " +
            "hàng rào chống lạm dụng ([EnableRateLimiting]) nếu endpoint nhận input từ người lạ.");

        Assert.True(stale.Count == 0,
            "AnonymousAllowlist còn mục không còn tương ứng với code: " + string.Join(", ", stale) +
            ". Allowlist mục ruỗng sẽ dần mất ý nghĩa và người sau sẽ thôi tin nó. Cách sửa: xoá mục " +
            "thừa khỏi AnonymousAllowlist (endpoint đã đổi tên hoặc đã bị xoá).");
    }

    /// <summary>
    /// Đối chứng — chứng minh bộ dò attribute biết nói KHÔNG. Nếu nó trả "có [AllowAnonymous]" cho
    /// mọi action thì test trên sẽ đỏ ầm ĩ (không nguy hiểm); nhưng nếu nó trả "không" cho mọi
    /// action thì test trên xanh vĩnh viễn — ca này bắt đúng chiều nguy hiểm đó bằng cách khẳng
    /// định nó phân biệt được HAI action trên CÙNG một controller.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò phân biệt được action CÓ và KHÔNG có [AllowAnonymous]")]
    public void AttributeDetector_Distinguishes_AnonymousFromProtectedActions()
    {
        var controllers = ConcreteControllerTypes();
        var anonymous = AnonymousEndpoints(controllers);

        var authControllers = controllers
            .Where(type => anonymous.Any(name => name.StartsWith(type.Name + ".", StringComparison.Ordinal)))
            .ToList();

        Assert.True(authControllers.Count > 0,
            "Không controller nào có action [AllowAnonymous] ⇒ ca đối chứng này không chứng minh được gì. " +
            "Xem thông điệp của EveryAllowAnonymous_IsOn_TheAllowlist.");

        foreach (var controller in authControllers)
        {
            var actions = PublicActions(controller).Select(method => method.Name).ToList();
            var openHere = anonymous
                .Where(name => name.StartsWith(controller.Name + ".", StringComparison.Ordinal))
                .Select(name => name[(controller.Name.Length + 1)..])
                .ToList();

            Assert.True(openHere.Count < actions.Count,
                $"Trên {controller.Name}, bộ dò báo MỌI action đều [AllowAnonymous] " +
                $"({openHere.Count}/{actions.Count}) ⇒ nó đang trả true vô điều kiện, và allowlist không " +
                "còn phân biệt được gì. Sửa bộ dò AnonymousEndpoints, ĐỪNG nới allowlist cho xanh.");
        }
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Mọi controller CỤ THỂ trong các assembly sản phẩm. Bỏ <c>IsAbstract</c> nên
    /// <see cref="ApiControllerBase"/> tự loại mình ra — nó là base, không phải controller.
    /// </summary>
    private static List<Type> ConcreteControllerTypes() =>
        [.. ProductAssemblies.AllLoadableTypes()
            .Where(type => type.IsClass
                        && !type.IsAbstract
                        && typeof(ControllerBase).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];

    /// <summary>
    /// Tập tên <c>Controller</c> (attribute ở cấp class) và <c>Controller.Action</c> (attribute ở
    /// cấp method) đang mang <see cref="AllowAnonymousAttribute"/>.
    ///
    /// <para><c>inherit: false</c> là CỐ Ý: chỉ tính attribute đặt TRỰC TIẾP tại đây. Nếu tính cả
    /// kế thừa thì một ngày nào đó có người đặt <c>[AllowAnonymous]</c> lên một base class và mọi
    /// controller con sẽ đồng loạt nhảy vào danh sách — thông điệp khi đó chỉ ra sai chỗ.</para>
    /// </summary>
    private static HashSet<string> AnonymousEndpoints(IEnumerable<Type> controllers)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var controller in controllers)
        {
            if (controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false).Length > 0)
                found.Add(controller.Name);

            foreach (var action in PublicActions(controller))
            {
                if (action.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false).Length > 0)
                    found.Add($"{controller.Name}.{action.Name}");
            }
        }

        return found;
    }

    /// <summary>
    /// Action công khai KHAI TRỰC TIẾP trên controller (<c>DeclaredOnly</c>) — không lấy phương
    /// thức thừa kế từ <see cref="ControllerBase"/> (<c>Ok()</c>, <c>StatusCode()</c>, …), vốn
    /// không phải endpoint và sẽ làm nhiễu mọi phép đếm.
    /// </summary>
    private static IEnumerable<System.Reflection.MethodInfo> PublicActions(Type controller) =>
        controller.GetMethods(System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);
}
