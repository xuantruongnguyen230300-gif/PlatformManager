using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>
/// MỘT chỗ dựng <c>IQueryable</c> đã lọc + đã sắp cho lưới chỉ tiêu — dùng chung bởi DM-2, bảng
/// chi tiết của DB-1, và (vòng 2) endpoint export DB-4.
///
/// <para><b>Vì sao phải là một chỗ</b> (Q68, doc/contracts/dashboard.md
/// §"Projection của export là RIÊNG"): kiểm nghiệm thu <i>"số dòng file xuất = số phần tử
/// <c>data.table</c>"</i> là thứ DUY NHẤT cưỡng chế hai đường còn lọc giống nhau. Hai projection
/// rời nhau nghĩa là sửa bộ lọc ở một đường sẽ KHÔNG làm đường kia đỏ. Vì vậy: một chỗ lọc, hai
/// (rồi ba) phép <c>Select</c> khác nhau.</para>
/// </summary>
internal static class CriteriaQueries
{
    /// <summary>
    /// Áp bộ lọc <c>search</c>/<c>groupId</c>/<c>status</c>. Soft-delete KHÔNG áp ở đây — global
    /// query filter của <c>PlatformManagerDbContext</c> đã phủ mọi <c>BaseEntity</c>; lặp lại là
    /// tạo nguồn sự thật thứ hai cho cùng một luật.
    /// </summary>
    public static IQueryable<Criteria> Filtered(PlatformManagerDbContext db, CriteriaFilterSpec spec)
    {
        var query = db.Set<Criteria>().AsNoTracking();

        if (spec.GroupId is { } groupId)
            query = query.Where(c => c.GroupId == groupId);

        if (spec.SearchNormalized is { Length: > 0 } name && spec.SearchCode is { Length: > 0 } code)
        {
            // Q59 — MỘT dòng khớp nếu khớp TÊN hoặc khớp MÃ:
            //  • tên: CHỨA chuỗi, so trên cột chuẩn hoá (Q47) đã bỏ dấu + chữ thường ở CẢ hai vế;
            //  • mã: khớp theo ĐOẠN — Code = q HOẶC Code bắt đầu bằng q + ".". Gõ "4.2" ra "4.2"
            //    và mọi "4.2.x", KHÔNG ra "4.20"–"4.29". Đây là khác biệt duy nhất giữa nhánh này
            //    và một StartsWith trần, và nó là toàn bộ lý do Q59 tồn tại.
            var codePrefix = code + ".";

            query = query.Where(c =>
                c.NameNormalized.Contains(name)
                || c.Code == code
                || c.Code.StartsWith(codePrefix));
        }

        if (spec.Status is { Length: > 0 } status)
        {
            // Lọc theo trạng thái của BẢN GHI ĐẠI DIỆN cho kỳ (§5.2): bản có AssessmentDate lớn
            // nhất trong khoảng ngày của kỳ. Truy vấn con vô hướng có ORDER BY + LIMIT 1 — cùng
            // luật chọn bản đại diện với mọi chỗ khác, chỉ khác là nó chạy phía SQL.
            var from = spec.Period.Start;
            var to = spec.Period.End;

            query = query.Where(c =>
                db.Set<CriteriaAssessment>()
                    .Where(a => a.CriteriaId == c.Id && a.AssessmentDate >= from && a.AssessmentDate <= to)
                    .OrderByDescending(a => a.AssessmentDate)
                    .Select(a => a.Status)
                    .FirstOrDefault() == status);
        }

        return query;
    }

    /// <summary>
    /// Thứ tự dòng: theo mã CHỈ TIÊU, TỰ NHIÊN (<c>4.2 &lt; 4.10 &lt; 4.22.11</c>) — BE sắp, FE
    /// không sắp lại, và không có tham số sắp xếp. Cài bằng cột <c>CodeSortKey</c> tính sẵn
    /// (Q53); sắp phụ theo <c>Code</c> để thứ tự XÁC ĐỊNH khi hai mã cho cùng khoá.
    /// </summary>
    public static IOrderedQueryable<Criteria> Sorted(IQueryable<Criteria> query) =>
        query.OrderBy(c => c.CodeSortKey).ThenBy(c => c.Code);
}
