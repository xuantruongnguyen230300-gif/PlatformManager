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

**KHÔNG phải finding:**

- Thấy `PlatformManager.Modules.<Tên>.*` — đó là **hiện trạng đã biết**
  (`Modules.DtiWeekly.*`), không phải dấu hiệu quay lại mô hình cũ. ArchTest
  `Modules_MustNotReference_OtherModules` đang canh nó là **đúng, phải giữ**.
- Chưa có `Core.Persistence`, `Core.Api`, `Business.*` — đang thi công.

**Là finding thật:**

- Code tạo **module mới** ngoài `DtiWeekly` mà không có lý do tách domain ghi rõ
  trong doc.
- `Core.*` `ProjectReference` (kể cả gián tiếp) tới `Business.*` — đọc `.csproj`
  trực tiếp, đừng chỉ tin tên project.
- `*.Api` reference thẳng `*.Persistence`/`*.Infrastructure` — chỉ được qua
  `*.Application`.
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

**FE:** `modules/` chỉ chứa module nghiệp vụ; 4 màn Core (`login`,
`doi-mat-khau`, `quan-tri-nguoi-dung`, `phan-quyen`) phải ở `platform/`. Gate G8
(ESLint `no-restricted-paths`) — kiểm bằng cách **đọc `eslint.config.js`**,
không tin báo cáo.

## 2. Phân quyền theo hành động

> 📖 [`be-api-controller.md`](be-api-controller.md) §Phân quyền theo hành động

Mỗi controller/action **ghi** dữ liệu nghiệp vụ phải có `[RequirePermission(key)]`
khớp `PermissionMatrix`, không phải `[Authorize]` trần. Thiếu → **MISSING, báo ở
mức nghiêm trọng nhất trong report**, không gộp chung với các PARTIAL khác: đây là
[OWASP #1 Broken Access Control](https://owasp.org/Top10/2025/A01_2025-Broken_Access_Control/),
và dự án đã qua giai đoạn demo.

**KHÔNG phải finding:** quyết định *"không phân biệt role cho nghiệp vụ này"* đã
CHỐT tường minh với người dùng **và** ghi rõ trong code.

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

**KHÔNG phải finding:** entity chỉ có 1 luồng ghi (CRUD thường).

## 4. Rate limiting & cấu hình fail-fast

> 📖 [`be-api-controller.md`](be-api-controller.md) §Rate limiting ·
> [`be-architecture.md`](be-architecture.md) §Cấu hình — fail-fast validation

- `Program.cs` thiếu `AddRateLimiter`/`UseRateLimiter`, hoặc `/api/auth/login`
  không có policy riêng chặt hơn API thường → **MISSING**.
- `IOptions<T>` mới thiếu `.ValidateDataAnnotations().ValidateOnStart()` →
  **PARTIAL** (nhẹ hơn 2 mục trên: hậu quả là lỗi runtime chậm phát hiện, không
  phải lỗ hổng bảo mật).

## 5. CI & gate — KHÔNG kiểm sự tồn tại, PHẢI kiểm còn chạy được

Repo **cố ý không có CI** (`.github/workflows/` rỗng, người dùng xoá 2026-08-21).
**Đừng báo "thiếu CI" như finding** — đó là lựa chọn đã biết.

Điều **vẫn phải kiểm**: các gate chạy tay còn **chạy được** không — `dotnet build`,
`dotnet test`, `npx ng lint`, `npx ng test`, `bash .claude/check-docs.sh`. Vì không
còn máy nào chạy hộ, một gate hỏng sẽ không ai biết cho tới lượt review sau.

> ⚠️ Đợt 2026-08-23 phát hiện `scripts/fe-gate.sh` — script mà `fe/trien-khai/05-gate.md`
> khai là chạy G1/G3/G6 — **không tồn tại**. Đây đúng là kịch bản trên. Kiểm sự
> tồn tại của script trước khi coi gate là xanh.

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
