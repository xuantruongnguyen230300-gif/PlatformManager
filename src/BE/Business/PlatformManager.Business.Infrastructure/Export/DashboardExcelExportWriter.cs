using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PlatformManager.Business.Application.Dashboard;

namespace PlatformManager.Business.Infrastructure.Export;

/// <summary>
/// Bộ ghi file <c>.xlsx</c> của DB-4 — bố cục đúng hai file mẫu đã duyệt
/// (spec/dashboard-dti/business-rules.md §4.2 – §4.5).
///
/// <para>🛑 <b>Dùng NPOI TRỰC TIẾP ở tầng này, KHÔNG qua seam <c>ITabularWriter</c> của Core</b> —
/// chốt Q9, doc/huong_dan/wiki-core/be/15-import-export.md §7. Chữ ký của seam đó (danh sách cột +
/// luồng dòng) chỉ diễn đạt được <i>một bảng phẳng bắt đầu từ ô đầu tiên</i>; bố cục ở đây có khối
/// nhận dạng kỳ nhiều dòng, một dòng <c>TỔNG CỘNG</c>, nền màu cho hàng header và tên sheet đổi
/// theo kỳ. Ép nó qua seam hiện tại chỉ có hai lối, cả hai đều tệ hơn việc chưa dựng seam.</para>
///
/// <para><b>Ràng buộc vẫn còn hiệu lực dù chưa có seam:</b> NPOI chỉ được reference ở tầng
/// <c>*.Infrastructure</c>. File này là chỗ DUY NHẤT của tầng nghiệp vụ biết tới nó; interface
/// <see cref="IDashboardExportWriter"/> ở <c>Business.Application</c> là ranh giới.</para>
///
/// <para><b><c>XSSFWorkbook</c> chứ không <c>SXSSFWorkbook</c>:</b> quy mô thật là vài chục dòng —
/// rất xa ngưỡng cần ghi luồng xuống đĩa tạm. Đổi sang <c>SXSSF</c> khi (và chỉ khi) có file lớn
/// thật; đổi trước là trả chi phí phức tạp cho một rủi ro chưa đo được.</para>
/// </summary>
public sealed class DashboardExcelExportWriter : IDashboardExportWriter
{
    /// <summary>
    /// 12 tiêu đề cột, ĐÚNG thứ tự §4.3. <c>Tiến độ %</c> chèn ở vị trí 11, ngay TRƯỚC
    /// <c>Minh chứng/Ghi chú</c>.
    ///
    /// <para>⚠️ <b>11 tiêu đề đầu phải trùng NGUYÊN VĂN tên cột của file IMPORT</b> — export là
    /// SUPERSET của import, và đó là ràng buộc thiết kế chứ không phải trùng hợp: import khớp
    /// header theo TÊN, không theo vị trí, nên đổi một tiêu đề ở đây là phá round-trip
    /// <i>export → sửa → import</i>.</para>
    ///
    /// <para><b>Cột 7 là NGOẠI LỆ DUY NHẤT, và nó an toàn:</b> tiêu đề ghi rõ công thức thay vì
    /// <c>Chênh lệch</c> trần (Q25), vì cột đó trong file gốc của BA tính theo chiều CŨ nên ngược
    /// dấu ở 27/62 dòng — ai đặt hai file cạnh nhau mà không có tiêu đề nói rõ chiều tính sẽ kết
    /// luận một trong hai file bị lỗi. Đổi được vì <c>Chênh lệch</c> là trường TÍNH: đường import
    /// bỏ qua nó hoàn toàn và không đòi nó phải có mặt.</para>
    /// </summary>
    private static readonly string[] Headers =
    [
        "Mã",
        "Chỉ tiêu",
        "Nhóm",
        "Điểm tối đa",
        "Tự đánh giá",
        "Thẩm định",
        "Chênh lệch (Thẩm định − Tự đánh giá)",
        "Trạng thái",
        "Phụ trách",
        "Hạn xử lý",
        "Tiến độ %",
        "Minh chứng/Ghi chú",
    ];

