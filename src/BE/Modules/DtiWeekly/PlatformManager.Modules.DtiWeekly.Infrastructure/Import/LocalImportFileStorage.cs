using Microsoft.AspNetCore.Hosting;
using PlatformManager.Modules.DtiWeekly.Application.Import;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Import;

/// <summary>
/// Lưu file import vào App_Data/imports/{jobId}{extension} dưới ContentRootPath của host
/// (PlatformManager.Api) — KHÔNG hardcode đường dẫn tuyệt đối, dùng IWebHostEnvironment theo
/// đúng yêu cầu thiết kế. Đặt tên theo jobId (không theo tên file gốc) để tránh trùng khi 2 user
/// cùng upload file cùng tên, và để ImportJobRunner tự tính lại được path nếu cần re-derive.
/// </summary>
public sealed class LocalImportFileStorage(IWebHostEnvironment env) : IImportFileStorage
{
    private string ImportsDirectory => Path.Combine(env.ContentRootPath, "App_Data", "imports");

    public async Task<string> SaveAsync(Guid jobId, Stream content, string fileName, CancellationToken ct)
    {
        Directory.CreateDirectory(ImportsDirectory);

        var extension = Path.GetExtension(fileName);
        var path = Path.Combine(ImportsDirectory, $"{jobId}{extension}");

        await using var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, ct);

        return path;
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct)
        => Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
}
