# 4. Kiểm thử là 1 phần của kiến trúc, không phải việc làm sau cùng

> Cùng nguyên tắc Nhóm A/B như [01-core-components.md](01-core-components.md):
> **không phải checklist bắt buộc**, chỉ đầu tư khi hệ thống thật sự chạm
> đúng nỗi đau tương ứng.

**Insight giá trị nhất từ `testing.md` của VNR: biến quy tắc kiến trúc thành TEST CHẠY ĐƯỢC (ArchTest), không chỉ ghi trong tài liệu.** Toàn bộ những nguyên tắc ở [01-core-components.md](01-core-components.md) (không public setter, controller phải có permission, không inject EF Core ở Application...) — VNR không chỉ *viết ra*, mà **viết thành 1 bộ test tự động chặn PR nếu vi phạm** (`dotnet test VNR.ArchTests` chạy **trước** unit test trong CI — kiến trúc sai thì unit test đúng cũng vô nghĩa). Đây là cách duy nhất đảm bảo quy ước không bị quên dần theo thời gian khi có người mới tham gia hoặc code base lớn lên — tài liệu (như wiki này) chỉ có tác dụng lúc đọc, ArchTest có tác dụng **mãi mãi, tự động**.

Với hệ thống mới bắt đầu nhỏ, không cần cả bộ ArchTest phức tạp như VNR ngay — nhưng nên có tối thiểu 2-3 test kiến trúc cốt lõi từ ngày đầu (ví dụ: "không entity nghiệp vụ nào có public setter", "mọi controller có `[Authorize]`/permission attribute"), rồi thêm dần khi phát hiện vi phạm thật.

## 3 tầng kiểm thử (test pyramid) — áp dụng đúng loại test cho đúng mục đích, đừng lẫn lộn

| Tầng | Kiểm tra gì | Công cụ .NET |
|---|---|---|
| ArchTest | Quy tắc kiến trúc (layer, naming, permission) | `NetArchTest`/Roslyn phân tích assembly |
| Unit test | Logic handler/domain — mock repository | xUnit + NSubstitute (không Moq) |
| Integration test | EF migration, FK constraint, unique index thật | **Testcontainers.PostgreSql** — container Postgres thật, không phải InMemory |

## Gotcha đáng nhớ nhất

❌ **Không bao giờ dùng `UseInMemoryDatabase` để test** — InMemory provider của EF Core **bỏ qua hoàn toàn** FK constraint, unique index, transaction thật. Test pass trên InMemory không chứng minh được gì về hành vi thật trên Postgres — chỉ dùng mock (unit test) hoặc Testcontainers (integration test).

## Áp dụng vào PlatformManager (2026-08-19)

### 3 test project, mỗi cái một mục đích

| Project | Tầng | Cần Docker? |
| --- | --- | --- |
| `Tests/PlatformManager.ArchTests` | ArchTest — quy tắc kiến trúc, IL-scan thuần | Không |
| `Tests/PlatformManager.Core.UnitTests` | Unit — luồng quyết định, phụ thuộc thay bằng NSubstitute | Không |
| `Tests/PlatformManager.Core.IntegrationTests` | Integration — Postgres THẬT qua Testcontainers | **Có** |

### Yêu cầu môi trường

`PlatformManager.Core.IntegrationTests` **cần Docker đang chạy** (Docker Desktop
trên Windows/macOS, `docker daemon` trên Linux). Không có Docker thì bộ test này
**fail rõ ràng kèm hướng dẫn**, **cố ý KHÔNG `Skip`** — skip im lặng tạo "xanh
giả", tức là `dotnet test` báo xanh trong khi phần kiểm tra bảo mật quan trọng
nhất chưa hề chạy. Thà đỏ và biết vì sao.

Chỉ muốn chạy phần không cần Docker:

```bash
dotnet test Tests/PlatformManager.ArchTests
dotnet test Tests/PlatformManager.Core.UnitTests
```

