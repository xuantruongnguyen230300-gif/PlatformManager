---
kind: luat
scope: du-an
verified: chua-doi-chieu
---

# Luật nghiệp vụ — Danh mục DTI

> ## 🚧 ĐÃ CHỐT — ĐANG THI CÔNG (viết 2026-09-05; vòng 1 thi công + soát lại 2026-09-10)
>
> **Luật của đường ĐỌC đã hiện thực; luật của đường GHI thì chưa.** Câu *"không một luật nào
> dưới đây đã được hiện thực"* của bản trước (2026-09-08) hết đúng từ 2026-09-10 — đối chiếu:
>
> - **BE — vòng 1 đã thi công 2026-09-10: NỀN + toàn bộ đường ĐỌC.** Đủ 5 project
>   `PlatformManager.Business.*`; 4 entity của §1.1–§1.5 và EF Configuration của chúng đã có;
>   migration `20260910100110_ThemBangNghiepVuDti` đã sinh (schema **chưa** áp lên database
>   nào — xem `doc/cau-truc-database-dti.md`); `BusinessSeeder` seed 6 nhóm của §1.6;
>   key `dti.manage` của §6.5 đã khai ở host với `SeedRoles = [Admin]` (Q36).
>   **Đường ĐỌC** DM-1 · DM-2 · DM-8 đã có endpoint thật.
>
>   **Chưa làm, có chủ đích (vòng 2):** toàn bộ đường GHI và import — DM-3…DM-7, tức §5.3
>   (upsert theo kỳ đích + copy-forward), §5.5 (xoá), §6.1–§6.4 (import). Bốn entity đã mang
>   sẵn bất biến của §2/§4 (khuôn mã, trần đoạn, 4 trạng thái) nhưng **không handler nào ghi**;
>   `ImportJob` có bảng mà chưa có đường tạo.
> - **FE — có KHUNG, chưa có màn thật (dựng 2026-09-09).** Thư mục
>   `src/FE/src/app/modules/danh-muc-dti/` tồn tại và route `/danh-muc/dti` đã khai
>   (`src/FE/src/app/app.routes.ts:40`), nhưng trang chỉ có hàng tiêu đề và một câu nói rõ
>   nó chưa xong; lý do dựng khung trước ghi tại chỗ, xem
>   `src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.ts:7`. Khung
>   đó **không** mang một luật nào của file này — không entity, không công thức, không quy
>   tắc kỳ, không lời gọi API.
>
> Mọi câu dưới đây vẫn là **luật phải hiện thực** cho phần chưa làm; phần đã làm (đường đọc)
> thì đọc như mô tả hành vi thật. Ranh giới hai phần: bảng ngay trên.
>
> **Kiến trúc: `PlatformManager.Business.*`** — entity ở `Business.Domain`, feature ở
> `Business.Application/{Criteria,CriteriaGroups,Dashboard}/`, EF Configuration ở
> `Business.Persistence`, controller ở `Business.Api`. **KHÔNG** dựng lại `Modules.DtiWeekly.*`.
> Ranh giới + thứ tự phụ thuộc: `doc/kien-truc-core-module.md`.

**File này giữ gì:** mô hình dữ liệu, công thức, quy tắc kỳ, quy tắc ghi, quy tắc import,
**quyền ghi** (§6.5) và **dấu vết ai sửa kỳ nào** (§5.6).
**File này KHÔNG giữ gì:** route/shape/mã lỗi (→ `doc/contracts/danh-muc-dti.md`), layout
màn hình (→ `spec/danh-muc-dti/ui-spec.md`), công thức tổng hợp của Dashboard
(→ `spec/dashboard-dti/business-rules.md`), luật import/export chung của Core
(→ `doc/huong_dan/wiki-core/be/15-import-export.md`).

---

## 1. Mô hình dữ liệu

Đây là **file chủ** của mô hình dữ liệu DTI. `spec/dashboard-dti/business-rules.md` đọc
sang đây, không mô tả lại.

**Bốn entity, không hơn** — ba entity dữ liệu (§1.1–§1.3) cộng `ImportJob` theo dõi job import
(§1.5). Schema `business` (khai **tường minh** trong `ToTable()` — entity không khai schema sẽ
rơi vào `core`, xem `doc/cau-truc-database.md` §1.1).

> 🔄 **LẬT 2026-09-10 (Q45).** Bản trước ghi *"Ba entity, không hơn"*, trong khi
> `doc/cau-truc-database-business.md` đã khai `ImportJobs` là bảng thứ tư của
> `Business.Domain` từ 2026-09-09 và chỉ trỏ bộ cột về một bảng lịch sử. Q45 chốt `ImportJobs`
> là entity **thứ tư**, đặc tả đầy đủ ở §1.5 của file này — file chủ của mô hình dữ liệu.

### 1.1 `CriteriaGroup` — nhóm chỉ tiêu

| Trường | Kiểu | Ghi chú |
| --- | --- | --- |
| `Id` | `Guid` | từ `BaseEntity` — UUID v7, sinh ở ứng dụng (`EntityId.New()`), **không** `DEFAULT gen_random_uuid()` |
| `Code` | `string(20)` | unique trong tập chưa xoá mềm |
| `Name` | `string(200)` | tên hiển thị, khớp cột `Nhóm` của file import |
| `DisplayOrder` | `int` | thứ tự hiển thị; BE sắp, FE không sắp lại |

Thừa kế `BaseEntity` (`src/BE/Core/PlatformManager.Core.Domain/Common/BaseEntity.cs`):
`CreatedBy`/`UpdatedBy`/`CreatedAt`/`UpdatedAt`/`IsDeleted`.

### 1.2 `Criteria` — chỉ tiêu

| Trường | Kiểu | Ghi chú |
| --- | --- | --- |
| `Id` | `Guid` | |
| `Code` | `string(20)` | unique trong tập chưa xoá mềm — xem §2 |
| `Name` | `string` | bắt buộc, không giới hạn cứng độ dài |
| `NameNormalized` | `string` | **cột chuẩn hoá cho tìm kiếm** (Q47) — bản không dấu, chữ thường của `Name`. Không nhận từ client, không ra dây. Xem mục ngay dưới |
| `CodeSortKey` | `string(49)` | **khoá sắp xếp tính sẵn** (Q53) — `Code` với từng đoạn số đệm `0` bên trái cho đủ 4 chữ số (Q58). Không nhận từ client, không ra dây. Xem mục sắp xếp dưới |
| `GroupId` | `Guid` | FK → `CriteriaGroup` |
| `MaxScore` | `decimal(10,2)` | **> 0**. Dữ liệu thật chỉ có 3 giá trị (10 · 20 · 30) nhưng **không** ràng buộc thành enum — BA có thể thêm mức khác |

#### Tìm kiếm không phân biệt dấu — cột chuẩn hoá (Q47, chốt 2026-09-10)

**Q47:** tìm kiếm **không phân biệt dấu** ở **cả hai màn** (Danh mục DTI · Dashboard), cài bằng
**cột chuẩn hoá lưu sẵn** — không dùng extension Postgres, không bỏ dấu lúc truy vấn trên cột
gốc. Mục này là **file chủ của cách cài**: `doc/contracts/danh-muc-dti.md` DM-2 giữ tham số
`search`, Dashboard trỏ về đây, không chép.

| Câu hỏi | Chốt |
| --- | --- |
| Trường nào có cột chuẩn hoá | **`Criteria.Name` → `NameNormalized`**. DM-2 tìm theo `Code` HOẶC `Name`; `Code` **không** cần cột riêng — khuôn của nó (§2) chỉ gồm chữ số và dấu chấm, bản chuẩn hoá trùng bản gốc |
| Chuẩn hoá thế nào | cắt khoảng trắng đầu/cuối · chữ thường (invariant) · bỏ dấu (tách Unicode NFD rồi gỡ ký tự tổ hợp) · **`đ`/`Đ` → `d`** |
| Ai ghi cột | **chính entity**, ở mọi chỗ gán `Name` — nên mọi đường ghi đều đi qua: tạo (DM-3), sửa (DM-4), import tạo mới (DM-7, §6.3). Không handler nào tự tính |
| Từ khoá tìm | server chuẩn hoá `search` bằng **cùng một hàm** rồi so với `NameNormalized`; FE gửi nguyên văn, không tự bỏ dấu |
| **Kiểu khớp (Q59, 2026-09-10)** | **tên: CHỨA chuỗi** — `NameNormalized` chứa từ khoá đã chuẩn hoá. **mã: khớp theo ĐOẠN** — `Code = q` **hoặc** `Code` bắt đầu bằng `q + "."` (`q` đã cắt khoảng trắng, so ordinal như §2). Gõ `4.2` ra `4.2` và mọi `4.2.x`, **không** ra `4.20`–`4.29`. Một dòng khớp nếu khớp tên **hoặc** khớp mã |
| Index | §1.4 |

⚠️ **`đ` là cái bẫy của cách bỏ dấu "chuẩn".** `đ` (U+0111) là một chữ cái riêng, không phải
`d` cộng dấu — tách NFD **không** gỡ được nó. Bỏ sót bước này thì gõ `dao tao` không ra
`Đào tạo`, và không có lỗi nào báo.

⚠️ **Q47 chỉ áp cho TÌM KIẾM.** Ba phép so khớp khác của file này **vẫn phân biệt dấu**, cố ý
và không đổi: `Trạng thái` (§4 luật 4), header cột import (§6.2), `Phụ trách` khi import
(§6.3). Đó là so khớp một **giá trị phải đúng**, không phải gõ để tìm — đừng "thống nhất" chúng
theo Q47.

#### Sắp mã theo thứ tự tự nhiên — cột khoá sắp xếp tính sẵn (Q53, chốt 2026-09-10)

**Q53:** lưới sắp theo mã **tự nhiên** (`4.2 < 4.10 < 4.22.11`, luật §2) bằng **cột
`CodeSortKey` tính sẵn**, có index — cùng khuôn với `NameNormalized` của Q47. Không sắp trong
bộ nhớ, không tách chuỗi lúc truy vấn.

| Câu hỏi | Chốt |
| --- | --- |
| Khoá dựng thế nào | tách `Code` (đã cắt khoảng trắng) theo dấu chấm, đệm `0` bên trái **từng đoạn** cho đủ **4** chữ số (trần của Q58, §2), nối lại bằng dấu chấm: `4.2` → `0004.0002`, `4.22.11` → `0004.0022.0011` |
| Ai ghi cột | **chính entity**, ở mọi chỗ gán `Code` — tạo (DM-3), sửa mã (DM-4), import tạo mới (DM-7). Import **không** đổi mã của chỉ tiêu đã có (§6.3), nên không có nhánh cập nhật từ import |
| So sánh | **ordinal** — khai `COLLATE "C"` cho cột. Collation theo ngôn ngữ có thể bỏ qua dấu câu khi so, mà khoá này dựa vào so từng byte |
| Trùng khoá | sắp phụ theo `Code` (ordinal). Trùng khoá xảy ra khi hai mã chỉ khác số `0` đầu đoạn (`4.02` và `4.2`) — §2 không cấm, và phân trang cần một thứ tự xác định |
| Độ dài cột | `string(49)` — phép tính ngay dưới |
| Index | §1.4 |

**Vì sao đệm 4 và cột dài 49 — suy từ hai ràng buộc của luật mã §2**, không từ dữ liệu mẫu:
tổng ≤ 20 ký tự, và mỗi đoạn ≤ 4 chữ số (Q58).

- Đệm **4** = trần chữ số của một đoạn ⇒ mọi mã hợp lệ đều sắp đúng; không mã nào có đoạn dài
  hơn phần đệm.
- `n` đoạn cần ít nhất `2n − 1` ký tự (mỗi đoạn ≥ 1 chữ số, cộng `n − 1` dấu chấm) ⇒
  `2n − 1 ≤ 20` ⇒ tối đa **10 đoạn**. Khoá của `n` đoạn dài `4n + (n − 1) = 5n − 1` ⇒ dài
  nhất `5 × 10 − 1` = **49**.

> 🔄 **LẬT 2026-09-10 (Q58).** Bản trước (cùng ngày) đệm **20** và khai `string(209)`, vì luật
> mã khi đó chỉ giới hạn tổng độ dài — một đoạn có thể dài 20 chữ số — và ghi rằng muốn khoá
> ngắn hơn thì phải chốt thêm một luật về số chữ số mỗi đoạn. Q58 chốt đúng luật đó.

### 1.3 `CriteriaAssessment` — một lần đánh giá của một chỉ tiêu

| Trường | Kiểu | Ghi chú |
| --- | --- | --- |
| `Id` | `Guid` | |
| `CriteriaId` | `Guid` | FK → `Criteria` |
| **`AssessmentDate`** | `date` | **ngày nghiệp vụ của lần đánh giá** — kỳ (tuần/tháng/năm) suy ra từ đây. Giá trị do **kỳ đích của lời ghi** quyết định, không phải "hôm nay" — luật neo ngày ở §5.3 (đổi 2026-09-05 theo Q20) |
| `SelfScore` | `decimal(10,2)?` | Tự đánh giá |
| `VerifiedScore` | `decimal(10,2)?` | Thẩm định |
| `ProgressPercent` | `int?` | 0..100 |
| `Status` | `string(40)?` | đúng 1 trong 4 giá trị §4 |
| `OwnerId` | `Guid?` | FK **xuyên schema** → `core."AspNetUsers".Id` |
| `Deadline` | `date?` | Hạn xử lý |
| `Note` | `text?` | Minh chứng/Ghi chú — **MỘT ô text** |
| `Version` | `uint` | token optimistic concurrency |

**`Chênh lệch` KHÔNG có cột.** Nó là trường tính (§3.1). Lưu một trường suy ra được là tạo
chỗ cho nó lệch khỏi hai trường sinh ra nó.

#### Ba quyết định của entity này — đọc trước khi sửa

**a. `Note` là một ô text, không phải danh sách minh chứng.** Thực thể `CriteriaEvidence`
của thiết kế cũ **bị bỏ hẳn** (Q5). Một dòng dữ liệu thật minh hoạ vì sao BA muốn thế:

```
*2121/TNH-TH - 04/03/2026: V/v thông tin số liệu phục vụ đánh giá mức độ
chuyển đổi số DTI các địa phương năm 2025
và đề xuất lịch tham vấn.
```

Đó là **một** minh chứng trải trên nhiều dòng, không phải nhiều minh chứng — tách theo dấu
xuống dòng sẽ cắt nát nó. 29/62 dòng của file BA gửi có nội dung ở cột này.

**b. `AssessmentDate` là cột RIÊNG, không dùng lại `CreatedAt`.**

> ⚠️ Mô hình cũ suy kỳ từ **phần ngày của `CreatedAt`**, và phải dựng một hàm SQL
> `IMMUTABLE` viết tay (`business.criteria_assessment_date_utc`, còn ở
> `doc/cau-truc-database.sql`) chỉ để index được biểu thức đó — Postgres từ chối
> `CAST("CreatedAt" AS date)` trong index với lỗi `42P17`.
>
> Ba lý do đổi:
> 1. `CreatedAt` là **trường audit**, do `AuditInterceptor` ghi từ bên ngoài entity
>    (`BaseEntity.cs` — setter public cho đúng 5 field audit). Dùng nó làm khoá nghiệp vụ
>    nghĩa là một thay đổi ở tầng audit sẽ lặng lẽ đổi kỳ của dữ liệu.
> 2. **Không nhập bù được.** Với mô hình cũ, ghi dữ liệu cho tuần trước là bất khả — kỳ
>    luôn là "hôm nay". Q12 yêu cầu kỳ ghi rõ từ ngày đến ngày, tức kỳ là dữ liệu nghiệp vụ
>    chứ không phải dấu vết hệ thống.
> 3. Cột `date` thật thì index được bằng EF Core, **không cần** hàm SQL viết tay nào.
>
> **Hệ quả cần xử lý khi thi công:** DDL của
> `UX_CriteriaAssessments_CriteriaId_CreatedAt_Day` và hàm
> `business.criteria_assessment_date_utc` ở `doc/cau-truc-database.sql` mô tả mô hình cũ.
> Chúng thuộc một module đã gỡ và file `doc/cau-truc-database-business.md` đã mang banner
> lịch sử. Lượt thi công đầu tiên phải cập nhật **file chủ schema**, không vá bên lề.

**c. `Version` phải là `uint` + `.IsRowVersion()`, KHÔNG `byte[]`.** Trên PostgreSQL,
`.IsRowVersion()` với `byte[]` tạo một cột `bytea` **không ai cập nhật** ⇒ check concurrency
vô hiệu **im lặng**. Với `uint`, Npgsql bind thẳng vào cột hệ thống `xmin`, không tạo cột
mới. Luật đầy đủ: `doc/huong_dan/quy-uoc/be-entity-domain.md` §RowVersion.

Hai luồng ghi độc lập đụng cùng bản ghi (import hàng loạt · sửa tay) là lý do trường này tồn
tại — không có nó, người ghi sau âm thầm nuốt thay đổi của người ghi trước.