    /// <summary>Bề rộng cột, đơn vị 1/256 ký tự của NPOI. Hai cột văn xuôi rộng hẳn ra.</summary>
    private static readonly int[] ColumnWidths =
    [
        10 * 256, 52 * 256, 28 * 256, 12 * 256, 12 * 256, 12 * 256,
        22 * 256, 22 * 256, 18 * 256, 13 * 256, 11 * 256, 46 * 256,
    ];

    /// <summary>
    /// Ô trống ghi <c>—</c>, KHÔNG để rỗng (§4.3). Ba lý do: người đọc phân biệt được "chưa có dữ
    /// liệu" với "file lỗi"; cột không bị Excel co lại; và đường import bỏ qua <c>—</c> như bỏ qua ô
    /// trống — nếu không, round-trip sẽ ghi chính chữ <c>—</c> vào database.
    /// </summary>
    private const string EmptyCell = "—";

    /// <summary>Nền hàng header, đúng hai file mẫu đã duyệt.</summary>
    private static readonly byte[] HeaderFill = [0x0F, 0x5B, 0xD7];

    public byte[] Write(DashboardExportLayout layout, IReadOnlyList<DashboardExportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(rows);

        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(layout.SheetName);

        var styles = new ExportStyles(workbook);

        for (var column = 0; column < ColumnWidths.Length; column++)
            sheet.SetColumnWidth(column, ColumnWidths[column]);

        WriteIdentityBlock(sheet, styles, layout, rows.Count);

        // Dòng 11 để trống; header ở dòng 12 (1-based) = chỉ số 11. KHÔNG ghim con số này thành một
        // hằng số rời: nó suy ra từ chính khối nhận dạng kỳ vừa ghi, nên thêm/bớt một dòng ở khối
        // đó không để lại một con số lạc ở đây (§4.5 dặn đúng điều này — dòng header đã dịch xuống
        // một lần khi "Bộ lọc đang áp" được thêm).
        var headerRowIndex = IdentityBlockRowCount + 1;

        WriteHeader(sheet, styles, headerRowIndex);

        var firstDataRow = headerRowIndex + 1;
        for (var i = 0; i < rows.Count; i++)
            WriteDataRow(sheet.CreateRow(firstDataRow + i), styles, rows[i]);

        // Một dòng TRỐNG giữa dữ liệu và dòng tổng — đúng bố cục đã duyệt.
        WriteTotalRow(sheet, styles, firstDataRow + rows.Count + 1, rows);

        using var buffer = new MemoryStream();

        // NPOI đóng stream sau khi ghi, nên phải lấy mảng byte bằng ToArray() TRƯỚC khi ra khỏi
        // using — đọc lại từ stream đã đóng là một IOException không liên quan gì tới Excel.
        workbook.Write(buffer, leaveOpen: true);

        return buffer.ToArray();
    }

    /// <summary>Số dòng của khối nhận dạng kỳ (§4.2) — dòng 1 tiêu đề + 9 dòng nhãn/giá trị.</summary>
    private const int IdentityBlockRowCount = 10;

    private static void WriteIdentityBlock(
        ISheet sheet, ExportStyles styles, DashboardExportLayout layout, int rowCount)
    {
        var title = sheet.CreateRow(0);
        var titleCell = title.CreateCell(0);
        titleCell.SetCellValue("BÁO CÁO TIẾN ĐỘ CHUYỂN ĐỔI SỐ (DTI) — XÃ CẦN GIUỘC");
        titleCell.CellStyle = styles.Title;

        // Cặp (nhãn cột A, giá trị cột B) — đúng thứ tự dòng 2..10 của §4.2.
        var lines = new (string Label, string Value)[]
        {
            ("Kỳ báo cáo", layout.PeriodLabel),
            ("Từ ngày", Day(layout.PeriodStart)),
            ("Đến ngày", Day(layout.PeriodEnd)),
            (layout.PeriodScopeLabel, layout.PeriodScopeValue),
            ("Năm", layout.Year.ToString(CultureInfo.InvariantCulture)),

            // Số dòng THỰC XUẤT, không ghi cứng 62 — export tôn trọng bộ lọc đang áp (Q23).
            ("Số chỉ tiêu", rowCount.ToString(CultureInfo.InvariantCulture)),

            // Ngày xuất cho người cầm file biết mình đang giữ ẢNH CHỤP lúc nào. Đừng bỏ dòng này
            // cho gọn: kỳ đã qua vẫn ghi được (Q20), nên một file tải hôm nay có thể mâu thuẫn với
            // số của chính kỳ đó vào tháng sau — dòng này là thứ giữ cho điều đó vô hại.
            ("Ngày xuất", layout.ExportedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),

            ("Người xuất", layout.ExportedBy),
            ("Bộ lọc đang áp", layout.FilterSummary),
        };

        for (var i = 0; i < lines.Length; i++)
        {
            var row = sheet.CreateRow(i + 1);

            var label = row.CreateCell(0);
            label.SetCellValue(lines[i].Label);
            label.CellStyle = styles.IdentityLabel;

            row.CreateCell(1).SetCellValue(lines[i].Value);
        }
    }

