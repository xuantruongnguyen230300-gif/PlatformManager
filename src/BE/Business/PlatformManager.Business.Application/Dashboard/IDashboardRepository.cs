using PlatformManager.Business.Application.Criteria;

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// Một chỉ tiêu CHƯA xoá mềm, kèm nhóm của nó — đầu vào cố định của mọi phép tổng hợp.
/// </summary>
public sealed record CriteriaFact(
    Guid CriteriaId,
    string Code,
    string CodeSortKey,
    string Name,
    Guid GroupId,
    string GroupCode,
    string GroupName,
    int GroupDisplayOrder,
    decimal MaxScore);

/// <summary>
/// Một bản ghi đánh giá, rút gọn về đúng những trường phép tổng hợp cần. Chỉ mang
/// <c>ProgressPercent</c> và <c>Status</c>: hai ô điểm và ghi chú chỉ xuất hiện ở bảng chi tiết,
/// và bảng đó đi đường riêng (<see cref="IDashboardRepository.GetTableAsync"/>).
/// </summary>
public sealed record AssessmentFact(
    Guid CriteriaId,
    DateOnly AssessmentDate,
    int? ProgressPercent,
    string? Status);

/// <summary>
/// Đọc DỮ LIỆU THÔ cho Dashboard. Hiện thực dùng EF sống ở <c>Business.Persistence</c>.
///
/// <para><b>Vì sao repository trả sự kiện thô thay vì trả sẵn số tổng hợp:</b> phép tổng hợp
/// (§1.1–§1.5) là NƠI DỄ SAI NHẤT của cả cụm — bình quân gia quyền, loại chỉ tiêu thiếu dữ liệu
/// khỏi mẫu, epsilon, "kỳ liền trước CÓ DỮ LIỆU", cửa sổ 12 tuần cắt ở đầu năm. Giữ nó ở
/// <see cref="DashboardCalculator"/> — một lớp THUẦN, không DB — là điều kiện để nó có test chạy
/// được mà không cần Docker (doc/huong_dan/wiki-core/be/04-testing-strategy.md).</para>
///
/// <para><b>Quy mô đã cân:</b> 62 chỉ tiêu × ~52 tuần ≈ vài nghìn dòng cho một năm. Đọc hai năm
/// (năm đang xem + năm trước, cần cho ô <c>So với kỳ trước</c> khi kỳ hiện tại là kỳ đầu năm) vẫn
/// nằm rất xa ngưỡng cần tổng hợp phía SQL. Ngưỡng đó đổi thì đổi ở ĐÂY, đừng rải điều kiện vào
/// calculator.</para>
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Mọi chỉ tiêu chưa xoá mềm, kèm nhóm — KHÔNG áp bộ lọc của bảng chi tiết.</summary>
    Task<IReadOnlyList<CriteriaFact>> GetCriteriaAsync(CancellationToken ct);

    /// <summary>
    /// Bản ghi đánh giá chưa xoá mềm có <c>AssessmentDate</c> trong <c>[from, to]</c>, thuộc chỉ
    /// tiêu chưa xoá mềm. Chỉ tiêu đã xoá mềm biến khỏi MỌI phép tổng hợp, kể cả khi xem lại kỳ
    /// cũ mà lúc đó nó còn sống (§5.5) — nếu không, tổng số chỉ tiêu của một kỳ sẽ đổi tuỳ thời
    /// điểm người ta mở màn hình lên xem.
    /// </summary>
    Task<IReadOnlyList<AssessmentFact>> GetAssessmentFactsAsync(DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>Mọi năm CÓ dữ liệu (theo <c>AssessmentDate</c>), tăng dần.</summary>
    Task<IReadOnlyList<int>> GetYearsWithDataAsync(CancellationToken ct);

    /// <summary>
    /// Bảng chi tiết 9 cột, đã áp bộ lọc <c>search</c>/<c>groupId</c>/<c>status</c> và đã sắp
    /// theo mã tự nhiên. KHÔNG phân trang — DB-1 không có tham số trang.
    ///
    /// <para>Dùng CHUNG chỗ dựng <c>IQueryable</c> đã lọc với DM-2 (Q68): hai đường chỉ khác phép
    /// <c>Select</c>.</para>
    /// </summary>
    Task<IReadOnlyList<DashboardTableRowDto>> GetTableAsync(CriteriaFilterSpec spec, CancellationToken ct);

    /// <summary>
    /// <b>DB-4</b> — 12 cột của FILE XUẤT, projection RIÊNG (Q68). Dùng CHUNG object bộ lọc và
    /// CHUNG chỗ dựng <c>IQueryable</c> với <see cref="GetTableAsync"/>; khác đúng phép
    /// <c>Select</c>.
    ///
    /// <para>Kiểm nghiệm thu bắt buộc: số dòng trả về ĐÚNG BẰNG số phần tử <c>data.table</c> của
    /// <c>GET /api/dashboard</c> với cùng bộ tham số. Đây là thứ DUY NHẤT cưỡng chế hai đường còn
    /// lọc giống nhau.</para>
    /// </summary>
    Task<IReadOnlyList<DashboardExportRow>> GetExportRowsAsync(CriteriaFilterSpec spec, CancellationToken ct);

    /// <summary>
    /// Nhãn nhóm dạng <c>Code. Name</c> (Q42) cho dòng <c>Bộ lọc đang áp</c> — MÔ TẢ cho người đọc,
    /// khác cột <c>Nhóm</c> trong bảng dữ liệu (tên trần, Q55, để round-trip không gãy).
    /// </summary>
    Task<string?> FindGroupLabelAsync(Guid groupId, CancellationToken ct);

    /// <summary>Tên đầy đủ của người đang xuất — dòng <c>Người xuất</c> của khối nhận dạng kỳ.</summary>
    Task<string?> FindUserFullNameAsync(Guid userId, CancellationToken ct);
}
