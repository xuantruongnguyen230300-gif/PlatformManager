---
kind: lich-su
scope: du-an
verified: 2026-09-03
---

# Cấu trúc Database — schema `business` (PlatformManager)

> ### 📕 TÀI LIỆU LỊCH SỬ — 5 bảng dưới đây KHÔNG còn trong code (2026-09-03)
>
> | Trong file này | Thực tế hôm nay |
> | --- | --- |
> | 5 bảng `business.*` của module DTI Weekly | **Không entity nào còn khai schema `business`** — đo: `grep -c 'ToTable("[A-Za-z]*", "business")' src/BE/PlatformManager.Api/Persistence/Migrations/PlatformManagerDbContextModelSnapshot.cs` → `0` |
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
> ### ➡️ Thiết kế THAY THẾ đã có (2026-09-06) — đọc trước khi viết migration
>
> Bộ bảng `business.*` được thiết kế lại cho đợt dựng lại DTI. **Đặc tả đầy đủ — thực thể,
> từng cột, kiểu, ràng buộc, khoá ngoại xuyên schema, chỉ mục — nằm ở
> [`../spec/danh-muc-dti/business-rules.md`](../spec/danh-muc-dti/business-rules.md) §1 "Mô hình
> dữ liệu".** File bạn đang đọc **không** được cập nhật theo: nó mô tả *schema đang chạy*, mà
> hôm nay schema `business` **rỗng** — nên nó chỉ trở lại thành nguồn sống sau khi migration
> đầu tiên chạy.
>
> Ba khác biệt lớn nhất so với 5 bảng cũ dưới đây, nêu ra để người đọc không chép nhầm:
>
> | Bảng/khái niệm cũ | Trong thiết kế mới |
> | --- | --- |
> | `CriteriaEvidences` (nhiều dòng minh chứng) | **bỏ hẳn** — `Minh chứng/Ghi chú` là MỘT ô text trên bản ghi đánh giá |
> | Kỳ suy ra từ phần ngày của `DateCreate`, kèm hàm SQL `IMMUTABLE` viết tay để index được | cột **`AssessmentDate`** riêng kiểu `date` — EF Core index thẳng, không cần hàm SQL tay |
> | Chỉ có `ProgressPercent` | thêm **`SelfScore`** · **`VerifiedScore`**; `Chênh lệch` là trường **TÍNH** (`Thẩm định − Tự đánh giá`), **không lưu** |

## 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG: 4 bảng `business` của đợt dựng lại (khai 2026-09-09)

**Chưa có dòng code nào**, và cũng chưa có migration nào — kiểm bằng chính lệnh ở banner
trên (`grep -c 'ToTable("[A-Za-z]*", "business")' …ModelSnapshot.cs` → `0`). Mục này khai
**bảng nào sẽ có, ở schema nào, entity thuộc project nào**; nó cố ý **không** chép lại từng
cột.

| Bảng (schema `business`) | Entity ở | Đặc tả từng cột — file chủ |
| --- | --- | --- |
| `CriteriaGroups` | `PlatformManager.Business.Domain` | [`../spec/danh-muc-dti/business-rules.md`](../spec/danh-muc-dti/business-rules.md) §1.1 |
| `Criteria` | `PlatformManager.Business.Domain` | cùng file, §1.2 |
| `CriteriaAssessments` | `PlatformManager.Business.Domain` | cùng file, §1.3 |
| `ImportJobs` | `PlatformManager.Business.Domain` | bộ cột giữ nguyên như hàng `ImportJobs` ở §Danh sách bảng dưới đây |

**Ba luật khai bảng, cả ba đều hỏng im lặng nếu quên:**

1. **Khai schema TƯỜNG MINH trong `ToTable("<Tên>", "business")`.** Entity không khai schema
   rơi vào `core` — [`cau-truc-database.md`](cau-truc-database.md) §1.1. Triệu chứng không
   phải lỗi biên dịch mà là một bảng nghiệp vụ nằm lẫn trong schema đi theo CoreBase.
2. **EF Configuration đặt ở `PlatformManager.Business.Persistence`**, để nó vào model qua
   `PersistenceAssembly` của registrar tầng nghiệp vụ — cơ chế ở
   [`kien-truc-core-module.md`](kien-truc-core-module.md) §`IModuleRegistrar`. Đặt nhầm sang
   `Core.Persistence` là kéo entity nghiệp vụ vào Core.
3. **`ImportJobs` là bảng NGHIỆP VỤ (Q11, chốt 2026-09-09)** — không phải bảng Core, dù nó
   lưu *trạng thái tiến trình* chứ không lưu dữ liệu nghiệp vụ. Core chỉ giữ **cơ chế** chạy
   job (`IBackgroundJobScheduler`) và lưu file; bảng theo dõi thuộc tầng nghiệp vụ dùng nó.
   Chốt này khớp [`cau-truc-database.md`](cau-truc-database.md) (đã trỏ `ImportJobs` sang
   file này từ trước) và
   [`huong_dan/wiki-core/be/15-import-export.md`](huong_dan/wiki-core/be/15-import-export.md)
   §1 (sửa cùng ngày — bản trước xếp "theo dõi trạng thái" nhầm vào cột Core).

**Khoá ngoại xuyên schema — đúng một, đúng một chiều:**

```
business."CriteriaAssessments"."OwnerId"  →  core."AspNetUsers"."Id"
```

Không có FK nào đi ngược `core → business`. Đây cũng là lý do hai schema ở chung **một**
database (Postgres không khai được FK xuyên database) — [`cau-truc-database.md`](cau-truc-database.md) §1.1.

**`CriteriaEvidences` KHÔNG dựng lại** — bảng thứ năm của thiết kế cũ bị bỏ hẳn, `Minh
chứng/Ghi chú` là **một ô text** trên `CriteriaAssessments` (bảng khác biệt ở banner trên).

> **Mục này trở thành ✅ khi nào:** sau khi migration đầu tiên chạy, cập nhật §Danh sách bảng
> bên dưới cho khớp schema thật rồi đổi `kind` của file khỏi `lich-su`. Đừng đổi nhãn trước
> lúc đó — [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4.

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
