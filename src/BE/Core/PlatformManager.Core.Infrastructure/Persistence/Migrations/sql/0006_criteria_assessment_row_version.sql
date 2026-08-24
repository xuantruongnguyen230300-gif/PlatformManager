-- =============================================================================
-- Migration 0006 — RowVersion (xmin) cho CriteriaAssessment
-- =============================================================================
-- Sinh từ EF Core migration `20260824075803_AddCriteriaAssessmentRowVersion` bằng
-- `dotnet ef migrations script 20260818101335_AddRolePermissionResourceKeyIndex
--  20260824075803_AddCriteriaAssessmentRowVersion --idempotent`.
--
-- LƯU Ý — đây là script DELTA (chỉ phần mới kể từ 0005), KHÔNG phải full script.
-- Chạy 0006 SAU khi 0003 + 0004 + 0005 đã áp dụng.
--
-- Nội dung: KHÔNG có DDL nào thật sự chạy — property `CriteriaAssessment.Version`
-- (kiểu `uint`, `.IsRowVersion()`) map vào cột hệ thống `xmin` có sẵn của Postgres
-- (xem doc/huong_dan/quy-uoc/be-entity-domain.md §RowVersion). Migration C# CÓ chứa
-- `migrationBuilder.AddColumn<uint>(name: "xmin", ...)` nhưng Npgsql's migrations SQL
-- generator NO-OP hoá thao tác này khi sinh SQL — Postgres cấm tuyệt đối cột trùng
-- tên cột hệ thống (`ALTER TABLE ... ADD COLUMN xmin` sẽ lỗi nếu thật sự chạy). Xác
-- nhận trực tiếp từ tác giả Npgsql.EntityFrameworkCore.PostgreSQL (roji,
-- npgsql/efcore.pg#3270, 2024): "A migration indeed gets generated for xmin, but
-- it's ignored when generating the actual SQL... Things are done this way because
-- of how EF is designed." Script này chỉ ghi 1 dòng vào __EFMigrationsHistory để
-- `dotnet ef migrations list` không còn báo (Pending).
--
-- Bản gốc (phải giữ khớp khi migration đổi):
-- src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0006_criteria_assessment_row_version.sql
-- (không có bản song song ở doc/ERD/ — thư mục đó đã xoá 2026-08-23).
-- =============================================================================

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260824075803_AddCriteriaAssessmentRowVersion') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824075803_AddCriteriaAssessmentRowVersion', '10.0.11');
    END IF;
END $EF$;
COMMIT;
