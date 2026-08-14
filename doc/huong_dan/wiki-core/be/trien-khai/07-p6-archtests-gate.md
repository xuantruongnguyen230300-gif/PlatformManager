# P6 — ArchTests: 36 gate thật, cái nào cần từ ngày đầu, cái nào để sau

> **Định nghĩa hoàn thành của P6 không phải "viết đủ 36 test"** — P6 chạy song
> song với P5 trở đi ([00-lộ-trình §2](00-lo-trinh-tong-the.md)), và phần lớn
> gate ở đây chỉ **có ý nghĩa** khi hệ thống đã chạm đúng tình huống mà nó canh
> gác (module thứ 2, entity có Value Object, i18n 2 tầng...). Viết 1 gate trước
> khi có tình huống để nó canh là viết code không ai kiểm chứng được — đúng
> nguyên tắc "khai theo nhu cầu" đã lặp lại xuyên suốt cả bộ tài liệu này.
> Hoàn thành P6 nghĩa là: **mỗi khi hệ thống chạm 1 cột mốc**, gate tương ứng
> **đã có mặt** — không phải viết trước, không phải quên viết sau.

---

## 1. Vì sao 34 file thật KHÔNG dùng `NetArchTest.Rules`

Khảo sát `Tests/VNR.ArchTests/*.cs` (34 file, không tính `obj/`) và
`VNR.ArchTests.csproj`: **không một `PackageReference` nào tới
`NetArchTest.Rules`**. Toàn bộ 34 file dùng công thức giống hệt nhau — xUnit +
`FluentAssertions` + **reflection trần trên DLL đã build**:

```csharp
private static List<Assembly> LoadAllBinAssemblies()
{
    var assemblies = new List<Assembly>();
    var dlls = Directory.GetFiles(SolutionDirectory, "VNR.Module.*.dll", SearchOption.AllDirectories)
        .Where(f => f.Contains("\\bin\\") && !f.Contains("\\ref\\"))
        .Distinct();
    foreach (var dll in dlls)
    {
        try { assemblies.Add(Assembly.LoadFrom(dll)); } catch { /* skip unloadable */ }
    }
    return assemblies;
}

private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
{
    try { return assembly.GetTypes(); }
    catch (ReflectionTypeLoadException ex) { return (ex.Types ?? []).Where(t => t is not null)!; }
}

private static string GetSolutionDirectory()
{
    var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
    while (dir is not null && !dir.GetFiles("*.sln").Any()) dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Could not find solution directory");
}
```

**3 lý do thật, đọc từ chính comment trong code:**

1. **"Self-contained — không build `ServiceProvider`, không cần `IConfiguration`/DB."**
   (`DiRegistrationCompletenessTests`, `ModuleInstallerArchTests`). Gate chạy
   **cực nhanh** (thuần IL-scan) và chạy được trên máy CI không có DB, không
   secret — so với test tích hợp thật (dựng `WebApplicationFactory`, cần
   connection string) thì rẻ hơn 1-2 bậc độ lớn.
2. **Quét DLL `bin/`, không quét source** — bắt được cả những gì compiler sinh
   ra mà source không thấy trực tiếp (attribute forward, base class implement
   interface gián tiếp), và đặc biệt: **assembly chỉ xuất hiện trong `bin/` khi
   nó thật sự được build** — một module viết dở, chưa từng `dotnet build` thành
   công, tự động **không** bị gate soi (`if (assemblies.Count == 0) return;` —
   xuất hiện lặp lại ở hầu hết test, đọc là "solution/module chưa build — skip",
   không phải "coi như pass").
