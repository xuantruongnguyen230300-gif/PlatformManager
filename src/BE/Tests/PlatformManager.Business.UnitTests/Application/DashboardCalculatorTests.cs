using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Domain.Entities;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Công thức tổng hợp — spec/dashboard-dti/business-rules.md §1. Đây là phần dễ sai nhất của cả
/// cụm, và cũng là phần duy nhất kiểm được mà không cần Postgres.
/// </summary>
public class DashboardCalculatorTests
{
    private static readonly Guid GroupA = Guid.CreateVersion7();
    private static readonly Guid GroupB = Guid.CreateVersion7();

    private static CriteriaFact Criteria(string code, decimal maxScore, Guid? group = null) => new(
        Guid.CreateVersion7(), code, code, $"Chỉ tiêu {code}",
        group ?? GroupA, "1", "Nhóm A", 1, maxScore);

    private static AssessmentFact Assessment(
        CriteriaFact criteria, string date, int? progress, string? status = null) =>
        new(criteria.CriteriaId, DateOnly.Parse(date), progress, status);

    /// <summary>
    /// Gia quyền theo <c>MaxScore</c>, KHÔNG trung bình cộng. Chỉ tiêu 30 điểm ở 100% và chỉ tiêu
    /// 10 điểm ở 0% cho ra 75%, không phải 50% — trung bình cộng cho một con số trông hợp lý và
    /// sai một cách không ai kiểm được.
    /// </summary>
    [Fact]
    public void WeightedProgress_WeighsBy_MaxScore()
    {
        var heavy = Criteria("1.1", 30m);
        var light = Criteria("1.2", 10m);

        var representative = DashboardCalculator.RepresentativeIn(
            [Assessment(heavy, "2026-08-12", 100), Assessment(light, "2026-08-12", 0)],
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 16));

