using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Một lần đánh giá của một chỉ tiêu — spec/danh-muc-dti/business-rules.md §1.3.
///
/// <para><b>Ba quyết định phải biết trước khi sửa entity này</b> (§1.3 a/b/c):</para>
/// <list type="number">
///   <item><see cref="Note"/> là MỘT ô text, không phải danh sách minh chứng. Thực thể
///   <c>CriteriaEvidence</c> của thiết kế cũ bị bỏ hẳn (Q5).</item>
///   <item><see cref="AssessmentDate"/> là cột RIÊNG, KHÔNG dùng lại <c>CreatedAt</c>: kỳ là dữ
///   liệu nghiệp vụ do người dùng chọn (Q20), không phải dấu vết hệ thống. Dùng
///   <c>CreatedAt</c> làm khoá kỳ nghĩa là một thay đổi ở tầng audit lặng lẽ đổi kỳ của dữ liệu,
///   và không nhập bù cho tuần trước được.</item>
///   <item><see cref="Version"/> là <c>uint</c> + <c>.IsRowVersion()</c>, KHÔNG <c>byte[]</c> —
///   trên PostgreSQL, <c>byte[]</c> tạo một cột <c>bytea</c> KHÔNG AI CẬP NHẬT nên check
///   concurrency vô hiệu IM LẶNG. Luật đầy đủ: doc/huong_dan/quy-uoc/be-entity-domain.md
///   §RowVersion.</item>
/// </list>
///
/// <para><b><c>Chênh lệch</c> KHÔNG có cột</b> — nó là trường TÍNH
/// (<c>VerifiedScore − SelfScore</c>, §3.1). Lưu một trường suy ra được là tạo chỗ cho nó lệch
/// khỏi hai trường sinh ra nó.</para>
/// </summary>
public class CriteriaAssessment : BaseEntity
{
    public Guid CriteriaId { get; private set; }

    /// <summary>
    /// Ngày nghiệp vụ của lần đánh giá — kỳ (tuần/tháng/năm) SUY RA từ đây. Giá trị do kỳ đích
    /// của lời ghi quyết định, không phải "hôm nay" (luật neo ngày §5.3).
    /// </summary>
    public DateOnly AssessmentDate { get; private set; }

    public decimal? SelfScore { get; private set; }

    public decimal? VerifiedScore { get; private set; }

    /// <summary>0..100, NHẬP TAY (§3.2). Không tự tính lại từ điểm; để TRỐNG khi import (Q24).</summary>
    public int? ProgressPercent { get; private set; }

    /// <summary>Đúng 1 trong 4 giá trị của <see cref="AssessmentStatuses"/>, hoặc chưa có.</summary>
    public string? Status { get; private set; }

    /// <summary>FK XUYÊN SCHEMA → <c>core."AspNetUsers"."Id"</c> — chiều duy nhất được phép.</summary>
    public Guid? OwnerId { get; private set; }

    public DateOnly? Deadline { get; private set; }

    /// <summary>Minh chứng/Ghi chú — MỘT ô text nhiều dòng (Q5).</summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Token optimistic concurrency. <c>uint</c> + <c>.IsRowVersion()</c> ⇒ Npgsql bind thẳng
    /// vào cột hệ thống <c>xmin</c>, KHÔNG tạo cột mới. Tồn tại vì entity này có HAI luồng ghi
    /// độc lập chạm cùng bản ghi: import hàng loạt và sửa tay — không có nó thì người ghi sau âm
    /// thầm nuốt thay đổi của người ghi trước.
    /// </summary>
    public uint Version { get; private set; }

    private CriteriaAssessment() { }

    /// <summary>
    /// Tạo bản ghi đánh giá cho một kỳ. <paramref name="assessmentDate"/> do LUẬT NEO NGÀY §5.3
    /// quyết định (hôm nay nếu hôm nay nằm trong tuần đích, ngược lại là Chủ nhật của tuần đích)
    /// — entity không tự chọn ngày, vì "kỳ đích" là thông tin của lời ghi chứ không của entity.
    /// </summary>
    public static CriteriaAssessment Create(Guid criteriaId, DateOnly assessmentDate)
    {
        if (criteriaId == Guid.Empty)
            throw new DomainException(CriteriaAssessmentDomainErrors.CriteriaRequired);

        return new CriteriaAssessment
        {
            CriteriaId = criteriaId,
            AssessmentDate = assessmentDate,
        };
    }

    public void SetScores(decimal? selfScore, decimal? verifiedScore)
    {
        SelfScore = selfScore;
        VerifiedScore = verifiedScore;
    }

    /// <summary>
    /// Ngoài miền 0..100 ⇒ KẸP về biên, KHÔNG báo lỗi (§3.2 — bảo vệ chiều sâu: FE kẹp trước,
    /// BE kẹp lại).
    /// </summary>
    public void SetProgressPercent(int? progressPercent) =>
        ProgressPercent = progressPercent is null ? null : Math.Clamp(progressPercent.Value, 0, 100);

    /// <summary>
    /// Gán trạng thái. Chuỗi được quy về đúng một trong 4 giá trị chuẩn; không khớp ⇒ TỪ CHỐI
    /// (§4 luật 3). <c>null</c> nghĩa là "chưa có trạng thái", hợp lệ.
    /// </summary>
    public void SetStatus(string? status)
    {
        if (status is null)
        {
            Status = null;
            return;
        }

        if (!AssessmentStatuses.TryResolve(status, out var resolved))
            throw new DomainException(CriteriaAssessmentDomainErrors.StatusInvalid);

        Status = resolved;
    }

    public void SetOwner(Guid? ownerId) => OwnerId = ownerId;

    public void SetDeadline(DateOnly? deadline) => Deadline = deadline;

    public void SetNote(string? note) => Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