**Repo KHÔNG có CI** (`.github/` đã xoá 2026-08-21, có chủ đích) ⇒ integration
test chạy **trên máy dev**, và **Docker Desktop phải đang bật** — Testcontainers
cần nó để dựng Postgres. Không bật thì 18+ test đỏ hàng loạt với cùng một
exception từ `PostgresFixture`; đó là lỗi **hạ tầng**, không phải lỗi code.

`PostgresFixture` cố ý **fail chứ không skip** khi thiếu Docker — skip im lặng
là xanh giả, và bộ test này tồn tại chính để bắt những thứ chỉ lộ ra khi chạy
thật.

### Schema cho integration test lấy từ file `.sql` của repo, KHÔNG từ `EnsureCreated()`

`PostgresFixture` dựng schema bằng `dotnet ef database update` rồi chạy `doc/cau-truc-database.sql` (nguồn cũ `doc/ERD/` đã xoá 2026-08-23) — trước là `0003 → 0004_* → 0005_*` vào
container. Như vậy test kiểm luôn **tính đúng của chính file `.sql` mà người dùng
chạy tay lên DB thật**. Dựng schema từ model EF (`EnsureCreated()`) sẽ test trên
một schema **khác** schema production — mà đợt tối ưu 2026-08-18 (thêm index
`IX_RolePermissions_ResourceKey_RoleId`) cho thấy khác biệt schema đúng là thứ
đáng quan tâm.

⚠️ Thêm file migration mới (`0006_*.sql`...) → **phải** thêm tên vào
`PostgresFixture.MigrationScripts`, nếu không schema test sẽ lệch schema thật —
đúng cái điều thiết kế này muốn tránh.

### Kiểm thử phân quyền — chia đôi có chủ đích

Code phân quyền (`RequirePermissionFilter`, `PermissionChecker`,
`SysMenuRoleRepository`) tách làm 2 nhóm test **không thay thế được cho nhau**:

- **Nhóm A (unit, không DB)** — luồng quyết định: claim rỗng → `Forbid` *và không
  chạm DB*; `SuperAdmin` → break-glass; không có quyền → `Forbid`. Chỉ test được
  sạch nhờ seam `IPermissionChecker` (khẳng định "chưa hề gọi" bằng
  `DidNotReceive`).
