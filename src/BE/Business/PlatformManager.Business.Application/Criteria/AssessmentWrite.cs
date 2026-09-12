using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Bộ 7 trường đánh giá mà MỘT lời ghi có thể đụng tới, mỗi trường mang cờ "client có gửi không"
/// (<see cref="Assigned{T}"/>). Ba đường ghi khai cùng kiểu này, khác nhau ở chỗ trường nào
/// <see cref="Assigned{T}.IsSet"/>:
///
/// <list type="table">
///   <item><term>Dialog (DM-4)</term><description>6 trường đánh giá —
///   <see cref="SelfScore"/> · <see cref="VerifiedScore"/> · <see cref="Status"/> ·
///   <see cref="OwnerId"/> · <see cref="Deadline"/> · <see cref="Note"/>.
///   <see cref="ProgressPercent"/> KHÔNG gửi.</description></item>
///   <item><term>Sửa inline (DM-6)</term><description>ĐÚNG 2 trường —
///   <see cref="ProgressPercent"/> · <see cref="Note"/>. Ràng buộc của hợp đồng, không phải giới
///   hạn tạm thời (Q9).</description></item>
///   <item><term>Import (DM-7)</term><description>6 trường ánh xạ từ 6 cột đánh giá của file.
///   <see cref="ProgressPercent"/> KHÔNG gửi — file BA gửi không có cột đó và đây là trường NHẬP
///   TAY (Q24): điền hộ một giá trị khởi tạo là bịa ra số liệu mà không ai ký tên.</description></item>
/// </list>
///
/// <para>Gói cả bảy vào một kiểu là cách ép <b>một luật ghi cho cả ba đường</b>
/// (spec/danh-muc-dti/business-rules.md §5.3): không đường nào dựng lại được một nửa của
/// copy-forward.</para>
/// </summary>
public sealed record AssessmentWrite
{
    public Assigned<decimal?> SelfScore { get; init; }

    public Assigned<decimal?> VerifiedScore { get; init; }

    public Assigned<int?> ProgressPercent { get; init; }

    /// <summary>Phải ĐÃ được quy về đúng 1 trong 4 giá trị chuẩn trước khi tới đây — giá trị lạ
    /// là lỗi của nơi gọi (400 ở đường tay, lỗi DÒNG ở đường import), không phải của bộ upsert.</summary>
    public Assigned<string?> Status { get; init; }

    public Assigned<Guid?> OwnerId { get; init; }

    public Assigned<DateOnly?> Deadline { get; init; }

    public Assigned<string?> Note { get; init; }
}

/// <summary>
/// <b>Bước 2 của luật ghi — ghi vào bản ghi nào của kỳ đích, và copy-forward</b>
/// (spec/danh-muc-dti/business-rules.md §5.3). Dùng CHUNG cho cả ba đường ghi.
/// </summary>
public static class AssessmentUpsert
{
    /// <summary>
    /// Áp <paramref name="write"/> lên <paramref name="target"/>.
    ///
    /// <para><b>Copy-forward</b>: trường KHÔNG nằm trong lời ghi lấy giá trị từ
    /// <paramref name="copyForwardSource"/> — bản ghi gần nhất TRƯỚC kỳ đích. Nơi gọi truyền
    /// <c>null</c> khi đang CẬP NHẬT một bản ghi đã có của kỳ đích (khi đó trường không gửi thì
    /// giữ nguyên giá trị đang có) và khi không tìm thấy bản ghi trước nào.</para>
    ///
    /// <para><b>Vì sao copy-forward tồn tại:</b> không có nó, người sửa mỗi <c>Tiến độ %</c> sẽ
    /// tạo một bản ghi mới mà <c>SelfScore</c>/<c>VerifiedScore</c>/<c>Status</c> đều trống — và
    /// Dashboard của tuần đó lập tức báo tụt về 0. Đây là lỗi "dữ liệu đúng theo từng lời ghi
    /// nhưng sai theo cái người ta đọc".</para>
    ///
    /// <para>⚠️ Copy-forward là luật của ĐƯỜNG GHI, đừng lẫn với <i>carry-forward</i> mà §5.2 CẤM ở
    /// đường đọc: kỳ trước có dữ liệu mà kỳ này không có bản ghi nào thì lưới hiện RỖNG, không kéo
    /// số cũ sang. Hai luật nói về hai thời điểm khác nhau — lúc TẠO bản ghi, và lúc ĐỌC.</para>
    /// </summary>
    public static void Apply(CriteriaAssessment target, CriteriaAssessment? copyForwardSource, AssessmentWrite write)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(write);

        // MỘT bản ghi nguồn cho mọi trường không gửi: bản ghi kỳ trước khi đang TẠO, chính bản ghi
        // đang sửa khi đang CẬP NHẬT. Gộp hai ca vào một biến thay vì rải điều kiện ở bảy chỗ —
        // bảy bản sao của cùng một điều kiện là bảy cơ hội để một chỗ quên.
        var fallback = copyForwardSource ?? target;

        // SetScores nhận cả hai vế cùng lúc, nên chốt cả hai rồi gọi một lần — gọi hai lần với một
        // vế "giữ nguyên" sẽ đọc lại chính giá trị vừa ghi.
        target.SetScores(
            Pick(write.SelfScore, fallback.SelfScore),
            Pick(write.VerifiedScore, fallback.VerifiedScore));

        target.SetProgressPercent(Pick(write.ProgressPercent, fallback.ProgressPercent));
        target.SetStatus(Pick(write.Status, fallback.Status));
        target.SetOwner(Pick(write.OwnerId, fallback.OwnerId));
        target.SetDeadline(Pick(write.Deadline, fallback.Deadline));
        target.SetNote(Pick(write.Note, fallback.Note));
    }

    /// <summary>
    /// Có gửi ⇒ dùng giá trị đã gửi (kể cả <c>null</c> — xoá trắng có chủ đích). Không gửi ⇒ lấy
    /// từ bản ghi nguồn.
    /// </summary>
    private static T Pick<T>(Assigned<T> assigned, T fallback) => assigned.IsSet ? assigned.Value : fallback;
}
