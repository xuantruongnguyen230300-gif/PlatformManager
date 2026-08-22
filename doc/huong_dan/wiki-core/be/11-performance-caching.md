# 11. Performance & Caching

> Bổ sung 2026-08-18. Trước file này, "performance" ở BE chỉ tồn tại dưới
> dạng 1 dòng trong [01-core-components.md](01-core-components.md) (#8
> Caching abstraction) — không có quy tắc nào về query pattern, index, hay
> thứ tự ưu tiên khi tối ưu. FE đã có [fe/13-performance.md](../fe/13-performance.md)
> tương ứng từ trước; đây là bản đối xứng cho BE.

## 1. Nguyên tắc gốc — 3 tầng, đi đúng thứ tự

| Tầng | Nội dung | Chi phí sửa | Sửa 1 lần thì |
| --- | --- | --- | --- |
| **1. Query pattern** | tracking, index, projection, N+1, đẩy phép lọc/gộp xuống SQL | Thấp nhất | Đúng mãi, không sinh nợ mới |
| **2. Thuật toán** | số round-trip, công tính lặp lại trên cùng input | Trung bình | Đúng mãi, không sinh nợ mới |
| **3. Cache** | tránh làm lại việc mà kết quả không đổi | Cao (invalidation) | Sinh nợ vĩnh viễn phải bảo trì |

**Luật:** không được nhảy sang tầng 3 khi tầng 1 và 2 chưa đúng.

Lý do không phải khẩu hiệu mà là cơ chế: cache **che** lỗi tầng 1/2 chứ
không sửa. Một endpoint đang seq scan + N+1 mà đem cache thì lần **miss**
đầu tiên vẫn chậm đúng như cũ (và mọi lần cache hết hạn, mọi lần deploy lại,
mọi lần invalidate) — bạn chỉ đổi "chậm luôn luôn" thành "chậm không đoán
được lúc nào", cộng thêm một tầng nữa để debug khi con số hiển thị ra sai.

## 2. Tầng 1 — Quy tắc query bắt buộc

| # | Quy tắc | Vì sao | Phát hiện bằng |
| --- | --- | --- | --- |
| Q1 | Query **chỉ để đọc** (map sang DTO, không sửa/`SaveChanges`) → bắt buộc `AsNoTracking()` | EF dựng snapshot cho từng entity để dò thay đổi — vô ích khi không ai sửa; tốn allocation + CPU + áp lực GC tỉ lệ thuận số dòng | `grep -c AsNoTracking` trên repository; số 0 là dấu hiệu chưa ai để ý |
| Q2 | Mỗi predicate lọc nóng phải có index **dẫn đầu đúng cột đó** | Index composite `(A, B)` **không** seek được cho query chỉ lọc theo `B` → seq scan toàn bảng, dù nhìn vào code tưởng "đã có index rồi" | Đọc `HasIndex` trong `*Configuration.cs`, đối chiếu từng `Where(...)` của repository |
| Q3 | `Distinct`/`GroupBy`/`Count`/phân trang chạy ở **SQL**, không kéo cả cột về app rồi mới làm | Chi phí tăng tuyến tính theo kích thước bảng thay vì theo kích thước kết quả — hỏng dần theo thời gian, không hỏng ngay | Tìm `.ToListAsync()` đứng **trước** `.Distinct()`/`.Skip()`/`.Where()` trong cùng một hàm |
| Q4 | Không gọi query trong vòng lặp (N+1) | `foreach` + `await` = 1 round-trip/phần tử; 20 dòng/trang thành 21 query | Tìm `await` bên trong `foreach`/`for`/`Select(async …)` |
| Q5 | Chỉ `Select` cột thật sự cần khi entity nặng hoặc chỉ dùng vài field | Bớt băng thông + bớt vật liệu cho change tracker | Đọc repository trả `List<Entity>` rồi chỉ dùng 2–3 property |

**Ngoại lệ được phép, nhưng phải ghi lý do tại chỗ:** Q3/Q5 có thể bỏ qua khi
tập dữ liệu có **trần trên rõ ràng và nhỏ** (vd 6 `CriteriaGroup` cố định).
Ghi comment nêu trần đó là bao nhiêu và điều kiện nào làm nó hết đúng —
"dataset hiện tại nhỏ" không kèm con số thì 2 năm sau không ai dám sửa.

