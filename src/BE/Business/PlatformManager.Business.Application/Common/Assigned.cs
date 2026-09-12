namespace PlatformManager.Business.Application.Common;

/// <summary>
/// "Client CÓ gửi trường này không" — phân biệt <b>vắng mặt</b> với <b>gửi lên giá trị rỗng</b>.
///
/// <para><b>Vì sao cần một kiểu riêng thay vì dùng <c>null</c>:</b> luật ghi ở
/// spec/danh-muc-dti/business-rules.md §5.3 bước 2 phân biệt đúng hai ca đó — trường <b>không</b>
/// nằm trong request thì được <i>copy-forward</i> từ bản ghi kỳ trước, còn trường có mặt mang giá
/// trị <c>null</c> thì <b>xoá trắng</b>. Với một trường vốn đã nullable
/// (<c>decimal?</c>/<c>string?</c>), <c>null</c> không diễn đạt được cả hai nghĩa cùng lúc.</para>
///
/// <para>Ca đã trả giá nếu gộp hai nghĩa: sửa inline (DM-6) chỉ gửi 2 trường; nếu 6 trường còn lại
/// bị hiểu là "gửi null" thì một lần sửa <c>Tiến độ %</c> sẽ xoá trắng điểm số của kỳ đó và
/// Dashboard tụt về 0 — đúng lỗi mà copy-forward sinh ra để chặn.</para>
///
/// <para><b>Tham số kiểu khai sẵn dạng nullable</b> — dùng <c>Assigned&lt;decimal?&gt;</c>,
/// <c>Assigned&lt;string?&gt;</c>. Để <c>T</c> không nullable rồi thêm <c>?</c> ở từng chỗ dùng sẽ
/// biến mọi phép gộp giá trị thành một phép toán trên <c>T</c> không ràng buộc, thứ C# không cho
/// dùng <c>??</c>.</para>
/// </summary>
/// <param name="IsSet">Client có gửi trường này không.</param>
/// <param name="Value">Giá trị đã gửi; vô nghĩa khi <paramref name="IsSet"/> là <c>false</c>.</param>
public readonly record struct Assigned<T>(bool IsSet, T Value)
{
    /// <summary>Không gửi ⇒ copy-forward khi tạo bản ghi mới, giữ nguyên khi cập nhật.</summary>
    public static Assigned<T> Unset => default;

    /// <summary>Có gửi — kể cả khi giá trị là <c>null</c> (xoá trắng có chủ đích).</summary>
    public static Assigned<T> Set(T value) => new(true, value);
}
