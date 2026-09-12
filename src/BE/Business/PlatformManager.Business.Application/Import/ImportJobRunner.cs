using System.Text.Json;
using System.Text.Json.Serialization;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Application.Storage;
using PlatformManager.Core.Application.Users;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// <b>DM-7 — thân của lượt nạp.</b> Đọc file từ kho, áp luật từng dòng (§6.3), ghi dữ liệu theo
/// ĐÚNG luật ghi chung của cả ba đường (§5.3), rồi ghi kết quả vào bản ghi job.
///
/// <para>🔒 <b>Q64 — CẢ LƯỢT NẠP chạy trong MỘT giao dịch.</b> Không dòng nào được lưu cho tới lời
/// gọi <c>SaveChangesAsync</c> DUY NHẤT ở cuối, và lời gọi đó ghi luôn trạng thái
/// <c>Succeeded</c> + <c>ResultJson</c>. Hệ quả: job <c>Failed</c> ⇒ kỳ đích giữ nguyên y như
/// trước khi nạp. Lý do chọn nguyên tử: một file cỡ vài chục dòng thì chi phí giao dịch không đáng
/// kể, còn ghi theo lô sẽ để lại một kỳ ĐÃ CHỐT SỐ bị nạp đè một nửa mà không ai biết.</para>
///
/// <para>⚠️ <b>Dòng lỗi KHÔNG làm job <c>Failed</c></b>, nên không có gì để cuộn ngược vì chúng:
/// job vẫn <c>Succeeded</c> và lỗi từng dòng nằm trong <c>result.errors</c> kèm <c>rowNumber</c>.
/// <c>Failed</c> dành riêng cho lỗi HẠ TẦNG — file hỏng, file biến mất khỏi kho, job crash.</para>
///
/// <para><b>Số truy vấn KHÔNG phụ thuộc số dòng.</b> Bốn phép đọc theo LÔ (nhóm · chỉ tiêu theo mã ·
/// bản ghi của kỳ đích · bản ghi kỳ trước cho copy-forward) rồi toàn bộ phần còn lại chạy trong bộ
/// nhớ. Tra từng dòng một là N+1 đúng nghĩa — với 62 dòng thì thành 200+ round-trip.</para>
/// </summary>
public sealed class ImportJobRunner(
    IImportJobRepository jobs,
    ICriteriaImportRepository data,
    IFileStorage storage,
    IImportFileReaderSelector readerSelector,
    IUserLookupService userLookup,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IImportJobRunner
{
    /// <summary>
    /// Khuôn tuần tự hoá của <c>ImportJobs.ResultJson</c> — CHỖ LƯU TRỮ, không phải chỗ ra dây.
    /// Hình dạng trên dây do serializer của host quyết định (camelCase, bỏ khoá <c>null</c>) sau
    /// khi handler poll dựng lại object; lẫn hai chỗ này là tự trói khuôn lưu trữ vào một quyết
    /// định của tầng HTTP.
    /// </summary>
    private static readonly JsonSerializerOptions ResultJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        var job = await jobs.GetTrackedAsync(jobId, ct);

        // Job đã bị dọn giữa lúc enqueue và lúc worker nhặt được. Không có gì để làm và cũng không
        // có chỗ nào để ghi lỗi — ném ở đây chỉ làm Hangfire thử lại một việc không tồn tại.
        if (job is null)
            return;

        job.MarkRunning();
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            var result = await ExecuteAsync(job, ct);

            job.MarkSucceeded(JsonSerializer.Serialize(result, ResultJsonOptions));

            // LỜI GỌI DUY NHẤT ghi dữ liệu — Q64. Mọi Criteria/CriteriaAssessment đã thay đổi ở
            // trên còn nằm trong change tracker cho tới đây, nên EF gói chúng vào một giao dịch
            // cùng với trạng thái job.
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(jobId, ex, ct);
        }
    }

    /// <summary>
    /// Ghi lỗi hạ tầng vào bản ghi job — sau khi đã VỨT mọi thay đổi chưa lưu.
    ///
    /// <para><b>Thứ tự bắt buộc:</b> <c>DiscardTrackedChanges</c> trước, rồi mới nạp lại job. Không
    /// vứt thì lần <c>SaveChanges</c> ghi trạng thái <c>Failed</c> sẽ kéo theo CẢ đống thay đổi dở
    /// dang của lượt nạp — đúng thứ Q64 cấm, và tệ hơn ca không có Q64 vì nó vừa ghi nửa file vừa
    /// khai báo là đã thất bại.</para>
    ///
    /// <para><b>KHÔNG ném lại.</b> Trạng thái thật của lượt nạp nằm ở bảng <c>ImportJobs</c>, không
    /// ở Hangfire — FE chỉ đọc bảng đó. Ném lại chỉ kích hoạt cơ chế thử lại mặc định và nạp lại
    /// một file hỏng thêm chín lần nữa, mỗi lần lại ghi đè <c>ErrorMessage</c>.</para>
    /// </summary>
    private async Task MarkFailedAsync(Guid jobId, Exception ex, CancellationToken ct)
    {
        unitOfWork.DiscardTrackedChanges();

        var job = await jobs.GetTrackedAsync(jobId, ct);
        if (job is null)
            return;

        // Dev-facing + fallback, đúng vai của `message` trong envelope gốc: FE ghi log hoặc hiện
        // cho quản trị, KHÔNG dùng làm câu cho người dùng cuối. Lỗi hạ tầng không có mã nghiệp vụ
        // để dịch, và đó là chủ đích.
        // Lỗi cả-file có NGUYÊN NHÂN NGHIỆP VỤ mang theo businessCode để FE dịch; lỗi hạ tầng thuần
        // thì không, và đó là chủ đích — không có mã nào dịch "job crash" thành câu hữu ích hơn câu
        // chung. Xem ImportFileRejectedException.
        var rejection = ex as ImportFileRejectedException;

        job.MarkFailed(
            rejection is not null ? rejection.Message : $"{ex.GetType().Name}: {ex.Message}",
            rejection?.BusinessCode);

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<ImportResultDto> ExecuteAsync(ImportJob job, CancellationToken ct)
    {
        var rows = await ReadRowsAsync(job, ct);

        if (rows.Count == 0)
            return new ImportResultDto(0, 0, 0, 0, []);

        var headers = ImportHeaderMap.From(rows[0].Cells.Keys);
        GuardRequiredHeaders(headers);

        // Kỳ đích đã được quy đổi và kiểm ở BƯỚC 1 (lúc nhận request, Q45) — ở đây chỉ đọc lại nó
        // từ mốc Chủ nhật đã lưu. Quy đổi lần thứ hai tại đây là mở lại đúng cửa sổ lệch mà Q45
        // đóng: một file bấm lúc 23:59 Chủ nhật, worker nhặt lúc 00:01 thứ Hai.
        var targetWeek = PeriodRange.IsoWeekOf(job.TargetWeekEnd);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var anchorDate = WritePeriodResolver.AnchorDate(targetWeek, today);

        var context = await LoadContextAsync(rows, headers, targetWeek, ct);
        var ownerCache = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

        var errors = new List<ImportRowErrorDto>();
        var firstRowByCode = new Dictionary<string, int>(StringComparer.Ordinal);
        var successCount = 0;
        var createdCount = 0;
        var failedRows = 0;

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            var before = errors.Count;
            var written = await ApplyRowAsync(row, headers, context, firstRowByCode, ownerCache, anchorDate, errors, ct);

            if (errors.Count > before)
                failedRows++;
            else if (written.IsWritten)
                successCount++;

            if (written.CriteriaCreated)
                createdCount++;
        }

        return new ImportResultDto(rows.Count, successCount, failedRows, createdCount, errors);
    }

    // ───────────────────────────── Đọc file ─────────────────────────────

    /// <summary>
    /// Nạp toàn bộ dòng dữ liệu vào bộ nhớ trước khi ghi gì.
    ///
    /// <para><b>Vì sao đọc hết trước chứ không vừa đọc vừa ghi:</b> luật "mã trùng trong CÙNG file
    /// thì báo ở dòng THỨ HAI" và mọi phép đọc THEO LÔ đều cần biết trọn tập mã trước. Vừa đọc vừa
    /// ghi thì hoặc phải tra từng dòng (N+1), hoặc phải ghi rồi sửa lại.</para>
    ///
    /// <para><b>Trần SỐ DÒNG do CORE áp, không phải hàm này</b> (Q75, thi công 2026-09-11): đọc qua
    /// <c>selection.ReadRowsAsync</c> — đường đọc đã áp trần — nên vượt trần thì Core ném
    /// <c>ImportRowLimitExceededException</c> ngay giữa vòng lặp, và hàm này chỉ ánh xạ nó sang mã
    /// lỗi của nghiệp vụ. Trần số dòng là chính sách CORE (bảng §1 của
    /// doc/huong_dan/wiki-core/be/15-import-export.md), nên con số sống ở <c>Import:MaxRows</c>,
    /// không phải một hằng số bịa ra ở tầng nghiệp vụ.</para>
    ///
    /// <para>Vì sao vẫn cần trần dù đã có trần DUNG LƯỢNG: hai trần chặn hai thứ khác nhau — một
    /// cái chặn <i>byte đọc từ đĩa</i>, một cái chặn <i>đối tượng dựng trong bộ nhớ</i>. Một file
    /// 200 KB toàn dòng ngắn vẫn có thể mang nửa triệu dòng.</para>
    /// </summary>
    private async Task<List<ImportFileRow>> ReadRowsAsync(ImportJob job, CancellationToken ct)
    {
        await using var stream = await storage.OpenReadAsync(job.StoragePath, ct);

        // Chọn lại reader theo MAGIC BYTE, không tin cột Format đã lưu: cột đó là dấu vết, không
        // phải hợp đồng, và nội dung trên kho mới là thứ đang đọc.
        var selection = readerSelector.Select(stream, job.FileName);

        if (!selection.IsAccepted)
            throw new InvalidDataException(
                $"File '{job.FileName}' trên kho không còn đọc được bằng bộ đọc nào " +
                $"(lý do: {selection.Rejection}). Nạp lại file.");

        var rows = new List<ImportFileRow>();

        // ĐỌC QUA `selection.ReadRowsAsync`, KHÔNG gọi thẳng `selection.Reader.ReadAsync`: đó là
        // đường đọc ĐÃ ÁP TRẦN SỐ DÒNG của Core (Q75). Gọi thẳng reader là bỏ qua trần mà vẫn biên
        // dịch sạch — đúng lỗ hổng finding F3 (2026-09-11) chỉ ra ở bản đầu, khi trần chỉ là một
        // `int` đi kèm và việc áp nó là chuyện tự giác của từng bên gọi.
        try
        {
            await foreach (var cells in selection.ReadRowsAsync(stream, job.FileName, ct))
            {
                // Dòng 1 của file là header, và seam cam kết KHÔNG bỏ dòng nào (kể cả dòng rỗng) —
                // nên dòng dữ liệu thứ n là dòng n+1 của file. Mọi câu lỗi cấp dòng trỏ theo con số
                // này, nên một phép cộng sai ở đây làm người dùng mở file ra và sửa nhầm dòng.
                rows.Add(new ImportFileRow(rows.Count + 2, cells));
            }
        }
        catch (ImportRowLimitExceededException ex)
        {
            // Core báo bằng ngoại lệ và CHỈ mang con số; ánh xạ sang mã lỗi của nghiệp vụ là việc
            // của tầng này — đúng y khuôn ImportFileRejection → ImportErrors ở trên. Core không
            // được biết mã lỗi của nghiệp vụ nào.
            throw new ImportFileRejectedException(ImportErrors.FileTooManyRows, ("MaxRows", ex.MaxRows));
        }

        return rows;
    }

    /// <summary>
    /// Thiếu cột <c>Mã</c> hoặc <c>Nhóm</c> ⇒ lỗi của CẢ FILE, không phải 62 lỗi dòng giống hệt
    /// nhau. Đây gần như luôn là ca "tải nhầm file", và một câu nói thẳng điều đó hữu ích hơn một
    /// danh sách 62 dòng.
    /// </summary>
    private static void GuardRequiredHeaders(ImportHeaderMap headers)
    {
        var missing = ImportColumns.RequiredHeaders.Where(column => !headers.Contains(column)).ToList();

        if (missing.Count > 0)
            throw new ImportFileRejectedException(
                ImportErrors.FileMissingColumn, ("Columns", string.Join(", ", missing)));
    }

    // ───────────────────────────── Nạp dữ liệu theo lô ─────────────────────────────

    private async Task<ImportContext> LoadContextAsync(
        List<ImportFileRow> rows, ImportHeaderMap headers, PeriodRange targetWeek, CancellationToken ct)
    {
        var groups = await data.GetGroupsAsync(ct);

        // Khớp tên nhóm: cắt khoảng trắng + KHÔNG phân biệt hoa/thường, CÓ phân biệt dấu — cùng
        // luật so khớp với cột `Phụ trách` (§6.3). Nhóm trùng tên sau khi bỏ hoa/thường thì giữ
        // nhóm đầu; danh mục 6 nhóm của §1.6 không có ca đó, nhưng tên nhóm là dữ liệu chạy.
        var groupIdByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
            groupIdByName.TryAdd(group.Name.Trim(), group.Id);

        // Chỉ lấy mã ĐỌC ĐƯỢC; mã rỗng/sai khuôn không cần tra DB vì dòng của chúng đã hỏng sẵn.
        var codes = rows
            .Select(row => headers.Text(row.Cells, ImportColumns.Code))
            .Where(code => code is not null)
            .Select(code => code!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var existing = await data.GetCriteriaByCodesAsync(codes, ct);
        var criteriaByCode = existing.ToDictionary(criteria => criteria.Code, StringComparer.Ordinal);
        var criteriaIds = existing.Select(criteria => criteria.Id).ToList();

        var inTarget = await data.GetAssessmentsInRangeAsync(criteriaIds, targetWeek.Start, targetWeek.End, ct);
        var prior = await data.GetLatestAssessmentsBeforeAsync(criteriaIds, targetWeek.Start, ct);

        return new ImportContext(
            groupIdByName,
            criteriaByCode,
            inTarget.ToDictionary(assessment => assessment.CriteriaId),
            prior.ToDictionary(assessment => assessment.CriteriaId));
    }

    // ───────────────────────────── Luật của một dòng (§6.3) ─────────────────────────────

    private async Task<RowOutcome> ApplyRowAsync(
        ImportFileRow row,
        ImportHeaderMap headers,
        ImportContext context,
        Dictionary<string, int> firstRowByCode,
        Dictionary<string, Guid?> ownerCache,
        DateOnly anchorDate,
        List<ImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var code = headers.Text(row.Cells, ImportColumns.Code);

        // ── Mã: năm ca hỏng, mỗi ca một mã lỗi, và DỪNG ngay ở ca đầu tiên ─────────────────
        // Dừng thay vì gom tiếp: mọi câu lỗi còn lại đều mang tham số {Code}, mà chính Code đang
        // sai — một danh sách lỗi trỏ vào một mã không hợp lệ không giúp được gì.
        if (code is null)
        {
            errors.Add(Error(row, ImportErrors.RowCodeMissing));
            return RowOutcome.Skipped;
        }

        if (code.Length > CriteriaCode.MaxLength)
        {
            errors.Add(Error(row, ImportErrors.RowCodeTooLong,
                ("Code", code), ("MaxLength", CriteriaCode.MaxLength)));
            return RowOutcome.Skipped;
        }

        if (!CriteriaCode.IsWellFormed(code))
        {
            errors.Add(Error(row, ImportErrors.RowCodeFormatInvalid, ("Code", code)));
            return RowOutcome.Skipped;
        }

        if (CriteriaCode.HasSegmentTooLong(code))
        {
            errors.Add(Error(row, ImportErrors.RowCodeSegmentTooLong,
                ("Code", code), ("MaxSegmentDigits", CriteriaCode.MaxSegmentDigits)));
            return RowOutcome.Skipped;
        }

        // Báo ở dòng THỨ HAI, và trỏ về dòng đầu tiên mang mã đó — không có FirstRowNumber thì
        // người dùng phải tự dò cả file để tìm cái còn lại.
        if (firstRowByCode.TryGetValue(code, out var firstRowNumber))
        {
            errors.Add(Error(row, ImportErrors.RowCodeDuplicatedInFile,
                ("Code", code), ("FirstRowNumber", firstRowNumber)));
            return RowOutcome.Skipped;
        }

        firstRowByCode[code] = row.RowNumber;

        // ── Các cột còn lại: GOM lỗi, không dừng ở cái đầu tiên ────────────────────────────
        // Một dòng sai cả hai cột điểm phải báo HAI phần tử errors[] cùng rowNumber — người dùng
        // mở file ra sửa một lần, không phải nạp lại để phát hiện lỗi kế tiếp.
        var errorsBefore = errors.Count;

        context.CriteriaByCode.TryGetValue(code, out var criteria);
        var isNew = criteria is null;

        var groupId = ResolveGroup(row, headers, code, context, errors);
        var (name, maxScore) = isNew ? ResolveNewCriteriaFields(row, headers, code, errors) : (null, null);

        // Điểm tối đa để so: chỉ tiêu ĐÃ CÓ thì lấy giá trị đang lưu — import KHÔNG đổi MaxScore
        // của chỉ tiêu đã có (§6.3), nên so với con số trong file sẽ so nhầm mốc.
        var effectiveMaxScore = criteria?.MaxScore ?? maxScore;

        var selfScore = ResolveScore(
            row, headers, code, ImportColumns.SelfScore, "SelfScore", effectiveMaxScore,
            ImportErrors.RowSelfScoreInvalid, ImportErrors.RowSelfScoreExceedsMax, errors);

        var verifiedScore = ResolveScore(
            row, headers, code, ImportColumns.VerifiedScore, "VerifiedScore", effectiveMaxScore,
            ImportErrors.RowVerifiedScoreInvalid, ImportErrors.RowVerifiedScoreExceedsMax, errors);

        var status = ResolveStatus(row, headers, code, errors);
        var deadline = ResolveDeadline(row, headers, code, errors);

        // `Phụ trách` không khớp ai ⇒ KHÔNG lỗi, OwnerId để trống (§6.3). Đối xử khác hẳn `Nhóm`
        // có lý do: nhóm là danh mục ĐÓNG do BA quản, còn người phụ trách chưa có tài khoản là
        // chuyện bình thường — chặn cả dòng vì lý do đó là chặn dữ liệu đúng.
        var ownerId = await ResolveOwnerAsync(row, headers, ownerCache, ct);

        var note = headers.Text(row.Cells, ImportColumns.Note);

        // Cột `Chênh lệch` (thứ 7): KHÔNG đọc. Nó là trường TÍNH, và cột đó trong file gốc của BA
        // tính theo chiều CŨ nên ngược dấu ở 27/62 dòng — đối chiếu là từ chối gần nửa file gốc.

        if (errors.Count > errorsBefore)
            return RowOutcome.Skipped;

        var created = false;

        if (criteria is null)
        {
            criteria = CriteriaEntity.Create(code, name!, groupId!.Value, maxScore!.Value);
            data.AddCriteria(criteria);
            context.CriteriaByCode[code] = criteria;
            created = true;
        }

        // Mã đã có ⇒ CHỈ cập nhật đánh giá; Name/MaxScore/GroupId của chỉ tiêu giữ nguyên (§6.3).
        // Import là đường nạp SỐ LIỆU, không phải đường sửa danh mục — đổi tên hàng loạt bằng một
        // file Excel là thứ không ai xem lại được.

        ApplyAssessment(criteria, context, anchorDate, new AssessmentWrite
        {
            SelfScore = Assign(headers, ImportColumns.SelfScore, selfScore),
            VerifiedScore = Assign(headers, ImportColumns.VerifiedScore, verifiedScore),
            Status = Assign(headers, ImportColumns.Status, status),
            OwnerId = Assign(headers, ImportColumns.Owner, ownerId),
            Deadline = Assign(headers, ImportColumns.Deadline, deadline),
            Note = Assign(headers, ImportColumns.Note, note),

            // ProgressPercent: CỐ Ý không gán (Q24). File nạp không có cột đó và đây là trường
            // NHẬP TAY — điền hộ một giá trị khởi tạo là bịa ra số liệu mà không ai ký tên. Nó
            // rơi vào nhánh "không gửi" của luật chung: giữ nguyên khi cập nhật, copy-forward khi
            // tạo bản ghi mới.
        });

        return new RowOutcome(true, created);
    }

    /// <summary>
    /// <b>Cột VẮNG KHỎI FILE ≠ ô TRỐNG trong một cột có mặt.</b> Đây là chỗ dễ làm sai nhất của cả
    /// đường nạp, và nó hỏng im lặng theo hướng MẤT DỮ LIỆU.
    ///
    /// <list type="bullet">
    ///   <item>Cột <b>có mặt</b>, ô trống (hoặc ghi <c>—</c>) ⇒ <c>Set(null)</c>: người dùng CHỦ Ý
    ///   xoá trắng ô đó. Đây cũng là ngữ nghĩa mà vòng <i>export → sửa → import</i> cần — file xuất
    ///   luôn có đủ 12 cột, nên xoá nội dung một ô trong Excel phải xoá được giá trị.</item>
    ///   <item>Cột <b>vắng mặt</b> ⇒ <c>Unset</c>: trường "KHÔNG nằm trong request" theo đúng chữ
    ///   của §5.3 ⇒ copy-forward khi tạo bản ghi mới, giữ nguyên khi cập nhật.</item>
    /// </list>
    ///
    /// <para><b>Ca đã đo được (2026-09-11, chạy thật):</b> nạp một file chỉ có
    /// <c>Mã · Chỉ tiêu · Nhóm · Điểm tối đa · Tự đánh giá · Thẩm định</c> — đúng kiểu file "số
    /// liệu tuần này" mà BA hay gửi — trước khi có hàm này sẽ XOÁ TRẮNG <c>Trạng thái</c>,
    /// <c>Phụ trách</c>, <c>Hạn xử lý</c> và <c>Minh chứng/Ghi chú</c> của kỳ đó. Không lỗi nào
    /// báo, và người dùng chỉ phát hiện khi mở lưới ra xem.</para>
    /// </summary>
    private static Assigned<T> Assign<T>(ImportHeaderMap headers, string column, T value) =>
        headers.Contains(column) ? Assigned<T>.Set(value) : Assigned<T>.Unset;

    /// <summary>
    /// Ghi bản ghi đánh giá của kỳ đích — <b>bước 2 của §5.3, đi qua đúng bộ upsert dùng chung</b>
    /// với dialog (DM-4) và sửa inline (DM-6). Không có luật riêng cho import.
    /// </summary>
    private void ApplyAssessment(
        CriteriaEntity criteria, ImportContext context, DateOnly anchorDate, AssessmentWrite write)
    {
        if (context.AssessmentInTargetWeek.TryGetValue(criteria.Id, out var existing))
        {
            // Kỳ đích ĐÃ CÓ bản ghi ⇒ cập nhật đúng bản ghi mà đường đọc sẽ đọc ra, và
            // AssessmentDate của nó GIỮ NGUYÊN. Nguồn copy-forward là `null`: trường không gửi thì
            // giữ giá trị đang có, không kéo lại số của kỳ trước.
            AssessmentUpsert.Apply(existing, copyForwardSource: null, write);
            return;
        }

        var assessment = CriteriaAssessment.Create(criteria.Id, anchorDate);
        context.PriorAssessment.TryGetValue(criteria.Id, out var prior);

        AssessmentUpsert.Apply(assessment, prior, write);

        data.AddAssessment(assessment);

        // Ghi vào bảng tra để lượt sau trong CÙNG file (nếu có) cập nhật đúng bản ghi này thay vì
        // tạo bản thứ hai — mã trùng trong file đã bị chặn, nhưng bất biến "1 bản ghi / 1 chỉ tiêu
        // / 1 kỳ" không được phụ thuộc vào một phép kiểm ở chỗ khác.
        context.AssessmentInTargetWeek[criteria.Id] = assessment;
    }

    // ───────────────────────────── Từng cột ─────────────────────────────

    private static Guid? ResolveGroup(
        ImportFileRow row, ImportHeaderMap headers, string code, ImportContext context, List<ImportRowErrorDto> errors)
    {
        var groupName = headers.Text(row.Cells, ImportColumns.Group);

        if (groupName is not null && context.GroupIdByName.TryGetValue(groupName, out var groupId))
            return groupId;

        // KHÔNG tự tạo nhóm mới: sai nhóm nghĩa là sai chính tả hoặc thừa khoảng trắng, và một
        // nhóm thứ 7 sinh ra từ lỗi gõ làm mọi phép tổng hợp theo nhóm sai ngay mà không ai thấy.
        errors.Add(Error(row, ImportErrors.RowGroupNotFound,
            ("Code", code), ("GroupName", groupName ?? string.Empty)));

        return null;
    }

    /// <summary>
    /// Hai cột chỉ bắt buộc KHI TẠO MỚI (§6.2). Mã đã có trong hệ thống thì import không đụng tới
    /// <c>Name</c>/<c>MaxScore</c>, nên ô trống là hợp lệ — và đó là ca thường gặp khi người dùng
    /// nạp một file chỉ có cột điểm.
    /// </summary>
    private static (string? Name, decimal? MaxScore) ResolveNewCriteriaFields(
        ImportFileRow row, ImportHeaderMap headers, string code, List<ImportRowErrorDto> errors)
    {
        var name = headers.Text(row.Cells, ImportColumns.Name);
        if (name is null)
            errors.Add(Error(row, ImportErrors.RowNameMissing, ("Code", code)));

        var cell = headers.Cell(row.Cells, ImportColumns.MaxScore);

        if (ImportColumns.TryDecimal(cell, out var maxScore) && maxScore is > 0)
            return (name, maxScore);

        errors.Add(Error(row, ImportErrors.RowMaxScoreInvalid,
            ("Code", code), ("MaxScore", ImportColumns.Text(cell) ?? string.Empty)));

        return (name, null);
    }

    /// <summary>
    /// Một cột điểm: rỗng ⇒ <c>null</c>; không đọc ra số ⇒ lỗi dòng; vượt điểm tối đa ⇒ lỗi dòng.
    ///
    /// <para><b>Hai cột điểm dùng HAI bộ mã rời</b> (tham số <paramref name="invalid"/> /
    /// <paramref name="exceedsMax"/>), không gộp: câu người dùng đọc phải nói đúng ô nào cần sửa.
    /// Một mã chung buộc FE dựng câu mơ hồ kiểu "một cột điểm vượt trần".</para>
    /// </summary>
    private static decimal? ResolveScore(
        ImportFileRow row,
        ImportHeaderMap headers,
        string code,
        string column,
        string paramName,
        decimal? maxScore,
        ErrorDescriptor invalid,
        ErrorDescriptor exceedsMax,
        List<ImportRowErrorDto> errors)
    {
        var cell = headers.Cell(row.Cells, column);

        if (ImportColumns.IsBlank(cell))
            return null;

        if (!ImportColumns.TryDecimal(cell, out var score) || score is null)
        {
            errors.Add(Error(row, invalid, ("Code", code), (paramName, ImportColumns.Text(cell) ?? string.Empty)));
            return null;
        }

        // maxScore là null khi chỉ tiêu mới mà cột `Điểm tối đa` cũng hỏng — ca đó đã có lỗi riêng,
        // thêm một lỗi "vượt trần" so với một cái trần không đọc được chỉ gây nhiễu.
        if (maxScore is { } max && score > max)
        {
            errors.Add(Error(row, exceedsMax,
                ("Code", code), (paramName, score.Value), ("MaxScore", max)));
            return null;
        }

        return score;
    }

    private static string? ResolveStatus(
        ImportFileRow row, ImportHeaderMap headers, string code, List<ImportRowErrorDto> errors)
    {
        var text = headers.Text(row.Cells, ImportColumns.Status);

        if (text is null)
            return null;

        // TryResolve: cắt khoảng trắng, KHÔNG phân biệt hoa/thường, CÓ phân biệt dấu (§4 luật 4).
        if (AssessmentStatuses.TryResolve(text, out var resolved))
            return resolved;

        errors.Add(Error(row, ImportErrors.RowStatusInvalid, ("Code", code), ("Status", text)));
        return null;
    }

    private static DateOnly? ResolveDeadline(
        ImportFileRow row, ImportHeaderMap headers, string code, List<ImportRowErrorDto> errors)
    {
        var cell = headers.Cell(row.Cells, ImportColumns.Deadline);

        if (ImportColumns.IsBlank(cell))
            return null;

        if (ImportColumns.TryDate(cell, out var deadline))
            return deadline;

        errors.Add(Error(row, ImportErrors.RowDeadlineInvalid,
            ("Code", code), ("Deadline", ImportColumns.Text(cell) ?? string.Empty)));

        return null;
    }

    /// <summary>
    /// Tra <c>AppUser.FullName</c> → <c>OwnerId</c>. Không khớp ai, hoặc khớp từ hai người trở lên
    /// ⇒ <c>null</c> và dòng vẫn nạp — <b>không sinh mã lỗi nào</b> (§6.3).
    ///
    /// <para>Có nhớ tạm theo tên trong phạm vi MỘT lượt nạp: 62 dòng của một đơn vị thường chỉ có
    /// vài người phụ trách, nên tra lại từng dòng là 62 round-trip cho vài giá trị. Nhớ tạm chỉ
    /// sống bằng đúng lượt nạp này nên không có rủi ro "quyền đã thu hồi vẫn còn hiệu lực" — đây là
    /// tra TÊN, không phải tra quyền.</para>
    /// </summary>
    private async Task<Guid?> ResolveOwnerAsync(
        ImportFileRow row, ImportHeaderMap headers, Dictionary<string, Guid?> cache, CancellationToken ct)
    {
        var fullName = headers.Text(row.Cells, ImportColumns.Owner);

        if (fullName is null)
            return null;

        if (cache.TryGetValue(fullName, out var cached))
            return cached;

        var ownerId = await userLookup.ResolveByFullNameAsync(fullName, ct);
        cache[fullName] = ownerId;

        return ownerId;
    }

    // ───────────────────────────── Kiểu phụ ─────────────────────────────

    private static ImportRowErrorDto Error(
        ImportFileRow row,
        ErrorDescriptor descriptor,
        params (string Name, object? Value)[] args)
    {
        // messageParams vắng mặt khi mã không có tham số — `IMPORT.ROW_CODE_MISSING` cố ý không
        // mang {Code}, vì chính Code là thứ đang thiếu.
        if (args.Length == 0)
            return new ImportRowErrorDto(row.RowNumber, descriptor.BusinessCode, null);

        var messageParams = new Dictionary<string, string>(args.Length, StringComparer.Ordinal);

        foreach (var (name, value) in args)
        {
            // Đi qua ĐÚNG chính sách của envelope, không tự định dạng: cùng allowlist, cùng cách
            // đọc số/ngày. Một bộ chuyển chuỗi thứ hai sẽ lệch ở đúng chỗ khó thấy nhất — dấu thập
            // phân theo locale máy chạy.
            messageParams[name] = MessageParamPolicy.Stringify(value);
        }

        return new ImportRowErrorDto(row.RowNumber, descriptor.BusinessCode, messageParams);
    }

    /// <summary>Một dòng dữ liệu đã đọc, kèm số dòng TRONG FILE (dòng 1 = header).</summary>
    private sealed record ImportFileRow(
        int RowNumber, IReadOnlyDictionary<string, ImportCellValue> Cells);

    /// <summary>Dữ liệu nền đã nạp theo lô cho cả lượt nạp.</summary>
    private sealed record ImportContext(
        IReadOnlyDictionary<string, Guid> GroupIdByName,
        Dictionary<string, CriteriaEntity> CriteriaByCode,
        Dictionary<Guid, CriteriaAssessment> AssessmentInTargetWeek,
        IReadOnlyDictionary<Guid, CriteriaAssessment> PriorAssessment);

    private readonly record struct RowOutcome(bool IsWritten, bool CriteriaCreated)
    {
        public static RowOutcome Skipped => default;
    }

}
