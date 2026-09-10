---
kind: luat
scope: core
verified: 2026-09-06
---

# Tiêu chí chấm review — cái gì là finding, cái gì không

Dùng bởi `core-reviewer`. File này **không nhắc lại quy ước** — quy ước nằm ở
`be-*.md` / `fe-*.md` cạnh đây. Nó chỉ trả lời: *lệch khỏi quy ước đó thì chấm
mức nào, và trường hợp nào lệch mà **không** phải lỗi.*

> **Lịch sử:** nội dung này trước nằm trong `.claude/agents/core-reviewer.md`
> (~170 dòng). Chuyển về đây 2026-08-23 theo `.claude/CLAUDE.md` §2 — *"cái gì
> là finding"* là tri thức về codebase, không phải quy trình của agent.

Mức chấm: **PASS** (đúng) · **PARTIAL** (có nhưng thiếu/sai một phần) ·
**MISSING** (không có). Mọi finding **bắt buộc** kèm `file:line`; không có
`file:line` thì không ghi finding.

---

## 1. Ranh giới Core ↔ Business

> 📖 Quy ước: [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) —
> **đọc bảng `🚧` "có thật hôm nay → sẽ thành" ở đầu file trước khi chấm mục này.**

> 🚧 **Hiện trạng 2026-09-10 — BE và FE nay LỆCH nhau, đừng chấm chung một câu.**
>
> - **BE — chưa có module nghiệp vụ.** Module duy nhất (`Modules.DtiWeekly.*`) đã gỡ
>   để xây lại và chưa dựng lại: không còn project `Modules.*` nào trong
>   `src/BE/PlatformManager.slnx`.
> - **FE — `src/FE/src/app/modules/` đã có trở lại** (đối chiếu 2026-09-10): module
>   nghiệp vụ dựng 2026-09-09, khai đủ trong `src/FE/eslint.config.js:29`.
>
> Kiểm bằng `grep -c '<Project Path=' src/BE/PlatformManager.slnx`,
> `ls -d src/FE/src/app/modules/*/` và `ls -d src/FE/src/app/*/` — đừng chấm theo
> con số nào chép sẵn trong văn bản.
>
> 🔄 **LẬT 2026-09-10:** bản trước ghi `src/FE/src/app/modules/` *"không tồn tại"*
> cho cả hai phía. Vế BE vẫn đúng; vế FE đã lạc hậu.

**KHÔNG phải finding:**

- **Không tìm thấy module nghiệp vụ nào phía BE.** Đó là hiện trạng đã biết ở trên,
  không phải MISSING, và không phải dấu hiệu ai đó xoá nhầm. 🔄 SỬA 2026-09-10: câu này
  trước ghi *"(BE lẫn FE)"* — vế FE hết hiệu lực, `src/FE/src/app/modules/` nay có thật.
- **ArchTest `Modules_MustNotReference_OtherModules` hiện pass "rỗng"** — không
  còn assembly `Modules.*` nào để nó bắt. Đây **không** phải test chết:
  `src/BE/Tests/PlatformManager.ArchTests/CoreModuleBoundaryTests.cs:49`. Rule
  còn lại trong cùng file, `Core_MustNotReference_AnyModulesAssembly`
  (`src/BE/Tests/PlatformManager.ArchTests/CoreModuleBoundaryTests.cs:32`),
  **vẫn kiểm thật** vì nó quét tham chiếu của `Core.*`, không cần module tồn tại.
- Thấy tên `PlatformManager.Modules.<Tên>.*` quay lại — bản thân cái tên không
  sai; chỉ thành finding khi thiếu lý do tách domain (xem ngay dưới).
- Chưa có `Business.*` — đang thi công. `Core.Api` **đã dựng 2026-09-09**; `Core.Persistence`
  **đã tách 2026-09-10** — nó vắng mặt nay LÀ finding (xem `doc/kien-truc-core-module.md` §DbContext).
- Namespace `PlatformManager.Core.Infrastructure.Persistence*` / `…Identity` nằm trong assembly
  `Core.Persistence` — nợ **có chủ đích** (giữ cho `ModelSnapshot` còn biên dịch), không phải finding.

**Là finding thật:**

- Code tạo **module nghiệp vụ mới** mà không có lý do tách domain ghi rõ trong
  doc — mô hình đã chốt là **1 khối `Business.*`**, không phải N-module.
