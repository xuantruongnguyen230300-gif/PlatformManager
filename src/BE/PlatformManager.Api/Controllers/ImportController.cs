using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Api.Services;

namespace PlatformManager.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController(CsvImportService csvImportService) : ControllerBase
{
    [HttpPost("csv")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportCsv(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw ApiException.BadRequest("IMPORT_FILE_EMPTY", "Vui lòng chọn file CSV cần import.");

        await using var stream = file.OpenReadStream();
        var result = await csvImportService.ImportAsync(stream, ct);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
