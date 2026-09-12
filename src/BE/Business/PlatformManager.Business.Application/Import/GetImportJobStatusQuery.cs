using System.Text.Json;
using MediatR;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// <b>DM-7 bước 2</b> — <c>GET /api/import/{jobId}</c>: FE poll cho tới khi
/// <c>status</c> rời khỏi <c>Pending</c>/<c>Running</c>.
/// </summary>
public sealed record GetImportJobStatusQuery(Guid JobId) : IQuery<ImportJobStatusDto>;

public sealed class GetImportJobStatusHandler(IImportJobRepository jobs)
    : BaseResponse, IRequestHandler<GetImportJobStatusQuery, IApiResult<ImportJobStatusDto>>
{
    /// <summary>
    /// Phải KHỚP bộ tuỳ chọn mà <see cref="ImportJobRunner"/> dùng lúc ghi. Hai bộ lệch nhau thì
    /// <c>result</c> đọc ra rỗng toàn phần mà không ném lỗi nào — đúng lớp hỏng im lặng.
    /// </summary>
    private static readonly JsonSerializerOptions ResultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IApiResult<ImportJobStatusDto>> Handle(GetImportJobStatusQuery query, CancellationToken ct)
    {
        var snapshot = await jobs.GetStatusAsync(query.JobId, ct);

        // KHÔNG phân biệt "sai id" với "job đã bị dọn theo retention" — xem docstring của mã. 404
        // ở đây là TÍN HIỆU DỪNG cho vòng poll của FE; thiếu nó thì một tab để mở qua đêm sẽ gọi
        // lại endpoint này mãi mãi.
        if (snapshot is null)
            return Fail<ImportJobStatusDto>(ImportErrors.JobNotFound);

        return Ok(new ImportJobStatusDto(
            snapshot.Status,
            ParseResult(snapshot),
            snapshot.ErrorCode,
            snapshot.ErrorMessage));
    }

    /// <summary>
    /// <c>result</c> CHỈ có khi <c>Succeeded</c> — kiểm trạng thái chứ không chỉ kiểm
    /// <c>ResultJson</c> có rỗng không: một job <c>Failed</c> sau khi đã chạy lại vẫn có thể còn
    /// <c>ResultJson</c> của lần trước, và trả nó ra sẽ báo "đã nạp xong" cho một lượt thất bại.
    ///
    /// <para>JSON hỏng ⇒ <c>null</c> chứ không ném: dữ liệu của một bản ghi cũ không được phép làm
    /// gãy endpoint poll. FE khi đó thấy <c>Succeeded</c> mà không có <c>result</c> — trạng thái
    /// nghèo thông tin, nhưng vẫn là tín hiệu DỪNG poll.</para>
    /// </summary>
    private static ImportResultDto? ParseResult(ImportJobStatusSnapshot snapshot)
    {
        if (!string.Equals(snapshot.Status, ImportJobStatuses.Succeeded, StringComparison.Ordinal))
            return null;

        if (string.IsNullOrWhiteSpace(snapshot.ResultJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ImportResultDto>(snapshot.ResultJson, ResultJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
