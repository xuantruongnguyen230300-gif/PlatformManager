-- =============================================================================
-- seed-role-permissions.sql — cấp RolePermission cho môi trường PRODUCTION
-- =============================================================================
-- Vì sao file này tồn tại: `CoreSeeder.SeedRolePermissionsAsync` chỉ chạy ở
-- Development (gate `IsDevelopment()` trong `Program.cs`). Không có file này,
-- production khởi động với bảng `core."RolePermissions"` RỖNG — và vì
-- `RequirePermissionFilter` chặn theo permission-key, **mọi user không phải
-- SuperAdmin sẽ bị 403 hàng loạt** ngay khi chạm bất kỳ endpoint nghiệp vụ nào.
--
-- Quy tắc và bối cảnh đầy đủ:
--   doc/huong_dan/wiki-core/be/13-core-data-migration.md §Seed
--   doc/contracts/permissions.md §"Rủi ro rollout"
--
-- CHẠY KHI NÀO: sau khi đã áp dụng migration (bảng `RolePermissions` và
-- `AspNetRoles` đã tồn tại, các role đã được tạo), trước khi mở cho người dùng.
--
-- CHẠY THẾ NÀO:
--   psql "<connection-string>" -f scripts/seed-role-permissions.sql
--
-- AN TOÀN CHẠY LẠI: toàn bộ script idempotent (`ON CONFLICT DO NOTHING`).
-- Chạy 10 lần cho kết quả giống chạy 1 lần — không sinh dòng trùng, không lỗi.
-- =============================================================================

BEGIN;

-- Cấp đủ MỌI permission-key của dự án cho `Admin` và `User`.
--
-- Giữ NGUYÊN hành vi trước khi có phân quyền theo hành động: trước đó endpoint
-- nghiệp vụ chỉ `[Authorize]` trần nên mọi user đăng nhập đều thao tác được.
-- Cấp đủ ở bước seed rồi THU HẸP qua UI phân quyền là đường an toàn; làm ngược
-- lại (seed rỗng rồi cấp dần) nghĩa là mở cho người dùng một hệ thống mà không
-- ai làm được gì.
--
-- `SuperAdmin` KHÔNG cần dòng nào: `RequirePermissionFilter` cho nó đi qua theo
-- cơ chế break-glass. Thêm dòng cho SuperAdmin ở đây là thừa và gây hiểu nhầm
-- rằng quyền của nó đến từ bảng này.
--
-- Danh sách key dưới đây phải khớp danh mục key của dự án, nay là
-- `AppResourceKeySource.Definitions` ở
-- src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs.
-- Thêm key mới ở đó → thêm vào đây cùng lượt, nếu không production sẽ 403 ở
-- đúng tính năng vừa thêm trong khi Development vẫn chạy tốt.
--
-- SỬA 2026-09-04 — chỉ đổi ĐỊA CHỈ, không đổi luật: danh mục key chuyển khỏi Core sang host ngày
-- 2026-09-03 (lớp `ResourceKeys` trong `Core.Application` không còn tồn tại; cơ chế ở lại Core sau
-- seam `ICoreResourceKeySource`). Chỉ dẫn cũ trỏ vào một file đã xoá — chạy lệnh đối chiếu bên dưới
-- sẽ ra 0 dòng và người vận hành không có cách nào biết danh sách này còn đúng hay không.
--
-- Đối chiếu bằng lệnh, đừng tin danh sách chép sẵn ở đây:
--   grep -n 'public const string' src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs
--
-- SỬA 2026-08-29: script trước chèn thêm `criteria.manage` và `criteria-groups.manage`.
-- Hai key đó đã bị gỡ khỏi danh mục cùng module DtiWeekly. Chèn key không còn ai khai
-- `[RequirePermission]` không gây lỗi ngay —
-- `RolePermissions` không có FK sang danh sách key — nên nó nằm im trong DB production, làm
-- màn hình Phân quyền hiện 2 tài nguyên không tồn tại và người vận hành tưởng đang cấp quyền
-- thật. Đúng dạng "gate xanh vì không kiểm gì cả": không lệnh nào bắt được, phải kiểm bằng mắt.
-- ⚠️ BA CHI TIẾT BẮT BUỘC, mỗi cái đều làm script ABORT nếu thiếu — đã đo thật 2026-09-04
-- trên schema dựng từ 0001_initial_baseline.sql, không phải trên bảng tự tạo:
--
--   (1) "Id" và "IsDeleted" là NOT NULL và KHÔNG có DEFAULT (baseline dòng 107, 114). Ở đường
--       C# thì EF điền hộ; ở đường SQL chạy tay thì KHÔNG AI điền, nên phải cấp tường minh.
--       Thiếu ⇒ `null value in column "Id" violates not-null constraint`.
--
--   (2) Unique index là index MỘT PHẦN:
--         CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_ResourceKey_Active"
--           ON core."RolePermissions" ("RoleId","ResourceKey") WHERE "IsDeleted" = false;
--       Postgres CHỈ nhận index một phần làm đích ON CONFLICT khi mệnh đề lặp lại ĐÚNG vị từ.
--       Thiếu `WHERE "IsDeleted" = false` ⇒ `there is no unique or exclusion constraint
--       matching the ON CONFLICT specification`. Đây là lý do câu "idempotent" ở đầu file
--       từng SAI: script abort trước cả khi ghi được dòng nào.
--
--   (3) "CreatedBy" = 'system' và "CreatedAt" khớp đúng thứ CoreSeeder ghi (đối chiếu
--       platformmanager_dev 2026-09-04), để hai đường seed không tạo ra hai dạng dữ liệu.
INSERT INTO core."RolePermissions"
       ("Id", "RoleId", "ResourceKey", "IsDeleted", "CreatedBy", "CreatedAt")
SELECT gen_random_uuid(), r."Id", k."ResourceKey", false, 'system', now()
FROM   core."AspNetRoles" r
CROSS  JOIN (VALUES
    ('import.manage')
) AS k("ResourceKey")
WHERE  r."Name" IN ('Admin', 'User')
ON CONFLICT ("RoleId", "ResourceKey") WHERE "IsDeleted" = false DO NOTHING;

COMMIT;

-- =============================================================================
-- KIỂM SAU KHI CHẠY — phải trả về đúng (số role) × (số key trong khối VALUES trên);
-- hôm nay là 2 role × 1 key = 2 dòng.
-- Trả về 0 dòng nghĩa là `AspNetRoles` chưa có 'Admin'/'User': chạy seed role
-- trước, rồi chạy lại file này.
--
-- Dòng có `ResourceKey` KHÔNG nằm trong danh mục key của dự án là rác từ lần seed cũ
-- (vd `criteria.manage`, `criteria-groups.manage` seed trước 2026-08-29) — dọn bằng
-- DELETE thủ công sau khi đã đối chiếu với AppResourceKeySource.cs.
-- =============================================================================
-- SELECT r."Name", rp."ResourceKey"
-- FROM   core."RolePermissions" rp
-- JOIN   core."AspNetRoles" r ON r."Id" = rp."RoleId"
-- ORDER  BY r."Name", rp."ResourceKey";
