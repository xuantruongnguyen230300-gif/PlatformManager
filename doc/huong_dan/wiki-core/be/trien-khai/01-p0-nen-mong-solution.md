# P0 — Nền móng solution

> 📍 **Tên project trong file này là của VNR.Successor, không phải PlatformManager.**
> Tra bảng ánh xạ + 4 mục "KHÔNG áp dụng" ở [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md)
> §ĐỌC TRƯỚC. Tóm tắt: `Platform.*`→`Core.*` · `Module.{M}.*`→tầng nghiệp vụ (`Business.*`) ·
> `Processes/`→**1** host · JWT→**cookie session** · per-module DbContext→**1** DbContext chung.

> **Định nghĩa hoàn thành:** `dotnet build` xanh trên một solution có đủ project
> rỗng, `Directory.Build.props` áp dụng được cho mọi project, và một ArchTest
> đầu tiên (kiểm tra hướng phụ thuộc) chạy đỏ đúng lúc cố tình vi phạm.

Phase này **không viết một dòng logic nào**. Toàn bộ giá trị nằm ở chỗ: mọi luật
kiến trúc mà ta muốn giữ suốt đời dự án phải được **cắm vào build** ngay lúc
solution còn rỗng — khi chi phí bằng 0. Sau này chi phí là số lượng file phải sửa.

---

## 1. Tạo solution & project

```bash
# Solution
dotnet new sln -n VNR.Solution         # hoặc PlatformManager

# Platform (core)
dotnet new classlib -o Src/Platform/VNR.Platform.Domain
dotnet new classlib -o Src/Platform/VNR.Platform.Common
dotnet new classlib -o Src/Platform/VNR.Platform.Application
dotnet new classlib -o Src/Platform/VNR.Platform.Persistence

# Hosting
dotnet new classlib -o Src/Hosting/VNR.Hosting.Api
dotnet new classlib -o Src/Hosting/VNR.Hosting.CompositionRoot

# Process (đơn vị chạy thật)
dotnet new web      -o Src/Processes/VNR.Process.Api

# Tests
dotnet new xunit    -o Tests/VNR.ArchTests

dotnet sln add $(find . -name "*.csproj")     # bash
# PowerShell: Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object { dotnet sln add $_.FullName }
```

### Hướng `ProjectReference` — khai đúng ngay từ đầu

| Project | Reference tới |
| --- | --- |
| `Platform.Domain` | *(không gì cả)* |
| `Platform.Common` | *(không gì cả — utility thuần)* |
| `Platform.Application` | `Platform.Domain`, `Platform.Common` |
| `Platform.Persistence` | `Platform.Application`, `Platform.Domain`, `Platform.Common` |
| `Hosting.Api` | `Platform.Application` (**không** Persistence) |
| `Hosting.CompositionRoot` | `Hosting.Api`, `Platform.Persistence`, mọi `Infrastructure.*` |
| `Process.*` | `Hosting.CompositionRoot`, `Module.*.Api`, `Module.*.Infrastructure` |
| `ArchTests` | tất cả (nó cần load assembly để soi) |

> **`Hosting.Api` KHÔNG được reference `Persistence`.** Đây là chỗ rò rỉ phổ biến
> nhất: base controller "chỉ cần `DbContext` một chút thôi" → 6 tháng sau
> controller nào cũng inject `DbContext`. `Hosting.CompositionRoot` mới là nơi
> duy nhất được biết cả hai bên.

---

## 2. `Directory.Build.props` — cấu hình build tập trung

