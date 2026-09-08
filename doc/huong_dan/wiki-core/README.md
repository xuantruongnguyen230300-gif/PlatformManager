---
kind: luat
scope: core
verified: 2026-09-06
---

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
Khác với [`../quy-uoc/`](../quy-uoc/) (quy ước **thực thi** hiện tại, gắn liền
code đang có), thư mục này là **kiến thức nền** ở tầm rộng hơn — bao gồm cả
những gì PlatformManager hiện tại chưa cần.

> 🔄 **LẬT 2026-09-06.** Câu trên trước đây viết *"Khác với `doc/huong_dan/quy-uoc/`
> hay `doc/huong_dan/quy-uoc/`"* — **cùng một đường dẫn lặp hai lần**, tàn dư của một
> lần sửa dở. Không có khu thứ hai nào ở vế đó.

**2 lớp nội dung cho BE, đọc theo thứ tự khác nhau tuỳ mục đích:**

- Loạt `be/NN-*.md` ở ngay gốc `be/` (liệt kê dưới đây) trả lời **"core gồm
  những gì và vì sao cần"** — lý thuyết, khái niệm, nguyên tắc Nhóm A/B.
  Đếm bằng lệnh, đừng tin dãy số chép trong câu: `ls doc/huong_dan/wiki-core/be/*.md`.
- `tham-khao-ngoai/vnr-successor/00-…08-…` trả lời **"làm thì làm theo thứ tự nào, đẻ ra
  file/class/interface nào, cấu trúc source code ra sao"** — thực hành, đối
  chiếu trực tiếp với source thật của `VNR.Successor`, không lý thuyết suông.
  Bắt đầu từ [../../tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md](../../tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md).

## Mục lục

### BE — lý thuyết (`be/`)

1. [Core components](be/01-core-components.md) — danh sách thành phần core thật sự, nguyên tắc Nhóm A/B
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
13. [Core data — di trú & seed](be/13-core-data-migration.md) — chỉ `AspNetUsers/Roles`, `SysMenus`, `RolePermissions`; expand/contract, seed production, break-glass
14. [Lưu file trên đĩa](be/14-file-storage.md) — upload/export/file mẫu, đường dẫn qua cấu hình, dọn file; **khác** #10 (dòng DB)
15. [Import & export dữ liệu](be/15-import-export.md) — `IImportFileReader`, `ITabularWriter`, bộ lọc export
16. [i18n phía BE](be/16-i18n-va-ma-loi.md) — BE sở hữu **mã**, không sở hữu câu chữ; ai dịch cái gì; những thứ **cố ý không làm** ở BE

### BE — thực hành / lộ trình triển khai (`tham-khao-ngoai/vnr-successor/`)

0. [Lộ trình tổng thể](../../tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md) — 7 phase (P0–P6), cây thư mục source đích, thứ tự phụ thuộc
1. [P0 — Nền móng solution](../../tham-khao-ngoai/vnr-successor/01-p0-nen-mong-solution.md) — `Directory.Build.props`, quy ước đặt tên, ArchTest đầu tiên
2. [P1 — `Platform.Domain`](../../tham-khao-ngoai/vnr-successor/02-p1-platform-domain.md) — `BaseEntity`, `AggregateRoot`, `Enumeration<TEnum>`, Value Object
3. [P2 — `Platform.Application`](../../tham-khao-ngoai/vnr-successor/03-p2-platform-application.md) — CQRS, 6 pipeline behavior, `IApiResult<T>`, `ErrorDescriptor`
4. [P3 — `Platform.Persistence`](../../tham-khao-ngoai/vnr-successor/04-p3-platform-persistence.md) — `BaseDbContext`, interceptor, `UnitOfWork`, DIP Seam
5. [P4 — `Hosting.Api`](../../tham-khao-ngoai/vnr-successor/05-p4-hosting-api.md) — `BaseApiController`, envelope, `ErrorCode → HTTP`, permission
6. [P5 — Module đầu tiên](../../tham-khao-ngoai/vnr-successor/06-p5-module-dau-tien.md) — 2 pattern CRUD (zero-handler vs vertical slice)
7. [P6 — ArchTests gate](../../tham-khao-ngoai/vnr-successor/07-p6-archtests-gate.md) — khi nào cần gate nào
8. [Tra cứu file/class](be/tra-cuu-file-class.md) — bảng tra thành phần **có thật** của PlatformManager (viết lại 2026-09-01), kèm mục "KHÔNG áp dụng — vì sao"

> ⚠️ **Tám file `00-…07-…` ở trên đều là `kind: tham-chieu`, mô tả VNR.Successor —
> KHÔNG phải luật của repo này.** Chỉ `08-tra-cuu-file-class.md` mang `kind: luat`.
> Đọc `kind:` ở frontmatter từng file trước khi đối chiếu code (xem §Cách dùng bên dưới);
> đừng suy từ tên thư mục. Đối chiếu 2026-09-06.

### FE — lý thuyết (`fe/`)

