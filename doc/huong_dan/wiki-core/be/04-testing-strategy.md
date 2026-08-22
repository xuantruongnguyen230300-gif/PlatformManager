# 4. Kiểm thử là 1 phần của kiến trúc, không phải việc làm sau cùng

> Cùng nguyên tắc Nhóm A/B như [01-core-components.md](01-core-components.md):
> **không phải checklist bắt buộc**, chỉ đầu tư khi hệ thống thật sự chạm
> đúng nỗi đau tương ứng.

**Insight giá trị nhất từ `testing.md` của VNR: biến quy tắc kiến trúc thành TEST CHẠY ĐƯỢC (ArchTest), không chỉ ghi trong tài liệu.** Toàn bộ những nguyên tắc ở [01-core-components.md](01-core-components.md) (không public setter, controller phải có permission, không inject EF Core ở Application...) — VNR không chỉ *viết ra*, mà **viết thành 1 bộ test tự động chặn PR nếu vi phạm** (`dotnet test VNR.ArchTests` chạy **trước** unit test trong CI — kiến trúc sai thì unit test đúng cũng vô nghĩa). Đây là cách duy nhất đảm bảo quy ước không bị quên dần theo thời gian khi có người mới tham gia hoặc code base lớn lên — tài liệu (như wiki này) chỉ có tác dụng lúc đọc, ArchTest có tác dụng **mãi mãi, tự động**.

Với hệ thống mới bắt đầu nhỏ, không cần cả bộ ArchTest phức tạp như VNR ngay — nhưng nên có tối thiểu 2-3 test kiến trúc cốt lõi từ ngày đầu (ví dụ: "không entity nghiệp vụ nào có public setter", "mọi controller có `[Authorize]`/permission attribute"), rồi thêm dần khi phát hiện vi phạm thật.

## 3 tầng kiểm thử (test pyramid) — áp dụng đúng loại test cho đúng mục đích, đừng lẫn lộn

| Tầng | Kiểm tra gì | Công cụ .NET |
|---|---|---|
| ArchTest | Quy tắc kiến trúc (layer, naming, permission) | `NetArchTest`/Roslyn phân tích assembly |
| Unit test | Logic handler/domain — mock repository | xUnit + NSubstitute (không Moq) |
| Integration test | EF migration, FK constraint, unique index thật | **Testcontainers.PostgreSql** — container Postgres thật, không phải InMemory |

## Gotcha đáng nhớ nhất

❌ **Không bao giờ dùng `UseInMemoryDatabase` để test** — InMemory provider của EF Core **bỏ qua hoàn toàn** FK constraint, unique index, transaction thật. Test pass trên InMemory không chứng minh được gì về hành vi thật trên Postgres — chỉ dùng mock (unit test) hoặc Testcontainers (integration test).

## Áp dụng vào PlatformManager (2026-08-19)

### 3 test project, mỗi cái một mục đích

| Project | Tầng | Cần Docker? |
| --- | --- | --- |
| `Tests/PlatformManager.ArchTests` | ArchTest — quy tắc kiến trúc, IL-scan thuần | Không |
| `Tests/PlatformManager.Core.UnitTests` | Unit — luồng quyết định, phụ thuộc thay bằng NSubstitute | Không |
| `Tests/PlatformManager.Core.IntegrationTests` | Integration — Postgres THẬT qua Testcontainers | **Có** |

### Yêu cầu môi trường

`PlatformManager.Core.IntegrationTests` **cần Docker đang chạy** (Docker Desktop
trên Windows/macOS, `docker daemon` trên Linux). Không có Docker thì bộ test này
**fail rõ ràng kèm hướng dẫn**, **cố ý KHÔNG `Skip`** — skip im lặng tạo "xanh
giả", tức là `dotnet test` báo xanh trong khi phần kiểm tra bảo mật quan trọng
nhất chưa hề chạy. Thà đỏ và biết vì sao.

Chỉ muốn chạy phần không cần Docker:

```bash
dotnet test Tests/PlatformManager.ArchTests
dotnet test Tests/PlatformManager.Core.UnitTests
```

**Repo KHÔNG có CI** (`.github/` đã xoá 2026-08-21, có chủ đích) ⇒ integration
test chạy **trên máy dev**, và **Docker Desktop phải đang bật** — Testcontainers
cần nó để dựng Postgres. Không bật thì 18+ test đỏ hàng loạt với cùng một
exception từ `PostgresFixture`; đó là lỗi **hạ tầng**, không phải lỗi code.

`PostgresFixture` cố ý **fail chứ không skip** khi thiếu Docker — skip im lặng
là xanh giả, và bộ test này tồn tại chính để bắt những thứ chỉ lộ ra khi chạy
thật.

### Schema cho integration test lấy từ file `.sql` của repo, KHÔNG từ `EnsureCreated()`

`PostgresFixture` chạy tuần tự `doc/ERD/migrations/0003_* → 0004_* → 0005_*` vào
container. Như vậy test kiểm luôn **tính đúng của chính file `.sql` mà người dùng
chạy tay lên DB thật**. Dựng schema từ model EF (`EnsureCreated()`) sẽ test trên
một schema **khác** schema production — mà đợt tối ưu 2026-08-18 (thêm index
`IX_RolePermissions_ResourceKey_RoleId`) cho thấy khác biệt schema đúng là thứ
đáng quan tâm.

⚠️ Thêm file migration mới (`0006_*.sql`...) → **phải** thêm tên vào
`PostgresFixture.MigrationScripts`, nếu không schema test sẽ lệch schema thật —
đúng cái điều thiết kế này muốn tránh.

### Kiểm thử phân quyền — chia đôi có chủ đích

Code phân quyền (`RequirePermissionFilter`, `PermissionChecker`,
`SysMenuRoleRepository`) tách làm 2 nhóm test **không thay thế được cho nhau**:

- **Nhóm A (unit, không DB)** — luồng quyết định: claim rỗng → `Forbid` *và không
  chạm DB*; `SuperAdmin` → break-glass; không có quyền → `Forbid`. Chỉ test được
  sạch nhờ seam `IPermissionChecker` (khẳng định "chưa hề gọi" bằng
  `DidNotReceive`).
- **Nhóm B (integration, Postgres thật)** — ngữ nghĩa truy vấn: `INNER JOIN` loại
  dòng `RolePermission` mồ côi, `NULL IN (...)` khi `AspNetRoles.Name` NULL, thu
  hồi/cấp lại có hiệu lực **ngay** (chứng minh không có cache/TTL nào ở đường
  phân quyền — xem [11-performance-caching.md](11-performance-caching.md) §6.2
  quyết định #5).

**Vì sao không gộp nhóm B vào nhóm A bằng LINQ-to-Objects trên `List<T>`:** cách
đó chỉ kiểm được phép tập hợp trong C#, **không** kiểm được EF Core còn dịch đúng
sang SQL sau khi nâng version — cùng bản chất với lý do cấm InMemory ở trên.
