namespace PlatformManager.Core.Application.Storage;

/// <summary>
/// Hai khu file RUNTIME, vòng đời khác hẳn nhau nên KHÔNG dùng chung thư mục
/// (doc/huong_dan/wiki-core/be/14-file-storage.md §1, §6).
/// </summary>
public enum FileStorageArea
{
    /// <summary>File người dùng tải lên. Giữ tới khi bản ghi công việc dùng nó bị dọn — còn cần để
    /// tra khi kết quả bị nghi ngờ, và với công việc lỗi thì giữ LÂU HƠN nữa vì đó đúng là thứ
    /// người ta cần khi đi tìm nguyên nhân.</summary>
    Upload,

    /// <summary>File hệ thống sinh ra cho người dùng tải về. Giữ NGẮN — sinh lại được từ dữ liệu,
    /// giữ lâu là tự tạo một bản sao dữ liệu lệch dần.</summary>
    Export,
}

/// <summary>
/// Lưu/đọc/xoá file RUNTIME trên một kho ngoài cây mã nguồn.
///
/// <para><b>Vì sao seam này tồn tại:</b> <c>IFormFile</c>/<c>Stream</c> KHÔNG sống sót qua ranh giới
/// request → job nền — <c>IBackgroundJobScheduler</c> chỉ nhận được đối số tuần tự hoá được. File
/// phải nằm trên kho TRƯỚC khi enqueue job, và job mở lại bằng khoá lưu trữ.</para>
///
/// <para><b>KHÔNG dùng seam này cho file mẫu import</b> (doc/huong_dan/wiki-core/be/14-file-storage.md
/// §7): file mẫu là TÀI SẢN CỦA SOURCE — không ai upload nó, nó không có vòng đời, và nó phải đổi
/// cùng lúc với code đọc cột. Nó nằm trong git cạnh code đó, phục vụ qua static file. Đặt nó lên
/// kho runtime nghĩa là mỗi lần deploy lên một volume mới thì nút "tải file mẫu" gãy.</para>
///
/// <para><b>Khoá lưu trữ là đường dẫn TƯƠNG ĐỐI</b> (<c>uploads/&lt;phân-hệ&gt;/&lt;id&gt;.csv</c>),
/// không phải đường dẫn tuyệt đối — đây là điểm khác quan trọng nhất so với bản cũ. Lưu đường dẫn
/// tuyệt đối vào DB nghĩa là mọi bản ghi cũ chết ngay lần đầu đổi <c>Storage:RootPath</c>, mà đổi
/// gốc kho chính là việc phải làm khi chuyển sang volume/mount (§4).</para>
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Ghi nội dung xuống kho, trả về KHOÁ LƯU TRỮ để lưu vào DB.
    /// </summary>
    /// <param name="area">Khu file — quyết định thư mục cấp một và chính sách dọn.</param>
    /// <param name="feature">
    /// Tên phân hệ sở hữu file, dùng làm thư mục cấp hai. Chữ thường, số và dấu gạch ngang.
    /// <b>Core không biết và không kiểm ý nghĩa của nó</b> — chỉ kiểm nó là một tên thư mục an
    /// toàn.
    /// </param>
    /// <param name="fileId">
    /// Danh tính file, thành tên file trên đĩa. Đặt tên theo id chứ KHÔNG theo tên file gốc: hai
    /// người upload hai file trùng tên là chuyện thường, và tên do người dùng đặt còn là một đường
    /// đưa ký tự tuỳ ý xuống hệ thống tệp.
    /// </param>
    /// <param name="originalFileName">Chỉ dùng để lấy phần mở rộng. Phần mở rộng lạ bị bỏ, không
    /// làm hỏng lượt lưu — định dạng thật vẫn được nhận theo nội dung lúc đọc.</param>
    Task<string> SaveAsync(
        FileStorageArea area,
        string feature,
        Guid fileId,
        string originalFileName,
        Stream content,
        CancellationToken ct);

    /// <summary>
    /// Mở lại file theo khoá đã lưu. Ném <see cref="FileNotFoundException"/> nếu file không còn —
    /// và đó là ca có thật, không phải ca lý thuyết: §6 của
    /// doc/huong_dan/wiki-core/be/14-file-storage.md chốt rằng khi buộc phải lệch pha thì giữ file
    /// LÂU HƠN dòng DB, nhưng chiều ngược lại vẫn xảy ra được (khôi phục DB về mốc cũ không mang
    /// file quay lại).
    /// </summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);

    /// <summary>Xoá file. Trả <c>false</c> nếu vốn không có — dọn dẹp phải chạy lại được nhiều lần
    /// mà không ném.</summary>
    Task<bool> DeleteAsync(string storageKey, CancellationToken ct);

    /// <summary>File còn trên kho hay không. Dùng để phát hiện "đường dẫn chết" trước khi mở.</summary>
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct);
}
