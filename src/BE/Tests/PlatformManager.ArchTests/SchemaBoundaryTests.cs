using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Luật (CHỐT 2026-09-03, quyết định người dùng):</b> "db core" và "db business" là HAI SCHEMA
/// TRONG MỘT DATABASE — không tách thành hai database, vì còn khoá ngoại và giao dịch chung giữa
/// hai bên. Ranh giới schema Postgres phải khớp ĐÚNG ranh giới Core/Business đã có ở tầng code:
/// <list type="bullet">
///   <item>entity khai trong assembly <c>PlatformManager.Core.*</c> (và các entity Identity do
///   framework cung cấp) PHẢI nằm ở schema <see cref="PlatformManagerDbContext.CoreSchema"/>;</item>
///   <item>entity khai trong assembly nghiệp vụ PHẢI nằm ở schema
///   <see cref="PlatformManagerDbContext.BusinessSchema"/>, và vì mặc định của model là
///   <c>core</c> nên điều đó chỉ đạt được bằng cách khai TƯỜNG MINH tại
///   <c>ToTable("…", BusinessSchema)</c> trong <c>IEntityTypeConfiguration&lt;T&gt;</c>.</item>
/// </list>
///
/// <para><b>Vì sao cần luật này.</b> Trước hôm nay nó chỉ tồn tại trong một CHÚ THÍCH ở
/// <c>PlatformManagerDbContext.OnModelCreating</c> — không có gì cưỡng chế. Cách hỏng thì im
/// lặng hoàn toàn: một entity nghiệp vụ quên tham số schema ở <c>ToTable</c> vẫn biên dịch sạch,
/// vẫn chạy, vẫn sinh migration — chỉ khác là bảng của nó rơi vào schema <c>core</c>. Không lỗi
/// nào, không test nào đỏ, và hậu quả chỉ lộ ra vào đúng lúc đắt nhất: lúc tách CoreBase sang dự
/// án thứ hai, khi không còn cách nào biết bảng nào thuộc bên nào. Migration đã chạy trên dữ liệu
/// thật thì đổi schema là việc của người dùng, không phải một dòng sửa.</para>
///
/// <para><b>Đo trên MODEL EF, không trên database.</b> Model là thứ sinh ra migration, nên bắt sai
/// ở đây là bắt TRƯỚC khi DDL kịp ra đời — trong khi soi database thật thì chỉ thấy sau khi đã
/// chạy migration, và lúc đó đã muộn. Cũng nhờ vậy test không cần Docker và nằm được ở ArchTests:
/// dựng model không mở kết nối nào (xem <see cref="EfModelProbe"/>).</para>
///
/// <para><b>Hiện trạng khi viết (2026-09-03): chưa có assembly nghiệp vụ nào.</b> Khối
/// <c>Business.*</c> chưa dựng lại, nên nhánh "phải ở schema business" KHÔNG chạy trên dữ liệu
/// thật — đúng tình trạng mà một test luôn xanh không phân biệt được với một test chết. Vì vậy
/// <see cref="Detector_Judges_EachSideCorrectly"/> là phần BẮT BUỘC của luật này chứ không phải
/// phần trang trí: nó chạy chính hàm <see cref="SchemaViolations"/> mà test thật chạy, trên các
/// hàng dựng tay phủ đủ 4 phán quyết.</para>
/// </summary>
public class SchemaBoundaryTests
{
    /// <summary>
    /// Một entity đã map, rút gọn về đúng 3 thứ luật này cần. Tách khỏi <c>IEntityType</c> để
    /// <see cref="SchemaViolations"/> là hàm THUẦN — nhờ đó ca đối chứng dựng được đầu vào bằng
    /// tay mà vẫn chạy đúng bộ dò đang canh mã sản phẩm, thay vì kiểm một bản sao của nó.
    /// </summary>
    internal readonly record struct MappedEntity(string TypeFullName, string AssemblyName, string? Schema);

    /// <summary>Bên nào của ranh giới — <see cref="Unknown"/> là fail-closed, xem <see cref="Classify"/>.</summary>
    internal enum Side { Core, Business, Unknown }

