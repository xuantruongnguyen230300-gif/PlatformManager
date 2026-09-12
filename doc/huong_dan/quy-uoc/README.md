---
kind: luat
scope: core
verified: 2026-09-06
---

# Quy ước đang thực thi — PlatformManager

Đây là **quy ước thi hành hiện tại** cho `src/BE` và `src/FE`: layer rule, cách
dựng entity, hình dạng handler, envelope response, cấu trúc feature FE, ranh
giới DTO/model. `backend-expert` và `frontend-expert` đọc file tương ứng
**trước khi** viết code cho vùng đó.

> **Khác gì `doc/huong_dan/wiki-core/`?** `wiki-core/` là **kiến thức nền** —
> "một core tốt gồm những gì và vì sao", có thể vượt nhu cầu của PlatformManager.
> Thư mục này là **những gì PlatformManager thực sự đang làm**. `core-reviewer`
> đọc cả hai, và chính khoảng cách giữa chúng cho phép nó phân biệt *"lệch vì đã
> cố ý đơn giản hoá"* (không phải finding) với *"lệch vì thiếu sót thật"* (là
> finding).

> **Lịch sử:** 7 file này trước nằm ở `src/BE/.claude/rules/` và
> `src/FE/.claude/docs/` (đã xoá), cùng 2 file mục lục `src/BE/CLAUDE.md` và
> `src/FE/CLAUDE.md` (đã xoá) — tổng 78 KB tri thức nằm ngoài `doc/`. Chuyển về
> đây **2026-08-23** theo `.claude/CLAUDE.md` §2: `doc/` là nguồn tri thức duy
> nhất, `.claude/` chỉ chứa quy trình và ràng buộc.
>
> Chi phí của việc để chúng ở ngoài đã đo được: recipe `RowVersion` sai provider
> tồn tại **song song** ở `wiki-core/be/06-concurrency-control.md` và
> `rules/entity-domain.md`, sửa một nơi không chạm nơi kia. Và
> `src/BE/CLAUDE.md` (đã xoá) vẫn khẳng định *"cả 5 project `Business.*` đã tồn tại"* —
> đúng câu sai mà `.claude/CLAUDE.md` ghi là đã sửa từ 2026-08-21; nó sống sót
> **10 tháng** vì nằm ngoài tầm với của mọi luật.

## Mục lục

| File | Đọc khi |
| --- | --- |
| [`be-architecture.md`](be-architecture.md) | Layer rule, dependency direction, project layout, cấu hình fail-fast |
| [`be-entity-domain.md`](be-entity-domain.md) | Base entity, soft delete, Value Object, factory method, `RowVersion` |
| [`be-cqrs-handler.md`](be-cqrs-handler.md) | Command/Query, Handler, Validator, `ErrorDescriptor` |
| [`be-api-controller.md`](be-api-controller.md) | Controller, envelope response, error → HTTP, rate limiting, phân quyền |
| [`be-performance.md`](be-performance.md) | Repository, query, index, N+1, cache — đọc cùng `wiki-core/be/11-performance-caching.md` |
| [`fe-architecture.md`](fe-architecture.md) | Tầng `core`/`modules`/`shared`, cấu trúc 1 feature |
| [`fe-api-client.md`](fe-api-client.md) | Gọi API, ranh giới DTO/model, mapper |
| [`fe-ui-conventions.md`](fe-ui-conventions.md) | Dựng UI, form, responsive, style theo token |
| [`fe-routing-guard.md`](fe-routing-guard.md) | Route + lazy-load, guard auth/role, `mustChangePassword` |
| [`repo-artifact.md`](repo-artifact.md) | Cái gì được commit: artifact build, dữ liệu runtime, secret |
| [`tieu-chi-review.md`](tieu-chi-review.md) | **`core-reviewer`**: cái gì là finding, cái gì KHÔNG phải |

> 🔄 **LẬT 2026-09-06.** Bảng trên trước đây **thiếu `be-performance.md`** — file
> tồn tại thật, được `doc/README.md` trỏ tới, nhưng ai vào thư mục này qua mục lục
> thì không thấy nó. Đếm lại bằng lệnh thay vì tin bảng:
> `ls doc/huong_dan/quy-uoc/*.md` phải khớp số dòng của bảng cộng chính `README.md`.

