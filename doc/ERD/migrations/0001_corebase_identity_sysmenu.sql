-- =============================================================================
-- ĐÃ THAY THẾ (2026-08-16) — xem doc/ERD/migrations/0003_corebase_v2.sql
-- =============================================================================
-- File này KHÔNG còn dùng để chạy lên DB thật — model đã đổi (bỏ cột
-- SysMenu.RequiredRole, thêm bảng SysMenuRole nhiều-nhiều, thêm cột
-- AppUser.MustChangePassword, gộp chung với migration nghiệp vụ DTI Weekly).
-- Giữ lại CHỈ để tham khảo lịch sử thiết kế — xem doc/ke-hoach-xay-lai-corebase.md.
-- =============================================================================
--
-- Migration 0001 — CoreBase: ASP.NET Core Identity (7 bảng chuẩn) + SysMenu
-- =============================================================================
-- Nguồn thiết kế: doc/ERD/ERD-corebase.md, doc/ERD/PlatformManager-corebase.dbml
-- DB: PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL khi lên EF Core thật)
--
-- QUAN TRỌNG — không set DEFAULT sinh uuid cho cột "Id" ở BẤT KỲ bảng nào:
-- theo quyết định đã CHỐT ở doc/huong_dan/wiki-core/be/trien-khai/04-p3-platform-persistence.md
-- §4.1 (ApplyKeyGenerationConvention/ValueGeneratedNever) — Id LUÔN do ỨNG DỤNG
-- sinh (EntityId.New() phía Domain, hoặc Guid.NewGuid() mặc định của
-- IdentityUser<Guid>/IdentityRole<Guid> phía Identity), KHÔNG BAO GIỜ để DB tự
-- sinh. Nếu thêm DEFAULT gen_random_uuid() vào đây, khi lên EF Core thật sẽ vỡ
-- đúng bug đã ghi trong file trên: EF coi key-đã-set = "đã tồn tại", sinh UPDATE
-- thay vì INSERT.
--
-- Migration này chạy TRƯỚC schema nghiệp vụ DTI (PlatformManager.dbml) — bảng
-- nghiệp vụ có FK trỏ vào "AspNetUsers".
-- =============================================================================

BEGIN;

-- =============================================================================
-- 1. ASP.NET Core Identity — 7 bảng chuẩn (IdentityDbContext<AppUser, AppRole, Guid>)
-- =============================================================================

CREATE TABLE "AspNetRoles" (
    "Id" uuid NOT NULL,
    "Name" varchar(256) NULL,
    "NormalizedName" varchar(256) NULL,
    "ConcurrencyStamp" text NULL,
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName")
    WHERE "NormalizedName" IS NOT NULL;

CREATE TABLE "AspNetUsers" (
    "Id" uuid NOT NULL,
    -- ===== [MỞ RỘNG] 3 cột ngoài chuẩn Identity — xem ERD-corebase.md §1.1 =====
    "FullName" varchar(255) NOT NULL,
    "DateCreate" timestamptz NULL,
    "DateUpdate" timestamptz NULL,
    -- ===== Cột chuẩn IdentityUser<Guid> =====
    "UserName" varchar(256) NULL,
    "NormalizedUserName" varchar(256) NULL,
    "Email" varchar(256) NULL,
    "NormalizedEmail" varchar(256) NULL,
    "EmailConfirmed" boolean NOT NULL DEFAULT false,
    "PasswordHash" text NULL,
    "SecurityStamp" text NULL,
    "ConcurrencyStamp" text NULL,
    "PhoneNumber" text NULL,
    "PhoneNumberConfirmed" boolean NOT NULL DEFAULT false,
    "TwoFactorEnabled" boolean NOT NULL DEFAULT false,
    "LockoutEnd" timestamptz NULL,
    "LockoutEnabled" boolean NOT NULL DEFAULT true,
    "AccessFailedCount" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName")
    WHERE "NormalizedUserName" IS NOT NULL;
CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE TABLE "AspNetUserRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId")
        REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE TABLE "AspNetUserClaims" (
    "Id" serial NOT NULL,
    "UserId" uuid NOT NULL,
    "ClaimType" text NULL,
    "ClaimValue" text NULL,
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" varchar(128) NOT NULL,
    "ProviderKey" varchar(128) NOT NULL,
    "ProviderDisplayName" text NULL,
    "UserId" uuid NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE TABLE "AspNetUserTokens" (
    "UserId" uuid NOT NULL,
    "LoginProvider" varchar(128) NOT NULL,
    "Name" varchar(128) NOT NULL,
    "Value" text NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId")
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" serial NOT NULL,
    "RoleId" uuid NOT NULL,
    "ClaimType" text NULL,
    "ClaimValue" text NULL,
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId")
        REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

-- =============================================================================
-- 2. SysMenu — metadata menu điều hướng (Loại C, xem ERD-corebase.md §2)
-- =============================================================================

CREATE TABLE "SysMenu" (
    "Id" uuid NOT NULL,
    "ParentId" uuid NULL,
    "Code" varchar(100) NOT NULL,
    "Label" varchar(255) NOT NULL,
    "Icon" varchar(100) NULL,
    "Route" varchar(255) NULL,
    "RequiredRole" varchar(100) NULL,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    -- ===== BaseEntity (tên field ĐÃ CHỐT LẠI 2026-08-15 — xem entity-domain.md) =====
    "UserCreate" varchar(50) NULL,
    "UserUpdate" varchar(50) NULL,
    "DateCreate" timestamptz NULL,
    "DateUpdate" timestamptz NULL,
    "IsDelete" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_SysMenu" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SysMenu_SysMenu_ParentId" FOREIGN KEY ("ParentId")
        REFERENCES "SysMenu" ("Id") ON DELETE RESTRICT
);

-- Unique Code CHỈ tính trên dòng còn sống — đúng pattern "2 lớp soft-delete"
-- đã áp dụng cho nghiệp vụ DTI (be/trien-khai/04-p3 §5): xoá mềm 1 Code rồi
-- tạo lại đúng Code đó phải thành công, không bị chặn bởi dòng đã xoá mềm.
CREATE UNIQUE INDEX "ux_sysmenu_code" ON "SysMenu" ("Code") WHERE "IsDelete" = false;
CREATE INDEX "ix_sysmenu_parentid" ON "SysMenu" ("ParentId");
CREATE INDEX "ix_sysmenu_displayorder" ON "SysMenu" ("DisplayOrder");

COMMIT;