### 1.4 Ràng buộc ở tầng DB

| Ràng buộc | Dạng |
| --- | --- |
| `Criteria.Code` unique | unique **partial** `WHERE "IsDeleted" = false` — xoá mềm một mã rồi tạo lại đúng mã đó phải thành công |
| `CriteriaGroup.Code` unique | như trên |
| **1 đánh giá / 1 chỉ tiêu / 1 ngày** | unique **partial** trên `("CriteriaId", "AssessmentDate") WHERE "IsDeleted" = false` |
| Tra cứu theo kỳ | index `("CriteriaId", "AssessmentDate")` |
| Tìm kiếm (Q47 + Q59, §1.2) | **KHÔNG khai index riêng — có chủ đích**, xem ghi chú dưới bảng |
| Sắp mã tự nhiên (Q53, §1.2) | index btree `IX_Criteria_CodeSortKey` trên `("CodeSortKey", "Code")`; cột `CodeSortKey` mang `COLLATE "C"` |
| `ImportJobs` — poll theo trạng thái | index `IX_ImportJobs_Status` trên `("Status")` — giữ từ bảng cũ (§1.5) |
| `ImportJobs.TargetWeekEnd` luôn là Chủ nhật | `CHECK (EXTRACT(ISODOW FROM "TargetWeekEnd") = 7)` — lưới an toàn cho §1.5 |

> **Tìm kiếm không có index riêng — lựa chọn có chủ đích, không phải bỏ sót (Q59, 2026-09-10).**
> Khớp **chuỗi con** trên `NameNormalized` (`LIKE '%x%'`) không dùng được btree, còn index
> trigram cần extension mà Q47 đã loại. Danh mục chỉ có vài chục chỉ tiêu, nên quét tuần tự là
> đủ — cho cả nhánh khớp tên lẫn nhánh khớp mã theo đoạn.
>
> 🔄 **LẬT 2026-09-10 (Q59).** Bản trước (cùng ngày) khai index btree `IX_Criteria_NameNormalized`
> kèm cảnh báo rằng nó chỉ phục vụ khớp tiền tố, và để ngỏ câu hỏi tiền tố hay chuỗi con. Q59
> chốt chuỗi con cho tên ⇒ btree đó không phục vụ truy vấn nào, nên gỡ.

> Ràng buộc "1 đánh giá / 1 chỉ tiêu / 1 ngày" là **nền móng của toàn bộ mô hình kỳ**. Mất
> nó, dữ liệu trùng lọt vào **im lặng** và mọi phép tổng hợp của Dashboard đếm hai lần.

> ⚠️ **Ràng buộc trên KHÔNG đủ để bảo đảm "1 bản ghi / 1 chỉ tiêu / 1 kỳ".** Một tuần có 7
> ngày, nên hai lời ghi vào cùng tuần ở hai ngày khác nhau vẫn tạo được hai dòng — DB không
> chặn nổi vì `period` không phải một cột, nó là thứ **suy ra** từ `AssessmentDate`. Việc gộp
> về một bản ghi/kỳ do **handler** làm (§5.3), và luật đọc §5.2 (lấy bản ghi có
> `AssessmentDate` lớn nhất trong kỳ) là thứ giữ cho kết quả vẫn xác định ngay cả khi có hai
> dòng lọt vào. Đừng ghi vào tài liệu rằng DB bảo đảm điều đó — nó không.

FK **xuyên schema** chỉ đi một chiều: `business.CriteriaAssessments.OwnerId → core.AspNetUsers.Id`.
Không có FK nào đi ngược `core → business` — khớp luật "Core không được biết về Business".

### 1.5 `ImportJob` — theo dõi một lần import (bảng `ImportJobs`, Q45)

**Entity thứ tư của `Business.Domain`** (Q45, chốt 2026-09-10). Bảng *trạng thái tiến trình*,
không phải dữ liệu nghiệp vụ — nhưng vẫn là bảng **nghiệp vụ**, không phải bảng Core (chốt
2026-09-09, luật 3 ở `doc/cau-truc-database-dti.md`).

| Trường | Kiểu | Ghi chú |
| --- | --- | --- |
| `Id` | `Guid` | PK, từ `BaseEntity` |
| `FileName` | `string(260)` | NOT NULL — tên file người dùng gửi |
| `Format` | `string(20)` | NOT NULL |
| `StoragePath` | `string(1000)` | NOT NULL — **đường dẫn** file tạm, không lưu nội dung file |
| `Status` | `string(20)` | NOT NULL — `Pending` · `Running` · `Succeeded` · `Failed` (DM-7 bước 2) |
| `ResultJson` | `text?` | `result` của DM-7 bước 2 |
| `ErrorMessage` | `text?` | khi `Failed` — dev-facing, **không** để hiển thị |
| **`ErrorCode`** | `string(100)?` | **MỚI (Q75, 2026-09-11)** — `businessCode` của lỗi CẢ FILE khi lỗi đó có mã nghiệp vụ (`IMPORT.FILE_TOO_MANY_ROWS`, `IMPORT.FILE_MISSING_COLUMN`). `NULL` với lỗi hạ tầng thuần. Ra dây qua `errorCode` của DM-7 bước 2 |
| **`TargetWeekEnd`** | `date` | **MỚI (Q45)** — NOT NULL. **Chủ nhật của tuần ISO đích, ĐÃ quy đổi** — không bao giờ mang nghĩa `"all"` |

Thừa kế `BaseEntity` như ba entity kia. **Không FK nào** — job độc lập với dữ liệu nó ghi ra.
Bộ cột, trừ `TargetWeekEnd` và `ErrorCode`, lấy nguyên từ hàng `ImportJobs` của bảng lịch sử ở
`doc/cau-truc-database-business.md` §Danh sách bảng; index ở §1.4.

> **Vì sao `ErrorCode` phải là một cột chứ không suy được từ `ErrorMessage`** (thêm 2026-09-11):
> nhánh `Failed` trước đó chỉ có `ErrorMessage`, thứ hợp đồng khai thẳng là *dev-facing, KHÔNG để
> hiển thị*. Nghĩa là mọi lỗi CẢ FILE — vượt trần số dòng, thiếu cột bắt buộc — **không có gì để FE
> dịch thành câu cho người dùng đọc**. Một trần mà người dùng chạm phải nhưng không đọc được lý do
> thì chẳng khác gì không có trần. Tách chuỗi mã ra khỏi câu văn là một phép đoán, nên nó là cột.
>
> Script áp cột: `src/BE/Business/PlatformManager.Business.Persistence/Migrations/sql/0004_import_jobs_error_code.sql`.

**Vì sao phải LƯU tuần đích, và quy đổi lúc nào.** `period` của DM-7 được quy đổi **lúc nhận
request**, không phải lúc job chạy: `"all"` nghĩa là *tuần hiện tại tại lúc người dùng bấm
Nhập*. Job nền chạy sau đó — quy lúc chạy thì một file bấm lúc 23:59 Chủ nhật mà worker nhặt lúc
00:01 thứ Hai sẽ đổ vào tuần **sau**. Kiểm T15 (`year`) cũng ở lúc nhận request, cùng chỗ với
lời từ chối `400`. Người dùng vẫn chọn kỳ trước khi nạp — **Q20 giữ nguyên**; Q45 chỉ quyết
thứ gì được ghi xuống cho job đọc lại. Job áp luật neo ngày §5.3 như mọi đường ghi khác:
`AssessmentDate` = ngày job chạy nếu ngày đó nằm trong `[TargetWeekEnd − 6, TargetWeekEnd]`,
không thì `= TargetWeekEnd`.

**Kiểu cột: `date` neo Chủ nhật, KHÔNG phải chuỗi `YYYY-Www`** — chọn cho khớp luật neo ngày
§5.3 và cách cả mô hình này lưu thời gian:

1. **Mô hình không lưu kỳ dạng chuỗi ở đâu cả.** Kỳ luôn **suy ra** từ một `date` qua lịch ISO
   (§1.3 `AssessmentDate`; cảnh báo §1.4 *"`period` không phải một cột"*), và Q37 đã bỏ phương
   án cột `PeriodKey`. Chuỗi `"YYYY-Www"` chỉ sống trên dây (API).
2. **Giá trị lưu chính là neo dự phòng của §5.3** — *Chủ nhật của tuần đích*. Job không phải
   phân tích chuỗi hay tính lịch ISO lần nữa: phép quy đổi tuần diễn ra **một lần**, lúc nhận
   request, ở BE (khớp Q40).
3. **Postgres kiểm được kiểu.** `date` + `CHECK ISODOW = 7` (§1.4) từ chối mọi giá trị không
   phải một mốc tuần; một cột chuỗi thì nhận cả `"2026-W99"`.

Cái giá: muốn hiện tuần đích lên màn thì phải quy ngược `date` → `"YYYY-Www"` — bằng đúng hàm
lịch ISO mà mọi chỗ khác dùng, không tự tính.

### 1.6 Danh mục nhóm — dữ liệu seed (Q42, chốt 2026-09-10)

`doc/contracts/danh-muc-dti.md` DM-1 trỏ về mục này. Nhóm là **danh mục đóng** (§6.3): import
**không** tự tạo nhóm, nên các nhóm phải có sẵn trước lần import đầu tiên. Bảng dưới là
**quyết định**, không phải số đo:

| `Code` | `DisplayOrder` | `Name` |
| --- | ---: | --- |
| `"1"` | 1 | `Hạ tầng và Nền tảng số` |
| `"2"` | 2 | `Nhân lực số` |
| `"3"` | 3 | `An toàn thông tin, an ninh mạng` |
| `"4"` | 4 | `Hoạt động chính quyền số` |
| `"5"` | 5 | `Hoạt động Kinh tế số` |
| `"6"` | 6 | `Hoạt động Xã hội số` |

- **`Name` là ĐÚNG chuỗi cột `Nhóm`** của `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` —
  không tiền tố số, giữ cả chỗ viết hoa không đều (`chính quyền số` thường, `Kinh tế số` hoa).
  Import tra nhóm bằng `Name` (§6.2 cột 3); sửa "cho đẹp" một chữ là mọi dòng của nhóm đó thành
  `IMPORT.ROW_GROUP_NOT_FOUND`. Các tên này cũng trùng tên nhóm đo từ file BA gửi (bảng §7).
- **`DisplayOrder` = thứ tự xuất hiện đầu tiên** của nhóm trong file đó; `Code` = cùng số, dạng
  chuỗi.
- **Giao diện hiện `Code. Name`** — vd `1. Hạ tầng và Nền tảng số`. Ghép chuỗi là việc của FE
  (`spec/danh-muc-dti/ui-spec.md`); DB và API giữ hai trường rời — DM-1 trả `code` + `name`,
  dòng DM-2 trả `groupCode` + `groupName`.

**Số chỉ tiêu và tổng điểm từng nhóm — đếm bằng lệnh, không chép** (`.claude/CLAUDE.md` §6).
Chạy ở gốc repo:

```bash
PYTHONIOENCODING=utf-8 python -c "import csv; rows=list(csv.DictReader(open('spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv', encoding='utf-8-sig'))); groups=list(dict.fromkeys(r['Nhóm'] for r in rows)); [print(i, g, sum(r['Nhóm']==g for r in rows), sum(float(r['Điểm tối đa']) for r in rows if r['Nhóm']==g), sep=' | ') for i, g in enumerate(groups, 1)]; print('tổng', len(rows))"
```

**PASS khi:** số dòng nhóm in ra bằng số hàng của bảng seed ở trên, và **thứ tự + tên** trùng
từng hàng; cột số chỉ tiêu cộng lại bằng dòng `tổng`. Lệch ⇒ file mẫu hoặc bảng seed đã đổi —
chốt lại với người dùng, **không** sửa một bên cho khớp bên kia.

⚠️ Cột điểm của file mẫu là **số liệu dựng** (§7) — tổng điểm từng nhóm từ lệnh này chỉ dùng
kiểm **hình dạng**, không phải số tham chiếu nghiệp vụ. Số tham chiếu (đo từ file BA gửi) ở
bảng §7.

🚧 **Cơ chế seed — ĐÃ CHỐT 2026-09-10 (Q67), ĐANG THI CÔNG: `BusinessSeeder` riêng ở
`Business.Persistence`**, không dùng `HasData`.

| | Chốt |
| --- | --- |
| Cơ chế | một seeder riêng, cùng khuôn `CoreSeeder` đang chạy ở `Core.Persistence` |
| Sinh `Id` | qua `EntityId.New()` — đúng §1.1, không viết cứng Guid nào |
| Chạy lại | phải idempotent: tra theo `Code` trước khi chèn. Unique partial `Code` (§1.4) là lưới an toàn thứ hai, **không** phải cơ chế chính |
| Đổi bảng seed về sau | sửa seeder, **không** sinh migration mới |

**Vì sao bỏ `HasData`:** nó buộc 6 `Guid` viết cứng trong code — ngược §1.1 (*UUID v7, sinh ở
ứng dụng qua `EntityId.New()`*) — và biến mọi lần sửa bảng seed ở §1.6 thành một migration mới,
trong khi bảng đó là **dữ liệu nghiệp vụ** BA có thể đổi, không phải lược đồ bảng.

**Nghiệm thu:** chạy seeder **hai lần liên tiếp** trên cùng một DB ⇒ `GET /api/criteria-groups`
trả đúng số hàng của bảng §1.6, không nhân đôi, và không nhóm nào đổi `Id` giữa hai lần.

**Điều ĐÃ chốt: cơ chế nằm ở `Business.*`, KHÔNG ở Core** (không ở `CoreSeeder`). Tên nhóm là
dữ liệu nghiệp vụ của đúng một sản phẩm, còn Core đi theo sang dự án thứ hai
(`doc/kien-truc-core-module.md`).

⚠️ **ArchTest KHÔNG canh được điều này — đừng dựa vào nó.**
`CoreSource_MustNotContain_BusinessNameStringLiteral`
(`src/BE/Tests/PlatformManager.ArchTests/CoreMustNotKnowBusinessNameTests.cs:80`) chỉ chặn
literal mang **tên tầng** nghiệp vụ — danh sách cấm của nó là `business` và `dtiweekly`. Một
seeder trong Core chép các chuỗi tiếng Việt ở trên vẫn **xanh** test đó trong khi phá ranh giới.
Lớp canh ở đây là review, không phải máy.

---

## 2. Mã chỉ tiêu (`Code`)

| Luật | Giá trị |
| --- | --- |
| Bắt buộc | có |
| Độ dài | ≤ **20** ký tự |
| Unique | trong tập **chưa xoá mềm** |
| So khớp | **ordinal**, phân biệt hoa/thường; cắt khoảng trắng đầu/cuối trước khi so |
| Định dạng | các đoạn số cách nhau bởi dấu chấm, **KHÔNG giới hạn 2 cấp** |
| **Mỗi đoạn** | ≤ **4** chữ số — **Q58** (2026-09-10) |

⚠️ **Đây là chỗ bản trước suýt làm sai.** Dữ liệu thật của BA có **11 mã ba cấp**:
`4.22.1` … `4.22.11`. Một regex kiểu `^\d+\.\d+$` sẽ từ chối đúng 11 chỉ tiêu hợp lệ. Mã dài
nhất hiện có là 7 ký tự, nên trần 20 vẫn dư — nhưng **số cấp** thì đừng giả định.

**Q58 — mỗi đoạn tối đa 4 chữ số (chốt 2026-09-10).** Luật trước chỉ giới hạn **tổng** độ dài,
nên một đoạn 20 chữ số vẫn hợp lệ. Nay kiểm ở **mọi** đường ghi mã, bằng cùng một hàm kiểm:

| Đường | Vi phạm trả |
| --- | --- |
| Tạo (DM-3), sửa (DM-4) | `400 CRITERIA.CODE_SEGMENT_TOO_LONG` |
| Import (DM-7) | lỗi **dòng đó**: `IMPORT.ROW_CODE_SEGMENT_TOO_LONG` — job vẫn `Succeeded` (§6.3) |

**Q65 — mã sai ĐỊNH DẠNG và mã quá dài khi import (chốt 2026-09-10).** Trước Q65, hai ca này
**không có mã lỗi nào**: mã chứa chữ cái hoặc có đoạn rỗng (`4..2`) lọt qua ở cả ba đường ghi,
còn import không có mã cho ca quá 20 ký tự (dialog thì đã có `CRITERIA.CODE_TOO_LONG`).
Định dạng hợp lệ: **chỉ chữ số, ngăn bằng dấu chấm đơn, không đoạn rỗng, không dấu chấm ở đầu
hoặc cuối** — cùng một hàm kiểm dùng cho cả ba đường ghi.

| Đường | Vi phạm trả |
| --- | --- |
| Tạo (DM-3), sửa (DM-4) | `400 CRITERIA.CODE_FORMAT_INVALID` |
| Import (DM-7) — sai định dạng | lỗi **dòng đó**: `IMPORT.ROW_CODE_FORMAT_INVALID` |
| Import (DM-7) — quá 20 ký tự | lỗi **dòng đó**: `IMPORT.ROW_CODE_TOO_LONG` |

