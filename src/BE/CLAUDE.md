# CLAUDE.md — src/BE

Chuẩn kiến trúc cho backend PlatformManager. File này được tạo **trước khi
có solution thật** — mục đích là để `dotnet new` đầu tiên (và mọi feature
sau đó) đi đúng đường ngay từ ngày một.

Agent chính chịu trách nhiệm vùng này: `backend-expert` (xem
`.claude/agents/backend-expert.md` ở workspace root). Chi tiết theo từng chủ
đề nằm ở `.claude/rules/` trong chính thư mục này — đọc file tương ứng
**trước khi** viết code cho vùng đó.

## Stack

- **.NET 10**.
- **Clean Architecture, 2 tầng Core ↔ Business**: `Core.{Domain,Application,
  Persistence,Infrastructure,Api}` (dùng lại được, không biết gì về nghiệp
  vụ) + `Business.{Domain,Application,Persistence,Infrastructure,Api}` (1
  khối duy nhất chứa MỌI tính năng nghiệp vụ — DTI Weekly là tính năng đầu
  tiên, KHÔNG phải 1 "module" riêng) + `PlatformManager.Api` (host mỏng,
  composition root duy nhất). Xem **`doc/kien-truc-core-module.md`** (root
  repo) TRƯỚC khi tạo project mới hoặc thêm tính năng nghiệp vụ mới — quyết
  định ranh giới Core/Business, lý do, ngưỡng nâng cấp tiếp theo (kể cả khi
  nào mới tách thành nhiều module thật) đều nằm ở đó.
- **CQRS-lite qua MediatR**: mỗi use case = 1 Command/Query + 1 Handler.
- **EF Core + PostgreSQL**.
- **FluentValidation** cho input validation.
- **ASP.NET Core Identity** cho auth (đã chốt — xem
  `.claude/rules/api-controller.md` § Auth/Permission). Entity `AppUser`
  (`IdentityUser<Guid>`) là bảng người dùng dùng chung cho toàn hệ thống,
  sống ở `Core.Infrastructure` (không phải Module nào).

## Đọc theo chủ đề

| File | Đọc khi |
| --- | --- |
| `doc/kien-truc-core-module.md` (root repo) | Ranh giới Core ↔ Business, thêm tính năng nghiệp vụ mới |
| `.claude/rules/architecture.md` | Layer rule, dependency direction, project layout |
| `.claude/rules/entity-domain.md` | Base entity, soft delete, Value Object, factory method |
| `.claude/rules/cqrs-handler.md` | Command/Query, Handler, Validator, `ErrorDescriptor` |
| `.claude/rules/api-controller.md` | Controller, envelope response, error → HTTP mapping |

## Thêm tính năng nghiệp vụ mới — KHÔNG tạo project mới

Cả 5 project `PlatformManager.Business.*` đã tồn tại — tính năng nghiệp vụ mới (sau DTI Weekly)
chỉ thêm thư mục/file MỚI vào project đã có (entity → `Business.Domain/`, feature vertical-slice
→ `Business.Application/<TênFeature>/`, EF config → `Business.Persistence/`, controller →
`Business.Api/Controllers/`) — xem checklist đầy đủ ở `.claude/rules/architecture.md` §
"Thêm tính năng nghiệp vụ mới". Chỉ tạo project mới (`PlatformManager.Modules.<Tên>.*`) khi đó là
1 domain nghiệp vụ ĐỘC LẬP thật — đọc kỹ `doc/kien-truc-core-module.md` § Khi nào tách thành
module độc lập thật trước khi tự quyết, hỏi người dùng nếu không chắc.

`{BE_ROOT}` marker `*.sln`/`*.slnx` đã tồn tại (`PlatformManager.slnx`).

## Maintenance Rules

1. Mọi feature mới đi theo đúng layer rule trong `.claude/rules/architecture.md`
   — không có ngoại lệ "vì đây chỉ là feature nhỏ".
2. `doc/ERD/PlatformManager.dbml` (+ mô tả đầy đủ ở `doc/ERD/ERD.md`) là
   **nguồn schema dự kiến hiện tại** cho domain "DTI Weekly" — tổng hợp từ
   `doc/ERD/example_db_ver1.csv` (dữ liệu mẫu) và `doc/Prototype/dashboard.html`
   (prototype JS). Đây vẫn là ERD **DỰ KIẾN**, chưa migrate/chưa chốt — khi
   dựng entity đầu tiên, dùng nó làm bản thiết kế tham chiếu, đối chiếu lại
   với người dùng trước khi tạo migration đầu tiên. Các quyết định sau **đã
   CHỐT** (xem mục "Quyết định đã CHỐT" đầu `doc/ERD/ERD.md`): `Owner`/
   `Deadline` gắn ở `CriteriaAssessment` (theo từng kỳ, không phải
   `Criteria`); `Status` là field nhập tay lưu DB, khác với badge tính động
   ở `dashboard.html`; auth dùng ASP.NET Core Identity
   (`CriteriaAssessment.OwnerId` → FK `AppUser.Id`). Chỉ còn 1 câu hỏi mở
   (cấu trúc hoá `CriteriaEvidence.Content`) — xem cuối `doc/ERD/ERD.md`.
   Nghiệp vụ chi tiết của màn hình DTI Weekly (validation, công thức tính
   delta, quy tắc `AssessmentPeriod`...) xem
   `spec/dashboard-dti-weekly/business-rules.md`.
3. Envelope response giữ nhất quán cho **mọi** endpoint (kể cả list/grid) —
   xem `.claude/rules/api-controller.md`, đừng để một dạng response khác đi.