## 3. Tầng 2 — Thuật toán

- **Gom round-trip.** Một màn hình cần N tập dữ liệu liên quan → cân nhắc
  gom thành ít query hơn thay vì N query tuần tự, hoặc chạy song song khi
  chúng độc lập (lưu ý: `DbContext` **không** thread-safe — muốn song song
  phải dùng scope/context riêng, đừng `Task.WhenAll` trên cùng 1 context).
- **Không tính lại cùng một thứ.** Vòng lặp gọi một hàm thuần trên cùng
  input chỉ khác tham số khung thời gian → tính một lượt rồi chia kết quả,
  đừng chạy lại toàn bộ phép quét cho từng khung.
- **Không gọi hàm nặng 2 lần trong 1 request** chỉ vì 2 nhánh code cùng cần
  nó — nâng lên biến cục bộ.

## 4. Tầng 3 — Cache

### 4.1 Lợi ích thật (chỉ có 3)

1. Bỏ round-trip cho dữ liệu **đọc rất nhiều, ghi rất hiếm**.
2. Bỏ **tính toán lặp lại** trên cùng input.
3. Giảm tải cho DB dùng chung (ở đây Postgres còn gánh cả Hangfire storage).

### 4.2 Cái giá (luôn phải trả, không có bản miễn phí)

- **Dữ liệu cũ.** Với dữ liệu phân quyền, "cũ" không phải phiền toái mà là
  **lỗ hổng bảo mật**: quyền đã bị thu hồi vẫn còn hiệu lực tới hết TTL.
- **Invalidation là nơi sinh bug.** Mỗi đường ghi mới (job nền, import, sửa
  tay) phải nhớ bump cache. Quên một đường = hiển thị số liệu sai — sai
  **lặng lẽ**, khó phát hiện hơn nhiều so với chậm.
- **Thêm hạ tầng** nếu chọn distributed: phải vận hành, monitor, và có đường
  fallback khi nó chết.

### 4.3 Cache cái gì / không cache cái gì

| Nên cache | Không nên cache |
| --- | --- |
| Đọc rất nhiều, ghi rất hiếm, **có đường ghi đếm được** để invalidate | Dữ liệu đổi liên tục hoặc đường ghi không kiểm soát được |
| Kết quả **không phụ thuộc người dùng** (hit rate cao) | Kết quả riêng từng user khi số user lớn (hit rate thấp, tốn RAM) |
| Tập dữ liệu **nhỏ, biết trần trên** | Tập không có trần trên rõ ràng |
| Chi phí tính lại **đã đo được** là đáng kể | Chưa đo — xem §5 |

### 4.4 In-memory hay distributed

**Mặc định: in-process** (`HybridCache` của .NET 9+, hoặc `IMemoryCache`).

Distributed (Redis) **chỉ** có ý nghĩa khi chạy **≥2 instance** — nhiều
instance mỗi cái giữ một bản in-memory riêng thì chúng lệch nhau, và
invalidate ở instance A không tới được instance B. Khi còn 1 process duy
nhất, in-memory là **nhất quán tuyệt đối** với chi phí vận hành bằng 0.

Ưu tiên `HybridCache` hơn `IMemoryCache` trần vì nó cho sẵn đường nâng cấp:
thêm một `IDistributedCache` vào DI là thành 2 tầng L1/L2, **không phải sửa
chỗ gọi**. Đây cũng chính là "distributed + local, tự fallback êm khi cache
down" mà [01-core-components.md](01-core-components.md) #8 mô tả — .NET 9 đã
đóng gói sẵn, không cần tự viết abstraction.

### 4.5 Invalidation

- **Dữ liệu ảnh hưởng quyền hạn → invalidate tường minh, KHÔNG dựa TTL.**
  Handler đổi dữ liệu phải xoá/bump key ngay trong cùng luồng ghi. TTL chỉ
  được dùng làm **lưới an toàn cuối** (phòng khi sót một đường ghi), không
  phải cơ chế chính.
- **Đường ghi từ job nền cũng phải invalidate.** Job Hangfire chạy trong
  worker riêng, không có `HttpContext` — dễ bị quên nhất.
- Khi có nhiều key dẫn xuất từ cùng một nguồn (vd dashboard theo từng kỳ) →
  dùng **version key** (bump 1 số, mọi key cũ tự trở thành lạc) thay vì cố
  liệt kê và xoá từng key.

