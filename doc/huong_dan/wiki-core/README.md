# Wiki Core — bộ quy tắc chuẩn cho phần "core" BE/FE

> Đúc kết từ đối chiếu thực tế (`D:\Successor\VNR.Successor\src\backend\.claude\rules\*.md`
> — 13 file quy ước của 1 backend .NET production lâu năm) + kiến thức kiến
> trúc phần mềm đã ổn định lâu năm (Odoo, SAP Data Dictionary, Salesforce
> Metadata API). Mục đích: làm nền tham chiếu khi thiết kế core cho hệ thống
> mới (không nhất thiết là PlatformManager — PlatformManager chỉ dùng 1 phần
> nhỏ, đơn giản hoá, xem mục "Áp dụng vào PlatformManager" ở cuối mỗi file).
>
> **Không phải checklist bắt buộc.** Áp dụng phần nào tuỳ mức độ đau thật sự
> hệ thống gặp — xem nguyên tắc "Nhóm A/Nhóm B" ở `be/01-core-components.md`.

## Vai trò của thư mục này

Đây là **chuẩn đối chiếu** mà agent `core-reviewer` dùng để kiểm tra xem
phần core của `src/BE` và `src/FE` có tuân thủ hay không — cũng là tài liệu
mà `backend-expert`/`frontend-expert` tham khảo khi implement phần core.
Khác với `src/BE/.claude/rules/` hay `src/FE/.claude/docs/` (quy ước **thực
thi** hiện tại, gắn liền code đang có), thư mục này là **kiến thức nền** ở
tầm rộng hơn — bao gồm cả những gì PlatformManager demo hiện tại chưa cần.

**2 lớp nội dung cho BE, đọc theo thứ tự khác nhau tuỳ mục đích:**

- `be/01-…11-…` (dưới đây) trả lời **"core gồm những gì và vì sao cần"** —
  lý thuyết, khái niệm, nguyên tắc Nhóm A/B.
- `be/trien-khai/00-…08-…` trả lời **"làm thì làm theo thứ tự nào, đẻ ra
  file/class/interface nào, cấu trúc source code ra sao"** — thực hành, đối
  chiếu trực tiếp với source thật của `VNR.Successor`, không lý thuyết suông.
  Bắt đầu từ [be/trien-khai/00-lo-trinh-tong-the.md](be/trien-khai/00-lo-trinh-tong-the.md).

## Mục lục

### BE — lý thuyết (`be/`)

1. [Core components](be/01-core-components.md) — danh sách 18 thành phần core thật sự, nguyên tắc Nhóm A/B
2. [Identity & Auth đa-Process](be/02-identity-auth.md) — xác thực khi hệ thống tách nhiều Process
3. [Metadata-driven design](be/03-metadata-driven-design.md) — khung 3 cơ chế A/B/C, JSON-column cho .NET 10
4. [Testing strategy](be/04-testing-strategy.md) — ArchTest, test pyramid, cấm InMemory DB
5. [Cross-module consistency](be/05-cross-module-consistency.md) — integration event, idempotency, Outbox
6. [Concurrency control](be/06-concurrency-control.md) — Optimistic Concurrency qua RowVersion
7. [Observability](be/07-observability.md) — health check, correlation ID, metrics
8. [ADR practice](be/08-adr-practice.md) — ghi lại quyết định kiến trúc
9. [Security beyond auth](be/09-security-beyond-auth.md) — rate limiting, secret management, raw SQL
10. [Data retention](be/10-data-retention.md) — soft-delete không phải archival
11. [Performance & Caching](be/11-performance-caching.md) — 3 tầng (query → thuật toán → cache), quy tắc query bắt buộc, chính sách cache
12. [Thông báo (Notification)](be/12-notifications.md) — **tạm dừng có chủ đích**; chọn kênh (in-app/email/Zalo ZNS), Outbox, idempotency, mảnh đã có sẵn để tái dùng

### BE — thực hành / lộ trình triển khai (`be/trien-khai/`)

0. [Lộ trình tổng thể](be/trien-khai/00-lo-trinh-tong-the.md) — 7 phase (P0–P6), cây thư mục source đích, thứ tự phụ thuộc
1. [P0 — Nền móng solution](be/trien-khai/01-p0-nen-mong-solution.md) — `Directory.Build.props`, quy ước đặt tên, ArchTest đầu tiên
2. [P1 — `Platform.Domain`](be/trien-khai/02-p1-platform-domain.md) — `BaseEntity`, `AggregateRoot`, `Enumeration<TEnum>`, Value Object
3. [P2 — `Platform.Application`](be/trien-khai/03-p2-platform-application.md) — CQRS, 6 pipeline behavior, `IApiResult<T>`, `ErrorDescriptor`
4. [P3 — `Platform.Persistence`](be/trien-khai/04-p3-platform-persistence.md) — `BaseDbContext`, interceptor, `UnitOfWork`, DIP Seam
5. [P4 — `Hosting.Api`](be/trien-khai/05-p4-hosting-api.md) — `BaseApiController`, envelope, `ErrorCode → HTTP`, permission
6. [P5 — Module đầu tiên](be/trien-khai/06-p5-module-dau-tien.md) — 2 pattern CRUD (zero-handler vs vertical slice)
7. [P6 — ArchTests gate](be/trien-khai/07-p6-archtests-gate.md) — 34 gate thật, khi nào cần cái nào
8. [Tra cứu file/class](be/trien-khai/08-tra-cuu-file-class.md) — mục lục ngược toàn bộ series

