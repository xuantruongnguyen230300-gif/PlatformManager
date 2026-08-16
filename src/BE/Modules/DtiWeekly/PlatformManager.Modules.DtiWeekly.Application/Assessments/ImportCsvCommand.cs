using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// POST /api/import/csv (multipart, giới hạn 20MB — chặn ở Api layer trước khi tới đây).
/// Nhận Stream thuần (không IFormFile — đó là kiểu ASP.NET Core, Application không được
/// biết tới, xem .claude/rules/architecture.md).
/// </summary>
public sealed record ImportCsvCommand(Stream FileStream) : ICommand<ImportCsvResultDto>;

public sealed class ImportCsvHandler(ICsvImportService importService)
    : BaseResponse, IRequestHandler<ImportCsvCommand, IApiResult<ImportCsvResultDto>>
{
    public async Task<IApiResult<ImportCsvResultDto>> Handle(ImportCsvCommand cmd, CancellationToken ct)
    {
        var result = await importService.ImportAsync(cmd.FileStream, ct);
        return Ok(result);
    }
}