    [Fact(DisplayName = "Mọi entity Core nằm schema 'core', mọi entity nghiệp vụ khai tường minh schema 'business'")]
    public void EveryMappedEntity_LivesInTheSchemaOfItsSide()
    {
        using var context = EfModelProbe.CreateModelOnlyContext();

        var mapped = MappedEntitiesOf(context);

        // Chặn "xanh mà không đo gì": model rỗng thì mọi assert bên dưới đúng một cách vô nghĩa.
        Assert.True(mapped.Count > 0,
            "Model EF không có entity type nào ⇒ luật schema này không đo gì. Xem " +
            "SoftDeleteQueryFilterTests.EveryBaseEntityDescendant_IsMappedIntoModel trước: nguyên nhân " +
            "thường là một EfConfigurationAssembly không được nộp vào DI.");

        // Chặn "xanh mà không đo gì" (2): nếu phép phân loại trả Unknown cho mọi thứ thì tập Core
        // rỗng, và một bộ dò không nhận ra bên nào cũng không canh được ranh giới nào.
        Assert.True(mapped.Any(entity => Classify(entity) == Side.Core),
            "Không entity nào được phân loại là Core ⇒ Classify() đang hỏng (đổi tên assembly?), " +
            "chứ không phải model đúng. Sửa bảng phân loại trong file này, ĐỪNG nới luật.");

        var violations = SchemaViolations(mapped);

        Assert.True(violations.Count == 0,
            "Entity nằm sai schema so với bên khai nó (luật ranh giới schema, CHỐT 2026-09-03): " +
            string.Join(" | ", violations) + ". Entity nghiệp vụ phải khai TƯỜNG MINH " +
            $"ToTable(\"…\", PlatformManagerDbContext.{nameof(PlatformManagerDbContext.BusinessSchema)}) " +
            "trong IEntityTypeConfiguration<T> của nó — mặc định của model là schema core " +
            "(PlatformManagerDbContext.OnModelCreating), nên QUÊN tham số schema là rơi thẳng vào core mà " +
            "không có tín hiệu nào.");
    }

    /// <summary>
    /// Đối chứng — chứng minh bộ dò biết nói CÓ với từng kiểu vi phạm và nói KHÔNG với code đúng.
    /// Đây là ca duy nhất chạy nhánh "business" hôm nay (chưa có assembly nghiệp vụ nào), và cũng
    /// là ca duy nhất chạy nhánh fail-closed.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: bộ dò bắt Core lạc sang business, nghiệp vụ quên khai schema, và assembly lạ")]
    public void Detector_Judges_EachSideCorrectly()
    {
        const string coreAssembly = "PlatformManager.Core.Domain";
        const string businessAssembly = "PlatformManager.Business.Domain";

        Assert.Empty(SchemaViolations(
        [
            new("PlatformManager.Core.Domain.Entities.SysMenu", coreAssembly, PlatformManagerDbContext.CoreSchema),
            new("PlatformManager.Business.Domain.Entities.Don", businessAssembly, PlatformManagerDbContext.BusinessSchema),
            // Entity Identity do framework khai — KHÔNG mang tên assembly nào của dự án, nhưng 7
            // bảng của nó là phần không tách rời của Core. Không có nhánh này thì luật đỏ oan ngay
            // từ lượt chạy đầu, và người sau sẽ tắt nó đi.
            new("Microsoft.AspNetCore.Identity.IdentityUserClaim", "Microsoft.Extensions.Identity.Stores",
                PlatformManagerDbContext.CoreSchema),
        ]));

        // Ca 1 — entity nghiệp vụ QUÊN tham số schema ở ToTable: nó nhận mặc định "core". Đây là
        // cách hỏng có thật và rẻ nhất, vì nó không đòi ai gõ sai chữ nào cả.
        var forgot = SchemaViolations(
            [new("PlatformManager.Business.Domain.Entities.Don", businessAssembly, PlatformManagerDbContext.CoreSchema)]);
        Assert.Contains(forgot, message => message.Contains("Don", StringComparison.Ordinal));
        Assert.True(forgot.Count == 1,
            "Bộ dò KHÔNG bắt được entity nghiệp vụ rơi vào schema core ⇒ luật này xanh mà không đo gì, " +
            "và đúng ca hỏng phổ biến nhất (quên tham số thứ hai của ToTable) đi lọt.");

        // Ca 2 — chiều ngược lại: một bảng Core bị đẩy sang business. Hiếm hơn, nhưng nếu lọt thì
        // dự án thứ hai kéo theo một bảng không phải của nó.
        Assert.Single(SchemaViolations(
            [new("PlatformManager.Core.Domain.Entities.SysMenu", coreAssembly, PlatformManagerDbContext.BusinessSchema)]));

        // Ca 3 — schema null (ai đó gỡ HasDefaultSchema): mọi bảng rơi về "public" của Postgres,
        // tức ranh giới biến mất hoàn toàn mà không có lỗi nào.
        Assert.Single(SchemaViolations(
            [new("PlatformManager.Core.Domain.Entities.SysMenu", coreAssembly, null)]));

        // Ca 4 — fail-closed: assembly không thuộc bảng phân loại nào thì PHẢI đỏ, không được im
        // lặng cho qua. Một tầng mới đặt tên khác (dự án thứ hai) phải buộc người ta mở file này
        // ra quyết định nó thuộc bên nào, chứ không được mặc nhiên hợp lệ.
        Assert.Single(SchemaViolations(
            [new("Ben.Thu.Ba.Entities.Gi", "Ben.Thu.Ba", PlatformManagerDbContext.CoreSchema)]));
    }

