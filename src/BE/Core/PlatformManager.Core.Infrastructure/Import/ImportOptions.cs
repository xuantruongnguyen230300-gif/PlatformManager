using System.ComponentModel.DataAnnotations;

namespace PlatformManager.Core.Infrastructure.Import;

/// <summary>
/// Trần dung lượng file import. <b>Là CẤU HÌNH, không phải hằng số rải trong code</b> —
/// doc/huong_dan/wiki-core/be/15-import-export.md §2 "Trần dung lượng file upload".
///
/// <para><b>Vì sao 10 MB</b> (chốt Q12b, 2026-09-09): file tham chiếu thật của nghiệp vụ đầu tiên
/// là 62 dòng ≈ 19 KB, nên 10 MB rộng hơn khoảng hai bậc độ lớn so với ca dùng thật — đủ chỗ cho
/// một <c>.xlsx</c> mang định dạng nặng, ảnh nhúng hoặc vài chục nghìn dòng, mà vẫn chặn được ca
/// một người kéo nhầm file video vào ô upload.</para>
///
/// <para>⚠️ Trần của ứng dụng KHÔNG thay thế trần của tầng phục vụ: Kestrel và reverse proxy đều
/// có giới hạn thân request riêng, và nếu chúng thấp hơn thì người dùng nhận một lỗi hạ tầng cụt
/// lủn trước khi tới được đường này. Đặt hai chỗ khớp nhau khi triển khai —
/// doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md.</para>
/// </summary>
public sealed class ImportOptions
{
    public const string SectionName = "Import";

    /// <summary>
    /// Trần dung lượng, tính bằng byte. Mặc định 10 MB.
    ///
    /// <para><c>[Range]</c> chặn giá trị 0 hoặc âm: một trần bằng 0 sẽ từ chối MỌI file, và triệu
    /// chứng ("không nạp được file nào") không gợi được về nguyên nhân là một dòng cấu hình.</para>
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; init; } = 10L * 1024 * 1024;

    /// <summary>
    /// Trần SỐ DÒNG dữ liệu (không kể header). Mặc định 20.000 — <b>Q75, chốt 2026-09-11</b>.
    ///
    /// <para><b>Vì sao trần này KHÔNG thừa dù đã có <see cref="MaxFileSizeBytes"/>:</b> hai trần
    /// chặn hai thứ khác nhau — một cái chặn <i>byte đọc từ đĩa</i>, một cái chặn <i>đối tượng dựng
    /// trong bộ nhớ</i>. Một file 200 KB gồm toàn dòng ngắn vẫn có thể mang nửa triệu dòng.</para>
    ///
    /// <para><b>Con số ĐO ra, không chọn tròn</b> (phép tính đầy đủ ở
    /// doc/huong_dan/wiki-core/be/15-import-export.md §2): file tham chiếu thật có 62 dòng ≈ 19 KB
    /// ⇒ một dòng ≈ 314 byte ⇒ trần dung lượng 10 MB còn cho lọt ≈ 33.000 dòng. Trần số dòng phải
    /// nằm DƯỚI con số đó, nếu không nó không bao giờ chạy tới. Cận dưới là ca lớn nhất còn hợp lý
    /// (≈ 12.000 dòng nếu về sau có người gộp nhiều đơn vị vào một file). 20.000 nằm giữa — dư
    /// khoảng 320 lần so với ca dùng thật.</para>
    ///
    /// <para>⚠️ Core KHÔNG tự cắt dòng thừa: nạp một file bị cắt cụt mà không ai biết còn tệ hơn từ
    /// chối nó. Bên gọi đọc trần này rồi TỪ CHỐI cả lượt.</para>
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxRows { get; init; } = 20_000;
}
