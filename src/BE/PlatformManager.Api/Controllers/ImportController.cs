using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase — mọi user đã đăng nhập đều import được.
[ApiController]
[Route("api/import")]
public class ImportController(ISender mediator) : ApiControllerBase
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

    [HttpPost("csv")]
    [RequestSizeLimit(MaxFileSizeBytes + 1024)]
    public async Task<IActionResult> ImportCsv(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return HandleResult(ApiResult<ImportCsvResultDto>.BusinessError(AssessmentErrors.FileRequired, AssessmentErrors.FileRequired.MessageTemplate));

        if (file.Length > MaxFileSizeBytes)
            return HandleResult(ApiResult<ImportCsvResultDto>.BusinessError(AssessmentErrors.FileTooLarge, AssessmentErrors.FileTooLarge.MessageTemplate));

        await using var stream = file.OpenReadStream();
        return HandleResult(await mediator.Send(new ImportCsvCommand(stream), ct));
    }
}
