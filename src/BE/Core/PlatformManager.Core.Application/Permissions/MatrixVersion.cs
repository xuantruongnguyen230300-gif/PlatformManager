using System.Security.Cryptography;
using System.Text;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Token phiên bản cấp TẬP HỢP cho hai ma trận phân quyền — hàm băm của CHÍNH dữ liệu, không
/// phải cột đếm. Quy trình: <c>GET</c> trả token của trạng thái hiện tại, <c>PUT</c> gửi lại
/// token đó, server TÍNH LẠI từ DB rồi so; lệch ⇒ 409 và KHÔNG ghi gì. Xem
/// doc/huong_dan/wiki-core/be/06-concurrency-control.md §"Cách chuẩn cho ca ghi đè cả tập" và
/// doc/contracts/permissions.md §"Quyết định người dùng 2026-08-30".
///
/// <para><b>Vì sao băm dữ liệu chứ không phải cột phiên bản:</b> không có trạng thái phụ để lệch
/// với dữ liệu thật, và nó phát hiện được MỌI thay đổi — kể cả do seeder hay SQL tay gây ra, thứ
/// mà một bộ đếm chỉ thấy khi có ai nhớ tăng nó.</para>
///
/// <para><b>Hai bất biến mà nơi gọi phải giữ, vì hàm này không kiểm được:</b></para>
/// <list type="number">
///   <item>Tập token đưa vào PHẢI chỉ gồm dòng ĐANG SỐNG (<c>IsDeleted = false</c>). Kể từ
///     2026-08-31 hai bảng ma trận xoá MỀM, nên đọc bằng <c>IgnoreQueryFilters()</c> sẽ trộn cả
///     lịch sử vào token — khi đó token đổi sau mỗi lần lưu kể cả khi ma trận không đổi, và mọi
///     <c>PUT</c> hợp lệ đều thành 409. Bẫy này ghi ở be/06-concurrency-control.md.</item>
///   <item>Mỗi token phải mô tả ĐỦ một dòng (cả hai cột của cặp), nếu không hai ma trận khác nhau
///     có thể ra cùng chuỗi.</item>
/// </list>
///
/// <para>Thứ tự do CHÍNH hàm này áp đặt (<see cref="StringComparer.Ordinal"/>), không phụ thuộc
/// <c>ORDER BY</c> của DB hay collation của Postgres — cùng nội dung phải luôn ra cùng chuỗi, kể
/// cả khi DB trả về theo thứ tự khác.</para>
/// </summary>
public static class MatrixVersion
{
    /// <summary>Chuỗi trả cho trạng thái RỖNG — vẫn là một giá trị hợp lệ, không phải chuỗi rỗng:
    /// ma trận chưa có dòng nào cũng là một trạng thái mà người khác có thể vừa ghi đè lên.</summary>
    public static string Compute(IEnumerable<string> rowTokens)
    {
        var canonical = string.Join('\n', rowTokens.Order(StringComparer.Ordinal));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));

        // Hex thường, không Base64: token này đi qua JSON và có thể lọt vào URL/log khi debug —
        // Base64 mang '+' và '/' nên phải nhớ escape ở mọi chỗ đó.
        return Convert.ToHexStringLower(hash);
    }
}
