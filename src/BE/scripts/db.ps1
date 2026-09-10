<#
.SYNOPSIS
  Ho tro tao migration EF Core + sinh file .sql cho PlatformManager (KHONG tu dong
  ap migration len DB that).

.DESCRIPTION
  Wrap `dotnet ef migrations add`/`dotnet ef migrations script` (tu cai tool neu
  thieu) + tuy chon import file CSV mau qua API sau khi ban DA TU CHAY file .sql
  tren Postgres. Script nay KHONG BAO GIO goi `dotnet ef database update` hay
  `dotnet ef database drop` - theo dung quyet dinh cua nguoi dung "DB rat quan
  trong khong the tuy tien sua doi" (xem doc/ke-hoach-xay-lai-corebase.md).

  Quy trinh dung file .sql sinh ra: tu doc lai noi dung, tu chay bang psql/pgAdmin/
  cong cu ban chon.

  -- Migration thuoc HOST, Core ship .sql (nguoi dung chot 2026-09-04) --------------
  File .cs migration + ModelSnapshot song trong PROJECT HOST (PlatformManager.Api),
  KHONG con trong project Core nao. Ly do: ModelSnapshot la file TRANG THAI dung
  chung - de o Core thi du an thu hai them bang nghiep vu dau tien se GHI DE snapshot
  cua Core, va lan Core ship ban va la lan merge hong. Moi du an giu Migrations/ +
  ModelSnapshot RIENG; Core chi ship baseline .sql lam artifact schema.

  Vi vay --project cua MOI lenh `dotnet ef` duoi day tro toi $apiProject. Cuong che
  bang may: PlatformManager.ArchTests/MigrationsLocationTests.cs - file .cs migration
  nam duoi src/BE/Core la test do.

  Thu muc .sql thi O LAI Core (Core/PlatformManager.Core.Persistence/Migrations/sql/,
  chuyen tu Core.Infrastructure 2026-09-10) - artifact Corebase ship, integration test doc thang tu do.

.PARAMETER AddMigration
  Ten migration moi can tao (vd "AddOwnerIndex") - chay `dotnet ef migrations add`
  (chi sinh class C#, KHONG dung DB that).

.PARAMETER ScriptOutput
  Duong dan file .sql muon sinh ra (chay `dotnet ef migrations script --idempotent`).
  Mac dinh in ra man hinh huong dan, khong tu sinh file neu khong truyen tham so nay.

.PARAMETER Import
  DA CHET - DUNG DUNG (do lai 2026-09-04). Ba thu no can deu khong con ton tai: file
  doc/ERD/example_db_ver1.csv (ca thu muc doc/ERD da xoa 2026-08-23), endpoint
  GET /api/criteria-groups va POST /api/import/csv (grep toan src/BE: 0 hit - chung
  thuoc module DtiWeekly da go khoi solution).

  Giu lai chu KHONG xoa vi day la khuon cua duong import se dung lai khi module
  nghiep vu quay ve. Chay bay gio chi nhan mot loi "API chua chay" gay hieu nham.

.PARAMETER ApiUrl
  Base URL cua API khi dung -Import. Mac dinh http://localhost:5027 (khop
  launchSettings.json).

