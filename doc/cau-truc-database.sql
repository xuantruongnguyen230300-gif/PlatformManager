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
--   → phải thấy UX_CriteriaAssessments_CriteriaId_DateCreate_Day
--
-- Lịch sử: gộp từ doc/ERD/migrations/0001–0005 (xoá 2026-08-23 khi hợp nhất
-- nguồn schema về một tài liệu). Lý do đầy đủ của từng đoạn nằm ở
-- doc/cau-truc-database.md §4.
-- =============================================================================


-- -----------------------------------------------------------------------------
-- 1. Hàm hỗ trợ index — BẮT BUỘC có trước khi tạo index ở mục 2
-- -----------------------------------------------------------------------------
-- Vì sao cần hàm riêng thay vì CAST thẳng:
--   Postgres từ chối  CAST("DateCreate" AS date)  trong biểu thức index với lỗi
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
-- ModelSnapshot chỉ có IX_CriteriaAssessments_CriteriaId_DateCreate (non-unique,
-- không filter, không hàm) — đó là index khác, không thay thế được cái này.

CREATE UNIQUE INDEX IF NOT EXISTS "UX_CriteriaAssessments_CriteriaId_DateCreate_Day"
    ON business."CriteriaAssessments" ("CriteriaId", business.criteria_assessment_date_utc("DateCreate"))
    WHERE "IsDelete" = false;


-- -----------------------------------------------------------------------------
-- 3. Soft-delete 2 lớp cho SysMenus — SỬA LỖI ĐÃ BIẾT
-- -----------------------------------------------------------------------------
-- Trạng thái: IX_SysMenus_Code hiện là unique KHÔNG filter, nên xoá mềm một
-- menu rồi tạo lại cùng Code sẽ THẤT BẠI vì trùng khoá — trong khi Criteria và
-- CriteriaGroups thì được. Mệnh đề partial từng tồn tại ở migration 0001, mất
-- trong lần dựng lại 0003, không ai ghi nhận. Phát hiện 2026-08-23.
--
-- ƯU TIÊN: khi viết lại src, khai .HasFilter("\"IsDelete\" = false") trong
-- cấu hình SysMenu để EF tự sinh — CriteriaAssessmentConfiguration đã có mẫu.
-- Đoạn dưới chỉ dùng khi vá một DB đã chạy mà chưa kịp sửa cấu hình.

-- DROP INDEX IF EXISTS core."IX_SysMenus_Code";
-- CREATE UNIQUE INDEX IF NOT EXISTS "IX_SysMenus_Code"
--     ON core."SysMenus" ("Code")
--     WHERE "IsDelete" = false;