- **Đề xuất xoá ArchTest ranh giới vì "không còn module nào để canh"** — chúng là
  bảo hiểm cho module đầu tiên được dựng lại; gỡ lúc này là mất lưới đúng ngay
  trước lúc cần nó nhất.
- `Core.*` `ProjectReference` (kể cả gián tiếp) tới `Business.*` — đọc `.csproj`
  trực tiếp, đừng chỉ tin tên project.
- `*.Api` reference thẳng `*.Persistence`/`*.Infrastructure` — chỉ được qua
  `*.Application`.
- `*.Persistence` reference `*.Infrastructure` hoặc `*.Api` (kể cả chỉ khai
  `ProjectReference`) — canh bởi `PersistenceLayerBoundaryTests` (ArchTests).
- `PlatformManagerDbContext` hardcode reference assembly `Business.*` thay vì
  nhận danh sách từ host.
- `Directory.Build.props`/`Directory.Packages.props` bị lồng vào `Core/` thay vì
  nằm ở `src/BE/` — `Business`/`Api` sẽ âm thầm mất cấu hình chung.
- ArchTest ranh giới không tồn tại **hoặc không pass** — xác nhận bằng
  `dotnet test`, không chỉ đọc code.

**SOLID/OOP** (📖 [`be-architecture.md`](be-architecture.md) §SOLID & OOP):
implementation ném `NotImplementedException`/`NotSupportedException` (vi phạm
LSP/ISP) · `Core.*` bị sửa chỉ để phục vụ một module cụ thể (vi phạm OCP) ·
field nghiệp vụ của entity không `private set` + mutation qua method tên nghiệp
vụ.

**FE:** màn Core phải ở `platform/`; `modules/` chỉ chứa module nghiệp vụ — và thư
mục này **đã dựng lại** (đối chiếu 2026-09-10, `src/FE/eslint.config.js:29`). Vì vậy
*"không có `modules/`"* **không còn là hiện trạng**: màn nghiệp vụ nằm trong
`platform/`, hoặc màn Core nằm trong `modules/`, là finding thật. Liệt kê thư mục
thật bằng `ls -d src/FE/src/app/*/`, `ls -d src/FE/src/app/modules/*/` và
`ls -d src/FE/src/app/platform/*/` thay vì tin danh sách chép sẵn (bản trước ghi
cứng "4 màn Core" và đã lạc hậu).

> 🔄 **LẬT 2026-09-10:** bản trước miễn trừ *"không có `modules/`"* khỏi finding —
> đúng khi thư mục thật sự không tồn tại, nhưng miễn trừ đó nay đã hết hiệu lực.

Gate G8 (ESLint `import/no-restricted-paths`) **đang khai zone thật và đang chạy**
(đối chiếu 2026-09-10): `BUSINESS_MODULES` ở `src/FE/eslint.config.js:29` khai đủ
module có trong `src/FE/src/app/modules/`, nên `moduleBoundaryZones` sinh ra thật
và block G8 không còn bị bỏ qua.

**Phép thử phải chạy mỗi lượt review** — so hai tập, chúng phải KHỚP:

```bash
ls -d src/FE/src/app/modules/*/
grep -n "BUSINESS_MODULES" src/FE/eslint.config.js
# PASS: mọi thư mục module có tên trong `BUSINESS_MODULES`, và ngược lại
```

Lệch theo **bất kỳ** chiều nào đều là finding thật — đây đúng là hồi quy mà
[`fe-architecture.md`](fe-architecture.md) gọi là *"bước dễ mất nhất, và mất thì
không ai biết"* (§Bước 3):

- `src/FE/src/app/modules/` **có** module mà `BUSINESS_MODULES` **thiếu** tên đó →
  module ấy hoàn toàn không được G8 canh, trong khi `ng lint` vẫn xanh.
- `BUSINESS_MODULES` khai tên **không có** thư mục tương ứng → còn tệ hơn không
  khai: `no-restricted-paths` không phân giải nổi `target` nên không chặn gì, tức
  G8 là no-op nhưng vẫn TRÔNG như đang chạy.

Kiểm bằng cách **đọc `src/FE/eslint.config.js`** và `ls` như trên, không tin báo cáo.

