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
| 6 | **Envelope response nhất quán** (`{Success, Data, ErrorCode, ErrorMessage, TraceId}`) cho MỌI endpoint kể cả list | FE phải viết 2 nhánh parse khác nhau | Bắt buộc, ngày đầu |
| 7 | **Auth/Identity + Permission framework** (context "current user", resource-action key) | Xem [02-identity-auth.md](02-identity-auth.md) | Bắt buộc, ngày đầu |
| 8 | **Caching abstraction** (distributed + local, tự fallback êm khi cache down) | Redis down làm sập app; không tra được "đang cache gì" | Khi có query nặng lặp lại |
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

## Áp dụng vào PlatformManager

PlatformManager đã có #1, #2, #3, #5, #6, #9 (mức tối giản) qua
`AssessmentUpsertService`/`AggregationService`/`IApiResult<T>`/
`GlobalExceptionHandler` (xem `src/BE/.claude/rules/api-controller.md`,
đã thay `ApiResponse<T>`/`ExceptionMiddleware` cũ). #7 **đã triển khai**
qua ASP.NET Core Identity (xem `src/BE/CLAUDE.md` §Stack,
`doc/ERD/ERD-corebase.md`), sống ở `PlatformManager.Core.Infrastructure`
theo kiến trúc Modular Monolith (xem `doc/kien-truc-core-module.md`) —
không phải Module nào; #8/#10/#13/#14/#17/#18 (chưa cần ở quy mô này),
#11/#12 (cố tình KHÔNG làm — xem
[03-metadata-driven-design.md](03-metadata-driven-design.md), vì chỉ có 2
module nghiệp vụ, làm engine generic lúc này là over-engineering).
