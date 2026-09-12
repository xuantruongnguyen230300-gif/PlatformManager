using System.Globalization;

namespace PlatformManager.Business.Application.Common;

/// <summary>
/// Dựng và đọc chuỗi ĐỊNH DANH của một kỳ trên dây — <c>"all"</c> · <c>"YYYY-Www"</c> ·
/// <c>"YYYY-MM"</c> (doc/contracts/danh-muc-dti.md §1). Đây là KHOÁ, khác hẳn NHÃN hiển thị
/// (<see cref="PeriodLabels"/>): khoá do máy đọc, nhãn do người đọc.
/// </summary>
public static class PeriodValues
{
    public const string All = "all";

    /// <summary>Số tuần luôn 2 chữ số: <c>2026-W07</c>, không phải <c>2026-W7</c>.</summary>
    public static string Week(int isoYear, int week) =>
        string.Create(CultureInfo.InvariantCulture, $"{isoYear:D4}-W{week:D2}");

    public static string Month(int year, int month) =>
        string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}");
}
