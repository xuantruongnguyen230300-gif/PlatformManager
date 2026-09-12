-- ============================================================================================
-- 0004_import_jobs_error_code.sql  —  business."ImportJobs" thêm cột "ErrorCode" (2026-09-11)
--
-- Chạy TAY, SAU 0003_criteria_assessment_date_index.sql.
--
-- ⚠️ VÌ SAO LÀ FILE RIÊNG chứ không sửa 0002: 0002 ĐÃ được chạy trên database dev (bảng
-- ImportJobs đang có thật, và đường import đã ghi vào nó). Sinh lại 0002 cho to hơn sẽ hỏng với
-- người đã chạy nó — cùng lý do đã ghi ở đầu 0003. Một file riêng thì đúng cho CẢ HAI phía.
--
-- VÌ SAO CẦN CỘT NÀY (Q75, 2026-09-11):
-- Nhánh `Failed` của DM-7 bước 2 trước đây chỉ có "ErrorMessage", thứ hợp đồng khai thẳng là
-- DEV-FACING, KHÔNG ĐỂ HIỂN THỊ. Nghĩa là mọi lỗi CẢ FILE — thiếu cột bắt buộc, và nay cả vượt
-- trần số dòng — đều không có gì để FE dịch thành câu cho người dùng đọc. Một trần mà người dùng
-- chạm phải nhưng không đọc được lý do thì chẳng khác gì không có trần.
--
-- Cột giữ `businessCode` (khuôn "MIEN.MA_LOI"), ra dây qua trường `errorCode` của
-- GET /api/import/{jobId}. NULL cho lỗi hạ tầng thuần (job crash, file hỏng ở mức byte) — chúng
-- cố ý không có mã, vì không mã nào dịch được chúng thành câu hữu ích hơn câu chung.
--
-- Đặc tả: doc/contracts/danh-muc-dti.md DM-7 § errorCode
-- Bảng:   spec/danh-muc-dti/business-rules.md §1.5
-- ============================================================================================

START TRANSACTION;

-- IF NOT EXISTS: file này phải chạy lại được nhiều lần mà không ném — schema ở repo áp bằng .sql
-- chạy tay, không có __EFMigrationsHistory nào canh hộ (doc/cau-truc-database.md §5.3).
ALTER TABLE business."ImportJobs"
    ADD COLUMN IF NOT EXISTS "ErrorCode" character varying(100) NULL;

COMMIT;

-- Nghiệm thu sau khi chạy:
--   \d business."ImportJobs"        -- phải thấy cột ErrorCode, character varying(100), nullable
