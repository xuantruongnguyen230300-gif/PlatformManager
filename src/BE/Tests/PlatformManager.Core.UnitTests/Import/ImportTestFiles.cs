using System.Text;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace PlatformManager.Core.UnitTests.Import;

/// <summary>
/// Dựng file import trong bộ nhớ cho test — <b>không commit file nhị phân nào vào repo</b>. File
/// <c>.xlsx</c>/<c>.xls</c> mẫu là thứ không ai đọc được trong diff và không ai sửa lại được khi
/// cần thêm một ca; sinh bằng chính NPOI thì mỗi ca dùng tự khai ra nó cần gì.
/// </summary>
internal static class ImportTestFiles
{
    /// <summary>
    /// Đường dẫn tương đối (từ gốc repo) của file CSV mẫu — <b>ẩn danh, và CÓ TRONG REPO</b>.
    /// </summary>
    private static readonly string SampleCsvRelativePath =
        Path.Combine("spec", "danh-muc-dti", "dti-mau-an-danh-62-dong.csv");

    /// <summary>
    /// File CSV mẫu cho hai test đọc-file-thật: <b>62 bản ghi · 11 cột · BOM UTF-8 · tiếng Việt có
    /// dấu · 13 ô chứa xuống dòng bên trong dấu nháy</b> (nên file dài hơn 63 dòng vật lý).
    ///
    /// <para><b>Sửa 2026-09-09 — trước đó chỗ này trỏ <c>spec/DTI_CanGiuoc_2026-08-11.csv</c>, một
    /// file KHÔNG có trong repo.</b> Nó bị <c>.gitignore:15</c> loại trừ <b>có chủ đích</b> (dữ
    /// liệu DTI thật của một địa phương: mã, chỉ tiêu, điểm tự đánh giá, điểm thẩm định — đưa vào
    /// git là đưa dữ liệu tổ chức thật vào lịch sử repo, xoá sau không gỡ được). Hệ quả đo được:
    /// trên một clone sạch, hai test dùng nó ném <see cref="DirectoryNotFoundException"/> ⇒ cổng
    /// BE (<c>.claude/CLAUDE.md</c> §8) đỏ trên mọi máy không phải máy đã có file đó, vì một lý do
    /// không liên quan gì tới code. Đây đúng thứ mục §5 của <c>.claude/check-docs.sh</c> sinh ra để
    /// chặn — chỉ khác là mục đó đóng khung bằng <c>--include='*.md'</c> nên không phủ file
    /// <c>.cs</c>.</para>
    ///
    /// <para><b>Bản thay thế là file ẩn danh, dựng theo đúng lối mà chính <c>.gitignore:14</c> chỉ
    /// ra</b> (<i>"Cần mẫu để phát triển thì dựng file ẩn danh"</i>). Nó giữ NGUYÊN mọi tính chất
    /// hai test đang đo — mất một tính chất là mất một phép đo:</para>
    /// <list type="bullet">
    ///   <item><b>62 bản ghi / 11 cột</b>: con số tham chiếu ở <c>spec/danh-muc-dti/business-rules.md</c> §7.</item>
    ///   <item><b>Ô chứa xuống dòng trong dấu nháy</b>: thứ DUY NHẤT phân biệt một bộ đọc CSV thật
    ///   với một phép tách theo ký tự xuống dòng. Bỏ nó đi thì khẳng định "62 dòng" hết ý nghĩa.</item>
    ///   <item><b>BOM UTF-8 + tiếng Việt có dấu</b>: đúng thứ Excel trên Windows xuất ra.</item>
    ///   <item><b>Hai cột <c>Phụ trách</c>/<c>Hạn xử lý</c> rỗng toàn bộ</b>: ca "ô trống" của §7.</item>
    /// </list>
    ///
    /// <para>Thứ KHÔNG giữ lại là điểm số và câu chữ minh chứng của một địa phương cụ thể — chúng
    /// không tham gia phép đo nào ở đây.</para>
    /// </summary>
    public static string RealCsvPath => Path.Combine(RepoRoot.Value, SampleCsvRelativePath);

    /// <summary>
    /// Gốc repo, tìm bằng cách đi ngược từ thư mục chạy test cho tới khi thấy file CSV mẫu.
    ///
    /// <para>Ném (chứ không bỏ qua test) nếu không thấy: một test "tự bỏ qua khi không tìm thấy
    /// fixture" là test xanh vĩnh viễn sau lần đầu ai đó đổi cấu trúc thư mục. Lựa chọn đó GIỮ
    /// NGUYÊN — nó vốn đúng; thứ đã sai là file được trỏ tới, không phải cách phản ứng khi thiếu
    /// nó.</para>
    /// </summary>
    private static readonly Lazy<string> RepoRoot = new(() =>
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SampleCsvRelativePath)))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Không tìm thấy gốc repo (thư mục chứa '{SampleCsvRelativePath}') khi đi ngược từ " +
            $"'{AppContext.BaseDirectory}'. File mẫu này CÓ trong repo — nếu nó thiếu thì đây là một " +
            "clone hỏng hoặc thư mục bin/ đã bị chép ra ngoài cây repo, không phải chuyện thiếu dữ liệu.");
    });

    /// <summary>CSV có BOM UTF-8 — đúng thứ Excel trên Windows xuất ra.</summary>
    public static MemoryStream Csv(string content) =>
        new(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray());

    /// <summary>CSV KHÔNG BOM.</summary>
    public static MemoryStream CsvWithoutBom(string content) => new(Encoding.UTF8.GetBytes(content));

    /// <summary>Workbook <c>.xlsx</c> (OOXML) dựng bằng <paramref name="build"/>.</summary>
    public static MemoryStream Xlsx(Action<ISheet> build) => Workbook(new XSSFWorkbook(), build);

    /// <summary>Workbook <c>.xls</c> (OLE2) dựng bằng <paramref name="build"/>.</summary>
    public static MemoryStream Xls(Action<ISheet> build) => Workbook(new HSSFWorkbook(), build);

    private static MemoryStream Workbook(IWorkbook workbook, Action<ISheet> build)
    {
        using (workbook)
        {
            build(workbook.CreateSheet("Sheet1"));

            // Ghi qua một MemoryStream trung gian: NPOI ĐÓNG stream sau khi Write, nên ghi thẳng
            // vào stream trả về sẽ cho ra một stream đã đóng.
            using var buffer = new MemoryStream();
            workbook.Write(buffer, leaveOpen: true);
            return new MemoryStream(buffer.ToArray());
        }
    }

    /// <summary>Điền một dòng toàn ô chữ.</summary>
    public static void WriteTextRow(this ISheet sheet, int rowIndex, params string[] values)
    {
        var row = sheet.CreateRow(rowIndex);
        for (var column = 0; column < values.Length; column++)
            row.CreateCell(column).SetCellValue(values[column]);
    }
}
