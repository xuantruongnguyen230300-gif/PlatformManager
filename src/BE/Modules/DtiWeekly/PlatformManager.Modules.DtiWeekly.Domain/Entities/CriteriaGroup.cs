using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Modules.DtiWeekly.Domain.Entities;

/// <summary>
/// Danh mục nhóm chỉ tiêu DTI — 6 dòng seed cố định (Code "1".."6"), có thể có thêm
/// nhóm mới tự sinh khi Import CSV gặp tên nhóm lạ (xem AssessmentUpsertService/
/// CsvImportService ở Application). Không có CRUD UI riêng cho entity này.
/// </summary>
public class CriteriaGroup : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    private CriteriaGroup() { }

    public static CriteriaGroup Create(string code, string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("CRITERIA_GROUP_CODE_REQUIRED", "Mã nhóm không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("CRITERIA_GROUP_NAME_REQUIRED", "Tên nhóm không được để trống.");

        return new CriteriaGroup
        {
            Id = EntityId.New(),
            Code = code.Trim(),
            Name = name.Trim(),
            DisplayOrder = displayOrder,
        };
    }
}