File này nằm ở gốc `src/backend/`, MSBuild tự áp dụng cho **mọi** `.csproj` bên
dưới. Đây là bản thật của VNR.Successor (87 dòng), đã chú thích lý do từng mục:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Bật nullable từ ngày đầu. Bật sau = hàng nghìn warning không ai đọc. -->
    <Nullable>enable</Nullable>

    <!-- Build nhanh hơn khi chỉ đổi phần thân method (không đổi public API) -->
    <ProduceReferenceAssembly>true</ProduceReferenceAssembly>

    <!-- Build lặp lại cho ra byte giống nhau — cần cho cache CI và so sánh artifact -->
    <Deterministic>true</Deterministic>

    <!-- Debug local không cần sinh .exe host → build nhanh hơn đáng kể -->
    <UseAppHost Condition="'$(Configuration)' == 'Debug'">false</UseAppHost>

    <!-- Analyzer chỉ chạy trên CI. Local build nhanh, CI mới là nơi gác cổng. -->
    <RunAnalyzersDuringBuild
        Condition="'$(CI)' != 'true' AND '$(BUILD_NUMBER)' == ''">false</RunAnalyzersDuringBuild>

    <!-- Nullable warning nguy hiểm nhất → coi là LỖI, không phải warning -->
    <WarningsAsErrors>$(WarningsAsErrors);CS8603;CS8604</WarningsAsErrors>
  </PropertyGroup>

  <!-- Analyzer chất lượng code, không đi vào package output -->
  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="…" PrivateAssets="all" />
  </ItemGroup>

  <!-- Roslyn analyzer TỰ VIẾT — chỉ gắn vào Module.*.Application, nơi có luật riêng -->
  <ItemGroup Condition="$(MSBuildProjectName.StartsWith('VNR.Module.'))
                    AND $(MSBuildProjectName.EndsWith('.Application'))">
    <ProjectReference Include="$(MSBuildThisFileDirectory)Tools\VNR.Analyzers\VNR.Analyzers.csproj"
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### Vì sao `CS8603`/`CS8604` là error mà không phải toàn bộ `TreatWarningsAsErrors`

`TreatWarningsAsErrors` bật toàn cục sẽ chặn build vì những warning vô hại
(XML doc thiếu, obsolete của thư viện bên thứ ba) → đội ngũ sẽ tắt nó trong 2
tuần. Chọn **đúng 2 mã** là "trả về null từ method khai không-null"
(`CS8603`) và "truyền null vào tham số không-null" (`CS8604`) — hai mã này gần
như luôn là bug thật, và số lượng đủ nhỏ để không ai muốn tắt.

### `<NoWarn>` cần khai ở project nào

| Mã | Khai ở | Lý do |
| --- | --- | --- |
| `IDE0130` | `Module.*.Application` | Namespace theo **slice** (`…Application.{Entity}`) cố tình khác đường dẫn thư mục (`…/Commands/Create/`) — xem [06-p5](06-p5-module-dau-tien.md) |

---

## 3. `Directory.Packages.props` — central package management

**Khuyến nghị mạnh** dù VNR.Successor chưa dùng. Không có nó, mỗi `.csproj` tự
khai version → 6 tháng sau có 3 version MediatR trong cùng solution.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MediatR" Version="…" />
    <PackageVersion Include="FluentValidation" Version="…" />
    <PackageVersion Include="AutoMapper" Version="…" />
    <PackageVersion Include="Riok.Mapperly" Version="…" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="…" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="…" />
    <PackageVersion Include="NetArchTest.Rules" Version="…" />
  </ItemGroup>
</Project>
```

**2 mapper cùng tồn tại có chủ đích, không phải trùng lặp:** `AutoMapper`
(reflection, runtime `CreateMap`) phục vụ đúng 1 chỗ — `CatalogAutoMapperProfile`
tự dựng mapping từ `CrudEntityRegistry` cho **pattern 1 zero-handler**, nơi cặp
kiểu (Request/Entity/Response) chỉ biết được lúc chạy qua `AddCatalogCrud<>()`,
không thể generate compile-time. `Riok.Mapperly` (source generator, zero
reflection) dùng cho mapper **khai tường minh** trong vertical slice (pattern 2)
khi cặp kiểu đã biết lúc viết code — ví dụ `ApprovalWorkflowMapper` của module
Succession. Chọn sai chiều (Mapperly cho catalog động, AutoMapper cho vertical
slice tĩnh) đều được nhưng phí — Mapperly không sinh được code cho cặp kiểu
chưa biết lúc compile, còn AutoMapper trả thêm chi phí reflection cho chỗ vốn
có thể zero-cost. Xem cách dùng thật của cả 2 ở
[06-p5 §5](06-p5-module-dau-tien.md).

Trong `.csproj` chỉ còn `<PackageReference Include="MediatR" />` — không version.

### Pin Roslyn version

VNR.Successor pin `Microsoft.CodeAnalysis.*` ở `4.8.0`. Lý do: analyzer tự viết
build bằng Roslyn version khác SDK sẽ crash lúc load analyzer với thông báo cực
tối nghĩa. Pin một lần, đừng để NuGet tự nâng.

---

## 4. Global usings theo layer

Mỗi project khai `GlobalUsings.cs` riêng — **không** dùng chung một file cho cả
solution, vì mỗi layer được phép "biết" những thứ khác nhau.

```csharp
// Platform.Domain/GlobalUsings.cs — CỰC NGẮN, đây là dấu hiệu Domain còn sạch
global using System;
global using System.Collections.Generic;