### 4.6 Cấm

- Cache trong tầng `*.Application` bằng cách `new`/gọi thẳng
  `IMemoryCache` — Application chỉ biết interface, đúng như mọi phụ thuộc
  hạ tầng khác (xem `.claude/rules/architecture.md` §Project layout).
- Dùng `static` `Dictionary`/`ConcurrentDictionary` làm cache dữ liệu **từ
  DB** — không có eviction, không có invalidation, rò rỉ theo thời gian.
  (Khác với cache **reflection/metadata bất biến trong 1 process**, vd
  `ConcurrentDictionary<Type, MethodInfo>` — cái đó hợp lệ, vì nguồn dữ liệu
  là chính assembly, không bao giờ đổi lúc chạy.)
- Thêm cache mà không kèm: (a) số đo trước/sau, (b) danh sách đầy đủ đường
  ghi phải invalidate.

## 5. Đo trước — không có số thì không tối ưu

Tối thiểu, trước khi sửa bất cứ thứ gì ở §2–§4:

- Bật EF Core command logging ở môi trường dev (`LogTo` + `EnableSensitiveDataLogging`
  **chỉ dev**) để **đếm số query thật/request** — con số này thường gây bất
  ngờ hơn thời gian từng query.
- `EXPLAIN ANALYZE` cho query nghi ngờ nhất — xác nhận `Seq Scan` hay
  `Index Scan`, đừng suy từ việc "đã khai `HasIndex` rồi".
- Ghi lại latency trước/sau vào chính file này (mục §7) — lần tối ưu sau sẽ
  cần biết lần này đã đi tới đâu.

Việc này thuộc [07-observability.md](07-observability.md) §Metrics — không
dựng APM đắt tiền, chỉ cần đếm được và so sánh được.

## 6. Áp dụng vào PlatformManager (2026-08-18)

### 6.1 Bối cảnh quy mô

62 chỉ tiêu, cập nhật theo tuần → ~3.200 bản ghi `CriteriaAssessment`/năm.
Một Postgres duy nhất, dùng chung với Hangfire storage. **Một process duy
nhất** (`AddHangfireServer()` chạy in-process trong `Program.cs`).

Kết luận từ quy mô này: vấn đề hiện tại **không phải "DB chậm"** mà là **số
round-trip và công lặp lại** — đúng tầng 1 và 2, không phải tầng 3.

### 6.2 Quyết định đã CHỐT

1. **Có áp dụng cache**, nhưng **phạm vi hẹp** và **không phải việc làm đầu
   tiên**. Thứ tự bắt buộc: sửa tầng 1 → tầng 2 → đo lại → mới cache.
2. **In-memory (`HybridCache`), KHÔNG Redis** — hệ thống 1 process, chưa có
   kế hoạch scale-out nào. Xem lại quyết định này khi thật sự chạy ≥2
   instance.
3. **Ứng viên cache duy nhất đã đủ bằng chứng ngay từ code: permission
   matrix** (§6.3 A0). Mọi ứng viên khác (dashboard aggregate, danh mục) chỉ
   xét lại **sau** khi đo ở bước 4.
4. Cache permission **bắt buộc invalidate tường minh** trong
   `UpdatePermissionMatrixCommand`/`UpdateResourcePermissionMatrixCommand` —
   không dựa TTL, vì lý do bảo mật ở §4.2.

Quyết định này **thay thế** trạng thái "hoãn vì chưa có bằng chứng" của mục
#8 trong [01-core-components.md](01-core-components.md) — bằng chứng đã có,
liệt kê ở §6.3.

#### Cập nhật 2026-08-18 (sau khi làm xong bước 0–4, phạm vi Core)

