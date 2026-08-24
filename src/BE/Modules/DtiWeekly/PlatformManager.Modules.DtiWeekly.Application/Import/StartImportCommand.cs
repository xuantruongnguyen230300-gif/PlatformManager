using Hangfire;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>
/// Bước 1 CONTRACT DM-7 (doc/contracts/danh-muc-dti.md) — POST /api/import (multipart, giới
/// hạn 20MB, .csv/.xlsx/.xls). Nhận Stream thuần (không IFormFile — Application không được biết
/// tới kiểu ASP.NET Core, xem .claude/rules/architecture.md). Handler validate + tạo
/// ImportJob(Pending) + lưu file qua IImportFileStorage + enqueue Hangfire NGAY, KHÔNG đợi xử
/// lý xong — xem .claude/rules/cqrs-handler.md §"Command chạy lâu → job nền".
/// </summary>
public sealed record StartImportCommand(Stream FileContent, string FileName, long FileLength) : ICommand<StartImportResultDto>;

public sealed record StartImportResultDto(Guid JobId);

public sealed class StartImportCommandHandler(
    IImportJobRepository jobRepo,
    IImportFileStorage fileStorage,
    IUnitOfWork uow) : BaseResponse, IRequestHandler<StartImportCommand, IApiResult<StartImportResultDto>>
{
    // Giữ NGUYÊN ngưỡng cũ (trước khi tách job nền) — xem ImportController cũ.
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private static readonly Dictionary<string, ImportFileFormat> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".csv"] = ImportFileFormat.Csv,
            [".xlsx"] = ImportFileFormat.Xlsx,
            [".xls"] = ImportFileFormat.Xls,
        };

    public async Task<IApiResult<StartImportResultDto>> Handle(StartImportCommand cmd, CancellationToken ct)
    {
        if (cmd.FileLength == 0)
            return Fail<StartImportResultDto>(ImportErrors.FileEmpty);

        if (cmd.FileLength > MaxFileSizeBytes)
            return Fail<StartImportResultDto>(ImportErrors.FileTooLarge);

        var extension = Path.GetExtension(cmd.FileName);
        if (!SupportedExtensions.TryGetValue(extension, out var format))
            return Fail<StartImportResultDto>(ImportErrors.FileFormatUnsupported);

        var job = ImportJob.Create(cmd.FileName, format);
        var storagePath = await fileStorage.SaveAsync(job.Id, cmd.FileContent, cmd.FileName, ct);
        job.SetStoragePath(storagePath);

        await jobRepo.AddAsync(job, ct);
        await uow.SaveChangesAsync(ct);

        // Enqueue SAU khi job đã commit DB — tránh race Hangfire worker chạy trước khi record
        // ImportJob tồn tại trong DB (worker tự resolve scope DI riêng, đọc lại bằng jobId).
        BackgroundJob.Enqueue<IImportJobRunner>(runner => runner.RunAsync(job.Id, CancellationToken.None));

        return Ok(new StartImportResultDto(job.Id));
    }
}
