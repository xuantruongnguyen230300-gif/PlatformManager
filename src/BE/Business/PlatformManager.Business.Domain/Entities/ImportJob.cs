using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Theo dõi MỘT lần import — spec/danh-muc-dti/business-rules.md §1.5 (Q45). Bảng
/// <b>trạng thái tiến trình</b>, nhưng vẫn là bảng NGHIỆP VỤ chứ không phải bảng Core: Core chỉ
/// giữ cơ chế chạy job (<c>IBackgroundJobScheduler</c>) và lưu file; bảng theo dõi thuộc tầng
/// nghiệp vụ dùng nó (luật 3, doc/cau-truc-database-dti.md).
///
/// <para><b>Entity của VÒNG 2.</b> Vòng 1 của cụm DTI chỉ có đường ĐỌC, nên hôm nay chưa handler
/// nào dựng <see cref="ImportJob"/>. Nó có mặt ngay từ lượt này vì bảng của nó phải nằm trong
/// CÙNG migration đầu tiên với ba bảng kia — thêm bảng sau là thêm một migration cho một quyết
/// định đã chốt xong từ trước.</para>
/// </summary>
public class ImportJob : BaseEntity
{
    /// <summary>Tên file người dùng gửi.</summary>
    public string FileName { get; private set; } = string.Empty;

    public string Format { get; private set; } = string.Empty;

    /// <summary>ĐƯỜNG DẪN file tạm — KHÔNG lưu nội dung file.</summary>
    public string StoragePath { get; private set; } = string.Empty;

    public string Status { get; private set; } = ImportJobStatuses.Pending;

    /// <summary><c>result</c> của DM-7 bước 2, dạng JSON.</summary>
    public string? ResultJson { get; private set; }

    /// <summary>Chỉ có khi <see cref="Status"/> là <c>Failed</c> — dev-facing.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// <c>businessCode</c> của lỗi CẢ FILE, khi lỗi đó có mã nghiệp vụ — thêm 2026-09-11 cùng Q75.
    ///
    /// <para><b>Lỗ hổng nó bịt:</b> trước đó nhánh <c>Failed</c> chỉ có <see cref="ErrorMessage"/>,
    /// thứ hợp đồng khai thẳng là <i>dev-facing, KHÔNG để hiển thị</i>. Nghĩa là mọi lỗi cả file —
    /// thiếu cột bắt buộc, vượt trần số dòng — đều không có gì để FE dịch thành câu cho người dùng.
    /// Một trần mà người dùng chạm phải nhưng không đọc được lý do thì chẳng khác gì không có.</para>
    ///
    /// <para><c>null</c> cho lỗi hạ tầng thuần (job crash, file hỏng ở mức byte): chúng không có mã
    /// nghiệp vụ để dịch, và đó là chủ đích.</para>
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// CHỦ NHẬT của tuần ISO đích, ĐÃ quy đổi — không bao giờ mang nghĩa <c>"all"</c> (Q45).
    ///
    /// <para><b>Vì sao lưu tuần đích, và vì sao quy đổi LÚC NHẬN REQUEST:</b> <c>"all"</c> nghĩa
    /// là <i>tuần hiện tại tại lúc người dùng bấm Nhập</i>. Job nền chạy sau đó — quy lúc job
    /// chạy thì một file bấm lúc 23:59 Chủ nhật mà worker nhặt lúc 00:01 thứ Hai sẽ đổ vào tuần
    /// SAU.</para>
    ///
    /// <para><b>Vì sao kiểu <c>date</c> chứ không phải chuỗi <c>"YYYY-Www"</c>:</b> mô hình này
    /// không lưu kỳ dạng chuỗi ở đâu cả — kỳ luôn suy ra từ một <c>date</c> qua lịch ISO; giá trị
    /// lưu chính là neo dự phòng của §5.3; và Postgres kiểm được kiểu (<c>CHECK ISODOW = 7</c>),
    /// còn một cột chuỗi thì nhận cả <c>"2026-W99"</c>.</para>
    /// </summary>
    public DateOnly TargetWeekEnd { get; private set; }

    private ImportJob() { }

    public static ImportJob Create(string fileName, string format, string storagePath, DateOnly targetWeekEnd)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException(ImportJobDomainErrors.FileNameRequired);
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new DomainException(ImportJobDomainErrors.StoragePathRequired);
        if (targetWeekEnd.DayOfWeek != DayOfWeek.Sunday)
            throw new DomainException(ImportJobDomainErrors.TargetWeekEndNotSunday);

        return new ImportJob
        {
            FileName = fileName.Trim(),
            Format = (format ?? string.Empty).Trim(),
            StoragePath = storagePath.Trim(),
            TargetWeekEnd = targetWeekEnd,
            Status = ImportJobStatuses.Pending,
        };
    }

    public void MarkRunning() => Status = ImportJobStatuses.Running;

    public void MarkSucceeded(string resultJson)
    {
        Status = ImportJobStatuses.Succeeded;
        ResultJson = resultJson;

        // Dọn CẢ HAI trường lỗi: một lượt chạy lại sau thất bại mà còn giữ mã lỗi cũ sẽ báo
        // "thành công" kèm một mã lỗi — FE không có cách nào hiểu đúng payload đó.
        ErrorMessage = null;
        ErrorCode = null;
    }

    /// <param name="errorCode">
    /// <c>businessCode</c> để FE dịch, hoặc <c>null</c> với lỗi hạ tầng thuần. Đây là tham số
    /// TUỲ CHỌN chứ không bắt buộc có chủ đích: ép mọi lỗi phải có mã sẽ đẻ ra một mã rác kiểu
    /// <c>IMPORT.UNKNOWN</c> cho mọi exception không lường trước, và một mã như vậy không dịch
    /// được thành câu nào hữu ích hơn câu chung.
    /// </param>
    public void MarkFailed(string errorMessage, string? errorCode = null)
    {
        Status = ImportJobStatuses.Failed;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}
