using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

public interface IImportJobRunner
{
    Task RunAsync(Guid jobId, CancellationToken ct);
}

/// <summary>
/// Chạy trong Hangfire worker — KHÔNG có HttpContext, không inject ICurrentUser/
/// IHttpContextAccessor như handler thường (xem .claude/rules/cqrs-handler.md §"Command chạy
/// lâu → job nền"). Tự tạo scope DI RIÊNG qua IServiceScopeFactory (không dựa vào scope Hangfire
/// tự cấp) để resolve mọi service scoped (DbContext, repository...) trong đúng 1 vòng đời rõ
/// ràng cho cả job, kể cả khi job chạy rất lâu.
///
/// Vòng lặp đọc-từng-dòng bên dưới PORT NGUYÊN VẸN từ CsvImportService.ImportAsync cũ: mỗi dòng
/// SaveChanges riêng, lỗi 1 dòng không dừng cả batch (catch + DiscardTrackedChanges), rowNumber
/// bắt đầu từ 2 (dòng 1 = header). Khác cái cũ: sau khi DiscardTrackedChanges (ChangeTracker.
/// Clear()) giữa vòng lặp, entity ImportJob đã fetch trước đó bị DETACH theo — phải fetch lại
/// job MỚI trước khi ghi Status cuối cùng, không tái dùng biến `job` đã có từ đầu.
/// </summary>
public sealed class ImportJobRunner(IServiceScopeFactory scopeFactory) : IImportJobRunner
{
    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var jobRepo = sp.GetRequiredService<IImportJobRepository>();
        var fileStorage = sp.GetRequiredService<IImportFileStorage>();
        var readers = sp.GetRequiredService<IEnumerable<IImportFileReader>>();
        var rowProcessor = sp.GetRequiredService<IImportRowProcessor>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var job = await jobRepo.GetByIdAsync(jobId, ct);
        if (job is null)
            return; // job bị xoá/không tồn tại — không có gì để cập nhật, không throw (tránh Hangfire retry vô ích)

        job.MarkRunning();
        await uow.SaveChangesAsync(ct);

        ImportResultDto? result = null;
        string? failureMessage = null;

        try
        {
            var reader = readers.FirstOrDefault(r => r.CanRead(job.FileName))
                ?? throw new InvalidOperationException($"Không tìm thấy IImportFileReader phù hợp cho file '{job.FileName}'.");

            await using var stream = await fileStorage.OpenReadAsync(job.StoragePath, ct);

            var errors = new List<ImportRowErrorDto>();
            int totalRows = 0, successCount = 0, criteriaCreated = 0, groupsCreated = 0, ownersCreated = 0;
            var rowNumber = 1; // dòng 1 = header, dòng dữ liệu đầu tiên = dòng 2 — giữ đúng quy ước cũ

            await foreach (var row in reader.ReadAsync(stream, job.FileName, ct))
            {
                ct.ThrowIfCancellationRequested();
                rowNumber++;
                totalRows++;
                var code = row.TryGetValue(ImportColumnNames.Code, out var codeValue) ? codeValue?.Trim() : null;

                try
                {
                    var outcome = await rowProcessor.ProcessRowAsync(row, ct);
                    if (outcome.CriteriaCreated) criteriaCreated++;
                    if (outcome.GroupCreated) groupsCreated++;
                    if (outcome.OwnerCreated) ownersCreated++;

                    await uow.SaveChangesAsync(ct);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(new ImportRowErrorDto(rowNumber, code, ex.Message));
                    uow.DiscardTrackedChanges();
                }
            }

            result = new ImportResultDto
            {
                TotalRows = totalRows,
                SuccessCount = successCount,
                ErrorCount = errors.Count,
                CriteriaCreatedCount = criteriaCreated,
                GroupsCreatedCount = groupsCreated,
                OwnersCreatedCount = ownersCreated,
                Errors = errors,
            };
        }
        catch (Exception ex)
        {
            // Lỗi hạ tầng trước/ngoài vòng lặp từng dòng (không mở được file, reader không tồn
            // tại, workbook hỏng...) — phản ánh qua Status=Failed+ErrorMessage (CONTRACT DM-7),
            // KHÁC lỗi từng dòng (nằm trong result.Errors).
            failureMessage = ex.Message;
        }

        // DiscardTrackedChanges() (ChangeTracker.Clear()) trong vòng lặp có thể đã detach entity
        // `job` fetch ở trên — fetch LẠI để chắc chắn có instance đang được track trước khi ghi
        // Status cuối cùng, không tái dùng biến `job` cũ.
        var freshJob = await jobRepo.GetByIdAsync(jobId, ct);
        if (freshJob is null)
            return;

        if (result is not null)
            freshJob.MarkSucceeded(System.Text.Json.JsonSerializer.Serialize(result));
        else
            freshJob.MarkFailed(failureMessage ?? "Lỗi không xác định.");

        await uow.SaveChangesAsync(ct);
    }
}
