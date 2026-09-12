using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Core.Api;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Api.Controllers;

/// <summary>
/// <b>DB-1</b> và <b>DB-3</b> (= <b>DM-8</b>, dùng chung cho cả hai màn). Dashboard là màn
/// <b>100% ĐỌC</b> — không có hành động ghi nào, kể cả nút "Xuất báo cáo" (nó chỉ tải file về).
///
/// <para><b>Mọi người đăng nhập đều xem được, không cần quyền DTI</b> (Q21) ⇒ giữ
/// <c>[Authorize]</c> trần, không <c>[RequirePermission]</c>.</para>
///
/// <para><b>KHÔNG gắn <c>[EnableRateLimiting]</c>, KHÔNG cache</b> — Q70 (chốt 2026-09-10).
/// <c>GlobalLimiter</c> ở host đã phủ mọi request; một policy riêng chỉ đáng khi endpoint có hồ
/// sơ lạm dụng khác phần còn lại (như <c>login</c>, nơi mỗi lần gọi là một lần đoán mật khẩu),
/// mà hai endpoint ở đây đều <c>[Authorize]</c>, chỉ đọc, chạy trên vài chục chỉ tiêu (Q70 nói
/// về cả BA endpoint của card, kể cả <c>export</c> của DB-4). Lý do
/// KHÔNG cache là lý do nghiệp vụ, không phải lười: dữ liệu đằng sau đổi ngay sau mỗi lần sửa
/// inline và mỗi lần import, mà cả hai đều là thao tác người dùng làm rồi quay lại xem ngay —
/// một bản cache vài chục giây biến "số tôi vừa nhập đi đâu mất" thành một báo lỗi không tái
/// hiện được.</para>
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Aggregate([FromQuery] GetDashboardQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    /// <summary>
    /// <b>DB-3 / DM-8</b> — danh sách Năm/Kỳ CÓ dữ liệu. Một route, hai màn: card DM-8 cố ý trỏ
    /// sang đây thay vì mô tả lại, và FE dùng một service chung ở <c>shared/</c>.
    /// </summary>
    [HttpGet("periods")]
    public async Task<IActionResult> Periods([FromQuery] GetPeriodOptionsQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    /// <summary>
    /// <b>DB-4</b> — "Xuất báo cáo": tải thẳng file <c>.xlsx</c>.
    ///
    /// <para>🛑 <b>Đây là endpoint DUY NHẤT của hệ thống không bọc envelope ở nhánh THÀNH CÔNG.</b>
    /// Lý do: <c>HandleResult&lt;T&gt;</c> serialize <c>T</c> thành JSON, nên nhồi vài trăm KB bytes
    /// vào <c>data</c> dưới dạng base64 làm file phình ~33% và buộc FE giải mã trong bộ nhớ trước
    /// khi đưa cho người dùng lưu.</para>
    ///
    /// <para><b>Ngoại lệ có PHẠM VI HẸP và KIỂM ĐƯỢC:</b> chỉ nhánh thành công. Nhánh lỗi
    /// (<c>400</c>/<c>403</c>/<c>429</c>/<c>500</c>) vẫn đi đúng <c>HandleResult</c> và vẫn là
    /// <c>IApiResult&lt;T&gt;</c> JSON — đó là lý do FE phải kiểm <c>Content-Type</c> của response
    /// trước khi coi body là file: nhận JSON tức là lỗi.</para>
    ///
    /// <para><b>Vẫn KHÔNG tự đặt status code.</b> Nhánh thành công dùng <c>File(...)</c> (200 do
    /// framework đặt), nhánh lỗi dùng đúng dispatcher chung — không có bảng map
    /// <c>ErrorCode → HTTP</c> thứ hai nào sinh ra ở đây.</para>
    ///
    /// <para>Giữ <c>[Authorize]</c> trần, KHÔNG <c>[RequirePermission]</c>: người thiếu quyền ghi
    /// vẫn xuất được báo cáo (Q39 — họ chỉ không ghi được dữ liệu).</para>
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ExportDashboardQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);

        if (result.Code != ErrorCode.Success || result.Data is null)
            return HandleResult(result);

        // File() tự đặt Content-Disposition: attachment, kèm cả `filename` lẫn `filename*`. Tên
        // file chỉ gồm ASCII (không dấu, không khoảng trắng — xem ExportDashboardHandler), nên hai
        // dạng trùng nhau và mọi trình duyệt/hệ tệp mở được mà không phải giải mã.
        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }
}
