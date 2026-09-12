using System.Globalization;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Import;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Bộ thay thế cho đường nạp file — <b>KHÔNG dùng NSubstitute cho ba interface có trạng thái</b>
/// (<see cref="IImportJobRepository"/>, <see cref="ICriteriaImportRepository"/>,
/// <see cref="IImportFileReader"/>).
///
/// <para>Lý do: cả ba đều cần nhớ thứ vừa được ghi vào rồi trả lại đúng thứ đó ở lời gọi sau —
/// dựng bằng <c>Returns(...)</c> thì mỗi ca kiểm phải khai lại toàn bộ hành vi, và một ca quên
/// khai sẽ nhận <c>null</c>/rỗng mà vẫn "chạy". Fake viết tay giữ bất biến ở một chỗ.</para>
/// </summary>
internal sealed class FakeImportJobRepository : IImportJobRepository
{
    public ImportJob? Job { get; private set; }

    /// <summary>Job dựng sẵn cho ca chạy runner (bước 1 đã chạy xong ở đời thật).</summary>
    public void Seed(ImportJob job) => Job = job;

    public void Add(ImportJob job) => Job = job;

    public Task<ImportJob?> GetTrackedAsync(Guid jobId, CancellationToken ct) =>
        Task.FromResult(Job?.Id == jobId ? Job : null);

    public Task<ImportJobStatusSnapshot?> GetStatusAsync(Guid jobId, CancellationToken ct) =>
        Task.FromResult(Job?.Id == jobId
            ? new ImportJobStatusSnapshot(Job.Status, Job.ResultJson, Job.ErrorMessage, Job.ErrorCode)
            : null);
}

/// <inheritdoc cref="FakeImportJobRepository"/>
internal sealed class FakeCriteriaImportRepository : ICriteriaImportRepository
{
    public List<CriteriaGroupRef> Groups { get; } = [];

    public List<CriteriaEntity> Criteria { get; } = [];

    public List<CriteriaAssessment> Assessments { get; } = [];

    public List<CriteriaEntity> AddedCriteria { get; } = [];

    public List<CriteriaAssessment> AddedAssessments { get; } = [];

    public Task<IReadOnlyList<CriteriaGroupRef>> GetGroupsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CriteriaGroupRef>>(Groups);

    public Task<IReadOnlyList<CriteriaEntity>> GetCriteriaByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CriteriaEntity>>(
            Criteria.Where(criteria => codes.Contains(criteria.Code, StringComparer.Ordinal)).ToList());

    public Task<IReadOnlyList<CriteriaAssessment>> GetAssessmentsInRangeAsync(
        IReadOnlyCollection<Guid> criteriaIds, DateOnly from, DateOnly to, CancellationToken ct) =>
        Task.FromResult(Latest(Assessments.Where(a =>
            criteriaIds.Contains(a.CriteriaId) && a.AssessmentDate >= from && a.AssessmentDate <= to)));

    public Task<IReadOnlyList<CriteriaAssessment>> GetLatestAssessmentsBeforeAsync(
        IReadOnlyCollection<Guid> criteriaIds, DateOnly before, CancellationToken ct) =>
        Task.FromResult(Latest(Assessments.Where(a =>
            criteriaIds.Contains(a.CriteriaId) && a.AssessmentDate < before)));

    public void AddCriteria(CriteriaEntity criteria)
    {
        Criteria.Add(criteria);
        AddedCriteria.Add(criteria);
    }

    public void AddAssessment(CriteriaAssessment assessment)
    {
        Assessments.Add(assessment);
        AddedAssessments.Add(assessment);
    }

    /// <summary>Đúng luật §5.2: MỘT bản ghi / chỉ tiêu, bản có <c>AssessmentDate</c> lớn nhất.</summary>
    private static IReadOnlyList<CriteriaAssessment> Latest(IEnumerable<CriteriaAssessment> scope) =>
        scope.GroupBy(a => a.CriteriaId)
            .Select(group => group.OrderByDescending(a => a.AssessmentDate).First())
            .ToList();
}

