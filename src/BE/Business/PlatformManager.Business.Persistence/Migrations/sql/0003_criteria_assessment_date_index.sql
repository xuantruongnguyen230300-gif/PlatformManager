-- ============================================================================================
-- 0003_criteria_assessment_date_index.sql  —  index cho truy vấn nóng của Dashboard (2026-09-10)
--
-- Chạy TAY, SAU 0002_business_dti_tables.sql.
--
-- ⚠️ VÌ SAO LÀ FILE RIÊNG chứ không gộp vào 0002:
-- 0002 có thể ĐÃ được chạy trước khi index này ra đời. Sinh lại 0002 cho to hơn sẽ hỏng với
-- người đã chạy nó — các khối `IF NOT EXISTS(... __EFMigrationsHistory ...)` KHÔNG bảo vệ được
-- ở repo này, vì không lệnh EF nào ghi vào bảng đó (schema áp bằng .sql chạy tay,
-- `dotnet ef database update` bị chặn — doc/cau-truc-database.md §5.3). Một file riêng thì đúng
-- cho CẢ HAI phía: ai đã chạy 0002 chỉ chạy thêm file này; ai chưa thì chạy 0002 rồi 0003.
--
-- VÌ SAO CẦN INDEX NÀY: DashboardRepository.GetAssessmentFactsAsync lọc CHỈ theo khoảng
-- AssessmentDate, không kèm CriteriaId. Index unique có sẵn là (CriteriaId, AssessmentDate) —
-- AssessmentDate ở vị trí THỨ HAI, nên nó không phục vụ truy vấn đó
-- (doc/huong_dan/quy-uoc/tieu-chi-review.md §6).
-- ============================================================================================

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910103037_ThemIndexAssessmentDate') THEN
    CREATE INDEX "IX_CriteriaAssessments_AssessmentDate" ON business."CriteriaAssessments" ("AssessmentDate") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910103037_ThemIndexAssessmentDate') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260910103037_ThemIndexAssessmentDate', '10.0.11');
    END IF;
END $EF$;
COMMIT;

