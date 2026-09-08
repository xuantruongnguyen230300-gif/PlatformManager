---
kind: luat
scope: core
verified: 2026-09-06
---

# 1. Core thật sự của senior lâu năm thường thiết kế

## Nguyên tắc chọn lọc — Nhóm A vs Nhóm B

Trước khi liệt kê, 1 nguyên tắc phải giữ xuyên suốt: **core không phải là "thêm càng nhiều abstraction càng chuyên nghiệp"**. Mỗi thành phần dưới đây giải quyết 1 nỗi đau *thật* — chỉ xây khi hệ thống đã/sắp chạm đúng nỗi đau đó, không xây trước "phòng khi cần" (premature abstraction là nguồn nợ kỹ thuật lớn nhất ở các core tự thiết kế).

## Danh sách thành phần core (đã xác nhận qua VNR + kiến thức chung ngành)

| # | Thành phần | Nỗi đau nó giải quyết | Mức ưu tiên |
|---|---|---|---|
| 1 | **BaseEntity** (`Id` sinh ở chính `BaseEntity` — `init` + UUID v7 qua `EntityId.New()`, factory KHÔNG gán lại; `CreatedAt`/`UpdatedAt`/`CreatedBy`/`UpdatedBy`, `IsDeleted`) + soft-delete qua **global query filter khai một chỗ ở `OnModelCreating`** | Quên filter `IsDeleted` ở 1 query = lộ dữ liệu đã xoá; khai filter lẻ từng entity thì entity mới quên khai là mất bảo vệ, không ai báo | Bắt buộc, ngày đầu — hợp đồng đầy đủ ở [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) |
| 2 | **Generic Repository/UnitOfWork** | Không viết lại CRUD cơ bản cho mỗi entity | Bắt buộc, ngày đầu |
| 3 | **Factory method + private setter** cho entity nghiệp vụ | Invariant bị vỡ do gán property tuỳ tiện | Bắt buộc, ngày đầu |
| 4 | **Value Object** cho field có luật (tiền tệ, %, email, SĐT...) | Dữ liệu sai lọt qua vì dùng `decimal`/`string` trơ | Nên có sớm |
| 5 | **Error-as-value (`Result<T>`)** cho lỗi nghiệp vụ mong đợi + **exception middleware toàn cục** cho lỗi thật bất ngờ (trả `ErrorCode`+`TraceId`, không lộ stack trace) | Exception-driven control flow rối, hoặc lộ chi tiết nội bộ ra client | Bắt buộc, ngày đầu |
| 6 | **Envelope response nhất quán** cho MỌI endpoint kể cả list — danh sách trường **không** liệt ở đây, đọc `doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response (bản liệt kê cũ ở ô này đã thiếu `fieldErrors` từ 2026-09-03; gỡ 2026-09-04) | FE phải viết 2 nhánh parse khác nhau | Bắt buộc, ngày đầu |
| 7 | **Auth/Identity + Permission framework** (context "current user", resource-action key) | Xem [02-identity-auth.md](02-identity-auth.md) | Bắt buộc, ngày đầu |
| 8 | **Caching abstraction** (distributed + local, tự fallback êm khi cache down) | Redis down làm sập app; không tra được "đang cache gì" | ⚠️ Đã có bằng chứng (2026-08-18) — xem [11-performance-caching.md](11-performance-caching.md), phạm vi hẹp + đúng thứ tự |
| 9 | **Logging/Audit abstraction** (structured, tách log kỹ thuật vs audit nghiệp vụ) | Log dạng string không tra cứu được | Bắt buộc, ngày đầu |
| 10 | **Config/Options abstraction** (`IOptions<T>` typed, fail-fast lúc khởi động) | Cấu hình sai chỉ lộ ra lúc runtime gọi tới, không phải lúc start | Nên có sớm |
| 11 | **Generic CRUD/Grid/Form engine** | Xem [03-metadata-driven-design.md](03-metadata-driven-design.md) — nguồn cột nên từ code, không phải DB tự do | Khi có ≥5-10 màn CRUD giống nhau |
| 12 | **Widget/Dashboard rendering engine** | Dựng lại UI biểu đồ/KPI cho mỗi dashboard mới | Khi có ≥2-3 dashboard |
| 13 | **Notification abstraction** (email/SMS/push — đổi kênh không đổi code gọi) | Đổi nhà cung cấp email phải sửa code khắp nơi | Khi có ≥2 kênh thông báo |
| 14 | **File storage abstraction** (local/S3/Blob — swap được) | Xem [14-file-storage.md](14-file-storage.md) — upload, export, file mẫu, dọn file | Khi cần production-ready |
| 15 | **Import/Export engine** (parse → validate từng dòng → map DTO → upsert) | Xem [15-import-export.md](15-import-export.md) — định dạng, ranh giới bộ lọc, hình dạng endpoint | **Ngưỡng đã đạt 2026-08-29** — chốt dựng thẳng ở Core |
| 16 | **Outbound HTTP integration engine** (gọi API bên thứ 3, có resilience + **anti-SSRF guard**) | Retry tay không nhất quán; endpoint cấu hình DB có thể trỏ vào mạng nội bộ | Khi gọi API 3rd-party cấu hình được qua UI |
| 17 | **Background job/scheduler abstraction** | Task nền viết tay dễ mất khi restart, không có retry | Khi có tác vụ chạy nền/định kỳ |
| 18 | **i18n/localization framework** | Chỉ cần nếu hệ thống thật sự đa ngôn ngữ — **yêu cầu đó đã xuất hiện (2026-08-27: vi + en)** | ⚠️ Không còn "tuỳ yêu cầu". **Hướng A giữ tên nhưng đổi nghĩa 2026-09-03**: BE vẫn không dựng hạ tầng dịch, nhưng KHÔNG còn "chỉ giữ mã lỗi ổn định" — có việc tiên quyết phải làm. File chủ: `be/16-i18n-va-ma-loi.md` |
| 19 | **Rate limiting** (`Microsoft.AspNetCore.RateLimiting`, có sẵn từ .NET 7) | Không có gì chặn brute-force login, hoặc 1 user spam endpoint nặng (import file) làm nghẽn hệ thống cho user khác | Bắt buộc trước khi có user thật ngoài đội dev |
| 20 | **CI pipeline** (build+test+ArchTest tự động trên mọi PR, không dựa vào con người nhớ chạy tay) | `dotnet test` chỉ chạy khi ai đó nhớ chạy — quy tắc kiến trúc/ArchTest có tồn tại cũng vô nghĩa nếu không ai chặn được PR vi phạm | Bắt buộc trước khi có ≥2 người cùng commit vào 1 nhánh |

## Áp dụng vào PlatformManager

> **Chuyển giai đoạn (2026-08-17):** PlatformManager đã qua giai đoạn demo,
> bắt đầu giai đoạn phát triển product thật (đối chiếu thêm tiêu chuẩn
> ngành ngoài VNR — [Clean Architecture template Jason Taylor](https://github.com/jasontaylordev/cleanarchitecture),
> [12-Factor App](https://12factor.net/), [OWASP Top 10:2025](https://owasp.org/Top10/2025/A01_2025-Broken_Access_Control/)).
> Nhiều mục dưới đây trước ghi "chưa cần ở quy mô demo" — **calibration đó
> hết hiệu lực từ giờ**, không phải vì quy mô code đổi, mà vì bản chất rủi
> ro đổi (có user thật/dữ liệu thật để mất, không còn là sandbox riêng của
> dev). Mục nào **thật sự vẫn nên hoãn** (i18n, engine generic) — lý do hoãn
> được ghi lại là lý do dựa trên **bằng chứng cụ thể** (chưa có traffic/chưa
> có yêu cầu nghiệp vụ), không dựa trên nhãn "demo" nữa — phân biệt 2 loại
> lý do này quan trọng vì loại đầu hết hạn theo giai đoạn, loại sau không tự
> hết hạn.
>
> **Cập nhật 2026-08-27:** #18 i18n cũng đã rời khỏi danh sách "vẫn nên hoãn"
> — người dùng xác nhận cần vi + en ngay. Câu "(i18n, engine generic)" ở đoạn
> trên **chỉ còn đúng với engine generic**. Đây là lần thứ hai cơ chế "lý do
> hoãn dựa trên bằng chứng thì chấm dứt bằng bằng chứng" vận hành đúng như
> thiết kế; chi tiết ở §Áp dụng #18 bên dưới.
>
> **Cập nhật 2026-08-18:** #8 caching đã rời khỏi danh sách "vẫn nên hoãn" —
> bằng chứng cụ thể đã xuất hiện khi rà soát code, xem
> [11-performance-caching.md](11-performance-caching.md). Đây đúng là cách
> nguyên tắc trên vận hành: lý do hoãn dựa trên bằng chứng thì cũng chấm dứt
> bằng bằng chứng, không phải bằng việc "tới giai đoạn".

Đã có, giữ nguyên: #1, #3, #5, #6, #9 (mức tối giản) qua `BaseEntity` +
`IApiResult<T>` + `GlobalExceptionHandler` (xem
`doc/huong_dan/quy-uoc/be-api-controller.md`, đã thay `ApiResponse<T>`/
`ExceptionMiddleware` cũ).

**#2 chỉ đúng một nửa:** `IUnitOfWork`/`UnitOfWork` có thật, nhưng **không có generic
repository** — mỗi entity Core có repository riêng (`SysMenuRepository`,
`SysMenuRoleRepository`, `RolePermissionRepository`). Đó là chủ đích, không phải thiếu
sót; đừng đọc ô #2 ở bảng trên thành "đã có `IRepository<T>`".

> **🔄 LẬT 2026-09-06.** Bản trước dẫn `AssessmentUpsertService`/`AggregationService` làm
> bằng chứng cho cụm này, và gộp #2 vào danh sách "đã có" không kèm dè dặt. Hai class đó
> **không còn trong `src/BE`** — chúng thuộc module DtiWeekly đã gỡ 2026-08-29; nay chỉ
> sót tên trong một docstring
> (`src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IDateTimeProvider.cs:4`).
> Kiểm bằng lệnh, đừng tin bảng (§6):
>
> ```bash
> grep -rn "AssessmentUpsertService\|AggregationService" src/BE --include=*.cs | grep -v /obj/
> grep -rn "interface IRepository" src/BE --include=*.cs | grep -v /obj/
> ```
>
> PASS hôm nay: lệnh đầu ra đúng **một** dòng và dòng đó là comment; lệnh sau ra **rỗng**.

**#7 Auth/Permission — cần tách rõ 3 nửa, dễ nhầm "đã xong":** nửa
**authentication** (đăng nhập là ai) đã triển khai qua ASP.NET Core Identity (2026-08-16)
(xem `doc/huong_dan/quy-uoc/README.md` §Stack, `doc/cau-truc-database.md` §4.1), sống ở
`PlatformManager.Core.Infrastructure`.

Nửa **authorization theo hành động** phải tách làm hai câu hỏi rời nhau, vì chúng có câu
trả lời **khác nhau** (đối chiếu 2026-09-05):

| Câu hỏi | Trạng thái |
|---|---|
| Cơ chế đã có chưa? | ✅ `RequirePermissionFilter` đăng ký toàn cục (`src/BE/PlatformManager.Api/Program.cs:93`), seam `ICoreResourceKeySource` + `CoreSeeder` đầy đủ |
| Có endpoint nào **đang dùng** nó chưa? | ❌ **không endpoint sản phẩm nào** mang `[RequirePermission]` |

Kiểm bằng lệnh, đừng tin bảng (§6):

```bash
grep -rn "\[RequirePermission" src/BE --include=*.cs | grep -v Tests | grep -v "///" | grep -v "//"
```

PASS cho tình trạng hôm nay: **rỗng**. Mọi kết quả `grep` thô đều là docstring, comment,
hoặc chính filter — không phải chỗ gắn lên action.

**Hệ quả phải nói thẳng:** hôm nay `[Authorize]` là lớp chặn thật sự duy nhất trên các
controller sản phẩm (`AuthController`, `MetaController`, `PermissionsController`,
`UsersController`). Ai đăng nhập được thì gọi được, không phân biệt hành động. Đây
**không** phải lỗi của cơ chế — nó là lỗ hổng của việc **áp dụng** cơ chế, và nó sẽ không
tự lộ ra: filter chạy đúng, test xanh, review đọc code filter thấy hoàn chỉnh.

Khoá `import.manage` còn trong `AppResourceKeys` là **di sản** của module DtiWeekly đã gỡ
2026-08-29; nơi duy nhất dùng nó là controller thăm dò của integration test. Nó được giữ
lại có chủ đích — `RolePermissions` đã seed đang có dòng mang key này, xoá key khỏi danh
mục biến chúng thành dòng mồ côi.

> **🔄 LẬT 2026-09-05.** Bản trước ghi *"`[RequirePermission]` gắn trên controller nghiệp
> vụ"* (đối chiếu 2026-08-27) — sai, và sai theo đúng chiều mà chính đoạn ghi chú bên dưới
> cảnh báo, chỉ ngược hướng. Bản 2026-08-27 sửa câu "CHƯA làm" thành "đã làm" vì cơ chế
> quả thật đã có; nhưng nó sửa **quá tay**, khẳng định luôn phần *áp dụng* mà không ai đo.
> Kết quả là tài liệu tuyên bố Broken Access Control đã đóng, trong khi trên dây không một
> endpoint nào khai quyền. Bài học: "cơ chế đã có" và "cơ chế đang được dùng" là **hai**
> khẳng định, và chỉ khẳng định thứ hai mới trả lời được câu hỏi bảo mật.
>
> *Ghi chú lịch sử 2026-08-27 (giữ nguyên vì lý do của nó vẫn đúng):* đoạn này từng ghi
> "CHƯA … chỉ `[Authorize]` trần", khiến mọi lượt audit chấm MISSING mức nghiêm trọng nhất
> cho một cơ chế đã làm xong, rồi người đọc báo cáo đi sửa thứ không hỏng.
>
> Phần **chưa** xong về dữ liệu: production chưa có đường cấp `RolePermission`
> — xem [13-core-data-migration.md](13-core-data-migration.md) §Seed.

> ### 🚧 #7b — Seed không cấp quyền theo TỪNG VAI được (mở 2026-09-06)
>
> `CoreSeeder.SeedRolePermissionsAsync` lặp `new[] { Roles.Admin, Roles.User }` và cấp
> **mọi** key cho **cả hai**. Đó là chủ đích lúc vá lần đầu — comment trong file nói rõ
> mục tiêu là *giữ nguyên hành vi trước khi vá (mọi user thao tác được)*.
>
> **Hệ quả nay đã thành vấn đề thật:** một key mới sinh ra là **tự động** cấp cho cả vai
> `User`. Nên một quyền được khai riêng để giới hạn ai được ghi sẽ **không giới hạn ai
> cả** — nó tồn tại trên giấy. Đây đúng là ca của quyền ghi DTI (`dti.manage`), chốt
> 2026-09-06: *chỉ cấp cho `Admin`, vai `User` phải cấp tay*.
>
> Chỗ chặn nằm ở seam chứ không ở seeder: `ICoreResourceKeySource` chỉ mang `Key` +
> `DisplayName`, không mang thông tin "vai nào được cấp mặc định". Hình dạng đề xuất —
> mở rộng `ResourceKeyDefinition` thêm danh sách vai, **mặc định `[Admin, User]`** để
> mọi key hiện có không đổi hành vi, key mới thì khai tường minh.
>
> Nghiệm thu: sau `--seed`, key DTI có dòng cho `Admin` và **không** có dòng cho `User`.

**#10 Config/Options fail-fast — nâng từ "chưa cần" lên "nên có sớm":** rule
cụ thể (`ValidateDataAnnotations().ValidateOnStart()`) thêm ở
`doc/huong_dan/quy-uoc/be-architecture.md` §"Cấu hình — fail-fast validation".

**#13 Notification, #14 File storage abstraction** — đã quyết định kiến trúc
(`INotificationSender` seam cho email, `IImportFileStorage` cho file tạm) khi
thiết kế lại Import CSV/Excel — xem `doc/huong_dan/quy-uoc/be-cqrs-handler.md`
§"Command chạy lâu → job nền" và `doc/huong_dan/quy-uoc/be-architecture.md`
§"Notification". Đây là ví dụ cho nguyên tắc ở trên: **quyết định** đã có,
chỉ **implement** chưa xong — khác hẳn "chưa cần" thật sự.

**#17 Background job/scheduler — ✅ CÓ THẬT (đối chiếu 2026-08-28, mở từng file
đếm dòng).** Không còn thuộc nhóm "quyết định xong, implement chưa xong" ở đoạn
trên; seam chạy thật từ khai báo tới nơi gọi:

| Mắt xích | Vị trí |
| --- | --- |
| Seam (Application, không biết Hangfire) | [`IBackgroundJobScheduler.cs:19`](../../../../src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs) |
| Hiện thực Hangfire (Infrastructure) | [`HangfireBackgroundJobScheduler.cs:15`](../../../../src/BE/Core/PlatformManager.Core.Infrastructure/BackgroundJobs/HangfireBackgroundJobScheduler.cs) |
| Nối seam → hiện thực | [`BackgroundJobInfrastructureExtensions.cs:19`](../../../../src/BE/Core/PlatformManager.Core.Infrastructure/BackgroundJobInfrastructureExtensions.cs) |
| Composition root gọi | [`Program.cs:175`](../../../../src/BE/PlatformManager.Api/Program.cs) — `AddBackgroundJobInfrastructure()` |
| Nơi dùng thật (enqueue import) | **chưa có** — chỗ dùng duy nhất nằm ở module DtiWeekly, đã xoá 2026-08-29 |
| Gate: Application cấm reference Hangfire | [`LayerDependencyTests.cs:35`](../../../../src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs) |
| Gate: job chạy thật, Pending → Succeeded | **đã gỡ 2026-08-29** cùng endpoint `/api/import` mà nó gọi — dựng lại cùng Import ở Core |

Phần **chưa** có là lịch biểu định kỳ (`RecurringJob`) — hiện chỉ dùng
fire-and-forget. Đó là tính năng chưa cần, không phải seam còn thiếu.

> ### 🚧 #17b — Job nền KHÔNG mang danh tính người khởi tạo (mở 2026-09-06)
>
> **Đây là seam còn thiếu thật, khác với `RecurringJob` ở trên.** Mọi thứ job nền ghi vào
> DB đều mang `UpdatedBy = "system"`, kể cả khi nó chạy vì một người bấm nút.
>
> Chuỗi nhân quả, đã đối chiếu source 2026-09-06:
>
> | Mắt xích | Điều xảy ra |
> | --- | --- |
> | `ICurrentUser` (seam, `Core.Application`) | đúng chỗ — `AuditInterceptor` chỉ hỏi seam này, **không** đọc `HttpContext` |
> | `HttpContextCurrentUser` | **bản cài đặt DUY NHẤT**, đăng ký ở `Program.cs:118` |
> | Trong worker Hangfire | không có `HttpContext` ⇒ seam trả "không ai" |
> | `AuditInterceptor` | rơi vào nhánh `?? "system"` |
>
> **Module KHÔNG tự vá được**: interceptor ghi đè vô điều kiện, nên code nghiệp vụ có tự
> gán `UpdatedBy` cũng bị đè. Và nhét `userName` vào tham số job là lối tắt sai — bug quay
> lại nguyên vẹn ở job tiếp theo.
>
> Ba bước: **(1)** chụp danh tính lúc enqueue, khi còn trong request · **(2)** thêm bản
> cài `ICurrentUser` thứ hai cho phạm vi job · **(3)** đăng ký nó cho vòng đời job của
> Hangfire. `AuditInterceptor` **không** phải sửa. `IBackgroundJobScheduler` **phải** mở
> rộng — hôm nay nó chỉ nhận đúng một biểu thức gọi hàm, không có chỗ mang danh tính.
>
> **Vì sao nó chặn tính năng đầu tiên dùng job nền:** đường import DTI ghi đè hàng loạt
> lên dữ liệu của kỳ đã báo cáo. Đó là thao tác rủi ro nhất của cả tính năng, và hiện là
> thao tác **duy nhất** không truy được người làm. Xem `spec/danh-muc-dti/business-rules.md`
> §Nhật ký.

> **#14 có file chủ riêng: [14-file-storage.md](14-file-storage.md).** Hôm nay
> `src/BE` **không còn seam lưu file nào** — bản `IImportFileStorage` duy nhất đi cùng
> module DtiWeekly, gỡ 2026-08-29 — nên đây là `📐 ĐÍCH ĐẾN`, không phải việc đang dở.
> Bảng "có thật hôm nay → sẽ thành" và thứ tự ưu tiên ở §2 file đó.
>
> **🔄 LẬT 2026-09-06.** Bản trước ghi *"Seam hiện tại mới đủ chạy trên máy dev (đường dẫn
> ghép cứng, chỉ có nhánh upload)"* — mô tả code đã bị xoá 8 ngày trước ngày viết. Cùng câu
> sai đó tồn tại song song ở đầu §2 của file chủ và đã sửa cùng lượt này.

> Đây cũng chính là 3 mục dễ rơi vào bẫy "viết đúng nhưng quên nối vào chỗ
> chạy thật" nhất (đăng ký DI, gắn middleware/pipeline) — mỗi khi implement
> xong 1 trong 3 mục này, viết kèm seam activation test theo luật ở
> [`04-testing-strategy.md`](04-testing-strategy.md) §"Seam activation test",
> đừng chỉ dựa vào unit test của riêng class đó.

**#19 Rate limiting, #20 CI pipeline** (mới, không nằm trong 18 mục gốc đối
chiếu VNR — tìm thấy khi đối chiếu thêm 12-Factor/OWASP/Clean Architecture
template) — xem `doc/huong_dan/quy-uoc/be-api-controller.md` §"Rate limiting" và
`../../../tham-khao-ngoai/vnr-successor/07-p6-archtests-gate.md` §6 cho thiết kế cụ thể.

**#8 Caching — bằng chứng đã xuất hiện, mục này KHÔNG còn ở trạng thái
"hoãn" (cập nhật 2026-08-18).** Nút thắt duy nhất còn hiệu lực trong `src/BE` hôm nay:
`RequirePermissionFilter` bắn **2 query DB mỗi request** có `[RequirePermission]`, trên
dữ liệu tí hon và gần như bất biến. Danh mục nút thắt đầy đủ (kể cả các mục đã hết hiệu
lực) ở [11-performance-caching.md](11-performance-caching.md) §"Nút thắt đã tìm thấy".

> **🔄 LẬT 2026-09-06.** Bản trước còn liệt hai bằng chứng nữa — *"dashboard 1 lần load ≈
> 10 round-trip"* và *"`GetPeriodsAsync` quét lại cùng một list 64 lần"* — cùng câu
> *"quan sát lý thuyết ở `AggregationService`"*. Cả ba đều nói về code của module DtiWeekly
> đã gỡ 2026-08-29; `AggregationService` và `GetPeriodsAsync` không còn tồn tại. File chủ
> [11-performance-caching.md](11-performance-caching.md) đã gạch A5/A6 đúng ngày đó, còn
> bản sao ở đây thì không ai sửa — đúng cơ chế hỏng mà `.claude/CLAUDE.md` §5 mô tả. Nay
> chỉ giữ vế còn đúng và trỏ về file chủ.

> **Đọc kèm §#7 (2026-09-05):** vế `RequirePermissionFilter` hôm nay là chi phí **tiềm
> tàng**, không phải chi phí đang phát sinh — chưa endpoint sản phẩm nào mang
> `[RequirePermission]`, nên số request kích hoạt 2 query đó đang là **0**. Nó trở thành
> chi phí thật ngay khi §#7 được đóng. Đừng lấy mục này làm bằng chứng "đang chậm". Chi tiết đầy đủ + quyết định đã CHỐT ở
[11-performance-caching.md](11-performance-caching.md).

Quyết định **không phải** "bật cache lên là xong": thứ tự bắt buộc là sửa
query pattern (`AsNoTracking`, index, N+1, đẩy `Distinct` xuống SQL) → sửa
thuật toán → **đo lại** → mới cache, và chỉ cache đúng phần có số đo biện
minh. Cache đặt trước các bước kia chỉ **che** lỗi chứ không sửa — xem lý do
ở [11-performance-caching.md](11-performance-caching.md) §1. Chọn in-memory
(`HybridCache`), **không** Redis, vì hệ thống hiện chỉ có 1 process.

**#18 i18n — 🚧 BẰNG CHỨNG ĐÃ XUẤT HIỆN (2026-08-27), mục này KHÔNG còn ở trạng
thái "hoãn".** Bản trước ghi *"chưa có yêu cầu đa ngôn ngữ nào từ nghiệp vụ. Khi
bằng chứng đó xuất hiện thật, quay lại mục tương ứng"*. Người dùng đã xác nhận
cần **tiếng Việt + tiếng Anh, ngay bây giờ** — đây đúng là cơ chế đó vận hành:
lý do hoãn dựa trên bằng chứng thì cũng chấm dứt bằng bằng chứng.

> 📖 i18n phía BE — file chủ: [`16-i18n-va-ma-loi.md`](16-i18n-va-ma-loi.md)
> 📖 i18n phía FE — file chủ: [`../fe/08-i18n.md`](../fe/08-i18n.md)

> **Rút gọn 2026-09-03 theo `.claude/CLAUDE.md` §5.** Mục này trước đây tự giữ
> nội dung: bảng so sánh hướng A/B, quyết định, và hệ quả cho BE. Đó là bản sao
> thứ hai của một chủ đề — và nó đã **lệch thật**: quyết định 2026-09-03 lật cơ
> chế bên dưới hướng A (đổi ngôn ngữ **ngay trong app**, không tải lại trang),
> nên câu *"việc ở BE: không có"* trong bảng cũ đã sai. File này là **bảng điểm
> danh** *"core gồm những gì, còn thiếu mảng nào"*, không phải nơi giữ luật —
> nay chỉ trỏ đường.

**#11/#12 vẫn cố tình KHÔNG làm** — xem
[03-metadata-driven-design.md](03-metadata-driven-design.md). Lý do **mạnh hơn** bản
trước chứ không yếu đi: hôm nay `src/BE` **không còn module nghiệp vụ nào**, nên engine
generic sẽ không có màn hình nào để phục vụ. Lý do này **không đổi theo giai đoạn
demo/product**, chỉ đổi theo số lượng module (đối chiếu VNR: engine generic chỉ hợp lý khi
có ≥5-10 màn hình CRUD hoặc ≥2-3 dashboard giống nhau thật).

Đếm bằng lệnh, đừng chép số (§6):

```bash
find src/BE -name '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' \
     -not -path 'src/BE/Core/*' -not -path 'src/BE/Tests/*' \
     -not -name 'PlatformManager.Api.csproj' | wc -l
