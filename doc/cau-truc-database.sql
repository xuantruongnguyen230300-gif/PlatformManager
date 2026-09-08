-- =============================================================================
-- 🚧 TÀI LIỆU LỊCH SỬ — KHÔNG CHẠY FILE NÀY TRÊN DB HIỆN NAY (2026-08-31)
-- =============================================================================
-- Toàn bộ nội dung dưới đây thuộc về module DtiWeekly (schema `business`) — hàm
-- `business.criteria_assessment_date_utc` và unique index trên `CriteriaAssessments`.
-- Module đó ĐÃ BỊ GỠ khỏi source, và đợt baseline lại migration ngày 2026-08-31 đã
-- bỏ 5 bảng `business.*` khỏi schema. Chạy file này trên DB hiện nay sẽ lỗi vì
-- không có bảng để tạo index.
--
--   Trong file này            → Thực tế hiện nay
--   ------------------------ → -------------------------------------------------
--   Chạy sau `database update` → Dựng DB bằng `doc/db-khoi-tao.sql`, MỘT file
--   DDL viết tay bắt buộc      → Không còn phần nào áp cho schema `core`
--   Ràng buộc 1 đánh giá/ngày  → Thuộc module DtiWeekly, quay lại cùng module
--
-- GIỮ LẠI vì khi module DtiWeekly được dựng lại, hai đoạn DDL này là thứ EF Core
-- không sinh được và sẽ cần lại nguyên văn.
--
-- Nguồn sống: `doc/cau-truc-database.md` §5 và §5.2.
-- =============================================================================

-- =============================================================================
-- cau-truc-database.sql — DDL VIẾT TAY, nguồn duy nhất
-- =============================================================================
-- Cặp đôi với doc/cau-truc-database.md:
--     .md  = mô tả schema để ĐỌC HIỂU (nguồn tham chiếu duy nhất)
--     .sql = DDL để CHẠY, phần EF Core KHÔNG tự sinh được
--
-- File này KHÔNG phải bản sao của migration. EF Core sinh và quản lý toàn bộ
-- bảng/cột/khoá/index thông thường. Ở đây chỉ chứa những thứ EF **không biết
-- cách sinh**, nên nếu ai đó dựng lại DB từ đầu bằng `dotnet ef` mà quên chạy
-- file này thì DB sẽ THIẾU ràng buộc — im lặng, không lỗi, không test nào báo.
--
-- CHẠY KHI NÀO:
--   1. Sau khi `dotnet ef database update` lần đầu trên một DB mới.
--   2. Sau mỗi lần buộc phải sinh full script thay vì delta.
--   Toàn bộ lệnh dưới đây idempotent — chạy lại nhiều lần vô hại.
--
-- KIỂM SAU KHI CHẠY:
--   \di business.*
--   → phải thấy UX_CriteriaAssessments_CriteriaId_CreatedAt_Day
--   \di core.*
--   → IX_SysMenus_Code phải có mệnh đề WHERE ("IsDeleted" = false); index này do
--     EF sinh qua migration 0008, KHÔNG do file này (xem mục 3)
--
-- Lịch sử: gộp từ doc/ERD/migrations/0001–0005 (xoá 2026-08-23 khi hợp nhất
-- nguồn schema về một tài liệu). Lý do đầy đủ của từng đoạn nằm ở
-- doc/cau-truc-database.md §4.
--
-- Cập nhật 2026-08-28 — đổi tên 6 cột audit (migration
-- src/BE/.../Migrations/sql/0007_rename_audit_columns.sql): mọi tên cột ở đây đã
-- là bộ tên mới (CreatedBy/UpdatedBy/CreatedAt/UpdatedAt/IsDeleted), và index ở
-- mục 2 đổi tên theo — `UX_CriteriaAssessments_CriteriaId_DateCreate_Day` →
-- `UX_CriteriaAssessments_CriteriaId_CreatedAt_Day`.
--
-- ⚠️ DB ĐÃ CHẠY BẢN CŨ: phải chạy 0007 TRƯỚC file này. 0007 có lệnh
-- `ALTER INDEX ... RENAME TO` cho index ở mục 2; bỏ qua bước đó rồi chạy thẳng
-- file này thì `CREATE UNIQUE INDEX IF NOT EXISTS` (kiểm theo TÊN) sẽ tạo thêm
-- một index TRÙNG NỘI DUNG dưới tên mới, tốn ghi và gây nhầm khi đọc `\di`.
-- (Đã tái hiện thật trên container Postgres 16 khi kiểm chứng 0007.)
-- =============================================================================


