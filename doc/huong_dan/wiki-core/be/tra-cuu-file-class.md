---
kind: luat
scope: core
verified: 2026-09-01
---

# Tra cứu file / class / interface — PlatformManager

> ✅ **CÓ THẬT** — đối chiếu source trong `src/BE/` ngày **2026-09-01**. Mọi tên dưới đây được
> xác minh bằng cách **mở chính file nguồn** ghi ở cột *Ở đâu*, không suy từ tên.
>
> ⚠️ **"Tên có thật" ≠ "danh sách đầy đủ".** Lượt 2026-09-01 xác minh từng dòng **đang có**;
> nó không kiểm chiều ngược lại — file nào trong `src/BE/` **thiếu** khỏi bảng. Đợt rà
> 2026-09-08 tìm ra bốn chỗ hụt theo đúng chiều đó (§2.1 `ErrorCode` bỏ mã 405, §2.1
> `BaseResponse` ghi "đúng 2 động từ" trong khi có 3, §5 thiếu 5 file của host, §6.1 liệt tay
> 5 trong số nhiều hơn thế) và đã bổ sung. Vì vậy `verified:` ở frontmatter **giữ nguyên
> 2026-09-01**: chưa ai mở lại toàn bộ file để đối chiếu hai chiều.

> ⚠️ **File này KHÁC bộ tài liệu tham chiếu của VNR.Successor**
> (`doc/tham-khao-ngoai/vnr-successor/`, `kind: tham-chieu`). Các file `00-` → `07-` ở đó mô tả
> **VNR.Successor** — một backend production khác, dùng làm *tham chiếu hình dạng*: chữ ký class,
> thứ tự đăng ký DI, một thành phần trông ra sao khi hệ đã lớn. File này mô tả **code thật của
> repo này**, nên khi hai bên nói khác nhau thì **file này đúng**.
>
> Đọc chéo hai bên: bảng ánh xạ tên ở [`00-lo-trinh-tong-the.md`](../../../tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md)
> §ĐỌC TRƯỚC, và bảng **"KHÔNG áp dụng — vì sao"** ở §7 bên dưới.
>
> Ranh giới `Core.*` ↔ `Business.*` và những project **chưa tồn tại**: đọc
> `doc/kien-truc-core-module.md` **trước khi tạo file mới**.

---

## 0. Cách tự dựng lại bảng này — đừng tin bảng, hãy chạy lệnh

Bảng liệt kê tay luôn mục ruỗng: bản trước của chính file này liệt kê hơn một trăm cái tên **của
một dự án khác**, và không một tên nào tồn tại ở đây. Vì vậy mỗi mục bên dưới mở đầu bằng **lệnh
đếm**, không bằng **số đếm** (`.claude/CLAUDE.md` §6). Lệnh mới là thứ không bao giờ cũ.

```bash
find src/BE -name "*.csproj" -not -path "*/obj/*" -not -path "*/bin/*" | sort   # project thật trên đĩa
grep -c '<Project Path=' src/BE/PlatformManager.slnx                            # project thật trong solution
find src/BE -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | wc -l      # tổng file nguồn
```

Hai con số project **có thể khác nhau** và đã lệch nhiều lần — lấy con số thứ hai khi hỏi
"`dotnet build` biên dịch cái gì".

### Tiêu chí chọn dòng cho bảng này

Chỉ những thành phần **có vai trò kiến trúc**: thứ mà người mới cần biết là *nó tồn tại* và
*nó giữ luật gì*. Cụ thể là **không** liệt kê:

- **DTO / record request-response** (`UserDto`, `MenuItemDto`, `PermissionMatrixDto`…) — chúng là
  hình dạng dữ liệu của một màn hình, đổi theo hợp đồng ở `doc/contracts/`, không phải luật.
- **Từng command / query / handler / validator** — chúng theo đúng một khuôn duy nhất (§2), nên
  biết khuôn là đủ; liệt kê từng cái chỉ tạo ra một danh sách phải bảo trì.

Cần danh sách đầy đủ của hai nhóm trên thì dùng lệnh, đừng chép ra tài liệu:

```bash
grep -rl ": ICommand<\|: IQuery<" --include="*.cs" src/BE   # mọi command/query
grep -rl "AbstractValidator<"     --include="*.cs" src/BE   # mọi validator
```

---

## 1. `PlatformManager.Core.Domain` — luật của dữ liệu, zero-dependency

```bash
find src/BE/Core/PlatformManager.Core.Domain -name "*.cs" | sort
grep -c PackageReference src/BE/Core/PlatformManager.Core.Domain/PlatformManager.Core.Domain.csproj
```

| Tên | Loại | Ở đâu | Giữ luật gì |
| --- | --- | --- | --- |
| `BaseEntity` | abstract class | `src/BE/Core/PlatformManager.Core.Domain/Common/BaseEntity.cs` | **Non-generic**, `Id` kiểu `Guid` và là `init` — sinh ngay lúc khởi tạo, không ai gán lại. Đúng 5 field audit (`CreatedBy`/`UpdatedBy`/`CreatedAt`/`UpdatedAt`/`IsDeleted`) có setter public, và **chỉ 5 field đó** — mở setter cho field nghiệp vụ là thứ `EntityEncapsulationTests` bắt |
| `EntityId.New()` | static method | `src/BE/Core/PlatformManager.Core.Domain/Common/EntityId.cs` | Điểm sinh Id **duy nhất** toàn hệ thống — `Guid.CreateVersion7()` (UUID v7, tăng dần theo thời gian nên không phân mảnh trang index B-tree). Không nơi nào gọi `Guid.NewGuid()` trực tiếp: seam một dòng để đổi chiến lược sinh Id |
| `DomainException` | exception | `src/BE/Core/PlatformManager.Core.Domain/Common/DomainException.cs` | Vi phạm invariant nghiệp vụ, mang `Code` riêng (không phải HTTP status) → 422 |
| `ConflictException` | exception | `src/BE/Core/PlatformManager.Core.Domain/Common/ConflictException.cs` | Xung đột trạng thái của chính resource → 409 |
| `SysMenu` | entity | `src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenu.cs` | Metadata menu, tự tham chiếu 1 cấp. Mẫu **factory method**: ctor `private`, field `private set`, `Create()` kiểm invariant. Có `ReviveWith()` — đường hồi sinh dòng đã xoá mềm, bắt buộc vì `Code` chỉ unique trong tập chưa xoá |
| `SysMenuRole` | entity | `src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenuRole.cs` | Bảng nối menu × role. Vắng mặt = **mở** cho mọi user đã đăng nhập |
| `RolePermission` | entity | `src/BE/Core/PlatformManager.Core.Domain/Entities/RolePermission.cs` | Bảng nối role × `ResourceKey`. Vắng mặt = **từ chối** (deny-by-default) — ngược chiều mặc định với `SysMenuRole`, và đó là điểm dễ nhầm nhất giữa hai bảng |

