using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Core.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace PlatformManager.Core.UnitTests.BackgroundJobs;

/// <summary>
/// Điều kiện nghiệm thu của Q35 đo ở ĐÚNG chỗ nó được phát biểu: giá trị thật sự nằm trong
/// <c>CreatedBy</c>/<c>UpdatedBy</c> sau khi <see cref="AuditInterceptor"/> chạy — không phải ở
/// một seam trung gian.
///
/// <para>Ghép <b>AuditInterceptor thật</b> (Core.Persistence, KHÔNG sửa dòng nào cho Q35) với
/// <b>bản cài ICurrentUser của job nền</b> (host). Đó chính là cặp chạy trong worker Hangfire.</para>
///
/// <para>Không cần Postgres: interceptor chỉ đọc <c>ChangeTracker</c> và ghi ngược vào entity, nên
/// một <see cref="DbContext"/> chưa từng mở kết nối là đủ. Gọi thẳng <c>SavingChanges</c> thay vì
/// <c>SaveChanges</c> đúng vì lý do đó — <c>SaveChanges</c> sẽ đòi một DB thật.</para>
/// </summary>
public class AuditFieldsInBackgroundJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 8, 30, 0, TimeSpan.Zero);

    /// <summary>Entity mồi — chỉ cần kế thừa BaseEntity, đó là toàn bộ điều kiện interceptor xét.</summary>
    private sealed class AuditProbe : BaseEntity;

    private sealed class AuditProbeDbContext(DbContextOptions<AuditProbeDbContext> options) : DbContext(options)
    {
        public DbSet<AuditProbe> Probes => Set<AuditProbe>();
    }

    private static AuditProbeDbContext NewContext() => new(
        new DbContextOptionsBuilder<AuditProbeDbContext>()
            // Chuỗi kết nối KHÔNG BAO GIỜ được dùng: không lệnh nào chạm tới DB trong test này.
            .UseNpgsql("Host=khong-ton-tai;Database=khong-ket-noi")
            .Options);

    private static AuditInterceptor InterceptorFor(BackgroundJobIdentity? stashed)
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);

        var currentUser = new BackgroundJobCurrentUser(
            new BackgroundJobIdentityAccessor { Current = stashed });

        return new AuditInterceptor(currentUser, clock);
    }

    private static void RunInterceptor(AuditInterceptor interceptor, DbContext context)
        => interceptor.SavingChanges(new DbContextEventData(null!, null!, context), default);

    [Fact(DisplayName = "NGHIỆM THU 1: job do người dùng kích hoạt → UpdatedBy là tên đăng nhập của CHÍNH người đó")]
    public void ModifiedInJobTriggeredByUser_WritesThatUserName()
    {
        using var context = NewContext();
        var probe = new AuditProbe { UpdatedBy = "gia-tri-cu" };
        context.Attach(probe);
        context.Entry(probe).State = EntityState.Modified;

        RunInterceptor(InterceptorFor(new BackgroundJobIdentity(Guid.NewGuid(), "alice")), context);

        Assert.Equal("alice", probe.UpdatedBy);
        Assert.Equal(Now, probe.UpdatedAt);
    }

    [Fact(DisplayName = "NGHIỆM THU 1: bản ghi TẠO MỚI trong job cũng mang tên người kích hoạt")]
    public void AddedInJobTriggeredByUser_WritesThatUserName()
    {
        using var context = NewContext();
        var probe = new AuditProbe();
        context.Add(probe);

        RunInterceptor(InterceptorFor(new BackgroundJobIdentity(Guid.NewGuid(), "alice")), context);

        Assert.Equal("alice", probe.CreatedBy);
        Assert.Equal("alice", probe.UpdatedBy);
    }

    [Fact(DisplayName = "NGHIỆM THU 2 (ca âm): job KHÔNG do người dùng kích hoạt vẫn ghi \"system\", không ném lỗi")]
    public void JobWithoutIdentity_FallsBackToSystem()
    {
        using var context = NewContext();
        var probe = new AuditProbe();
        context.Add(probe);

        // stashed = null: đúng trạng thái sau khi filter đọc job parameter rỗng.
        RunInterceptor(InterceptorFor(stashed: null), context);

        Assert.Equal("system", probe.CreatedBy);
        Assert.Equal("system", probe.UpdatedBy);
    }

    [Fact(DisplayName = "Danh tính chỉ có khoá tài khoản (không tên) vẫn rơi về \"system\", không ghi Guid vào cột tên")]
    public void IdentityWithoutUserName_FallsBackToSystem()
    {
        using var context = NewContext();
        var probe = new AuditProbe();
        context.Add(probe);

        RunInterceptor(InterceptorFor(new BackgroundJobIdentity(Guid.NewGuid(), UserName: null)), context);

        Assert.Equal("system", probe.CreatedBy);
    }
}