3. **`ProjectReference` của chính `VNR.ArchTests.csproj` quyết định phạm vi
   quét** — đây là cơ chế tinh tế nhất, đáng nhớ nhất của cả file này. Đọc
   nguyên văn comment trong `.csproj` thật:

   ```xml
   <!-- KHÔNG reference UserAccess.Application ở đây: nó kéo UserAccess.Domain.dll vào bin →
        LoadAllBinAssemblies scan → lộ tech-debt Iam_* (ngoài scope). F3 self-contained (hardcode contract). -->
   ```

   `LoadAllBinAssemblies()` quét **toàn bộ thư mục output**, nhưng thư mục
   output chỉ chứa DLL của những project mà `VNR.ArchTests` (trực tiếp hoặc
   gián tiếp) reference. Nghĩa là: **muốn 1 module tạm thời không bị một gate
   soi** (vì nó có nợ kỹ thuật đã biết, đang chờ dọn), cách làm đúng là **không
   thêm `ProjectReference`** tới module đó trong `VNR.ArchTests.csproj` —
   không phải `[Fact(Skip = "...")]` rải rác từng test. Đây là cơ chế loại trừ
   **tường minh, ở 1 chỗ, có comment giải thích** — khác hẳn `[Skip]` (im lặng,
   dễ quên gỡ) hoặc sửa test cho lỏng đi (làm yếu gate cho mọi module, không
   chỉ module có nợ).

**[ĐƠN GIẢN HOÁ]/lựa chọn cho hệ thống mới:** `NetArchTest.Rules` vẫn là thư
viện hợp lệ và có API gọn hơn cho luật phụ thuộc đơn giản (`Types.InAssembly(...)
.Should().NotHaveDependencyOn(...)`, minh hoạ ở [01-p0 §7](01-p0-nen-mong-solution.md)).
Nhưng khi luật phức tạp hơn "A không được phụ thuộc B" — cần đọc attribute,
convention đặt tên, base class, nội dung i18n JSON — thì reflection trần linh
hoạt hơn nhiều và là lựa chọn Successor thật sự dùng cho **mọi** gate ngoài
layer-dependency đơn giản nhất. Chọn cách nào không quan trọng bằng việc **giữ
nhất quán 1 cách trong toàn bộ project test** — 34 file thật dùng đúng 1 công
thức, không trộn 2 thư viện.

---

## 2. Bảng 34 gate thật — khi nào cần, không phải thứ tự trong repo

Cột "Khi nào cần" dùng đúng ngôn ngữ đã thiết lập ở các file trước: **P{n}**
= cần ngay khi phase đó xong; **module 2+** = chỉ có ý nghĩa khi có ≥2 module
(luật biên giới cross-module); **⏳ [tuỳ nhu cầu]** = chỉ viết khi tính năng
tương ứng thật sự được xây (đừng viết trước).

### Nhóm A — Purity & Seam (viết sớm nhất, không phụ thuộc module thứ 2)

| Test class | ID | Canh gác điều gì | Khi nào cần |
| --- | --- | --- | --- |
| `LayerDependencyTests` | — | `Platform.Domain` không reference EF/AspNetCore/MediatR | **P0** (đã viết ở [01-p0 §7](01-p0-nen-mong-solution.md)) |
| `DomainPurityTests` | T_DOMAIN_01–03 | Entity nghiệp vụ (`Hre_*`, `Org_*`...) không có public setter; Domain không có attribute EF/DataAnnotation; Domain không reference EF Core | **P1**, ngay khi có ≥1 entity nghiệp vụ + ≥1 catalog entity để test phân biệt được 2 nhóm prefix |
| `ConfigAccessArchTests` | T_CONFIG_01 | Application/Domain không inject `IConfiguration`/`IConfigurationService` trực tiếp — phải qua `IOptions<T>` | **P2**, ngay khi chốt convention "config qua `IOptions<T>`" |
| `ApiResultSerializationContractTests` | F1 gate | `ApiResult<T>` serialize **cùng shape** qua cả STJ (middleware) lẫn Newtonsoft (MVC) | **P2/P4** — bảo vệ đúng "dual-serializer discipline" đã nêu ở [03-p2 §4.7](03-p2-platform-application.md); viết ngay sau khi `ApiResult<T>` có cả 2 bộ attribute |
| `EventSeamArchTests` | T_EVENT_01 | Business layer không `using MediatR;` trực tiếp — chỉ qua `IEventPublisher`/`IIntegrationEventHandler<T>` (seam **integration event**, khác CQRS pipeline nội bộ) | **module 2+**, chỉ khi có cơ chế tích hợp sự kiện cross-module — đừng nhầm với `IRequestHandler` nội bộ mà `CommandHandler<,>` (P2) đã bọc sẵn |