- **Nhóm B (integration, Postgres thật)** — ngữ nghĩa truy vấn: `INNER JOIN` loại
  dòng `RolePermission` mồ côi, `NULL IN (...)` khi `AspNetRoles.Name` NULL, thu
  hồi/cấp lại có hiệu lực **ngay** (chứng minh không có cache/TTL nào ở đường
  phân quyền — xem [11-performance-caching.md](11-performance-caching.md) §6.2
  quyết định #5).

**Vì sao không gộp nhóm B vào nhóm A bằng LINQ-to-Objects trên `List<T>`:** cách
đó chỉ kiểm được phép tập hợp trong C#, **không** kiểm được EF Core còn dịch đúng
sang SQL sau khi nâng version — cùng bản chất với lý do cấm InMemory ở trên.

---

## Test data builder — tránh `new Entity(...)` rải rác với magic value

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "Kiểm thử phân quyền" ở trên đã nhắc `RolePermission` nhiều lần nhưng không
> nói dữ liệu test đó **được tạo ra thế nào** — khoảng trống đủ nhỏ để bị bỏ
> qua lúc thiết kế, nhưng đủ lớn để thành nợ thật khi số test tăng lên vài
> trăm.

**Vấn đề thật, không phải lý thuyết:** không có quy ước, mỗi test tự viết
`new RolePermission { RoleId = "...", ResourceKey = "..." }` với giá trị tự
bịa tại chỗ. Hậu quả tích luỹ dần: test A dùng `"role-1"`, test B dùng
`Guid.NewGuid().ToString()` — không ai biết giá trị nào có ý nghĩa, giá trị
nào chỉ là filler; entity thêm 1 field bắt buộc mới thì phải sửa **từng** chỗ
`new RolePermission { ... }` rải rác, dễ sót; và phần dựng dữ liệu (10+ dòng)
lấn át phần assert thật sự (2-3 dòng), khó thấy ngay test đang kiểm điều gì.

**Cách làm: 1 builder cho mỗi entity hay dùng trong test, đặt trong chính
test project (KHÔNG đặt trong `Domain`/`Application` — đây là code test, không
phải code sản phẩm):**

```csharp
// Tests/PlatformManager.Core.IntegrationTests/Builders/RolePermissionBuilder.cs
public sealed class RolePermissionBuilder
{
    private string _roleId = "role-default";
    private string _resourceKey = "criteria.manage";
    private bool _canView = true;

    public RolePermissionBuilder WithRole(string roleId) { _roleId = roleId; return this; }
    public RolePermissionBuilder WithResourceKey(string key) { _resourceKey = key; return this; }
    public RolePermissionBuilder Revoked() { _canView = false; return this; }

    public RolePermission Build() => new()
    {
        RoleId = _roleId,
        ResourceKey = _resourceKey,
        CanView = _canView,
    };
}

// Trong test — chỉ khai giá trị KHÁC mặc định:
var revoked = new RolePermissionBuilder().WithRole(userRoleId).Revoked().Build();
```

Đọc test là đọc thẳng "cái gì khác biệt so với trường hợp bình thường" — đúng
thứ reviewer cần thấy, thay vì phải so từng field trong khối khởi tạo dài.

**Ngưỡng áp dụng:** đừng viết builder cho entity chỉ xuất hiện trong 1-2 test
— chi phí bảo trì builder (thêm field, đổi tên) chỉ đáng khi ≥3 test dùng
chung 1 entity, hoặc entity có ≥4 field bắt buộc.

## Coverage % — chỉ đo, không đặt mục tiêu; coverage cao không chứng minh test chặt

> Bổ sung 2026-08-24, đối chiếu thực hành ngành: file này chưa có dòng nào về
> coverage. Đúng phần lớn thời gian — nhưng đội ngũ tầm trung rất hay tự đặt
> "80% coverage" làm tiêu chí xong việc, đây là sai lầm phổ biến bậc nhất
> trong ngành nên cần nói rõ TRƯỚC khi ai đó đề xuất bật gate coverage.

**Vì sao "coverage cao" không chứng minh được gì:** công cụ coverage
(`dotnet test --collect:"XPlat Code Coverage"`) chỉ đếm **dòng được chạy
qua**, không đếm **assert có đủ chặt để bắt lỗi**. Ví dụ sau đạt 100%
coverage cho nhánh `if` bên trong `HasPermissionAsync`, nhưng không bắt được
gì:

```csharp
[Fact]
public async Task HasPermission_KhongThrow()
{
    var result = await _checker.HasPermissionAsync(userId, "criteria.manage");
    // KHÔNG Assert kết quả true/false — chỉ chứng minh hàm chạy xong không
    // văng exception. Coverage tool vẫn tính nhánh if/else bên trong là "đã
    // chạy qua".
}
```

Test này **xanh vĩnh viễn** dù logic phân quyền bị đảo ngược (cho qua khi lẽ
ra phải chặn) — coverage report vẫn báo 100%, không có gì cảnh báo. Đây chính
là lý do bảng "Nhóm A/B" của [`02-identity-auth.md`](02-identity-auth.md) yêu
cầu test kiểm chứng **hành vi cuối** (401/403 thật) chứ không chỉ "hàm chạy
không lỗi".

**Không đặt ngưỡng coverage % làm tiêu chí PR pass.** Thay vào đó:

- Dùng report để **tìm chỗ chưa test**, không dùng để **chứng minh đã test
  đủ**. Đọc report để hỏi "vùng đỏ này có đáng test không", không đuổi theo
  con số phần trăm.
- Ưu tiên đúng test pyramid ở trên hơn phủ coverage dàn trải — cùng tinh thần
  [fe/06-testing-strategy.md](../fe/06-testing-strategy.md) đã chốt cho FE
  ("Coverage 100% — không bắt buộc, ưu tiên đúng 3 tầng hơn phủ hết mọi
  file"), áp dụng tương tự cho BE.
- Muốn biết assert có đủ chặt hay không thật sự, công cụ đúng là **mutation
  testing** (`dotnet-stryker`) — cố tình đổi 1 dòng logic (`>` thành `>=`,
  `&&` thành `||`) rồi xem có test nào đỏ không. Đắt hơn coverage nhiều (chạy
  lại toàn bộ suite cho mỗi mutant) nên **không chạy trên mọi PR** — chạy
  định kỳ (vd hàng tuần, hoặc trước release) trên riêng phần logic nhạy cảm
  nhất (`PermissionChecker`, luồng `SecurityStampValidator`) là đủ ở quy mô
  5-15 dev.

## Test isolation trên Postgres thật — cô lập dữ liệu giữa các integration test

> Bổ sung 2026-08-24, đối chiếu thực hành ngành: mục "Schema cho integration
> test" ở trên nói kỹ container Postgres dựng từ đâu, nhưng không nói điều gì
> xảy ra khi **nhiều test cùng ghi vào cùng 1 bảng trong cùng 1 container** —
> nguồn flaky phổ biến nhất của integration test dùng DB thật, và dễ bị bỏ
> sót vì với 5-10 test đầu tiên chạy tuần tự, nó không hề lộ ra.

**Cơ chế gây lỗi:** xUnit mặc định chạy các test **class** song song (không
phải test method trong cùng 1 class). Nếu 2 class cùng seed
`AspNetUsers`/`RolePermissions` vào **cùng 1 database**, chúng đụng nhau:
class A xoá hết `RolePermissions` để kiểm "role không có quyền nào" đúng lúc
class B đang assert "role X có 3 quyền" — cả hai đúng logic riêng, chỉ sai vì
chạy chung sân. Kết quả là flaky kinh điển: đỏ ngẫu nhiên, pass lại khi chạy
riêng lẻ, không ai dám tin.

**3 cách cô lập, chọn 1 — không trộn:**

| Cách | Cơ chế | Chi phí | Phù hợp khi |
| --- | --- | --- | --- |
| Transaction rollback per test | Mở transaction ở `IAsyncLifetime.InitializeAsync`, rollback ở `DisposeAsync` — thay đổi trong test biến mất, không cần dọn tay | Thấp nhất | Test không cần dữ liệu đã **commit** thật sự (đa số trường hợp) |
| Reset dữ liệu giữa các test (`Respawn` NuGet) | `Checkpoint.Reset(connection)` ở `DisposeAsync`, xoá theo đúng thứ tự FK | Trung bình | Test cần dữ liệu **đã commit** (kiểm hành vi qua ≥2 `DbContext`/request khác nhau) |
| 1 container riêng cho mỗi test class | Testcontainers dựng container mới mỗi class, không dùng chung fixture | Cao — chậm, tốn RAM/CPU khi nhiều class | Test class cần schema/dữ liệu nền khác hẳn nhau, không đáng chia sẻ |

**Khuyến nghị cho quy mô 5-15 dev: transaction rollback per test làm mặc
định**, dùng chung 1 container (đúng `PostgresFixture` đã mô tả ở trên) — rẻ
nhất, và đa số handler đi qua đúng 1 `SaveChanges` mỗi request nên rollback
sạch không để lại gì. Chỉ rơi xuống Respawn khi có test thật sự cần dữ liệu
đã commit.

**Dấu hiệu đang thiếu cô lập, không phải suy đoán:** chạy lại `dotnet test`
liên tiếp 3-5 lần trước khi tin bộ integration test ổn định — tỷ lệ đỏ không
phải 0% hoặc 100% mà lơ lửng thất thường là triệu chứng thiếu cô lập, không
phải "máy hôm nay chậm".

## Snapshot/golden-file test cho envelope response — bắt shape đổi ngoài ý muốn

> Bổ sung 2026-08-24, đối chiếu thực hành ngành:
> [`07-p6-archtests-gate.md`](trien-khai/07-p6-archtests-gate.md) Nhóm A đã có
> `ApiResultSerializationContractTests` (F1 gate) — nhưng gate đó kiểm
> `ApiResult<T>` serialize **giống nhau giữa 2 serializer** (STJ vs
> Newtonsoft), KHÔNG kiểm **shape đó có đổi so với hôm qua hay không**. Hai
> việc khác nhau: F1 bắt "2 serializer lệch nhau", không bắt "cả 2 cùng lệch
> so với FE đang mong đợi" — đây đúng là chỗ breaking change cho FE chỉ bị bắt
> bằng review tay, dễ lọt.

**Kịch bản thật hay xảy ra:** ai đó thêm field mới vào `ErrorDescriptor`
(`string? Category = null`) — biên dịch qua, `ApiResultSerializationContractTests`
vẫn xanh (2 serializer vẫn khớp nhau), `dotnet test` xanh toàn bộ. Nhưng field
mới **đổi shape JSON** mà FE Angular đang deserialize theo interface cứng —
nếu FE strict-check field lạ, hoặc field trùng tên với thứ FE tự thêm cho mục
đích khác, đây là breaking change không ai chủ đích tạo ra và không có gì đỏ
để báo.

**Cách bắt: 1 test so khớp JSON thật với 1 file `.json` đã chốt trước (golden
file), thất bại khi shape đổi:**

```csharp
[Fact]
public async Task ApiResult_Success_Shape_KhongDoiSoVoiGolden()
{
    var result = ApiResult<CriteriaDto>.Ok(new CriteriaDto { Id = 1, Name = "Test" });
    var actualJson = JsonSerializer.Serialize(result, _stjOptions);

    const string goldenPath = "Snapshots/ApiResult.Success.json";
    if (!File.Exists(goldenPath))
    {
        // Lần đầu: ghi golden file, review diff này như review code — KHÔNG
        // tự ghi đè mỗi lần chạy, chỉ ghi khi chưa tồn tại.
        await File.WriteAllTextAsync(goldenPath, actualJson);
        Assert.Fail($"Golden file mới tạo tại {goldenPath} — review rồi commit, chạy lại test.");
    }

    var expected = JsonNode.Parse(await File.ReadAllTextAsync(goldenPath))!.ToJsonString();
    Assert.Equal(expected, JsonNode.Parse(actualJson)!.ToJsonString());
}
```

- Golden file **commit vào git cùng code** — đổi shape cố ý thì diff của
  golden file xuất hiện ngay trong PR, reviewer thấy được "FE sẽ nhận đổi thế
  nào" mà không cần tự chạy app lên soi Network tab.
- Chỉ cần golden file cho **shape chung** (`ApiResult<T>.Ok`, `.Fail` kèm
  `ErrorDescriptor`, response phân trang) — không cần 1 file cho từng
  endpoint, vì phần biến thiên theo endpoint chỉ là `T`, còn khung envelope
  (`status`/`message`/`errorCode`/`data`) mới là thứ cần khoá cứng.
- Đây là kiểm **thêm**, không thay `ApiResultSerializationContractTests` —
  gate đó vẫn cần để bắt lệch giữa 2 serializer; golden file bắt lệch giữa
  "hôm nay" và "hôm qua".

## Chính sách xử lý test không ổn định (flaky) — có luật rõ, không "chạy lại cho qua"

> Bổ sung 2026-08-24, đối chiếu thực hành ngành: file này (và
> [`07-p6-archtests-gate.md`](trien-khai/07-p6-archtests-gate.md)) chưa nói
> phải làm gì khi 1 test **thỉnh thoảng đỏ** — khác test luôn đỏ vì bug thật
> (dễ xử lý). Đây là nợ kỹ thuật tốn thời gian nhất ở team tầm trung nếu không
> có chính sách rõ: mỗi lần đỏ ngẫu nhiên, người gặp phải tốn 10-20 phút nghi
> ngờ chính thay đổi của mình trước khi nhận ra test vốn đã flaky từ trước.

**Cấm: commit/push với 1 test đang biết là flaky mà không xử lý gì** — không
"chạy lại vài lần cho tới khi xanh rồi merge". Mỗi lần "chạy lại cho qua" là
một lần xác nhận ngầm rằng flaky chấp nhận được, số lượng chỉ tăng theo thời
gian.

**Quy trình khi phát hiện 1 test flaky (đỏ ngẫu nhiên, không do thay đổi vừa
commit):**

1. **Quarantine ngay, kèm ngày + lý do nghi ngờ + nơi theo dõi** — không xoá
   âm thầm, không `Skip` trần:
   ```csharp
   [Fact(Skip = "FLAKY 2026-08-24 - nghi race condition ở PostgresFixture.SeedAsync, xem issue #123")]
   public async Task ThuHoiQuyen_CoHieuLucNgay() { ... }
   ```
   `[Fact(Skip = "flaky")]` không ngày, không lý do là cách chắc chắn nhất để
   nó bị quên vĩnh viễn.
2. **Có hạn xử lý** — 1-2 tuần là hợp lý ở quy mô 5-15 dev. Quá hạn mà chưa
   sửa thì nhắc lại ở buổi sync gần nhất, không để `Skip` nằm mãi trong code.
3. **Sửa gốc, không tăng timeout/thêm retry để né.** Nguyên nhân phổ biến
   nhất của flaky trên Postgres thật: thiếu cô lập dữ liệu (xem mục "Test
   isolation" ở trên) hoặc thiếu `await` khiến test đọc trước khi ghi commit
   xong. Tăng timeout chỉ che triệu chứng — xác suất đỏ không về 0, chỉ giảm
   tần suất quan sát được.
4. **Retry tự động chỉ là biện pháp tạm trong lúc quarantine, không phải giải
   pháp lâu dài.** Nếu retry ở lại vĩnh viễn, nó biến flaky thành "bình
   thường" — test suite mất khả năng báo hiệu khi có bug thật liên quan.

**Không cần công cụ phát hiện flaky tự động ở quy mô 5-15 dev** — chi phí CI
time không tương xứng. Tín hiệu đủ tốt: ai gặp test đỏ mà diff của mình không
liên quan gì tới test đó — đó là lúc mở bước 1.

## Seam activation test — bắt buộc cho mọi cross-cutting seam mới

> Bổ sung 2026-08-24, đúc kết từ 1 mẫu lỗi lặp lại **3 lần** phát hiện qua
> audit cùng ngày (permission filter, job nền Hangfire, menu cache phía FE):
> mỗi lần đều đúng interface, đúng implementation, đúng test cho riêng class
> đó — nhưng không có gì buộc class đó phải thật sự nằm trong luồng request
> chạy thật. Đây không phải lỗi hiếm — nó là hệ quả tất yếu khi 1 tính năng
> cross-cutting bị chia thành nhiều mảnh (interface ở Application, impl ở
> Infrastructure, đăng ký DI ở extension method, gắn vào pipeline ở
> `Program.cs`) mà không có bước nào ép các mảnh đó phải chạm nhau.

### Định nghĩa: "cross-cutting seam" khác "business logic class"

Một class **business logic thuần** (entity, handler, validator) chỉ cần unit
test đúng — nó luôn được gọi trực tiếp bởi 1 chỗ rõ ràng (MediatR pipeline
qua `ISender`), không có bước "đăng ký" trung gian nào có thể bị quên.

Một **seam cross-cutting** (filter, middleware, background job registration,
notification sender, permission checker...) thì khác: nó chỉ có tác dụng
*nếu* được đăng ký đúng ở composition root. Quên bước đăng ký này **không
gây lỗi biên dịch, không gây exception lúc chạy** — seam chỉ đơn giản là
không tồn tại trong luồng request, im lặng.

### Luật: 1 seam activation test cho mỗi seam cross-cutting, riêng biệt với unit test

Unit test của class đó (vd `RequirePermissionFilterTests`) **vẫn cần**,
nhưng **không đủ** — nó chứng minh logic đúng khi được gọi trực tiếp, không
chứng minh seam có thật sự nằm trong pipeline hay không. Bắt buộc thêm
**đúng 1** integration test qua `WebApplicationFactory`, dùng chính
composition root thật (`Program.cs`/`AddXModule()`, không tự đăng ký lại
trong test) — gọi qua HTTP thật, assert **hành vi quan sát được** mà chỉ có
thể đúng nếu wiring đúng.

Mẫu chuẩn đã có sẵn trong `wiki-core` — [`02-identity-auth.md`](02-identity-auth.md)
§"Cách chứng minh nó hoạt động thật" (đăng nhập → gọi endpoint → phải 200 →
khoá tài khoản → gọi lại **cùng cookie** → phải 401). Điểm mấu chốt của mẫu
này, áp dụng cho mọi seam khác: **đo cả trạng thái "trước" lẫn "sau"**, không
chỉ assert 1 kết quả đơn — 1 endpoint luôn trả 401 vì lý do khác (bug không
liên quan) cũng "pass" nếu chỉ kiểm đúng 1 chiều.

### Áp mẫu này vào 3 loại seam cụ thể của hệ thống này

| Seam | Seam activation test nên làm gì |
| --- | --- |
| **Permission** (`RequirePermissionFilter`) | Đăng nhập role không có `RolePermission` cho key X → gọi endpoint `[RequirePermission("X")]` → phải **403**. Seed đúng `RolePermission` cho role đó → gọi lại → phải **200**. Nếu filter chưa đăng ký DI/pipeline, bước đầu đã sai (endpoint trả 200 dù chưa seed) |
| **Background job** (Hangfire/`StartImportCommand`) | Gọi endpoint trigger job thật qua HTTP → poll endpoint trạng thái tới khi **chuyển từ `Pending` sang `Completed`** trong thời gian giới hạn. Chứng minh Hangfire đã thật sự dequeue và chạy — không chỉ chứng minh handler tạo `ImportJob` đúng shape |
| **Cache** (menu, permission...) | Gọi endpoint 2 lần trong cùng phiên → đếm số lệnh SQL qua EF command logging (đúng phương pháp đo đã có ở [`11-performance-caching.md`](11-performance-caching.md) §7) → lần 2 phải **ít hơn** lần 1. Nếu cache chưa được service tiêu thụ đọc, số lệnh SQL sẽ bằng nhau |

### Vị trí trong test pyramid

Đây là 1 loại integration test **có mục đích khác** với các integration test
nghiệp vụ thông thường (vốn để chứng minh business rule đúng trên DB thật,
xem "Test isolation" ở trên) — mục đích duy nhất của seam activation test là
chứng minh **wiring tồn tại**, nên chỉ cần đúng 1 test/seam, không cần phủ
nhiều case (case nghiệp vụ đã có unit test lo). Viết seam activation test
**cùng lúc** với việc thêm seam — không phải việc dọn dẹp làm sau, vì đây
chính xác là cách phát hiện sớm lỗi "quên nối dây" thay vì chờ audit thủ công
tìm ra.
