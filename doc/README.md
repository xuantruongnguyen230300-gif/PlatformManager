---
kind: luat
scope: core
verified: 2026-09-06
---

# `doc/` — nguồn tri thức DUY NHẤT của PlatformManager

> **Đây là mục lục cấp cao nhất.** Không tìm thấy chủ đề trong bảng định tuyến
> của agent → tra ở đây rồi mở **đúng một** file. Đừng đọc cả thư mục.
>
> Ranh giới ba khu: `.claude/` giữ **quy trình**, `doc/` giữ **quy tắc**, `spec/`
> giữ **nghiệp vụ theo feature**. Luật đầy đủ ở
> [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §2–§3. Khi có thay đổi: **nội dung
> vào `doc/`**, `.claude/` chỉ sửa khi **đường dẫn** đổi.

## Tra theo chủ đề

| Đang làm gì | Đọc file nào |
| --- | --- |
| **Layer rule, dependency, project layout BE** | [`huong_dan/quy-uoc/be-architecture.md`](huong_dan/quy-uoc/be-architecture.md) |
| **Entity, soft delete, Value Object, `RowVersion`** | [`huong_dan/quy-uoc/be-entity-domain.md`](huong_dan/quy-uoc/be-entity-domain.md) |
| **Command/Query, Handler, Validator, `ErrorDescriptor`** | [`huong_dan/quy-uoc/be-cqrs-handler.md`](huong_dan/quy-uoc/be-cqrs-handler.md) |
| **Controller, envelope, error → HTTP, rate limit, phân quyền** | [`huong_dan/quy-uoc/be-api-controller.md`](huong_dan/quy-uoc/be-api-controller.md) |
| **Repository, query, index, N+1, cache** | [`huong_dan/quy-uoc/be-performance.md`](huong_dan/quy-uoc/be-performance.md) |
| **Lưu file: upload, export, file mẫu, dọn file** | [`huong_dan/wiki-core/be/14-file-storage.md`](huong_dan/wiki-core/be/14-file-storage.md) |
| **Import/Export: định dạng file, bộ lọc export, endpoint** | [`huong_dan/wiki-core/be/15-import-export.md`](huong_dan/wiki-core/be/15-import-export.md) |
| **Cái gì được commit: artifact build, App_Data, secret** | [`huong_dan/quy-uoc/repo-artifact.md`](huong_dan/quy-uoc/repo-artifact.md) |
| **Tầng `core`/`modules`/`shared`, cấu trúc 1 feature FE** | [`huong_dan/quy-uoc/fe-architecture.md`](huong_dan/quy-uoc/fe-architecture.md) |
| **Gọi API, ranh giới DTO/model, mapper** | [`huong_dan/quy-uoc/fe-api-client.md`](huong_dan/quy-uoc/fe-api-client.md) |
| **Control flow Angular 20, form, responsive, style theo token** | [`huong_dan/quy-uoc/fe-ui-conventions.md`](huong_dan/quy-uoc/fe-ui-conventions.md) |
| **Route, lazy-load, guard auth/role, `mustChangePassword`, state trên URL** | [`huong_dan/quy-uoc/fe-routing-guard.md`](huong_dan/quy-uoc/fe-routing-guard.md) |
| **Bảo mật FE: render HTML không tin cậy, CSP, secret trong bundle** | [`huong_dan/wiki-core/fe/14-security.md`](huong_dan/wiki-core/fe/14-security.md) |
| **Accessibility — mức chuẩn, checklist màn hình mới** | [`huong_dan/wiki-core/fe/15-accessibility.md`](huong_dan/wiki-core/fe/15-accessibility.md) |
| **Phục vụ FE: static file, SPA fallback, cache header** | [`huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md`](huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md) |
| **Nâng cấp Angular/PrimeNG, trình duyệt hỗ trợ** | [`huong_dan/wiki-core/fe/16-nen-tang-va-nang-cap.md`](huong_dan/wiki-core/fe/16-nen-tang-va-nang-cap.md) |
| **Ranh giới Core ↔ Business, khi nào tách module** | [`kien-truc-core-module.md`](kien-truc-core-module.md) |
| **Giao diện: layout, copy, token, component, ảnh màn hình** | [`Design/`](Design/) — nguồn UI **duy nhất**, cả FE lẫn BE |
| **Hợp đồng API một endpoint cụ thể** | [`contracts/`](contracts/) |
| **Schema thật, bảng/cột/index đang chạy** | [`cau-truc-database.md`](cau-truc-database.md) — schema `core`; bảng nghiệp vụ ở [`cau-truc-database-business.md`](cau-truc-database-business.md) |
| **Dựng DB từ trống (chạy trong DBeaver)** | [`db-khoi-tao.sql`](db-khoi-tao.sql) — sinh tự động từ `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0001_initial_baseline.sql`, đừng sửa tay. Sau đó chạy lệnh seed: `dotnet run --project src/BE/PlatformManager.Api -- --seed` |
| **Chấm review: cái gì là finding, cái gì không** | [`huong_dan/quy-uoc/tieu-chi-review.md`](huong_dan/quy-uoc/tieu-chi-review.md) |
| **"Core đủ chưa, còn thiếu mảng nào"** | [`huong_dan/wiki-core/be/01-core-components.md`](huong_dan/wiki-core/be/01-core-components.md) §Áp dụng |
| **Kiến thức nền về core (chuẩn chung, không riêng dự án)** | [`huong_dan/wiki-core/`](huong_dan/wiki-core/) |

> 🔄 **LẬT 2026-09-06 — cột nhãn trạng thái đã bị gỡ khỏi bảng trên.** Bảng này từng gắn `🚧`/`📐`
> vào bảy dòng. Đối chiếu ngày 2026-09-06: **năm trong bảy** đã sai — `be/14-file-storage.md` nay
> tự khai `📐 ĐÍCH ĐẾN` (không phải `🚧`), còn `fe/14-security.md`, `fe/16-nen-tang-va-nang-cap.md`,
> `repo-artifact.md` và phần lớn `fe/15-accessibility.md` **không còn nhãn `🚧` nào** vì việc đã
> làm xong. Đây đúng khuôn §5 `.claude/CLAUDE.md` cấm: trạng thái của một file được chép ra chỗ
> thứ hai, và chỗ thứ hai không bao giờ được sửa cùng lúc.
>
> **Trạng thái của một file đọc ở ĐẦU CHÍNH FILE ĐÓ**, cùng với khoá `verified:` ở frontmatter.
> Bảng dưới (§Bảng trạng thái) chỉ giữ nhãn ở **cấp khu** và cho những file thật sự đã chết —
> loại trạng thái không đổi mỗi lần ai đó đóng một mục.

## Các khu — mỗi khu một vai

| Khu | Vai | Ai đọc |
| --- | --- | --- |
| [`huong_dan/quy-uoc/`](huong_dan/quy-uoc/) | **Quy ước ĐANG THỰC THI** — PlatformManager thật sự làm thế nào. Có code mẫu. | `backend-expert`, `frontend-expert` khi viết code |
| [`huong_dan/wiki-core/`](huong_dan/wiki-core/) | **Kiến thức nền** — một core tốt gồm gì và vì sao. Có thể vượt nhu cầu dự án. | `core-reviewer` khi audit |
| [`Design/`](Design/) | **Giao diện** — token, component spec, screen spec, prompt pack, ảnh. **Ngoại lệ có chủ đích: phủ CẢ màn Core lẫn màn nghiệp vụ** | `design-expert`, `frontend-expert` |
| [`contracts/`](contracts/) + [`cau-truc-database.md`](cau-truc-database.md) | **Hợp đồng & dữ liệu** — endpoint, schema | cả hai phía |
| `spec/` *(ngoài `doc/`)* | **Nghiệp vụ theo feature** — `business-rules.md`, `ui-spec.md`. Đặt ngoài `doc/` là **có chủ đích** | `backend-expert`/`frontend-expert` khi làm nghiệp vụ |

> **`doc/` giữ QUY TẮC, `spec/` giữ NGHIỆP VỤ.** Quy tắc kiến trúc/code luôn ở
> `doc/`, và agent phải tuân thủ chúng kể cả khi đang làm việc thuộc `spec/`.
> `doc/Design/` là ngoại lệ duy nhất của ranh giới Core↔nghiệp vụ — xem
> [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §2.

Khoảng cách giữa `quy-uoc/` và `wiki-core/` là **cố ý**: nó cho phép `core-reviewer`
phân biệt *"lệch vì đã cố ý đơn giản hoá"* (không phải lỗi) với *"lệch vì thiếu
sót thật"* (là finding). Đừng gộp hai khu này.

## Bảng trạng thái — đọc trước khi tin

Theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4, mọi tuyên bố về hiện trạng
phải mang nhãn. Ở cấp file:

| File / khu | Trạng thái |
| --- | --- |
| [`huong_dan/quy-uoc/`](huong_dan/quy-uoc/) | ✅ sống — quy ước đang áp dụng |
| [`Design/`](Design/) | ✅ sống — nguồn UI duy nhất |
| [`contracts/`](contracts/) | ✅ sống — nhưng đọc `Status:` ở đầu **từng** card (`DRAFT` / `AGREED` / `IMPLEMENTED`); `DRAFT` nghĩa là BE **chưa** cam kết làm |
| [`cau-truc-database.md`](cau-truc-database.md) | ✅ sống (2026-09-03) — schema **`core`**, `scope: core`. Tách khỏi phần nghiệp vụ 2026-09-03 để khoá `scope` khai được cho đúng |
| [`cau-truc-database-business.md`](cau-truc-database-business.md) | 🗄️ đóng băng (2026-09-03) — schema **`business`**, `scope: du-an`. Không code nào sở hữu; chờ module DTI dựng lại |
| [`cau-truc-database.sql`](cau-truc-database.sql) | 🗄️ lịch sử — DDL viết tay EF không sinh được, nay toàn bộ là `business` |
| [`kien-truc-core-module.md`](kien-truc-core-module.md) | 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Layout `Core.*`/`Business.*` là **đích đến**, chưa phải hiện trạng. Đọc bảng đối chiếu ở đầu file trước khi tạo project mới |
| ~~`ERD/`~~ | 🗑️ **đã xoá 2026-08-23.** 6 nguồn mô tả schema mâu thuẫn nhau (6 con số bảng khác nhau, 2 bộ tên cột `BaseEntity`) đã gộp về `cau-truc-database.md` |
| [`ke-hoach-xay-lai-corebase.md`](ke-hoach-xay-lai-corebase.md) | 🗄️ **lịch sử, đã thực thi xong.** Không dùng làm mô tả hiện trạng |

**Nguồn chuẩn cho schema:** [`cau-truc-database.md`](cau-truc-database.md) (schema `core`) +
[`cau-truc-database-business.md`](cau-truc-database-business.md) (schema `business`) +
[`cau-truc-database.sql`](cau-truc-database.sql) (DDL viết tay). Không còn nguồn nào khác.
Ba file, hai schema, **một** database — xem `cau-truc-database.md` §1.1.

## Trước khi coi một thay đổi tài liệu là xong

```bash
bash .claude/check-docs.sh
```

Gate chặn hồi quy: đường dẫn trích dẫn tồn tại, link resolve, tuyên bố hoàn thành
có ngày, `.claude/` không lẫn tri thức. **PASS không có nghĩa là nội dung đúng** —
xem [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §8 để biết ba loại lỗi gate không
bao giờ bắt được.
