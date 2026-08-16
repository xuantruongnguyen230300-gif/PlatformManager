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
  cong cu ban chon. Rieng migration InitialCreate da co san tay-patch index unique
  loc theo ngay cho CriteriaAssessments trong doc/ERD/migrations/0003_corebase_v2.sql
  - neu tao migration MOI sau nay co dung lai loai index tuong tu, nho vá tay lai.

.PARAMETER AddMigration
  Ten migration moi can tao (vd "AddOwnerIndex") - chay `dotnet ef migrations add`
  (chi sinh class C#, KHONG dung DB that).

.PARAMETER ScriptOutput
  Duong dan file .sql muon sinh ra (chay `dotnet ef migrations script --idempotent`).
  Mac dinh in ra man hinh huong dan, khong tu sinh file neu khong truyen tham so nay.

.PARAMETER Import
  Import file CSV mau (doc/ERD/example_db_ver1.csv) qua API POST /api/import/csv -
  CHI dung SAU KHI ban da tu chay file .sql migration tren Postgres va API dang
  chay duoc voi schema da co san.

.PARAMETER ApiUrl
  Base URL cua API khi dung -Import. Mac dinh http://localhost:5027 (khop
  launchSettings.json).

.EXAMPLE
  ./db.ps1 -AddMigration AddCriteriaEvidenceOrderIndex
  Chi tao migration moi (class C#) tu thay doi entity - KHONG dung DB.

.EXAMPLE
  ./db.ps1 -ScriptOutput ../../../doc/ERD/migrations/0004_add_owner_index.sql
  Sinh file .sql moi tu cac migration chua duoc ap - tu doc lai, tu chay tay tren
  Postgres, KHONG co buoc nao trong script nay dung vao DB that.

.EXAMPLE
  ./db.ps1 -Import
  (Sau khi da tu chay file .sql va API dang chay) import 62 dong CSV mau qua API.
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
# DbContext song o Core.Infrastructure (Modular Monolith - xem doc/kien-truc-core-module.md) -
# dung project nay cho --project, KHONG phai Modules.DtiWeekly.Infrastructure. Duong dan tinh
# tu $beRoot (src/BE/) - Core/Modules la thu muc vat ly nhom project, PlatformManager.Api KHONG
# nam trong Core/ hay Modules/ (khong phai "thu vien").
$infraProject = "Core/PlatformManager.Core.Infrastructure"
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
        dotnet ef migrations add $AddMigration --project $infraProject --startup-project $apiProject --output-dir Persistence/Migrations
        if ($LASTEXITCODE -ne 0) { throw "Tao migration that bai." }
        Write-Host "Da sinh migration - doc lai class vua tao truoc khi tin, xem PlatformManager.Core.Infrastructure/Persistence/Migrations/." -ForegroundColor Green
    }

    if ($ScriptOutput) {
        Write-Host ""
        Write-Host "Sinh file .sql idempotent tai '$ScriptOutput' (chi xuat file, khong dung DB)..." -ForegroundColor Cyan
        dotnet ef migrations script --idempotent --project $infraProject --startup-project $apiProject -o $ScriptOutput
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