Trần 4 còn dư so với dữ liệu thật (đoạn dài nhất là `22`), và là thứ cho khoá sắp xếp
`CodeSortKey` một độ dài cố định (§1.2).

Sắp xếp mặc định của lưới là **theo mã, thứ tự tự nhiên**: `4.2` đứng trước `4.10`, và
`4.22.2` trước `4.22.11`. So sánh chuỗi thuần cho kết quả ngược — phải tách từng đoạn số rồi
so theo số. Cách cài: cột khoá sắp xếp tính sẵn `CodeSortKey` (**Q53**, 2026-09-10) — §1.2,
index ở §1.4.

---

## 3. Công thức

### 3.1 `Chênh lệch` (`diff`) — ĐỔI CHIỀU 2026-09-05 (Q25)

```
diff = verifiedScore − selfScore      (Thẩm định − Tự đánh giá)
```

- Trường **TÍNH**, không nhập tay, không lưu (Q2). Client gửi lên thì bỏ qua, không báo lỗi.
- Một trong hai vế vắng ⇒ `diff` vắng.
- So sánh với 0 dùng **epsilon `0.001`**, không so `== 0` — hai `decimal` sinh ra từ phép trừ
  luôn có ca sát 0.
- Không lấy trị tuyệt đối, không đảo dấu ở tầng hiển thị. Dấu **chính là** thông tin.

**Dấu mang nghĩa nghiệp vụ, và đó là lý do đổi chiều:**

| Dấu | Nghĩa | Tô màu |
| --- | --- | --- |
| `diff > ε` | thẩm định chấm **cao hơn** tự chấm — có lợi | xanh (`.delta.up`) |
| `abs(diff) ≤ ε` | hai bên khớp | xám (`.delta.flat`) |
| `diff < −ε` | thẩm định **cắt bớt** điểm tự chấm — cần xử lý | đỏ (`.delta.down`) |

Hai ca kiểm trên dữ liệu thật của BA (**file BA gửi tháng 8/2026**, đối chiếu 2026-09-05):

| Chỉ tiêu | Tự đánh giá | Thẩm định | `diff` mới | Màu |
| --- | ---: | ---: | ---: | --- |
| `1.1` | 7,04 | 10 | **+2,96** | xanh |
| `1.4` | 5 | 0 | **−5,00** | đỏ |

> **Vì sao bản trước sai:** với công thức cũ `selfScore − verifiedScore`, chỉ tiêu `1.4` — tự
> chấm 5 điểm, thẩm định cho **0**, tức bị bác trắng — hiện ra `+5,00` và **tô xanh**. Con số
> đúng về phép trừ, sai hoàn toàn về thứ người đọc rút ra từ màu sắc. Đổi chiều là để dấu và
> màu nói cùng một câu với nghiệp vụ. Ánh xạ dấu → lớp CSS giữ **một chỗ**:
> `spec/dashboard-dti/business-rules.md` §2.

> ### ⚠️⚠️ Số trong phần mềm sẽ NGƯỢC DẤU với cột `Chênh lệch` trong file BA gửi
>
> **file BA gửi tháng 8/2026** tính cột `Chênh lệch` theo chiều **cũ**: dòng `1.1`
> ghi `-2.96` (`= 7,04 − 10`), dòng `1.4` ghi `5.00` (`= 5 − 0`). Phần mềm sẽ hiện `+2,96` và
> `−5,00` cho đúng hai dòng đó.
>
> **Điều này KHÔNG làm hỏng import.** `Chênh lệch` là trường **tính**: đường import **bỏ qua**
> cột đó hoàn toàn, không đọc, không đối chiếu, không báo lỗi khi lệch (§6.2 cột 7). Hệ thống
> tự tính lại từ hai cột điểm — mà hai cột điểm thì giống hệt nhau ở cả hai chiều.
>
> Nơi khác biệt **lộ ra** là mắt người: ai mở file gốc của BA cạnh file `.xlsx` do hệ thống
> xuất ra sẽ thấy cột `Chênh lệch` lệch dấu ở đúng 27 dòng. Vì vậy tiêu đề cột trong file xuất
> ghi rõ công thức — `Chênh lệch (Thẩm định − Tự đánh giá)`, xem
> `spec/dashboard-dti/business-rules.md` §4.3.

### 3.2 `Tiến độ %` (`progressPercent`)

- Kiểu `int`, miền hợp lệ **0..100**. Ngoài miền ⇒ kẹp về biên, **không** báo lỗi khi sửa
  inline (bảo vệ chiều sâu: FE kẹp trước, BE kẹp lại).

  > **Hệ quả đã thi hành 2026-09-11:** `CRITERIA.PROGRESS_PERCENT_INVALID` — mã mà
  > `doc/contracts/danh-muc-dti.md` DM-6 từng liệt — **không được khai** trong catalog, vì không
  > đường nào ném được nó. Card đã sửa theo dòng này. Đây là ca luật nghiệp vụ thắng hợp đồng
  > đường dây khi hai bên nói ngược nhau.
- **Nhập tay** khi sửa inline hoặc qua dialog. Không tự tính lại từ điểm.
- Khởi tạo khi import: **để TRỐNG** (Q24) — xem §6.4. Không phải `0`, không suy từ điểm.
- Đây là trường mà thanh tiến độ theo nhóm và biểu đồ đường trên Dashboard vẽ theo (Q11);
  3 cột điểm chỉ đọc ở bảng chi tiết.

### 3.3 Làm tròn và định dạng

| Đại lượng | Lưu | Hiển thị |
| --- | --- | --- |
| `MaxScore`, `SelfScore`, `VerifiedScore`, `diff` | `decimal(10,2)` — **không** làm tròn khi lưu | 2 chữ số thập phân, dấu **phẩy** thập phân (`7,04`) |
| `ProgressPercent` | `int` | `70%` |
| % tổng hợp | tính trên `decimal` | 1 chữ số thập phân (`82,1%`) |

Làm tròn **chỉ ở tầng hiển thị**. Làm tròn khi lưu là mất dữ liệu không lấy lại được, và
tổng của 62 số đã làm tròn không bằng tổng thật.

---

## 4. Trạng thái — 4 giá trị, người dùng CHỌN TAY

| Giá trị (lưu nguyên văn) | Ý nghĩa | Số dòng trong file BA gửi |
| --- | --- | ---: |
| `Chưa thực hiện` | chưa bắt đầu | 1 |
| `Đang thực hiện` | đang làm | 13 |
| `Cần bổ sung minh chứng` | có số liệu nhưng thiếu minh chứng | 22 |
| `Hoàn thành` | xong | 26 |

**Bốn luật cứng:**

1. **Hệ thống KHÔNG tự tính, KHÔNG tự đổi trạng thái.** Đây là điểm khác hẳn thiết kế cũ,
   nơi trạng thái là nhãn runtime suy từ số liệu (`Hoàn thành`/`Đang thực hiện`/`Không tăng`/
   `Chưa có dữ liệu`). Bộ cũ **bỏ hẳn**; đừng port lại bất kỳ nhánh nào tính nó.
2. **Không phụ thuộc điểm.** Một chỉ tiêu có `verifiedScore = maxScore` vẫn có thể mang
   trạng thái `Cần bổ sung minh chứng`, và dữ liệu thật có đúng những dòng như vậy.
   Không viết luật kiểu "đủ điểm thì tự chuyển Hoàn thành".
3. **Giá trị ngoài 4 giá trị trên bị TỪ CHỐI**, không âm thầm bỏ qua và không tự map gần
   đúng. Ghi tay ⇒ `400`; nhập từ file ⇒ lỗi **dòng đó** (§6.3).
4. **So khớp**: cắt khoảng trắng đầu/cuối, so **không phân biệt hoa/thường**, **có** phân
   biệt dấu tiếng Việt. `"đang thực hiện"` khớp; `"Dang thuc hien"` **không** khớp — bỏ dấu
   làm `Chưa thực hiện` và `Chua thuc hien` cùng khớp, mà đó là hai chuỗi người dùng gõ với
   ý định khác nhau.

Trạng thái hiển thị bằng `Badge` có màu ở **cả hai** màn (Q10). Ánh xạ màu là việc của FE:
`spec/dashboard-dti/business-rules.md` §Trạng thái giữ bảng ánh xạ duy nhất.

---

## 5. Quy tắc kỳ và quy tắc ghi

### 5.1 Kỳ suy từ `AssessmentDate`

| `period` | Nghĩa | Khoảng ngày |
| --- | --- | --- |
| `"YYYY-Www"` | tuần **ISO-8601** | thứ Hai → Chủ nhật |
| `"YYYY-MM"` | tháng dương lịch | ngày 1 → ngày cuối tháng |
| `"all"` | cả năm `year` | 01/01 → 31/12 |

Tuần ISO 2026, tính bằng lịch (không gõ tay): W28 `06/07–12/07` · W29 `13/07–19/07` ·
W30 `20/07–26/07` · W31 `27/07–02/08` · W32 `03/08–09/08` · W33 `10/08–16/08` ·
W34 `17/08–23/08` · W35 `24/08–30/08`.

**Mọi chỗ hiển thị kỳ phải ghi rõ từ ngày đến ngày** (Q12) — nhãn kỳ, option chọn kỳ, dòng
lịch sử, trục X biểu đồ chế độ tuần. **Định dạng nhãn kỳ** có **một** file chủ:
`spec/dashboard-dti/business-rules.md` §Nhãn kỳ. Chuỗi copy verbatim của từng màn thì thuộc
screen spec (`doc/Design/Frontend/PlatformManager/Screens/`).

#### CHỈ NHẬP THEO TUẦN — tháng và năm là tổng hợp TỰ TÍNH, chỉ đọc (chốt Q37, 2026-09-06)

**Tuần ISO là đơn vị kỳ duy nhất mà dữ liệu được GHI vào.** Tháng và năm không phải một đơn
vị nhập liệu song song — chúng là kết quả **gộp** các kỳ-tuần khi đọc
(`spec/dashboard-dti/business-rules.md` §1.2; khối nhận dạng của file xuất theo tháng liệt kê
`Gồm các tuần`, §4.2 của cùng file).

| Kỳ người dùng chọn | Đọc | Ghi |
| --- | --- | --- |
| `"YYYY-Www"` — một tuần | ✔ | ✔ kể cả tuần **đã qua**, kể cả năm trước (Q20) |
| `"all"` | ✔ bản ghi mới nhất trong năm | ✔ — rơi vào **tuần hiện tại** (Q26) |
| `"YYYY-MM"` — một tháng | ✔ | ✘ **chỉ đọc** |
| năm (`mode=year` của Dashboard) | ✔ | ✘ **chỉ đọc** |

> **Vì sao cấm ghi theo tháng — đây là lỗi đo được, không phải sở thích.** Mô hình lưu một
> bản ghi theo `AssessmentDate` (một **ngày**), nên ghi cho "tháng 8/2026" buộc phải neo vào
> một ngày cụ thể trong tháng. Neo vào ngày cuối là 31/08/2026, mà **tuần ISO chứa 31/08/2026
> là tuần 36 (31/08 – 06/09)** — nửa nằm sang tháng 9. Cột `Kỳ của số liệu` (Q31) khi đó báo
> "tuần 36" cho số liệu người dùng nhập là "tháng 8", và Dashboard `mode=week` xếp nó vào
> tuần 36. Không có ngày neo nào trong một tháng bất kỳ mà tuần ISO của nó nằm gọn trong
> tháng đó — nên đây là lỗi **không vá được bằng cách chọn ngày neo khéo hơn**.
>
> Hai lối ra đã cân nhắc: (a) lưu kỳ đích thành một cột riêng, chấp nhận hai nguồn sự thật
> (ngày và kỳ) có thể lệch nhau; (b) chỉ cho ghi theo tuần. Người dùng chọn **(b)** ngày
> 2026-09-06. Muốn sửa số của tháng 8 thì chọn **một tuần cụ thể** trong tháng đó.

**"Kỳ hiện tại" = tuần ISO chứa ngày hệ thống hôm nay.** Đây là định nghĩa dùng cho Q26
(§5.3) — không phải tháng hiện tại, không phải "bản ghi mới nhất".

> **Điều Q37 KHÔNG chạm tới:** đường **đọc** giữ nguyên hoàn toàn — lọc lưới theo tháng vẫn
> chạy, Dashboard `mode=month`/`mode=year` vẫn chạy, và **export `mode=month` vẫn chạy**
> (`spec/dashboard-dti/business-rules.md` §4.1). Xuất một tháng là **tổng hợp**, không phải
> nhập liệu; gỡ nó đi là hiểu nhầm phạm vi của quyết định này.

> ⚠️ **Tuần ISO không nằm gọn trong một tháng, và cũng không nằm gọn trong một năm.**
> W31/2026 = `27/07–02/08` vắt qua hai tháng. Tuần cuối/đầu năm còn vắt qua hai năm —
> "tuần ISO thứ 1 của 2027" có thể bắt đầu từ tháng 12/2026. Mọi phép quy đổi phải dùng
> lịch ISO thật, **không** tự tính bằng `ngày / 7`.
>
> Ranh giới kiến trúc: *"tuần ISO thứ 33 của 2026 là từ ngày nào tới ngày nào"* là logic
> **thời gian thuần** — đặt ở Core được. *"lọc bản ghi đánh giá theo `AssessmentDate` trong
> khoảng đó"* là **nghiệp vụ** — thuộc `Business.*`. Đưa cái sau lên Core là hỏng ranh giới
> (`doc/huong_dan/wiki-core/be/15-import-export.md` §1).

### 5.2 Đọc — giá trị nào đại diện cho một kỳ

- `period` là tuần/tháng cụ thể ⇒ bản ghi có `AssessmentDate` **lớn nhất nằm trong** khoảng
  ngày của kỳ. Không có bản ghi nào trong kỳ ⇒ chỉ tiêu vẫn hiện một dòng, mọi trường đánh
  giá vắng mặt.
- `period = "all"` ⇒ bản ghi có `AssessmentDate` **lớn nhất trong năm** `year`.
- **Không carry-forward.** Kỳ trước có dữ liệu, kỳ này không, thì kỳ này hiện **rỗng** chứ
  không kéo số cũ sang. Kéo sang sẽ tạo ra một tuần "có tiến độ" mà thật ra không ai làm gì.

> **Q46 (2026-09-10) — luật "lấy bản `AssessmentDate` lớn nhất" là file chủ cho CẢ HAI màn.**
> Hai bản ghi cùng chỉ tiêu lọt vào cùng một tuần (§1.4: DB không chặn được ca này) thì bản
> đại diện là bản có `AssessmentDate` lớn nhất — ở lưới Danh mục **và** ở mọi phép tổng hợp của
> Dashboard. `spec/dashboard-dti/business-rules.md` trỏ về mục này, không chép lại. Kết quả luôn
> **xác định**: unique partial `("CriteriaId", "AssessmentDate")` ở §1.4 bảo đảm không có hai
> bản ghi còn sống cùng ngày để hoà nhau.

#### Kỳ của TỪNG DÒNG — thêm 2026-09-05 (Q31)

Ở chế độ `period = "all"`, mỗi dòng của lưới có thể đến từ **một kỳ khác nhau**: chỉ tiêu này
lấy bản ghi của tuần 12, chỉ tiêu kia của tuần 33. Người đọc không có cách nào biết điều đó
nếu lưới không nói ra — nên API trả **kỳ của chính bản ghi đang đại diện cho dòng đó**, và
màn hình hiện nó ở cột `Kỳ của số liệu`.

| Đại lượng | Giá trị |
| --- | --- |
| Kỳ của dòng | tuần ISO chứa `AssessmentDate` của bản ghi được §5.2 chọn ra ⇒ chuỗi `"YYYY-Www"` |
| Không có bản ghi nào trong phạm vi đang xem | **vắng mặt** (dòng vẫn hiện, các ô đánh giá trống) |

**Giá trị này LUÔN là một tuần, không bao giờ là tháng** — hệ quả trực tiếp của Q37: dữ liệu
chỉ vào hệ thống qua đường ghi theo tuần, nên mọi bản ghi đều thuộc đúng một tuần ISO. Ca
"dòng báo sai đơn vị" từng là mục Cần chốt của file này; Q37 đóng nó ở gốc.

Hai luật hiển thị đi kèm (chỗ hiện, lúc nào hiện) thuộc màn hình —
`spec/danh-muc-dti/ui-spec.md`. Luật ở đây chỉ nói **giá trị là gì**.

> **Q38 (2026-09-06) — cột này hiện KHOẢNG NGÀY, bỏ số tuần:** `10/08 – 16/08` (khoảng trắng
> quanh dấu gạch theo T14 — bản trước viết dính), không phải
> `Tuần 33 (10/08 – 16/08)`. Đây là **ngoại lệ có chủ đích** so với Q12, đăng ký ở
> `spec/dashboard-dti/business-rules.md` §6.2 — không phải chỗ quên áp luật. API **không đổi**:
> vẫn trả cả `assessmentPeriod` lẫn `assessmentPeriodLabel`; cột hẹp chọn hiển thị gì là việc
> của màn hình.

