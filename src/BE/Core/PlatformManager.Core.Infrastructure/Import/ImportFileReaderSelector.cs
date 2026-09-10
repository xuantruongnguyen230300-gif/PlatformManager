using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Import;

namespace PlatformManager.Core.Infrastructure.Import;

/// <inheritdoc cref="IImportFileReaderSelector"/>
public sealed class ImportFileReaderSelector(
    IEnumerable<IImportFileReader> readers,
    IOptions<ImportOptions> options) : IImportFileReaderSelector
{
    public ImportFileReaderSelection Select(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
            throw new ArgumentException(
                "Stream file import phải seek được: phép nhận diện định dạng đọc vài byte đầu rồi tua " +
                "về đầu để reader đọc lại từ byte 0. Với stream một chiều, chép sang FileStream/" +
                "MemoryStream trước khi gọi.", nameof(stream));

        var maxBytes = options.Value.MaxFileSizeBytes;

        // Trần dung lượng kiểm TRƯỚC khi đọc byte nội dung nào — đúng yêu cầu
        // doc/huong_dan/wiki-core/be/15-import-export.md §2.
        var length = stream.Length;
        if (length > maxBytes)
            return new ImportFileReaderSelection(null, ImportFileRejection.FileTooLarge, length, maxBytes);

        stream.Seek(0, SeekOrigin.Begin);

        // File ngắn hơn HeaderLength là chuyện bình thường (file rỗng, file một dòng) — đọc được
        // bao nhiêu thì so bấy nhiêu, mọi CanRead đều phải chịu được header ngắn.
        Span<byte> header = stackalloc byte[ImportFileSignatures.HeaderLength];
        var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        // Tua lại TRƯỚC khi trả về: đây là chỗ dễ quên nhất, và quên thì reader mất đúng dòng
        // header — biểu hiện ra thành "file thiếu cột", không thành lỗi đọc file.
        stream.Seek(0, SeekOrigin.Begin);

        // Vòng lặp tường minh chứ không FirstOrDefault(lambda): ReadOnlySpan<byte> không đi vào
        // được lambda (CS8175). Đây cũng là chỗ luật "không switch theo enum định dạng" được thi
        // hành — thêm định dạng thứ tư chỉ là thêm một đăng ký DI.
        IImportFileReader? reader = null;
        foreach (var candidate in readers)
        {
            if (!candidate.CanRead(header[..read], fileName))
                continue;

            reader = candidate;
            break;
        }

        return reader is null
            ? new ImportFileReaderSelection(null, ImportFileRejection.UnsupportedFormat, length, maxBytes)
            : new ImportFileReaderSelection(reader, ImportFileRejection.None, length, maxBytes);
    }
}
