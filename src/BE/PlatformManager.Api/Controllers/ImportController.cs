using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Modules.DtiWeekly.Application.Import;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase — [RequirePermission] cộng dồn: chỉ role được cấp
// ResourceKeys.Import (xem doc/contracts/permissions.md) mới import được. CONTRACT DM-7
// (doc/contracts/danh-muc-dti.md) — job nền qua Hangfire, KHÔNG còn xử lý đồng bộ trong request.
[ApiController]
[Route("api/import")]
[RequirePermission(ResourceKeys.Import)]
public class ImportController(ISender mediator) : ApiControllerBase
{
    // Giữ NGUYÊN ngưỡng cũ — StartImportCommandHandler tự validate lại đúng ngưỡng này bằng
    // cmd.FileLength, khai lại ở đây chỉ để giới hạn kích thước multipart nhận vào.
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

    /// <summary>Bước 1 — bắt đầu import, trả ngay <c>{ jobId }</c> KHÔNG đợi xử lý xong (job
    /// chạy nền qua Hangfire). Envelope <c>IApiResult&lt;T&gt;</c> luôn map HTTP 200 khi thành
    /// công (đúng dispatcher chung <see cref="ApiControllerBase.HandleResult{T}"/> — "đã bắt đầu,
    /// chưa xử lý xong" nằm ở tầng dữ liệu/envelope, không phải HTTP status; xem ghi chú cập nhật
    /// 2026-08-24 ở CONTRACT DM-7).</summary>
    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes + 1024)]
    public async Task<IActionResult> Start(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return HandleResult(ApiResult<StartImportResultDto>.BusinessError(ImportErrors.FileEmpty, ImportErrors.FileEmpty.MessageTemplate));

        await using var stream = file.OpenReadStream();
        return HandleResult(await mediator.Send(new StartImportCommand(stream, file.FileName, file.Length), ct));
    }

    /// <summary>Bước 2 — FE poll tới khi <c>status</c> chuyển "Succeeded"/"Failed".</summary>
    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetImportJobStatusQuery(jobId), ct));
}
