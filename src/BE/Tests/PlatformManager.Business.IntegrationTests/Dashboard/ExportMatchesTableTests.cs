using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.IntegrationTests;
using Xunit;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.IntegrationTests.Dashboard;

/// <summary>
/// <b>Kiểm nghiệm thu §4.6 của spec/dashboard-dti/business-rules.md — khai đúng MỘT chỗ, và đây là
/// chỗ đó:</b> số dòng dữ liệu trong file xuất <b>=</b> số phần tử <c>data.table</c> của
/// <c>GET /api/dashboard</c> với ĐÚNG bộ tham số đó.
///
/// <para><b>Vì sao phải là integration test:</b> hai đường đi qua hai phép <c>Select</c> khác nhau
/// trên CÙNG một <c>IQueryable</c> đã lọc, và thứ có thể lệch nằm ở phần EF DỊCH SANG SQL — chính
/// xác cái mà một fake trong bộ nhớ không tái hiện được.</para>
///
/// <para>Đây là thứ <b>DUY NHẤT</b> cưỡng chế hai đường còn lọc giống nhau: hai projection rời nhau
/// nghĩa là một lần sửa bộ lọc ở một đường sẽ KHÔNG làm đường kia đỏ — trừ kiểm này. Thiếu nó thì
/// triệu chứng lúc hỏng là "file tải về không khớp màn hình", và không ai biết bên nào đúng.</para>
///
/// <para><b>Lấy repository từ DI của HOST THẬT</b>, không <c>new</c> trực tiếp: hiện thực là
/// <c>internal</c> của <c>Business.Persistence</c>, và đi qua container còn kiểm luôn rằng đường
/// đăng ký DI của tầng nghiệp vụ được nối vào host — một phép kiểm miễn phí mà bản <c>new</c> trực
/// tiếp sẽ bỏ mất.</para>
///
/// <para>⚠️ <b>Cần Docker.</b> Không có Docker thì fixture NÉM kèm hướng dẫn thay vì skip im lặng —
/// "xanh giả" là rủi ro đã được cân khi chọn hạ tầng test.</para>
/// </summary>
[Collection(BusinessPostgresCollection.Name)]
public class ExportMatchesTableTests(BusinessPostgresFixture fixture)
{
    private static readonly PeriodRange Week33 = PeriodRange.IsoWeek(2026, 33);

    [Theory(DisplayName = "Số dòng file xuất = số phần tử data.table, ở MỌI bộ lọc")]
    // Ca không lọc gì là ca dễ nhất và nó KHÔNG đủ: hai đường chỉ lệch được khi CÓ bộ lọc, nên §4.6
    // đòi chạy "với ít nhất một bộ lọc khác mặc định".
    [InlineData(null, null)]
    [InlineData("4.2", null)]
    [InlineData(null, "Hoàn thành")]
    [InlineData("chỉ tiêu", "Đang thực hiện")]
    public async Task ExportRowCount_EqualsTableRowCount(string? search, string? status)
    {
        await SeedAsync();

        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDashboardRepository>();

        var spec = CriteriaFilterSpec.Create(search, null, status, Week33);

        var table = await repository.GetTableAsync(spec, CancellationToken.None);
        var export = await repository.GetExportRowsAsync(spec, CancellationToken.None);

        Assert.Equal(table.Count, export.Count);

        // Không chỉ ĐẾM bằng nhau mà còn CÙNG TẬP và CÙNG THỨ TỰ: hai đường dùng chung
        // CriteriaQueries.Sorted, nên lệch thứ tự nghĩa là một đường đã tự sắp lại.
        Assert.Equal(table.Select(row => row.Code), export.Select(row => row.Code));
    }

    /// <summary>
    /// Q68 — export mang đủ 12 cột, trong đó BA cột mà <c>table[]</c> của DB-1 cố ý không có. Thiếu
    /// chúng là phá round-trip <i>export → sửa → import</i>.
    /// </summary>
    [Fact(DisplayName = "Projection export mang ĐỦ ba cột mà table[] của DB-1 không có")]
    public async Task ExportRows_CarryOwnerDeadlineAndProgress()
    {
        await SeedAsync(withOwner: true);

        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDashboardRepository>();

        var rows = await repository.GetExportRowsAsync(
            CriteriaFilterSpec.Create(null, null, null, Week33), CancellationToken.None);

        var row = rows.Single(item => item.Code == "1.1");

        Assert.NotNull(row.OwnerName);
        Assert.Equal(new DateOnly(2026, 12, 31), row.Deadline);
        Assert.Equal(65, row.ProgressPercent);

        // Cột `Nhóm` của bảng dữ liệu là TÊN TRẦN (Q55) — không có tiền tố `Code. `, để đường import
        // khớp nhóm được khi người dùng nạp ngược file về.
        Assert.Equal("Hạ tầng và Nền tảng số", row.GroupName);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // PHẢI đặt trước khi host boot — cùng khuôn với PostgresFixture.SeedCoreAsync.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        return new WebApplicationFactory<Program>();
    }

    /// <summary>
    /// Dữ liệu nền riêng cho bộ test này — dựng trong schema THẬT, đi qua ràng buộc THẬT (unique
    /// partial trên <c>(CriteriaId, AssessmentDate)</c>, FK xuyên schema sang
    /// <c>core."AspNetUsers"</c>).
    /// </summary>
    private async Task SeedAsync(bool withOwner = false)
    {
        await using var db = fixture.CreateDbContext();

        // Dọn sạch trước mỗi ca: collection này chạy TUẦN TỰ trên một database dùng chung, nên dữ
        // liệu của ca trước còn lại sẽ làm phép đếm của ca sau vô nghĩa.
        await db.Set<CriteriaAssessment>().ExecuteDeleteAsync();
        await db.Set<CriteriaEntity>().ExecuteDeleteAsync();

        var group = await db.Set<CriteriaGroup>().FirstOrDefaultAsync();
        if (group is null)
        {
            group = CriteriaGroup.Create("1", "Hạ tầng và Nền tảng số", 1);
            db.Set<CriteriaGroup>().Add(group);
            await db.SaveChangesAsync();
        }

        var ownerId = withOwner
            ? await db.Users.Select(user => (Guid?)user.Id).FirstOrDefaultAsync()
            : null;

        foreach (var (code, status) in new[]
                 {
                     ("1.1", AssessmentStatuses.Done),
                     ("4.2", AssessmentStatuses.InProgress),
                     ("4.2.1", AssessmentStatuses.InProgress),

                     // Q59 — "4.20" phải KHÔNG khớp từ khoá "4.2" (khớp theo ĐOẠN, không phải tiền
                     // tố chuỗi). Giữ dòng này để ca lọc ở trên thật sự phân biệt được hai luật.
                     ("4.20", AssessmentStatuses.NotStarted),
                 })
        {
            var criteria = CriteriaEntity.Create(code, $"Chỉ tiêu {code}", group.Id, 20m);
            db.Set<CriteriaEntity>().Add(criteria);

            var assessment = CriteriaAssessment.Create(criteria.Id, Week33.End);
            assessment.SetScores(10m, 9m);
            assessment.SetStatus(status);

            if (code == "1.1")
            {
                assessment.SetProgressPercent(65);
                assessment.SetDeadline(new DateOnly(2026, 12, 31));
                assessment.SetOwner(ownerId);
            }

            db.Set<CriteriaAssessment>().Add(assessment);
        }

        await db.SaveChangesAsync();
    }
}