5. **KHÔNG thêm cache permission matrix. Bước 5 đóng lại, không phải nợ.**
   Quyết định #3 ở trên nói permission matrix là "ứng viên cache duy nhất đã đủ
   bằng chứng" — bằng chứng đó là **2 query/request**, tức một vấn đề *tầng 2*
   (số round-trip). Sau khi sửa đúng tầng 1/2, số đo (§7.1, §7.2) cho thấy:
   - Còn **1** query, `Execution Time 0,038 ms`, `shared hit=2` — rẻ tới mức
     nằm dưới ngưỡng nhiễu đo được của chính phép đo.
   - Lý do gốc khiến nó "đáng cache" đã biến mất, nên đem cache vào lúc này chỉ
     còn lại **cái giá**: mỗi đường ghi tới `RolePermissions` phải nhớ bump
     (`UpdatePermissionMatrixCommand`, `UpdateResourcePermissionMatrixCommand`,
     `CoreSeeder`, và bất kỳ đường ghi nào thêm sau này), đổi lấy ~0,04 ms.
   - Với dữ liệu phân quyền, cái giá đó **không** trung tính: quên một đường ghi
     = quyền đã thu hồi vẫn còn hiệu lực (§4.2). §7.3 đã kiểm chứng hành vi hiện
     tại là thu hồi/cấp lại có tác dụng **ngay lập tức** — chính xác thứ mà cache
     sẽ phá.

   Đây là kết quả đúng theo §6.4 ("nhiều khả năng dừng được ở bước 4"), **không
   phải việc bỏ dở**. Mở lại chỉ khi có số đo mới chứng minh permission check
   thành nút thắt thật (vd số tổ hợp role × resource key tăng vài bậc, hoặc
   `EXPLAIN` cho thấy chi phí đã khác hẳn §7.2).

### 6.3 Findings trong code hiện tại

Nhóm A — sửa trước, rủi ro gần bằng 0, không cần hạ tầng mới:

| Mã | Vi phạm | Vị trí | Quy tắc |
| --- | --- | --- | --- |
| **A0** | Permission check bắn **2 query DB mỗi request** có `[RequirePermission]` (`Roles` + `AnyAsync` trên `RolePermissions`); dữ liệu tí hon, gần như bất biến | [`RequirePermissionFilter.cs:39-50`](../../../../src/BE/Core/PlatformManager.Core.Infrastructure/Permissions/RequirePermissionFilter.cs) | §4.3 — ứng viên cache |
| **A1** | **Không có một `AsNoTracking()` nào trong toàn bộ `src/BE`** — mọi query đọc đều track | toàn repository | Q1 |
| **A2** | Index `(CriteriaId, DateCreate)` không phục vụ được query lọc **chỉ theo `DateCreate`** → seq scan. `RolePermission` không có index trên `ResourceKey` | [`CriteriaAssessmentConfiguration.cs:53`](../../../../src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Infrastructure/Persistence/Configurations/CriteriaAssessmentConfiguration.cs) vs [`CriteriaAssessmentRepository.cs:73`](../../../../src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Infrastructure/Persistence/Repositories/CriteriaAssessmentRepository.cs) | Q2 |
| **A3** | `GetAllDistinctAssessmentDatesAsync` kéo **toàn bộ** cột `DateCreate` về app rồi mới `Distinct()` trong C#; được gọi 2 lần/lần load dashboard | [`CriteriaAssessmentRepository.cs:110`](../../../../src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Infrastructure/Persistence/Repositories/CriteriaAssessmentRepository.cs) | Q3 |
| **A4** | N+1: `GetListAsync` lặp `ToDtoAsync` → `GetRolesAsync` **1 query/user** (20 user/trang = 21 query) | [`UserAdminService.cs:29-33`](../../../../src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs) | Q4 |
| **A5** | Dashboard 1 lần load ≈ **10 round-trip**: `GetRecordsInRangeAsync` gọi 2 lần (kỳ hiện tại + kỳ trước), mỗi lần 5 round-trip con | [`AggregationService.cs:26-45`](../../../../src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Application/Dashboard/AggregationService.cs) | §3 |
| **A6** | `GetPeriodsAsync` gọi `PeriodAggregateCalculator.Compute()` riêng cho **từng tuần và từng tháng** — 52 + 12 = **64 lần quét lại** cùng một list | [`AggregationService.cs:86-106`](../../../../src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Application/Dashboard/AggregationService.cs) | §3 |

Nhóm B — chấp nhận được ở quy mô hiện tại, **theo dõi**, chưa sửa:

