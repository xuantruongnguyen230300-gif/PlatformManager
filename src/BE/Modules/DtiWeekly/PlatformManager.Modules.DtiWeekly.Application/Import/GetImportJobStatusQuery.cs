using System.Text.Json;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>Bước 2 CONTRACT DM-7 — GET /api/import/{jobId}, FE poll tới khi Status =
/// Succeeded/Failed (không có cơ chế push nào khác ở version này, xem
/// .claude/rules/cqrs-handler.md §"Command chạy lâu → job nền").</summary>
public sealed record GetImportJobStatusQuery(Guid JobId) : IQuery<ImportJobStatusDto>;

public sealed record ImportJobStatusDto(string Status, ImportResultDto? Result, string? ErrorMessage);

public sealed class GetImportJobStatusHandler(IImportJobRepository jobRepo)
    : BaseResponse, IRequestHandler<GetImportJobStatusQuery, IApiResult<ImportJobStatusDto>>
{
    public async Task<IApiResult<ImportJobStatusDto>> Handle(GetImportJobStatusQuery query, CancellationToken ct)
    {
        var job = await jobRepo.GetByIdAsync(query.JobId, ct);
        if (job is null)
            return Fail<ImportJobStatusDto>(ImportErrors.JobNotFound);

        var result = job is { Status: ImportJobStatus.Succeeded, ResultJson: not null }
            ? JsonSerializer.Deserialize<ImportResultDto>(job.ResultJson)
            : null;

        return Ok(new ImportJobStatusDto(job.Status.ToString(), result, job.ErrorMessage));
    }
}
