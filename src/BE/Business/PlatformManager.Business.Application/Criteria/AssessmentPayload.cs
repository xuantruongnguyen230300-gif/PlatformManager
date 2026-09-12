using PlatformManager.Business.Application.Common;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Object <c>assessment</c> lồng trong request của DM-3 và DM-4 — <b>6 trường đánh giá</b> của Q9.
///
/// <para><b>Vì sao là object LỒNG chứ không phải 6 trường phẳng trộn với 4 trường danh mục</b>
/// (doc/contracts/danh-muc-dti.md DM-4): hai nhóm ghi vào HAI BẢNG khác nhau với vòng đời khác
/// nhau, và <c>assessment</c> <b>vắng mặt</b> phải phân biệt được với <c>assessment</c> có mọi
/// trường <c>null</c> — ca thứ hai là "xoá trắng dữ liệu đánh giá", ca thứ nhất là "không đụng
/// tới". Một object lồng nullable diễn đạt được cả hai; sáu trường phẳng thì không.</para>
///
/// <para><b>Từng trường dùng <see cref="Assigned{T}"/>, không phải kiểu nullable trần</b> — Q74,
/// spec/danh-muc-dti/business-rules.md §6.2. Khoá VẮNG MẶT khỏi JSON ⇒ copy-forward; khoá CÓ MẶT
/// mang <c>null</c> ⇒ xoá trắng. Đây là cùng một luật mà đường import áp cho "cột vắng khỏi file"
/// và "ô trống trong cột có mặt"; ba đường ghi phải trả lời giống nhau cho cùng câu hỏi.</para>
///
/// <para><c>ProgressPercent</c> KHÔNG có ở đây, và đó là ràng buộc hợp đồng chứ không phải bỏ sót:
/// nó chỉ sửa được qua đường inline (DM-6). Ranh giới trường giữa hai đường ghi là chốt Q9, không
/// được nới.</para>
/// </summary>
/// <param name="Period">
/// KỲ ĐÍCH, BẮT BUỘC khi có object này: <c>"YYYY-Www"</c> hoặc <c>"all"</c>. Đi trong THÂN request
/// chứ không trong query string, vì đây là <b>dữ liệu của lời ghi</b> — thứ server ghi theo, không
/// phải trạng thái màn hình để server từ chối.
/// </param>
/// <param name="Year">Bắt buộc khi <paramref name="Period"/> là <c>"all"</c> (T15).</param>
/// <param name="Version">Token đọc ở DM-2; vắng mặt = không kiểm concurrency.</param>
public sealed record AssessmentPayload(
    string? Period = null,
    int? Year = null,
    string? Version = null)
{
    public Assigned<decimal?> SelfScore { get; init; }

    public Assigned<decimal?> VerifiedScore { get; init; }

    /// <summary>Phải là 1 trong 4 giá trị của §4; giá trị lạ ⇒ <c>400 CRITERIA.STATUS_INVALID</c>.</summary>
    public Assigned<string?> Status { get; init; }

    public Assigned<Guid?> OwnerId { get; init; }

    public Assigned<DateOnly?> Deadline { get; init; }

    public Assigned<string?> Note { get; init; }
}
