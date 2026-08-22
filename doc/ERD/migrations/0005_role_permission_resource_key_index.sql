-- =============================================================================
-- Migration 0005 — Index IX_RolePermissions_ResourceKey_RoleId (performance A2)
-- =============================================================================
-- Sinh từ EF Core migration `20260818101335_AddRolePermissionResourceKeyIndex`
-- bằng `dotnet ef migrations script 20260818090451_AddRolePermissionAndImportJob
--  20260818101335_AddRolePermissionResourceKeyIndex --idempotent`.
--
-- Đây là script DELTA (chỉ phần mới), theo đúng quy ước đã ghi ở 0004: từ 0004
-- trở đi luôn sinh delta từ migration liền trước, KHÔNG sinh lại full script
-- (0003 có 2 đoạn vá tay EF Core không biết). Chạy 0005 SAU khi 0004 đã áp dụng.
--
-- Bản sao thứ 2 (phải giữ khớp khi migration đổi):
-- src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0005_role_permission_resource_key_index.sql
--
-- Nội dung: 1 index duy nhất trên core."RolePermissions" ("ResourceKey", "RoleId").
-- KHÔNG có thay đổi bảng/cột/dữ liệu nào — an toàn chạy trên DB đã có dữ liệu.
--
-- Vì sao cần:
--   PK ghép hiện tại là ("RoleId", "ResourceKey") — dẫn đầu bằng "RoleId". Query
--   nóng nhất của hệ thống (RequirePermissionFilter, chạy trên MỌI request có
--   [RequirePermission]) lại lọc TRƯỚC HẾT theo "ResourceKey" rồi mới ghép
--   "RoleId", nên PK không seek được cho nó (quy tắc Q2, xem
--   src/BE/.claude/rules/performance.md và
--   doc/huong_dan/wiki-core/be/11-performance-caching.md §6.3 finding A2).
--   Index này dẫn đầu đúng cột được lọc và phủ luôn cột join → index-only scan.
--
-- Đọc EXPLAIN cho đúng: ở cỡ dữ liệu hiện tại (6 dòng RolePermissions) Postgres
-- vẫn chọn Seq Scan vì cả bảng nằm gọn trong 1 page — đó là lựa chọn ĐÚNG của
-- planner, không phải dấu hiệu index vô dụng. Index có giá trị khi số tổ hợp
-- (role × resource key) tăng lên; chi phí duy trì gần bằng 0 vì bảng ghi rất hiếm.
--
-- ⚠️ KHÔNG có bước seed nào đi kèm migration này (khác 0004).
-- =============================================================================

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260818101335_AddRolePermissionResourceKeyIndex') THEN
    CREATE INDEX "IX_RolePermissions_ResourceKey_RoleId" ON core."RolePermissions" ("ResourceKey", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260818101335_AddRolePermissionResourceKeyIndex') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818101335_AddRolePermissionResourceKeyIndex', '10.0.11');
    END IF;
END $EF$;
COMMIT;