1. [Core components](fe/01-core-components.md) — danh sách thành phần core FE thật sự, nguyên tắc Nhóm A/B
2. [HTTP Client & Envelope](fe/02-http-envelope.md) — tiêu thụ `IApiResult<T>` từ BE
3. [State management](fe/03-state-management.md) — `signal()` → `signalStore()` có điều kiện
4. [Design-token system](fe/04-design-token-system.md) — bridge với `doc/Design/`
5. [Component library](fe/05-component-library.md) — xem `COMPONENTS.md` để biết số thật, 5 trạng thái bắt buộc
6. [Testing strategy](fe/06-testing-strategy.md) — mapper/interceptor trước, không coverage dàn trải
7. [Auth/Identity](fe/07-auth-identity.md) — cookie session của ASP.NET Core Identity
8. [i18n](fe/08-i18n.md) — `@ngx-translate/core` v18, đổi ngôn ngữ tại chỗ
9. [Forms & Validation](fe/09-forms-validation.md)
10. [Observability](fe/10-observability.md) — correlation với `traceId` của BE
11. [Grid & Metadata sync](fe/11-grid-and-metadata.md) — PrimeNG `p-table`, hợp đồng menu/cột với BE
12. [Charting](fe/12-charting.md) — PrimeNG `p-chart`, ngưỡng nâng cấp `ngx-echarts`
13. [Performance](fe/13-performance.md) — zoneless, `@defer`, virtual scroll, bundle budget
14. [Bảo mật FE](fe/14-security.md) — `DomSanitizer`, CSP, secret trong bundle, `npm audit`
15. [Accessibility](fe/15-accessibility.md) — **điểm vào duy nhất** cho a11y + bản đồ phần a11y nằm ở file nào
16. [Nền tảng & nâng cấp](fe/16-nen-tang-va-nang-cap.md) — nhịp nâng Angular/PrimeNG, `browserslist`
17. [Phục vụ & triển khai](fe/17-phuc-vu-va-trien-khai.md) — static file, SPA fallback, cache header

### FE — thực hành / lộ trình triển khai (`fe/trien-khai/`)

0. [Lộ trình tổng thể](../../tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md) — 5 giai đoạn F0–F3 + Gate, hình dạng giống P0–P6 của BE vì `src/FE` viết mới từ số 0
1. [F0 — Nền móng](fe/trien-khai/01-f0-nen-mong.md) — `ng new` zoneless, cây thư mục, `core/http` + interceptor
2. [F1 — Design token → code](fe/trien-khai/02-f1-design-token.md) — đổ token từ `DESIGN.md` vào `styles.scss`, preset PrimeNG
3. [F2 — Auth + routing/guard](fe/trien-khai/03-f2-auth-routing.md) — `CurrentUserService`, 3 guard, màn đăng nhập/đổi mật khẩu
4. [F3 — Hai màn quản trị Core](fe/trien-khai/04-f3-man-quan-tri.md) — quản trị người dùng, phân quyền
5. [Gate](fe/trien-khai/05-gate.md) — lint rule + check tương đương ArchTest

> **Nguồn tham chiếu khác BE:** `fe/` không có "VNR.Successor frontend" để
> đối chiếu — nguồn là kiến trúc chính thức của Angular, hệ thống thiết kế
> thật của chính PlatformManager (`doc/Design/Frontend/PlatformManager/`),
> và các quyết định đã chốt trực tiếp với người dùng (2026-08-15) khi đối
> chiếu với BE. Xem [fe/01-core-components.md](fe/01-core-components.md)
> § đầu file.

## Cách dùng

- **`backend-expert`/`frontend-expert`**: đọc file tương ứng chủ đề đang làm
  khi implement phần core (không cần đọc hết mọi file cho mọi task). Khi
  scaffold core BE lần đầu hoặc thêm module mới, ưu tiên `tham-khao-ngoai/vnr-successor/`
  (thứ tự thao tác cụ thể) hơn loạt `be/NN-*.md` (lý thuyết) — dùng
  [be/tra-cuu-file-class.md](be/tra-cuu-file-class.md)
  để tra nhanh 1 class/interface cụ thể mà không đọc lại cả file phase.
- **`core-reviewer`**: đọc file theo chủ đề đang audit, đối chiếu code thật, báo cáo
  PASS/PARTIAL/MISSING kèm bằng chứng.

  ⚠️ **Lọc theo `kind:` TRƯỚC khi đối chiếu — đây là bước bắt buộc, không phải tối ưu.**
  Chỉ file `kind: luat` mới là thứ code phải tuân. File `kind: tham-chieu` mô tả **một dự
  án khác** (loạt `tham-khao-ngoai/vnr-successor/` ánh xạ từ VNR.Successor qua bảng dịch) — đối chiếu code
  repo này với chúng sẽ sinh ra một loạt MISSING cho những yêu cầu **chưa bao giờ là luật
  ở đây**. Chuyện này đã xảy ra thật ngày 2026-09-01.

  ```bash
  grep -l "^kind: luat" $(find doc/huong_dan/wiki-core -name '*.md')
  ```

  Với review đụng phase P0–P6 (BE) hoặc F0–F3 + Gate (FE), file `trien-khai/` tương ứng
  vẫn hữu ích — nhưng đọc nó như **gợi ý hình dạng** (chữ ký thật, thứ tự đăng ký, test
  tương ứng), không như tiêu chí PASS/FAIL. File lý thuyết (`be/01-…`/`fe/01-…`) nói *nên*
  có gì; chỉ nó mới chấm điểm được.

  > **🔄 LẬT 2026-09-05.** Bản trước dặn *"đọc **toàn bộ** `be/*.md` + `fe/*.md` trước khi
  > audit"*. Hai vấn đề: (1) nó mâu thuẫn với §9 `.claude/CLAUDE.md` — ba khoá phân loại
  > tồn tại chính là để **không** phải đọc tất cả và để phân biệt luật với tham chiếu; (2)
  > "đọc toàn bộ" là đường ngắn nhất tới cạn context giữa chừng, đúng cách hỏng đã ghi ở
  > §3 `.claude/CLAUDE.md` (corpus 780 KB, 3 lượt review chết giữa chừng).
