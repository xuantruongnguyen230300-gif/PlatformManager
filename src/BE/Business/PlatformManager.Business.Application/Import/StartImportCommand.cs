using FluentValidation;
using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Application.Storage;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// <b>DM-7 bước 1</b> — <c>POST /api/import</c>: nhận file, ghi ra kho tạm, tạo bản ghi theo dõi
/// rồi ĐẨY VIỆC RA CHẠY NỀN. Trả <c>jobId</c> để FE poll bước 2.
///
/// <para>⚠️ <b><see cref="Content"/> là <c>Stream</c>, KHÔNG phải <c>IFormFile</c>.</b> Tầng
/// Application bị cấm chạm <c>Microsoft.AspNetCore</c> (<c>LayerDependencyTests</c>), nên
/// controller mở stream rồi truyền xuống. Đây không phải thủ tục hình thức: nó cũng là thứ giữ cho
/// use case này kiểm được bằng unit test mà không phải dựng một request HTTP.</para>
///
/// <para><b>Stream PHẢI seek được.</b> Phép nhận diện định dạng đọc vài byte đầu rồi tua về 0
/// (<see cref="IImportFileReaderSelector"/>). <c>IFormFile.OpenReadStream()</c> luôn thoả — nội
/// dung form đã được ASP.NET đệm vào bộ nhớ hoặc đĩa trước khi tới controller.</para>
/// </summary>
/// <param name="Period">
/// Kỳ đích của TOÀN BỘ file: <c>"YYYY-Www"</c> hoặc <c>"all"</c>. Cùng luật với DM-4/DM-6 — MỘT
/// luật ghi cho cả ba đường, không phải ba luật.
///
/// <para>⚠️ Nạp một file CỦA KỲ CŨ trong lúc màn hình đang ở chế độ <c>Tất cả</c> sẽ đổ cả file vào
/// TUẦN HIỆN TẠI, không phải kỳ mà file nói tới — hệ thống không đọc kỳ từ nội dung file. Theo Q26
/// đây là hành vi ĐÚNG và không chặn; việc cho người dùng thấy kỳ đích trước khi bấm Nhập thuộc
/// màn hình.</para>
/// </param>
/// <param name="Year">Bắt buộc khi <paramref name="Period"/> là <c>"all"</c> (T15); bỏ qua khi là một tuần cụ thể.</param>
public sealed record StartImportCommand(
    Stream? Content,
    string? FileName,
    string? Period,
    int? Year) : ICommand<StartImportResultDto>;

