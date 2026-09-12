using MediatR;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Payload phản hồi của DM-3 — <b>KHÔNG</b> phải một dòng lưới.
///
/// <para><b>Q49:</b> <c>POST</c> giữ trả <c>CriteriaDto</c> và FE <b>tải lại lưới</b>, khác đường
/// sửa (DM-4 trả một dòng lưới để thay tại chỗ). Lý do: chỉ tiêu vừa tạo có thể rơi vào bất kỳ
/// trang nào của lưới — thứ tự là theo mã tự nhiên, không theo thời điểm tạo — nên "chèn dòng vào
/// trang đang xem" sẽ hiện một dòng ở chỗ nó không thuộc về.</para>
/// </summary>
public sealed record CriteriaDto(
    Guid Id, string Code, string Name, Guid GroupId, string GroupName, decimal MaxScore);

/// <summary>
/// <b>DM-3</b> — <c>POST /api/criteria</c>: tạo chỉ tiêu, kèm dữ liệu đánh giá đầu tiên nếu muốn.
/// </summary>
/// <param name="Assessment">
/// TUỲ CHỌN. Vắng mặt = chỉ tạo chỉ tiêu, không đụng tới dữ liệu đánh giá — đúng khuôn Q74 ở cấp
/// object (xem <see cref="AssessmentPayload"/>).
/// </param>
public sealed record CreateCriteriaCommand(
    string? Code,
    string? Name,
    Guid GroupId,
    decimal MaxScore,
    AssessmentPayload? Assessment = null) : ICommand<CriteriaDto>;

public sealed class CreateCriteriaHandler(
    ICriteriaWriteRepository repository,
    AssessmentWriter assessmentWriter,
    IUnitOfWork unitOfWork)
    : BaseResponse, IRequestHandler<CreateCriteriaCommand, IApiResult<CriteriaDto>>
{
    public async Task<IApiResult<CriteriaDto>> Handle(CreateCriteriaCommand cmd, CancellationToken ct)
    {
        var codeError = CriteriaCodeRules.Validate(cmd.Code);
        if (codeError is { } invalidCode)
            return Fail<CriteriaDto>(invalidCode.Error, invalidCode.Args);

        var code = cmd.Code!.Trim();

        if (string.IsNullOrWhiteSpace(cmd.Name))
            return Fail<CriteriaDto>(CriteriaErrors.AsValidation(CriteriaDomainErrors.NameRequired));

        if (cmd.MaxScore <= 0)
            return Fail<CriteriaDto>(CriteriaErrors.AsValidation(CriteriaDomainErrors.MaxScoreNotPositive));

        // Trùng mã kiểm TRONG TẬP CHƯA XOÁ MỀM — unique index là partial theo IsDeleted = false,
        // nên xoá mềm một mã rồi tạo lại đúng mã đó PHẢI thành công. Repository đã đi qua global
        // query filter nên nó chỉ thấy tập đó.
        if (await repository.FindByCodeAsync(code, ct) is not null)
            return Fail<CriteriaDto>(CriteriaErrors.DuplicateCode, ("Code", code));

        var groupName = await repository.FindGroupNameAsync(cmd.GroupId, ct);
        if (groupName is null)
            return Fail<CriteriaDto>(CriteriaErrors.GroupNotFound);

        var criteria = CriteriaEntity.Create(code, cmd.Name.Trim(), cmd.GroupId, cmd.MaxScore);
        repository.Add(criteria);

        if (cmd.Assessment is { } payload)
        {
            var write = AssessmentPayloadMapper.ToWrite(payload, out var statusError);
            if (statusError is not null)
                return Fail<CriteriaDto>(statusError.Value.Error, statusError.Value.Args);

            var outcome = await assessmentWriter.WriteAsync(
                criteria, payload.Period, payload.Year, payload.Version, write, ct);

            if (!outcome.IsSuccess)
                return Fail<CriteriaDto>(outcome.Error!, outcome.ErrorArgs);
        }

        // MỘT SaveChanges cho cả use case: chỉ tiêu và bản ghi đánh giá đầu tiên vào cùng một giao
        // dịch. Tách đôi sẽ để lại một chỉ tiêu không có đánh giá khi vế sau hỏng — và người dùng
        // thấy dialog báo lỗi trong khi chỉ tiêu đã được tạo.
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new CriteriaDto(
            criteria.Id, criteria.Code, criteria.Name, criteria.GroupId, groupName, criteria.MaxScore));
    }
}

/// <summary>
/// Dịch <see cref="AssessmentPayload"/> (hình dạng ĐƯỜNG DÂY) sang <see cref="AssessmentWrite"/>
/// (hình dạng LUẬT GHI). Một chỗ, dùng chung bởi DM-3 và DM-4.
/// </summary>
internal static class AssessmentPayloadMapper
{
    /// <summary>
    /// <paramref name="statusError"/> khác <c>null</c> khi <c>status</c> được gửi nhưng không thuộc
    /// 4 giá trị của §4 — <b>TỪ CHỐI, không âm thầm bỏ qua và không tự map gần đúng</b> (§4 luật 3).
    ///
    /// <para>Quy chuẩn ở ĐÂY chứ không để entity ném: <c>CriteriaAssessment.SetStatus</c> ném
    /// <c>DomainException</c> ⇒ 422, còn hợp đồng đòi <b>400 CRITERIA.STATUS_INVALID</c> cho tham
    /// số sai khuôn. Cùng lý do với <see cref="CriteriaCodeRules"/>.</para>
    /// </summary>
    public static AssessmentWrite ToWrite(
        AssessmentPayload payload,
        out (ErrorDescriptor Error, (string Name, object? Value)[] Args)? statusError)
    {
        statusError = null;

        var status = payload.Status;

        if (status is { IsSet: true, Value: { } raw } && !string.IsNullOrWhiteSpace(raw))
        {
            if (!AssessmentStatuses.TryResolve(raw, out var resolved))
            {
                statusError = (CriteriaErrors.StatusInvalid, [("Status", raw)]);
                return new AssessmentWrite();
            }

            status = Assigned<string?>.Set(resolved);
        }

        return new AssessmentWrite
        {
            SelfScore = payload.SelfScore,
            VerifiedScore = payload.VerifiedScore,
            Status = status,
            OwnerId = payload.OwnerId,
            Deadline = payload.Deadline,
            Note = payload.Note,

            // ProgressPercent KHÔNG bao giờ đi qua đường dialog (Q9) — nó rơi vào nhánh "không
            // gửi" của luật chung: giữ nguyên khi cập nhật, copy-forward khi tạo bản ghi mới.
        };
    }
}