    private static void WriteHeader(ISheet sheet, ExportStyles styles, int rowIndex)
    {
        var row = sheet.CreateRow(rowIndex);

        for (var column = 0; column < Headers.Length; column++)
        {
            var cell = row.CreateCell(column);
            cell.SetCellValue(Headers[column]);
            cell.CellStyle = styles.Header;
        }
    }

    private static void WriteDataRow(IRow row, ExportStyles styles, DashboardExportRow source)
    {
        Text(row, 0, source.Code, styles.Text);
        Text(row, 1, source.Name, styles.Wrapped);
        Text(row, 2, source.GroupName, styles.Wrapped);

        Number(row, 3, source.MaxScore, styles);
        Number(row, 4, source.SelfScore, styles);
        Number(row, 5, source.VerifiedScore, styles);
        Number(row, 6, source.Diff, styles);

        Text(row, 7, source.Status ?? EmptyCell, styles.Wrapped);
        Text(row, 8, source.OwnerName ?? EmptyCell, styles.Wrapped);

        // Ngày ghi dạng NGÀY THẬT, không phải chuỗi — người nhận còn lọc/sắp theo ngày được.
        if (source.Deadline is { } deadline)
        {
            var cell = row.CreateCell(9);
            cell.SetCellValue(deadline.ToDateTime(TimeOnly.MinValue));
            cell.CellStyle = styles.Date;
        }
        else
        {
            Text(row, 9, EmptyCell, styles.Text);
        }

        if (source.ProgressPercent is { } progress)
        {
            var cell = row.CreateCell(10);
            cell.SetCellValue(progress);
            cell.CellStyle = styles.Integer;
        }
        else
        {
            Text(row, 10, EmptyCell, styles.Text);
        }

        Text(row, 11, source.Note ?? EmptyCell, styles.Wrapped);
    }

    /// <summary>
    /// Dòng <c>TỔNG CỘNG</c> (§4.4) — tổng của bốn cột điểm.
    ///
    /// <para><b>Cột <c>Tiến độ %</c> CỐ Ý không có dòng tổng:</b> cộng các giá trị phần trăm lại
    /// cho ra một con số vô nghĩa (<c>Σ%</c> không phải <c>%</c>). "Tiến độ chung" nếu cần thì nằm
    /// ở khối nhận dạng kỳ, không nằm ở đây.</para>
    /// </summary>
    private static void WriteTotalRow(
        ISheet sheet, ExportStyles styles, int rowIndex, IReadOnlyList<DashboardExportRow> rows)
    {
        var row = sheet.CreateRow(rowIndex);

        var label = row.CreateCell(0);
        label.SetCellValue("TỔNG CỘNG");
        label.CellStyle = styles.TotalLabel;

        Total(row, 3, rows.Sum(item => item.MaxScore), styles);
        Total(row, 4, rows.Sum(item => item.SelfScore ?? 0m), styles);
        Total(row, 5, rows.Sum(item => item.VerifiedScore ?? 0m), styles);
        Total(row, 6, rows.Sum(item => item.Diff ?? 0m), styles);
    }