/// <summary>
/// Chỉ kiểm thứ KHÔNG có mã nghiệp vụ riêng.
///
/// <para><b><c>period</c> CỐ Ý không ở đây</b> — bốn ca hỏng của nó (thiếu · sai khuôn · kỳ tháng ·
/// nhảy năm) mỗi ca một <c>businessCode</c> để FE bind được câu chữ, mà đường validator thì ra
/// <c>ValidationError</c> không mang mã nghiệp vụ nào. Handler kiểm và trả <c>Fail</c>.</para>
///
/// <para><b><c>year</c> thiếu khi <c>period = "all"</c> THÌ ở đây</b>, và đó đúng là thứ hợp đồng
/// yêu cầu: <i>"lỗi validate trường bắt buộc (<c>fields</c>), không cần một mã nghiệp vụ riêng"</i>
/// (DM-4, áp y hệt cho DM-7).</para>
/// </summary>
public sealed class StartImportValidator : AbstractValidator<StartImportCommand>
{
    public StartImportValidator()
    {
        // Cùng miền với GetCriteriaListValidator: `year` đi vào phép dựng khoảng ngày của kỳ, và
        // giá trị ngoài 1..9999 làm chỗ đó ném ArgumentOutOfRangeException — một tham số sai của
        // client thành lỗi 500 thay vì 400.
        RuleFor(x => x.Year)
            .InclusiveBetween(1, 9999)
            .When(x => x.Year.HasValue)
            .WithMessage("Năm phải nằm trong khoảng 1..9999.");

        RuleFor(x => x.Year)
            .NotNull()
            .When(x => string.Equals(x.Period?.Trim(), PeriodValues.All, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Chọn kỳ 'Tất cả' thì phải gửi kèm năm đang xem.");
    }
}

public sealed class StartImportHandler(
    IImportJobRepository jobs,
    IFileStorage storage,
    IImportFileReaderSelector readerSelector,
    IBackgroundJobScheduler scheduler,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<StartImportCommand, IApiResult<StartImportResultDto>>
{
    /// <summary>
    /// Thư mục cấp hai trong kho file (<c>uploads/danh-muc-dti/…</c>). Tên PHÂN HỆ, không phải tên
    /// bảng: file này thuộc nghiệp vụ Danh mục DTI, và khi có phân hệ thứ hai biết nạp file thì
    /// hai khu tách nhau sẵn.
    /// </summary>
    private const string StorageFeature = "danh-muc-dti";

    public async Task<IApiResult<StartImportResultDto>> Handle(StartImportCommand cmd, CancellationToken ct)
    {
        if (cmd.Content is null || string.IsNullOrWhiteSpace(cmd.FileName))
            return Fail<StartImportResultDto>(ImportErrors.FileMissing);

        var fileName = cmd.FileName.Trim();

        if (!cmd.Content.CanSeek)
        {
            // Không tới được từ đường HTTP (IFormFile luôn đệm sẵn). Ném thay vì trả 400: đây là
            // lỗi của NƠI GỌI, không phải của người dùng, và im lặng nuốt nó sẽ biến một bug thành
            // "file của tôi bị từ chối".
            throw new ArgumentException(
                "Stream file nạp phải seek được — phép nhận diện định dạng đọc vài byte đầu rồi tua về 0. " +
                "Nơi gọi phải chép sang FileStream/MemoryStream trước khi gửi command này.",
                nameof(cmd));
        }

        if (cmd.Content.Length == 0)
            return Fail<StartImportResultDto>(ImportErrors.FileEmpty, ("FileName", fileName));

        // Kỳ đích kiểm ở ĐÂY, lúc nhận request — KHÔNG phải lúc job chạy (Q45). "all" nghĩa là tuần
        // hiện tại TẠI LÚC NGƯỜI DÙNG BẤM NHẬP; quy đổi lúc worker nhặt job thì một file bấm lúc
        // 23:59 Chủ nhật mà worker chạy lúc 00:01 thứ Hai sẽ đổ vào tuần SAU.
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var period = WritePeriodResolver.Resolve(cmd.Period, cmd.Year, today);

        if (!period.IsAccepted)
            return Fail<StartImportResultDto>(PeriodError(period.Rejection), PeriodArgs(cmd, today));

        // Trần dung lượng + nhận diện định dạng bằng MAGIC BYTE, TRƯỚC khi ghi một byte nào xuống
        // kho (§2 của 15-import-export.md). Selector tự tua stream về 0 khi trả về.
        var selection = readerSelector.Select(cmd.Content, fileName);

        switch (selection.Rejection)
        {
            case ImportFileRejection.FileTooLarge:
                return Fail<StartImportResultDto>(
                    ImportErrors.FileTooLarge,
                    ("MaxFileSizeBytes", selection.MaxFileSizeBytes),
                    ("FileSizeBytes", selection.FileSizeBytes));

            case ImportFileRejection.UnsupportedFormat:
                return Fail<StartImportResultDto>(ImportErrors.FormatUnsupported, ("FileName", fileName));
        }

        // Tên file trên kho là một Id sinh mới, KHÔNG phải tên người dùng đặt: hai người nạp hai
        // file trùng tên là chuyện thường, và tên do người dùng đặt còn là một đường đưa ký tự tuỳ
        // ý xuống hệ thống tệp. Id này đi vào StoragePath nên bản ghi job vẫn tra ngược ra file.
        var storageKey = await storage.SaveAsync(
            FileStorageArea.Upload, StorageFeature, EntityId.New(), fileName, cmd.Content, ct);

        var job = ImportJob.Create(
            fileName,
            FormatNameOf(selection.Reader!),
            storageKey,
            period.TargetWeek!.End);

        jobs.Add(job);

        // LƯU TRƯỚC, ENQUEUE SAU — không đảo. Worker có thể nhặt job ngay lập tức; enqueue trước
        // khi dòng ImportJobs tồn tại là một cuộc đua mà phía thua im lặng ("job không tìm thấy"
        // trong worker, còn người dùng thì thấy một jobId poll mãi không đổi).
        await unitOfWork.SaveChangesAsync(ct);

        // Enqueue qua SEAM, không gọi thẳng Hangfire (LayerDependencyTests cưỡng chế). Danh tính
        // người bấm nút đi theo job nhờ BackgroundJobIdentityFilter TOÀN CỤC ở host (Q35) — không
        // nơi gọi nào phải "nhớ" truyền nó.
        //
        // CancellationToken.None cho lời gọi bên trong biểu thức: token của request HTTP chết ngay
        // khi response rời đi, còn job thì mới bắt đầu chạy.
        await scheduler.EnqueueAsync<IImportJobRunner>(runner => runner.RunAsync(job.Id, CancellationToken.None), ct);

        return Ok(new StartImportResultDto(job.Id));
    }

    /// <summary>
    /// Ánh xạ lý do từ chối kỳ sang catalog CỦA ĐƯỜNG IMPORT. Ba đường ghi dùng chung bộ giải
    /// (<see cref="WritePeriodResolver"/>) nhưng mỗi đường có bộ mã riêng — xem docstring của
    /// <see cref="WritePeriodRejection"/>.
    /// </summary>
    private static ErrorDescriptor PeriodError(WritePeriodRejection rejection) => rejection switch
    {
        WritePeriodRejection.Required => ImportErrors.PeriodRequired,
        WritePeriodRejection.NotWeekly => ImportErrors.PeriodNotWeekly,
        WritePeriodRejection.OutOfYear => ImportErrors.PeriodOutOfYear,
        _ => ImportErrors.PeriodInvalid,
    };

    /// <summary>
    /// Tham số câu lỗi. Chỉ gửi ra giá trị người dùng vừa CHỌN TRÊN MÀN HÌNH và năm hiện tại của
    /// server — đúng allowlist "chính sách/vị trí" của <c>MessageParamPolicy</c>.
    /// </summary>
    private static (string Name, object? Value)[] PeriodArgs(StartImportCommand cmd, DateOnly today) =>
    [
        ("Period", cmd.Period),
        ("Year", cmd.Year),
        ("CurrentYear", today.Year),
    ];

    /// <summary>
    /// Tên định dạng ghi vào <c>ImportJobs.Format</c> — lấy từ KIỂU READER ĐÃ ĐƯỢC CHỌN, không lấy
    /// từ phần mở rộng file.
    ///
    /// <para>Đây không phải chi tiết vặt: phần mở rộng do người dùng đặt và là thứ §2a của
    /// 15-import-export.md dạy đừng tin. Ghi lại reader nào đã đọc file thì cột này nói đúng thứ
    /// đã xảy ra — kể cả với một <c>.xlsx</c> bị đổi tên thành <c>.xls</c>.</para>
    /// </summary>
    private static string FormatNameOf(IImportFileReader reader)
    {
        const string suffix = "ImportFileReader";
        var typeName = reader.GetType().Name;

        return typeName.EndsWith(suffix, StringComparison.Ordinal)
            ? typeName[..^suffix.Length].ToUpperInvariant()
            : typeName;
    }
}
