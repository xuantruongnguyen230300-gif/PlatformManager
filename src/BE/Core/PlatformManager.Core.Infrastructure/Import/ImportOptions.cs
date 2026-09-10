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
}