### Nhóm B — Naming & Security (viết cùng lúc P4–P5, khi có controller/handler đầu tiên)

| Test class | ID | Canh gác điều gì | Khi nào cần |
| --- | --- | --- | --- |
| `NamingConventionTests` | T_NAMING_01–03 | `*Handler` phải kế thừa `CommandHandler<,>`/`QueryHandler<,>` (không tự `IRequestHandler<,>` trần — bypass pipeline); `*Validator` phải kế thừa `AbstractValidator<T>`; `*Command` không được abstract | **P5**, ngay khi viết handler pattern 2 đầu tiên ([06-p5 §3](06-p5-module-dau-tien.md)) |
| `PermissionEnforcementTests` | T_PERM_01–03 | Controller module mới phải có `[RequirePermission]` **ở class**; cấm `[CheckAccess]` (IAM cũ); cấm `[AllowAnonymous]` ở bất kỳ đâu | **P4/P5**, ngay khi có controller thật đầu tiên |
| `MagicStringArchTests` | T_STRING_01 | `[RequirePermission("...")]` phải dùng hằng số `{Module}ResourceKeyNames`, không string literal | **P4/P5**, cùng lúc với `PermissionEnforcementTests` |
| `MassAssignmentArchTests` | T_MASSASSIGN_01 | Request DTO (`*Request`) không được có `Id` (trên command — phải tới từ route), `IsAdmin`/`IsSuperAdmin`, hay field audit server-tự-set (`UserCreate`, `DateUpdate`, `IsDelete`...) | **P5**, ngay khi viết Request DTO đầu tiên cho pattern 2 |
| `DiRegistrationCompletenessTests` | T_DI_01 | Mọi `I*Service`/`I*Repository`/`I*Manager` được implement + được inject ở đâu đó phải có đăng ký DI thật (không sống nhờ scanner ngầm đã gỡ) | **P5**, ngay khi module đầu tiên tự đăng ký DI tường minh (`Add{Module}Infrastructure`) |
| `SmartEnumHandlerUsageTests` | T_ENUM_USE_01 | Handler không được gọi `Enumeration<T>.FromName/FromId` trực tiếp (throw `KeyNotFoundException` → 500) — phải `TryFromName` + `Fail<T>(ErrorDescriptor)` | **P5/P6**, chỉ khi module dùng Smart Enum (`Enumeration<TEnum>`, P1) trong 1 handler |

### Nhóm C — Ranh giới cross-module (chỉ có ý nghĩa từ module thứ 2)

| Test class | ID | Canh gác điều gì | Khi nào cần |
| --- | --- | --- | --- |
| `BoundedContextArchTests` | T091 | `Infrastructure.A` không reference `Infrastructure.B` | **module 2+** — đây chính là luật `❌ Infrastructure.A → Infrastructure.B` đã cấm ở [00-lộ-trình §3](00-lo-trinh-tong-the.md); trước khi có module 2, luật này **không thể vi phạm** (chỉ có 1 Infrastructure) nên viết sớm hơn cũng không gate được gì |
| `ContractArchTests` | — | `Module.*.Contracts` giữ zero-dependency (không kéo Domain/Application/EF vào) | **module 2+**, khi bắt đầu có project `Contracts` chia sẻ cross-module thật |
| `ReadModelParityArchTests` | T_READMODEL_01 | Cột mà `*ReadDbContext` (đọc cross-schema) map vào bảng vật lý phải tồn tại trong model của DbContext **chủ sở hữu** bảng đó | **module 2+**, chỉ khi có read model đọc bảng của module khác (Dapper/EF cross-schema — đã hoãn ở P3 §2) |
| `{Module}ModuleTests` (`HumanResourceModuleTests`, `OrganizationModuleTests`, `ReferenceDataModuleTests`...) | T_HRE_01–04, T_ORG_01–04, T_REF_01–05 | Luật biên giới **riêng của từng module** (vd Contracts không leak Domain type, Api không reference Infrastructure của module khác) | **Viết 1 file MỚI cho MỖI module**, không phải 1 gate chung — xem §3 |
| `ModuleInstallerArchTests` | T_MOD_01–02 | Mỗi `Module.*.Infrastructure` có đúng 1 `IModuleInstaller` descriptor | ⏳ **[tuỳ nhu cầu]** — chỉ khi hệ thống áp dụng pattern "declarative module install" (`InstallFull<TModule>()` thay gọi tay `Add{Module}*`); hệ thống nhỏ gọi tay vẫn ổn, đừng thêm tầng trừu tượng này chỉ để "cho giống Successor" |