| Mã | Nội dung | Vị trí | Điều kiện thành finding thật |
| --- | --- | --- | --- |
| **B1** | Phân trang + search chạy trong bộ nhớ (load hết rồi `Skip/Take`) | [`GetCriteriaListQuery.cs:57-60`](../../../../src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Application/Criteria/GetCriteriaListQuery.cs) | Khi số `Criteria` vượt ~vài trăm, hoặc grid chuyển sang liệt kê record lịch sử nhiều năm |
| **B2** | Menu load 4 query mỗi lần dựng sidebar | [`SysMenuRoleRepository.cs:29-45`](../../../../src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Repositories/SysMenuRoleRepository.cs) | Sau khi A0 xong — cùng bản chất (dữ liệu nhỏ, ghi hiếm), gộp chung đợt cache nếu đo thấy đáng |
| **B3** | FE không cache `GET /meta/menu`, gọi lại mỗi lần khởi tạo sidebar | `src/FE/src/app/shared/services/menu.service.ts` | Thuộc `frontend-expert` — xem [fe/13-performance.md](../fe/13-performance.md) |

**Trạng thái sau đợt 1 (2026-08-18) — người dùng chốt phạm vi CHỈ Core, cố ý
không đụng `Modules.DtiWeekly`:**

| Mã | Trạng thái | Ghi chú |
| --- | --- | --- |
| A0 | ✅ Xong (gộp 2 → 1 query) | KHÔNG cache — xem §6.2 mục 5 |
| A1 | ✅ Xong **phần Core** | Query đọc thuần trong `Core.*` đã có `AsNoTracking`; 2 chỗ **cố ý giữ tracking** (`RolePermissionRepository.ReplaceAllAsync`, `SysMenuRoleRepository.ReplaceAllAsync` — entity lấy ra để `RemoveRange` rồi `SaveChanges`). Repository của `Modules.*` **chưa** làm |

> **`Modules.*` chưa có `AsNoTracking()` — HOÃN CÓ CHỦ ĐÍCH, không phải sót.**
> Người dùng đang tập trung phát triển **core**, `src/BE/Modules/**` nằm ngoài
> phạm vi cho tới khi core ổn định. Đừng báo lại đây như finding mới ở mỗi lượt
> review; cũng đừng "tiện tay" sửa hàng loạt — Q1 có ngoại lệ thật (entity lấy
> ra để sửa rồi `SaveChanges` thì KHÔNG được thêm `AsNoTracking`), nên việc này
> phải đọc kỹ từng call-site chứ không sed toàn bộ. Ghi nhận 2026-08-20, chuyển
> vào đây 2026-08-21 khi bỏ thư mục `audit/`.
| A2 | ✅ Xong **phần Core** (`RolePermission`) | Migration `20260818101335_AddRolePermissionResourceKeyIndex`, script `doc/ERD/migrations/0005_*.sql`. Phần `CriteriaAssessment.DateCreate` thuộc Modules — **chưa** làm |
| A3 | ⛔ Ngoài phạm vi đợt này (Modules) | |
| A4 | ✅ Xong (22 → 3 query) | |
| A5 | ⛔ Ngoài phạm vi đợt này (Modules) | Đo lại khi có dữ liệu `CriteriaAssessments` thật — lúc đo bảng này **rỗng**, số đo hiện tại không phản ánh A5/A6 |
| A6 | ⛔ Ngoài phạm vi đợt này (Modules) | |
| B1 | ⛔ Ngoài phạm vi đợt này (Modules) | |
| B2 | ✅ Xong (4 → 1 query) | Nâng lên sửa luôn thay vì "theo dõi": gộp round-trip không cần hạ tầng mới, và điều kiện "gộp chung đợt cache" ở cột bên đã không còn vì cache bị bác bỏ |
| B3 | ✅ Xong (2026-08-19, `frontend-expert`) | Cache theo **khoá phiên** trong `MenuService` (`src/FE/src/app/shared/services/menu.service.ts`); invalidate ở `AuthService.login/logout` + sau khi lưu ma trận phân quyền màn hình. Số đo S1 3→2, S2 5→1 (§7.1) |

### 6.4 Thứ tự thực hiện đã chốt

