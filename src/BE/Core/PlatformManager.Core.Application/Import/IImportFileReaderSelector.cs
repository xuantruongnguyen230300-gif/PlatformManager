namespace PlatformManager.Core.Application.Import;

/// <summary>Lý do một file bị từ chối TRƯỚC khi đọc nội dung.</summary>
public enum ImportFileRejection
{
    /// <summary>Không bị từ chối — <see cref="ImportFileReaderSelection.Reader"/> có giá trị.</summary>
    None = 0,

    /// <summary>Vượt trần dung lượng (<see cref="ImportFileReaderSelection.MaxFileSizeBytes"/>).</summary>
    FileTooLarge,

    /// <summary>Không reader nào nhận file này — nội dung không khớp định dạng nào đã hỗ trợ.</summary>
    UnsupportedFormat,
}

/// <summary>
/// Kết quả chọn reader. Mang theo cả hai con số dung lượng để bên gọi dựng được câu lỗi tử tế
/// ("file 24 MB, trần 10 MB") mà không phải tự đo lại.
/// </summary>
/// <param name="Reader">Reader đã chọn, hoặc <c>null</c> nếu bị từ chối.</param>
/// <param name="Rejection">Lý do từ chối; <see cref="ImportFileRejection.None"/> khi nhận.</param>
/// <param name="FileSizeBytes">Dung lượng file thật.</param>
/// <param name="MaxFileSizeBytes">Trần đang áp, đọc từ cấu hình.</param>
/// <param name="MaxRows">
/// Trần SỐ DÒNG dữ liệu đang áp (Q75) — <b>Core giữ con số, bên gọi thi hành</b>.
///
/// <para><b>Vì sao trần dòng đi ra đây thay vì được selector tự chặn như trần dung lượng:</b> số
/// dòng chỉ biết được KHI ĐANG ĐỌC, mà việc đọc thì nằm ở vòng lặp của bên gọi. Đưa con số ra là
/// cách duy nhất để Core vẫn sở hữu CHÍNH SÁCH trong khi bên gọi sở hữu MÃ LỖI — cùng ranh giới
/// mà <see cref="ImportFileRejection"/> dựng cho trần dung lượng.</para>
///
/// <para>⚠️ Bên gọi phải TỪ CHỐI cả lượt khi vượt, KHÔNG cắt bớt dòng thừa: một file bị cắt cụt
/// mà báo "thành công" để lại một kỳ thiếu dữ liệu và không ai biết.</para>
/// </param>
public sealed record ImportFileReaderSelection(
    IImportFileReader? Reader,
    ImportFileRejection Rejection,
    long FileSizeBytes,
    long MaxFileSizeBytes,
    int MaxRows)
{
    /// <summary>Có reader để đọc tiếp hay không.</summary>
    public bool IsAccepted => Reader is not null;
}

/// <summary>
/// Chọn reader cho một file: đọc vài byte đầu, hỏi <see cref="IImportFileReader.CanRead"/> từng
/// cái trong <c>IEnumerable&lt;IImportFileReader&gt;</c> đã đăng ký — <b>không</b> có
/// <c>switch</c> nào theo enum định dạng (doc/huong_dan/wiki-core/be/15-import-export.md §2).
///
/// <para><b>Vì sao Core giữ việc này thay vì để mỗi nghiệp vụ tự làm:</b> phép nhận diện bằng
/// magic byte chỉ có tác dụng nếu ai đó lấy được vài byte đầu và tua stream về chỗ cũ. Để mỗi
/// người gọi tự làm nghĩa là mỗi người tự quên một lần — và cái quên phổ biến nhất (không tua lại
/// stream) làm mất đúng dòng header.</para>
///
/// <para><b>Trần dung lượng kiểm ở đây, mã lỗi thì KHÔNG.</b> Trần là chính sách Core (cùng hàng
/// với trần số dòng); còn "trả mã lỗi gì cho client" thuộc catalog của nghiệp vụ gọi — nên seam
/// này trả về <see cref="ImportFileRejection"/> để bên gọi tự ánh xạ sang
/// <c>ErrorDescriptor</c> của mình. Core không được biết mã lỗi của một nghiệp vụ nào.</para>
/// </summary>
public interface IImportFileReaderSelector
{
    /// <summary>
    /// Chọn reader cho <paramref name="stream"/>. Stream PHẢI seek được (file trên đĩa/bộ nhớ);
    /// khi trả về, con trỏ đã được tua về đầu để reader đọc từ byte 0.
    ///
    /// <para>Kiểm trần dung lượng TRƯỚC khi đọc byte nội dung nào — vượt trần thì không chạm tới
    /// file.</para>
    /// </summary>
    /// <exception cref="ArgumentException">Stream không seek được.</exception>
    ImportFileReaderSelection Select(Stream stream, string fileName);
}