### Nhóm D — Value Object, Index, Grid/Search (chỉ khi tính năng tương ứng tồn tại)

| Test class | ID | Canh gác điều gì | Khi nào cần |
| --- | --- | --- | --- |
| `IndexArchTests` | T_IDX_001 | Mọi FK phải có index covering | ⏳ Đã có `ApplyForeignKeyIndexConvention` tự động ở [04-p3 §4](04-p3-platform-persistence.md) — gate này là **lưới kiểm chứng convention không bị bypass** (raw SQL, Fluent config override thủ công), không phải cơ chế chính. Thêm khi có ai từng bypass convention 1 lần |
| `OwnedTypeInlineArchTests` | T_VO_001 | `OwnsOne` (Value Object — Address, Money...) phải table-split inline, không tách bảng riêng | ⏳ Ngay khi entity đầu tiên dùng `OwnsOne` cho Value Object |
| `SharedKernelArchTests` | — | `{Module}.SharedKernel` chỉ phụ thuộc `Platform.Domain` + Smart Enum, không hơn | ⏳ Chỉ khi module dùng project tuỳ chọn thứ 6 (`SharedKernel`, [00-lộ-trình §3](00-lo-trinh-tong-the.md)) |
| `DapperGridRegistrationTests` | T_GRID_01 | Mọi `DapperGridBase<,>` subclass phải được phủ đăng ký `IGridQuery<,>` | ⏳ P6 — cùng lúc với Dapper grid engine, đã hoãn ở [04-p3 §2](04-p3-platform-persistence.md) |
| `GridColumnLiteralTests` | — | Grid dùng typed column API (`GridColumn.For<T>`), không raw string | ⏳ P6, cùng nhóm Dapper grid |
| `GridJoinPlannerTests` | — | Unit test thuần cho `GridJoinPlanner` (topo-sort JOIN) — không phải arch gate, là test logic | ⏳ P6, viết cùng lúc viết `GridJoinPlanner` |
| `SearchArchTests` | T_SEARCH_01 | Handler không tự inject `ISearchProvider` — search field khai qua `IEntitySearchConfig<T>` | ⏳ P6, cùng lúc cơ chế search (`ISearchProvider`, đã liệt kê ⏳ ở P3 §2) |
| `IamUserGridSchemaContractTests` | — | Hợp đồng schema riêng cho **1 màn hình cụ thể** (`ui-config/iam/user.json`) | Không tái dùng được — ví dụ cho thấy: **arch test cũng có loại chỉ áp dụng cho đúng 1 màn hình**, viết khi màn hình đó có schema JSON cần khoá |

### Nhóm E — i18n 2 tầng (hoãn cùng lúc với cơ chế i18n, đã ghi ở P0 §6 backlog)