    private static void Text(IRow row, int column, string value, ICellStyle style)
    {
        var cell = row.CreateCell(column);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    /// <summary>
    /// Số ghi ra dạng SỐ, không phải chuỗi (§4.5) — người nhận còn <c>SUM</c> được. Dấu thập phân
    /// do Excel hiển thị theo locale máy người dùng, nên ở đây không định dạng theo văn hoá nào.
    /// </summary>
    private static void Number(IRow row, int column, decimal? value, ExportStyles styles)
    {
        var cell = row.CreateCell(column);

        if (value is { } number)
        {
            cell.SetCellValue((double)number);
            cell.CellStyle = styles.Decimal;
            return;
        }

        cell.SetCellValue(EmptyCell);
        cell.CellStyle = styles.Text;
    }

    private static void Total(IRow row, int column, decimal value, ExportStyles styles)
    {
        var cell = row.CreateCell(column);
        cell.SetCellValue((double)value);
        cell.CellStyle = styles.TotalNumber;
    }

    private static string Day(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// Style dựng MỘT LẦN cho cả workbook. Excel có trần cứng ~64.000 style trong một file, và
    /// <c>CreateCellStyle()</c> trong vòng lặp theo dòng là cách chạm trần đó nhanh nhất — file
    /// phình lên rồi hỏng ở một quy mô mà không ai thử tới lúc viết.
    /// </summary>
    private sealed class ExportStyles
    {
        public ExportStyles(IWorkbook workbook)
        {
            var format = workbook.CreateDataFormat();

            var bold = workbook.CreateFont();
            bold.IsBold = true;

            var titleFont = workbook.CreateFont();
            titleFont.IsBold = true;
            titleFont.FontHeightInPoints = 14;

            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;

            Title = workbook.CreateCellStyle();
            Title.SetFont(titleFont);

            IdentityLabel = workbook.CreateCellStyle();
            IdentityLabel.SetFont(bold);

            Header = workbook.CreateCellStyle();
            Header.SetFont(headerFont);
            Header.Alignment = HorizontalAlignment.Center;
            Header.VerticalAlignment = VerticalAlignment.Center;
            Header.WrapText = true;

            // XSSFCellStyle mới đặt được màu nền RGB tuỳ ý; bảng màu chỉ-mục của HSSF không có
            // #0F5BD7. Đây cũng là một lý do nữa để file xuất chỉ hỗ trợ .xlsx.
            ((XSSFCellStyle)Header).SetFillForegroundColor(new XSSFColor(HeaderFill, null));
            Header.FillPattern = FillPattern.SolidForeground;

            // Dòng dữ liệu canh TRÊN và bật xuống dòng: cột `Chỉ tiêu` và `Minh chứng/Ghi chú` dài,
            // và một ô ghi chú thật có thể trải nhiều dòng (§1.3 a — 29/62 dòng có nội dung).
            Wrapped = workbook.CreateCellStyle();
            Wrapped.VerticalAlignment = VerticalAlignment.Top;
            Wrapped.WrapText = true;

            Text = workbook.CreateCellStyle();
            Text.VerticalAlignment = VerticalAlignment.Top;

            Decimal = workbook.CreateCellStyle();
            Decimal.VerticalAlignment = VerticalAlignment.Top;
            Decimal.DataFormat = format.GetFormat("0.00");

            Integer = workbook.CreateCellStyle();
            Integer.VerticalAlignment = VerticalAlignment.Top;
            Integer.DataFormat = format.GetFormat("0");

            Date = workbook.CreateCellStyle();
            Date.VerticalAlignment = VerticalAlignment.Top;
            Date.DataFormat = format.GetFormat("dd/MM/yyyy");

            TotalLabel = workbook.CreateCellStyle();
            TotalLabel.SetFont(bold);

            TotalNumber = workbook.CreateCellStyle();
            TotalNumber.SetFont(bold);
            TotalNumber.DataFormat = format.GetFormat("0.00");
        }

        public ICellStyle Title { get; }

        public ICellStyle IdentityLabel { get; }

        public ICellStyle Header { get; }

        public ICellStyle Wrapped { get; }

        public ICellStyle Text { get; }

        public ICellStyle Decimal { get; }

        public ICellStyle Integer { get; }

        public ICellStyle Date { get; }

        public ICellStyle TotalLabel { get; }

        public ICellStyle TotalNumber { get; }
    }
}
