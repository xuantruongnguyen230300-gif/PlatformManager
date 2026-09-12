using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// Toàn bộ phép tổng hợp của Dashboard — spec/dashboard-dti/business-rules.md §1. Lớp THUẦN:
/// không DB, không thời gian hệ thống, không cấu hình. Đó là điều kiện để phần dễ sai nhất của
/// cụm có test chạy được mà không cần Docker.
///
/// <para><b>MỘT công thức, MỘT chỗ cài</b> (§1.1): <c>kpi.overallProgress</c>,
/// <c>groups[].progress</c>, <c>trend[].value</c> và <c>overallProgress</c> của từng kỳ trong
/// DB-3 đều gọi <see cref="ProgressOfPeriod"/>. Bốn chỗ tính riêng sẽ lệch nhau.
/// </para>
/// </summary>
public static class DashboardCalculator
{
    /// <summary>
    /// Ngưỡng so sánh. So bằng 0 tuyệt đối trên <c>decimal</c> sinh ra từ phép trừ là nguồn của
    /// ca "gần 0 nhưng không bằng 0" — dùng epsilon ở MỌI chỗ so sánh, kể cả <c>diff</c> (§3.1).
    /// </summary>
    public const decimal Epsilon = 0.001m;

    /// <summary>
    /// Bản ghi ĐẠI DIỆN cho mỗi chỉ tiêu trong một khoảng ngày: bản có <c>AssessmentDate</c> LỚN
    /// NHẤT nằm trong khoảng (§5.2 — file chủ là spec/danh-muc-dti/business-rules.md, Q46 khẳng
    /// định luật này áp cho CẢ HAI màn).
    ///
    /// <para>Hai bản ghi cùng chỉ tiêu lọt vào cùng một tuần là ca DB không chặn được (§1.4:
    /// <c>period</c> không phải một cột). Lấy bản mới nhất giữ cho kết quả vẫn XÁC ĐỊNH — và
    /// unique partial trên cặp (CriteriaId, AssessmentDate) bảo đảm không có hai bản còn sống
    /// cùng ngày để hoà nhau.</para>
    /// </summary>
    public static Dictionary<Guid, AssessmentFact> RepresentativeIn(
        IEnumerable<AssessmentFact> facts, DateOnly from, DateOnly to)
    {
        var representative = new Dictionary<Guid, AssessmentFact>();

        foreach (var fact in facts)
        {
            if (fact.AssessmentDate < from || fact.AssessmentDate > to)
                continue;

            if (!representative.TryGetValue(fact.CriteriaId, out var current)
                || fact.AssessmentDate > current.AssessmentDate)
            {
                representative[fact.CriteriaId] = fact;
            }
        }

        return representative;
    }

    /// <summary>
    /// Bình quân gia quyền theo <c>MaxScore</c> (§1.1):
    /// tổng của (progressPercent × maxScore) chia cho tổng maxScore.
    ///
    /// <para><b>Gia quyền chứ KHÔNG trung bình cộng</b>: một chỉ tiêu 30 điểm không thể có cùng
    /// sức nặng với một chỉ tiêu 10 điểm. Trung bình cộng cho ra một con số trông hợp lý và sai
    /// một cách không ai kiểm được.</para>
    ///
    /// <para><b>Chỉ tiêu KHÔNG có <c>progressPercent</c> bị loại khỏi CẢ tử số lẫn mẫu số</b>,
    /// không tính là 0 — tính là 0 làm một tuần chưa ai nhập liệu trông như một tuần tụt dốc.
    /// Loại hết thì trả <c>null</c> (vắng mặt trên dây), không phải 0.</para>
    /// </summary>
    public static decimal? WeightedProgress(
        IEnumerable<CriteriaFact> criteria, IReadOnlyDictionary<Guid, AssessmentFact> representative)
    {
        decimal weightedSum = 0;
        decimal weightSum = 0;

        foreach (var item in criteria)
        {
            if (!representative.TryGetValue(item.CriteriaId, out var fact) || fact.ProgressPercent is not { } progress)
                continue;

            weightedSum += progress * item.MaxScore;
            weightSum += item.MaxScore;
        }

        return weightSum <= 0 ? null : weightedSum / weightSum;
    }

    /// <summary>
    /// Tiến độ chung của MỘT KỲ bất kỳ (§1.1 + §1.2) — hàm dùng chung cho cả bốn nơi tính.
    ///
    /// <list type="bullet">
    ///   <item>Kỳ là một TUẦN: phép gộp bên dưới cho ra đúng một nhóm, tức bằng đúng bình quân
    ///   gia quyền của tuần đó.</item>
    ///   <item>Kỳ là THÁNG hoặc NĂM: <b>trung bình cộng tiến độ chung của các KỲ-TUẦN CÓ dữ
    ///   liệu</b> trong phạm vi, KHÔNG carry-forward. Kỳ nào không có thao tác nào cho một chỉ
    ///   tiêu thì chỉ tiêu đó bị loại khỏi mẫu tính của kỳ đó, chứ không kéo số tuần trước
    ///   sang.</item>
    /// </list>
    ///
    /// <para>Viết thành MỘT hàm thay vì hai nhánh là có chủ đích: hai hàm thì ca "một tuần" và ca
    /// "một tháng gồm một tuần" có thể cho hai kết quả khác nhau mà không ai đối chiếu.</para>
    ///
    /// <para><b>Carry-forward bị cấm</b> vì nó biến một tháng nghỉ thành một tháng tiến độ ổn
    /// định — con số không sai về phép tính, nó chỉ trả lời một câu hỏi khác với câu người đọc
    /// đang hỏi.</para>
    /// </summary>
    public static decimal? ProgressOfPeriod(
        IReadOnlyCollection<CriteriaFact> criteria, IEnumerable<AssessmentFact> facts, PeriodRange period)
    {
        var weekly = new List<decimal>();

        foreach (var week in WeeksWithDataIn(facts, period))
        {
            var representative = RepresentativeIn(facts, week.Start, week.End);
            if (WeightedProgress(criteria, representative) is { } value)
                weekly.Add(value);
        }

        return weekly.Count == 0 ? null : weekly.Sum() / weekly.Count;
    }