| Test class | ID | Canh gác điều gì | Khi nào cần |
| --- | --- | --- | --- |
| `EnumerationArchTests` | T_ENUM_001+ | Mọi Smart Enum member có bản dịch ở **mọi** ngôn ngữ (`i18nEnum.{lang}.json`) | ⏳ Cùng lúc với i18n 2 tầng — [00-lộ-trình §6](00-lo-trinh-tong-the.md) đã liệt "i18n 2 tầng" vào "chưa cần ngay" |
| `ErrorMessageKeyArchTests` | — | `ErrorDescriptor.BusinessCode` có bản dịch ở mọi ngôn ngữ | ⏳ cùng nhóm |
| `TranslationKeyParityArchTests` | — | Mọi key trong 1 file i18n module có mặt ở **tất cả** ngôn ngữ (không lang này có, lang kia thiếu) | ⏳ cùng nhóm |
| `EnumTranslateEnrichmentArchTests` | T_ENUMTRANSLATE_01 | `[EnumTranslate]`/`[EnumTranslateOptions]` đúng shape để enricher bind được | ⏳ cùng nhóm, chỉ khi dùng cơ chế enrichment tự động này |
| `ReferenceEnrichmentArchTests` | T_REFENRICH_01 | `[ReferenceLabel<T>]`/`[ReferenceOptions<T>]` đúng shape | ⏳ cùng nhóm |
| `ErrorDescriptorDocsTests` | — | Mọi `ErrorDescriptor` có trang docs markdown tương ứng | ⏳ gate quy trình (governance), không phải kiến trúc — chỉ cần khi team quyết định docs lỗi là bắt buộc |

### Nhóm F — Cấu hình động & quản trị nợ kỹ thuật (đặc thù quy mô lớn)

| Test class | ID | Canh gác điều gì | Khi nào cần |
| --- | --- | --- | --- |
| `UiConfigSchemaTests` | — | JSON schema cho `ui-config/*.json` (cơ chế metadata-driven UI) | ⏳ — [00-lộ-trình §6](00-lo-trinh-tong-the.md) đã liệt "metadata-driven" vào backlog chưa cần ngay |
| `OptionDtoArchTests` | T_OPTION_01–02 | `*OptionDto` phải kế thừa `SelectOptionDto`, phải là `record` | ⏳ chỉ nếu hệ thống áp dụng đúng convention "1 họ DTO cho mọi dropdown" này |
| `SuppressionBudgetArchTests` | T_SUPPRESS_01 | Tổng số `[SuppressMessage]`/`#pragma warning disable` cho rule Sonar trong `Src/` không vượt baseline | ⏳ chỉ khi team đã bật Sonar ratchet (`.editorconfig` khoá severity=error) — gate quản trị nợ, không phải luật kiến trúc |

**Tổng cộng 34 file .cs thật tìm thấy** (không tính `obj/`, không tính
`BinProbingAssemblyResolver.cs` — helper resolve assembly, không phải test).
Con số "36" ở [00-lộ-trình](00-lo-trinh-tong-the.md) là mô tả tổng thể quy mô
hệ thống ở một thời điểm khảo sát khác — số thật dao động theo thời gian,
không phải hằng số cần khớp chính xác.

---

## 3. Cách viết `{Module}ModuleTests` — mẫu cho **mỗi** module mới

Khác với nhóm A/B/D/E/F (viết 1 lần, áp dụng chung), `{Module}ModuleTests` là
**khuôn phải lặp lại cho mỗi module**. Đọc cấu trúc thật của
`OrganizationModuleTests`/`HumanResourceModuleTests`/`ReferenceDataModuleTests`:

```csharp
public class {Module}ModuleTests
{
    private static readonly Assembly ContractsAssembly =
        typeof(VNR.Module.{Module}.Contracts.{Module}ContractsMarker).Assembly;
    private static readonly Assembly ApiAssembly = /* tương tự, .Api */;

    [Fact]
    public void T_{MOD}_01_Contracts_ShouldNotReference_DomainOrApplication() { /* ... */ }

    [Fact]
    public void T_{MOD}_02_Api_ShouldNotReference_OtherModuleInfrastructure() { /* ... */ }

    // T_{MOD}_03, 04... — luật đặc thù của CHÍNH module này, không tổng quát hoá được
}
```

