using System.Linq.Expressions;
using NSubstitute;
using PlatformManager.Business.Application.Import;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Application.Storage;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>ĐƯỜNG NỐI của DM-7 bước 1</b> — phần mà <see cref="ImportJobRunnerTests"/> không chạm tới:
/// handler chọn <c>ErrorDescriptor</c> nào cho từng nhánh hỏng, ghi file trước hay sau khi kiểm
/// trần, và có enqueue đúng một lần không.
///
/// <para><b>Vì sao đường nối cần test riêng:</b> một handler gọi đúng bộ giải kỳ nhưng trả nhầm mã
/// (vd trả <c>PERIOD_INVALID</c> cho ca kỳ tháng) thì mọi test logic vẫn xanh — trong khi FE mất
/// đúng câu dẫn đường <i>"nạp theo từng tuần"</i>.</para>
/// </summary>
public class StartImportHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);

    private readonly FakeImportJobRepository _jobs = new();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IImportFileReaderSelector _selector = Substitute.For<IImportFileReaderSelector>();
    private readonly IBackgroundJobScheduler _scheduler = Substitute.For<IBackgroundJobScheduler>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public StartImportHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        _selector.Select(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns(new ImportFileReaderSelection(
                new FakeImportFileReader([]), ImportFileRejection.None, 19_000, 10L * 1024 * 1024, 20_000));

        _storage.SaveAsync(
                Arg.Any<FileStorageArea>(), Arg.Any<string>(), Arg.Any<Guid>(),
                Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/danh-muc-dti/abc.csv");
    }

    private StartImportHandler CreateHandler() =>
        new(_jobs, _storage, _selector, _scheduler, _unitOfWork, _clock);

    private static Stream File(int bytes = 64) => new MemoryStream(new byte[bytes]);

    private Task<IApiResult<StartImportResultDto>> SendAsync(
        Stream? content, string? fileName = "dti.csv", string? period = "2026-W33", int? year = null) =>
        CreateHandler().Handle(new StartImportCommand(content, fileName, period, year), CancellationToken.None);

    [Fact(DisplayName = "Ca thành công: tạo job Pending, lưu file, LƯU TRƯỚC rồi mới enqueue")]
    public async Task Success_CreatesJob_SavesBeforeEnqueue()
    {
        var result = await SendAsync(File());

        Assert.Equal(ErrorCode.Success, result.Code);

        var job = _jobs.Job!;
        Assert.Equal(job.Id, result.Data!.JobId);
        Assert.Equal("Pending", job.Status);
        Assert.Equal("uploads/danh-muc-dti/abc.csv", job.StoragePath);

        // Tuần 33/2026 ⇒ Chủ nhật 16/08. Cột này lưu MỐC ĐÃ QUY ĐỔI, không bao giờ mang nghĩa "all".
        Assert.Equal(new DateOnly(2026, 8, 16), job.TargetWeekEnd);
        Assert.Equal(DayOfWeek.Sunday, job.TargetWeekEnd.DayOfWeek);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _scheduler.Received(1).EnqueueAsync(
            Arg.Any<Expression<Func<IImportJobRunner, Task>>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "period = \"all\" + năm hiện tại ⇒ quy về TUẦN HIỆN TẠI lúc nhận request (Q26 + Q45)")]
    public async Task All_InCurrentYear_ResolvesToCurrentWeekAtRequestTime()
    {
        await SendAsync(File(), period: "all", year: 2026);

        Assert.Equal(new DateOnly(2026, 8, 16), _jobs.Job!.TargetWeekEnd);
    }

    [Fact(DisplayName = "Không có file ⇒ IMPORT.FILE_MISSING, không chạm kho và không enqueue")]
    public async Task NoFile_IsFileMissing()
    {
        var result = await SendAsync(content: null, fileName: null);

        Assert.Equal("IMPORT.FILE_MISSING", result.BusinessCode);
        Assert.Null(_jobs.Job);

        await _scheduler.DidNotReceive().EnqueueAsync(
            Arg.Any<Expression<Func<IImportJobRunner, Task>>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "File 0 byte ⇒ IMPORT.FILE_EMPTY (tách khỏi FILE_MISSING, hai cách sửa khác nhau)")]
    public async Task EmptyFile_IsFileEmpty()
    {
        var result = await SendAsync(File(bytes: 0));

        Assert.Equal("IMPORT.FILE_EMPTY", result.BusinessCode);
    }

    [Theory(DisplayName = "Bốn ca hỏng của period ⇒ bốn mã RỜI NHAU")]
    [InlineData(null, null, "IMPORT.PERIOD_REQUIRED")]
    [InlineData("2026-W99", null, "IMPORT.PERIOD_INVALID")]
    [InlineData("2026-08", null, "IMPORT.PERIOD_NOT_WEEKLY")]
    [InlineData("all", 2025, "IMPORT.PERIOD_OUT_OF_YEAR")]
    public async Task PeriodFailures_MapToDistinctCodes(string? period, int? year, string expected)
    {
        var result = await SendAsync(File(), period: period, year: year);

        Assert.Equal(expected, result.BusinessCode);
        Assert.Null(_jobs.Job);
    }

    /// <summary>
    /// Trần dung lượng phải chặn TRƯỚC khi ghi một byte nào xuống kho (Q12b + §2 của
    /// 15-import-export.md) — không chỉ trước khi đọc nội dung.
    /// </summary>
    [Fact(DisplayName = "Vượt trần ⇒ IMPORT.FILE_TOO_LARGE, và KHÔNG ghi gì xuống kho")]
    public async Task TooLarge_DoesNotTouchStorage()
    {
        _selector.Select(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns(new ImportFileReaderSelection(null, ImportFileRejection.FileTooLarge, 24_000_000, 10_485_760, 20_000));

        var result = await SendAsync(File());

        Assert.Equal("IMPORT.FILE_TOO_LARGE", result.BusinessCode);
        Assert.Equal("10485760", result.MessageParams!["MaxFileSizeBytes"]);
        Assert.Equal("24000000", result.MessageParams["FileSizeBytes"]);

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<FileStorageArea>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ca nghiệm thu mục 4 của hợp đồng: file <c>.xls</c> đổi đuôi thành <c>.xlsx</c> phải nhận mã
    /// này (hoặc đọc đúng), KHÔNG được ném exception hạ tầng.
    /// </summary>
    [Fact(DisplayName = "Không reader nào nhận ⇒ IMPORT.FORMAT_UNSUPPORTED, không ném")]
    public async Task UnsupportedFormat_IsBusinessError()
    {
        _selector.Select(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns(new ImportFileReaderSelection(null, ImportFileRejection.UnsupportedFormat, 100, 10_485_760, 20_000));

        var result = await SendAsync(File(), fileName: "bao-cao.xlsx");

        Assert.Equal("IMPORT.FORMAT_UNSUPPORTED", result.BusinessCode);
        Assert.Equal("bao-cao.xlsx", result.MessageParams!["FileName"]);
    }
}
