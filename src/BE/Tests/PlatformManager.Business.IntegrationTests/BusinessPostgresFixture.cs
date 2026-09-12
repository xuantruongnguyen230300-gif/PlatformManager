using PlatformManager.Core.IntegrationTests;

namespace PlatformManager.Business.IntegrationTests;

/// <summary>
/// <see cref="PostgresFixture"/> của Core, nối thêm script schema <c>business</c> của cụm DTI.
///
/// <para>🛑 <b>Đây là chỗ danh sách script nghiệp vụ được phép sống, và là lý do lớp cha có
/// <c>ExtraScripts</c>.</b> Bản đầu của lượt 2026-09-11 khai ba file này thẳng vào
/// <c>PostgresFixture</c>; khi đó gỡ cây <c>src/BE/Business/</c> đi — đúng thao tác của lượt tách
/// CoreBase sang dự án thứ hai — sẽ làm <b>toàn bộ</b> collection integration test của Core chết ở
/// <c>InitializeAsync</c> vì <c>FileNotFoundException</c>. Hỏng cả bộ, không phải "vài test đỏ", và
/// nó phá đúng lời hứa viết tay trong <c>PlatformManager.Business.IntegrationTests.csproj</c>.
/// <c>core-reviewer</c> bắt được (F5) và đây là bản sửa.</para>
///
/// <para><b>Phép thử để không tái phát:</b> xoá thư mục <c>src/BE/Business/</c> thì
/// <c>PlatformManager.Core.IntegrationTests</c> phải vẫn biên dịch và chạy được nguyên vẹn — cả
/// project này lẫn file này biến mất cùng lúc với cây đó, nên không còn ai trỏ tới script nghiệp vụ.</para>
/// </summary>
public sealed class BusinessPostgresFixture : PostgresFixture
{
    /// <summary>
    /// Thứ tự CÓ Ý NGHĨA — mỗi script là một DELTA, chạy sau baseline của Core.
    ///
    /// <para>Thêm file <c>.sql</c> mới của tầng nghiệp vụ thì thêm tên vào ĐÂY, không phải vào lớp
    /// cha. Thiếu một dòng ở đây nghĩa là schema test lệch schema thật — đúng thứ bộ fixture sinh
    /// ra để tránh.</para>
    /// </summary>
    protected override IEnumerable<MigrationScript> ExtraScripts =>
    [
        new(BusinessSqlDirectory, "0002_business_dti_tables.sql"),
        new(BusinessSqlDirectory, "0003_criteria_assessment_date_index.sql"),
        new(BusinessSqlDirectory, "0004_import_jobs_error_code.sql"),
    ];

    private static readonly string[] BusinessSqlDirectory =
        ["src", "BE", "Business", "PlatformManager.Business.Persistence", "Migrations", "sql"];
}