Ranh giới Core ↔ Business và ngưỡng tách module: [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md).
Giao diện người dùng — **mọi** surface, cả FE lẫn BE: [`../../Design/`](../../Design/).

## Stack — Backend (`src/BE`)

- **.NET 10**, Clean Architecture, CQRS-lite qua **MediatR** (mỗi use case = 1
  Command/Query + 1 Handler), **EF Core + PostgreSQL**, **FluentValidation** cho
  input validation.
- **ASP.NET Core Identity** cho auth (đã CHỐT — **cookie session, KHÔNG JWT**,
  xem `be-api-controller.md` § Auth/Permission). Entity `AppUser`
  (`IdentityUser<Guid>`) là bảng người dùng dùng chung toàn hệ thống, sống ở
  `Core.Infrastructure`.

### 🚧 Layout project — ĐÃ CHỐT, ĐANG THI CÔNG

Đích đến là 2 tầng `Core.*` + `Business.*`. **Hiện trạng chưa phải như vậy** —
bảng đầy đủ "có thật hôm nay → sẽ thành" (đối chiếu `PlatformManager.slnx`)
là **`doc/kien-truc-core-module.md`** §"ĐÃ CHỐT — ĐANG THI CÔNG", đọc file đó
trước khi tạo file mới. Đừng tin số project chép lại ở bất kỳ đâu — con số này
đã lệch nhiều lần khi project test được thêm rồi module nghiệp vụ bị gỡ. Đếm
bằng lệnh:

```bash
find src/BE -name "*.csproj" -not -path "*/obj/*" | sort
```

Vì vậy **chưa** thêm tính năng nghiệp vụ vào `Business.*` — project đó chưa có.
Đọc [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) trước khi
tạo project mới, và hỏi người dùng nếu không chắc.

## Stack — Frontend (`src/FE`)

- **Angular 20** standalone + Signals, **zoneless** (không dùng `NgModule`).
- Control flow `@if` / `@for` / `@switch` / `@defer`; `input()` / `output()` kiểu
  signal thay cho decorator `@Input()`/`@Output()`.
- **PrimeNG** + PrimeIcons v7 với preset riêng
  (`core/theme/core-preset.ts`, bảng màu do app bơm vào); SCSS scoped theo component.
- Token màu/spacing lấy từ [`../../Design/Frontend/PlatformManager/`](../../Design/Frontend/PlatformManager/)
  — không tự phát minh giá trị song song.

## Maintenance Rules

1. Mọi feature mới đi đúng layer rule (`be-architecture.md` / `fe-architecture.md`)
   — không có ngoại lệ *"vì đây chỉ là feature nhỏ"*.
2. Envelope response giữ nhất quán cho **mọi** endpoint, kể cả list/grid — xem
   `be-api-controller.md`. Hợp đồng từng endpoint ở [`../../contracts/`](../../contracts/).
3. DTO/model tách biệt ngay từ slice đầu tiên (`fe-api-client.md`) — đừng đợi tới
   khi có bug wire mới tách.
4. UI mới phải khớp token/component đã tài liệu hoá trong
   [`../../Design/`](../../Design/) — đây là nguồn giao diện **duy nhất**
   (`.claude/CLAUDE.md` §7).
5. Schema: nguồn chuẩn là [`../../cau-truc-database.md`](../../cau-truc-database.md)
   (schema `core`, đang sống) + [`../../cau-truc-database-dti.md`](../../cau-truc-database-dti.md)
   (schema `business`, đang sống) + [`../../cau-truc-database.sql`](../../cau-truc-database.sql)
   (DDL viết tay EF không sinh được). Ba file, hai schema, **một** database —
   bảng trạng thái ở [`../../README.md`](../../README.md).

   > 🔄 **LẬT 2026-09-06.** Bản trước của mục này là một câu bị nối vỡ và sai hai
   > điểm: (a) nó trỏ tới `*.dbml` như một tài liệu đang tồn tại — `find doc -name "*.dbml"`
   > không cho kết quả nào; (b) nó gọi `cau-truc-database.md` là *"nguồn chuẩn duy nhất"*
   > trong khi phần nghiệp vụ đã tách ra file riêng từ 2026-09-03 để khai đúng khoá `scope`.
