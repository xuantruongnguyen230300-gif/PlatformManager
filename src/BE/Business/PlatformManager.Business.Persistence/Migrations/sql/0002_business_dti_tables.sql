-- ============================================================================================
-- 0002_business_dti_tables.sql  —  4 bảng schema `business` của cụm DTI (sinh 2026-09-10)
--
-- Sinh từ: dotnet ef migrations script 20260831165117_InitialCreate 20260910100110_ThemBangNghiepVuDti
--          --idempotent   (delta, KHÔNG phải full — full sẽ dựng lại cả 11 bảng `core` đã có)
--
-- Chạy TAY lên Postgres, sau khi đã áp 0001_initial_baseline.sql. `dotnet ef database update`
-- bị chặn bằng máy ở repo này (.claude/settings.json) — migration ở đây chỉ là cỗ máy TÍNH DELTA,
-- không phải đường áp schema. Xem doc/cau-truc-database.md §5.3.
--
-- ⚠️ VÌ SAO FILE NÀY KHÔNG NẰM Ở Core/PlatformManager.Core.Persistence/Migrations/sql/
-- Thư mục đó là ARTIFACT SCHEMA mà CoreBase ship cho dự án thứ hai (chốt 2026-09-04). Bốn bảng
-- dưới đây là NGHIỆP VỤ của riêng sản phẩm này — để chúng ở đó là ship bảng DTI sang một dự án
-- không có nghiệp vụ DTI. File .sql của tầng nghiệp vụ vì vậy ở lại cùng tầng nghiệp vụ.
--
-- Sau khi chạy, PHẢI seed 6 nhóm chỉ tiêu (danh mục ĐÓNG — import không tự tạo nhóm):
--   dotnet run --project src/BE/PlatformManager.Api -- --seed
-- ============================================================================================

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'business') THEN
            CREATE SCHEMA business;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE TABLE business."CriteriaGroups" (
        "Id" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_CriteriaGroups" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE TABLE business."ImportJobs" (
        "Id" uuid NOT NULL,
        "FileName" character varying(260) NOT NULL,
        "Format" character varying(20) NOT NULL,
        "StoragePath" character varying(1000) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ResultJson" text,
        "ErrorMessage" text,
        "TargetWeekEnd" date NOT NULL,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_ImportJobs" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_ImportJobs_TargetWeekEnd_Sunday" CHECK (EXTRACT(ISODOW FROM "TargetWeekEnd") = 7)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE TABLE business."Criteria" (
        "Id" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" text NOT NULL,
        "NameNormalized" text NOT NULL,
        "CodeSortKey" character varying(49) COLLATE "C" NOT NULL,
        "GroupId" uuid NOT NULL,
        "MaxScore" numeric(10,2) NOT NULL,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Criteria" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Criteria_CriteriaGroups_GroupId" FOREIGN KEY ("GroupId") REFERENCES business."CriteriaGroups" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE TABLE business."CriteriaAssessments" (
        "Id" uuid NOT NULL,
        "CriteriaId" uuid NOT NULL,
        "AssessmentDate" date NOT NULL,
        "SelfScore" numeric(10,2),
        "VerifiedScore" numeric(10,2),
        "ProgressPercent" integer,
        "Status" character varying(40),
        "OwnerId" uuid,
        "Deadline" date,
        "Note" text,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_CriteriaAssessments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CriteriaAssessments_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES core."AspNetUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_CriteriaAssessments_Criteria_CriteriaId" FOREIGN KEY ("CriteriaId") REFERENCES business."Criteria" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE UNIQUE INDEX "IX_Criteria_Code_Active" ON business."Criteria" ("Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE INDEX "IX_Criteria_CodeSortKey" ON business."Criteria" ("CodeSortKey", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE INDEX "IX_Criteria_GroupId" ON business."Criteria" ("GroupId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE INDEX "IX_CriteriaAssessments_OwnerId" ON business."CriteriaAssessments" ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE UNIQUE INDEX "UX_CriteriaAssessments_CriteriaId_AssessmentDate" ON business."CriteriaAssessments" ("CriteriaId", "AssessmentDate") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE UNIQUE INDEX "IX_CriteriaGroups_Code_Active" ON business."CriteriaGroups" ("Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    CREATE INDEX "IX_ImportJobs_Status" ON business."ImportJobs" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260910100110_ThemBangNghiepVuDti') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260910100110_ThemBangNghiepVuDti', '10.0.11');
    END IF;
END $EF$;
COMMIT;