**Khi tạo module thứ N, việc cuối cùng trước khi coi module đó "xong" là tạo
`{Module}ModuleTests.cs` mới** — copy khuôn từ module gần nhất, đổi tên
assembly, giữ nguyên 2 luật biên giới cố định (Contracts zero-dep, Api không
reference Infrastructure module khác), rồi thêm luật riêng nếu module có ràng
buộc đặc thù (`ReferenceDataModuleTests` có tới 5 luật — nhiều hơn 2 module
kia vì `ReferenceData` có nhiều catalog entity với luật riêng, ví dụ luật về
`AddCatalogCrud` không bị gọi trùng).

---

## 4. Cấm ở P6

| Cấm | Vì sao | 
| --- | --- |
| Viết gate cho tính năng **chưa tồn tại** (vd viết `DapperGridRegistrationTests` trước khi có `DapperGridBase` nào) | Gate chưa từng đỏ = gate chưa được chứng minh (nguyên tắc lặp lại từ P0 §7) — tệ hơn nữa, nó tạo ảo giác "đã canh gác" trong khi chưa canh gác gì |
| Sửa gate cho lỏng đi để 1 module cụ thể pass, thay vì loại trừ module đó tường minh qua `ProjectReference` (§1.3) | Làm yếu gate cho **mọi** module, không chỉ module có nợ — nợ kỹ thuật ẩn hoàn toàn, không ai thấy trong diff PR |
| `[Fact(Skip = "...")]` rải rác để tạm tắt 1 gate | Không có nơi tập trung để review "đang tắt bao nhiêu gate, vì sao" — dùng cơ chế loại trừ qua `ProjectReference` (rõ ràng trong `.csproj`, có comment) thay vì `Skip` rải rác |
| Trộn `NetArchTest.Rules` và reflection trần trong cùng 1 project test | Không nhất quán — dev đọc test sau phải học 2 API khác nhau cho cùng 1 loại việc. Chọn 1, giữ nguyên |
| Copy-paste `LoadAllBinAssemblies`/`SafeGetTypes`/`GetSolutionDirectory` vào **mọi** file test mới mà không cân nhắc gộp | Đây là nợ kỹ thuật **có thật ngay trong Successor** (30+ file lặp y hệt 3 helper này) — hệ thống mới bắt đầu sạch nên gộp chúng vào 1 `TestHelpers.cs`/base class ngay từ file test thứ 2, đừng lặp lại chính nợ đó |

---

## 5. Checklist P6 (liên tục, không "rời" một lần)

- [ ] Mỗi cột mốc trong bảng §2 đều có gate tương ứng **xuất hiện đúng lúc**, không sớm hơn (chưa có gì để canh) và không trễ hơn (đã có vi phạm trước khi có gate)
- [ ] Mỗi gate mới **đã kiểm chứng đỏ** ít nhất 1 lần bằng cách cố tình vi phạm — không có ngoại lệ, kể cả gate "chỉ có 5 dòng"
- [ ] Module mới nào cũng có `{Module}ModuleTests.cs` riêng trước khi coi module đó hoàn thành (§3)
- [ ] Nếu loại trừ 1 module khỏi 1 gate cụ thể: loại trừ qua `ProjectReference` của test project + có comment giải thích, không dùng `[Skip]`
- [ ] `dotnet test Tests/{ArchTestProject}` là lệnh **duy nhất** cần chạy trước khi coi bất kỳ PR nào sẵn sàng — đúng dòng lệnh đã ghi trong `CLAUDE.md` gốc backend ([01-p0 §6](01-p0-nen-mong-solution.md))
- [ ] 3 helper (`LoadAllBinAssemblies`/`SafeGetTypes`/`GetSolutionDirectory`) đã gộp vào 1 chỗ dùng chung, không lặp lại ở từng file test

---

**Tiếp theo:** [08-tra-cuu-file-class.md](08-tra-cuu-file-class.md) — bảng tra
cứu tổng hợp mọi file/class/interface đã xuất hiện xuyên suốt 7 file trước:
tên → thuộc layer nào → phase nào tạo ra nó → nó làm gì trong 1 câu. Dùng khi
cần tra nhanh "`ITransactionManager` khai ở đâu, ai implement" mà không muốn
đọc lại cả file 03/04.
