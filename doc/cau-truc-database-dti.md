---
kind: quyet-dinh
scope: du-an
verified: 2026-09-11
---

# Cấu trúc Database — 4 bảng `business` của cụm DTI

**File chủ** cho câu hỏi *"schema `business` hôm nay gồm bảng nào, entity ở project nào, ràng
buộc nào đã vào migration"*.

Tách khỏi [`cau-truc-database-business.md`](cau-truc-database-business.md) ngày 2026-09-10 theo
quyết định người dùng. File kia mô tả **5 bảng ĐÃ CHẾT** của module `DtiWeekly` gỡ 2026-08-29 và
mang banner `TÀI LIỆU LỊCH SỬ`; banner đó **miễn trừ file khỏi mục 4/5/6 của `check-docs.sh`**
(`.claude/CLAUDE.md` §5). Để phần đang sống trong vùng miễn trừ ấy đã trả giá ngay trong lượt đầu
tiên: **cả ba lệnh tự-kiểm** viết ở đó đều sai mà không có gì báo — chi tiết ở §"Ba lệnh tự-kiểm"
bên dưới. Bốn bảng này sẽ bị tra cứu suốt vòng 2 (import DM-7, export DB-4), nên chúng phải nằm
trong vùng gate kiểm được.

| Câu hỏi | File chủ |
| --- | --- |
| Schema `business` hôm nay có gì | **file này** |
| Đặc tả TỪNG CỘT của từng bảng | [`../spec/danh-muc-dti/business-rules.md`](../spec/danh-muc-dti/business-rules.md) §1 |
| Schema `core`, quy trình dựng lại DB, chốt "migration thuộc host" | [`cau-truc-database.md`](cau-truc-database.md) |
| 5 bảng cũ đã gỡ (lịch sử) | [`cau-truc-database-business.md`](cau-truc-database-business.md) |
| Ranh giới Core ↔ Business ở tầng code | [`kien-truc-core-module.md`](kien-truc-core-module.md) |

---

## 🚧 ĐÃ CHỐT — ĐANG THI CÔNG (khai 2026-09-09, thi công 2026-09-10)

**Entity + EF Configuration + migration đã có trong mã nguồn; schema thì CHƯA áp lên database
nào.** Phân biệt hai vế đó là toàn bộ ý nghĩa của nhãn `🚧` ở đây — ở repo này migration là **cỗ
máy tính delta**, không phải đường áp schema ([`cau-truc-database.md`](cau-truc-database.md)
§5.3), nên *"migration đã sinh"* và *"bảng đã tồn tại"* là hai sự thật khác nhau.

| Có thật hôm nay (2026-09-10) | Sẽ thành |
| --- | --- |
| 4 entity ở `PlatformManager.Business.Domain`, 4 `IEntityTypeConfiguration<T>` ở `Business.Persistence/Configurations/` | không đổi |
| 2 migration ở `PlatformManager.Api/Persistence/Migrations/` — `Up` của cả hai **không có** lệnh `DropTable` nào | không đổi |
| 2 script delta ở `Business/PlatformManager.Business.Persistence/Migrations/sql/` | **được chạy tay** lên Postgres, theo đúng thứ tự |
| Schema `business` trên mọi database: **rỗng** | 4 bảng, rồi 6 nhóm chỉ tiêu qua `--seed` |
| `PostgresFixture` chưa đọc hai script này ⇒ integration test chưa có schema `business` | nối vào cùng lượt viết `Business.IntegrationTests` |

