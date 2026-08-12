# CLAUDE.md — src/BE

Chuẩn kiến trúc cho backend PlatformManager. File này được tạo **trước khi
có solution thật** — mục đích là để `dotnet new` đầu tiên (và mọi feature
sau đó) đi đúng đường ngay từ ngày một.

Agent chính chịu trách nhiệm vùng này: `backend-expert` (xem
`.claude/agents/backend-expert.md` ở workspace root). Chi tiết theo từng chủ
đề nằm ở `.claude/rules/` trong chính thư mục này — đọc file tương ứng
**trước khi** viết code cho vùng đó.

## Stack

- **.NET** (bản LTS/STS mới nhất tại thời điểm scaffold — ghi rõ version vào
  đây sau khi chốt, vd. ".NET 9").
- **Clean Architecture**: Domain / Application / Infrastructure / Api.
- **CQRS-lite qua MediatR**: mỗi use case = 1 Command/Query + 1 Handler.
- **EF Core + PostgreSQL** (mặc định — đổi nếu dự án chọn DB khác).
- **FluentValidation** cho input validation.
- **ASP.NET Core Identity** cho auth (đã chốt — xem
  `.claude/rules/api-controller.md` § Auth/Permission). Entity `AppUser`
  (`IdentityUser<Guid>`) là bảng người dùng dùng chung cho toàn hệ thống.

## Đọc theo chủ đề

| File | Đọc khi |
| --- | --- |
| `.claude/rules/architecture.md` | Layer rule, dependency direction, project layout |
| `.claude/rules/entity-domain.md` | Base entity, soft delete, Value Object, factory method |
| `.claude/rules/cqrs-handler.md` | Command/Query, Handler, Validator, `Result<T>` |
| `.claude/rules/api-controller.md` | Controller, envelope response, error → HTTP mapping |

## Scaffold lần đầu (khi `.sln` chưa tồn tại)

```bash
cd src/BE
dotnet new sln -n PlatformManager
dotnet new classlib -n PlatformManager.Domain -o src/PlatformManager.Domain
dotnet new classlib -n PlatformManager.Application -o src/PlatformManager.Application
dotnet new classlib -n PlatformManager.Infrastructure -o src/PlatformManager.Infrastructure
dotnet new webapi -n PlatformManager.Api -o src/PlatformManager.Api
dotnet sln add src/**/*.csproj
# Thiết lập project reference đúng chiều — xem .claude/rules/architecture.md
```

Sau khi scaffold xong, `{BE_ROOT}` marker `*.sln` sẽ tồn tại và các
skill/agent tự resolve bình thường.

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
