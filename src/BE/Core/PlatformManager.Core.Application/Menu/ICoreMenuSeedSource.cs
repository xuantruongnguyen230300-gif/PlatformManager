namespace PlatformManager.Core.Application.Menu;

/// <summary>
/// Seam "dữ liệu menu của dự án" — <b>host cung cấp, Core tiêu thụ</b>.
///
/// <para><b>Vì sao tồn tại</b> (tách 2026-09-02): trước đó <c>CoreSeeder.SeedMenuAsync</c> khai
/// cứng 4 mục menu kèm nhãn tiếng Việt, route FE và class icon của CHÍNH dự án này. Cả ba thứ đó
/// chắc chắn khác ở dự án thứ hai, nên muốn dùng lại CoreBase thì phải sửa vào TRONG Core — đúng
/// thứ mà định nghĩa "CoreBase xong" loại trừ. Ranh giới đã chốt: <b>Core giữ CƠ CHẾ, dự án cung
/// cấp DỮ LIỆU</b> (doc/kien-truc-core-module.md).</para>
///
/// <para><b>Phải đăng ký ở host; Core cố ý KHÔNG có hiện thực mặc định.</b> Một bản mặc định trả
/// danh sách rỗng sẽ biến "quên đăng ký" thành một database không mục menu nào và không lỗi nào —
/// hỏng trong im lặng, đúng lớp lỗi mà seeder này đã trả giá một lần (xem
/// <c>CoreSeeder.UpsertMenuAsync</c>). Thiếu đăng ký ⇒ <c>CoreSeeder</c> không phân giải được từ
/// DI ⇒ lệnh <c>--seed</c> thoát khác 0 ngay trước khi ghi bất cứ thứ gì.</para>
///
/// <para>Menu NGHIỆP VỤ của một module tương lai KHÔNG khai qua đây — mỗi module tự có seeder
/// riêng cho dữ liệu của nó (doc/kien-truc-core-module.md).</para>
/// </summary>
public interface ICoreMenuSeedSource
{
    /// <summary>
    /// Toàn bộ mục menu Core của dự án, dạng DANH SÁCH PHẲNG — quan hệ cha-con nằm ở
    /// <see cref="MenuSeedItem.ParentCode"/>, không lồng cấu trúc (cùng shape với response
    /// <c>GET /api/meta/menu</c>, xem doc/contracts/meta-menu.md).
    ///
    /// <para>Thứ tự khai KHÔNG bắt buộc cha-trước-con: seeder xử lý mọi mục gốc
    /// (<c>ParentCode = null</c>) trước, rồi tới phần còn lại theo đúng thứ tự host khai.</para>
    ///
    /// <para>Đồng bộ có chủ đích — đây là bảng hằng số của một dự án, không phải thứ đọc từ I/O.
    /// Nguồn cần I/O là một cơ chế khác, không phải seam này.</para>
    /// </summary>
    IReadOnlyCollection<MenuSeedItem> GetMenuItems();
}
