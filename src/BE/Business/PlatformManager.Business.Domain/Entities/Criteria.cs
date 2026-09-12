using PlatformManager.Business.Domain.Common;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Chỉ tiêu đánh giá DTI — spec/danh-muc-dti/business-rules.md §1.2.
///
/// <para><b>Hai cột dẫn xuất do CHÍNH entity ghi</b> (Q47 + Q53): <see cref="NameNormalized"/>
/// theo mọi lần gán <see cref="Name"/>, <see cref="CodeSortKey"/> theo mọi lần gán
/// <see cref="Code"/>. Không handler nào tự tính, không đường ghi nào bỏ qua được — đó là toàn
/// bộ lý do hai cột này nằm trong entity thay vì trong một service. Cả hai KHÔNG nhận từ client
/// và KHÔNG ra dây.</para>
/// </summary>
public class Criteria : BaseEntity
{
    /// <summary>Mã chỉ tiêu — unique trong tập CHƯA xoá mềm (§1.4), khuôn ở §2.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Tên chỉ tiêu — bắt buộc, không giới hạn cứng độ dài (§1.2).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Bản không dấu, chữ thường của <see cref="Name"/> — cột chuẩn hoá cho tìm kiếm (Q47).
    /// KHÔNG index riêng, có chủ đích (Q59, §1.4): khớp CHUỖI CON không dùng được btree.
    /// </summary>
    public string NameNormalized { get; private set; } = string.Empty;

    /// <summary>
    /// Khoá sắp xếp tự nhiên tính sẵn từ <see cref="Code"/> (Q53) — xem
    /// <see cref="CriteriaCode.BuildSortKey"/>.
    /// </summary>
    public string CodeSortKey { get; private set; } = string.Empty;

    public Guid GroupId { get; private set; }

    /// <summary>
    /// Điểm tối đa, <c>&gt; 0</c>. Dữ liệu thật chỉ có 3 giá trị (10 · 20 · 30) nhưng CỐ Ý
    /// không ràng buộc thành enum — BA có thể thêm mức khác (§1.2).
    /// </summary>
    public decimal MaxScore { get; private set; }

    private Criteria() { }

    public static Criteria Create(string code, string name, Guid groupId, decimal maxScore)
    {
        var entity = new Criteria { GroupId = groupId };
        entity.SetCode(code);
        entity.Rename(name);
        entity.SetMaxScore(maxScore);
        return entity;
    }

    /// <summary>
    /// Gán mã VÀ dựng lại <see cref="CodeSortKey"/> — một phép, không tách được. Mọi đường ghi
    /// mã (tạo, sửa, import) đi qua đây.
    /// </summary>
    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(CriteriaDomainErrors.CodeRequired);

        var trimmed = code.Trim();

        if (!CriteriaCode.IsWellFormed(trimmed))
            throw new DomainException(CriteriaDomainErrors.CodeFormatInvalid);

        if (CriteriaCode.HasSegmentTooLong(trimmed))
            throw new DomainException(CriteriaDomainErrors.CodeSegmentTooLong);

        Code = trimmed;
        CodeSortKey = CriteriaCode.BuildSortKey(trimmed);
    }

    /// <summary>
    /// Gán tên VÀ dựng lại <see cref="NameNormalized"/> — một phép, không tách được.
    /// </summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(CriteriaDomainErrors.NameRequired);

        Name = name.Trim();
        NameNormalized = SearchTextNormalizer.Normalize(Name);
    }

    public void MoveToGroup(Guid groupId) => GroupId = groupId;

    public void SetMaxScore(decimal maxScore)
    {
        if (maxScore <= 0)
            throw new DomainException(CriteriaDomainErrors.MaxScoreNotPositive);

        MaxScore = maxScore;
    }
}