-- -----------------------------------------------------------------------------
-- 1. Hàm hỗ trợ index — BẮT BUỘC có trước khi tạo index ở mục 2
-- -----------------------------------------------------------------------------
-- Vì sao cần hàm riêng thay vì CAST thẳng:
--   Postgres từ chối  CAST("CreatedAt" AS date)  trong biểu thức index với lỗi
--   42P17 "functions in index expression must be marked IMMUTABLE" — phép đổi
--   timestamptz → date phụ thuộc TimeZone của session nên không deterministic.
--   ĐÃ XẢY RA LỖI THẬT, không phải phòng xa.

CREATE OR REPLACE FUNCTION business.criteria_assessment_date_utc(ts timestamptz)
RETURNS date
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT (ts AT TIME ZONE 'UTC')::date;
$$;


-- -----------------------------------------------------------------------------
-- 2. Ràng buộc "1 đánh giá / 1 chỉ tiêu / 1 ngày"
-- -----------------------------------------------------------------------------
-- Nền móng của toàn bộ mô hình "kỳ suy từ ngày tạo" (xem spec/danh-muc-dti/
-- business-rules.md). Mất index này = dữ liệu trùng lọt vào IM LẶNG.
--
-- EF Core KHÔNG sinh được: index theo BIỂU THỨC HÀM + partial filter.
-- ModelSnapshot chỉ có IX_CriteriaAssessments_CriteriaId_CreatedAt (non-unique,
-- không filter, không hàm) — đó là index khác, không thay thế được cái này.

CREATE UNIQUE INDEX IF NOT EXISTS "UX_CriteriaAssessments_CriteriaId_CreatedAt_Day"
    ON business."CriteriaAssessments" ("CriteriaId", business.criteria_assessment_date_utc("CreatedAt"))
    WHERE "IsDeleted" = false;


-- -----------------------------------------------------------------------------
-- 3. Soft-delete 2 lớp cho SysMenus — ĐÃ VÁ 2026-08-28, KHÔNG CÒN VIỆC Ở ĐÂY
-- -----------------------------------------------------------------------------
-- KHÔNG có lệnh nào trong mục này nữa — cố ý. Mục này giữ lại để người từng đọc
-- bản cũ (có sẵn 3 dòng DDL chép tay, đang comment) biết vì sao chúng biến mất,
-- thay vì tưởng bị xoá nhầm rồi chép lại.
--
-- Lịch sử: IX_SysMenus_Code từng là unique KHÔNG filter, nên xoá mềm một menu
-- rồi tạo lại cùng Code sẽ THẤT BẠI vì trùng khoá — trong khi Criteria và
-- CriteriaGroups thì được. Mệnh đề partial từng tồn tại ở migration 0001, mất
-- trong lần dựng lại 0003, không ai ghi nhận. Phát hiện 2026-08-23.
--
-- Nay EF Core TỰ SINH được ràng buộc này, nên nó không còn thuộc phạm vi file
-- "DDL viết tay" nữa:
--   Cấu hình: src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/
--             Configurations/SysMenuConfiguration.cs  (.HasFilter)
--   Migration: src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/
--             Migrations/sql/0008_sysmenu_code_partial_unique_index.sql
--
-- ⚠️ Vì vậy 0008 phải được chạy như MỘT MIGRATION BÌNH THƯỜNG (cùng dãy 0003 →
-- 0008), KHÔNG phải qua file này. Dựng DB mới bằng `dotnet ef database update`
-- thì EF đã bao gồm sẵn — chỉ mục 1 và 2 bên trên mới cần chạy tay.
