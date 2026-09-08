-- =============================================================================
-- db-khoi-tao.sql — dựng DB PlatformManager từ TRỐNG. Chạy MỘT file này là đủ.
-- =============================================================================
-- CHẠY TRONG DBEAVER
--   1. Mở kết nối tới database TRỐNG.
--   2. Mở file này bằng SQL Editor.
--   3. Bấm **Execute script (Alt+X)** — KHÔNG phải Ctrl+Enter.
--      Ctrl+Enter chỉ chạy 1 câu lệnh; Alt+X mới chạy cả file.
--
-- Chạy lại bao nhiêu lần cũng được — script sinh ở chế độ idempotent.
--
-- SAU ĐÓ: chạy LỆNH SEED để tạo role, 2 tài khoản quản trị và cây menu
-- (mật khẩu phải do ASP.NET Identity băm, SQL không làm được):
--
--   dotnet run --project src/BE/PlatformManager.Api -- --seed
--
-- Cần đặt trước 2 secret, nếu không lệnh seed cố ý DỪNG và không ghi dòng nào:
--   dotnet user-secrets set "Bootstrap:SuperAdminPassword" "..." --project src/BE/PlatformManager.Api
--   dotnet user-secrets set "Bootstrap:AdminPassword"      "..." --project src/BE/PlatformManager.Api
--   (Production: biến môi trường Bootstrap__SuperAdminPassword / Bootstrap__AdminPassword)
--
-- Tiến trình API phục vụ thật KHÔNG seed gì cả — đó là chủ đích, xem
-- doc/huong_dan/wiki-core/be/13-core-data-migration.md §"Quyết định người dùng 2026-08-30".
--
-- ⚠️ Sinh tự động từ Migrations/sql/0001_initial_baseline.sql.
--    ĐỪNG sửa tay file này — sửa model rồi sinh lại, nếu không nó lệch repo.
--
-- 🚧 SINH LẠI 2026-08-31 sau khi baseline lịch sử migration. Bản trước dựng 16 bảng,
--    trong đó 5 bảng business.* thuộc module DtiWeekly đã bị gỡ khỏi source. Khi module
--    đó dựng lại, nó mang migration RIÊNG của nó — không thêm tay vào đây.
-- =============================================================================


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'core') THEN
        CREATE SCHEMA core;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS core."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'core') THEN
            CREATE SCHEMA core;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetRoles" (
        "Id" uuid NOT NULL,
        "Name" character varying(256),
        "NormalizedName" character varying(256),
        "ConcurrencyStamp" text,
        CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetUsers" (
        "Id" uuid NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "DateCreate" timestamp with time zone,
        "DateUpdate" timestamp with time zone,
        "CreatedBy" character varying(256),
        "UpdatedBy" character varying(256),
        "MustChangePassword" boolean NOT NULL DEFAULT TRUE,
        "UserName" character varying(256),
        "NormalizedUserName" character varying(256),
        "Email" character varying(256),
        "NormalizedEmail" character varying(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" text,
        "SecurityStamp" text,
        "ConcurrencyStamp" text,
        "PhoneNumber" text,
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" integer NOT NULL,
        CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."SysMenus" (
        "Id" uuid NOT NULL,
        "Code" character varying(100) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Route" character varying(200),
        "Icon" character varying(50),
        "ParentId" uuid,
        "DisplayOrder" integer NOT NULL,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_SysMenus" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SysMenus_SysMenus_ParentId" FOREIGN KEY ("ParentId") REFERENCES core."SysMenus" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetRoleClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "RoleId" uuid NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."RolePermissions" (
        "Id" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "ResourceKey" character varying(100) NOT NULL,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RolePermissions_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetUserClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" uuid NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES core."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetUserLogins" (
        "LoginProvider" text NOT NULL,
        "ProviderKey" text NOT NULL,
        "ProviderDisplayName" text,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
        CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES core."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetUserRoles" (
        "UserId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES core."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."AspNetUserTokens" (
        "UserId" uuid NOT NULL,
        "LoginProvider" text NOT NULL,
        "Name" text NOT NULL,
        "Value" text,
        CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
        CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES core."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE TABLE core."SysMenuRoles" (
        "Id" uuid NOT NULL,
        "SysMenuId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "CreatedBy" character varying(50),
        "UpdatedBy" character varying(50),
        "CreatedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_SysMenuRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SysMenuRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_SysMenuRoles_SysMenus_SysMenuId" FOREIGN KEY ("SysMenuId") REFERENCES core."SysMenus" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON core."AspNetRoleClaims" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON core."AspNetRoles" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_AspNetUserClaims_UserId" ON core."AspNetUserClaims" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_AspNetUserLogins_UserId" ON core."AspNetUserLogins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_AspNetUserRoles_RoleId" ON core."AspNetUserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "EmailIndex" ON core."AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON core."AspNetUsers" ("NormalizedUserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_RolePermissions_ResourceKey_RoleId" ON core."RolePermissions" ("ResourceKey", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_ResourceKey_Active" ON core."RolePermissions" ("RoleId", "ResourceKey") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_SysMenuRoles_RoleId" ON core."SysMenuRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SysMenuRoles_SysMenuId_RoleId_Active" ON core."SysMenuRoles" ("SysMenuId", "RoleId") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SysMenus_Code" ON core."SysMenus" ("Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    CREATE INDEX "IX_SysMenus_ParentId" ON core."SysMenus" ("ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260831165117_InitialCreate') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260831165117_InitialCreate', '10.0.11');
    END IF;
END $EF$;
COMMIT;



-- =============================================================================
-- KIỂM SAU KHI CHẠY — chạy 4 truy vấn dưới, đối chiếu với phần "mong đợi"
-- =============================================================================

-- (1) Migration đã ghi nhận. Mong đợi: ĐÚNG 1 dòng, kết thúc bằng "_InitialCreate".
SELECT "MigrationId" FROM core."__EFMigrationsHistory" ORDER BY "MigrationId";

-- (2) 3 bảng kế thừa BaseEntity phải đủ 5 cột audit.
--     Mong đợi: 3 dòng, cột "Thiếu" đều là {} (mảng rỗng).
SELECT t.tbl AS "Bảng",
       ARRAY(SELECT c FROM unnest(ARRAY['CreatedAt','UpdatedAt','CreatedBy','UpdatedBy','IsDeleted']) c
             WHERE NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_schema = t.sch AND table_name = t.tbl AND column_name = c)) AS "Thiếu"
FROM (VALUES ('core','SysMenus'), ('core','SysMenuRoles'), ('core','RolePermissions'))
     AS t(sch, tbl)
ORDER BY 1;

-- (3) AspNetUsers KHÔNG kế thừa BaseEntity (Identity tự quản vòng đời) nhưng vẫn phải
--     truy được AI đã tạo/sửa. Mong đợi: đúng 4 dòng.
SELECT column_name FROM information_schema.columns
WHERE table_schema = 'core' AND table_name = 'AspNetUsers'
  AND column_name IN ('DateCreate','DateUpdate','CreatedBy','UpdatedBy')
ORDER BY 1;

-- (4) Index unique phải LỌC theo IsDeleted. Đây là kiểm quan trọng nhất của bản này:
--     thiếu filter thì dòng đã xoá mềm vẫn chiếm khoá, và lần lưu THỨ HAI của cùng một ô
--     trong ma trận phân quyền sẽ vỡ với 23505.
--     Mong đợi: 3 dòng, cột "Có lọc IsDeleted" đều là t.
SELECT indexname AS "Index", (indexdef LIKE '%IsDeleted%') AS "Có lọc IsDeleted"
FROM pg_indexes
WHERE schemaname = 'core'
  AND indexdef LIKE '%UNIQUE%'
  AND (indexname LIKE 'IX_SysMenus_Code%'
    OR indexname LIKE 'IX_SysMenuRoles_%'
    OR indexname LIKE 'IX_RolePermissions_%')
ORDER BY 1;
