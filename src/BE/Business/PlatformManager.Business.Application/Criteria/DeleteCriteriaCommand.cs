using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Payload phản hồi của DM-5.
/// </summary>
/// <param name="HardDeleted">
/// <c>true</c> = xoá CỨNG (chưa từng có bản ghi đánh giá nào) · <c>false</c> = xoá MỀM (đã có lịch
/// sử, giữ nguyên).
///
/// <para><b>BE quyết định, FE chỉ đọc kết quả.</b> FE KHÔNG được đoán trước bằng
/// <c>row.assessmentId !== null</c> để đổi câu xác nhận: trường đó chỉ phản ánh <b>kỳ đang xem</b>,
/// không phản ánh toàn bộ lịch sử nhiều năm — bản cũ làm vậy và câu xác nhận sai trong đúng ca
/// người dùng cần nó đúng nhất.</para>
/// </param>
public sealed record DeleteCriteriaResultDto(bool HardDeleted);

/// <summary>
/// <b>DM-5</b> — <c>DELETE /api/criteria/{id}</c>.
/// </summary>
public sealed record DeleteCriteriaCommand(Guid Id) : ICommand<DeleteCriteriaResultDto>;

public sealed class DeleteCriteriaHandler(ICriteriaWriteRepository repository, IUnitOfWork unitOfWork)
    : BaseResponse, IRequestHandler<DeleteCriteriaCommand, IApiResult<DeleteCriteriaResultDto>>
{
    public async Task<IApiResult<DeleteCriteriaResultDto>> Handle(
        DeleteCriteriaCommand cmd, CancellationToken ct)
    {
        var criteria = await repository.GetTrackedAsync(cmd.Id, ct);
        if (criteria is null)
            return Fail<DeleteCriteriaResultDto>(CriteriaErrors.NotFound);

        // "Đã TỪNG có bản ghi đánh giá nào chưa" — MỌI NĂM, mọi kỳ (§5.5). Không phải "có bản ghi
        // trong kỳ đang xem không": một chỉ tiêu có số liệu năm ngoái mà trống năm nay vẫn phải xoá
        // MỀM, nếu không lịch sử của năm ngoái biến mất cùng nó.
        var hasHistory = await repository.HasAnyAssessmentAsync(criteria.Id, ct);

        if (hasHistory)
        {
            // Xoá MỀM. Chỉ tiêu biến khỏi mọi lưới và mọi phép tổng hợp — kể cả khi xem lại kỳ cũ
            // mà lúc đó nó còn sống (§5.5). Nếu không, tổng số chỉ tiêu của một kỳ sẽ đổi tuỳ thời
            // điểm người ta mở màn hình lên xem.
            //
            // Đặt cờ thay vì gọi Remove: repo này KHÔNG có interceptor dịch Delete thành soft
            // delete — global query filter chỉ lọc khi ĐỌC. Gọi Remove ở đây là xoá thật.
            criteria.IsDeleted = true;
        }
        else
        {
            repository.Remove(criteria);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new DeleteCriteriaResultDto(!hasHistory));
    }
}