// Platform.Application/GlobalUsings.cs
global using MediatR;
global using VNR.Platform.Application.CQRS;
global using VNR.Platform.Domain.Entities;
// ❌ TUYỆT ĐỐI KHÔNG: global using Microsoft.EntityFrameworkCore;

// Platform.Persistence/GlobalUsings.cs
global using Microsoft.EntityFrameworkCore;
global using VNR.Platform.Persistence.Context;
```

> Nếu một ngày ai đó thêm `global using Microsoft.EntityFrameworkCore;` vào
> `Application/GlobalUsings.cs`, mọi handler đột nhiên gọi được `.Include()` và
> layer rule chết im lặng. ArchTest `T_LAYER_APP_NO_EF` (P6) bắt chính việc này.

---

## 5. Quy ước đặt tên — chốt ở P0, không bàn lại

| Đối tượng | Quy ước | Ví dụ |
| --- | --- | --- |
| Project | `{Prefix}.{Nhóm}.{Tên}` | `VNR.Module.Organization.Application` |
| Namespace project | trùng tên project | `VNR.Platform.Domain.Entities` |
| Namespace file trong slice | `{Prefix}.Module.{M}.Application.{Entity}` — **cố ý khác đường dẫn** | mọi file trong `Application/Org/OrgStructure/**` đều namespace `…Application.OrgStructure` |
| Entity | `{Prefix}_{Tên}` theo module | `Cat_CostCenter`, `Org_Company`, `Hre_Profile`, `Iam_User` |
| Bảng DB | trùng tên entity, schema theo module | `org.Cat_CostCenter` |
| Command | `{Verb}{Entity}Command` | `CreateCatOrgStructureCommand` |
| Query | `Get{Entity}{Cách}Query` | `GetCatOrgStructureByIdQuery` |
| Handler | `{Tên command/query bỏ hậu tố}Handler` | `CreateCatOrgStructureHandler` |
| Request DTO | `{Verb}{Entity}Request` | `CreateCatOrgStructureRequest` |
| Response DTO | `{Entity}Response` | `CatOrgStructureResponse` |
| DTO cho grid | `{Entity}ListItem` | `IamUserListItem` |
| Error descriptor | `{Aggregate}Errors` đặt **cạnh slice** | `Application/Company/CompanyErrors.cs` |
| Hằng độ dài field | `{Entity}FieldLengths` trong `Contracts/Constants` | `OrgFieldLengths.CodeMaxLength` |
| Permission key | `{module}.{entity}` kebab-case | `organization.org-structure` |
| Route | `/api/v{version}/{module-prefix}/[controller]` | `/api/v1/org/CatOrgStructure` |

### Vì sao namespace cố ý khác thư mục

Một vertical slice có ~15 file rải khắp `Commands/Create/`, `Queries/GetById/`,
`Services/`. Nếu namespace bám thư mục thì mỗi file một namespace → mọi file
trong cùng một slice phải `using` lẫn nhau. Gộp về **một namespace theo entity**
làm cả slice tự nhìn thấy nhau, đổi lại phải `<NoWarn>IDE0130</NoWarn>`.

---

## 6. `CLAUDE.md` ở gốc backend — viết gì và KHÔNG viết gì

VNR.Successor có file này dài **49 dòng**, và đó là điểm mạnh chứ không phải
điểm yếu. Cấu trúc:

```markdown
# {Tên} Backend

## Lệnh
dotnet build VNR.Solution.sln
dotnet test Tests/VNR.ArchTests/VNR.ArchTests.csproj   # CI gate — MUST pass before PR

## Module tham chiếu
Src/Modules/Organization/ — chứa cả 2 pattern CRUD, đọc nó trước khi viết module mới.

## Bảng rule file
| Chủ đề | File |
| architecture | doc/huong_dan/quy-uoc/be-architecture.md |
| entity/domain | doc/huong_dan/quy-uoc/be-entity-domain.md |
| … (13 dòng)

## Cảnh báo đang có
Domain events: ⚠️ PLT-003 broken — don't use in production.

## Nguyên tắc
Verify bằng code, KHÔNG tin index/doc.
```

**Không** nhét rule chi tiết vào đây. `CLAUDE.md` là **bảng chỉ đường**; rule
sống trong `doc/huong_dan/quy-uoc/{chủ-đề}.md` với frontmatter `paths:` để tự gắn theo
file đang sửa:

```markdown
---
paths:
  - "**/Commands/**"
  - "**/Queries/**"
  - "**/Application/**"
---
# CQRS & Handler Conventions
```

---

## 7. ArchTest đầu tiên — viết ở P0, không đợi P6

Chỉ cần **một** test, nhưng có nó từ ngày đầu thì mọi vi phạm sau này đỏ ngay
tại commit gây ra, chứ không phải phát hiện sau 200 file.

> **[SỬA LẠI]** Bản trước của mục này dùng `NetArchTest.Rules` (`Types.InAssembly(...)`)
> làm ví dụ — **VNR.ArchTests thật của Successor không dùng thư viện đó** (đã
> kiểm `VNR.ArchTests.csproj`: không có `PackageReference` nào tới
> `NetArchTest.Rules`). Cách thật — và cũng là cách ví dụ dưới đây dùng — là
> **reflection trần + FluentAssertions**, quét thẳng **DLL đã build trong
> `bin/`** chứ không quét source hay `Assembly` đã load sẵn trong process test.
> Chi tiết đầy đủ về vì sao chọn cách này (và cái giá phải trả) ở
> [07-p6 §1](07-p6-archtests-gate.md).

```csharp
public class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotReference_EntityFrameworkCoreOrAspNetCore()
    {
        var domainAssembly = typeof(BaseEntity<>).Assembly;   // Platform.Domain — assembly ĐANG chạy, không cần quét bin/ ở P0

        var bannedPrefixes = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MediatR" };

        var violations = domainAssembly.GetReferencedAssemblies()
            .Where(r => bannedPrefixes.Any(p => r.Name?.StartsWith(p) == true))
            .Select(r => r.Name)
            .ToList();

        violations.Should().BeEmpty(
            because: "Platform.Domain phải zero-dependency — không EF Core, không ASP.NET Core, không MediatR.");
    }
}
```

**Vì sao quét `GetReferencedAssemblies()` của chính assembly đang chạy là đủ ở
P0**, thay vì quét `bin/` như `VNR.ArchTests` thật làm cho 30+ test module sau
này: ở P0 chỉ có 1 assembly cần kiểm (`Platform.Domain`), và nó **đã** được
load sẵn vào process test (test project reference thẳng nó) — không cần
`Directory.GetFiles(..., "*.dll", SearchOption.AllDirectories)` để tìm DLL nằm
ở project khác. Cách quét `bin/` chỉ trở nên cần thiết khi test cần kiểm
**nhiều module** mà bản thân test project không (và không nên) reference trực
tiếp từng cái — xem lý do đầy đủ ở 07-p6 §1.

**Kiểm chứng test có thật sự hoạt động:** cố tình thêm
`using Microsoft.EntityFrameworkCore;` vào một file trong `Platform.Domain`, chạy
test, phải **đỏ**. Gỡ ra, phải **xanh**. Một ArchTest chưa từng đỏ là một
ArchTest chưa được chứng minh.

---

## 8. Checklist rời P0

- [ ] `dotnet build` xanh, 0 warning
- [ ] `Directory.Build.props` áp dụng được (kiểm chứng: `dotnet build -v:n` thấy `Nullable=enable` ở mọi project)
- [ ] Hướng `ProjectReference` khớp bảng §1 — `Hosting.Api` **không** thấy `Persistence`
- [ ] `Directory.Packages.props` bật, không `.csproj` nào còn khai version
- [ ] `GlobalUsings.cs` của `Application` **không** có EF Core
- [ ] 2 ArchTest ở §7 chạy được, và đã **kiểm chứng bằng cách làm nó đỏ**
- [ ] `CLAUDE.md` gốc backend đã có, ≤ 60 dòng, trỏ tới `doc/huong_dan/quy-uoc/`
- [ ] Bảng đặt tên §5 đã chốt và ghi vào `doc/huong_dan/quy-uoc/be-architecture.md`