**Hai bảng nối trên kế thừa `BaseEntity` từ 2026-08-31**, kèm một hệ quả không hiển nhiên: khoá
chính **không** còn là cặp ghép mà là `Id`; tính duy nhất của cặp giữ bằng **unique index lọc theo
`IsDeleted = false`**. Cặp ghép làm khoá thì một dòng đã xoá mềm và một dòng mới cùng cặp không thể
cùng tồn tại — lần lưu thứ hai của cùng một ô sẽ trùng khoá.

📖 Hợp đồng đầy đủ: `doc/huong_dan/quy-uoc/be-entity-domain.md`

---

## 2. `PlatformManager.Core.Application` — envelope, CQRS, và mọi seam ra hạ tầng

```bash
find src/BE/Core/PlatformManager.Core.Application -name "*.cs" | sort
```

Tầng này **không được** biết tới EF Core, ASP.NET Core, Hangfire hay bất kỳ hạ tầng cụ thể nào —
luật đó do `LayerDependencyTests` cưỡng chế, không do review người.

### 2.1 Envelope kết quả — `Common/Results/`

| Tên | Loại | Ở đâu | Giữ luật gì |
| --- | --- | --- | --- |
| `ErrorCode` | enum | `src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorCode.cs` | **Giá trị enum CHÍNH LÀ mã HTTP** — không có bảng map thứ hai để lệch. On-wire là **tên member** (string), không phải số. Đừng chép danh sách mã vào đây (`.claude/CLAUDE.md` §6): `grep -n '= [0-9]' src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorCode.cs`. ⚠️ `MethodNotAllowed = 405` (`ErrorCode.cs:27`, thêm 2026-09-04) **chỉ hạ tầng định tuyến sinh ra** — handler nghiệp vụ không bao giờ chọn nó |
| `ApiResultStatus` | enum | `src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiResultStatus.cs` | 4 nhóm cho FE quyết định cách phản ứng: `SUCCESS`/`VALIDATION_ERROR`/`BUSINESS_ERROR`/`SYSTEM_ERROR` |
| `ApiErrorIds.StatusForCode` | static method | `src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiErrorIds.cs` | Nguồn **DUY NHẤT** map `ErrorCode → ApiResultStatus`. `Status` không bao giờ gán tay ở nơi khác |
| `IApiResult<T>` · `IHasApiResultStatus` | interface | `src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs` | Envelope chuẩn cho **mọi** endpoint kể cả danh sách. `IHasApiResultStatus` cho phép đọc `Status`/`Code` mà không cần biết `T` |
| `ApiResult<T>` | class | `src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiResult.cs` | Hiện thực duy nhất — 4 factory `Success`/`BusinessError`/`ValidationError`/`SystemError` |
| `ErrorDescriptor` | record | `src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorDescriptor.cs` | `BusinessCode + ErrorCode + MessageTemplate + Retryable` — thay cho magic string. Khai **tập trung theo nhóm tính năng**, không rải literal trong handler |
| `BaseResponse` | abstract class | `src/BE/Core/PlatformManager.Core.Application/Common/Results/BaseResponse.cs` | Mặt duy nhất handler chạm tới. **Ba** phương thức, không phải hai: `Ok<T>()` (`:11`), `Fail<T>(ErrorDescriptor, params (string Name, object? Value)[])` (`:39`) và nạp chồng thứ hai của `Fail<T>` nhận `fieldErrors` (`:78`, thêm 2026-09-05) — dành cho lỗi mà nguyên nhân thật nằm ở **từng ô nhập**, câu giữ khuôn cố định còn danh sách mã đi ra `fieldErrors`. Đối chiếu 2026-09-08 bằng `grep -n 'protected static' …/BaseResponse.cs` |
| `PagedList<T>` | class | `src/BE/Core/PlatformManager.Core.Application/Common/Models/PagedList.cs` | `Items` + `TotalCount` + `Page` + `PageSize`, luôn bọc trong `IApiResult<PagedList<T>>`, không trả trần |

Nơi khai `ErrorDescriptor` là **một file tĩnh cạnh nhóm tính năng**, không phải một registry trung
tâm — tìm bằng lệnh, đừng chép danh sách:

```bash
grep -rln "static readonly ErrorDescriptor" --include="*.cs" src/BE
```

Riêng `RateLimitErrors`
(`src/BE/Core/PlatformManager.Core.Application/Common/Results/RateLimitErrors.cs`) nằm cạnh
`ErrorCode` chứ không cạnh nhóm tính năng nào — vì 429 là lỗi **hạ tầng dùng chung**, phát ra từ
`RateLimiterOptions.OnRejected` trong host, không thuộc tính năng nào.

### 2.2 CQRS và pipeline

| Tên | Loại | Ở đâu | Giữ luật gì |
| --- | --- | --- | --- |
| `ICommand<TResult>` | interface | `src/BE/Core/PlatformManager.Core.Application/Common/CQRS/ICommand.cs` | `: IRequest<IApiResult<TResult>>` — envelope bị ép ở **tầng type**, handler không thể "quên" trả envelope |
| `IQuery<TResult>` | interface | `src/BE/Core/PlatformManager.Core.Application/Common/CQRS/IQuery.cs` | Như trên, phía đọc |
| `ExceptionHandlingBehavior<,>` | pipeline behavior | `src/BE/Core/PlatformManager.Core.Application/Common/Behaviors/ExceptionHandlingBehavior.cs` | **Ngoài cùng.** Bắt đúng 2 loại — `DomainException` → 422, `ConflictException` → 409 — và dịch thành envelope **ngay tại Application**. Cố ý **không** bắt `ValidationException`: cái đó phải bay tiếp lên middleware toàn cục |
| `ValidationBehavior<,>` | pipeline behavior | `src/BE/Core/PlatformManager.Core.Application/Common/Behaviors/ValidationBehavior.cs` | Chạy mọi FluentValidation validator trước handler rồi **ném** `ValidationException` — không tự dựng response lỗi. Mỗi validator một `ValidationContext` **riêng**: dùng chung thì rule `Custom` ghi lỗi vào context chung và lỗi bị đếm lặp |
| `AddCoreApplication()` | extension method | `src/BE/Core/PlatformManager.Core.Application/DependencyInjection.cs` | **Nơi duy nhất** đăng ký 2 behavior trên. Behavior là open-generic nên áp cho mọi request của mọi assembly — tầng nghiệp vụ **không** đăng ký lại |

Thứ tự đăng ký **chính là** thứ tự pipeline. Đảo hai dòng đó thì `ValidationException` bị
`ExceptionHandlingBehavior` nhìn thấy trước — hành vi đổi mà không có lỗi biên dịch nào.
`PipelineBehaviorTests` (§6) chạy **thật** qua MediatR để bắt đúng chuyện này.

### 2.3 Seam ra hạ tầng — interface khai ở đây, hiện thực ở tầng khác