### 5.3 Ghi — upsert theo KỲ ĐÍCH, copy-forward các trường không gửi

> **Viết lại 2026-09-05 theo Q20 + Q26.** Bản trước ghi mọi thứ vào `AssessmentDate` = *hôm
> nay* và coi kỳ đã qua là chỉ đọc. Cả hai đã bị lật: kỳ là **dữ liệu của lời ghi**, do người
> dùng chọn.

Cả ba đường ghi — dialog (DM-4), sửa inline (DM-6), import (DM-7) — dùng **một** luật, không
phải ba. Luật đó có đúng hai bước.

#### Bước 1 — xác định KỲ ĐÍCH

| `period` client gửi | Kỳ đích |
| --- | --- |
| `"YYYY-Www"` | đúng tuần ISO đó — kể cả tuần **đã qua**, kể cả năm trước (Q20) |
| **`"all"`** | **kỳ hiện tại** = tuần ISO chứa hôm nay (§5.1) — **chỉ khi `year` đang xem là năm hiện tại** (T15) |
| `"YYYY-MM"` | **TỪ CHỐI** — `400 CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY` (Q37) |
| `"all"` + `year` ≠ năm hiện tại | **TỪ CHỐI** — `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR` (T15) |
| vắng mặt | `400 CRITERIA.ASSESSMENT_PERIOD_REQUIRED` — server **không** chọn hộ, xem ghi chú dưới |
| sai khuôn (`"2026-W99"`, `"tuần 33"`…) | `400 CRITERIA.ASSESSMENT_PERIOD_INVALID` |

> ### T15 (2026-09-06) — `"all"` không được nhảy năm
>
> Ca đo được: người dùng đặt `Năm = 2025`, để `Kỳ trong năm = Tất cả`, rồi sửa một ô. Theo
> Q26 thì `"all"` ghi được, và lời ghi rơi vào **tuần hiện tại của 2026** — một năm người dùng
> **không hề đang xem**. Bộ chọn kỳ nói "ghi được", còn lệnh ghi thì đi chỗ khác.
>
> **Chốt: xử lý y như ca chọn tháng của Q37 — chỉ đọc.** Ở tầng dữ liệu: lời ghi mang
> `period = "all"` **bắt buộc** kèm `year` (năm người dùng đang xem); `year` ≠ năm hiện tại ⇒
> `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`.
>
> **Vì sao server cần `year` gửi kèm:** `"all"` tự nó không mang năm nào cả. Server quy nó về
> tuần hiện tại và **không có cách nào biết** người dùng đang nhìn năm nào — trừ khi client
> nói ra. Không có `year` thì lớp chặn thứ hai không tồn tại, chỉ còn FE ẩn nút, mà ẩn nút là
> trải nghiệm chứ không phải luật. `year` **không** cần cho ca `"YYYY-Www"`: năm đã nằm sẵn
> trong chính chuỗi tuần.
>
> ⚠️ **T15 KHÔNG lật Q20 — khẳng định lại bằng Q41 (2026-09-10).** Chọn `Năm = 2025` rồi chọn
> **`Tuần 33/2025`** thì vẫn ghi được —
> kỳ đích là đúng tuần người dùng đang nhìn. Điều T15 chặn là **lối tắt `"all"`**, thứ giải
> nghĩa thành một kỳ không có trên màn hình. Đọc T15 thành "cấm ghi vào năm cũ" là hiểu
> ngược: cấm như vậy sẽ xoá sạch mục đích của Q20 (nhập bù cho kỳ đã qua).

> **Ba mã lỗi rời nhau, cố ý.** `NOT_WEEKLY` tách khỏi `INVALID` vì hai ca có **hai cách xử lý
> khác nhau ở FE**: chuỗi sai khuôn là bug của client (log rồi báo lỗi chung), còn "kỳ tháng"
> là tình huống người dùng gặp thật và cần câu dẫn đường *"chọn một tuần trong tháng để sửa"*.
> Gộp chúng làm câu hướng dẫn đó không dựng được.
>
> Trong luồng bình thường FE **không bao giờ** kích hoạt `NOT_WEEKLY`: chọn tháng thì
> `isEditable = false` nên không có control nào để bấm (§5.4). Mã này là **lưới chặn phía
> server**, đúng khuôn "FE ẩn là trải nghiệm, BE từ chối mới là luật".

> **`"all"` là ca mới của Q26, và nó KHÔNG chặn, KHÔNG hỏi lại.** Ở chế độ
> `Kỳ trong năm = Tất cả`, màn hình đang hiện "bản ghi mới nhất trong năm" của từng chỉ tiêu —
> tức không có kỳ nào đang được chọn. Người dùng vẫn sửa được, và lời sửa **rơi vào kỳ hiện
> tại**. Đây là quyết định của người dùng ngày 2026-09-05; bản trước từ chối ca này bằng
> `CRITERIA.ASSESSMENT_PERIOD_INVALID`.
>
> Người dùng biết lời ghi vừa rơi vào đâu nhờ cột `Kỳ của số liệu` (Q31): sau khi lưu, ô của
> dòng đó đổi sang kỳ hiện tại. Đó là **phản hồi**, không phải trang trí — nó phân biệt "vừa
> tạo số cho tuần này" với "vừa ghi đè số của một tuần cũ".
>
> ⚠️ Ca cần để mắt: nếu dòng đang hiện số của tuần 12 và người dùng sửa ở chế độ `Tất cả`, số
> cũ của tuần 12 **vẫn còn nguyên** — lời sửa tạo/cập nhật bản ghi của tuần hiện tại chứ không
> đụng tới tuần 12. Lưới sẽ hiện giá trị mới vì "mới nhất trong năm" nay là bản ghi vừa ghi.
>
> **`period` vắng mặt vẫn là lỗi**, không rơi về `"all"`. `"all"` là một lựa chọn người dùng
> thấy trên màn hình và chủ động để nguyên; `period` thiếu là một client quên gửi. Đối xử hai
> ca đó như nhau nghĩa là mọi bug quên-gửi-tham-số đều âm thầm ghi vào tuần này.

> **Q40 (2026-09-10) — SERVER quy đổi, FE gửi nguyên.** Phép quy `"all"` → tuần hiện tại nằm
> ở **một** chỗ là BE, cho cả ba đường ghi. FE gửi nguyên giá trị ô `Kỳ trong năm`, kể cả
> `"all"`, kèm `year`; server quy đổi và tự kiểm T15. Lịch ISO không có bản sao ở FE —
> `spec/danh-muc-dti/ui-spec.md` §7.4b (viết lại cùng ngày; bản trước nói ngược). Riêng import,
> tuần đã quy đổi được lưu vào `ImportJobs` lúc nhận request (Q45, §1.5).

#### Một nguyên tắc, ba quyết định — đọc cái này thay vì nhớ ba luật rời

> ### 🎯 **Lời ghi không bao giờ được rơi vào một kỳ mà người dùng KHÔNG nhìn thấy trên màn hình.**

Ba quyết định của hai vòng chốt là **ba lỗ hổng của cùng nguyên tắc đó**, bịt theo ba chiều
khác nhau:

| Chiều | Lỗ hổng | Bịt bằng |
| --- | --- | --- |
| *"kỳ nào vừa nhận số?"* | ở chế độ `Tất cả`, người dùng không biết lời ghi rơi vào tuần nào | **Q26** — cột `Kỳ của số liệu` đổi ngay sau khi lưu, nói thẳng kỳ đích |
| **đơn vị** kỳ | ghi cho "tháng 8" thì bản ghi nằm ở tuần 36, vắt sang tháng 9 | **Q37** — bỏ hẳn đơn vị tháng khỏi đường ghi |
| **năm** | `Tất cả` + năm 2025 ⇒ ghi vào tuần hiện tại của 2026 | **T15** — `"all"` chỉ hợp lệ khi đang xem năm hiện tại |

Ai thêm một lối ghi mới về sau thì đối chiếu với **nguyên tắc**, đừng đối chiếu với ba luật —
ba luật là hệ quả, và một lối vào thứ tư sẽ có lỗ hổng thứ tư mà chúng không phủ.

#### Bước 2 — ghi vào bản ghi nào của kỳ đích

1. Kỳ đích **đã có** bản ghi ⇒ **cập nhật đúng bản ghi mà §5.2 đọc ra cho kỳ đó** (bản có
   `AssessmentDate` lớn nhất trong kỳ). `AssessmentDate` của nó **giữ nguyên**.
2. Kỳ đích **chưa có** bản ghi ⇒ **tạo mới**, `AssessmentDate` theo luật neo dưới đây, và
   **sao chép** giá trị các trường **không** nằm trong request từ bản ghi gần nhất trước đó
   (copy-forward). Không có bản ghi trước ⇒ các trường đó để trống.

**Luật neo ngày** — kỳ đích luôn là **một tuần** (Q37), nên luật gọn lại còn một dòng:

```
AssessmentDate = hôm nay                     nếu hôm nay nằm TRONG tuần đích
               = Chủ nhật của tuần đích      nếu không
```

Hệ quả đọc thẳng ra được: ghi cho tuần hiện tại ⇒ neo vào hôm nay (đúng hành vi cũ, không đổi
gì cho đường dùng hàng ngày). Ghi bù cho tuần 30 ⇒ neo vào Chủ nhật của tuần 30. Không bao giờ
sinh ra một ngày **nằm ngoài** tuần đích, và không bao giờ sinh ra ngày ở tương lai trừ khi
chính tuần đích ở tương lai.

*Nhánh "ngày cuối của một kỳ THÁNG" từng có ở bản 2026-09-05 đã biến mất cùng Q37 — không còn
kỳ đích nào là tháng.*

> **Vì sao là "upsert theo KỲ" chứ không còn là "upsert theo NGÀY":** khi kỳ do người dùng
> chọn, hai lần sửa cùng một kỳ phải chồng lên nhau — nếu không, sửa tuần 30 vào thứ Hai rồi
> sửa lại vào thứ Tư sẽ để lại hai bản ghi khác ngày trong cùng tuần 30, và luật đọc §5.2 âm
> thầm chọn một cái. Ràng buộc DB (§1.4) vẫn là mức ngày; nó là **lưới an toàn**, không phải
> thứ cưỡng chế luật này.

> **Vì sao copy-forward:** không có nó, người sửa mỗi `Tiến độ %` sẽ tạo một bản ghi mới mà
> `SelfScore`/`VerifiedScore`/`Status` đều trống — và dashboard của tuần đó lập tức báo tụt
> về 0. Đây là lỗi "dữ liệu đúng theo từng lời ghi nhưng sai theo cái người ta đọc".

**Ranh giới giữa hai đường ghi** (Q9), không được nới:

| Đường | Trường được ghi |
| --- | --- |
| Dialog "Sửa chỉ tiêu" | 4 trường danh mục + **6** trường đánh giá: `SelfScore` · `VerifiedScore` · `Status` · `OwnerId` · `Deadline` · `Note` |
| Sửa **inline** trong lưới | đúng **2** trường: `ProgressPercent` · `Note` |

### 5.4 `canWrite` và `isEditable` — HAI cờ, hai câu hỏi khác nhau

> **Viết lại lần thứ ba, 2026-09-06 theo Q37 + Q39.** Lịch sử của trường `isEditable`: bản
> DRAFT = "kỳ đang xem có phải trạng thái Live không"; Q20 + Q26 rút hết nội dung đó đi
> (không còn kỳ nào chỉ đọc vì lý do *thời gian*); Q37 lại đưa **một** điều kiện về kỳ quay
> trở lại — nhưng là điều kiện khác hẳn: **đơn vị** của kỳ, không phải **tuổi** của kỳ.

```
canWrite   = người gọi được phép GHI dữ liệu DTI
isEditable = canWrite
             VÀ  đơn vị kỳ đang chọn là TUẦN (hoặc "all")        ← Q37
             VÀ  năm đang lọc cho phép kỳ đích nằm trong nó      ← T15
```

**Ba điều kiện, không phải hai** (Q27 + Q39 · Q37 · T15):

| # | Điều kiện | Sai thì người dùng phải làm gì |
| ---: | --- | --- |
| 1 | Người gọi **có quyền ghi** DTI | đi xin cấp quyền — không tự làm được |
| 2 | Đơn vị kỳ là **tuần** hoặc `Tất cả` | **chọn một tuần** trong tháng đang xem |
| 3 | Kỳ đích **nằm trong năm đang lọc** — thực tế chỉ chặn ca `Tất cả` + năm ≠ năm hiện tại | **chọn một tuần cụ thể** của năm đó, hoặc đổi năm về năm hiện tại |