```

PASS hôm nay (đối chiếu 2026-09-08): **0** — mọi project trong solution đều thuộc `Core/`,
`Tests/`, hoặc là host `PlatformManager.Api`.

> **🔄 SỬA 2026-09-08 — lệnh cũ đã thành tiêu chí LUÔN-LUÔN-ĐÚNG.** Bản trước đếm bằng
> `find src/BE/Modules -name '*.csproj' | wc -l` kèm câu *"thư mục `src/BE/Modules/DtiWeekly/`
> chỉ còn `bin/`+`obj/`"*. Thư mục `src/BE/Modules/` **đã xoá 2026-09-08**
> (`doc/kien-truc-core-module.md:42`), nên `find` in lỗi ra **stderr** rồi trả **rỗng** —
> `wc -l` vẫn ra `0`, tức PASS. Nghĩa là tiêu chí đó nay không đo gì và không thể fail: nó
> sẽ báo "0 module" **kể cả sau khi** tầng nghiệp vụ được dựng lại, đúng lúc con số này cần
> đổi nhất.
>
> Lệnh mới **loại trừ** những gì đã biết (Core, Tests, host) thay vì **trỏ vào** một thư mục
> có thể không tồn tại — nên nó đúng cả hôm nay lẫn ngày tầng nghiệp vụ ra đời, và không phụ
> thuộc việc tầng đó được đặt tên `Business/` hay `Modules/`.

> **🔄 LẬT 2026-09-06.** Bản trước ghi *"vì chỉ có 2 module nghiệp vụ"*. Con số đó đã sai
> từ 2026-08-29 (ngày gỡ DtiWeekly) — đúng khuôn "bảng đếm tay mục ruỗng" mà
> `.claude/CLAUDE.md` §6 cấm, nên thay bằng lệnh + tiêu chí PASS.