| Bước | Việc | Điều kiện xong |
| --- | --- | --- |
| **0** | Bật EF command logging (dev), đếm query/request cho dashboard + danh mục + user list; ghi số vào §7 | Có bảng số liệu "trước" |
| **1** | A1 (`AsNoTracking`) + A2 (index) | `dotnet test` xanh, `EXPLAIN` xác nhận hết seq scan |
| **2** | A3 (`Distinct` xuống SQL) + A4 (gộp query role) | Số query/request giảm đo được |
| **3** | A5 (gom round-trip) + A6 (tính 1 lượt dùng lại) | Kết quả dashboard **không đổi** (đối chiếu trước/sau) |
| **4** | Đo lại, ghi số "sau" vào §7 | Có bảng so sánh |
| **5** | Cache permission matrix (in-memory + invalidate tường minh) | Có test xác nhận thu hồi quyền có hiệu lực **ngay** |
| **6** | Cache dashboard aggregate — **chỉ khi** bước 4 vẫn cho thấy chậm | Có số đo biện minh, có danh sách đầy đủ đường ghi cần invalidate |

**Bước 3 là ranh giới rủi ro:** A5/A6 sửa đường tính toán ra con số hiển thị
cho người dùng. Bắt buộc đối chiếu output trước/sau trên cùng dữ liệu, không
chỉ "chạy được là xong".

Nhiều khả năng dừng được ở bước 4 — nếu vậy, bước 5–6 **không** trở thành
"nợ chưa làm", mà là quyết định đúng: không thêm cache khi không cần.

> **Đã xảy ra đúng như vậy (2026-08-18, phạm vi Core).** Bước 0–4 chạy hết; bước
> 5 **đóng lại với kết luận KHÔNG cache** (lý do + số đo ở §6.2 mục 5). Bước 6
> chưa xét vì A5/A6 nằm ngoài phạm vi đợt này và bảng `CriteriaAssessments`
> đang rỗng nên chưa có số đo có ý nghĩa cho dashboard.

## 7. Số đo

### 7.1 Đợt 1 — 2026-08-18, phạm vi Core (A0/A1/A2/A4/B2)

**Cách đo.** EF command logging bật CHỈ ở Development
([`Core.Infrastructure/DependencyInjection.cs`](../../../../src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs)
— `LogTo` lọc riêng kênh `DbLoggerCategory.Database.Command` + `EnableSensitiveDataLogging`,
bọc trong `IHostEnvironment.IsDevelopment()`). Đếm số dòng
`RelationalEventId.CommandExecuted[20101]` phát sinh giữa 2 mốc quanh 1 request
(bỏ request warm-up đầu tiên). Đăng nhập bằng cookie session thật, gọi qua
`https://localhost:7168`.

**Môi trường.** Database ĐO RIÊNG (`platformmanager_perf`) dựng từ chính 2 file
`doc/ERD/migrations/0003_*.sql` + `0004_*.sql`, KHÔNG dùng DB làm việc và KHÔNG
ghi gì vào database `postgres`. Postgres local, app 1 process, client tuần tự 1
luồng. DB đo là **dùng một lần rồi xoá** — dựng lại y hệt bằng đúng 2 file .sql
trên khi cần đo lại, đừng giữ nó làm môi trường lâu dài (dữ liệu user trong đó
là giả, `PasswordHash` không đăng nhập được).

**Cỡ dữ liệu lúc đo** (bắt buộc ghi — số query không kèm cỡ dữ liệu thì không so
sánh lại được): `AspNetUsers` = 51 (grid lấy `pageSize=20`), `AspNetRoles` = 3,
`AspNetUserRoles` = 52, `SysMenus` = 6, `SysMenuRoles` = 5, `RolePermissions` = 6,
`Criteria` = 62, `CriteriaAssessments` = **0**.