> **Vì sao mảng `zones` không bao giờ được để rỗng** — kiến thức vẫn đúng và vẫn
> cần khi dựng repo mới, hoặc nếu module nghiệp vụ cuối cùng bị gỡ: schema của rule
> đòi `zones` tối thiểu 1 phần tử, nên truyền mảng rỗng làm ESLint chết ngay lúc nạp
> config ("Invalid Options") và `ng lint` đỏ vì lý do không liên quan tới code. Vì
> vậy block G8 bọc trong spread có điều kiện — `BUSINESS_MODULES` rỗng →
> `moduleBoundaryZones` rỗng → block không sinh ra.
>
> 🔄 **LẬT 2026-09-10:** bản trước ghi G8 *"hiện không khai zone nào … và đó
> không phải finding"*. Đúng khi chưa module nào tồn tại, nhưng module nghiệp vụ đã
> dựng 2026-09-09 — để nguyên câu đó là dạy reviewer bỏ qua đúng hồi quy nêu trên.

## 2. Phân quyền theo hành động

> 📖 [`be-api-controller.md`](be-api-controller.md) §Phân quyền theo hành động

Mỗi controller/action **ghi** dữ liệu nghiệp vụ phải có `[RequirePermission(key)]`
khớp `PermissionMatrix`, không phải `[Authorize]` trần. Thiếu → **MISSING, báo ở
mức nghiêm trọng nhất trong report**, không gộp chung với các PARTIAL khác: đây là
[OWASP #1 Broken Access Control](https://owasp.org/Top10/2025/A01_2025-Broken_Access_Control/),
và dự án đã qua giai đoạn demo.

**KHÔNG phải finding:**

- Quyết định *"không phân biệt role cho nghiệp vụ này"* đã CHỐT tường minh với
  người dùng **và** ghi rõ trong code.
- **Endpoint Core hiện có (`AuthController` / `MetaController` / `PermissionsController` /
  `UsersController`) chỉ mang `[Authorize]`.** Đây là hiện trạng đã biết và đã ghi
  ở [`be-api-controller.md`](be-api-controller.md) §Phân quyền theo hành động:
  cơ chế `[RequirePermission]` **đã dựng đủ** nhưng **chưa endpoint sản phẩm nào
  khai nó**. Ghi *"gap đã biết, đã theo dõi ở file chủ"* — **đừng mở lại thành N
  finding MISSING mới**, mỗi lượt review một lần, cho cùng một gap.

> 🔄 **LẬT 2026-09-06.** Bản trước của mục này chỉ có đúng một câu miễn trừ
> (*"đã CHỐT … ghi rõ trong code"*), nên đọc theo đúng chữ thì **mọi** action ghi
> của 4 controller Core đều phải bị chấm MISSING *"ở mức nghiêm trọng nhất"* — một
> tiêu chí bắt reviewer báo lại cùng một gap đã có chủ sở hữu, mỗi lượt một lần.
> Đo hiện trạng bằng lệnh, đừng tin câu văn:
> `grep -rn "\[RequirePermission" src/BE --include=*.cs | grep -v Tests | grep -v "///" | grep -v "//"`
> — PASS của mục này là *"khớp con số mà `be-api-controller.md` đang ghi"*, không
> phải *"in ra 0"*.

## 3. Concurrency — token phiên bản

> 📖 [`be-entity-domain.md`](be-entity-domain.md) §RowVersion

Entity mới/sửa có **≥2 luồng ghi độc lập** chạm cùng bản ghi (vd import hàng loạt
+ sửa tay từng field) mà **thiếu token concurrency** → **MISSING**.

⚠️ **Chấm theo KIỂU CLR của property, không chấm theo tên method.** Dự án chạy
**Npgsql**, nên đúng là property CLR **`uint`** + `.Property(x => x.Version)
.IsRowVersion()` (Npgsql tự bind vào cột hệ thống `xmin`) — `builder
.UseXminAsConcurrencyToken()` (recipe cũ) đã bị Npgsql GỠ HẲN khỏi
`Npgsql.EntityFrameworkCore.PostgreSQL` từ khoảng bản 7.x, KHÔNG còn biên dịch được
với package version dự án đang dùng (xác nhận 2026-08-24), nên thấy method đó trong
diff là dấu hiệu code chưa build thật, không phải điểm cộng. Ngược lại thấy
`.IsRowVersion()` trên property **`byte[]`** thì đó **là finding nghiêm trọng,
không phải đạt** — công thức SQL Server trên PostgreSQL tạo cột không ai cập nhật,
check concurrency **vô hiệu im lặng**. Xem `be-entity-domain.md` §RowVersion (đã
cập nhật 2026-08-24) cho recipe đầy đủ.

**KHÔNG phải finding:**

- Entity chỉ có 1 luồng ghi (CRUD thường).
- **`AppUser` không có property `uint Version`.** Token phiên bản của người dùng
  là `ConcurrencyStamp` **có sẵn của ASP.NET Core Identity** (kiểu `string`,
  không thêm cột) — đây là lựa chọn đúng, không phải thiếu sót. Đường ghi thật:
  `src/BE/Core/PlatformManager.Core.Application/Users/UpdateUserCommand.cs:62`;
  test khoá hành vi:
  `src/BE/Tests/PlatformManager.Core.IntegrationTests/Users/UserUpdateVersionTests.cs:77`.
  Chỉ chấm finding khi endpoint sửa `AppUser` **bỏ qua** token này, không phải khi
  nó không dùng `uint`.

> 🔄 **LẬT 2026-09-06.** Bản trước chỉ nêu hai kiểu CLR (`uint` đúng, `byte[]` sai)
> và không nhắc `ConcurrencyStamp`. Chấm đúng theo chữ đó thì entity duy nhất
> **đang thật sự có** kiểm tra concurrency trong repo sẽ bị đánh MISSING vì sai
> kiểu, trong khi cơ chế của nó chạy và có test phủ.

## 4. Rate limiting & cấu hình fail-fast

> 📖 [`be-api-controller.md`](be-api-controller.md) §Rate limiting ·
> [`be-architecture.md`](be-architecture.md) §Cấu hình — fail-fast validation

- `Program.cs` thiếu `AddRateLimiter`/`UseRateLimiter`, hoặc `/api/auth/login`
  không có policy riêng chặt hơn API thường → **MISSING**.
- `IOptions<T>` mới thiếu `.ValidateDataAnnotations().ValidateOnStart()` →
  **PARTIAL** (nhẹ hơn 2 mục trên: hậu quả là lỗi runtime chậm phát hiện, không
  phải lỗ hổng bảo mật).

## 5. CI & gate — KHÔNG kiểm sự tồn tại, PHẢI kiểm còn chạy được

Repo **cố ý không có CI**: thư mục `.github/` **không tồn tại** (người dùng xoá
2026-08-21; đối chiếu 2026-09-06 bằng `ls -d .github`).
**Đừng báo "thiếu CI" như finding** — đó là lựa chọn đã biết.

Điều **vẫn phải kiểm**: các gate chạy tay còn **chạy được** không — `dotnet build`,
`dotnet test`, `npx ng lint`, `npx ng test`, `bash .claude/check-docs.sh`. Vì không
còn máy nào chạy hộ, một gate hỏng sẽ không ai biết cho tới lượt review sau.

> ⚠️ Đợt 2026-08-23 phát hiện `scripts/fe-gate.sh` — script mà `fe/trien-khai/05-gate.md`
> khai là chạy G1/G3/G6 — **không tồn tại**. Đây đúng là kịch bản trên. Kiểm sự
> tồn tại của script trước khi coi gate là xanh.
>
> Script này **nay đã có** (`ls -l scripts/fe-gate.sh`, đối chiếu 2026-09-06) — nên
> "thiếu `fe-gate.sh`" không còn là finding. Nhưng nó **không phải toàn bộ cổng FE**:
> phần còn lại nằm ở `ng lint`/`ng build`/`ng test`, xem `.claude/CLAUDE.md` §8.

## 6. Query, index, N+1, cache

> 📖 [`be-performance.md`](be-performance.md) · nền: [`../wiki-core/be/11-performance-caching.md`](../wiki-core/be/11-performance-caching.md)

Đây là mục **dễ chấm sai nhất** — đọc phần "KHÔNG phải finding" trước.

**Là finding thật:**

- Repository/query **chỉ đọc** (map sang DTO, không `SaveChanges`) thiếu
  `AsNoTracking()` → PARTIAL, liệt kê `file:line` từng chỗ.
- Query lọc nóng không có index **dẫn đầu đúng cột đó**. Index `(A, B)` mà query
  chỉ lọc theo `B` → **vẫn MISSING**, không tính là "đã có index".
- `.ToListAsync()` đứng **trước** `.Distinct()`/`.Skip()`/`.Where()` trong cùng
  một hàm → PARTIAL.
- `await` trong `foreach`/`for` gọi DB (N+1) → PARTIAL.
- Cache được thêm mà thiếu 1 trong 3: số đo, danh sách đường ghi cần invalidate,
  test invalidation → PARTIAL, ghi rõ thiếu cái nào.
- Cache dữ liệu **phân quyền** chỉ dựa TTL, không invalidate tường minh →
  **mức nghiêm trọng nhất**: quyền đã thu hồi còn hiệu lực tới hết TTL là lỗ hổng
  bảo mật, không phải vấn đề hiệu năng.
- `static Dictionary`/`ConcurrentDictionary` làm cache dữ liệu **từ DB** →
  PARTIAL (không eviction, không invalidation).
- Thêm `IDistributedCache`/Redis khi hệ thống vẫn 1 process → PARTIAL, trái
  quyết định đã CHỐT.

**KHÔNG phải finding:**

- Thiếu cache ở một endpoint chậm. Quyết định đã CHỐT là cache đi **sau** bước
  sửa query/thuật toán và **sau** khi đo. Ghi *"chưa áp dụng — chưa tới ngưỡng"*.
- Phân trang/search trong bộ nhớ trên tập có **trần trên nhỏ ghi rõ bằng con số**
  trong comment. Comment *"dataset hiện tại nhỏ"* **không kèm con số** → PARTIAL,
  vì ngoại lệ không kiểm chứng được.
- `ConcurrentDictionary<Type, MethodInfo>` cache reflection/metadata bất biến
  trong 1 process → hợp lệ, nguồn dữ liệu là chính assembly.
- Query lấy entity **để sửa** mà không `AsNoTracking()` → **đúng**. Ngược lại,
  thấy `AsNoTracking()` trên đường đọc-rồi-sửa thì đó **là** finding nghiêm trọng
  (thay đổi âm thầm không được ghi).

## 7. Test coverage cho thay đổi mới

> 📖 [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) ·
> test thật nằm ở `src/BE/Tests/`

Nhẹ, không thay QA: mỗi command/query/handler **mới hoặc đổi hành vi đáng kể**
(không phải sửa text lỗi hay thêm field nhỏ) cần ít nhất 1 test happy-path và 1
test edge-case/lỗi mong đợi. Tìm bằng Grep tên class test tương ứng.

Thiếu hẳn test cho handler mới → **MISSING**. Chỉ có happy-path → **PARTIAL**,
ghi rõ edge-case nào chưa phủ. **Không** chạy toàn bộ suite để chấm coverage %.

## 8. Đối chiếu API Contract Card — BE ↔ FE

> 📖 [`../../contracts/`](../../contracts/)

Chỉ áp dụng khi review **cả hai phía**, hoặc khi có card ở trạng thái
`IMPLEMENTED`. Mục đích: bắt lệch giữa cái BE **thật sự trả về** và cái FE **thật
sự gọi** — BE/FE tự đánh dấu `IMPLEMENTED` không đồng nghĩa đã khớp.

- `Route`/`Verb` trong card ↔ controller action thật (`[Route]`/`[Http*]`) —
  khớp route, khớp verb, request **phẳng** đúng như card ghi.
- `Response` fields trong card ↔ DTO thật BE trả ↔ model/mapper phía FE. FE map
  field không tồn tại ở BE, hoặc BE đổi tên mà FE không theo → **finding thật**.
  ⚠️ Casing trên dây là **camelCase** (`Program.cs` đặt `PropertyNamingPolicy`),
  **ngoại lệ duy nhất là key của `fields`** — xem
  [`be-api-controller.md`](be-api-controller.md) §Envelope response. Đừng chấm
  camelCase là lệch.
- `Lỗi mong đợi` (ErrorCode) trong card ↔ `ErrorDescriptor` khai ở BE ↔ logic
  bind lỗi phía FE. ErrorCode BE khai mà FE không xử lý → MISSING phía FE.

**KHÔNG phải finding:** card còn `DRAFT`/`AGREED` — ghi nhận trạng thái "đang
chờ", không đối chiếu code.

Bằng chứng phải là `file:line` của **cả 2 phía** đặt cạnh nhau trong cùng 1 finding.
