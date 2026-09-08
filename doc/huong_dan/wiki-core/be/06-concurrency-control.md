---
kind: luat
scope: core
verified: 2026-09-06
---

# 6. Kiểm soát ghi đè khi nhiều người sửa cùng lúc (Concurrency)

Gotcha rất thực tế: 2 người cùng mở 1 bản ghi, cùng sửa, người lưu sau **ghi đè mất luôn** thay đổi của người lưu trước — họ không biết vì UI vẫn báo "lưu thành công".

## Cách chuẩn — Optimistic Concurrency bằng token phiên bản

Nguyên lý: mỗi bản ghi mang một **token phiên bản** đổi giá trị ở mọi lần UPDATE. Khi ghi, câu lệnh kèm thêm `WHERE <token> = @giá_trị_lúc_đọc`. Nếu người khác đã ghi trước → token không còn khớp → UPDATE ảnh hưởng **0 dòng** → EF Core ném `DbUpdateConcurrencyException` → BE trả **409** kèm thông điệp rõ ("dữ liệu đã bị người khác thay đổi, tải lại trang") thay vì âm thầm ghi đè.

**Token đó là gì thì tuỳ provider — đây là điểm dễ sai nhất:**

| Provider | Token | Khai thế nào |
| --- | --- | --- |
| **PostgreSQL (đang dùng)** | cột hệ thống **`xmin`**, DB tự tăng sẵn | property CLR `uint` + `.IsRowVersion()` — Npgsql tự bind vào `xmin` |
| SQL Server | kiểu `rowversion`, DB tự tăng | `byte[] RowVersion` + `.IsRowVersion()` |

> ⚠️ Dùng nhầm công thức SQL Server trên PostgreSQL thì Npgsql tạo một cột `bytea` **không ai cập nhật** — điều kiện `WHERE` luôn khớp và **check concurrency vô hiệu hoàn toàn, im lặng**. Không lỗi biên dịch, không lỗi lúc chạy.

