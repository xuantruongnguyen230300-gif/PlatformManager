using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// <b>DM-4</b> — <c>PUT /api/criteria/{id}</c>, dialog "Sửa chỉ tiêu": 4 trường danh mục + object
/// <c>assessment</c> 6 trường, trong MỘT request.
///
/// <para><b>Vì sao 6 trường đánh giá đi CHUNG một request với 4 trường danh mục, không tách hai
/// lời gọi:</b> trên màn hình đó là MỘT nút <c>Lưu chỉ tiêu</c> (Q9). Tách đôi tạo ra một cửa sổ
/// hỏng nửa vời — danh mục lưu xong, đánh giá lỗi, người dùng thấy dialog báo lỗi trong khi tên chỉ
/// tiêu đã đổi. Một use case, một giao dịch.</para>
///
/// <para><b>Không có query param nào.</b> Kỳ đích đi trong <c>assessment.period</c>.</para>
/// </summary>
/// <param name="Assessment">
/// TUỲ CHỌN — vắng mặt = <b>không đụng tới dữ liệu đánh giá</b>, khác hẳn "có mặt với mọi trường
/// <c>null</c>" (xoá trắng). Xem <see cref="AssessmentPayload"/>.
/// </param>
public sealed record UpdateCriteriaCommand(
    Guid Id,
    string? Code,
    string? Name,
    Guid GroupId,
    decimal MaxScore,
    AssessmentPayload? Assessment = null) : ICommand<CriteriaRowDto>;

public sealed class UpdateCriteriaHandler(
    ICriteriaWriteRepository repository,
    AssessmentWriter assessmentWriter,
    IDtiPeriodContext periodContext,
    IUnitOfWork unitOfWork)
    : BaseResponse, IRequestHandler<UpdateCriteriaCommand, IApiResult<CriteriaRowDto>>
{
    public async Task<IApiResult<CriteriaRowDto>> Handle(UpdateCriteriaCommand cmd, CancellationToken ct)
    {
        var criteria = await repository.GetTrackedAsync(cmd.Id, ct);
        if (criteria is null)
            return Fail<CriteriaRowDto>(CriteriaErrors.NotFound);

        var codeError = CriteriaCodeRules.Validate(cmd.Code);
        if (codeError is { } invalidCode)
            return Fail<CriteriaRowDto>(invalidCode.Error, invalidCode.Args);

        var code = cmd.Code!.Trim();

        if (string.IsNullOrWhiteSpace(cmd.Name))
            return Fail<CriteriaRowDto>(CriteriaErrors.AsValidation(CriteriaDomainErrors.NameRequired));

        if (cmd.MaxScore <= 0)
            return Fail<CriteriaRowDto>(CriteriaErrors.AsValidation(CriteriaDomainErrors.MaxScoreNotPositive));

        // Trùng mã: bản trùng LÀ CHÍNH nó thì không phải trùng — giữ nguyên mã mà đổi tên là ca
        // dùng thường xuyên nhất của dialog này.
        var duplicate = await repository.FindByCodeAsync(code, ct);
        if (duplicate is not null && duplicate.Id != criteria.Id)
            return Fail<CriteriaRowDto>(CriteriaErrors.DuplicateCode, ("Code", code));

        if (!await repository.GroupExistsAsync(cmd.GroupId, ct))
            return Fail<CriteriaRowDto>(CriteriaErrors.GroupNotFound);

        criteria.SetCode(code);
        criteria.Rename(cmd.Name.Trim());
        criteria.MoveToGroup(cmd.GroupId);

        // MaxScore gán TRƯỚC khi ghi đánh giá: AssessmentWriter so điểm với MaxScore ĐANG LƯU của
        // entity, và dialog cho phép sửa cả hai cùng lượt. Đảo thứ tự thì một lời ghi nâng cả
        // MaxScore lẫn SelfScore lên sẽ bị từ chối theo trần CŨ — người dùng không có cách nào
        // thoát ra ngoài việc lưu hai lần.
        criteria.SetMaxScore(cmd.MaxScore);

        // Kỳ để ĐỌC LẠI dòng phản hồi. Có ghi đánh giá thì đọc theo KỲ ĐÍCH vừa ghi (nghiệm thu
        // Q26(c) — assessmentPeriod của phản hồi phải nói đúng nơi lời ghi vừa rơi vào); không ghi
        // gì thì đọc theo kỳ hiện tại, vì lúc đó không có "kỳ đích" nào cả.
        var rowPeriod = periodContext.CurrentWeek();

        if (cmd.Assessment is { } payload)
        {
            var write = AssessmentPayloadMapper.ToWrite(payload, out var statusError);
            if (statusError is not null)
                return Fail<CriteriaRowDto>(statusError.Value.Error, statusError.Value.Args);

            var outcome = await assessmentWriter.WriteAsync(
                criteria, payload.Period, payload.Year, payload.Version, write, ct);

            if (!outcome.IsSuccess)
                return Fail<CriteriaRowDto>(outcome.Error!, outcome.ErrorArgs);

            rowPeriod = outcome.TargetWeek!;
        }

        await unitOfWork.SaveChangesAsync(ct);

        // Đọc lại SAU khi lưu: dòng phản hồi phải là thứ lưới sẽ hiện, kể cả các trường dẫn xuất
        // (diff, nhãn kỳ, tên người phụ trách) mà handler không tự dựng.
        var row = await repository.FindRowAsync(criteria.Id, rowPeriod, ct);

        return row is null ? Fail<CriteriaRowDto>(CriteriaErrors.NotFound) : Ok(row);
    }
}