### FE — lý thuyết (`fe/`)

1. [Core components](fe/01-core-components.md) — 12 thành phần core FE thật sự, nguyên tắc Nhóm A/B
2. [HTTP Client & Envelope](fe/02-http-envelope.md) — tiêu thụ `IApiResult<T>` từ BE
3. [State management](fe/03-state-management.md) — `signal()` → `signalStore()` có điều kiện
4. [Design-token system](fe/04-design-token-system.md) — bridge với `doc/Design/`
5. [Component library](fe/05-component-library.md) — 12 component thật, 5 trạng thái bắt buộc
6. [Testing strategy](fe/06-testing-strategy.md) — mapper/interceptor trước, không coverage dàn trải
7. [Auth/Identity](fe/07-auth-identity.md) — cookie session của ASP.NET Core Identity
8. [i18n](fe/08-i18n.md) — `@angular/localize`
9. [Forms & Validation](fe/09-forms-validation.md)
10. [Observability](fe/10-observability.md) — correlation với `traceId` của BE
11. [Grid & Metadata sync](fe/11-grid-and-metadata.md) — PrimeNG `p-table`, hợp đồng menu/cột với BE
12. [Charting](fe/12-charting.md) — PrimeNG `p-chart`, ngưỡng nâng cấp `ngx-echarts`
13. [Performance](fe/13-performance.md) — zoneless, `@defer`, virtual scroll, bundle budget

### FE — thực hành / lộ trình triển khai (`fe/trien-khai/`)

0. [Lộ trình tổng thể](fe/trien-khai/00-lo-trinh-tong-the.md) — 6 giai đoạn F0–F5, khác cách đọc P0–P6 của BE vì Angular CLI đã scaffold sẵn
1. [F0 — Đồng bộ envelope](fe/trien-khai/01-f0-dong-bo-envelope.md) — việc cấp bách nhất, chặn mọi việc khác
2. [F1 — Đồng bộ Design](fe/trien-khai/02-f1-dong-bo-design.md) — re-run pipeline thiết kế, bổ sung 5 trạng thái
3. [F2 — Dọn nợ kỹ thuật](fe/trien-khai/03-f2-don-no-ky-thuat.md) — hardcode hex, test còn thiếu
4. [F3 — Auth](fe/trien-khai/04-f3-auth.md) — phụ thuộc BE scaffold Identity thật
5. [Gate](fe/trien-khai/05-gate.md) — lint rule + CI check tương đương ArchTest

> **Nguồn tham chiếu khác BE:** `fe/` không có "VNR.Successor frontend" để
> đối chiếu — nguồn là kiến trúc chính thức của Angular, hệ thống thiết kế
> thật của chính PlatformManager (`doc/Design/Frontend/PlatformManager/`),
> và các quyết định đã chốt trực tiếp với người dùng (2026-08-15) khi đối
> chiếu với BE. Xem [fe/01-core-components.md](fe/01-core-components.md)
> § đầu file.

## Cách dùng

- **`backend-expert`/`frontend-expert`**: đọc file tương ứng chủ đề đang làm
  khi implement phần core (không cần đọc hết mọi file cho mọi task). Khi
  scaffold core BE lần đầu hoặc thêm module mới, ưu tiên `be/trien-khai/`
  (thứ tự thao tác cụ thể) hơn `be/01-…10-…` (lý thuyết) — dùng
  [be/trien-khai/08-tra-cuu-file-class.md](be/trien-khai/08-tra-cuu-file-class.md)
  để tra nhanh 1 class/interface cụ thể mà không đọc lại cả file phase.
- **`core-reviewer`**: đọc toàn bộ `be/*.md` + `fe/*.md` trước khi audit,
  đối chiếu code thật, báo cáo PASS/PARTIAL/MISSING kèm bằng chứng. Với
  review đụng tới các phase P0–P6 (BE) hoặc F0–F5 (FE), đối chiếu thêm với
  `be/trien-khai/`/`fe/trien-khai/` tương ứng — file lý thuyết (`be/01-…`/
  `fe/01-…`) nói *nên* có gì, file thực hành nói *đúng hình dạng* của nó
  (chữ ký thật, thứ tự đăng ký, test tương ứng).