    // ── Bộ dò ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phán quyết cho từng hàng. Trả về danh sách thông điệp vi phạm (rỗng = sạch). Là hàm THUẦN
    /// và <c>internal</c> có chủ đích — xem lý do ở <see cref="MappedEntity"/>.
    /// </summary>
    internal static List<string> SchemaViolations(IEnumerable<MappedEntity> entities)
    {
        var found = new List<string>();

        foreach (var entity in entities)
        {
            var side = Classify(entity);

            var expected = side switch
            {
                Side.Core => PlatformManagerDbContext.CoreSchema,
                Side.Business => PlatformManagerDbContext.BusinessSchema,
                _ => null,
            };

            if (expected is null)
            {
                found.Add($"{entity.TypeFullName} (assembly {entity.AssemblyName}): không phân loại được " +
                          "thuộc bên Core hay bên nghiệp vụ. Thêm assembly đó vào Classify() trong " +
                          "SchemaBoundaryTests và nói rõ nó thuộc bên nào — im lặng cho qua là bỏ ngỏ đúng " +
                          "ranh giới luật này canh.");
                continue;
            }

            if (!string.Equals(entity.Schema, expected, StringComparison.Ordinal))
            {
                found.Add($"{entity.TypeFullName} (assembly {entity.AssemblyName}, bên {side}): " +
                          $"đang ở schema '{entity.Schema ?? "<null>"}', phải là '{expected}'");
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    /// <summary>
    /// Bên của một entity suy ra từ NƠI KHAI NÓ, không phải từ schema nó đang nằm — nếu lấy cả kỳ
    /// vọng lẫn thực tế từ cùng một chỗ thì test chỉ hỏi model "anh ở đâu" rồi kiểm đúng chỗ đó.
    ///
    /// <para>Thứ tự xét có ý nghĩa: kiểu Identity của framework xét TRƯỚC, vì chúng không mang tên
    /// assembly nào của dự án. Nhánh cuối cố ý trả <see cref="Side.Unknown"/> (fail-closed) thay
    /// vì đoán một bên mặc định.</para>
    /// </summary>
    internal static Side Classify(MappedEntity entity) => entity switch
    {
        // 7 bảng Identity (IdentityUserClaim/IdentityUserRole/…) do gói framework khai, nhưng
        // chúng là phần không tách rời của Core: PlatformManagerDbContext kế thừa IdentityDbContext.
        _ when entity.TypeFullName.StartsWith("Microsoft.AspNetCore.Identity.", StringComparison.Ordinal)
            => Side.Core,

        _ when entity.AssemblyName.StartsWith("PlatformManager.Core.", StringComparison.Ordinal)
            => Side.Core,

        // Tên tầng nghiệp vụ theo kiến trúc v3 (doc/kien-truc-core-module.md). Dự án thứ hai đặt
        // tên khác thì thêm tiền tố của nó vào đây — nhánh Unknown bên dưới sẽ ép làm việc đó.
        _ when entity.AssemblyName.StartsWith("PlatformManager.Business.", StringComparison.Ordinal)
            => Side.Business,

        _ => Side.Unknown,
    };

    /// <summary>Đọc model EF ra dạng <see cref="MappedEntity"/> — gồm CẢ owned type và kiểu con
    /// trong hệ thừa kế: chúng cũng thành bảng/cột trong một schema nào đó.</summary>
    private static List<MappedEntity> MappedEntitiesOf(DbContext context) =>
        [.. context.Model.GetEntityTypes().Select(entityType => new MappedEntity(
            entityType.ClrType.FullName ?? entityType.Name,
            entityType.ClrType.Assembly.GetName().Name ?? "<unknown>",
            entityType.GetSchema()))];
}