/// <summary>
/// Reader trả về đúng bộ dòng đã dựng sẵn. Thay cho reader thật của Core có chủ đích: bộ test này
/// kiểm LUẬT NGHIỆP VỤ của lượt nạp, còn việc CsvHelper/NPOI đọc đúng byte thì đã có bộ test riêng
/// ở <c>PlatformManager.Core.UnitTests/Import</c>.
/// </summary>
internal sealed class FakeImportFileReader(IReadOnlyList<IReadOnlyDictionary<string, ImportCellValue>> rows)
    : IImportFileReader
{
    public bool CanRead(ReadOnlySpan<byte> header, string fileName) => true;

    public async IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
        Stream stream, string fileName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }
}

/// <summary>Reader ném ngay — ca "file hỏng", để kiểm bất biến Q64 (job Failed ⇒ 0 dòng được ghi).</summary>
internal sealed class ThrowingImportFileReader(string message) : IImportFileReader
{
    public bool CanRead(ReadOnlySpan<byte> header, string fileName) => true;

#pragma warning disable CS1998 // Thân không có await: iterator ném NGAY khi bắt đầu duyệt, đúng ý đồ.
    public async IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
        Stream stream, string fileName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        throw new InvalidDataException(message);
#pragma warning disable CS0162 // Không tới được: bắt buộc phải có để compiler nhận đây là iterator.
        yield break;
#pragma warning restore CS0162
    }
#pragma warning restore CS1998
}

/// <summary>Bộ chọn reader luôn trả về reader đã cho — phép nhận diện magic byte có bộ test riêng ở Core.</summary>
internal sealed class FakeImportFileReaderSelector(IImportFileReader reader, int maxRows = 20_000)
    : IImportFileReaderSelector
{
    public ImportFileReaderSelection Select(Stream stream, string fileName) =>
        new(reader, ImportFileRejection.None, stream.Length, 10L * 1024 * 1024, maxRows);
}

/// <summary>Dựng bộ dòng của một file nạp theo TÊN CỘT — khớp đúng cách reader thật trả về.</summary>
internal sealed class ImportFileBuilder(params string[] headers)
{
    private readonly List<IReadOnlyDictionary<string, ImportCellValue>> _rows = [];

    public IReadOnlyList<IReadOnlyDictionary<string, ImportCellValue>> Rows => _rows;

    /// <summary>
    /// Một dòng, giá trị theo ĐÚNG thứ tự header. <c>string</c> ⇒ ô chữ (đường của CSV);
    /// <c>decimal</c>/<c>double</c> ⇒ ô SỐ THẬT; <c>DateOnly</c>/<c>DateTime</c> ⇒ ô NGÀY THẬT
    /// (đường của Excel); <c>null</c> ⇒ ô trống.
    /// </summary>
    public ImportFileBuilder Row(params object?[] values)
    {
        // StringComparer.Ordinal: đúng comparer mà reader thật dùng, nên bộ test đi qua CHÍNH
        // ImportHeaderMap chứ không được một dictionary khoan dung đỡ hộ.
        var row = new Dictionary<string, ImportCellValue>(StringComparer.Ordinal);

        for (var i = 0; i < headers.Length; i++)
            row[headers[i]] = Cell(i < values.Length ? values[i] : null);

        _rows.Add(row);
        return this;
    }

    private static ImportCellValue Cell(object? value) => value switch
    {
        null => ImportCellValue.Empty,
        string text => ImportCellValue.FromText(text),
        decimal number => ImportCellValue.FromNumber((double)number),
        double number => ImportCellValue.FromNumber(number),
        int number => ImportCellValue.FromNumber(number),
        DateOnly date => ImportCellValue.FromDate(date.ToDateTime(TimeOnly.MinValue)),
        DateTime date => ImportCellValue.FromDate(date),
        _ => ImportCellValue.FromText(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };
}