| Tên | Ở đâu | Hiện thực ở |
| --- | --- | --- |
| `ICurrentUser` | `src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/ICurrentUser.cs` | **Host**, không phải Infrastructure — `HttpContextCurrentUser` (§5), vì `HttpContext` là khái niệm ASP.NET Core |
| `IUnitOfWork` | `src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IUnitOfWork.cs` | `UnitOfWork` (§4). Handler own `SaveChanges`, **đúng một lần, ở cuối** — repository không bao giờ tự gọi |
| `IDateTimeProvider` | `src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IDateTimeProvider.cs` | `SystemDateTimeProvider` (§4) — để logic phụ thuộc "hôm nay" test được |
| `IBackgroundJobScheduler` | `src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs` | `HangfireBackgroundJobScheduler` (§4). Gọi thẳng `BackgroundJob.Enqueue` nghĩa là Application reference Hangfire — `LayerDependencyTests` liệt "Hangfire" vào danh sách cấm đúng vì chuyện này đã xảy ra thật |
| `INotificationSender` | `src/BE/Core/PlatformManager.Core.Application/Notifications/INotificationSender.cs` | `SmtpNotificationSender` (§4) — hiện **chưa được đăng ký DI**, xem §4 |
| `ISysMenuRepository` · `ISysMenuRoleRepository` | `src/BE/Core/PlatformManager.Core.Application/Menu/ISysMenuRepository.cs` · `src/BE/Core/PlatformManager.Core.Application/Menu/ISysMenuRoleRepository.cs` | §4. Application chỉ làm việc với **tên role** (string); Infrastructure tự resolve sang `AppRole.Id` |
| `IRolePermissionRepository` | `src/BE/Core/PlatformManager.Core.Application/Permissions/IRolePermissionRepository.cs` | §4 |
| `IIdentityService` | `src/BE/Core/PlatformManager.Core.Application/Auth/IIdentityService.cs` | §4. Application không biết `AppUser`/`SignInManager` tồn tại |
| `IUserAdminService` · `IUserLookupService` | `src/BE/Core/PlatformManager.Core.Application/Users/IUserAdminService.cs` · `src/BE/Core/PlatformManager.Core.Application/Users/IUserLookupService.cs` | §4 |
| `IPermissionChecker` | `src/BE/Core/PlatformManager.Core.Application/Permissions/IPermissionChecker.cs` | §4. Tách khỏi filter để **luồng quyết định** test được không cần Postgres, còn **ngữ nghĩa truy vấn** vẫn phải test trên Postgres thật |

### 2.4 Phân quyền và tài khoản — luật khai ở Application

| Tên | Loại | Ở đâu | Giữ luật gì |
| --- | --- | --- | --- |
| `RequirePermissionAttribute` | attribute | `src/BE/Core/PlatformManager.Core.Application/Permissions/RequirePermissionAttribute.cs` | **Metadata thuần**, không logic. Cưỡng chế thật nằm ở `RequirePermissionFilter` (§4) |
| `ICoreResourceKeySource` + `ResourceKeyDefinition` | interface + record | `src/BE/Core/PlatformManager.Core.Application/Permissions/ICoreResourceKeySource.cs` | **Seam** danh mục permission-key: Core tiêu thụ, host cung cấp. Core cố ý **không** có hiện thực mặc định — thiếu đăng ký thì DI hỏng ngay thay vì trả về danh mục rỗng (deny-by-default sẽ biến thành 403 hàng loạt không lời giải thích). `ResourceKeyDefinition.SeedRoles` khai vai được cấp key ở **lần seed**, mặc định `[Admin, User]` (2026-09-09) |
| `ResourceKeyCatalogStartupValidator` | `IHostedService` | `src/BE/Core/PlatformManager.Core.Infrastructure/Permissions/ResourceKeyCatalogStartupValidator.cs` | **Fail-fast cho DỮ LIỆU đi qua seam** (thêm 2026-09-09): chạy `Catalog()` một lần lúc khởi động ⇒ host khai danh mục sai thì **không boot**. Trước đó phép kiểm chỉ chạy khi có người DÙNG danh mục, nên cấu hình sai thành **500 lúc mở màn Phân quyền** thay vì lỗi lúc deploy. Guard trong `Catalog()` giữ nguyên làm lưới thứ hai — đường `--seed` không đi qua host |
| `AppResourceKeys` + `AppResourceKeySource` | static class + hiện thực seam | `src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs` | **Dữ liệu** của dự án này: permission-key **thô** (không tách View/Create/Delete) + tên hiển thị map **tĩnh**, không lưu DB. Trước 2026-09-03 nằm trong Core dưới tên `ResourceKeys`; tách ra vì đó là dữ liệu dự án, xem `doc/kien-truc-core-module.md` |
| `MatrixVersion` | static class | `src/BE/Core/PlatformManager.Core.Application/Permissions/MatrixVersion.cs` | Token phiên bản **cấp tập hợp** cho hai ma trận phân quyền: băm SHA-256 của chính dữ liệu, không phải cột đếm. `GET` trả token, `PUT` gửi lại, server tính lại rồi so; lệch ⇒ 409 và **không ghi gì** |
| `PermissionErrors` | static class | `src/BE/Core/PlatformManager.Core.Application/Permissions/PermissionErrors.cs` | `VersionConflict`. **Thiếu hẳn** `version` cũng rơi vào đây (409), không phải 400: với lệnh ghi đè toàn bộ, "không gửi token" và "gửi token cũ" đều là ghi mà không biết đang đè lên cái gì |
| `SuperAdminAccountGuard` | static class | `src/BE/Core/PlatformManager.Core.Application/Users/SuperAdminAccountGuard.cs` | **Một chỗ duy nhất** cho mọi luật bảo vệ tài khoản quản trị (leo thang quyền, tự gỡ quyền mình, khoá `SuperAdmin`, tự khoá mình). Trả `ErrorDescriptor` chứ không `bool` — mỗi vi phạm có câu thông báo riêng |
| `Roles` | static class | `src/BE/Core/PlatformManager.Core.Application/Common/Roles.cs` | 3 role đã chốt: `SuperAdmin`/`Admin`/`User`, so khớp theo **tên**, không FK cứng bằng Guid ở tầng này |

📖 `doc/huong_dan/quy-uoc/be-cqrs-handler.md` · `doc/contracts/permissions.md`

---

## 3. Nhóm tính năng của Core — vertical slice, không phải layer

```bash
ls src/BE/Core/PlatformManager.Core.Application    # mỗi thư mục = 1 nhóm tính năng
```

`Auth/`, `Users/`, `Menu/`, `Permissions/`, `Notifications/` — mỗi thư mục chứa **command/query +
handler + validator + DTO + catalog lỗi** của nhóm đó, đặt cạnh nhau. Đây là điều cần biết về hình
dạng; danh sách từng file thì dùng lệnh ở §0.

Ranh giới đáng nhớ nhất là giữa **hai** cơ chế phân quyền, vì chúng trông giống nhau mà không thay
thế nhau:

| | `SysMenuRole` (PERM-1) | `RolePermission` (PERM-2) |
| --- | --- | --- |
| Điều khiển | Menu **nhìn thấy được** | **Gọi được API** hay không |
| Vắng mặt nghĩa là | Mở cho mọi user đã đăng nhập | Từ chối (deny-by-default) |
| Không chặn được | Gọi thẳng API bằng `curl` | — |