    /// <summary>
    /// Các tuần ISO CÓ dữ liệu nằm trong <paramref name="period"/>, tăng dần theo thời gian.
    ///
    /// <para>Cắt theo giao của tuần với kỳ: một tuần vắt qua hai tháng (vd tuần 31 của 2026 là
    /// 27/07 đến 02/08) chỉ góp phần ngày NẰM TRONG kỳ. Nhờ vậy ranh giới tháng không cần một
    /// luật riêng — nó rơi ra từ phép giao.</para>
    /// </summary>
    private static IEnumerable<PeriodRange> WeeksWithDataIn(IEnumerable<AssessmentFact> facts, PeriodRange period)
    {
        var weeks = new SortedDictionary<DateOnly, PeriodRange>();

        foreach (var fact in facts)
        {
            if (!period.Contains(fact.AssessmentDate))
                continue;

            var week = PeriodRange.IsoWeekOf(fact.AssessmentDate);
            if (weeks.ContainsKey(week.Start))
                continue;

            // Giao tuần với kỳ — xem docstring.
            var start = week.Start < period.Start ? period.Start : week.Start;
            var end = week.End > period.End ? period.End : week.End;
            weeks[week.Start] = week with { Start = start, End = end };
        }

        return weeks.Values;
    }

    /// <summary>
    /// Ba ô đếm tăng/không tăng/giảm (§1.4) — so <c>progressPercent</c> của từng chỉ tiêu giữa kỳ
    /// này và kỳ trước.
    ///
    /// <para><b>Chỉ tiêu thiếu dữ liệu ở MỘT TRONG HAI kỳ thì không vào ô nào cả.</b> Hệ quả bắt
    /// buộc phải biết: tổng ba ô có thể NHỎ HƠN <c>totalCriteria</c> — đó là hành vi đúng, đừng
    /// "vá" cho ba số cộng lại bằng tổng.</para>
    ///
    /// <para>Không có kỳ trước thì cả ba là 0 — đây chính là trạng thái NGAY SAU IMPORT (§1.6):
    /// ô "Không tăng" hiện <b>0</b>, KHÔNG phải 62, vì "không tăng" nghĩa là <i>đã đo hai lần và
    /// không đổi</i>, không phải <i>chưa đo lần nào</i>.</para>
    /// </summary>
    public static (int Up, int Flat, int Down) CountMovement(
        IEnumerable<CriteriaFact> criteria,
        IReadOnlyDictionary<Guid, AssessmentFact> current,
        IReadOnlyDictionary<Guid, AssessmentFact>? previous)
    {
        if (previous is null)
            return (0, 0, 0);

        var up = 0;
        var flat = 0;
        var down = 0;

        foreach (var item in criteria)
        {
            if (!current.TryGetValue(item.CriteriaId, out var now) || now.ProgressPercent is not { } nowValue)
                continue;
            if (!previous.TryGetValue(item.CriteriaId, out var before) || before.ProgressPercent is not { } beforeValue)
                continue;

            var delta = (decimal)nowValue - beforeValue;

            if (delta > Epsilon)
                up++;
            else if (delta < -Epsilon)
                down++;
            else
                flat++;
        }

        return (up, flat, down);
    }

    /// <summary>
    /// Số chỉ tiêu ở trạng thái Hoàn thành trong kỳ — đếm theo <c>status</c> người dùng chọn tay,
    /// KHÔNG suy từ điểm. Đây là ô KPI duy nhất có số thật ngay sau import (§1.6).
    /// </summary>
    public static int CountDone(IReadOnlyDictionary<Guid, AssessmentFact> representative)
    {
        var done = 0;

        foreach (var fact in representative.Values)
        {
            if (string.Equals(fact.Status, AssessmentStatuses.Done, StringComparison.Ordinal))
                done++;
        }

        return done;
    }

    /// <summary>
    /// Chênh lệch = thẩm định trừ tự đánh giá (Q25 — ĐẢO CHIỀU 2026-09-05). Một trong hai vế vắng
    /// thì chênh lệch vắng.
    ///
    /// <para>Dấu MANG NGHĨA: dương = thẩm định chấm CAO HƠN tự chấm (có lợi, tô xanh); âm = thẩm
    /// định CẮT BỚT điểm tự chấm (cần xử lý, tô đỏ). Với công thức cũ, một chỉ tiêu tự chấm 5 mà
    /// thẩm định cho 0 — tức bị bác trắng — hiện ra dương và tô XANH: con số đúng về phép trừ,
    /// sai hoàn toàn về thứ người đọc rút ra từ màu sắc.</para>
    /// </summary>
    public static decimal? Diff(decimal? selfScore, decimal? verifiedScore) =>
        selfScore is null || verifiedScore is null ? null : verifiedScore - selfScore;
}
