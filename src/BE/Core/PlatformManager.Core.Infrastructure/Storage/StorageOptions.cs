using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PlatformManager.Core.Infrastructure.Storage;

/// <summary>
/// Gốc kho file RUNTIME. Một section, một root, mỗi loại file là một thư mục con
/// (doc/huong_dan/wiki-core/be/14-file-storage.md §3):
///
/// <code>
/// Storage:RootPath
///   ├── uploads/&lt;phân-hệ&gt;/&lt;id&gt;&lt;ext&gt;
///   └── exports/&lt;phân-hệ&gt;/&lt;id&gt;&lt;ext&gt;
/// </code>
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Bỏ trống = <c>{ContentRootPath}/App_Data</c>. Ở <b>Production PHẢI</b> khai, và phải trỏ ra
    /// NGOÀI thư mục app — xem <see cref="StorageOptionsValidator"/>.
    /// </summary>
    public string? RootPath { get; init; }
}

/// <summary>
/// Fail-fast cho <see cref="StorageOptions"/>. Phải là <see cref="IValidateOptions{T}"/> chứ không
/// phải <c>[Required]</c>: luật khác nhau theo môi trường, mà DataAnnotations thì không biết môi
/// trường nào.
///
/// <para><b>Vì sao Production không được có giá trị mặc định</b>
/// (doc/huong_dan/wiki-core/be/14-file-storage.md §3, §4): một đường dẫn mặc định "chạy được" là
/// đường dẫn không ai kiểm lại, và nó luôn trỏ vào TRONG thư mục app — đúng chỗ bị xoá sạch mỗi
/// lần deploy. Ba kịch bản hỏng, không kịch bản nào báo lỗi lúc build và không kịch bản nào tái
/// hiện được trên máy dev:</para>
/// <list type="number">
/// <item><b>Container</b> — thư mục app nằm trong image layer. Redeploy = mất toàn bộ file đã
/// nhận; job đang chờ mở vào một đường dẫn không còn tồn tại.</item>
/// <item><b>Nhiều instance</b> — instance A ghi file, job chạy ở instance B, B không thấy gì.</item>
/// <item><b>Thư mục app chỉ-đọc</b> — cấu hình siết chặt gắn app read-only; lượt upload đầu tiên
/// ném ngay ở <c>Directory.CreateDirectory</c>.</item>
/// </list>
///
/// <para>Cùng khuôn với <c>CorsPolicyOptions</c>: chỉ Production mới bắt buộc, và thông điệp nêu
/// ĐÍCH DANH biến môi trường cần đặt — một lỗi fail-fast không chỉ ra cách sửa chỉ đổi "hỏng âm
/// thầm" thành "hỏng ồn ào", người vận hành vẫn phải đi đọc source.</para>
/// </summary>
public sealed class StorageOptionsValidator(IHostEnvironment environment) : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var rootPath = options.RootPath;

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return environment.IsProduction()
                ? ValidateOptionsResult.Fail(
                    "Thiếu cấu hình Storage__RootPath. Ở Production, gốc kho file PHẢI khai tường minh và " +
                    "trỏ ra NGOÀI thư mục app (volume/mount): mặc định {ContentRootPath}/App_Data nằm trong " +
                    "thư mục bị xoá sạch mỗi lần deploy, và không dùng chung được giữa nhiều instance. " +
                    "Đặt biến môi trường Storage__RootPath=/đường/dẫn/volume.")
                : ValidateOptionsResult.Success;
        }

        // Đường dẫn TƯƠNG ĐỐI phân giải theo thư mục làm việc của TIẾN TRÌNH — thứ khác nhau giữa
        // `dotnet run`, một Windows service và một container. Nghĩa là cùng một dòng cấu hình trỏ
        // vào ba chỗ khác nhau, và không chỗ nào báo gì.
        return Path.IsPathFullyQualified(rootPath)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Storage__RootPath phải là đường dẫn TUYỆT ĐỐI, đang khai '{rootPath}'. Đường dẫn tương " +
                "đối phân giải theo thư mục làm việc của tiến trình — khác nhau giữa `dotnet run`, service " +
                "và container, nên cùng một dòng cấu hình sẽ trỏ vào ba chỗ khác nhau mà không báo gì.");
    }
}
