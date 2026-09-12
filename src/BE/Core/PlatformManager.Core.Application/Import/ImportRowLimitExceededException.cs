namespace PlatformManager.Core.Application.Import;

/// <summary>
/// File vượt trần SỐ DÒNG của Core (<c>Import:MaxRows</c>).
///
/// <para><b>Vì sao trần dòng báo bằng NGOẠI LỆ còn trần dung lượng báo bằng giá trị trả về</b>
/// (<see cref="ImportFileRejection"/>): trần dung lượng biết được TRƯỚC khi đọc, nên nó chặn ngay
/// ở <see cref="IImportFileReaderSelector.Select"/> và trả <c>Reader = null</c>. Số dòng thì chỉ
/// biết được ĐANG GIỮA CHỪNG một luồng bất đồng bộ — ở đó không có chỗ nào để trả một giá trị "đã
/// từ chối" mà bên gọi buộc phải đọc.</para>
///
/// <para><b>Điểm chung quan trọng hơn điểm khác:</b> cả hai đều <b>ép bên gọi phải xử lý</b>. Bỏ
/// qua <see cref="ImportFileRejection"/> thì <c>Reader</c> là <c>null</c> và nổ ngay; bỏ qua ngoại
/// lệ này thì nó bay lên và làm hỏng cả lượt nạp — ồn ào. Đó chính là thứ một trần chỉ-là-con-số
/// KHÔNG có: quên đọc nó thì biên dịch sạch, test xanh, và trần biến mất im lặng (finding F3,
/// 2026-09-11).</para>
///
/// <para><b>Core KHÔNG biết mã lỗi của nghiệp vụ nào.</b> Ngoại lệ này chỉ mang con số; bên gọi bắt
/// nó rồi ánh xạ sang <c>ErrorDescriptor</c> của mình — đúng y khuôn
/// <see cref="ImportFileRejection"/> → catalog của nghiệp vụ.</para>
///
/// <para>⚠️ <b>Core KHÔNG tự cắt dòng thừa.</b> Nạp một file bị cắt cụt mà báo "thành công" để lại
/// một kỳ thiếu dữ liệu và không ai biết; từ chối thì người dùng còn biết mà tách file.</para>
/// </summary>
/// <param name="maxRows">Trần đang áp — bên gọi dùng nó để dựng câu lỗi.</param>
public sealed class ImportRowLimitExceededException(int maxRows)
    : Exception($"File vượt trần {maxRows} dòng dữ liệu.")
{
    /// <summary>Trần đang áp, đọc từ <c>Import:MaxRows</c>.</summary>
    public int MaxRows { get; } = maxRows;
}