| Trường | Trả lời câu hỏi | FE dùng để |
| --- | --- | --- |
| `canWrite` | *"Người này có quyền sửa dữ liệu DTI không?"* | Hiện/**ẩn** `+ Thêm chỉ tiêu`, `Import CSV/Excel`, `Sửa`, `Xoá` |
| `isEditable` | *"Ở bộ lọc đang đặt, có sửa được ngay bây giờ không?"* | Bật/**tắt** sửa inline và nút Lưu |
| `editBlockedBy` | *"Vì sao không sửa được, và người dùng đổi cái gì thì sửa được?"* | Chọn lời nhắc, và trỏ đúng ô lọc đang chặn |

**Cả ba đều ở CẤP MÀN** (cạnh `items` của response lưới), **không** lặp ở dòng nào — cả ba
điều kiện đều thuộc về request, không thuộc về dòng.

`canWrite = true` khi mang role `SuperAdmin` (break-glass, đi qua mọi `[RequirePermission]` —
`src/BE/Core/PlatformManager.Core.Infrastructure/Permissions/RequirePermissionFilter.cs:43`)
**hoặc** có ít nhất một role được cấp key ghi DTI (§6.5).

> ### `canWrite` phải ở CẤP MÀN — lỗ hổng ở giao của Q39 và T9
>
> Ca T9: chưa import lần nào, lưới có **0 dòng**. Nếu quyền chỉ sống trong từng dòng thì lúc
> đó không có dòng nào để đọc — trong khi đúng hai nút `+ Thêm chỉ tiêu` và `Import CSV/Excel`
> vẫn phải quyết ẩn hay hiện, và đó lại là lúc nút Import **quan trọng nhất** (việc duy nhất
> làm được trên màn rỗng). Một cờ chỉ tồn tại khi đã có dữ liệu thì vắng mặt đúng lúc cần nhất.
>
> **Một nguồn sự thật, không hai.** `canWrite` là nguồn; `isEditable` và `editBlockedBy`
> **suy ra từ nó** trong **cùng một** lần đánh giá của request:
>
> ```
> isEditable = true   ⟺   canWrite = true  VÀ  editBlockedBy = []
> canWrite   = false  ⇒   editBlockedBy = ["NO_WRITE_PERMISSION"]  (đúng một phần tử)
> canWrite   = true   ⇒   editBlockedBy ⊆ ["PERIOD_NOT_WEEKLY", "PERIOD_OUT_OF_YEAR"]
> ```
>
> Lưới rỗng vẫn có **đủ cả ba** — đó chính là lý do chúng ở cấp màn.
>
> FE **không** được suy quyền từ `items.length` (lưới rỗng không nói gì về quyền) cũng **không**
> từ `roles` của `GET /api/auth/me` — payload đó không mang permission-key, và ánh xạ role → key
> là dữ liệu chạy sửa được ở màn Phân quyền. Chi tiết + vì sao không nhét quyền vào `/me`:
> `doc/contracts/danh-muc-dti.md` DM-2 mục 3.

#### Vì sao PHẢI là hai cờ chứ không phải một

Sau Q37 và Q39, `isEditable = false` có **hai nguyên nhân khác nhau**, và mỗi nguyên nhân có
một cách sửa khác nhau:

| Nguyên nhân | Người dùng phải làm gì | Màn hình phải nói gì |
| --- | --- | --- |
| Không có quyền ghi (Q39) | xin cấp quyền — **không tự làm được** | ẩn hẳn mọi nút ghi |
| Đang chọn một kỳ THÁNG (Q37) | **chọn một tuần** trong tháng đó | giữ nguyên nút, báo "đang xem tổng hợp" |

Một cờ duy nhất buộc FE phải **đoán** nguyên nhân bằng cách tự đọc lại `period` mình vừa gửi.
Suy luận đó đúng hôm nay và sai vào ngày có nguyên nhân thứ ba — đúng khuôn hỏng đã xảy ra
với chính trường này hai lần trong hai ngày. Hai cờ tường minh rẻ hơn một cờ cộng một quy tắc
suy luận nằm trong đầu người viết FE.

#### Q39 — không có quyền thì vào được màn, nhưng CHỈ ĐỌC

Người thiếu quyền **vẫn vào được** `/danh-muc/dti`: không chặn ở route, không ẩn mục menu.
Họ xem, lọc, và xuất báo cáo bình thường; chỉ mất các đường ghi.

> **Vì sao không chặn hẳn màn:** Dashboard đã hiện **đủ 62 chỉ tiêu** cho mọi người đăng nhập
> (Q21). Chặn màn Danh mục vì thế không giấu được dữ liệu nào — nó chỉ làm người dùng gặp một
> màn 403 khó hiểu cho thứ họ đã đọc được ở trang chủ. Khác biệt thật giữa hai loại người dùng
> là **có nút sửa hay không**, nên hợp đồng chỉ cần diễn đạt đúng khác biệt đó.

#### Vì sao lý do phải là một MẢNG MÃ, không phải một `bool`

Ba điều kiện trượt cho ra **hai kiểu hiển thị** và **ba lời nhắc**:

| Điều kiện trượt | Hiển thị | Lời nhắc |
| --- | --- | --- |
| Không có quyền ghi (Q39) | **ẩn** nút — "không phải của bạn" | **không có dải băng** (khẳng định lại bằng Q51, 2026-09-10); đổi bộ lọc không cứu được |
| Kỳ là tháng (Q37) | **disabled** — "không phải lúc này" | bảo đổi ô `Kỳ trong năm` |
| Năm ≠ năm hiện tại khi kỳ = `Tất cả` (T15) | **disabled** | bảo đổi ô `Năm đánh giá` |

Một `bool` trần gộp cả ba, nên dải băng chỉ nói được câu chung *"chọn lại bộ lọc"* — bắt người
dùng thử từng ô để đoán ô nào đang chặn mình.

> 🔄 **LẬT 2026-09-10 (Q48).** Đoạn trên từng viết tiếp: *"Và **hai ô có thể cùng lúc trượt**
> (`Năm = 2025` **và** `Kỳ = Tháng 8`): nhắc một ô thì họ sửa xong vẫn thấy bảng chỉ đọc"*. Sai
> theo bất biến của hợp đồng (`doc/contracts/danh-muc-dti.md` DM-2 mục 3): `PERIOD_OUT_OF_YEAR`
> chỉ sinh khi `period = "all"`, còn `Tháng 8` là `period = "YYYY-MM"`. **`Năm = 2025` +
> `Tháng 8` cho đúng `["PERIOD_NOT_WEEKLY"]`.** Với ba mã hôm nay, hai mã lọc loại trừ nhau —
> khi có quyền, mảng mang **tối đa một** phần tử.
>
> Cái vấp thật của ca đó là **nối tiếp**, không phải đồng thời: đổi `Tháng 8` sang `Tất cả` thì
> rơi tiếp vào `PERIOD_OUT_OF_YEAR`. Lối thoát một bước duy nhất là **một tuần cụ thể của 2025**
> (Q41). **Q60 (2026-09-10)** chốt cách nhắc: đang xem năm hiện tại thì gợi ý *"chọn một tuần
> hoặc `Tất cả`"*; năm cũ thì **chỉ** *"chọn một tuần cụ thể"* — vì `Tất cả` của năm cũ chỉ đọc
> (T15). Câu chữ: `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` § Copy.

`editBlockedBy` vẫn liệt kê **mọi** điều kiện đang trượt (khi có quyền), theo **thứ tự cố
định** ở bảng trên — luật giữ nguyên để một điều kiện thứ tư về sau không phải đổi hợp đồng. Riêng ca thiếu quyền trả **đúng một** phần tử: hai mã kia là lời mời "đổi
bộ lọc đi rồi sửa được", nói câu đó với người không có quyền là dắt họ đi một vòng vô ích.

**FE KHÔNG tự suy lại ba điều kiện** từ bộ lọc nó đang giữ — suy lại là dựng nguồn sự thật thứ
hai, và nó sẽ lệch ngay lần có điều kiện thứ tư. Server tính, FE đọc mã rồi tra bảng dịch
(đúng khuôn `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` §3). Hai mã lọc **trùng tên** với
hai mã lỗi tương ứng của đường ghi, bỏ tiền tố `CRITERIA.ASSESSMENT_` — một bộ từ vựng, không
hai. Shape đầy đủ + bất biến: `doc/contracts/danh-muc-dti.md` DM-2 mục 3.

#### Hai lớp chặn, không phải một

1. FE ẩn/tắt control theo `canWrite` và `isEditable`.
2. **BE từ chối**, mỗi điều kiện một mã riêng:

| Điều kiện hỏng | BE trả |
| --- | --- |
| 1 — không có quyền | `403` qua `[RequirePermission]` (do filter, không phải mã của catalog DTI) |
| 2 — kỳ đích là tháng | `400 CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY` |
| 3 — `"all"` khi đang xem năm khác | `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR` |

Lớp thứ hai là lớp thật. Lớp thứ nhất chỉ là trải nghiệm — một client tự chế bỏ qua được lớp
một mà không bỏ qua được lớp hai. Cả ba mã (và mọi mã khác của cụm) phải khai trong catalog
`CriteriaErrors.cs` / `ImportErrors.cs`; `ArchTests` nhận diện file catalog bằng đuôi
`Errors.cs` và khai thiếu là test **đỏ** (`doc/contracts/danh-muc-dti.md` §2).

> **Hệ quả về shape (đổi 2026-09-06):** `isEditable` **đã rời khỏi `CriteriaRowDto`**. Cả ba
> điều kiện đều ở cấp request nên giá trị giống hệt ở mọi dòng — giữ một bản sao mỗi dòng chỉ
> tạo chỗ cho chúng lệch nhau, và **không dùng được ở ca lưới rỗng**, đúng lúc dải băng cần
> nói lý do nhất. Response của DM-4/DM-6 vì thế không mang `isEditable` nữa; lời ghi vừa thành
> công thì quyền và bộ lọc không đổi, FE giữ nguyên khối quyền của lần tải lưới gần nhất.

> `CRITERIA.ASSESSMENT_READONLY_PERIOD` **đã gỡ khỏi hợp đồng** (Q20) và **không quay lại**
> cùng Q37 — Q37 chặn theo **đơn vị kỳ**, dùng mã riêng `ASSESSMENT_PERIOD_NOT_WEEKLY` ở tầng
> validate đầu vào, không phải một xung đột trạng thái `409`.

### 5.5 Xoá chỉ tiêu

| Điều kiện | Hành vi |
| --- | --- |
| Chưa **từng** có bản ghi đánh giá nào (mọi năm) | **xoá cứng** — `hardDeleted: true` |
| Đã có ít nhất một bản ghi | **xoá mềm** — `hardDeleted: false`; lịch sử giữ nguyên |

BE quyết định, FE chỉ đọc kết quả. Chỉ tiêu đã xoá mềm **biến khỏi mọi lưới và mọi phép tổng
hợp**, kể cả khi xem lại kỳ cũ mà lúc đó nó còn sống — nếu không, tổng số chỉ tiêu của một
kỳ sẽ đổi tuỳ thời điểm người ta mở màn hình lên xem.

### 5.6 Dấu vết "ai sửa kỳ nào" — 4 trường audit của `BaseEntity`, KHÔNG bảng lịch sử

**Chốt Q28 (2026-09-05).** Khi kỳ đã qua ghi được (Q20), câu hỏi *"số của tuần 30 bị ai sửa,
lúc nào"* trở thành câu hỏi thật. Câu trả lời **không** phải một bảng lịch sử mới — nó là bốn
trường audit mà `CriteriaAssessment` đã thừa kế sẵn từ `BaseEntity`.

| Trường | Ghi cái gì | Ai ghi |
| --- | --- | --- |
| `CreatedBy` · `CreatedAt` | người tạo bản ghi đánh giá của kỳ đó + thời điểm | `AuditInterceptor` |
| `UpdatedBy` · `UpdatedAt` | **người sửa cuối cùng** + thời điểm | `AuditInterceptor` |

Cơ chế **đã chạy thật**, không phải thứ phải xây (đối chiếu source 2026-09-05):

- 4 trường khai ở `src/BE/Core/PlatformManager.Core.Domain/Common/BaseEntity.cs:24` (setter
  `public` là chủ đích, để interceptor ghi được từ ngoài entity).
- `src/BE/Core/PlatformManager.Core.Persistence/Interceptors/AuditInterceptor.cs:37`
  duyệt **mọi** `BaseEntity` đang `SaveChanges`, nhánh `Modified` ghi `UpdatedBy`/`UpdatedAt`
  ở `:51`. Entity mới thừa kế `BaseEntity` là **tự động** có, không phải đăng ký gì thêm.
- Giá trị `UpdatedBy` là `ICurrentUser.UserName` — **tên đăng nhập dạng chuỗi**, không phải
  `Guid` (`AuditInterceptor.cs:34`). Đổi tên đăng nhập về sau sẽ không đổi dấu vết đã ghi.

#### Ba giới hạn — đã được người dùng CHẤP NHẬN, không phải thiếu sót

| Không trả lời được | Vì |
| --- | --- |
| "Đã sửa **mấy lần**?" | mỗi lần ghi đè `UpdatedBy`/`UpdatedAt` của lần trước |
| "Giá trị **cũ** là bao nhiêu?" | không lưu ở đâu cả |
| "Ai là người sửa **thứ hai từ cuối**?" | chỉ nhớ người cuối |

Đây là **đánh đổi có ý thức**: một bảng lịch sử thay đổi giải quyết cả ba, nhưng kéo theo
bảng mới, chính sách lưu giữ (`10-data-retention.md`), màn hình tra cứu, và một quyết định về
dung lượng — trong khi câu hỏi thực tế cần trả lời là *"ai vừa động vào số này"*. Nếu về sau
cần đủ ba, đó là một lượt riêng, và **không** phá thứ đang có.

#### 🔴 ĐIỀU KIỆN TIÊN QUYẾT — nhật ký của import phải mang ĐÚNG người đăng nhập (Q35)

**Chốt Q35 (2026-09-06): `UpdatedBy` của một lần import phải là tài khoản đã bấm nút nạp
file, không phải `"system"`. Đây là ĐIỀU KIỆN TIÊN QUYẾT của DM-7** — không bật đường import
lên môi trường thật khi hạng mục này chưa xong.

**Hiện trạng đã đo (đối chiếu source 2026-09-10): ✅ CÓ THẬT — ba bước của chốt 2026-09-09 đã
thi công, seam `ICurrentUser` nay có HAI bản cài.** `Core.*` không sửa dòng nào; cả ba nằm trong
`PlatformManager.Api`.

| # | Việc | Ở đâu (2026-09-10) |
| ---: | --- | --- |
| 1 | Client filter chụp danh tính lúc enqueue, stash vào job parameter | `src/BE/PlatformManager.Api/Common/BackgroundJobIdentityFilter.cs:42` (`OnCreating`); nửa server đọc lại ở `:68` (`OnPerforming`) |
| 2 | Bản cài `ICurrentUser` **thứ hai**, đọc danh tính đã stash thay vì `HttpContext` | `src/BE/PlatformManager.Api/Common/BackgroundJobCurrentUser.cs:19`, đặt cạnh `HttpContextCurrentUser.cs`; chỗ chứa danh tính theo luồng chạy ở `BackgroundJobIdentityAccessor.cs:45` |
| 3 | Chọn bản cài theo **ngữ cảnh chạy** | `src/BE/PlatformManager.Api/Program.cs:145` — có `HttpContext` thì `HttpContextCurrentUser`, không thì `BackgroundJobCurrentUser`. Filter nối vào Hangfire ở `:209` |

`AuditInterceptor` **không đổi một dòng nào** — nó vẫn chỉ hỏi seam
(`src/BE/Core/PlatformManager.Core.Persistence/Interceptors/AuditInterceptor.cs:34`),
và `IBackgroundJobScheduler` giữ nguyên chữ ký.

**Đã xác minh bằng test, KHÔNG cần Docker:**
`src/BE/Tests/PlatformManager.Core.UnitTests/BackgroundJobs/AuditFieldsInBackgroundJobTests.cs:56`
ghép `AuditInterceptor` thật với bản cài thứ hai — có danh tính thì `UpdatedBy` là tên người đó,
không có thì `"system"` (`:83`), và không ném lỗi ở ca nào.
`BackgroundJobIdentityTests.cs:46` phủ ca âm "enqueue ngoài HTTP request".

⚠️ **Chưa xác minh (cần Docker/Postgres):** rằng filter thật sự được Hangfire gọi và worker thật
sự dequeue job mang danh tính — đó là *seam activation test*
(`doc/huong_dan/wiki-core/be/04-testing-strategy.md` §"Seam activation test"), thuộc
`PlatformManager.Core.IntegrationTests` và **chưa viết**. Nghiệm thu "chạy một lần import bằng
tài khoản A" bên dưới vẫn phải làm tay khi DM-7 có đường import thật.

**Trạng thái TRƯỚC khi thi công (đo 2026-09-06)** — giữ lại vì nó là lý do của quyết định: import
chạy trong **job nền** (Hangfire), ở đó **không có `HttpContext`**, nên
`HttpContextCurrentUser.IsAuthenticated` trả `false`
(`src/BE/PlatformManager.Api/Common/HttpContextCurrentUser.cs:15` — `User` lấy từ
`IHttpContextAccessor.HttpContext?`; khi đó đó là bản cài đặt **duy nhất** của seam), và
`AuditInterceptor` rơi vào nhánh `userName ?? "system"` (`AuditInterceptor.cs:43` và `:49`).
Nghĩa là thao tác **rủi ro nhất** — nạp đè 62 dòng lên một kỳ đã chốt số liệu — lại là thao tác
**không có tên người**.

**Tầng nghiệp vụ không tự vá được**, và đó là lý do việc này thuộc **host** chứ không thuộc
`Business.*`: interceptor ghi đè `UpdatedBy` **vô điều kiện** ở cả hai nhánh
`Added`/`Modified`, nên một handler trong `Business.*` có gán tay giá trị trước
`SaveChangesAsync` thì cũng bị ghi đè lại.

##### Cách làm — ĐI ĐƯỜNG HOST, **0 dòng sửa trong `Core.*`** (chốt 2026-09-09)

| # | Việc | Ở đâu |
| ---: | --- | --- |
| 1 | **Hangfire client filter** đọc `ICurrentUser.UserName` lúc enqueue — thời điểm còn nằm trong HTTP request, nên seam còn trả đúng người — rồi stash vào **job parameter** | `PlatformManager.Api` (host) |
| 2 | **Bản cài `ICurrentUser` thứ HAI** cho job nền: đọc danh tính đã stash thay vì đọc `HttpContext`. Đặt **cạnh** `src/BE/PlatformManager.Api/Common/HttpContextCurrentUser.cs` | `PlatformManager.Api` (host) |
| 3 | `Program.cs` **chọn bản cài theo ngữ cảnh** — đường đăng ký **lúc chốt (2026-09-09)** là `builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>()`, một dòng duy nhất; worker Hangfire phải phân giải ra bản cài thứ hai | `PlatformManager.Api` (host) |

> 🔄 **SỬA 2026-09-10.** Ô số 3 trước neo vào dòng đăng ký một-bản-cài trong `Program.cs`.
> Dòng đó **không còn tồn tại** — Q35 đã thi công, nên neo bị gỡ thay vì dời. Neo thật của
> trạng thái hôm nay nằm ở bảng §"Đã thi công" phía trên
> (`src/BE/PlatformManager.Api/Program.cs:145`); bảng kế hoạch này cố ý mô tả trạng thái
> **trước** khi làm.

**`AuditInterceptor` KHÔNG phải sửa.** Nó đã làm đúng việc của nó: hỏi `ICurrentUser`. Vấn đề
nằm ở chỗ trong worker, seam đó trả về "không ai". Sửa interceptor để nó nhận danh tính từ
ngoài là đi vòng qua seam và tạo đường thứ hai ghi trường audit — đúng thứ mà việc gom tất cả
vào một interceptor sinh ra để tránh.

**`IBackgroundJobScheduler` KHÔNG mở rộng.** Chữ ký hôm nay
(`src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs:29`)
giữ **nguyên**, không thêm tham số danh tính, không thêm overload.

> ### 🔄 LẬT 2026-09-09 — bản trước của mục này ghi *"`IBackgroundJobScheduler` **PHẢI** mở
> rộng"* và xếp cả ba bước vào Core
>
> Đường đã chốt là **client filter**, và nó tốt hơn ở đúng chỗ quan trọng nhất: filter chạy
> cho **mọi** job đi qua Hangfire, kể cả job viết sau này và job không ai nhớ tới. Mở rộng
> chữ ký thì mỗi nơi enqueue phải **nhớ truyền** danh tính, và nơi nào quên sẽ lặng lẽ ghi
> `"system"` trở lại — tức bug này quay về, một điểm gọi một lần, không có gì canh.
>
> Nguyên tắc bên dưới **không đổi** và chính nó chọn ra đường mới: danh tính là **ngữ cảnh
> chạy**, không phải tham số nghiệp vụ của từng job. Filter đặt nó vào đúng tầng ngữ cảnh
> (hạ tầng job), trong khi mở rộng chữ ký lại kéo nó vào hợp đồng mà mỗi nơi gọi phải khai.
>
> Hệ quả thứ hai, đáng giá không kém: **`Core.*` không phải sửa dòng nào.** Cả ba bước nằm
> trong `PlatformManager.Api` — nơi vốn đã là chỗ hợp lệ duy nhất biết tới `HttpContext` và
> tới Hangfire cùng lúc. Việc này vì thế **không** còn là "hạng mục Core"; xem lại nhãn ở
> `doc/contracts/danh-muc-dti.md` §4, đã sửa cùng ngày.

##### Ranh giới và điều kiện nghiệm thu

- **Không sửa `Core.*`** ⇒ lượt thi công **không** bắt buộc đi qua `core-reviewer` vì lý do
  Q35 (vẫn đi nếu lượt đó chạm core vì việc khác). Cũng vì vậy **không** có luật mới nào phải
  chuyển sang `doc/huong_dan/wiki-core/be/` khi làm xong — mục này ở lại đây.
- ⚠️ **Nhưng bản cài `ICurrentUser` thứ hai vẫn là hạ tầng dùng chung của host.** Nó phục vụ
  mọi job nền tương lai, không riêng import. Đừng đặt nó trong thư mục của feature import,
  và đừng để tên nó nhắc tới DTI.
- Nghiệm thu: chạy một lần import bằng tài khoản A, đọc `UpdatedBy` của bản ghi đánh giá vừa
  ghi — phải là tên đăng nhập của **A**, không phải `"system"`. Ca âm cũng phải kiểm: một job
  nền **không** do người dùng kích hoạt (nếu có) vẫn ghi `"system"` chứ không ném lỗi.
- Ca âm thứ hai, riêng cho đường filter: một job enqueue **ngoài** HTTP request (vd từ
  `SeedCommand` hoặc một recurring job) không được làm filter ném lỗi — không có ai để chụp
  thì stash rỗng, và bản cài thứ hai trả `null` để `AuditInterceptor` rơi về `"system"` như cũ.

#### Hệ quả của Q20 lên báo cáo đã xuất

Kỳ đã qua ghi được nghĩa là **một file `.xlsx` đã tải về hôm nay có thể mâu thuẫn với số của
chính kỳ đó vào tháng sau**. Đây là hệ quả trực tiếp của quyết định, không phải lỗi.

Thứ giữ cho nó vô hại đã nằm sẵn trong bố cục file đã duyệt: dòng `Ngày xuất` ở khối nhận
dạng kỳ (`spec/dashboard-dti/business-rules.md` §4.2, dòng 8) cho người cầm file biết mình
đang giữ ảnh chụp lúc nào. Đừng bỏ dòng đó đi cho gọn.

---

## 6. Import

Luật chung của Core (seam `IImportFileReader`, thư viện CsvHelper + NPOI, NPOI **chỉ** ở
`Core.Infrastructure`, nhận diện định dạng bằng **magic byte**, ba lỗi phải sửa khi bê code
cũ): `doc/huong_dan/wiki-core/be/15-import-export.md` §2. **Không lặp lại ở đây.** Mục này
chỉ giữ phần riêng của DTI.

### 6.1 Định dạng và cách đọc

- Nhận `.csv`, `.xlsx`, `.xls` (Q6).
- Excel: đọc **sheet đầu tiên**, dòng 1 = header. Không hỗ trợ nhiều sheet, không hỗ trợ ô
  gộp (merged cell) ở bản đầu.
- CSV: tự nhận BOM. File BA gửi **có** BOM UTF-8.
- Chạy **nền** qua Hangfire, FE poll trạng thái. Hạ tầng có sẵn ở Core, không dựng mới.

### 6.2 Ánh xạ 11 cột

Khớp header theo **tên cột**, không theo vị trí. So khớp: cắt khoảng trắng, không phân biệt
hoa/thường, **có** phân biệt dấu.

| # | Cột trong file | Trường | Bắt buộc |
| ---: | --- | --- | --- |
| 1 | `Mã` | `Criteria.Code` | ✔ |
| 2 | `Chỉ tiêu` | `Criteria.Name` | ✔ (khi tạo mới) |
| 3 | `Nhóm` | tra `CriteriaGroup.Name` → `Criteria.GroupId` | ✔ |
| 4 | `Điểm tối đa` | `Criteria.MaxScore` | ✔ (khi tạo mới) |
| 5 | `Tự đánh giá` | `CriteriaAssessment.SelfScore` | |
| 6 | `Thẩm định` | `CriteriaAssessment.VerifiedScore` | |
| 7 | `Chênh lệch` | **BỎ QUA** — trường tính (§3.1) | |
| 8 | `Trạng thái` | `CriteriaAssessment.Status` | |
| 9 | `Phụ trách` | tra `AppUser.FullName` → `OwnerId` | |
| 10 | `Hạn xử lý` | `CriteriaAssessment.Deadline` | |
| 11 | `Minh chứng/Ghi chú` | `CriteriaAssessment.Note` | |

**Cột 7 đọc vào rồi vứt, không đối chiếu.** Nếu đối chiếu và báo lỗi khi lệch, một file
người dùng sửa tay ở cột 5 mà quên sửa cột 7 sẽ bị từ chối — trong khi cột 7 vốn không phải
dữ liệu.

> ⚠️ **Sau Q25 thì luật "bỏ qua" này còn quan trọng hơn trước.** Cột `Chênh lệch` trong file
> BA gửi tính theo chiều **cũ** (`Tự đánh giá − Thẩm định`), tức **ngược dấu** với thứ hệ
> thống tính ra (§3.1). Một đường import có đối chiếu cột này sẽ từ chối **27/62 dòng của
> chính file gốc** — và câu lỗi sẽ trông như dữ liệu của BA sai, trong khi nó đúng theo quy
> ước của họ. Đọc vào rồi vứt là cách duy nhất để hai quy ước cùng tồn tại được.

#### Cột VẮNG KHỎI FILE ≠ ô TRỐNG trong một cột có mặt — **Q74 (chốt 2026-09-11)**

Đây là hệ quả trực tiếp của §5.3 bước 2 (*"sao chép giá trị các trường **không nằm trong
request**"*), nhưng bảng 11 cột ở trên không nói ra, và bỏ sót nó làm **mất dữ liệu im lặng**:

| Trong file | Nghĩa của lời ghi | Kết quả |
| --- | --- | --- |
| Cột **có mặt**, ô trống (hoặc ghi `—`) | xoá trắng CÓ CHỦ ĐÍCH | ghi `null` |
| Cột **vắng mặt** khỏi dòng header | trường không nằm trong lời ghi | copy-forward khi tạo bản ghi mới, giữ nguyên khi cập nhật |

**Ca đo được (2026-09-11, chạy thật trên `platformmanager_dev`):** nạp một file chỉ có
`Mã · Chỉ tiêu · Nhóm · Điểm tối đa · Tự đánh giá · Thẩm định` — đúng kiểu file *"số liệu tuần
này"* mà BA hay gửi. Nếu bốn cột vắng mặt bị đọc như "gửi null" thì lượt nạp đó **xoá trắng**
`Trạng thái`, `Phụ trách`, `Hạn xử lý` và `Minh chứng/Ghi chú` của kỳ đích. Không lỗi nào báo, và
người dùng chỉ phát hiện khi mở lưới ra xem.

**Hai vế, hai lý do tồn tại — bỏ vế nào cũng hỏng một chiều:**

- **Vế "ô trống ⇒ xoá trắng"** là thứ vòng **export → sửa trong Excel → import** cần: file xuất
  luôn có đủ 12 cột, nên xoá nội dung một ô trong Excel phải xoá được giá trị. Không có vế này thì
  không có cách nào xoá một ô bằng đường nạp file.
- **Vế "cột vắng ⇒ copy-forward"** bảo vệ file *"số liệu tuần này"* — kiểu file chỉ mang vài cột mà
  BA gửi thường xuyên. Không có vế này thì mỗi lần nạp như vậy là một lần xoá trắng bốn cột.

> 🧭 **Bài học đáng giữ, không phải chi tiết thi công:** bản cài đầu tiên gộp hai vế làm một, và
> lỗi đó **lọt qua toàn bộ vòng đọc tài liệu** — nó chỉ lộ ra khi **chạy thật** trên
> `platformmanager_dev` và nhìn dòng dữ liệu sau khi nạp. Một luật suy được từ §5.3 nhưng không
> được VIẾT RA ở §6.2 là một luật mà người thi công phải tự suy lại, và lần suy sai không có cổng
> nào bắt.

🔴 **Áp cho CẢ BA đường ghi, không riêng import.** DM-3 và DM-4 nhận JSON, nên "cột vắng" ở đó là
**khoá vắng mặt khỏi thân request**, còn "ô trống" là **khoá có mặt mang giá trị `null`**. Hai
đường ghi trả lời khác nhau cho cùng một câu hỏi là đúng thứ §5.3 sinh ra để chặn.

⚠️ **Hai cột `Mã` và `Nhóm` thì khác:** thiếu *header* của chúng là lỗi của **cả file**, không
phải lỗi dòng — thiếu `Mã` thì không định danh được dòng nào, còn `Nhóm` bắt buộc ở mọi dòng nên
thiếu header sẽ sinh đúng một lỗi giống hệt nhau cho từng dòng, và một danh sách như vậy không nói
được điều gì mà một câu không nói được.

⚠️ **Ô công thức trong `.xlsx`/`.xls`:** NPOI trả **chuỗi công thức** chứ không phải kết quả
nếu đọc thẳng `cell.ToString()` — ô `=B2*100` sẽ vào DB thành chữ `"B2*100"`. Phải đọc
`CachedFormulaResultType` rồi lấy theo đúng kiểu. Đây là **lỗi im lặng**: không crash, không
log, chỉ có dữ liệu rác nằm chờ (`15-import-export.md` §2b).

⚠️ **Ngày:** giữ kiểu ngày thật qua seam, **không** ép về chuỗi `dd/MM/yyyy` rồi parse ngược
— vừa mất thông tin vừa dính locale (`15-import-export.md` §2c). Cột `Hạn xử lý` **rỗng toàn
bộ** trong file BA gửi, nên đường này sẽ không có ai thử cho tới khi có file thật khác.

### 6.3 Luật từng dòng

| Tình huống | Xử lý |
| --- | --- |
| `Mã` chưa có trong hệ thống | **tạo `Criteria` mới**, đếm vào `criteriaCreatedCount` |
| `Mã` đã có | cập nhật đánh giá; **không** đổi `Name`/`MaxScore`/`GroupId` của chỉ tiêu đã có |
| `Nhóm` không khớp `CriteriaGroup.Name` nào | **lỗi dòng đó** — KHÔNG tự tạo nhóm mới |
| `Trạng thái` ngoài 4 giá trị §4 | **lỗi dòng đó** |
| `Tự đánh giá` hoặc `Thẩm định` > `Điểm tối đa` | **lỗi dòng đó** |
| `Phụ trách` không khớp `AppUser.FullName` nào | **KHÔNG lỗi** — `OwnerId` để trống |
| `Mã` rỗng | lỗi dòng đó |
| `Mã` xuất hiện hai lần trong cùng file | lỗi dòng **thứ hai** |
| Một đoạn của `Mã` quá 4 chữ số (Q58, §2) | lỗi dòng đó — `IMPORT.ROW_CODE_SEGMENT_TOO_LONG` |

##### Năm tình huống lỗi dòng mà bảng trên bỏ sót — **Q73 (chốt 2026-09-11)**

Bảng trên (và bảng mã ở `doc/contracts/danh-muc-dti.md` §`errors[].code`) **không phủ** năm ca
dưới đây. Chúng vẫn phải có mã, vì cả ba lối xử lý còn lại đều tệ hơn: bỏ qua thì mất dữ liệu im
lặng; để `DomainException` bay lên thì theo **Q64** cả file không dòng nào được ghi — một ô trống
làm hỏng những dòng đúng còn lại; điền giá trị mặc định thì bịa số liệu, đúng thứ Q24 cấm.

| Tình huống | Mã | `messageParams` |
| --- | --- | --- |
| Mã CHƯA CÓ trong hệ thống (⇒ phải tạo mới) nhưng cột `Chỉ tiêu` rỗng | `IMPORT.ROW_NAME_MISSING` | `Code` |
| Như trên, cột `Điểm tối đa` rỗng / không phải số / `<= 0` | `IMPORT.ROW_MAX_SCORE_INVALID` | `Code`, `MaxScore` |
| Ô `Tự đánh giá` có nội dung nhưng không đọc ra số | `IMPORT.ROW_SELF_SCORE_INVALID` | `Code`, `SelfScore` |
| Ô `Thẩm định` có nội dung nhưng không đọc ra số | `IMPORT.ROW_VERIFIED_SCORE_INVALID` | `Code`, `VerifiedScore` |
| Ô `Hạn xử lý` có nội dung nhưng không đọc ra ngày | `IMPORT.ROW_DEADLINE_INVALID` | `Code`, `Deadline` |

Hai cột điểm dùng **hai mã rời**, cùng lý do đã ghi cho cặp `*_EXCEEDS_MAX`: câu người dùng đọc
phải nói đúng ô nào cần sửa.

**Người dùng duyệt NGUYÊN VĂN năm tên trên ngày 2026-09-11 (Q73).** Đổi tên thì đổi ở đúng hai
chỗ: catalog `ImportErrors.cs` và bảng `errors[].code` của card — không có chỗ thứ ba giữ chuỗi này.

> 🛑 **Đừng "dọn cho gọn" bằng cách bỏ năm mã này đi.** Lý do chúng tồn tại nằm ở giao của hai luật
> đã chốt: §6.2 khai `Chỉ tiêu`/`Điểm tối đa` là bắt buộc **khi tạo mới**, còn **Q64** bắt cả lượt
> nạp chạy trong MỘT giao dịch. Để `DomainException` của `Criteria.Create` bay lên thì job thành
> `Failed` và theo Q64 **không dòng nào** được ghi — tức một ô trống ở một dòng làm hỏng toàn bộ
> những dòng đúng còn lại. Năm mã này là thứ giữ cho một ô hỏng chỉ hỏng đúng một dòng.

**Vì sao "nhóm lạ" là lỗi còn "phụ trách lạ" thì không:** nhóm là **danh mục đóng** do BA
quản (6 nhóm), sai nhóm nghĩa là sai chính tả hoặc thừa khoảng trắng — tự tạo nhóm thứ 7 làm
mọi phép tổng hợp theo nhóm sai ngay và không ai thấy. Còn `Phụ trách` là **tham chiếu mềm**
sang danh sách người dùng của Core: người phụ trách chưa có tài khoản là chuyện bình thường,
chặn cả dòng vì lý do đó là chặn dữ liệu đúng. Trong file BA gửi, cột này **rỗng toàn bộ**.

`Phụ trách` khớp theo **tên đầy đủ**, cắt khoảng trắng, không phân biệt hoa/thường, **có**
phân biệt dấu. Khớp nhiều hơn một người ⇒ để trống (không đoán).

##### 🚧 ĐÃ CHỐT — ĐANG THI CÔNG: `IUserLookupService` của Core phải SỬA theo luật trên (2026-09-09)

Luật ở bảng trên là luật đúng, và **Core hôm nay làm ngược nó**. Đây là một ràng buộc mà DTI
đặt lên Core, nên ghi ở đây; việc sửa thuộc lượt code Core.

| Có thật hôm nay (đối chiếu source 2026-09-09) | Sẽ thành |
| --- | --- |
| Không ai khớp ⇒ **tự tạo `AppUser` mới** (UserName/mật khẩu tự sinh vô danh, `MustChangePassword = true`) — khai trong docstring `src/BE/Core/PlatformManager.Core.Application/Users/IUserLookupService.cs:14` | Không ai khớp ⇒ trả **`null`**, `OwnerId` để trống. **Bỏ hẳn nhánh tự tạo** |
| Chữ ký `ResolveOrCreateByFullNameAsync` mang chữ `Create` trong tên (`IUserLookupService.cs:19`) | Đổi tên cho khớp hành vi mới — không còn `Create` |
| Kiểu trả `(Guid? OwnerId, bool WasCreated)` — cờ `WasCreated` chỉ có nghĩa khi còn nhánh tạo | Bỏ cờ, còn `Guid?` |
| Docstring `:7` trỏ *"`spec/danh-muc-dti/business-rules.md` mục 2.2 câu #16"* | Trỏ **§6.3 của file này**. Mục `2.2` **không tồn tại** trong file — §2 là "Mã chỉ tiêu", không có mục con nào |

**Vì sao bỏ nhánh tự tạo.** Một tài khoản sinh ra từ một ô văn bản trong file Excel là tài
khoản **không ai chủ động cấp**: nó không đi qua màn Quản trị người dùng, không có email, và
tồn tại chỉ vì ai đó gõ đúng một cái tên vào cột `Phụ trách`. Người phụ trách chưa có tài
khoản là chuyện bình thường (§ ngay trên), nên câu trả lời đúng là **để trống**, không phải
tạo ra một danh tính rồi để đó. `null` cũng làm hành vi của cả ba nhánh nhất quán: không
khớp ai và khớp nhiều người đều cho ra cùng một kết quả — `OwnerId` trống, dòng vẫn nạp.

**Điều KHÔNG đổi:** nhánh khớp đúng một người vẫn trả `Id` đó, và nhánh khớp ≥2 người vẫn trả
`null`. Hai nhánh này đã đúng.

⚠️ Không sửa file `.cs` từ tài liệu — mục này là **đầu vào** cho lượt code, không phải bằng
chứng rằng nó đã xong. Docstring sai ở `:7` cũng để lượt đó sửa cùng lúc.

**Q64 — cả lượt nạp chạy trong MỘT giao dịch (chốt 2026-09-10).** Job `Failed` (lỗi hạ tầng,
job chết giữa chừng) ⇒ **không dòng nào** được ghi; kỳ đích giữ nguyên y như trước khi nạp.
Dòng lỗi thì vẫn chỉ bị bỏ qua như dưới đây — chúng **không** làm job `Failed`, nên không có
gì để cuộn ngược. Vì sao chọn nguyên tử: một file cỡ vài chục dòng thì chi phí giao dịch
không đáng kể, còn ghi theo lô sẽ để lại một kỳ **đã chốt số** bị nạp đè một nửa mà không ai
biết — và khi đó câu báo cho người dùng buộc phải nói ra điều đó.

**Một dòng lỗi không làm hỏng cả file.** Job vẫn `Succeeded`; lỗi từng dòng nằm trong
`result.errors` kèm `rowNumber` (số dòng **trong file người dùng gửi**, dòng 1 = header) để
người dùng mở file ra sửa đúng chỗ.

### 6.4 `AssessmentDate` và `Tiến độ %` khi import

- **`AssessmentDate` theo KỲ ĐÍCH của file, không theo ngày chạy job.** Người dùng chọn kỳ
  trước khi nạp (`period` của DM-7); áp **nguyên** luật hai bước ở §5.3, kể cả luật neo ngày
  và ca `"all"` ⇒ tuần hiện tại. **Một luật ghi cho cả ba đường** — không có luật riêng cho
  import.

  Hệ quả của Q37: kỳ đích của một file import **chỉ được là một TUẦN** (hoặc `"all"`). Nạp
  một file cho "tháng 8" là không hợp lệ ⇒ `400 IMPORT.PERIOD_NOT_WEEKLY`. Muốn nạp số liệu
  cả tháng thì nạp theo từng tuần — và đó cũng đúng cách BA đang làm việc, file mẫu của họ là
  file **một kỳ**.

  > Bản trước ghi *"`AssessmentDate` = ngày hệ thống lúc job chạy"*, nghĩa là nạp một file
  > của tuần 33 vào tháng 9 sẽ đổ dữ liệu đó vào tuần 36. Bỏ theo Q20.

  Chạy import hai lần cho **cùng một kỳ** thì lần sau **ghi đè** lần trước (bước 2 mục 1 của
  §5.3), không tạo bản ghi thứ hai.

- **`ProgressPercent` để TRỐNG — chốt Q24 (2026-09-05).** Không phải `0`, không suy từ điểm.
  File BA gửi **không có** cột `Tiến độ %`, và đây là trường **nhập tay** (§3.2) — điền hộ
  một giá trị khởi tạo nghĩa là bịa ra số liệu mà không ai ký tên.

  > **Hệ quả bắt buộc phải biết, và người dùng đã chấp nhận:** thanh tiến độ theo nhóm và
  > biểu đồ đường của Dashboard vẽ theo **chính** trường này (Q11). Nên **ngay sau mỗi lần
  > import, Dashboard trống** — không phải 0%, mà là "chưa có dữ liệu" — cho tới khi có người
  > nhập tay. Đây là **trạng thái bình thường**, phải hiển thị tử tế chứ không phải lỗi cần
  > vá: `spec/dashboard-dti/business-rules.md` §1.1 (loại khỏi mẫu tính, không tính là 0) và
  > `doc/contracts/dashboard.md` §0 (các trường tổng hợp **vắng mặt**, không bằng `0`).
  >
  > Vì vậy các con số 74,3% · 51,8% … trong prototype là ảnh chụp **sau khi người dùng đã
  > nhập**, không phải ảnh chụp ngay sau import. Đừng dùng chúng làm kỳ vọng cho ca import.

  > **Phương án đã BỎ:** `progressPercent = round(selfScore / maxScore × 100)`. Nó làm
  > Dashboard đẹp ngay sau import, nhưng biến `Tiến độ %` thành nửa-trường-tính
  > nửa-trường-nhập: người sau nhìn công thức đó sẽ tính lại nó ở một chỗ khác và **ghi đè con
  > số người dùng đã gõ tay**. Một trường chỉ được có một chủ.

### 6.5 Quyền ghi — MỘT key cho toàn bộ DTI (chốt Q27, 2026-09-05)

Câu hỏi "ai được ghi dữ liệu DTI" **không còn để ngỏ**. Trả lời: **đúng một permission-key**,
phủ **mọi** đường ghi của cả nghiệp vụ — tạo/sửa/xoá chỉ tiêu, lưu đánh giá qua dialog, sửa
inline, **và import**.

| | Chốt |
| --- | --- |
| Số lượng key | **một** |
| Phạm vi kỳ | **mọi kỳ**, kể cả kỳ đã qua và năm trước |
| Tách "sửa kỳ hiện tại" / "sửa kỳ cũ" | **không** |
| Khái niệm "chốt kỳ" / khoá kỳ | **không tồn tại** |
| Quyền **xem** | không cần key — chỉ cần đăng nhập (Q21, `doc/contracts/dashboard.md` §0) |

**Tên key: `dti.manage`.** Nhãn hiển thị trên màn phân quyền: `Nhập & sửa dữ liệu DTI`.

Quy ước đặt tên không phải do file này nghĩ ra — nó đọc từ hai nguồn:

- `doc/huong_dan/quy-uoc/be-api-controller.md:550` — *"action luôn khai tường minh trong
  `key` (vd `import.manage` **dùng chung cho mọi thao tác ghi** của Import: tải file lên, huỷ
  job, xoá job)"*. Khuôn là `{tài nguyên}.{hành động}`, chữ thường, và **một key `.manage`
  phủ hết đường ghi của một tài nguyên** — đúng hình dạng Q27 yêu cầu. Cùng mục, dòng 571:
  quyền áp cho endpoint **ghi** trước, endpoint chỉ đọc giữ `[Authorize]` trần — khớp Q21.
- `doc/contracts/permissions.md:216` — hai key `criteria.manage` / `criteria-groups.manage`
  của module DtiWeekly cũ đã bị gỡ 2026-08-29. Chúng là **hai**; Q27 chốt **một**, nên không
  khôi phục cặp đó. Chọn `dti.manage` thay vì `criteria.manage` vì phạm vi rộng hơn
  `/api/criteria`: nó phủ cả đường import. Đặt tên hẹp là mời người sau khai thêm một key thứ
  hai cho import — đúng thứ Q27 cấm.

#### KHÔNG dùng lại `import.manage`

Key đó còn trong `AppResourceKeys`
(`src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs:33`) nhưng docstring ngay
trên nó khai rõ đó là **di sản** của module đã gỡ, giữ lại chỉ vì bảng `RolePermissions` đã
seed có dòng mang key này. Dùng lại một key vì nó "trông đúng tên" sẽ trộn quyền của một năng
lực Core dùng chung với quyền của một nghiệp vụ — và khi sản phẩm thứ hai dùng lại CoreBase,
hai thứ đó phải tách rời được.

#### Ba việc phải làm khi thi công, thiếu bước nào cũng hỏng im lặng

1. Thêm một `const` vào `AppResourceKeys` **và** một dòng vào
   `AppResourceKeySource.Definitions` (ở host, **không** sửa vào Core —
   `src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs:17` mô tả đúng ba bước
   này). Thiếu bước thứ hai: key không hiện trên màn phân quyền, không role nào cấp được,
   endpoint 403 cho tất cả trừ SuperAdmin.
2. Gắn `[RequirePermission(...)]` lên **mọi** action ghi của DM-3…DM-7. Endpoint đọc (DM-1,
   DM-2, DM-8) giữ `[Authorize]` trần.
3. Kiểm ma trận sau khi seed — xem cảnh báo dưới.

#### Seed chỉ cấp cho `Admin` — NGOẠI LỆ có chủ đích so với luật seed hiện tại (Q36)

**Chốt Q36 (2026-09-06): lần seed đầu cấp `dti.manage` cho `Admin` và CHỈ `Admin`.** Vai
`User` **không** được cấp sẵn; ai cần thì SuperAdmin cấp tay ở màn Phân quyền.

> ### 🔴 Đây là ngoại lệ, KHÔNG phải hành vi mặc định. Đọc trước khi "dọn cho nhất quán".
>
> Luật seed **mặc định** vẫn làm ngược lại, và điều đó **cố ý**: key nào không khai gì thì
> được cấp cho `Admin` + `User`
> (`src/BE/Core/PlatformManager.Core.Application/Permissions/ICoreResourceKeySource.cs:34`).
> Mục tiêu ghi ngay tại chỗ là *"GIỮ NGUYÊN hành vi trước khi vá (mọi user thao tác được)"* —
> một quyết định di trú, để việc bật deny-by-default không khoá mất người đang dùng hệ thống.
>
> **Vì sao DTI được miễn:** lý lẽ "giữ nguyên hành vi cũ" không áp cho một key **mới toanh**.
> Không có ai đang ghi dữ liệu DTI để mà giữ nguyên hành vi cho họ — module đã gỡ 2026-08-29.
> Cấp sẵn cho `User` ở đây không phải "không làm hỏng cái đang chạy", nó là **mở quyền ghi
> cho toàn bộ người đăng nhập ngay ở lần seed đầu**, trên một tập dữ liệu mà một lần nạp đè
> file có thể ghi lại 62 dòng của một kỳ đã báo cáo.
>
> ✅ **Hệ quả kỹ thuật: Q36 NAY CÀI ĐƯỢC bằng dữ liệu ở host** (thi công 2026-09-09). Seam đã
> mở rộng đúng hình dạng mà chính khối này đề xuất: `ResourceKeyDefinition` có thêm
> `SeedRoles` (`ICoreResourceKeySource.cs:51`), mặc định `[Admin, User]` (`:34`) nên mọi key
> hiện có **không đổi hành vi**, còn host thu hẹp bằng cú pháp khởi tạo đối tượng. Seeder lấy
> vai từ định nghĩa key thay vì lặp cứng
> (`src/BE/Core/PlatformManager.Core.Persistence/CoreSeeder.cs:93`).
>
> Hai lối còn lại vẫn là lối sai, ghi lại để lượt sau không quay về: hard-code danh sách trừ
> trong Core làm Core biết tên một nghiệp vụ (`CoreMustNotKnowBusinessNameTests` canh đúng
> điều đó), còn bỏ seed hẳn thì `Admin` cũng không có quyền và trái Q36.
>
> Ba ca khai sai bị chặn ngay lúc `Catalog()` chạy, **trước khi ghi dòng nào**
> (`ICoreResourceKeySource.cs:153`): `SeedRoles` rỗng, tên vai lạ, và khai `SuperAdmin`. Cả ba
> đều hỏng im lặng nếu để lọt — seed vẫn thoát 0, chỉ có ma trận quyền khác thứ người khai định
> làm.
>
> 🔴 **Việc CÒN LẠI của Q36 là ở host, không phải ở Core:** `dti.manage` chưa có trong
> `AppResourceKeySource.Definitions` (`src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs:60`
> — hôm nay chỉ có `import.manage`). Bước 3 thêm dòng
> `new(AppResourceKeys.DtiManage, "Nhập & sửa dữ liệu DTI") { SeedRoles = [Roles.Admin] }`.
> **Seam có sẵn không tự làm việc đó** — quên `SeedRoles` thì key rơi về mặc định `[Admin, User]`
> và Q36 bị vi phạm mà không có gì báo, vì mặc định là một giá trị hợp lệ.
>
> **Nghiệm thu bắt buộc, đừng bỏ:** sau khi seed, mở màn Phân quyền (hoặc
> `GET /api/admin/permissions/resources`) và xác nhận dòng `dti.manage` có `Admin` **và
> KHÔNG có `User`**. Đây là bước kiểm một **ngoại lệ** — nó là thứ dễ bị một lần refactor
> "cho nhất quán" âm thầm xoá, và triệu chứng lúc hỏng là không có triệu chứng nào.
>
> Tin tốt đi kèm: `IPermissionChecker` **không cache**
> (`src/BE/Core/PlatformManager.Core.Application/Permissions/IPermissionChecker.cs:15`), nên
> cấp tay hay thu hồi đều có hiệu lực ngay ở request kế tiếp.

---

## 7. Số liệu tham chiếu — đo từ **file BA gửi tháng 8/2026**

> **Xuất xứ nêu bằng TÊN, không bằng đường dẫn — có chủ đích (2026-09-10).** File đó cố ý
> không có trong repo (dữ liệu điểm thật của một địa phương), nên viết nó thành một đường dẫn
> là tạo một neo mà người thứ hai không mở được. Bộ mẫu ẩn danh
> `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` **KHÔNG** thay thế được ở đây: nó giữ hình
> dạng (62 bản ghi · 11 cột · 6 nhóm) nhưng là **dataset khác**, và cột `Chênh lệch` của nó
> tính theo chiều **ngược lại** — trỏ sang nó là biến đoạn này thành câu tự bác bỏ.

Đây là **dữ liệu nghiệp vụ**, không phải số đếm cấu trúc repo — chép ra đây là hợp lệ, và
dùng làm mốc kiểm khi import file gốc lần đầu.

> ⚠️ **File gốc KHÔNG có trong repo, có chủ đích** (`.gitignore:15`): nó mang điểm tự đánh giá
> và điểm thẩm định thật của một địa phương, đưa vào git là đưa dữ liệu tổ chức thật vào lịch
> sử repo — xoá sau không gỡ được. Các con số dưới đây đo trên máy có file đó; người không có
> file **không kiểm lại được**, và đó là giới hạn đã biết của mục này.
>
> **Test thì KHÔNG dùng file gốc** (sửa 2026-09-09, finding F7). Trước đó
> `CsvImportFileReaderTests` neo thẳng vào nó, nên trên một clone sạch hai test đỏ vì
> `DirectoryNotFoundException` — cổng BE hỏng vì một lý do không liên quan tới code. Nay chúng
> đọc `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv`: **ẩn danh, có trong repo**, giữ nguyên
> mọi tính chất cấu trúc mà hai test đo (62 bản ghi · 11 cột · BOM UTF-8 · ô xuống dòng trong
> nháy kép · hai cột `Phụ trách`/`Hạn xử lý` rỗng toàn bộ). Điểm số và câu chữ minh chứng thì
> là số liệu dựng — chúng không tham gia phép đo nào ở đó.

- **62 chỉ tiêu · 6 nhóm · tổng `Điểm tối đa` 960**
- Tổng `Tự đánh giá` **787,84** (= **82,1%**) · tổng `Thẩm định` **606,27** (= **63,2%**)
- 27 chỉ tiêu có `Chênh lệch ≠ 0` · 31 chỉ tiêu có `Thẩm định = Điểm tối đa`
- `Phụ trách` và `Hạn xử lý`: **rỗng toàn bộ 62 dòng**. `Minh chứng/Ghi chú`: 29/62 dòng có nội dung
- `Điểm tối đa` chỉ nhận 3 giá trị: 10 · 20 · 30
- Không dòng nào có `Tự đánh giá > Điểm tối đa` — luật §6.3 chưa bị kích hoạt bởi file này

| # | Nhóm | Chỉ tiêu | Điểm tối đa | Tự đánh giá | Thẩm định | Tiến độ |
| ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | Hạ tầng và Nền tảng số | 7 | 80 | 59,47 | 50,00 | 74,3% |
| 2 | Nhân lực số | 5 | 100 | 51,85 | 35,38 | 51,8% |
| 3 | An toàn thông tin, an ninh mạng | 6 | 100 | 100,00 | 94,52 | 100,0% |
| 4 | Hoạt động chính quyền số | 32 | 520 | 442,43 | 320,94 | 85,1% |
| 5 | Hoạt động Kinh tế số | 4 | 80 | 80,00 | 80,00 | 100,0% |
| 6 | Hoạt động Xã hội số | 8 | 80 | 54,09 | 25,43 | 67,6% |

Phân bố `Trạng thái`: `Hoàn thành` 26 · `Cần bổ sung minh chứng` 22 · `Đang thực hiện` 13 ·
`Chưa thực hiện` 1.

---

## 8. Cần chốt — danh sách CÒN MỞ của cả cụm DTI + sổ quyết định

> 🔄 **LẬT 2026-09-10.** Mục này từng mang tiêu đề *"Cần chốt — HẾT MỤC (đóng nốt
> 2026-09-06)"* và mở bằng *"Không còn câu hỏi nghiệp vụ nào để ngỏ cho cụm DTI"*. Sai: bảng
> ngay dưới là những mục **đang** mở. Đây là **chỗ duy nhất** giữ danh sách đó cho cả hai màn
> DTI — `doc/contracts/danh-muc-dti.md` §4 và phía Dashboard trỏ về đây, không chép.

### 8.1 Còn mở

| Mục | Trạng thái | Đang ghi ở |
| --- | --- | --- |
| ~~**Tên năm mã lỗi dòng mới của đường import**~~ | **ĐÓNG 2026-09-11 bằng Q73** — người dùng duyệt nguyên văn cả năm tên | §6.3 · `doc/contracts/danh-muc-dti.md` §`errors[].code` |
| Khe băng V3 — hai file phân loại theo **hai trục khác nhau** (phần còn lại sau Q51) | chờ người duyệt hợp nhất trục; **đừng tự gộp** | `spec/danh-muc-dti/ui-spec.md` §9 mục 4 · `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` § Layout Blueprint |
| ~~**Q56** — copy cho các mã lỗi người dùng thấy (`IMPORT.ROW_*`, `IMPORT.JOB_NOT_FOUND`, `CRITERIA.*`, `DASHBOARD.*`)~~ | **ĐÓNG 2026-09-10** — người dùng duyệt **nguyên văn** toàn bộ bảng copy | khu `doc/Design/` (§ Copy của màn tương ứng) |
| ~~**Q54** — cách cho các nhãn khoảng ngày vừa trục biểu đồ Dashboard~~ | **ĐÓNG 2026-09-10** — duyệt phương án: nhãn ngang, tự lược bớt đều, nhãn tuần đang xem luôn hiện | khu `doc/Design/`; luật nhãn kỳ: `spec/dashboard-dti/business-rules.md` §Nhãn kỳ |
| ~~Cơ chế seed nhóm (Q42): `HasData` hay seeder riêng ở `Business.*`~~ | **ĐÓNG 2026-09-10 bằng Q67** — `BusinessSeeder` riêng ở `Business.Persistence`, `Id` qua `EntityId.New()` | §1.6 |
| ~~Dashboard — điều kiện nhận biết ca Q32 ở ui-spec lệch proxy của contract DB-1~~ | **ĐÓNG 2026-09-10 bằng Q69** — proxy đọc từ `kpi`, không từ `table` | `doc/contracts/dashboard.md` §0 · `spec/dashboard-dti/ui-spec.md` §5.3.1 |
| ~~Dashboard — projection của export cần `OwnerName`/`Deadline` mà `table[]` của DB-1 không mang~~ | **ĐÓNG 2026-09-10 bằng Q68** — export có projection riêng; DB-1 giữ nguyên 9 cột | `doc/contracts/dashboard.md` DB-4 |
| ~~Dashboard — rate limit / cache của các endpoint~~ | **ĐÓNG 2026-09-10 bằng Q70** — không policy riêng, không cache | `doc/contracts/dashboard.md` § Rate limit và cache |
| ~~Lối thoát của `PERIOD_NOT_WEEKLY` khi đang xem năm cũ~~ | **ĐÓNG 2026-09-10 bằng Q60** — gợi ý thoát tuỳ năm đang xem | §8.3 · `spec/danh-muc-dti/ui-spec.md` §5.5.1 |
| ~~Tìm kiếm khớp tiền tố hay chuỗi con~~ | **ĐÓNG 2026-09-10 bằng Q59** — tên chứa chuỗi, mã khớp theo đoạn | §8.3 · §1.2 |
| ~~Mã chỉ tiêu **sai định dạng** (có chữ, đoạn rỗng như `4..2`) chưa có mã lỗi nào ở DM-3/DM-4; import cũng chưa có mã lỗi dòng cho mã sai định dạng hay quá 20 ký tự~~ | **ĐÓNG 2026-09-10 bằng Q65** — thêm `CRITERIA.CODE_FORMAT_INVALID`, `IMPORT.ROW_CODE_FORMAT_INVALID`, `IMPORT.ROW_CODE_TOO_LONG` | §2 · §6.3 · `doc/contracts/danh-muc-dti.md` DM-3 · DM-7 |

### 8.2 Đã đóng

Bảng dưới giữ lại các mục đã đóng, kèm đáp án, để người từng đọc bản trước không đi tìm ở chỗ
khác. Sổ các chốt ngày 2026-09-10 ở §8.3.

| Mục cũ | Đóng bằng | Đáp án | Nay ghi ở |
| --- | --- | --- | --- |
| Ghi vào một kỳ **THÁNG** thì dòng báo kỳ nào (lệch đơn vị: neo `Tháng 8` vào 31/08 ⇒ tuần ISO **36**) | **Q37** (2026-09-06) | **Chỉ nhập theo TUẦN.** Tháng/năm là tổng hợp tự tính, chọn chúng thì chỉ đọc. Không dựng cột `PeriodKey`, không có hai nguồn sự thật | §5.1 · §5.3 · §5.4 |
| Import ghi `UpdatedBy = "system"` | **Q35** (2026-09-06) | Phải ghi **đúng người đăng nhập**. Hạng mục **Core**, **điều kiện tiên quyết** của DM-7, ba bước | §5.6 |
| Seed cấp key DTI cho vai nào | **Q36** (2026-09-06) | **Chỉ `Admin`** — ngoại lệ so với luật seed hiện tại | §6.5 |
| `Tiến độ %` khi import | Q24 | để trống | §6.4 |
| Quyền ghi của màn | Q27 | một key `dti.manage`, mọi kỳ | §6.5 |
| Người không có quyền thấy gì | **Q39** (2026-09-06) | vào được màn, **chỉ đọc**; không chặn route, không ẩn menu | §5.4 |
| `Tất cả` + năm cũ thì lời ghi đi đâu | **T15** (2026-09-06) | **chỉ đọc** — `"all"` chỉ hợp lệ khi đang xem năm hiện tại; đường ghi nhận thêm `year` để chặn được ở server | §5.3 · §5.4 |
| Lưới rỗng thì FE biết ẩn nút bằng gì | lỗ hổng Q39 × T9, vá 2026-09-06 | cả khối quyền (`canWrite` · `isEditable` · `editBlockedBy`) ở **cấp màn**, không phải cấp dòng | §5.4 · `doc/contracts/danh-muc-dti.md` DM-2 mục 3 |
| `isEditable = false` thì màn hình nhắc gì | vá 2026-09-06 | `editBlockedBy` mang **mảng mã lý do** — đủ 3 ca, diễn đạt được hai điều kiện cùng trượt | §5.4 |
| Tên thư mục `spec/` | — | `spec/danh-muc-dti/` + `spec/dashboard-dti/`, xong 2026-09-05 | `doc/contracts/danh-muc-dti.md` §4 |

### 8.3 Sổ quyết định chốt 2026-09-10

| Câu hỏi | Chốt | Đáp án | Ghi ở |
| --- | --- | --- | --- |
| Ai quy `"all"` về tuần khi ghi — FE hay server | **Q40** | **server**; FE gửi nguyên giá trị ô `Kỳ trong năm`, kể cả `"all"`, kèm `year` — hợp đồng thắng bản cũ của `ui-spec.md` §7.4b | §5.3 · `spec/danh-muc-dti/ui-spec.md` §7.4b · `doc/contracts/danh-muc-dti.md` DM-4 khối Q26 |
| Năm cũ + một tuần cụ thể có ghi được không | **Q41** | **được** — khẳng định Q27 và Q20; T15 (năm cũ + `Tất cả`) và Q37 (tháng) giữ nguyên | §5.3 khối T15 |
| Nhóm chỉ tiêu seed thế nào | **Q42** | `Code` `"1"`…`"6"`, `DisplayOrder` theo thứ tự xuất hiện trong file mẫu, `Name` đúng chuỗi cột `Nhóm`; giao diện hiện `Code. Name`; cơ chế ở `Business.*` | §1.6 |
| `ImportJobs` là gì, lưu kỳ đích thế nào | **Q45** | entity **thứ tư** của `Business.Domain`; thêm cột `TargetWeekEnd` (`date`, Chủ nhật của tuần đích đã quy đổi); Q20 giữ nguyên | §1 · §1.5 · §1.4 |
| Hai bản ghi cùng chỉ tiêu trong cùng một tuần | **Q46** | lấy bản `AssessmentDate` lớn nhất — áp **cả** Dashboard | §5.2 |
| Tìm kiếm có phân biệt dấu không | **Q47** | **không**, ở cả hai màn — cột chuẩn hoá `NameNormalized` (index btree ban đầu đã gỡ theo Q59) | §1.2 · §1.4 · `doc/contracts/danh-muc-dti.md` DM-2 |
| `editBlockedBy` khi `Năm = 2025` + `Tháng 8` | **Q48** | **chỉ** `["PERIOD_NOT_WEEKLY"]` — hai mã lọc loại trừ nhau | §5.4 · `doc/contracts/danh-muc-dti.md` DM-2 mục 3 và §5 mục 15 |
| Tạo chỉ tiêu xong FE cập nhật lưới thế nào | **Q49** | `POST` giữ trả `CriteriaDto`; FE **tải lại lưới**. Đường sửa (DM-4 trả dòng lưới, thay tại chỗ) không đổi | `spec/danh-muc-dti/ui-spec.md` §3.3 · §7.3 |
| Câu băng năm cũ + `Tất cả` | **Q50** | duyệt **nguyên văn** — spec chỉ trỏ, không chép | `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` § Copy |
| Người thiếu quyền ghi có dải băng giải thích không | **Q51** | **không** — khẳng định Q39; vai đó gỡ khỏi khe V3 | §5.4 · `spec/danh-muc-dti/ui-spec.md` §3 hàng V3 · §5.5.1 · §5.6.1 · §9 mục 4 |
| Sắp mã tự nhiên cài thế nào | **Q53** | cột khoá sắp xếp tính sẵn `CodeSortKey`, có index; sau Q58 mỗi đoạn đệm **4** chữ số, cột `string(49)` (bản đầu cùng ngày: đệm 20, `string(209)`) | §1.2 · §1.4 · §2 · `doc/contracts/danh-muc-dti.md` DM-2 |
| Mỗi đoạn của mã tối đa mấy chữ số | **Q58** | **4** — kiểm ở tạo, sửa, import; `CRITERIA.CODE_SEGMENT_TOO_LONG` (DM-3/DM-4) · `IMPORT.ROW_CODE_SEGMENT_TOO_LONG` (lỗi dòng DM-7) | §2 · §6.3 · §1.2 · `doc/contracts/danh-muc-dti.md` DM-3 · DM-4 · DM-7 · §2 |
| Tìm kiếm khớp kiểu gì | **Q59** | tên **chứa chuỗi** trên `NameNormalized`; mã **khớp theo đoạn** (`Code = q` hoặc bắt đầu bằng `q + "."`); không index riêng, có chủ đích | §1.2 · §1.4 · `doc/contracts/danh-muc-dti.md` DM-2 |
| Băng `PERIOD_NOT_WEEKLY` gợi ý thoát thế nào | **Q60** | năm hiện tại: *chọn một tuần hoặc `Tất cả`*; năm cũ: **chỉ** *chọn một tuần cụ thể* (T15) | §5.4 · `spec/danh-muc-dti/ui-spec.md` §5.5.1 · copy: `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` |
| Tham số lọc lạ trên URL (link cũ) | **Q62** | FE đưa về mặc định, sửa URL, không báo, không gọi API với giá trị sai; BE vẫn giữ `CRITERIA.STATUS_INVALID` / `CRITERIA.ASSESSMENT_PERIOD_INVALID` | `spec/danh-muc-dti/ui-spec.md` §2 |
| Import hỏng giữa chừng thì dòng đã ghi có bị hoàn tác không | **Q64** | **có** — cả lượt nạp chạy trong MỘT giao dịch; job `Failed` ⇒ không dòng nào được ghi | §6.3 |
| Mã sai định dạng / quá 20 ký tự khi import chưa có mã lỗi nào | **Q65** | thêm ba mã: `CRITERIA.CODE_FORMAT_INVALID`, `IMPORT.ROW_CODE_FORMAT_INVALID`, `IMPORT.ROW_CODE_TOO_LONG` | §2 · `doc/contracts/danh-muc-dti.md` §2 |
| FE biết "năm đang xem là năm hiện tại" từ đâu (cần cho Q60) | **Q66** | BE trả `isCurrentYear` trong DM-2 — **không** suy từ đồng hồ máy khách, cùng lý do Q40 | `doc/contracts/danh-muc-dti.md` DM-2 · §5.4 |
| Cơ chế seed 6 nhóm chỉ tiêu (mục để ngỏ của Q42) | **Q67** | `BusinessSeeder` riêng ở `Business.Persistence`, `Id` qua `EntityId.New()`, idempotent theo `Code`; **không** `HasData` | §1.6 |
| Export lấy `OwnerName`/`Deadline`/`ProgressPercent` ở đâu khi `table[]` của DB-1 không mang | **Q68** | projection **riêng** cho export; DB-1 giữ nguyên 9 cột; hai đường dùng chung object bộ lọc và luật §5.2 | `doc/contracts/dashboard.md` DB-4 |
| FE nhận ra ca Q32 bằng trường nào trên dây | **Q69** | `kpi.totalCriteria > 0` **và** `kpi.overallProgress` vắng mặt — **không** dùng `table.length`, vì `table` bị bộ lọc còn `kpi` thì không | `doc/contracts/dashboard.md` §0 |
| Rate limit / cache cho các endpoint Dashboard | **Q70** | không policy riêng (dựa `GlobalLimiter` sẵn có), không cache — dữ liệu đổi ngay sau mỗi lần sửa inline và mỗi lần import | `doc/contracts/dashboard.md` § Rate limit và cache |
| DB-1 `mode=week` gửi `year` mà không gửi `date` | **Q71** | **tuần ISO CUỐI của năm đó**; `400 PERIOD_YEAR_MISMATCH` chỉ sinh khi gửi **cả** `date` lẫn `year` mà lệch | `doc/contracts/dashboard.md` DB-1 § Mã lỗi |
| Năm tình huống lỗi dòng §6.3 bỏ sót cần mã gì | **Q73** (2026-09-11) | duyệt **nguyên văn** năm tên: `IMPORT.ROW_NAME_MISSING` · `ROW_MAX_SCORE_INVALID` · `ROW_SELF_SCORE_INVALID` · `ROW_VERIFIED_SCORE_INVALID` · `ROW_DEADLINE_INVALID`. Không có chúng thì theo Q64 một ô trống cuộn ngược cả file | §6.3 · `doc/contracts/danh-muc-dti.md` §`errors[].code` |
| Cột vắng khỏi file nạp có khác ô trống không | **Q74** (2026-09-11) | **có** — cột vắng header ⇒ copy-forward; cột có header mà ô trống/ghi `—` ⇒ xoá trắng. Áp cho **cả ba** đường ghi; với DM-3/DM-4 là khoá vắng mặt khỏi JSON so với khoá mang `null` | §6.2 · §5.3 |
| `progressPercent` ngoài miền: kẹp hay báo lỗi | **làm rõ 2026-09-11** | **KẸP** — §3.2 thắng; `CRITERIA.PROGRESS_PERCENT_INVALID` KHÔNG khai, vì không đường nào ném được nó | §3.2 · `doc/contracts/danh-muc-dti.md` DM-6 |
| Trần SỐ DÒNG của file nạp | **Q75** (2026-09-11) | thêm trần, **cấu hình ở Core** cạnh trần dung lượng; mã lỗi `IMPORT.FILE_TOO_MANY_ROWS` (tiền tố `FILE_`, **không** `ROW_` — `ROW_` dành riêng cho lỗi một dòng) | `doc/huong_dan/wiki-core/be/15-import-export.md` §2 · `doc/contracts/danh-muc-dti.md` DM-7 |
| FE biết kỳ nào là KỲ HIỆN TẠI từ đâu (cần cho băng V3 §5.5) | **Q72** | DM-2 trả `currentPeriod` + `currentPeriodLabel` ở khối quyền cấp màn — cùng lần đánh giá với `editBlockedBy`, không lấy từ DB-3, không từ đồng hồ máy khách | `doc/contracts/danh-muc-dti.md` DM-2 mục 3 |

**Hai hạng mục CORE là điều kiện tiên quyết, không phải việc để sau** — cả hai đều phải xong
**trước** khi bật đường ghi lên môi trường thật, và cả hai đi qua `core-reviewer`:

| Hạng mục | Chặn cái gì | Ghi ở |
| --- | --- | --- |
| Danh tính trong job nền (Q35) | **DM-7 — import** | §5.6 |
| Seed theo vai cho từng key (Q36) | quyền ghi đúng như đã chốt | §6.5 |
