# 1. Core thật sự của senior lâu năm thường thiết kế

## Nguyên tắc chọn lọc — Nhóm A vs Nhóm B

Trước khi liệt kê, 1 nguyên tắc phải giữ xuyên suốt: **core không phải là "thêm càng nhiều abstraction càng chuyên nghiệp"**. Mỗi thành phần dưới đây giải quyết 1 nỗi đau *thật* — chỉ xây khi hệ thống đã/sắp chạm đúng nỗi đau đó, không xây trước "phòng khi cần" (premature abstraction là nguồn nợ kỹ thuật lớn nhất ở các core tự thiết kế).

## Danh sách thành phần core (đã xác nhận qua VNR + kiến thức chung ngành)

| # | Thành phần | Nỗi đau nó giải quyết | Mức ưu tiên |
|---|---|---|---|
| 1 | **BaseEntity** (`Id`, `CreatedAt`/`UpdatedAt`, `IsDelete`) + soft-delete qua **global query filter** | Quên filter `IsDelete` ở 1 query = lộ dữ liệu đã xoá | Bắt buộc, ngày đầu |
| 2 | **Generic Repository/UnitOfWork** | Không viết lại CRUD cơ bản cho mỗi entity | Bắt buộc, ngày đầu |
| 3 | **Factory method + private setter** cho entity nghiệp vụ | Invariant bị vỡ do gán property tuỳ tiện | Bắt buộc, ngày đầu |
| 4 | **Value Object** cho field có luật (tiền tệ, %, email, SĐT...) | Dữ liệu sai lọt qua vì dùng `decimal`/`string` trơ | Nên có sớm |
| 5 | **Error-as-value (`Result<T>`)** cho lỗi nghiệp vụ mong đợi + **exception middleware toàn cục** cho lỗi thật bất ngờ (trả `ErrorCode`+`TraceId`, không lộ stack trace) | Exception-driven control flow rối, hoặc lộ chi tiết nội bộ ra client | Bắt buộc, ngày đầu |
| 6 | **Envelope response nhất quán** (`{data, message, status, code, businessCode, traceId, retryable, fields}` — xem `doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope) cho MỌI endpoint kể cả list | FE phải viết 2 nhánh parse khác nhau | Bắt buộc, ngày đầu |
| 7 | **Auth/Identity + Permission framework** (context "current user", resource-action key) | Xem [02-identity-auth.md](02-identity-auth.md) | Bắt buộc, ngày đầu |
| 8 | **Caching abstraction** (distributed + local, tự fallback êm khi cache down) | Redis down làm sập app; không tra được "đang cache gì" | ⚠️ Đã có bằng chứng (2026-08-18) — xem [11-performance-caching.md](11-performance-caching.md), phạm vi hẹp + đúng thứ tự |
| 9 | **Logging/Audit abstraction** (structured, tách log kỹ thuật vs audit nghiệp vụ) | Log dạng string không tra cứu được | Bắt buộc, ngày đầu |
| 10 | **Config/Options abstraction** (`IOptions<T>` typed, fail-fast lúc khởi động) | Cấu hình sai chỉ lộ ra lúc runtime gọi tới, không phải lúc start | Nên có sớm |
| 11 | **Generic CRUD/Grid/Form engine** | Xem [03-metadata-driven-design.md](03-metadata-driven-design.md) — nguồn cột nên từ code, không phải DB tự do | Khi có ≥5-10 màn CRUD giống nhau |
| 12 | **Widget/Dashboard rendering engine** | Dựng lại UI biểu đồ/KPI cho mỗi dashboard mới | Khi có ≥2-3 dashboard |
| 13 | **Notification abstraction** (email/SMS/push — đổi kênh không đổi code gọi) | Đổi nhà cung cấp email phải sửa code khắp nơi | Khi có ≥2 kênh thông báo |
| 14 | **File storage abstraction** (local/S3/Blob — swap được) | Chuyển hạ tầng lưu file phải viết lại toàn bộ | Khi cần production-ready |
| 15 | **Import/Export engine** (parse → validate từng dòng → map DTO → upsert) | Mỗi màn Import viết lại pipeline riêng, dễ lệch quy tắc lỗi | Khi có ≥2 màn Import |
| 16 | **Outbound HTTP integration engine** (gọi API bên thứ 3, có resilience + **anti-SSRF guard**) | Retry tay không nhất quán; endpoint cấu hình DB có thể trỏ vào mạng nội bộ | Khi gọi API 3rd-party cấu hình được qua UI |
| 17 | **Background job/scheduler abstraction** | Task nền viết tay dễ mất khi restart, không có retry | Khi có tác vụ chạy nền/định kỳ |
| 18 | **i18n/localization framework** | Chỉ cần nếu hệ thống thật sự đa ngôn ngữ | Tuỳ yêu cầu |
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
> **Cập nhật 2026-08-18:** #8 caching đã rời khỏi danh sách "vẫn nên hoãn" —
> bằng chứng cụ thể đã xuất hiện khi rà soát code, xem
> [11-performance-caching.md](11-performance-caching.md). Đây đúng là cách
> nguyên tắc trên vận hành: lý do hoãn dựa trên bằng chứng thì cũng chấm dứt
> bằng bằng chứng, không phải bằng việc "tới giai đoạn".

Đã có, giữ nguyên: #1, #2, #3, #5, #6, #9 (mức tối giản) qua
`AssessmentUpsertService`/`AggregationService`/`IApiResult<T>`/
`GlobalExceptionHandler` (xem `doc/huong_dan/quy-uoc/be-api-controller.md`,
đã thay `ApiResponse<T>`/`ExceptionMiddleware` cũ).

**#7 Auth/Permission — cần tách rõ 2 nửa, dễ nhầm "đã xong":** nửa
**authentication** (đăng nhập là ai) đã triển khai qua ASP.NET Core Identity (2026-08-16)
(xem `doc/huong_dan/quy-uoc/README.md` §Stack, `doc/cau-truc-database.md` §4.1), sống ở
`PlatformManager.Core.Infrastructure`. Nửa **authorization theo hành động**
(đăng nhập rồi được làm gì) **CHƯA** — endpoint nghiệp vụ hiện chỉ
`[Authorize]` trần. Rule cụ thể đã viết ở
`doc/huong_dan/quy-uoc/be-api-controller.md` §"Phân quyền theo hành động" nhưng
chưa implement. **Nâng độ ưu tiên lên "bắt buộc trước khi có user thật ngoài
đội dev"** — đây là [OWASP #1 Broken Access Control](https://owasp.org/Top10/2025/A01_2025-Broken_Access_Control/),
không phải tuỳ chọn "nên có sớm".

**#10 Config/Options fail-fast — nâng từ "chưa cần" lên "nên có sớm":** rule
cụ thể (`ValidateDataAnnotations().ValidateOnStart()`) thêm ở
`doc/huong_dan/quy-uoc/be-architecture.md` §"Cấu hình — fail-fast validation".

**#13 Notification, #17 Background job/scheduler, #14 File storage
abstraction** — đã quyết định kiến trúc (Hangfire cho job nền,
`INotificationSender` seam cho email, `IImportFileStorage` cho file tạm) khi
thiết kế lại Import CSV/Excel — xem `doc/huong_dan/quy-uoc/be-cqrs-handler.md`
§"Command chạy lâu → job nền" và `doc/huong_dan/quy-uoc/be-architecture.md`
§"Notification". Đây là ví dụ cho nguyên tắc ở trên: **quyết định** đã có,
chỉ **implement** chưa xong — khác hẳn "chưa cần" thật sự.

**#19 Rate limiting, #20 CI pipeline** (mới, không nằm trong 18 mục gốc đối
chiếu VNR — tìm thấy khi đối chiếu thêm 12-Factor/OWASP/Clean Architecture
template) — xem `doc/huong_dan/quy-uoc/be-api-controller.md` §"Rate limiting" và
`be/trien-khai/07-p6-archtests-gate.md` §6 cho thiết kế cụ thể.

**#8 Caching — bằng chứng đã xuất hiện, mục này KHÔNG còn ở trạng thái
"hoãn" (cập nhật 2026-08-18).** Rà soát code thật đã tìm ra nút thắt cụ thể,
không còn là quan sát lý thuyết ở `AggregationService`: `RequirePermissionFilter`
bắn **2 query DB mỗi request** có `[RequirePermission]` trên dữ liệu tí hon
và gần như bất biến; dashboard 1 lần load ≈ 10 round-trip; `GetPeriodsAsync`
quét lại cùng một list 64 lần. Chi tiết đầy đủ + quyết định đã CHỐT ở
[11-performance-caching.md](11-performance-caching.md).

Quyết định **không phải** "bật cache lên là xong": thứ tự bắt buộc là sửa
query pattern (`AsNoTracking`, index, N+1, đẩy `Distinct` xuống SQL) → sửa
thuật toán → **đo lại** → mới cache, và chỉ cache đúng phần có số đo biện
minh. Cache đặt trước các bước kia chỉ **che** lỗi chứ không sửa — xem lý do
ở [11-performance-caching.md](11-performance-caching.md) §1. Chọn in-memory
(`HybridCache`), **không** Redis, vì hệ thống hiện chỉ có 1 process.

**#18 i18n — vẫn hoãn, lý do là bằng chứng, không phải giai đoạn:** chưa có
yêu cầu đa ngôn ngữ nào từ nghiệp vụ. Khi bằng chứng đó xuất hiện thật, quay
lại mục tương ứng — không phải "chờ qua giai đoạn nào đó".

**#11/#12 vẫn cố tình KHÔNG làm** — xem
[03-metadata-driven-design.md](03-metadata-driven-design.md), vì chỉ có 2
module nghiệp vụ, làm engine generic lúc này là over-engineering — lý do
này **không đổi theo giai đoạn demo/product**, chỉ đổi theo số lượng module
(đối chiếu VNR: engine generic chỉ hợp lý khi có ≥5-10 màn hình CRUD hoặc
≥2-3 dashboard giống nhau thật).
