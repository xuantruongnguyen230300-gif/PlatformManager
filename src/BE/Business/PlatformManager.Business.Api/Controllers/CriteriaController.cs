using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Permissions;
using PlatformManager.Core.Api;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Business.Api.Controllers;

/// <summary>
/// <b>DM-2</b> (đọc) · <b>DM-3</b> (tạo) · <b>DM-4</b> (sửa qua dialog) · <b>DM-5</b> (xoá) ·
/// <b>DM-6</b> (sửa inline).
///
/// <para><b><c>[RequirePermission]</c> gắn trên từng ACTION GHI, KHÔNG trên class</b> — khác hẳn
/// <c>ImportController</c>. Lý do: controller này có cả đường đọc lẫn đường ghi, và DM-2 phải giữ
/// <c>[Authorize]</c> trần để người thiếu quyền vẫn vào được màn ở chế độ chỉ đọc (Q39). Gắn ở
/// class là chặn luôn đường đọc và biến Q39 thành một màn 403.</para>
///
/// <para><b>Người thiếu quyền ghi VẪN gọi được endpoint này</b> (Q39): không chặn ở route, không
/// ẩn mục menu. Họ nhận <c>200</c> với <c>canWrite = false</c> — KHÔNG phải 403. Dashboard đã
/// hiện đủ 62 chỉ tiêu cho mọi người đăng nhập, nên chặn màn Danh mục không giấu được dữ liệu
/// nào; nó chỉ đẩy người dùng vào một màn 403 khó hiểu cho thứ họ đã đọc được ở trang chủ.</para>
/// </summary>
[ApiController]
[Route("api/criteria")]
public class CriteriaController(ISender mediator) : ApiControllerBase
{
    /// <summary>
    /// <c>GET</c> + query string (không phải <c>POST list</c>): bộ lọc của màn này là 7 tham số
    /// phẳng, đủ đơn giản cho query string — cùng lựa chọn với <c>GET /api/users</c> của Core.
    /// Nó cũng là điều kiện để FE bookmark/chia sẻ được một bộ lọc.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetCriteriaListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    /// <summary>
    /// <b>DM-3</b> — tạo chỉ tiêu. Trả <c>CriteriaDto</c>, KHÔNG trả một dòng lưới: FE tải lại lưới
    /// (Q49), vì chỉ tiêu mới có thể rơi vào bất kỳ trang nào — thứ tự là theo mã tự nhiên.
    /// </summary>
    [HttpPost]
    [RequirePermission(BusinessResourceKeys.DtiManage)]
    public async Task<IActionResult> Create([FromBody] CreateCriteriaCommand command, CancellationToken ct)
        => HandleResult(await mediator.Send(command, ct));

    /// <summary>
    /// <b>DM-4</b> — dialog "Sửa chỉ tiêu": 4 trường danh mục + object <c>assessment</c> 6 trường,
    /// MỘT request (Q9). Trả một dòng lưới để FE thay tại chỗ.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(BusinessResourceKeys.DtiManage)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCriteriaCommand command, CancellationToken ct)
        // Id lấy từ ROUTE, không từ thân request: hai nguồn cho cùng một danh tính thì chúng lệch
        // được, và phía thua là phía không ai nhìn. `with` giữ nguyên phần còn lại của payload.
        => HandleResult(await mediator.Send(command with { Id = id }, ct));

    /// <summary>
    /// <b>DM-6</b> — sửa inline, ĐÚNG 2 trường (<c>progressPercent</c> · <c>note</c>). Ngữ nghĩa
    /// <c>PUT</c>: ghi đè cả hai, nên FE luôn gửi cả hai kể cả khi chỉ sửa một.
    /// </summary>
    [HttpPut("{id:guid}/assessment")]
    [RequirePermission(BusinessResourceKeys.DtiManage)]
    public async Task<IActionResult> UpdateAssessment(
        Guid id, [FromBody] UpdateCriteriaAssessmentCommand command, CancellationToken ct)
        => HandleResult(await mediator.Send(command with { Id = id }, ct));

    /// <summary>
    /// <b>DM-5</b> — xoá. BE quyết cứng hay mềm theo lịch sử đánh giá của MỌI năm; FE chỉ đọc
    /// <c>hardDeleted</c> để hiện thông báo sau khi xoá.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(BusinessResourceKeys.DtiManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new DeleteCriteriaCommand(id), ct));
}