**Mục này trở thành ✅ khi nào:** sau khi hai script được **chạy thật** lên Postgres, cập nhật
§"Bốn bảng" cho khớp schema thật rồi đổi `kind` sang `luat`. Đừng đổi nhãn trước lúc đó —
[`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4. Lượt 2026-09-10 CỐ Ý dừng ở `🚧`: nó sinh ra
migration và script, nhưng `dotnet ef database update` bị chặn bằng máy và việc áp schema thuộc
người dùng.

---

## Bốn bảng, và file chủ của từng bộ cột

Bảng dưới đây chỉ nói *bảng nào ở đâu*; **đặc tả từng cột nằm ở file luật nghiệp vụ**, không chép
lại (một chủ đề, một file chủ).

| Bảng (schema `business`) | Entity ở | Đặc tả từng cột — file chủ |
| --- | --- | --- |
| `CriteriaGroups` | `PlatformManager.Business.Domain` | [`../spec/danh-muc-dti/business-rules.md`](../spec/danh-muc-dti/business-rules.md) §1.1 |
| `Criteria` | `PlatformManager.Business.Domain` | cùng file, §1.2 |
| `CriteriaAssessments` | `PlatformManager.Business.Domain` | cùng file, §1.3 |
| `ImportJobs` | `PlatformManager.Business.Domain` | cùng file, §1.5 |

**Ba luật khai bảng, cả ba đều hỏng im lặng nếu quên:**

1. **Khai schema TƯỜNG MINH trong `ToTable`.** Entity không khai schema rơi vào `core`
   ([`cau-truc-database.md`](cau-truc-database.md) §1.1). Triệu chứng không phải lỗi biên dịch mà
   là một bảng nghiệp vụ nằm lẫn trong schema đi theo CoreBase. Canh bằng `SchemaBoundaryTests`.
2. **EF Configuration đặt ở `PlatformManager.Business.Persistence`**, để nó vào model qua
   `PersistenceAssembly` của registrar — cơ chế ở
   [`kien-truc-core-module.md`](kien-truc-core-module.md) §`IModuleRegistrar`. Đặt nhầm sang
   `Core.Persistence` là kéo entity nghiệp vụ vào Core.
3. **`ImportJobs` là bảng NGHIỆP VỤ** (Q11, chốt 2026-09-09) — không phải bảng Core, dù nó lưu
   *trạng thái tiến trình* chứ không lưu dữ liệu nghiệp vụ. Core chỉ giữ **cơ chế** chạy job
   (`IBackgroundJobScheduler`) và lưu file; bảng theo dõi thuộc tầng nghiệp vụ dùng nó.

**Khoá ngoại xuyên schema — đúng một, đúng một chiều:**

```
business."CriteriaAssessments"."OwnerId"  →  core."AspNetUsers"."Id"
```

Không có FK nào đi ngược `core → business`. Đây cũng là lý do hai schema ở chung **một** database
(Postgres không khai được FK xuyên database) — [`cau-truc-database.md`](cau-truc-database.md) §1.1.

**Cột thêm sau migration đầu** — bảng cột đầy đủ ở `spec/danh-muc-dti/business-rules.md` §1.5,
đây chỉ ghi những cột KHÔNG có trong `0002` để người đọc biết phải chạy thêm script nào:

| Bảng | Cột | Script | Vì sao thêm |
| --- | --- | --- | --- |
| `ImportJobs` | `ErrorCode` `character varying(100)` NULL | `0004_import_jobs_error_code.sql` | Q75 (2026-09-11) — nhánh `Failed` cần một `businessCode` để FE dịch; trước đó chỉ có `ErrorMessage` dev-facing, nên lỗi cả file không hiển thị được |

**Ràng buộc đã vào migration** (§1.4 của file luật là nguồn; đây chỉ ghi chúng ĐÃ được sinh ra):

| Ràng buộc | Tên trong DB |
| --- | --- |
| Unique partial `WHERE "IsDeleted" = false` cho mã nhóm và mã chỉ tiêu | `IX_CriteriaGroups_Code_Active` · `IX_Criteria_Code_Active` |
| 1 đánh giá / 1 chỉ tiêu / 1 ngày — unique partial | `UX_CriteriaAssessments_CriteriaId_AssessmentDate` |
| Sắp mã tự nhiên; cột `CodeSortKey` mang `COLLATE "C"` | `IX_Criteria_CodeSortKey` |
| Lọc theo khoảng ngày của Dashboard — partial, thêm 2026-09-10 | `IX_CriteriaAssessments_AssessmentDate` |
| `TargetWeekEnd` luôn là Chủ nhật | `CK_ImportJobs_TargetWeekEnd_Sunday` |
| Poll job theo trạng thái | `IX_ImportJobs_Status` |

**Bốn điều dễ hiểu sai, ghi ra để lượt sau không "dọn nhầm":**

1. **KHÔNG có index cho `NameNormalized`** — Q59 gỡ nó có chủ đích: khớp CHUỖI CON không dùng
   được btree, và index trigram cần extension mà Q47 đã loại.
2. **KHÔNG có cột `Version` thật trên `CriteriaAssessments`.** Property CLR `uint Version` +
   `.IsRowVersion()` bind thẳng vào cột hệ thống `xmin` của Postgres — migration sinh ra
   `xmin … type: "xid", rowVersion: true`, không tạo cột mới. Thấy một cột `bytea` tên
   `Version`/`RowVersion` xuất hiện nghĩa là ai đó đã đổi kiểu sang `byte[]`, và check concurrency
   đã **vô hiệu im lặng**.
3. **Có HAI index chạm `AssessmentDate`, và cả hai đều cần.** Index unique
   `(CriteriaId, AssessmentDate)` phục vụ đường DM-2 (luôn lọc kèm `CriteriaId`) và phục vụ luôn
   hàng *"tra cứu theo kỳ"* của §1.4 — nó dùng được vì mọi đường đọc đi qua global query filter
   soft-delete. Nhưng nó **không** phục vụ truy vấn của Dashboard, thứ lọc CHỈ theo khoảng ngày:
   `AssessmentDate` ở vị trí thứ hai, và
   [`huong_dan/quy-uoc/tieu-chi-review.md`](huong_dan/quy-uoc/tieu-chi-review.md) §6 nói thẳng
   index `(A, B)` mà query chỉ lọc theo `B` là **vẫn thiếu**. Đó là lý do
   `IX_CriteriaAssessments_AssessmentDate` ra đời.
4. **Hàm SQL `business.criteria_assessment_date_utc` KHÔNG được dựng lại**, và index
   `UX_CriteriaAssessments_CriteriaId_CreatedAt_Day` cũng vậy. Cả hai thuộc mô hình cũ (suy kỳ từ
   phần ngày của `CreatedAt`); mô hình mới có cột `date` thật nên EF Core index thẳng được. DDL
   của chúng còn trong [`cau-truc-database.sql`](cau-truc-database.sql) — file đó mang banner lịch
   sử, **chạy nó lên DB hôm nay sẽ lỗi**.

---

## Hai script `.sql`, chạy tay, đúng thứ tự

| Thứ tự | File | Nội dung |
| ---: | --- | --- |
| 1 | `Business/PlatformManager.Business.Persistence/Migrations/sql/0002_business_dti_tables.sql` | 4 bảng + ràng buộc + 6 index |
| 2 | `Business/PlatformManager.Business.Persistence/Migrations/sql/0003_criteria_assessment_date_index.sql` | riêng `IX_CriteriaAssessments_AssessmentDate` |

Cả hai chạy **sau** baseline `core` (`0001_initial_baseline.sql`). Rồi seed 6 nhóm chỉ tiêu —
danh mục ĐÓNG, import không tự tạo nhóm:

```bash
dotnet run --project src/BE/PlatformManager.Api -- --seed
```

**Vì sao index đi thành file 0003 RIÊNG thay vì gộp vào 0002:** 0002 có thể đã được chạy trước
khi index đó ra đời. Sinh lại 0002 cho to hơn sẽ hỏng với người đã chạy nó — các khối
`IF NOT EXISTS(… __EFMigrationsHistory …)` **không** bảo vệ được ở repo này, vì không lệnh EF nào
ghi vào bảng đó (schema áp bằng `.sql` chạy tay). Một file riêng thì đúng cho **cả hai** phía.

**Vì sao `.sql` của tầng nghiệp vụ KHÔNG nằm trong thư mục `.sql` của Core:** thư mục
`Core/PlatformManager.Core.Persistence/Migrations/sql/` là **artifact CoreBase ship cho dự án
thứ hai** (chốt 2026-09-04). Bốn bảng ở đây là nghiệp vụ của riêng sản phẩm này — để chúng ở đó
là ship bảng DTI sang một dự án không có nghiệp vụ DTI. Xem
[`kien-truc-core-module.md`](kien-truc-core-module.md) §"`.sql` của tầng nghiệp vụ".

---

## Ba lệnh tự-kiểm — đừng tin câu văn, chạy chúng

Chạy ở gốc repo. **Cả ba lệnh này từng SAI** ở bản viết ngày 2026-09-10 buổi sáng, và không có gì
báo vì khi đó chúng nằm trong file mang banner lịch sử — vùng mà `check-docs.sh` cố ý không kiểm.
Đó chính là lý do khối này được tách sang đây. Bản dưới đã sửa; mỗi lệnh kèm **lý do bản cũ sai**,
vì cùng cái bẫy sẽ quay lại ở lệnh thứ tư.

**1 — model EF đã biết đủ 4 bảng `business`:**

```bash
grep -c 'ToTable("[A-Za-z]*", "business"' src/BE/PlatformManager.Api/Persistence/Migrations/PlatformManagerDbContextModelSnapshot.cs
```

PASS: **4**. *Bản cũ đóng dấu nháy sau `"business")`, nên nó không khớp `ImportJobs` — cấu hình
của bảng đó dùng overload BA tham số (`ToTable(name, schema, table => …)`) để khai `CHECK`
constraint, nên sau `"business"` là dấu phẩy chứ không phải dấu đóng ngoặc. Lệnh cũ trả **3**
trong khi câu văn ngay trên nó khẳng định 4.*

**2 — không migration nào XOÁ bảng ở nhánh `Up`:**

```bash
for f in src/BE/PlatformManager.Api/Persistence/Migrations/2026*_*.cs; do
  case "$f" in *Designer.cs) continue;; esac
  echo "$f: $(sed -n '/void Up(/,/^        }$/p' "$f" | grep -c 'DropTable')"
done
```

PASS: **mọi dòng in ra `: 0`**. *Bản cũ `grep -c "DropTable"` trên cả file, nên nó đếm luôn nhánh
`Down` — nơi `DropTable` là ĐÚNG và bắt buộc — rồi trả `4` cho một tệp hoàn toàn lành. Một lệnh
trả số khác 0 trong khi mọi thứ đều đúng là lệnh sẽ bị bỏ qua ngay lần chạy thứ hai.*

**3 — snapshot khớp model, tức không còn thay đổi nào chưa vào migration:**

```bash
cd src/BE/PlatformManager.Api && dotnet ef migrations has-pending-model-changes \
  --project PlatformManager.Api.csproj --startup-project PlatformManager.Api.csproj
```

PASS: *"No changes have been made to the model since the last migration."* — **không cần kết nối
DB**. Đây là phép thử quyết định cho *tính đúng của snapshot*; hai lệnh trên chỉ nói về nội dung
file.