.EXAMPLE
  ./db.ps1 -AddMigration AddCriteriaEvidenceOrderIndex
  Chi tao migration moi (class C#) tu thay doi entity - KHONG dung DB.

.EXAMPLE
  ./db.ps1 -ScriptOutput Core/PlatformManager.Core.Persistence/Migrations/sql/0002_add_owner_index.sql
  Sinh file .sql moi tu cac migration chua duoc ap - tu doc lai, tu chay tay tren
  Postgres, KHONG co buoc nao trong script nay dung vao DB that. Duong dan vi du tro
  vao thu muc sql/ cua Core: do la cho dat dung cua artifact schema.

.EXAMPLE
  ./db.ps1 -Import
  Da chet - xem .PARAMETER Import.
#>
[CmdletBinding()]
param(
    [string]$AddMigration,
    [string]$ScriptOutput,
    [switch]$Import,
    [string]$ApiUrl = "http://localhost:5027"
)

$ErrorActionPreference = "Stop"
$beRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
# DbContext song o Core.Persistence, nhung MIGRATION thi thuoc HOST (chot 2026-09-04) - nen
# --project VA --startup-project deu tro toi $apiProject. Truoc do --project tro toi
# "Core/PlatformManager.Core.Infrastructure"; de nguyen the thi `dotnet ef` ghi Migrations/ +
# ModelSnapshot vao Core - dung cai vua go bo. Xem .DESCRIPTION.
#
# --project PHAI khop assembly khai trong MigrationsAssembly
# (PlatformManager.Api/PlatformManagerDbContextFactory.cs); lech nhau thi EF bao thang
# "Your target project ... doesn't match your migrations assembly".
#
# Duong dan tinh tu $beRoot (src/BE/) - PlatformManager.Api KHONG nam trong Core/.
$apiProject = "PlatformManager.Api"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$sampleCsv = Join-Path $repoRoot "doc\ERD\example_db_ver1.csv"

function Test-EfTool {
    $installed = dotnet tool list -g | Select-String "dotnet-ef"
    if (-not $installed) {
        Write-Host "Chua co dotnet-ef CLI - cai global tool..." -ForegroundColor Cyan
        dotnet tool install --global dotnet-ef
        if ($LASTEXITCODE -ne 0) { throw "Cai dotnet-ef that bai." }
    }
}

Write-Host "== PlatformManager - EF Core migration (KHONG dung DB that) ==" -ForegroundColor Magenta
Test-EfTool

Push-Location $beRoot
try {
    if ($AddMigration) {
        Write-Host ""
        Write-Host "Tao migration '$AddMigration' (chi sinh class C#, khong dung DB)..." -ForegroundColor Cyan
        dotnet ef migrations add $AddMigration --project $apiProject --startup-project $apiProject --output-dir Persistence/Migrations
        if ($LASTEXITCODE -ne 0) { throw "Tao migration that bai." }
        Write-Host "Da sinh migration - doc lai class vua tao truoc khi tin, xem PlatformManager.Api/Persistence/Migrations/." -ForegroundColor Green
    }

    if ($ScriptOutput) {
        Write-Host ""
        Write-Host "Sinh file .sql idempotent tai '$ScriptOutput' (chi xuat file, khong dung DB)..." -ForegroundColor Cyan
        dotnet ef migrations script --idempotent --project $apiProject --startup-project $apiProject -o $ScriptOutput
        if ($LASTEXITCODE -ne 0) { throw "Sinh script that bai." }
        Write-Host ""
        Write-Host "Da sinh '$ScriptOutput' - TU DOC LAI, TU CHAY TAY tren Postgres (psql/pgAdmin). Script nay KHONG tu chay len DB." -ForegroundColor Yellow
    }

    if (-not $AddMigration -and -not $ScriptOutput -and -not $Import) {
        Write-Host ""
        Write-Host "Khong truyen tham so nao - xem -AddMigration / -ScriptOutput / -Import. Vi du: ./db.ps1 -AddMigration TenMigration" -ForegroundColor Yellow
    }

    if ($Import) {
        Write-Host ""
        Write-Warning "-Import DA CHET (do lai 2026-09-04): doc/ERD/example_db_ver1.csv va 2 endpoint /api/criteria-groups, /api/import/csv deu khong con ton tai. Xem .PARAMETER Import trong file nay."
        $health = $null
        try { $health = Invoke-RestMethod -Uri "$ApiUrl/api/criteria-groups" -TimeoutSec 3 } catch {}
        if (-not $health) {
            throw "API chua chay tai $ApiUrl (hoac schema chua duoc ap) - tu chay 'dotnet run' trong $apiProject SAU KHI da tu chay file .sql migration tren Postgres, roi chay lai -Import."
        }

        Write-Host "Import $sampleCsv qua $ApiUrl/api/import/csv..." -ForegroundColor Cyan
        $curlExe = (Get-Command curl.exe -ErrorAction SilentlyContinue).Source
        if (-not $curlExe) { throw "Khong tim thay curl.exe (co san tu Windows 10+) de upload multipart." }
        $result = & $curlExe -s -X POST "$ApiUrl/api/import/csv" -F "file=@$sampleCsv"
        Write-Host $result
    }
}
finally {
    Pop-Location
}
