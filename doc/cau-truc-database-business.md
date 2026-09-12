---
kind: lich-su
scope: du-an
verified: khong-ap-dung
---

# Cấu trúc Database — schema `business` (PlatformManager)

> ### 📕 TÀI LIỆU LỊCH SỬ — 5 bảng dưới đây KHÔNG còn trong code (2026-09-03)
>
> | Trong file này | Thực tế hôm nay |
> | --- | --- |
> | 5 bảng `business.*` của module DTI Weekly | **Không bảng nào trong 5 bảng đó còn trong code.** ⚠️ Lệnh đếm cũ ở ô này (`grep -c 'ToTable(…, "business")'` trên `ModelSnapshot`) nay trả **4**, KHÔNG phải `0` — nhưng 4 đó là **bộ bảng MỚI** (2026-09-10), không phải 5 bảng cũ. Lệnh ấy đếm "có bao nhiêu bảng ở schema `business`", nó **không** phân biệt được bộ cũ với bộ mới, nên nó đã thôi chống lưng được cho câu này. Phép kiểm đúng: 3 lệnh ở [`cau-truc-database-dti.md`](cau-truc-database-dti.md) §"Ba lệnh tự-kiểm", cộng `grep -rn "class CriteriaEvidence" src/BE --include=*.cs \| grep -v /obj/` → **rỗng** (bảng thứ năm không dựng lại). Chữ `class` là bắt buộc: mẫu trần khớp cả hai CHÚ THÍCH đang nhắc tên nó, tức trả `2` cho một cây mã hoàn toàn đúng |
> | `src/BE/Modules/DtiWeekly.*` | Thư mục **đã xoá 2026-09-08** — module gỡ 2026-08-29, phần rác build còn lại dọn nốt; xem `doc/kien-truc-core-module.md:42` |
> | Baseline `0001_initial_baseline.sql` | Chỉ dựng **11 bảng `core`**, không bảng `business` nào |
>
> Giữ lại vì hai lý do, không phải vì tiếc: (1) `doc/cau-truc-database.sql` vẫn còn DDL thật của
> `business.criteria_assessment_date_utc` và unique index trên `CriteriaAssessments` — chạy nó lên
> DB hôm nay sẽ lỗi, và người mở file `.sql` cần biết vì sao; (2) đây là **ví dụ đã có thật** về
> hình dạng một schema nghiệp vụ, dùng làm mẫu khi dự án 2 thêm bảng đầu tiên.
>
> Nguồn sống về ranh giới schema: [`cau-truc-database.md`](cau-truc-database.md) §1.1.
>
> ### ➡️ Thiết kế THAY THẾ đã có (2026-09-06), và đã THI CÔNG (2026-09-10)
>
> Bộ bảng `business.*` được thiết kế lại cho đợt dựng lại DTI, và nay đã thành entity +
> migration thật. **Schema `business` hôm nay:**
> [`cau-truc-database-dti.md`](cau-truc-database-dti.md). **Đặc tả từng cột:**
> [`../spec/danh-muc-dti/business-rules.md`](../spec/danh-muc-dti/business-rules.md) §1.
>
> File bạn đang đọc **không** mô tả bộ bảng mới, và cố ý không bao giờ mô tả — nó chỉ giữ 5 bảng
> đã chết.
>
> Ba khác biệt lớn nhất so với 5 bảng cũ dưới đây, nêu ra để người đọc không chép nhầm:
>
> | Bảng/khái niệm cũ | Trong thiết kế mới |
> | --- | --- |
> | `CriteriaEvidences` (nhiều dòng minh chứng) | **bỏ hẳn** — `Minh chứng/Ghi chú` là MỘT ô text trên bản ghi đánh giá |
> | Kỳ suy ra từ phần ngày của `DateCreate`, kèm hàm SQL `IMMUTABLE` viết tay để index được | cột **`AssessmentDate`** riêng kiểu `date` — EF Core index thẳng, không cần hàm SQL tay |
> | Chỉ có `ProgressPercent` | thêm **`SelfScore`** · **`VerifiedScore`**; `Chênh lệch` là trường **TÍNH** (`Thẩm định − Tự đánh giá`), **không lưu** |

## ➡️ 4 bảng ĐANG SỐNG — đã tách sang file chủ riêng (2026-09-10)

📖 Schema `business` hôm nay (4 bảng của cụm DTI, ràng buộc, hai script `.sql`, ba lệnh tự-kiểm):
đọc [`cau-truc-database-dti.md`](cau-truc-database-dti.md).