Ai chỉ cài PERM-1 rồi tưởng đã phân quyền là đã bỏ trống toàn bộ hàng rào thật.

---

## 4. `PlatformManager.Core.Infrastructure` — EF Core, Identity, hiện thực seam

```bash
find src/BE/Core/PlatformManager.Core.Infrastructure -name "*.cs" -not -path "*Migrations*" | sort
```

| Tên | Loại | Ở đâu | Giữ luật gì |
| --- | --- | --- | --- |
| `PlatformManagerDbContext` | DbContext | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/PlatformManagerDbContext.cs` | **Một** DbContext cho toàn hệ (1 Postgres). Kế thừa `IdentityDbContext<AppUser, AppRole, Guid>`. Schema mặc định `core`; tầng nghiệp vụ khai `business` tường minh trong `ToTable()`. Hằng `SoftDeleteFilterKey` là **public** vì ArchTest tra đúng khoá đó |
| `EfConfigurationAssembly` | record | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/EfConfigurationAssembly.cs` | **Seam đảo phụ thuộc cho EF**: mỗi tầng tự đăng ký assembly chứa `IEntityTypeConfiguration<T>` của mình qua DI, `OnModelCreating` quét từng assembly đã đăng ký. Nhờ vậy Core **không** cần `ProjectReference` tới tầng nghiệp vụ nào |
| `*Configuration` | `IEntityTypeConfiguration<T>` | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Configurations/SysMenuConfiguration.cs` (và 4 file cạnh nó) | Nơi khai **unique index lọc** `HasFilter("\"IsDeleted\" = false")` — vế thứ hai của soft-delete, khác hẳn global query filter |
| `AuditInterceptor` | `SaveChangesInterceptor` | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs` | Ghi 4 field audit cho **mọi** `BaseEntity` đang SaveChanges. Lúc `Added` set luôn `UpdatedAt`/`UpdatedBy` — để FE không phải viết `updatedAt ?? createdAt` ở mọi chỗ. **Không** chạm `AppUser`/`AppRole` (chúng không kế thừa `BaseEntity`), đó là lý do `AppUser.CreatedBy`/`UpdatedBy` phải ghi tay |
| `UnitOfWork` | class | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/UnitOfWork.cs` | `SaveChangesAsync` + `DiscardTrackedChanges()` (dọn change-tracker khi một dòng import lỗi giữa chừng, tránh rò entity chưa lưu sang dòng sau) |
| `CoreSeeder` | class | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs` | Seed role + 2 tài khoản bootstrap + menu + quyền. **DML thuần, idempotent**, và chỉ chạy khi có `--seed` — không bao giờ chạy lúc app khởi động |
| `BootstrapOptions` | options | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/BootstrapOptions.cs` | **Không có giá trị mặc định, và không được đặt** — một tài khoản quản trị với mật khẩu ai cũng đoán được còn tệ hơn hẳn một app từ chối khởi động |
| `AppUser` · `AppRole` | Identity entity | `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/AppUser.cs` · `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/AppRole.cs` | **Không** kế thừa `BaseEntity` — Identity tự quản vòng đời bằng field riêng (`LockoutEnd`/`SecurityStamp`). Đây là chủ đích, không phải sót |
| `IdentityService` · `UserAdminService` · `UserLookupService` | class | `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs` (và 2 file cạnh nó) | Hiện thực 3 seam của §2.3, orchestrate qua `UserManager`/`SignInManager`/`RoleManager` |
| `PermissionChecker` | class | `src/BE/Core/PlatformManager.Core.Infrastructure/Permissions/PermissionChecker.cs` | **Đúng 1 query** `EXISTS`, INNER JOIN `AspNetRoles` — join chứ không lọc thẳng, để dòng `RolePermission` **mồ côi** bị loại. **Không cache**: quyền thu hồi phải có hiệu lực ngay |
| `RequirePermissionFilter` | `IAsyncAuthorizationFilter` | `src/BE/Core/PlatformManager.Core.Infrastructure/Permissions/RequirePermissionFilter.cs` | Cơ chế cưỡng chế thật của `[RequirePermission]`. Không khai attribute = không chặn thêm gì — hai cơ chế (`[Authorize]` + permission-key) **cộng dồn**, không thay thế |
| `SystemDateTimeProvider` | class | `src/BE/Core/PlatformManager.Core.Infrastructure/Common/SystemDateTimeProvider.cs` | Hiện thực `IDateTimeProvider` |
| `HangfireBackgroundJobScheduler` | class | `src/BE/Core/PlatformManager.Core.Infrastructure/BackgroundJobs/HangfireBackgroundJobScheduler.cs` | **Nơi duy nhất ngoài host** được biết tới Hangfire. Dùng `IBackgroundJobClient` qua DI, không dùng facade tĩnh `BackgroundJob` (facade đọc trạng thái toàn cục) |
| `SmtpNotificationSender` · `SmtpOptions` | class · options | `src/BE/Core/PlatformManager.Core.Infrastructure/Notifications/SmtpNotificationSender.cs` · `src/BE/Core/PlatformManager.Core.Infrastructure/Notifications/SmtpOptions.cs` | Hiện thực `INotificationSender`. ⚠️ **Cố ý chưa đăng ký** — chưa có section `Smtp` trong cấu hình, mà `ValidateOnStart()` sẽ chặn boot nếu thiếu |

### Bốn cách đăng ký DI, và vì sao không gộp làm một

| Extension | Ở đâu | Vì sao tách |
| --- | --- | --- |
| `AddCoreModule(config, requireBootstrapOptions)` | `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs` | Composition của Core. `requireBootstrapOptions` chỉ `true` ở đường `--seed`: bắt buộc secret cho một tiến trình API không bao giờ đọc tới nó khiến người vận hành kết luận nhầm rằng đã bootstrap xong |
| `AddPermissionInfrastructure()` | `src/BE/Core/PlatformManager.Core.Infrastructure/PermissionInfrastructureExtensions.cs` | Chỉ đăng ký DI, **không** tự thêm filter vào MVC options — host tự làm, để đọc `Program.cs` là thấy |
| `AddBackgroundJobInfrastructure()` | `src/BE/Core/PlatformManager.Core.Infrastructure/BackgroundJobInfrastructureExtensions.cs` | Phụ thuộc `AddHangfire(...)` đã chạy. Gộp ngầm sẽ biến "quên gọi `AddHangfire`" thành lỗi runtime lúc resolve |
| `AddNotificationInfrastructure(config)` | `src/BE/Core/PlatformManager.Core.Infrastructure/NotificationInfrastructureExtensions.cs` | Cùng lý do — và hiện **không dòng nào gọi nó** |

### Migration — sinh `.sql`, không áp schema

```bash
ls src/BE/PlatformManager.Api/Persistence/Migrations/            # .cs + ModelSnapshot (host)
ls src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/   # .sql (Core ship)
```

**Chuyển 2026-09-04:** migration `.cs` + `ModelSnapshot` nay thuộc **project host**, chỉ thư mục
`sql/` ở lại Core — vì hai dự án dùng chung một `ModelSnapshot` là hỏng không tránh được. Vấn đề,
chốt và thao tác cho dự án 2: đọc `doc/cau-truc-database.md` §5.3.

Schema thật áp bằng file `.sql` chạy tay
(`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0001_initial_baseline.sql`),
**không** bằng `dotnet ef database update` — lệnh đó bị `settings.json` chặn. Migration chỉ là cỗ
máy tính delta để sinh ra chính file `.sql` đó, nên `MigrationsAssembly` khai đúng một chỗ:
design-time factory `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs`, **không** khai
ở `Core.Infrastructure` (khối chú thích tại chỗ nói vì sao).

> ⚠️ Khối cảnh báo *"🛑 DỪNG — KHÔNG CHẠY `dotnet ef migrations add`"* ở đầu
> `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs` nói `migrations add` sẽ sinh
> `DropTable` cho 5 bảng `business`. **Tiền đề đó không còn đúng** sau baseline 2026-08-31 —
> snapshot hiện hành không khai bảng `business` nào (đo 2026-09-04). Khối vẫn để nguyên vì đóng
> băng là quyết định của người dùng; đừng lấy nó làm nguồn, xem `doc/cau-truc-database.md` §5.3
> §"Còn treo".

📖 Quy trình dựng lại DB: `doc/cau-truc-database.md` §5 · `doc/huong_dan/quy-uoc/be-performance.md`

---

## 5. `PlatformManager.Api` — host mỏng, và là composition root duy nhất

```bash
find src/BE/PlatformManager.Api -name "*.cs" | sort
```

Đây là project **duy nhất** được thấy mọi tầng. Controller hiện đặt ở đây (`Controllers/`) vì
`Core.Api` **đã dựng 2026-09-09** (Q8), hiện chỉ chứa `ApiControllerBase` — đọc `doc/kien-truc-core-module.md` trước khi thêm file vào đó.

| Tên | Loại | Ở đâu | Giữ luật gì |
| --- | --- | --- | --- |
| `Program.cs` | top-level statements | `src/BE/PlatformManager.Api/Program.cs` | Composition root. **Thứ tự middleware là hợp đồng**, không phải sở thích — mỗi khối có comment nói rõ vì sao nó đứng ở đó |
| `ApiControllerBase` | abstract class | `src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs` | `[Authorize]` đặt **ở base** ⇒ fail-closed mặc định, không opt-in (`AuthController.Logout` từng vô tình public vì thiếu cả hai attribute). `HandleResult<T>` là **nơi duy nhất** map `ErrorCode` → HTTP status |
| `GlobalExceptionHandler` | `IExceptionHandler` | `src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs` | Lưới **ngoài** MediatR pipeline, dịch 3 loại: `ValidationException` → 400 kèm `fields`, `AntiforgeryValidationException` → 403, còn lại → 500. Không lộ stack trace |
| `HttpContextCurrentUser` | class | `src/BE/PlatformManager.Api/Common/HttpContextCurrentUser.cs` | Hiện thực `ICurrentUser` từ `ClaimsPrincipal` của cookie session |
| `CorsPolicyOptions` | options | `src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs` | Allowlist origin qua `IOptions<T>` chứ **không** `?? []` — allowlist rỗng chặn *mọi* origin trong khi `/health` vẫn xanh. `ValidateOnStart()` chỉ gắn ở Production, để Development vẫn chạy được với cấu hình sẵn trong repo |
| `OriginValidationMiddleware` | middleware | `src/BE/PlatformManager.Api/Common/OriginValidationMiddleware.cs` | **CSRF lớp 1.** FE ở origin khác ⇒ cookie phải `SameSite=None` ⇒ lớp bảo vệ `SameSite` **không tồn tại** ở dự án này; đây là thứ thay vào đúng chỗ đó. JavaScript không đặt được header `Origin`, nên nó độc lập hoàn toàn với token antiforgery |
| `LoginUserNameRateLimitMiddleware` | middleware | `src/BE/PlatformManager.Api/Common/LoginUserNameRateLimitMiddleware.cs` | Đọc trước tên đăng nhập trong thân request để rate limit phân vùng **theo tên** (hàng rào thứ hai, cạnh phân vùng theo IP). Phải **tua thân request về 0** — quên là mọi lần đăng nhập trả 400 "UserName không được rỗng". Đặt **ngay trước** `UseRateLimiter()` |
| `TraceIdLogEnrichmentMiddleware` | middleware | `src/BE/PlatformManager.Api/Common/TraceIdLogEnrichmentMiddleware.cs` | Gắn `TraceIdentifier` vào **mọi** log entry. Thiếu bước này thì mã `traceId` người dùng đọc cho bộ phận hỗ trợ **không nối được với log nào**. Đặt **trước mọi middleware khác** |
| `SeedCommand` | static class | `src/BE/PlatformManager.Api/Common/SeedCommand.cs` | Lệnh `--seed`: chạy một lần rồi thoát, **không** mở cổng. Tách khỏi đường khởi động vì seed-lúc-boot từng nằm sau `IsDevelopment()`, nên một DB Production mới = không ai đăng nhập được và cũng không có đường tạo tài khoản đầu tiên. Trả **mã thoát**, không nuốt lỗi |
| `HangfireDashboardAuthFilter` | `IDashboardAuthorizationFilter` | `src/BE/PlatformManager.Api/Common/HangfireDashboardAuthFilter.cs` | `/hangfire` **không** có auth mặc định — để mở nguyên là lộ toàn bộ job. Chỉ `SuperAdmin` |
| `PlatformManagerDbContextFactory` | `IDesignTimeDbContextFactory` | `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs` | Cho `dotnet ef` chạy được ngoài runtime DI. Mang khối cảnh báo đóng băng migration — đọc trước khi chạy lệnh EF nào |
| `ApiStatusCodeEnvelopeMiddleware` | middleware | `src/BE/PlatformManager.Api/Common/ApiStatusCodeEnvelopeMiddleware.cs` | Bọc envelope cho **đúng hai** mã do hạ tầng định tuyến tự sinh với thân RỖNG: 404 (không route nào khớp) và 405 (sai verb). Chúng không đến từ handler, không từ exception, không từ model binding — nên mọi bộ dựng envelope đã có đều không thấy. Cố ý **không** dùng `MapFallback` (nuốt mất 405, biến nó thành 404) và **không** dùng `UseStatusCodePages` (bắt-tất-cả 400–599, âm thầm hứng nhánh mới không ai khai mã) |
| `ModelBindingProblemFactory` | static class | `src/BE/PlatformManager.Api/Common/ModelBindingProblemFactory.cs` | Nhánh 400 xảy ra **trước** MediatR nên `GlobalExceptionHandler` không thấy: `[ApiController]` tự trả `ValidationProblemDetails` thô, không `status`/`code`/`businessCode`/`fields`. Cố ý **không** chuyển tiếp `ModelError.ErrorMessage` (rò nội tại bộ đọc JSON và tên tham số C#) — thông điệp gốc về log kèm `TraceId`. Không giải bằng `SuppressModelStateInvalidFilter`: cần đổi **hình dạng** response, không phải bỏ kiểm tra |
| `AppResourceKeys` · `AppResourceKeySource` | `ICoreResourceKeySource` | `src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs` | **Seam dữ liệu của host** (chuyển khỏi Core 2026-09-03). Danh mục permission-key + nhãn tiếng Việt là thứ riêng của dự án; cơ chế (deny-by-default, ma trận, luật "PUT phải phủ đủ danh mục", seed) ở lại Core |
| `AppBootstrapAccountSource` | `ICoreBootstrapAccountSource` | `src/BE/PlatformManager.Api/Seeding/AppBootstrapAccountSource.cs` | **Seam dữ liệu của host** (chuyển 2026-09-02). Email `@platformmanager.local` + nhãn tiếng Việt của 2 tài khoản bootstrap. **Mật khẩu không bao giờ khai ở đây** — đọc từ `BootstrapOptions`. Đổi email rồi seed lại **không** có tác dụng: `CoreSeeder` tìm theo tên đăng nhập |
| `AppMenuSeedSource` | `ICoreMenuSeedSource` | `src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs` | **Seam dữ liệu của host** (chuyển 2026-09-02). Nhãn/route/icon menu Core. Sửa bảng này là sửa hợp đồng `GET /api/meta/menu` — cập nhật `doc/contracts/meta-menu.md` cùng lượt. Menu **nghiệp vụ** không khai ở đây |
| `Controllers/` | controller | `src/BE/PlatformManager.Api/Controllers/AuthController.cs` (và 3 file cạnh nó) | `AuthController` (`api/auth`), `MetaController` (`api/meta`), `UsersController` (`api/users`, gate role), `PermissionsController` (`api/admin/permissions`, gate `SuperAdmin`). Đều mỏng: nhận request → `ISender` → `HandleResult` |

📖 `doc/huong_dan/quy-uoc/be-api-controller.md` · `doc/huong_dan/wiki-core/be/02-identity-auth.md` ·
`doc/huong_dan/wiki-core/be/07-observability.md`

---

## 6. `Tests/` — luật nào được máy canh

```bash
find src/BE/Tests -name "*.cs" -not -path "*/obj/*" | sort
dotnet test src/BE/PlatformManager.slnx
```

### 6.1 ArchTests — tra theo tên test, không theo mã `T_xxx`

Dự án này **không dùng hệ mã `T_xxx`** của VNR; mỗi test tự mô tả bằng tên.

> **🔄 Sửa 2026-09-08 — bảng cũ liệt tay 5 file trong khi thư mục có nhiều hơn hẳn.** Một
> bảng chép tay ở đây mục ruỗng theo đúng khuôn `.claude/CLAUDE.md` §6, và cách nó hỏng thì
> im lặng: người đọc thấy 5 dòng, tin rằng đó là toàn bộ lưới đang canh, rồi kết luận sai về
> thứ **không** được canh. Thay bằng lệnh.

Danh sách file — chạy lệnh, đừng tin đoạn văn nào:

```bash
ls src/BE/Tests/PlatformManager.ArchTests/*.cs
```

Tên từng test và luật nó canh — tên test **là** lời mô tả, đọc thẳng:

```bash
grep -rn "public void \|public async Task " src/BE/Tests/PlatformManager.ArchTests/*.cs
```

Bốn ghi chú **không** đọc ra được từ tên test, nên phải viết ra (đối chiếu 2026-09-08):

| File | Điều mà tên test không nói |
| --- | --- |
| `LayerDependencyTests.cs` | ⚠️ Giới hạn đã đo: `GetReferencedAssemblies()` chỉ thấy assembly **thật sự được dùng**, nên test bắt "code gọi thẳng hạ tầng", **không** bắt "csproj còn PackageReference thừa" |
| `PipelineBehaviorTests.cs` | Không phải reflection thuần — **chạy thật qua MediatR** với command giả lập, nên nó canh cả **hành vi** lẫn **thứ tự** hai behavior (§2.2) |
| `CoreModuleBoundaryTests.cs` | Danh sách module hiện **rỗng** (module nghiệp vụ đã gỡ 2026-08-29) — giữ nguyên vì đây là bảo hiểm miễn phí cho module đầu tiên được dựng lại |
| Nhiều file có test tên `Detector_*` / `*Detector_Catches_*` | Đó là **test của chính bộ dò**, không phải test của sản phẩm. Chúng tồn tại vì một bộ dò quét mã nguồn có thể xanh **vì mù**; xoá chúng là bỏ đúng thứ chứng minh lưới còn bắt được |

⚠️ **`ArchTests` không phải toàn bộ lưới.** Hai project test còn lại
(`PlatformManager.Core.UnitTests`, `PlatformManager.Core.IntegrationTests`) canh phần khác;
`dotnet test` trên solution chạy cả ba.

### 6.2 Hạ tầng kiểm thử — phải hiểu trước khi viết test mới

| Tên | Ở đâu | Vì sao tồn tại |
| --- | --- | --- |
| `PostgresFixture` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/PostgresFixture.cs` | Postgres **thật** trong container, schema dựng từ **chính các file `.sql`** sẽ chạy lên production — không `EnsureCreated()` từ model EF. Thêm file `.sql` mới thì **phải** thêm tên vào `MigrationScripts`, nếu không schema test lệch schema thật đúng cái điều đang muốn tránh |
| `IntegrationTestHostEnvironment` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/IntegrationTestHostEnvironment.cs` | Cấu hình bắt buộc để host thật boot được, đặt bằng **biến môi trường** vì `Program.cs` đọc connection string ngay lúc đăng ký DI. Gom một chỗ: trước đó có 5 bản sao, thêm một options mới là phải sửa cả 5 |
| `CsrfTestClientExtensions` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/CsrfTestClientExtensions.cs` | Giả lập đúng thứ Angular làm tự động: đọc cookie `XSRF-TOKEN` → echo vào header. Đọc **từ cookie**, không từ body JSON — bản trước đọc sai chỗ mà vẫn xanh, trong khi client thật sẽ nhận 403 |
| `AdminApiTestClient` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/AdminApiTestClient.cs` | Ba việc mọi test gọi endpoint quản trị đều phải làm (tạo user đúng role, đăng nhập kèm CSRF, bóc envelope). Chúng là **điều kiện để bắt đầu kiểm**, không phải thứ đang kiểm — mỗi bản sao chỉ là thêm một chỗ để lệch |
| `ProductionHostFactory` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/Production/ProductionHostFactory.cs` | Nhóm test **duy nhất** chạy host ở cấu hình `Production`. Truyền môi trường qua `UseEnvironment` chứ không qua biến môi trường — biến môi trường là trạng thái của cả tiến trình, ghi đè rồi khôi phục là một cửa sổ rò rỉ |
| `SessionTerminationFactory` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/Auth/SessionTerminationFactory.cs` | Kiểm `SecurityStampValidator` — thứ sống ở tầng cookie middleware và chết **âm thầm** nếu ai đó đổi `AddIdentity` → `AddIdentityCore` |
| `RateLimitPartitionFactory` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/RateLimiting/RateLimitPartitionFactory.cs` | Cho test tự chọn IP người gọi — `TestServer` không có kết nối TCP thật nên `RemoteIpAddress` luôn null và mọi request rơi vào cùng một phân vùng |
| `PermissionSeamProbeController` | `src/BE/Tests/PlatformManager.Core.IntegrationTests/Permissions/PermissionSeamProbeController.cs` | Endpoint thăm dò chỉ tồn tại trong assembly test. Trước đó seam test mượn endpoint của module nghiệp vụ, nên khi module bị gỡ thì test **đỏ vì lý do sai** |

📖 `doc/huong_dan/wiki-core/be/04-testing-strategy.md`

---

## 7. KHÔNG áp dụng — khái niệm của VNR cố ý không dùng

Bảng này tồn tại vì *"đã cân nhắc và loại"* khác hẳn *"quên"*. Đọc `00-` → `07-` mà không tìm thấy
những cái tên dưới đây trong `src/BE/` thì **không phải code thiếu** — đó là quyết định.

| Khái niệm ở `trien-khai/00-07` | PlatformManager làm gì thay thế | Vì sao |
| --- | --- | --- |
| `BaseEntity<TId>` generic + `IHasId<TKey>` | `BaseEntity` **non-generic**, `Id` luôn `Guid` | Chưa có ca dùng khoá kiểu khác. Generic hoá trước sẽ phải kéo `TId` qua mọi repository/handler, để đổi lấy một khả năng chưa ai cần |
| `IAuditEntity` · `ISoftDelete` | 5 field nằm thẳng trong `BaseEntity`; interceptor và query filter quét **theo kiểu `BaseEntity`** | Hai interface chỉ có giá trị khi tồn tại entity audit-mà-không-soft-delete (hoặc ngược lại). Hiện không có — nên chúng chỉ là hai điểm phải nhớ implement |
| `AggregateRoot<TId>`, `IHasDomainEvents`, `IDomainEvent`, `DomainEventBase`, `DomainEventInterceptor` | **Không có domain event** | Chưa có ca "một hành động phải kích hoạt phản ứng ở nơi khác" trong Core. Dựng trước là dựng một bus không ai gửi gì vào |
| `CatalogEntityBase<TId>`, `ICatalogEntity`, `IActiveStatus` | Không có base danh mục | Chưa có bảng danh mục nào trong Core |
| `IOptimisticConcurrency`, `ConcurrencyTokenConvention`, `RowVersion`/`xmin` | Hai cơ chế **khác nhau, chọn theo hình dạng lệnh ghi**: `MatrixVersion` (băm cấp tập hợp) cho ma trận ghi-đè-toàn-bộ; `ConcurrencyStamp` của Identity cho sửa **một** user | Token cấp dòng không bảo vệ được lệnh ghi đè cả tập: hai người cùng lưu ma trận thì không dòng nào "xung đột", mà thay đổi của một người vẫn mất |
| `IChangeTracked`, `ChangeLogInterceptor`, `AuditLogBehavior` | Chỉ 5 field audit "ai / lúc nào", **không** lưu old/new value | Chưa có yêu cầu tra lịch sử giá trị. Đường xoá mềm giữ được bản ghi cũ, đủ cho ca đã biết |
| `ValueObject`, `Enumeration<TEnum>`, `TryFromName` vs `FromName` | Không có VO / smart enum nào | Không có kiểu giá trị nào trong Core cần equality theo nội dung, hay một tập giá trị cố định lưu xuống DB |
| `IGenericRepository<TEntity,TKey>`, `ICrudRepository`, `ILegacyQuerySupport` | Repository **chuyên biệt**, mỗi method đúng một nhu cầu | Repository generic đẩy người viết về phía "lấy hết rồi lọc ở C#". Repository hẹp buộc mỗi truy vấn phải nêu tên mục đích của nó |
| `CrudEntityRegistry`, `CrudTypeResolver`, `CrudHandlerBehavior`, `AddCatalogCrud<>()`, `BaseCrudApiController` — toàn bộ **pattern zero-handler** | Không có. Mọi command/query đều tường minh | Pattern đó trả giá bằng một tầng resolve chạy lúc runtime, chỉ hoàn vốn khi có hàng chục bảng danh mục CRUD thuần. Core hiện không có bảng nào như vậy |
| AutoMapper + `CatalogAutoMapperProfile` | Map bằng tay trong handler | Không có cặp kiểu nào chỉ biết lúc runtime — mà đó là lý do duy nhất khiến mapper động đáng giá |
| `ITransactionalCommand`, `ITransactionManager`, `ModuleTransactionBehaviorBase` | Chưa có behavior transaction; handler own `SaveChanges` một lần ở cuối | Cơ chế "opt-in bằng marker interface" có một kiểu hỏng **im lặng**: quên marker = chạy ngoài transaction mà không ai báo. Chỉ nên trả giá đó khi thật sự có lệnh ghi nhiều bảng |
| `LoggingBehavior` (pipeline) | Log ở middleware/Serilog cấp request, không ở pipeline MediatR | Chỉ **2** behavior tồn tại — thêm behavior là thêm một tầng vào mọi request |
| `IApplicationContext` (`CurrentUser`/`Cache`/`Translation`) | Chỉ `ICurrentUser` | Chưa có tầng cache nào (**có chủ đích** — xem `doc/huong_dan/wiki-core/be/11-performance-caching.md`), chưa có i18n phía BE |
| `BaseDbContext`, `SchemaName`, `IBoundedContext`, `AddModuleDbContext<T>` — mỗi module một DbContext | **Một** `PlatformManagerDbContext`, ranh giới bằng **schema Postgres** (`core`/`business`), đảo phụ thuộc bằng `EfConfigurationAssembly` | Một Postgres duy nhất. Nhiều DbContext ở đây chỉ mua thêm nhiều lịch sử migration phải giữ đồng bộ |
| `PlatformConventions` (naming, precision, collation, FK index, cascade, UTC, `ValueGeneratedNever`, unaccent…) | Khai tường minh trong từng `*Configuration`, cộng một vòng lặp soft-delete duy nhất trong `OnModelCreating` | Convention ẩn tiết kiệm khi có hàng trăm entity; ở quy mô này nó chỉ làm model khó đọc bằng mắt |
| `PermissionAction` enum, `CrudActionResolver`, `PermissionPolicyProvider`, `PermissionRequirement` | Permission-key **thô** (`ResourceKeys`) + một filter | Không tách View/Detail/Create/Modify/Delete/Export. Suy quyền từ **tên method** là một tầng ma thuật: đổi tên method là đổi quyền, và không có gì báo |
| JWT, `[Authorize(AuthenticationSchemes = JwtBearer)]` | **Cookie session** của ASP.NET Core Identity | JWT trần **không thu hồi được** — nút "khoá tài khoản" vô tác dụng cho tới khi token hết hạn. Cookie có `SecurityStampValidator`. Dự án chạy **1 process** nên không có lý do xuyên-process nào để đánh đổi |
| `DefaultRouteConvention` (route fallback) | Mọi controller khai `[Route]` tường minh | Fallback che đi việc quên khai route — đúng thứ đáng để lộ ra |
| `PagedResult<T>` với `TotalRow = -1` sentinel | `PagedList<T>` với `TotalCount` thật | Sentinel âm làm `Ceiling(-1/pageSize) = 0` và pager sập, im lặng |
| `IQueryListGrid` vs `IQueryListGridLegacy` — 2 hình dạng envelope | **Một** envelope `IApiResult<T>` cho mọi endpoint, kể cả danh sách | Đó chính là "envelope drift" mà bộ tài liệu gốc ghi lại như một khoản nợ, không phải như một mẫu để theo |
| `VnrExceptionHandler` + `Hosting.CompositionRoot` tách riêng | `GlobalExceptionHandler` + `Program.cs` trong cùng một host mỏng | 1 process, không có gì để tách |
| `Module.{M}.*`, `Module.{M}.Contracts`, `SharedKernel`, `IModuleInstaller` | Hai tầng `Core.*` ↔ `Business.*`; seam `IModuleRegistrar` **đã dựng 2026-09-08** | Nghiệp vụ là **1 khối thống nhất**, không phải N domain độc lập. Neo `file:dòng` của seam: `doc/kien-truc-core-module.md` §`IModuleRegistrar` (file chủ) |
| Mục "Module đầu tiên — mẫu thật" (`06-p5`) | **Không có tương ứng**: module nghiệp vụ duy nhất đã gỡ khỏi solution 2026-08-29 | Mẫu vertical slice thật gần nhất hiện nay là `Permissions/` và `Users/` trong `Core.Application` |

---

## 8. Tra theo tình huống — "tôi đang gặp X, đọc đâu?"

| Tình huống | Đích đến |
| --- | --- |
| FE nói `code` lúc là số lúc là chữ | `ErrorCode` — **giá trị enum = HTTP status**, on-wire = tên member: `src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorCode.cs` |
| Hai lỗi giống nhau trả `status` khác nhau tuỳ đường đi | `ApiErrorIds.StatusForCode` là nguồn duy nhất — tìm xem có chỗ nào gán `Status` bằng tay không |
| Không biết dùng 400 / 404 / 409 / 422 cho một lỗi cụ thể | `doc/huong_dan/quy-uoc/be-api-controller.md` §Error → HTTP, đối chiếu `ErrorCode` |
| Handler ném `DomainException` nhưng client nhận 500 | `ExceptionHandlingBehavior` chỉ dịch được khi `TResponse` là `IApiResult<T>` — request phải là `ICommand<T>`/`IQuery<T>` |
| Validator fail nhưng response không đúng envelope | `ValidationBehavior` **ném**, `GlobalExceptionHandler` mới dịch — kiểm `UseExceptionHandler()` còn được gọi không |
| Lỗi validation hiện **lặp hai lần** trong `fields` | Mỗi validator phải có `ValidationContext` riêng — xem `ValidationBehavior` |
| Lưu ma trận phân quyền lần nào cũng 409 | `MatrixVersion` — token phải tính **chỉ trên dòng đang sống**; đọc bằng `IgnoreQueryFilters()` là trộn cả lịch sử vào token |
| Xoá mềm rồi thêm lại cùng `Code` thì trùng khoá | Unique index phải có `HasFilter("\"IsDeleted\" = false")` — xem `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Configurations/SysMenuConfiguration.cs` |
| Thêm entity mới, dữ liệu đã xoá mềm vẫn lọt ra API | `SoftDeleteQueryFilterTests` — thường là quên đăng ký `EfConfigurationAssembly`, hoặc đảo thứ tự trong `OnModelCreating` |
| `dotnet ef migrations add` sinh `DropTable` | 🛑 Đọc khối cảnh báo đầu `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs` **trước khi chạy**, và `doc/cau-truc-database.md` §5 |
| DB Production trống, không ai đăng nhập được | `SeedCommand` — chạy binary với `--seed`. Seed **không** chạy lúc app khởi động, đó là chủ đích |
| App không khởi động ở Production | Options fail-fast: `BootstrapOptions`, `CorsPolicyOptions`, `SmtpOptions`. Thông điệp lỗi của chúng nói luôn tên biến môi trường cần đặt |
| FE gọi được `GET` nhưng mọi lệnh ghi trả 403 | Hai lớp CSRF cộng dồn: `OriginValidationMiddleware` (header `Origin`) và token antiforgery (`X-XSRF-TOKEN` đọc từ cookie) |
| Đăng nhập trả 400 "UserName không được rỗng" | `LoginUserNameRateLimitMiddleware` quên tua thân request về 0 |
| Có log nhưng không nối được với `traceId` người dùng đưa | `TraceIdLogEnrichmentMiddleware` phải đứng **trước mọi middleware khác** |
| `[RequirePermission]` không chặn gì | Cần **cả hai**: `AddPermissionInfrastructure()` và `options.Filters.Add<RequirePermissionFilter>()`. Thiếu vế thứ hai thì attribute chỉ là metadata |
| Menu đã ẩn mà gọi API vẫn được | Đúng thiết kế: `SysMenuRole` chỉ điều khiển hiển thị. Chặn thật là `RolePermission` + `[RequirePermission]` — xem §3 |
| Admin tự khoá mình, hoặc tự gỡ quyền `SuperAdmin` của mình | `SuperAdminAccountGuard` — thêm luật mới **vào đó**, đừng rải điều kiện vào từng handler |
| Cần chạy một việc lâu ngoài vòng đời request | `IBackgroundJobScheduler`, **không** gọi thẳng `BackgroundJob.Enqueue` (ArchTest chặn) |
| Gửi email không chạy | `AddNotificationInfrastructure(...)` hiện chưa được gọi, và section `Smtp` chưa có trong cấu hình |
| Integration test đỏ vì thiếu bảng/cột | `PostgresFixture.MigrationScripts` — file `.sql` mới phải khai vào đó |
| Muốn thêm cache cho nhanh | `doc/huong_dan/wiki-core/be/11-performance-caching.md` — cache dữ liệu phân quyền là **rủi ro bảo mật**: quyền đã thu hồi vẫn còn hiệu lực |
| Không biết đặt file mới vào project nào | `doc/kien-truc-core-module.md` — bảng "có thật hôm nay → sẽ thành". Nhiều project nhắc trong đó **chưa tồn tại** |

---

Đây là file cuối của series `tham-khao-ngoai/vnr-successor/`. Quay lại
[00-lo-trinh-tong-the.md](../../../tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md) để xem toàn cảnh 7 phase của bộ tham chiếu VNR,
hoặc [README.md](../README.md) để về mục lục `wiki-core/`.