> 📖 **Recipe thi hành cụ thể** (code mẫu, khi nào thêm, khi nào không): [`doc/huong_dan/quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §RowVersion. Bản đó là **file chủ** — mục này chỉ nói *vì sao cần*, không lặp lại *làm thế nào*.

**Chỉ cần cho**: entity có ≥2 luồng ghi độc lập chạm cùng bản ghi (danh mục dùng chung, cấu hình hệ thống, bản ghi trạng thái workflow). Không cần cho entity chỉ 1 người sở hữu/sửa (ví dụ hồ sơ cá nhân do đúng người đó tự sửa).


## Áp dụng vào PlatformManager

> ### ⚠️ Sửa 2026-08-30 — câu dưới đây từng nói sai, giữ lại để thấy sai ở đâu
>
> Bản trước khẳng định: *"Hiện KHÔNG có entity nào cần `RowVersion` (2026-08-29)
> — Core chỉ có `SysMenu`/`SysMenuRole`, đều một luồng ghi (seeder)."*
>
> Vế *"một luồng ghi (seeder)"* **sai**. `SysMenuRole` và `RolePermission` còn có
> luồng ghi thứ hai: màn Phân quyền, qua `PUT /api/admin/permissions`
> (`src/BE/PlatformManager.Api/Controllers/PermissionsController.cs:22`). Gate
> `SuperAdmin` thu hẹp *ai* ghi được, không thu hẹp *bao nhiêu người* ghi cùng lúc.
>
> Đây đúng khuôn lỗi mà `.claude/CLAUDE.md` §4 cảnh báo: một tuyên bố hiện trạng
> có ngày, đúng cú pháp, qua được mọi cổng máy — và sai.

**Ba nơi trong `core` cần kiểm soát ghi đè, và cả ba đã CÓ cơ chế** (đối chiếu source
2026-09-06):

| Nơi | Hình dạng ghi | Token dùng | Ở đâu |
|---|---|---|---|
| `SysMenuRole` (PERM-1) | ghi đè cả tập | băm cấp tập hợp | `SysMenuRoleRepository.GetVersionAsync` (`…/Repositories/SysMenuRoleRepository.cs:66`) + `MatrixVersion.Compute` (`…/Permissions/MatrixVersion.cs:35`) |
| `RolePermission` (PERM-2) | ghi đè cả tập | băm cấp tập hợp | `RolePermissionRepository.GetVersionAsync` (`…/Repositories/RolePermissionRepository.cs:32`) |
| `AspNetUsers` (`PUT /api/users/{id}`) | sửa từng bản ghi | `ConcurrencyStamp` **có sẵn của Identity** — không thêm cột, không migration | so trong handler: `UpdateUserCommand.cs:62` |

Ca `CriteriaAssessment` của module DtiWeekly đã xoá cùng module.

⚠️ Vế `AspNetUsers` **còn tuỳ chọn, không bắt buộc**: handler chỉ so khi client thật sự
gửi token (`cmd.Version is not null`). Siết lại = xoá đúng điều kiện đó, và khi siết thì
test `UserUpdateVersionTests.Put_WithoutVersion_StillSucceeds` **phải đỏ** — nó là mỏ neo
cố ý cho trạng thái nửa vời này.

> **🔄 LẬT 2026-09-06.** Bản trước ghi *"Hai bảng `core` cần kiểm soát ghi đè"* và bỏ sót
> hoàn toàn `AspNetUsers`, trong khi đường `Version`/`ConcurrencyStamp` cho `PUT /api/users/{id}`
> đã nối từ 2026-08-31. Bỏ sót này nguy hiểm theo một chiều cụ thể: người đọc file **chủ** về
> concurrency sẽ kết luận rằng sửa người dùng chưa có gì chặn ghi đè, rồi đi dựng cơ chế thứ hai
> song song với cơ chế đã có.

Ca đó đáng ghi lại vì nó là **khuôn nhận diện**, không phải chuyện riêng của DTI: một entity vừa nhận **ghi hàng loạt** (import CSV ghi đè toàn bộ field) vừa nhận **sửa tay từng field** (partial-update đọc-rồi-ghi, không có gì phát hiện ghi đè) là đúng hình dạng cần `RowVersion`. Dựng lại nghiệp vụ có import + sửa tay trên cùng bảng thì áp ngay từ đầu, đừng đợi va lỗi. Recipe cụ thể xem [`doc/huong_dan/quy-uoc/be-entity-domain.md`](../../../../doc/huong_dan/quy-uoc/be-entity-domain.md) §"RowVersion — optimistic concurrency".

### Nhưng token theo DÒNG không giải được ca này

Đây là điểm dễ đi nhầm nhất, nên nói rõ trước khi ai đó áp recipe `RowVersion` vào:

`PUT /api/admin/permissions` **không** cập nhật từng dòng — nó thay thế **toàn bộ** tập:
đánh dấu xoá mềm mọi dòng đang sống rồi chèn lại một thế hệ đầy đủ theo payload
(`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Repositories/SysMenuRoleRepository.cs:89-91`
và `:115`). Dòng còn lại sau đó là dòng **đã đánh dấu xoá**, không phải dòng hiện hành, nên
**không còn dòng nào để so token theo dòng** — `WHERE xmin = @cũ` không có gì để bám vào.

> **🔄 LẬT 2026-09-06 — nhãn 🚧 cho việc đã xong, và một tiền đề đã lỗi thời.** Bản trước
> viết hai điều, cả hai nay đều sai theo cùng một kiểu:
> 1. *"Nó xoá sạch bảng rồi chèn lại"* + citation `SysMenuRoleRepository.cs:63`. `RemoveRange`
>    đã bị thay bằng xoá mềm **2026-08-31** (comment lý do ở `SysMenuRoleRepository.cs:82-85`),
>    và dòng `:63` hôm nay nằm giữa một docstring, không phải câu lệnh nào — đúng loại citation
>    mà cổng `check-docs.sh` §6 **không** bắt được vì nó chỉ kiểm số dòng có nằm trong file.
> 2. Nhãn `🚧 "tiền đề RemoveRange sắp đổi"` và câu *"hệ quả thi công **bắt buộc** phải nhớ:
>    hàm băm phải lọc `IsDeleted = false`"*. Cả hai đã **xong**: `GetVersionAsync` đọc
>    `db.SysMenuRoles` bình thường (global query filter tự loại dòng xoá mềm) và cấm
>    `IgnoreQueryFilters()` ngay trong docstring của chính nó
>    (`SysMenuRoleRepository.cs:59-65`), `RolePermissionRepository.cs:30-31` chép lại cùng điều
>    cấm. Không còn gì "sắp đổi".
>
> Điều **không** đổi và vẫn là kết luận của mục này: token cấp tập hợp là lời giải đúng cho
> hình dạng ghi này. Thêm `RowVersion` vào `SysMenuRole` sẽ tạo cảm giác đã bảo vệ trong khi
> không bảo vệ gì cả — đúng loại thất bại im lặng mà cảnh báo provider ở đầu file nói tới.

### Cách chuẩn cho ca ghi đè cả tập — token cấp TẬP HỢP (✅ CÓ THẬT, đối chiếu 2026-09-06)

Nguyên lý không đổi, chỉ đổi thứ mang token: thay vì mỗi dòng một token, **cả tập
hợp** mang một token.

Đây **không** còn là đích đến — cơ chế đã chạy trong `src/BE`. Kiểm bằng lệnh:

```bash
grep -rn "MatrixVersion.Compute\|GetVersionAsync" src/BE --include=*.cs | grep -v /obj/
```

PASS hôm nay: khai báo ở `MatrixVersion.cs`, dùng ở **cả hai** repository ma trận, và có
integration test chốt (`Tests/PlatformManager.Core.IntegrationTests/Permissions/PermissionMatrixVersioningTests.cs`).

| Bước | Làm gì |
|---|---|
| `GET` | Tính token từ trạng thái hiện tại của bảng, trả kèm dữ liệu |
| Người dùng sửa | FE giữ nguyên token vừa nhận, không sinh mới |
| `PUT` | Gửi lại token đó. Server **tính lại** token từ DB rồi so |
| Lệch | **409** — "có người vừa đổi, tải lại trang". Không ghi gì cả |

Token nên là **hàm băm của chính dữ liệu** (băm trên tập cặp đã sắp thứ tự ổn
định), không phải cột đếm mới. Ba lý do, lý do cuối là ràng buộc cứng:

1. Không cần cột nào, nên **không cần migration**
2. Không có trạng thái phụ để lệch với dữ liệu thật
3. Phát hiện được **mọi** khác biệt, kể cả thay đổi do seeder hay SQL tay gây ra —
   một bộ đếm chỉ tăng khi có ai nhớ tăng nó

Đánh đổi đã biết: băm phải đọc toàn bộ bảng ở cả `GET` lẫn `PUT`. Với ma trận
phân quyền cỡ vài chục dòng thì không đáng kể; nếu sau này có tập hàng chục nghìn
dòng cần cùng cơ chế, đổi sang cột phiên bản riêng — và lúc đó mới cần migration.

### Ranh giới giữa hai cách

| Hình dạng ghi | Dùng | Có ai trong repo đang dùng chưa |
|---|---|---|
| Cập nhật từng bản ghi, đọc-rồi-ghi, entity **của ta** | Token theo dòng (`xmin` trên Postgres) — recipe ở [`../../quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §RowVersion | **Chưa** — không entity nào khai `.IsRowVersion()`; kiểm: `grep -rn "IsRowVersion" src/BE --include=*.cs` ra rỗng |
| Cập nhật từng bản ghi, entity của **ASP.NET Core Identity** | `ConcurrencyStamp` Identity đã có sẵn — **đừng** thêm cột thứ hai chồng lên | ✅ `AspNetUsers`, xem §"Áp dụng" |
| Ghi đè cả tập (thay thế toàn bộ, dù xoá cứng hay xoá mềm) | Token cấp tập hợp như trên | ✅ `SysMenuRole`, `RolePermission` |

> **🔄 LẬT 2026-09-06.** Bản trước mô tả ca thứ ba là *"(`RemoveRange` + chèn lại)"* — cơ
> chế đó đã đổi sang xoá mềm 2026-08-31, mà điều kiện nhận diện thì không phụ thuộc vào
> việc xoá cứng hay mềm: cái quyết định là **thay thế toàn bộ tập**. Và bảng thiếu hẳn dòng
> Identity, nên nó gợi ý sai rằng sửa người dùng phải dựng `RowVersion` mới.
