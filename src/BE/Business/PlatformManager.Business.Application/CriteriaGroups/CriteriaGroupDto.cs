namespace PlatformManager.Business.Application.CriteriaGroups;

/// <summary>
/// Một nhóm chỉ tiêu trên dây — doc/contracts/danh-muc-dti.md DM-1.
///
/// <para><b>Giữ HAI trường rời, không ghép sẵn.</b> Giao diện hiện <c>code. name</c>
/// (vd <c>1. Hạ tầng và Nền tảng số</c>) nhưng phép ghép là việc của FE — DB và API giữ
/// <c>code</c> + <c>name</c> tách nhau (§1.6). Ghép ở BE làm chuỗi đó không dùng lại được ở chỗ
/// cần tên trần: cột <c>Nhóm</c> của file xuất phải là TÊN TRẦN để round-trip qua import (Q55).</para>
/// </summary>
public sealed record CriteriaGroupDto(Guid Id, string Code, string Name, int DisplayOrder);
