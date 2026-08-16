-- =============================================================================
-- ĐÃ THAY THẾ (2026-08-16) — seed nay do DbSeeder.cs (code, gate IsDevelopment())
-- đảm nhiệm, không còn là file SQL riêng — xem
-- PlatformManager.Infrastructure/Persistence/DbSeeder.cs và
-- doc/ke-hoach-xay-lai-corebase.md. Giữ lại CHỈ để tham khảo lịch sử.
-- =============================================================================
--
-- Migration 0002 — Seed CoreBase: AspNetRoles + SysMenu
-- =============================================================================
-- Chạy SAU 0001_corebase_identity_sysmenu.sql. Đây là DML (dữ liệu), không
-- phải DDL — tách riêng khỏi 0001 theo đúng quy ước "schema và seed là 2 mối
-- quan tâm khác nhau".
--
-- KHÔNG seed "AspNetUsers" ở đây — xem ERD-corebase.md §1.3: PasswordHash
-- phải qua PasswordHasher<TUser> của Identity (PBKDF2 + salt ngẫu nhiên), không
-- có cách tạo hash hợp lệ bằng SQL thuần. Tài khoản đầu tiên phải tạo qua code
-- thật (vd 1 lần chạy DbSeeder gọi UserManager.CreateAsync(user, password) lúc
-- khởi động app ở môi trường Development, hoặc 1 endpoint /api/auth/register
-- nếu được chốt) — KHÔNG viết migration SQL chèn user thật vào đây.
--
-- 2 role dưới đây là TẠM — xem "Câu hỏi còn mở" ở ERD-corebase.md §4, cần xác
-- nhận tên/số lượng role thật trước khi gán cho user thật (đổi tên role sau
-- khi đã gán sẽ cần thêm 1 migration UPDATE riêng, không sửa trực tiếp ở đây).
-- =============================================================================

BEGIN;

-- =============================================================================
-- 1. AspNetRoles — 2 role khởi tạo (TẠM, chờ xác nhận)
-- =============================================================================

-- ConcurrencyStamp: giá trị placeholder cố định là đủ cho seed (Identity chỉ
-- cần 1 chuỗi bất kỳ, tự đổi lại ở lần update đầu tiên qua code thật) — không
-- phụ thuộc extension pgcrypto/gen_random_uuid() để migration chạy được trên
-- mọi bản PostgreSQL không cần cài thêm gì.
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp") VALUES
    ('11111111-1111-1111-1111-111111111111', 'Admin', 'ADMIN', '00000000-0000-0000-0000-000000000001'),
    ('22222222-2222-2222-2222-222222222222', 'User',  'USER',  '00000000-0000-0000-0000-000000000002');

-- =============================================================================
-- 2. SysMenu — khớp đúng 3 mục đã có thật trong doc/Prototype/*.html
--    (KHÔNG bịa thêm mục nào chưa có trang UI thật — xem ERD-corebase.md §2.4)
-- =============================================================================

-- Item gốc #1 — "Dashboard" (phẳng, không có con)
INSERT INTO "SysMenu"
    ("Id", "ParentId", "Code", "Label", "Icon", "Route", "RequiredRole", "DisplayOrder", "IsActive", "DateCreate", "IsDelete")
VALUES
    ('a1111111-0000-0000-0000-000000000001', NULL, 'dti-weekly', 'Dashboard', 'pi-th-large', '/dashboard', NULL, 1, true, now(), false);

-- Item gốc #2 — "Danh mục" (cha, không có Route) + con "DTI"
INSERT INTO "SysMenu"
    ("Id", "ParentId", "Code", "Label", "Icon", "Route", "RequiredRole", "DisplayOrder", "IsActive", "DateCreate", "IsDelete")
VALUES
    ('a2222222-0000-0000-0000-000000000001', NULL, 'danh-muc', 'Danh mục', 'pi-folder', NULL, NULL, 2, true, now(), false);
INSERT INTO "SysMenu"
    ("Id", "ParentId", "Code", "Label", "Icon", "Route", "RequiredRole", "DisplayOrder", "IsActive", "DateCreate", "IsDelete")
VALUES
    ('a2222222-0000-0000-0000-000000000002', 'a2222222-0000-0000-0000-000000000001', 'danh-muc-dti', 'DTI', 'pi-list', '/danh-muc/dti', NULL, 1, true, now(), false);

-- Item gốc #3 — "Quản trị hệ thống" (cha, không có Route) + con "Người dùng"
-- RequiredRole = 'Admin' — chỉ role Admin mới thấy nhóm quản trị, khớp đúng
-- tinh thần màn hình quan-tri-nguoi-dung.html (quản lý tài khoản hệ thống).
INSERT INTO "SysMenu"
    ("Id", "ParentId", "Code", "Label", "Icon", "Route", "RequiredRole", "DisplayOrder", "IsActive", "DateCreate", "IsDelete")
VALUES
    ('a3333333-0000-0000-0000-000000000001', NULL, 'quan-tri', 'Quản trị hệ thống', 'pi-cog', NULL, 'Admin', 3, true, now(), false);
INSERT INTO "SysMenu"
    ("Id", "ParentId", "Code", "Label", "Icon", "Route", "RequiredRole", "DisplayOrder", "IsActive", "DateCreate", "IsDelete")
VALUES
    ('a3333333-0000-0000-0000-000000000002', 'a3333333-0000-0000-0000-000000000001', 'sys-user', 'Người dùng', 'pi-user', '/quan-tri/nguoi-dung', 'Admin', 1, true, now(), false);

COMMIT;
