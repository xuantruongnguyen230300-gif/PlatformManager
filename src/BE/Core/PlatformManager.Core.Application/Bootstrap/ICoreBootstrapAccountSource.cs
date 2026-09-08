namespace PlatformManager.Core.Application.Bootstrap;

/// <summary>
/// Seam "danh tính hiển thị của 2 tài khoản bootstrap" — <b>host cung cấp, Core tiêu thụ</b>.
///
/// <para><b>Vì sao tồn tại</b> (tách 2026-09-02): trước đó <c>CoreSeeder.SeedBootstrapUserAsync</c>
/// khai cứng 2 email mang tên miền nội bộ và 2 tên hiển thị tiếng Việt của CHÍNH dự án này. Tên
/// miền và ngôn ngữ là hai thứ chắc chắn khác ở dự án thứ hai dựng trên cùng CoreBase, nên
/// để chúng trong Core nghĩa là muốn tái dùng Core thì phải mổ vào trong Core — đúng thứ mà định
/// nghĩa "CoreBase xong" loại trừ. Ranh giới đã chốt: <b>Core giữ CƠ CHẾ, dự án cung cấp DỮ
/// LIỆU</b> (doc/kien-truc-core-module.md §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU").</para>
///
/// <para><b>Vì sao HAI THUỘC TÍNH chứ không phải một danh sách.</b> Đây là khác biệt cố ý so với
/// <c>ICoreMenuSeedSource</c> — cùng một ranh giới nhưng KHÔNG cùng hình dạng. Việc hệ thống có
/// đúng hai tài khoản bootstrap, mỗi tài khoản đúng MỘT vai, là quyết định least-privilege
/// 2026-08-24 (SuperAdmin thuần break-glass, Admin thuần vận hành hàng ngày) chứ không phải một
/// con số ngẫu nhiên. Phơi ra <c>IEnumerable&lt;…&gt;</c> sẽ cho phép dự án thứ hai khai một tài
/// khoản mang cả hai vai, hoặc năm tài khoản SuperAdmin — đúng thứ quyết định kia sinh ra để chặn,
/// và chặn lại được thì phải viết thêm guard runtime. Hai thuộc tính đặt tên sẵn thì
/// <b>compiler</b> ép, không guard nào phải chạy.</para>
///
/// <para><b>Cái gì KHÔNG đi qua seam này</b> — phần Core giữ lại vì nó là cơ chế, không phải dữ
/// liệu của dự án:</para>
/// <list type="bullet">
///   <item>Tên đăng nhập <c>"SuperAdmin"</c>/<c>"Admin"</c>: chúng soi gương
///   <c>Roles.SuperAdmin</c>/<c>Roles.Admin</c>, là khái niệm của Core. Mọi guard phân quyền dựa
///   vào VAI chứ không dựa vào tên (kiểm 2026-09-02, xem <c>SuperAdminAccountGuard</c>).</item>
///   <item><c>MustChangePassword = true</c> cho cả hai — mật khẩu bootstrap chỉ dùng một lần.</item>
///   <item>Mật khẩu: đọc từ <c>BootstrapOptions</c>, và <b>options đó KHÔNG đổi theo lượt tách
///   này</b>. Nếu số lượng tài khoản trở thành dữ liệu của host thì mật khẩu phải tra theo khoá,
///   kéo theo biến môi trường, tài liệu vận hành và phép nghiệm thu triển khai phải sửa hết.</item>
/// </list>
///
/// <para><b>Phải đăng ký ở host; Core cố ý KHÔNG có hiện thực mặc định.</b> Cùng khuôn với
/// <c>ICoreMenuSeedSource</c>, nhưng hậu quả của một bản mặc định ở đây nặng hơn hẳn: một email
/// mặc định biến "host quên đăng ký" thành một tài khoản quản trị THẬT mang tên miền của dự án
/// khác, đã nằm trong bảng và đã có role — không phải một dòng cấu hình sửa lại được bằng cách
/// khai lại (<c>CoreSeeder</c> thấy tài khoản đã tồn tại thì không ghi đè). Thiếu đăng ký ⇒
/// <c>CoreSeeder</c> không phân giải được từ DI ⇒ lệnh <c>--seed</c> thoát khác 0 ngay trước khi
/// ghi bất cứ thứ gì.</para>
/// </summary>
public interface ICoreBootstrapAccountSource
{
    /// <summary>Hồ sơ của tài khoản mang vai <c>Roles.SuperAdmin</c> — break-glass.</summary>
    BootstrapAccountProfile SuperAdmin { get; }

    /// <summary>Hồ sơ của tài khoản mang vai <c>Roles.Admin</c> — vận hành hàng ngày.</summary>
    BootstrapAccountProfile Admin { get; }
}
