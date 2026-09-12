using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Nhóm chỉ tiêu DTI — spec/danh-muc-dti/business-rules.md §1.1.
///
/// <para><b>Danh mục ĐÓNG</b>: import không tự tạo nhóm (§6.3), nên 6 nhóm của §1.6 phải có sẵn
/// trước lần import đầu tiên. Chúng vào DB qua <c>BusinessSeeder</c> (Q67) — KHÔNG qua
/// <c>HasData</c>, vì <c>HasData</c> buộc viết cứng 6 <c>Guid</c> (ngược §1.1: UUID v7 sinh ở
/// ứng dụng) và biến mỗi lần BA sửa tên nhóm thành một migration mới.</para>
/// </summary>
public class CriteriaGroup : BaseEntity
{
    /// <summary>Mã nhóm — unique trong tập CHƯA xoá mềm (§1.4).</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Tên hiển thị — khớp NGUYÊN VĂN cột <c>Nhóm</c> của file import (§1.6).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Thứ tự hiển thị — BE sắp, FE không sắp lại (DM-1).</summary>
    public int DisplayOrder { get; private set; }

    private CriteriaGroup() { }

    public static CriteriaGroup Create(string code, string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(CriteriaGroupDomainErrors.CodeRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(CriteriaGroupDomainErrors.NameRequired);

        return new CriteriaGroup
        {
            Code = code.Trim(),
            Name = name.Trim(),
            DisplayOrder = displayOrder,
        };
    }

    /// <summary>
    /// Đồng bộ lại định nghĩa của một nhóm đã có (tên + thứ tự) — <c>Code</c> là khoá tra nên
    /// không đổi ở đây. Dùng bởi <c>BusinessSeeder</c> khi bảng seed ở §1.6 đổi.
    /// </summary>
    public void UpdateDefinition(string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(CriteriaGroupDomainErrors.NameRequired);

        Name = name.Trim();
        DisplayOrder = displayOrder;
    }
}
