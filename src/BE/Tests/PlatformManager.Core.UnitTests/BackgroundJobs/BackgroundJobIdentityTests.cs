using NSubstitute;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using Xunit;

namespace PlatformManager.Core.UnitTests.BackgroundJobs;

/// <summary>
/// Q35 — danh tính người dùng đi theo job nền (spec/danh-muc-dti/business-rules.md §5.6).
///
/// <para>Phủ luồng QUYẾT ĐỊNH của hai nửa cơ chế: nửa "chụp" lúc enqueue
/// (<c>BackgroundJobIdentityFilter.CaptureFrom</c>) và nửa "đọc lại" lúc worker chạy
/// (<c>RestoreFrom</c> + <see cref="BackgroundJobCurrentUser"/>). Cả hai nửa được tách khỏi kiểu
/// ngữ cảnh của Hangfire đúng để kiểm được ở đây, không phải dựng storage thật.</para>
///
/// <para><b>Cái test này KHÔNG chứng minh:</b> rằng filter đã thật sự được nối vào Hangfire và
/// worker thật sự dequeue job. Đó là seam activation test, thuộc
/// PlatformManager.Core.IntegrationTests và cần Docker — xem
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Seam activation test".</para>
/// </summary>
public class BackgroundJobIdentityTests
{
    private static readonly Guid AliceId = Guid.Parse("0199a1f0-1111-7000-8000-00000000a11c");

    private static ICurrentUser SignedIn(string? userName, Guid? userId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserName.Returns(userName);
        user.UserId.Returns(userId);
        return user;
    }

    // ── Nửa "chụp" — chạy lúc enqueue, còn trong HTTP request ────────────────────────────────

    [Fact(DisplayName = "Enqueue trong request của người đã đăng nhập → chụp đúng tên đăng nhập")]
    public void CaptureFrom_SignedInUser_KeepsUserNameAndUserId()
    {
        var identity = BackgroundJobIdentityFilter.CaptureFrom(SignedIn("alice", AliceId));

        Assert.NotNull(identity);
        Assert.Equal("alice", identity.UserName);
        Assert.Equal(AliceId, identity.UserId);
    }

    [Fact(DisplayName = "CA ÂM 2: enqueue NGOÀI HTTP request (--seed, recurring job) → không có gì để chụp, không ném lỗi")]
    public void CaptureFrom_NoRequestUser_ReturnsNull()
    {
        // null = HttpContext không tồn tại nên filter không phân giải được ICurrentUser nào.
        Assert.Null(BackgroundJobIdentityFilter.CaptureFrom(null));
    }

    [Fact(DisplayName = "CA ÂM 2: có request nhưng chưa đăng nhập → không chụp gì")]
    public void CaptureFrom_AnonymousUser_ReturnsNull()
    {
        var anonymous = Substitute.For<ICurrentUser>();
        anonymous.IsAuthenticated.Returns(false);
        anonymous.UserName.Returns("khong-duoc-doc-den");

        Assert.Null(BackgroundJobIdentityFilter.CaptureFrom(anonymous));
    }

    [Theory(DisplayName = "Phiên rỗng ruột (không tên, không khoá) không tạo ra danh tính giả")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CaptureFrom_BlankUserNameWithoutUserId_ReturnsNull(string? blank)
        => Assert.Null(BackgroundJobIdentityFilter.CaptureFrom(SignedIn(blank, userId: null)));

    // ── Nửa "đọc lại" — chạy trong worker, không có HttpContext ──────────────────────────────

    [Fact(DisplayName = "Worker đọc lại đúng danh tính đã stash")]
    public void RestoreFrom_StashedValues_RebuildsIdentity()
    {
        var identity = BackgroundJobIdentityFilter.RestoreFrom("alice", AliceId);

        Assert.NotNull(identity);
        Assert.Equal("alice", identity.UserName);
        Assert.Equal(AliceId, identity.UserId);
    }

    [Fact(DisplayName = "CA ÂM 1: job không do người dùng kích hoạt → stash rỗng → không có danh tính")]
    public void RestoreFrom_EmptyStash_ReturnsNull()
        => Assert.Null(BackgroundJobIdentityFilter.RestoreFrom(userName: null, userId: null));

    // ── Bản cài ICurrentUser thứ hai ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Có danh tính stash → seam trả đúng người, IsAuthenticated = true")]
    public void BackgroundJobCurrentUser_WithIdentity_ReportsTheUser()
    {
        var currentUser = new BackgroundJobCurrentUser(
            new BackgroundJobIdentityAccessor { Current = new BackgroundJobIdentity(AliceId, "alice") });

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal("alice", currentUser.UserName);
        Assert.Equal(AliceId, currentUser.UserId);
    }

    [Fact(DisplayName = "Không có danh tính → seam trả \"không ai\" thay vì ném lỗi")]
    public void BackgroundJobCurrentUser_WithoutIdentity_ReportsNobody()
    {
        var currentUser = new BackgroundJobCurrentUser(new BackgroundJobIdentityAccessor());

        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserName);
        Assert.Null(currentUser.UserId);
    }

    [Fact(DisplayName = "Vai trò CỐ Ý không đi theo job — quyền phải đọc lại từ DB, không dùng ảnh chụp")]
    public void BackgroundJobCurrentUser_NeverCarriesRoles()
    {
        var currentUser = new BackgroundJobCurrentUser(
            new BackgroundJobIdentityAccessor { Current = new BackgroundJobIdentity(AliceId, "alice") });

        Assert.Empty(currentUser.Roles);
        Assert.False(currentUser.IsInRole("SuperAdmin"));
    }

    [Fact(DisplayName = "Dọn danh tính sau khi job xong — worker tái dùng luồng, không được gán nhầm người")]
    public void Accessor_ClearedAfterJob_ReportsNobodyAgain()
    {
        var accessor = new BackgroundJobIdentityAccessor();
        var currentUser = new BackgroundJobCurrentUser(accessor);

        accessor.Current = new BackgroundJobIdentity(AliceId, "alice");
        Assert.Equal("alice", currentUser.UserName);

        // Đúng thứ BackgroundJobIdentityFilter.OnPerformed làm.
        accessor.Current = null;

        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserName);
    }

    // ── Vòng khép kín: enqueue → worker ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Chụp rồi đọc lại phải ra CÙNG một người — không rơi rụng qua ranh giới request→job")]
    public void CaptureThenRestore_RoundTrips()
    {
        var captured = BackgroundJobIdentityFilter.CaptureFrom(SignedIn("alice", AliceId));
        Assert.NotNull(captured);

        // Mô phỏng đúng cặp SetJobParameter/GetJobParameter: giá trị đi qua storage rồi quay lại.
        var restored = BackgroundJobIdentityFilter.RestoreFrom(captured.UserName, captured.UserId);

        Assert.Equal(captured, restored);
    }
}