        Assert.Equal(75m, DashboardCalculator.WeightedProgress([heavy, light], representative));
    }

    /// <summary>
    /// Chỉ tiêu KHÔNG có <c>progressPercent</c> bị loại khỏi CẢ tử số lẫn mẫu số. Tính nó là 0
    /// làm một tuần chưa ai nhập liệu trông như một tuần tụt dốc.
    /// </summary>
    [Fact]
    public void WeightedProgress_Excludes_CriteriaWithoutProgress_FromBothSides()
    {
        var scored = Criteria("1.1", 10m);
        var blank = Criteria("1.2", 30m);

        var representative = DashboardCalculator.RepresentativeIn(
            [Assessment(scored, "2026-08-12", 80), Assessment(blank, "2026-08-12", null)],
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 16));

        // 80 chứ không phải 80×10/(10+30) = 20 — chỉ tiêu trống rời khỏi mẫu số, không kéo tổng xuống.
        Assert.Equal(80m, DashboardCalculator.WeightedProgress([scored, blank], representative));
    }

    /// <summary>Loại hết ⇒ VẮNG MẶT (null), KHÔNG phải 0 — §1.1 và §1.6 b.</summary>
    [Fact]
    public void WeightedProgress_IsAbsent_WhenNoCriteriaHasProgress()
    {
        var criteria = Criteria("1.1", 10m);

        var representative = DashboardCalculator.RepresentativeIn(
            [Assessment(criteria, "2026-08-12", null)],
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 16));

        Assert.Null(DashboardCalculator.WeightedProgress([criteria], representative));
    }

    /// <summary>
    /// §5.2 + Q46: bản ĐẠI DIỆN là bản có <c>AssessmentDate</c> LỚN NHẤT trong kỳ. Ca hai bản ghi
    /// cùng chỉ tiêu lọt vào cùng một tuần là ca DB không chặn được — luật này giữ cho kết quả
    /// vẫn xác định, và giữ cho hai màn không tính ra hai con số khác nhau cho cùng một tuần.
    /// </summary>
    [Fact]
    public void RepresentativeIn_Picks_TheLatestAssessmentDate()
    {
        var criteria = Criteria("1.1", 10m);

        var representative = DashboardCalculator.RepresentativeIn(
            [
                Assessment(criteria, "2026-08-10", 40),
                Assessment(criteria, "2026-08-14", 70),
                Assessment(criteria, "2026-08-12", 55),
            ],
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 16));

        Assert.Equal(70, representative[criteria.CriteriaId].ProgressPercent);
    }

    /// <summary>
    /// §1.2 — kỳ THÁNG là trung bình cộng tiến độ chung của các KỲ-TUẦN CÓ dữ liệu, KHÔNG phải
    /// bình quân gia quyền trên toàn tháng. Hai cách cho kết quả khác nhau, và ca này chọn số
    /// liệu để chúng khác nhau rõ ràng.
    /// </summary>
    [Fact]
    public void ProgressOfPeriod_ForMonth_AveragesWeeklyValues()
    {
        var criteria = Criteria("1.1", 10m);

        AssessmentFact[] facts =
        [
            Assessment(criteria, "2026-08-05", 20),   // tuần 32
            Assessment(criteria, "2026-08-12", 40),   // tuần 33
            Assessment(criteria, "2026-08-19", 90),   // tuần 34
        ];

        // (20 + 40 + 90) / 3 = 50 — trung bình cộng của BA TUẦN, không phải giá trị tuần cuối.
        Assert.Equal(50m, DashboardCalculator.ProgressOfPeriod([criteria], facts, PeriodRange.CalendarMonth(2026, 8)));
    }

    /// <summary>
    /// Với kỳ TUẦN, hàm gộp phải cho ra đúng bình quân gia quyền của tuần đó — tức "một tuần" và
    /// "một tháng gồm một tuần" không được cho hai kết quả khác nhau. Đó là lý do §1.1 và §1.2
    /// dùng chung MỘT hàm thay vì hai nhánh.
    /// </summary>
    [Fact]
    public void ProgressOfPeriod_ForSingleWeek_EqualsWeightedProgress()
    {
        var heavy = Criteria("1.1", 30m);
        var light = Criteria("1.2", 10m);

        AssessmentFact[] facts =
        [
            Assessment(heavy, "2026-08-12", 100),
            Assessment(light, "2026-08-12", 0),
        ];

        Assert.Equal(75m, DashboardCalculator.ProgressOfPeriod([heavy, light], facts, PeriodRange.IsoWeek(2026, 33)));
    }

    /// <summary>
    /// KHÔNG carry-forward (§1.2): tuần không có thao tác nào thì không góp vào trung bình, chứ
    /// không kéo số tuần trước sang. Carry-forward biến một tháng nghỉ thành một tháng tiến độ ổn
    /// định — con số không sai về phép tính, nó chỉ trả lời một câu hỏi khác.
    /// </summary>
    [Fact]
    public void ProgressOfPeriod_DoesNot_CarryForward_IntoEmptyWeeks()
    {
        var criteria = Criteria("1.1", 10m);

        AssessmentFact[] facts = [Assessment(criteria, "2026-08-12", 60)];

        // Chỉ tuần 33 có dữ liệu ⇒ trung bình của đúng một tuần = 60, KHÔNG bị pha loãng bởi bốn
        // tuần rỗng còn lại của tháng 8.
        Assert.Equal(60m, DashboardCalculator.ProgressOfPeriod([criteria], facts, PeriodRange.CalendarMonth(2026, 8)));
    }

    /// <summary>
    /// Tuần vắt qua hai tháng (tuần 31/2026 = 27/07–02/08) chỉ góp phần ngày NẰM TRONG kỳ. Ca này
    /// canh phép giao tuần ∩ kỳ: hai bản ghi của cùng chỉ tiêu, một ở tháng 7 một ở tháng 8, phải
    /// cho hai con số khác nhau tuỳ tháng đang xem.
    /// </summary>
    [Fact]
    public void ProgressOfPeriod_ClipsWeek_ToThePeriodBoundary()
    {
        var criteria = Criteria("1.1", 10m);

        AssessmentFact[] facts =
        [
            Assessment(criteria, "2026-07-28", 30),   // tuần 31, phần nằm trong tháng 7
            Assessment(criteria, "2026-08-01", 90),   // tuần 31, phần nằm trong tháng 8
        ];

        Assert.Equal(30m, DashboardCalculator.ProgressOfPeriod([criteria], facts, PeriodRange.CalendarMonth(2026, 7)));
        Assert.Equal(90m, DashboardCalculator.ProgressOfPeriod([criteria], facts, PeriodRange.CalendarMonth(2026, 8)));
    }

    /// <summary>
    /// §1.4 — chỉ tiêu thiếu dữ liệu ở MỘT TRONG HAI kỳ không vào ô nào, nên tổng ba ô có thể NHỎ
    /// HƠN <c>totalCriteria</c>. Đừng "vá" cho ba số cộng lại bằng tổng.
    /// </summary>
    [Fact]
    public void CountMovement_Skips_CriteriaMissingInEitherPeriod()
    {
        var rising = Criteria("1.1", 10m);
        var flat = Criteria("1.2", 10m);
        var onlyNow = Criteria("1.3", 10m);

        AssessmentFact[] facts =
        [
            Assessment(rising, "2026-08-05", 40), Assessment(rising, "2026-08-12", 70),
            Assessment(flat, "2026-08-05", 50), Assessment(flat, "2026-08-12", 50),
            Assessment(onlyNow, "2026-08-12", 90),
        ];

        var previous = DashboardCalculator.RepresentativeIn(facts, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 9));
        var current = DashboardCalculator.RepresentativeIn(facts, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 16));

        var (up, notChanged, down) = DashboardCalculator.CountMovement([rising, flat, onlyNow], current, previous);

        Assert.Equal(1, up);
        Assert.Equal(1, notChanged);
        Assert.Equal(0, down);
        Assert.True(up + notChanged + down < 3, "Tổng ba ô phải NHỎ HƠN số chỉ tiêu — đó là hành vi đúng của §1.4.");
    }

    /// <summary>
    /// §1.6 a — NGAY SAU IMPORT, ô "Không tăng" hiện <b>0</b>, KHÔNG phải 62. Trực giác nói
    /// "chưa ai nhập gì thì cả 62 chỉ tiêu đều không tăng"; luật nói ngược, vì lúc đó CẢ HAI kỳ
    /// đều thiếu dữ liệu. "Không tăng" nghĩa là đã đo hai lần và không đổi.
    /// </summary>
    [Fact]
    public void CountMovement_RightAfterImport_IsAllZero()
    {
        var criteria = Criteria("1.1", 10m);
        var current = DashboardCalculator.RepresentativeIn(
            [Assessment(criteria, "2026-08-12", null, AssessmentStatuses.Done)],
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 16));

        var (up, notChanged, down) = DashboardCalculator.CountMovement([criteria], current, previous: null);

        Assert.Equal(0, up);
        Assert.Equal(0, notChanged);
        Assert.Equal(0, down);

        // ...trong khi ô "Hoàn thành" có SỐ THẬT: nó đếm theo status, mà status đến thẳng từ cột
        // Trạng thái của file import. Đây là ô KPI duy nhất nói được điều gì đó lúc đó.
        Assert.Equal(1, DashboardCalculator.CountDone(current));
    }

    /// <summary>
    /// Q25 — dấu của <c>Chênh lệch</c>. Hai ca kiểm lấy thẳng từ §3.1: chỉ tiêu 1.1 (tự chấm 7,04
    /// / thẩm định 10) phải ra <b>+2,96</b>; chỉ tiêu 1.4 (tự chấm 5 / thẩm định 0 — bị bác
    /// trắng) phải ra <b>−5,00</b>. Ra −2,96 nghĩa là công thức còn theo chiều CŨ, và khi đó một
    /// chỉ tiêu bị bác trắng sẽ hiện dương và tô XANH.
    /// </summary>
    [Fact]
    public void Diff_Is_VerifiedMinusSelf()
    {
        Assert.Equal(2.96m, DashboardCalculator.Diff(7.04m, 10m));
        Assert.Equal(-5.00m, DashboardCalculator.Diff(5m, 0m));
    }

    /// <summary>Một trong hai vế vắng ⇒ chênh lệch vắng (§3.1).</summary>
    [Theory]
    [InlineData(null, 10d)]
    [InlineData(10d, null)]
    [InlineData(null, null)]
    public void Diff_IsAbsent_WhenEitherSideIsMissing(double? self, double? verified)
    {
        Assert.Null(DashboardCalculator.Diff((decimal?)self, (decimal?)verified));
    }

    /// <summary>Nhóm khác nhau tính riêng — <c>groups[].progress</c> chỉ nhìn chỉ tiêu của nhóm mình.</summary>
    [Fact]
    public void ProgressOfPeriod_CanBeScoped_ToASingleGroup()
    {
        var inA = Criteria("1.1", 10m, GroupA);
        var inB = Criteria("2.1", 10m, GroupB);

        AssessmentFact[] facts = [Assessment(inA, "2026-08-12", 20), Assessment(inB, "2026-08-12", 80)];
        var week = PeriodRange.IsoWeek(2026, 33);

        Assert.Equal(20m, DashboardCalculator.ProgressOfPeriod([inA], facts, week));
        Assert.Equal(80m, DashboardCalculator.ProgressOfPeriod([inB], facts, week));
        Assert.Equal(50m, DashboardCalculator.ProgressOfPeriod([inA, inB], facts, week));
    }
}
