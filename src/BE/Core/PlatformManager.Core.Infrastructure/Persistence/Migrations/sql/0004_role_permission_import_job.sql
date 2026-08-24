-- Bản sao đặt cạnh code migration C# (bản gốc: doc/ERD/migrations/0004_role_permission_import_job.sql).
-- Model đổi → sinh lại rồi cập nhật CẢ 2 nơi, KỂ CẢ phần header cảnh báo dưới đây.
-- =============================================================================
-- Migration 0004 — RolePermission (phân quyền theo hành động) + ImportJob
-- =============================================================================
-- Sinh từ EF Core migration `20260818090451_AddRolePermissionAndImportJob` bằng
-- `dotnet ef migrations script 20260816150234_InitialCreate
--  20260818090451_AddRolePermissionAndImportJob --idempotent`.
--
-- LƯU Ý — đây là script DELTA (chỉ phần mới), KHÔNG phải full script từ đầu:
-- 0003_corebase_v2.sql có 2 đoạn VÁ TAY mà EF Core không biết (hàm IMMUTABLE
-- `business.criteria_assessment_date_utc` + unique filtered index
-- `UX_CriteriaAssessments_CriteriaId_DateCreate_Day`). Sinh lại full script sẽ
-- làm MẤT 2 đoạn đó — nên từ 0004 trở đi luôn sinh delta từ migration liền
-- trước. Chạy 0004 SAU khi 0003 đã áp dụng.
--
-- Bản gốc (phải giữ khớp khi migration đổi):
-- doc/ERD/migrations/0004_role_permission_import_job.sql
--
-- Nội dung:
-- 1. core."RolePermissions" — bảng gán quyền theo tài nguyên cho role, vá gap
--    OWASP #1 Broken Access Control (xem doc/contracts/permissions.md CONTRACT
--    PERM-2, src/BE/.claude/rules/api-controller.md §"Phân quyền theo hành động").
--    PK ghép (RoleId, ResourceKey); FK → AspNetRoles ON DELETE CASCADE, cùng quy
--    ước với SysMenuRoles đã có ở 0003 (xoá role thì gỡ luôn quyền của role đó).
-- 2. business."ImportJobs" — trạng thái job import CSV/Excel chạy nền qua
--    Hangfire (xem doc/contracts/danh-muc-dti.md CONTRACT DM-7). Index trên
--    "Status" phục vụ endpoint poll `GET /api/import/{jobId}`.
--
-- ⚠️ SEED BẮT BUỘC sau khi chạy migration này — KHÔNG bỏ qua:
-- (Ghi chú 2026-08-19: câu "trừ SuperAdmin bypass" dưới đây MÔ TẢ ĐÚNG kể từ
--  2026-08-19 — trước đó code KHÔNG hề có bypass nào, tài khoản chỉ mang role
--  SuperAdmin vẫn bị 403. Nay RequirePermissionFilter đã bypass tường minh cho
--  Roles.SuperAdmin, xem src/BE/.claude/rules/api-controller.md.)
-- `RequirePermissionFilter` deny-by-default: bảng "RolePermissions" rỗng nghĩa
-- là MỌI role (trừ SuperAdmin bypass) đều bị 403 ở các endpoint đã gắn
-- [RequirePermission] (Criteria/CriteriaGroups/Import) — kể cả thao tác họ vẫn
-- làm được trước đây. CoreSeeder.SeedRolePermissionsAsync() đã cấp đủ 3 key cho
-- Admin + User (giữ nguyên hành vi cũ), nhưng seeder chỉ chạy ở môi trường
-- Development (gate IsDevelopment() trong Program.cs). Với DB thật/production
-- phải seed tay tương đương — chạy scripts/seed-role-permissions.sql (thêm
-- 2026-08-24), xem mục "Rủi ro rollout" ở doc/contracts/permissions.md.
--
-- Bảng Hangfire (schema `hangfire`) KHÔNG nằm trong file này — Hangfire tự tạo
-- schema/bảng của nó lúc app khởi động lần đầu (UsePostgreSqlStorage), không đi
-- qua EF Core migration.
-- =============================================================================

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260818090451_AddRolePermissionAndImportJob') THEN
    CREATE TABLE business."ImportJobs" (
        "Id" uuid NOT NULL,
        "FileName" character varying(260) NOT NULL,
        "Format" character varying(20) NOT NULL,
        "StoragePath" character varying(1000) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ResultJson" text,
        "ErrorMessage" text,
        "UserCreate" character varying(50),
        "UserUpdate" character varying(50),
        "DateCreate" timestamp with time zone,
        "DateUpdate" timestamp with time zone,
        "IsDelete" boolean NOT NULL,
        CONSTRAINT "PK_ImportJobs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260818090451_AddRolePermissionAndImportJob') THEN
    CREATE TABLE core."RolePermissions" (
        "RoleId" uuid NOT NULL,
        "ResourceKey" character varying(100) NOT NULL,
        CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "ResourceKey"),
        CONSTRAINT "FK_RolePermissions_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260818090451_AddRolePermissionAndImportJob') THEN
    CREATE INDEX "IX_ImportJobs_Status" ON business."ImportJobs" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260818090451_AddRolePermissionAndImportJob') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818090451_AddRolePermissionAndImportJob', '10.0.11');
    END IF;
END $EF$;
COMMIT;