| Ngày | Endpoint | Query/request trước | Query/request sau | Ghi chú |
| --- | --- | --- | --- | --- |
| 2026-08-18 | `GET /api/users?page=1&pageSize=20` | **22** | **3** | A4. 22 = 1 `COUNT` + 1 trang + **20 query role (N+1)**. 3 = `COUNT` + trang + **1** query role cho cả trang |
| 2026-08-18 | `GET /api/meta/menu` | **5** | **2** | B2. 5 = 1 (`SysMenuRepository`) + **4** (`GetVisibleSysMenuIdsForRolesAsync`). 2 = 1 + **1** |
| 2026-08-18 | `GET /api/criteria` (có `[RequirePermission]`) | **5** | **4** | A0: phần permission **2 → 1**. 3 query còn lại thuộc `Modules.DtiWeekly`, **cố ý không đụng** |
| 2026-08-18 | `GET /api/dashboard` | 4 | 4 | Ngoài phạm vi (Modules) — chỉ đo để đối chiếu về sau. Không đổi là ĐÚNG |
| 2026-08-18 | `GET /api/dashboard/periods` | 3 | 3 | Ngoài phạm vi (Modules). Số thấp hơn ước tính A5/A6 vì `CriteriaAssessments` rỗng — đo lại khi có dữ liệu thật trước khi động vào A5/A6 |
| 2026-08-19 | `GET /api/meta/menu` — đếm **ở FE**, kịch bản **S1** (đơn vị: **request/kịch bản**, KHÔNG phải query/request) | **3** | **2** | B3 (cache FE). Xem định nghĩa S1 + cách đo ngay dưới bảng. 1 trong 2 request "sau" là BẮT BUỘC (đổi sang tài khoản khác → phải tải menu mới) |
| 2026-08-19 | `GET /api/meta/menu` — đếm **ở FE**, kịch bản **S2** (đơn vị: **request/kịch bản**) | **5** | **1** | B3. S2 cho thấy điểm mấu chốt: số request không còn tỉ lệ thuận với số lần app-shell dựng lại |

**Latency p95 — đo được nhưng KHÔNG dùng để kết luận.** 30 request tuần tự, 1
client, localhost, không phải load test: `/api/users` 50,9 → 35,1 ms;
`/api/meta/menu` 49,7 → 43,5 ms; `/api/criteria` 57,1 → 19,2 ms; `/api/dashboard`
41,2 → 33,9 ms; `/api/dashboard/periods` 34,1 → **54,8** ms. Dòng cuối *xấu đi*
dù **không có một dòng code nào trên đường đó bị sửa** — đủ để kết luận cột
latency ở cỡ dữ liệu/thiết bị này bị nhiễu chi phối, chỉ dùng **số query** làm
căn cứ. Ghi lại nguyên vẹn để lần sau không ai tưởng đã có baseline latency đáng
tin.

**Số đo FE — B3 (2026-08-19, `frontend-expert`).** Đơn vị là **số request
`GET /api/meta/menu` trên một kịch bản điều hướng xác định**, không phải
query/request và không phải milli-giây — thứ cache FE thay đổi là *số lần gọi*,
nên đó là con số phải đo (đúng `src/BE/.claude/rules/performance.md` §Đo).
Latency **không đo**: cùng lý do đã ghi ở đoạn trên, và ở FE nó còn phụ thuộc
mạng/BE nên không nói lên gì về cache.

*Cách đo.* Chạy kịch bản trong harness test thật của FE (`ng test`, Karma +
`HttpTestingController`), spec
[`menu-cache-scenario.spec.ts`](../../../../src/FE/src/app/shared/services/menu-cache-scenario.spec.ts)
— dùng chính component `Sidebar` và `AuthService` thật, vì cơ chế sinh request
nằm ở **vòng đời component**: app-shell trong `app.html` bọc
`@if (showShell())`, nên mỗi lần vào/ra route `noShell` (`login`,
`doi-mat-khau`) là một lần `Sidebar` bị huỷ rồi dựng lại → 1 lần `getMenu()`.
Số **"trước"** lấy bằng cách tạm khôi phục hành vi tiền-cache trong
`MenuService.getMenu()`, chạy lại đúng kịch bản đó, rồi revert (cùng cách đã
dùng để kiểm chứng test thật sự đỏ). Không cần BE/DB thật vì đại lượng đo là số
request FE phát ra, không phải chi phí xử lý phía server. Spec này ở lại làm
chốt chặn hồi quy: bỏ cache đi thì con số đổi và test đỏ.

*Kịch bản.* **S1 — 1 phiên làm việc điển hình:** mở app (đã đăng nhập, user A) →
dashboard *(shell dựng lần 1)* → Danh mục DTI → dashboard *(vẫn trong shell,
không dựng lại)* → "Đổi mật khẩu" (`noShell`, shell bị huỷ) → dashboard *(shell
dựng lần 2)* → logout → đăng nhập **tài khoản khác** (user B) → dashboard *(shell
dựng lần 3)*. Trước: 1+0+1+1 = **3**. Sau: 1+0+**0**+1 = **2**.
**S2 — cùng 1 phiên, app-shell dựng lại 5 lần** (ra/vào route `noShell` 5 lượt,
không đổi tài khoản): trước **5**, sau **1**.

