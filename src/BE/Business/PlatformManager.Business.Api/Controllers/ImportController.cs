using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Application.Permissions;
using PlatformManager.Core.Api;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Business.Api.Controllers;

/// <summary>
/// Thân form của <c>POST /api/import</c> — DM-7 bước 1.
///
/// <para>Kiểu này sống ở tầng <c>Api</c> chứ không ở <c>Application</c> vì nó mang
/// <c>IFormFile</c>, thứ mà tầng Application bị cấm chạm (<c>LayerDependencyTests</c>). Controller
/// dịch nó thành một command mang <c>Stream</c> trần.</para>
/// </summary>
public sealed class StartImportRequest
{
    /// <summary>
    /// <c>.csv</c> · <c>.xlsx</c> · <c>.xls</c>. <b>Định dạng nhận diện bằng MAGIC BYTE, không bằng
    /// phần mở rộng</b> — phần mở rộng do người dùng đặt, nó là một GỢI Ý chứ không phải bằng chứng
    /// về nội dung.
    /// </summary>
    public IFormFile? File { get; init; }

    /// <summary>Kỳ đích của TOÀN BỘ file: <c>"YYYY-Www"</c> hoặc <c>"all"</c>. Bắt buộc.</summary>
    public string? Period { get; init; }

    /// <summary>Bắt buộc khi <see cref="Period"/> là <c>"all"</c> — năm người dùng đang xem (T15).</summary>
    public int? Year { get; init; }
}

/// <summary>
/// <b>DM-7</b> — nạp <c>.csv</c>/<c>.xlsx</c>/<c>.xls</c> bằng job nền, và poll trạng thái.
///
/// <para><b><c>[RequirePermission]</c> đặt ở CẤP LỚP, phủ cả hai action.</b> Action nạp file là
/// đường GHI nên bắt buộc theo §6.5 bước 2. Endpoint poll đi kèm cũng vào phạm vi vì nó chỉ phục
/// vụ luồng nạp: người không có quyền ghi không có <c>jobId</c> hợp lệ nào để hỏi, và mở nó ra là
/// biến một endpoint chỉ-đọc-trạng-thái thành nơi dò sự tồn tại của job người khác. Hợp đồng chỉ
/// miễn <c>[RequirePermission]</c> cho ba endpoint ĐỌC DỮ LIỆU đã nêu đích danh — DM-1, DM-2,
/// DM-8.</para>
///
/// <para>⚠️ <b>HTTP status là 200, KHÔNG phải 202.</b> <c>ApiControllerBase.HandleResult</c> map mọi
/// response thành công về 200, và <c>ErrorCode</c> không có member nào mang giá trị 202. "Đã bắt
/// đầu chứ chưa xong" thể hiện ở tầng DỮ LIỆU (<c>jobId</c> cần poll tiếp), không ở HTTP status —
/// một endpoint tự đặt status là nguồn sự thật thứ hai cho mapping <c>ErrorCode → HTTP</c>.</para>
///
/// <para>🛡️ Cả hai action đi qua middleware CSRF của host như mọi request ghi khác: client phải gửi
/// header <c>X-XSRF-TOKEN</c> kèm <c>POST</c>. Với <c>multipart/form-data</c> thì token đi ở
/// HEADER, không phải ở một trường form.</para>
/// </summary>
[ApiController]
[Route("api/import")]
[RequirePermission(BusinessResourceKeys.DtiManage)]
public class ImportController(ISender mediator) : ApiControllerBase
{
    /// <summary>
    /// <b>Bước 1</b> — nhận file, ghi ra kho tạm, tạo bản ghi theo dõi rồi đẩy việc ra chạy nền.
    /// Trả <c>{ jobId }</c>; KHÔNG đợi job chạy xong.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Start([FromForm] StartImportRequest request, CancellationToken ct)
    {
        // Mở stream ở ĐÂY và đóng ngay sau khi handler dùng xong: nội dung form do ASP.NET đệm sẵn
        // (bộ nhớ hoặc đĩa) nên stream seek được — điều kiện bắt buộc của phép nhận diện định dạng
        // bằng magic byte. `using` chứ không để handler tự đóng: chỗ MỞ tài nguyên là chỗ đóng nó.
        using var content = request.File?.OpenReadStream();

        var command = new StartImportCommand(content, request.File?.FileName, request.Period, request.Year);

        return HandleResult(await mediator.Send(command, ct));
    }

    /// <summary>
    /// <b>Bước 2</b> — FE poll cho tới khi <c>status</c> rời khỏi <c>Pending</c>/<c>Running</c>.
    /// <c>404 IMPORT.JOB_NOT_FOUND</c> là tín hiệu DỪNG poll.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> Status(Guid jobId, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetImportJobStatusQuery(jobId), ct));
}
