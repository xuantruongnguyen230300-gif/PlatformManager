using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// <b>DM-6</b> — <c>PUT /api/criteria/{id}/assessment</c>, sửa inline trong lưới.
///
/// <para><b>ĐÚNG 2 trường nghiệp vụ, không hơn</b> (Q9): <c>ProgressPercent</c> và <c>Note</c>.
/// Bốn trường còn lại của bộ 6 chỉ sửa được qua dialog (DM-4). Đây là <b>ràng buộc của hợp
/// đồng</b>, không phải giới hạn tạm thời — nới nó ra là xoá mất ranh giới mà Q9 dựng.</para>
///
/// <para><b>Ngữ nghĩa <c>PUT</c>: ghi đè CẢ HAI trường.</b> FE luôn gửi cả hai kể cả khi chỉ sửa
/// một, lấy giá trị hiện tại của trường còn lại từ dòng đang có trong bộ nhớ. Bỏ trống một trường
/// thì trường đó bị null-hoá — đúng vế "khoá có mặt mang <c>null</c> ⇒ xoá trắng" của Q74.</para>
///
/// <para>⚠️ <b>Endpoint này nhận NHIỀU lời gọi hơn vẻ ngoài của nó:</b> FE chốt <i>rời ô sửa inline
/// thì LƯU</i>, nên mỗi lần người dùng rời một ô là một <c>PUT</c>. Bất biến phải giữ: nhiều lời
/// ghi vào CÙNG một kỳ chồng lên nhau trên MỘT bản ghi, không sinh bản thứ hai — §5.3 bước 2 mục 1,
/// và có test khoá riêng.</para>
/// </summary>
/// <param name="ProgressPercent">
/// Ngoài miền 0..100 ⇒ <b>KẸP về biên, KHÔNG báo lỗi</b> (§3.2 — bảo vệ chiều sâu: FE kẹp trước,
/// BE kẹp lại). Đây là lý do <c>CRITERIA.PROGRESS_PERCENT_INVALID</c> không được khai trong catalog
/// — xem docstring của <see cref="CriteriaErrors"/>.
/// </param>
public sealed record UpdateCriteriaAssessmentCommand(
    Guid Id,
    string? Period = null,
    int? Year = null,
    int? ProgressPercent = null,
    string? Note = null,
    string? Version = null) : ICommand<CriteriaRowDto>;

public sealed class UpdateCriteriaAssessmentHandler(
    ICriteriaWriteRepository repository,
    AssessmentWriter assessmentWriter,
    IUnitOfWork unitOfWork)
    : BaseResponse, IRequestHandler<UpdateCriteriaAssessmentCommand, IApiResult<CriteriaRowDto>>
{
    public async Task<IApiResult<CriteriaRowDto>> Handle(
        UpdateCriteriaAssessmentCommand cmd, CancellationToken ct)
    {
        var criteria = await repository.GetTrackedAsync(cmd.Id, ct);
        if (criteria is null)
            return Fail<CriteriaRowDto>(CriteriaErrors.NotFound);

        // Cả hai trường LUÔN Set — đó chính là ngữ nghĩa PUT của endpoint này. Dùng Unset ở đây sẽ
        // biến nó thành PATCH và làm "xoá trắng ô ghi chú" trở thành thao tác bất khả.
        var write = new AssessmentWrite
        {
            ProgressPercent = Assigned<int?>.Set(cmd.ProgressPercent),
            Note = Assigned<string?>.Set(cmd.Note),
        };

        var outcome = await assessmentWriter.WriteAsync(
            criteria, cmd.Period, cmd.Year, cmd.Version, write, ct);

        if (!outcome.IsSuccess)
            return Fail<CriteriaRowDto>(outcome.Error!, outcome.ErrorArgs);

        await unitOfWork.SaveChangesAsync(ct);

        // Đọc lại theo KỲ ĐÍCH vừa ghi, không theo kỳ đang lọc: phản hồi phải mang
        // assessmentPeriod = nơi lời ghi vừa rơi vào (nghiệm thu Q26(c)).
        var row = await repository.FindRowAsync(criteria.Id, outcome.TargetWeek!, ct);

        return row is null ? Fail<CriteriaRowDto>(CriteriaErrors.NotFound) : Ok(row);
    }
}