*Cỡ dữ liệu lúc đo:* menu 2 item gốc (kích thước payload không ảnh hưởng đại
lượng đang đo — số lần gọi).

### 7.2 `EXPLAIN ANALYZE` — index `IX_RolePermissions_ResourceKey_RoleId` (A2)

Query thật của `RequirePermissionFilter` sau khi gộp (`EXISTS` + join `AspNetRoles`):

- **Ở cỡ hiện tại (6 dòng `RolePermissions`)**: `Seq Scan`, `shared hit=2`,
  Execution Time **0,038 ms**. Planner **không** dùng index — và đó là lựa chọn
  ĐÚNG: cả bảng nằm gọn trong 1 page, đọc index còn tốn hơn. Đây là lý do phải
  chạy `EXPLAIN` thật thay vì suy từ việc "đã khai `HasIndex` rồi" (§5).
- **Ở cỡ 30.000 dòng** (bơm dữ liệu giả vào DB đo để kiểm chứng): `Bitmap Index
  Scan on IX_RolePermissions_ResourceKey_RoleId`, Execution Time 0,072 ms —
  index vào việc đúng như thiết kế khi số tổ hợp (role × resource key) tăng.

Kết luận: index **không** cải thiện gì ở cỡ hiện tại, nhưng cũng gần như không
tốn gì (bảng ghi rất hiếm) và là thứ duy nhất chặn seq scan khi phân quyền chi
tiết dần lên. Giữ.

### 7.3 Kiểm chứng hành vi phân quyền (bắt buộc — A0/B2 là code bảo mật)

> **Cập nhật 2026-08-19 — bảng này giờ là TEST TỰ ĐỘNG, không còn là quy trình
> tay.** Audit 2026-08-18 (finding #1) chỉ ra đúng điểm yếu: kiểm chứng thủ công
> chạy một lần rồi mất, lần sửa sau không có gì chặn regression. Toàn bộ các dòng
> dưới đây nay được phủ bởi `Tests/PlatformManager.Core.UnitTests` (luồng quyết
> định) + `Tests/PlatformManager.Core.IntegrationTests` (ngữ nghĩa truy vấn trên
> Postgres thật). Xem [04-testing-strategy.md](04-testing-strategy.md)
> §"Kiểm thử phân quyền — chia đôi có chủ đích". Giữ bảng lại làm bản ghi lịch sử
> của lần đo, đừng chạy tay lại.

Chạy thật trên DB đo, KHÔNG chỉ dựa vào build xanh:

| Tình huống | Kỳ vọng | Kết quả |
| --- | --- | --- |
| Menu của SuperAdmin | có đủ `quan-tri`/`sys-user`/`phan-quyen` | ✅ đúng |
| Menu của role `User` | **không** có `quan-tri`/`sys-user`/`phan-quyen`, vẫn có `dashboard`/`danh-muc`/`danh-muc-dti` (menu không gán role = mở cho mọi user) | ✅ đúng |
| `GET /api/criteria` — role có key | 200 | ✅ 200 |
| `GET /api/criteria` — **thu hồi** `criteria.manage` của role `User` | 403 ngay lập tức | ✅ 403 |
| `GET /api/criteria` — SuperAdmin (qua role Admin) khi role `User` bị thu hồi | vẫn 200 | ✅ 200 |
| `GET /api/criteria` — cấp lại quyền | 200 **ngay**, không chờ TTL | ✅ 200 |
| `GET /api/criteria` — chưa đăng nhập | 401 | ✅ 401 |

## 8. Chưa cần (Nhóm B)

- **Redis / `IDistributedCache`** — 1 process duy nhất, xem §4.4.
- **Response caching / `OutputCache` ở tầng HTTP** — mọi endpoint đều sau
  auth và phần lớn phụ thuộc role; lợi ích thấp, rủi ro rò rỉ dữ liệu giữa
  user cao nếu cấu hình sai `VaryBy`.
- **Read replica / CQRS tách DB đọc-ghi** — quy mô còn cách rất xa ngưỡng
  này; ghi ở đây chỉ để đóng câu hỏi "sao không làm luôn".
- **Compiled model của EF Core** (`UseCompiledModel`) — chỉ đáng khi startup
  time thành vấn đề thật (model rất lớn); hiện chưa.