**Vì sao tách** (quyết định người dùng 2026-09-10): file bạn đang đọc mang banner
`TÀI LIỆU LỊCH SỬ`, và banner đó **miễn trừ file khỏi mục 4/5/6 của `check-docs.sh`**
([`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §5). Miễn trừ ấy đúng cho 5 bảng đã chết bên
dưới — không còn source để đối chiếu — nhưng **sai cho phần đang sống**, và cái giá lộ ra ngay
lượt đầu: ba lệnh tự-kiểm viết ở đây đều cho kết quả mâu thuẫn với câu văn ngay trên chúng, mà
không có gì báo. Bốn bảng đó còn bị tra cứu suốt vòng 2 (import DM-7, export DB-4) nên chúng phải
nằm trong vùng gate kiểm được.

Giữ **một dòng trỏ đường** thay vì hai bản mô tả — `.claude/CLAUDE.md` §5.

## Vì sao file này tách khỏi file chủ schema `core`

Khoá `scope` của [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §9 là khoá **của cả file**, không phải
của từng mục. Một file mô tả lẫn bảng `core` (đi theo khi tách CoreBase) và bảng nghiệp vụ (ở lại
dự án) thì **không khai được giá trị đúng** cho khoá đó — bản trước khai `scope: du-an` cho cả 11
bảng `core`, tức nói sai về chính phần quan trọng nhất khi tách CoreBase.

Tách đôi làm mỗi file trả lời được một câu rõ ràng: *"bảng này có đi theo CoreBase không"*.

## Danh sách bảng

### Schema `business`

| Bảng | Mô tả |
|---|---|
| `CriteriaGroups` | Nhóm chỉ tiêu đánh giá DTI Weekly |
| `Criteria` | Chỉ tiêu đánh giá cụ thể, thuộc 1 `CriteriaGroups` |
| `CriteriaAssessments` | Kết quả đánh giá 1 `Criteria` theo từng kỳ (phần ngày của `DateCreate` = kỳ). ✅ Optimistic concurrency (2026-08-24, migration `0006_criteria_assessment_row_version.sql` — **file này không còn tồn tại**, đã gộp vào `0001_initial_baseline.sql` khi baseline lại lịch sử 2026-08-31) — property CLR `Version` (`uint`) bind thẳng vào cột hệ thống `xmin` có sẵn của Postgres, KHÔNG tạo cột thật nào (xem `doc/huong_dan/quy-uoc/be-entity-domain.md` §RowVersion) |
| `CriteriaEvidences` | Minh chứng đính kèm 1 `CriteriaAssessments` (nhiều dòng/bản ghi) |
| `ImportJobs` | ✅ **Trạng thái job import CSV/Excel chạy nền qua Hangfire** (đã thi công 2026-08-24; mục §2.1 mô tả nó nằm ở bản trước khi tách file, nay **đã gỡ cùng module**). Cột: `Id` (uuid, PK), `FileName` `varchar(260)` NOT NULL, `Format` `varchar(20)` NOT NULL, `StoragePath` `varchar(1000)` NOT NULL, `Status` `varchar(20)` NOT NULL, `ResultJson` `text`, `ErrorMessage` `text` + 5 field còn lại của `BaseEntity` (`Id` đã liệt kê ở
đầu, `BaseEntity` có 6 field tổng cộng). Index `IX_ImportJobs_Status` phục vụ endpoint poll `GET /api/import/{jobId}`. **Không có FK nào** — job độc lập với dữ liệu nó ghi ra |

> `ImportJobs` là bảng *trạng thái tiến trình*, khác 4 bảng còn lại (dữ liệu
> nghiệp vụ). Nó lưu **đường dẫn** file tạm (`StoragePath`), không lưu nội
> dung file — file upload không sống sót qua ranh giới request→job nền, xem
> `doc/huong_dan/quy-uoc/be-cqrs-handler.md` § "Command chạy lâu → job nền".

## Quan hệ xuyên schema

Chỉ 1 khoá ngoại đi từ `business` sang `core` (đúng chiều — nghiệp vụ được
phép biết về Core, Core không được biết về nghiệp vụ):

```
business.CriteriaAssessments.OwnerId → core.AspNetUsers.Id
```

Không có FK nào đi chiều ngược lại (`core` → `business`) — khớp đúng luật
"Core không được biết về Business" đã chốt ở tầng code.

> **Ghi chú 2026-09-03.** Chính các khoá ngoại liệt ở trên là lý do quyết định giữ **một database
> hai schema** thay vì hai database: Postgres không khai được khoá ngoại xuyên database. Xem
> [`cau-truc-database.md`](cau-truc-database.md) §1.1.
