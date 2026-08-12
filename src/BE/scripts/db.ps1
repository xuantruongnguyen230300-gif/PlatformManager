<#
.SYNOPSIS
  Quan ly EF Core migration + database Postgres localhost cho PlatformManager.Api.

.DESCRIPTION
  Wrap cac lenh `dotnet ef` (tu cai tool neu thieu) + tuy chon import luon file CSV
  mau qua API sau khi DB da san sang, de co data that thao tac CRUD ngay.
  Doc connection string tu appsettings.Development.json (Host=localhost;Port=5432;
  Database=platformmanager_dev) - khong can truyen tham so ket noi.

.PARAMETER AddMigration
  Ten migration moi can tao (vd "AddOwnerIndex") - chay `dotnet ef migrations add`.
  Bo qua neu chi muon ap migration hien co vao DB.

.PARAMETER Reset
  XOA SACH database "platformmanager_dev" tren localhost truoc khi tao lai tu dau.
  Co xac nhan truoc khi xoa - hanh dong pha huy du lieu, chi dung khi that su muon
  lam lai tu dau.

.PARAMETER SkipUpdate
  Chi tao migration (khi dung cung -AddMigration), KHONG ap vao database.

.PARAMETER Import
  Sau khi database da update, import file CSV mau (doc/ERD/example_db_ver1.csv)
  qua API POST /api/import/csv de co ngay data that cho CRUD/Dashboard. Neu API
  chua chay, script tu start tam (giu chay sau khi xong de dung tiep Swagger).

.PARAMETER ApiUrl
  Base URL cua API khi dung -Import. Mac dinh http://localhost:5027 (khop
  launchSettings.json).

.EXAMPLE
  ./db.ps1
  Ap toan bo migration hien co vao DB localhost (dung sau khi clone moi hoac pull
  migration moi tu nguoi khac).

.EXAMPLE
  ./db.ps1 -AddMigration AddCriteriaEvidenceOrderIndex
  Tao migration moi tu thay doi entity, roi ap luon vao DB localhost.

.EXAMPLE
  ./db.ps1 -Reset -Import
  Xoa sach DB, tao lai tu dau, roi import 62 dong CSV mau de co data that ngay.
#>
[CmdletBinding()]
param(
    [string]$AddMigration,
    [switch]$Reset,
    [switch]$SkipUpdate,
    [switch]$Import,
    [string]$ApiUrl = "http://localhost:5027"
)

$ErrorActionPreference = "Stop"
$apiProject = Resolve-Path (Join-Path $PSScriptRoot "..\PlatformManager.Api")
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$sampleCsv = Join-Path $repoRoot "doc\ERD\example_db_ver1.csv"

function Test-Postgres {
    $test = Test-NetConnection -ComputerName localhost -Port 5432 -WarningAction SilentlyContinue
    if (-not $test.TcpTestSucceeded) {
        Write-Host "Khong ket noi duoc Postgres tai localhost:5432 - kiem tra service Postgres da chay chua (Get-Service postgresql*)." -ForegroundColor Red
        exit 1
    }
}

function Test-EfTool {
    $installed = dotnet tool list -g | Select-String "dotnet-ef"
    if (-not $installed) {
        Write-Host "Chua co dotnet-ef CLI - cai global tool..." -ForegroundColor Cyan
        dotnet tool install --global dotnet-ef
        if ($LASTEXITCODE -ne 0) { throw "Cai dotnet-ef that bai." }
    }
}

function Test-ApiNotRunning {
    if ($SkipUpdate -and -not $AddMigration -and -not $Reset) { return }
    $proc = Get-Process -Name "PlatformManager.Api" -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "API dang chay (PID $($proc.Id), start luc $($proc.StartTime)) - lenh 'dotnet ef' can build lai project nen se bi khoa file .exe." -ForegroundColor Red
        Write-Host "Dung API lai truoc (dong cua so debug Visual Studio, hoac chay: Stop-Process -Id $($proc.Id) -Force) roi chay lai script nay." -ForegroundColor Red
        exit 1
    }
}

Write-Host "== PlatformManager.Api - DB localhost ==" -ForegroundColor Magenta
Test-Postgres
Test-EfTool
Test-ApiNotRunning

Push-Location $apiProject
try {
    if ($Reset) {
        Write-Host ""
        Write-Host "Sap XOA SACH database 'platformmanager_dev' tren localhost." -ForegroundColor Yellow
        $confirm = Read-Host "Go 'yes' de xac nhan xoa"
        if ($confirm -ne "yes") {
            Write-Host "Da huy." -ForegroundColor Yellow
            exit 0
        }
        dotnet ef database drop --force
        if ($LASTEXITCODE -ne 0) { throw "Xoa database that bai." }
    }

    if ($AddMigration) {
        Write-Host ""
        Write-Host "Tao migration '$AddMigration'..." -ForegroundColor Cyan
        dotnet ef migrations add $AddMigration
        if ($LASTEXITCODE -ne 0) { throw "Tao migration that bai." }
    }

    if (-not $SkipUpdate) {
        Write-Host ""
        Write-Host "Ap migration vao DB localhost (platformmanager_dev)..." -ForegroundColor Cyan
        dotnet ef database update
        if ($LASTEXITCODE -ne 0) { throw "Update database that bai." }
    }

    Write-Host ""
    Write-Host "DB localhost da san sang." -ForegroundColor Green

    if ($Import) {
        $startedHere = $false
        $health = $null
        try { $health = Invoke-RestMethod -Uri "$ApiUrl/api/criteria-groups" -TimeoutSec 3 } catch {}

        if (-not $health) {
            Write-Host ""
            Write-Host "API chua chay o $ApiUrl - tu start tam (giu chay sau khi xong)..." -ForegroundColor Cyan
            Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", $ApiUrl -WorkingDirectory $apiProject -WindowStyle Hidden
            $startedHere = $true
            $ready = $false
            for ($i = 0; $i -lt 30; $i++) {
                Start-Sleep -Seconds 1
                try {
                    $health = Invoke-RestMethod -Uri "$ApiUrl/api/criteria-groups" -TimeoutSec 2
                    $ready = $true
                    break
                } catch {}
            }
            if (-not $ready) { throw "API khong len sau 30s - kiem tra log/port." }
        }

        Write-Host ""
        Write-Host "Import $sampleCsv qua $ApiUrl/api/import/csv..." -ForegroundColor Cyan
        $curlExe = (Get-Command curl.exe -ErrorAction SilentlyContinue).Source
        if (-not $curlExe) { throw "Khong tim thay curl.exe (co san tu Windows 10+) de upload multipart." }
        $result = & $curlExe -s -X POST "$ApiUrl/api/import/csv" -F "file=@$sampleCsv"
        Write-Host $result

        if ($startedHere) {
            Write-Host ""
            Write-Host "API dang chay nen tai $ApiUrl (tu start boi script nay) - vao Swagger $ApiUrl/swagger de CRUD tiep, hoac Stop-Process theo ten 'PlatformManager.Api' khi xong." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host ""
        Write-Host "Chay 'dotnet run' (hoac F5 Visual Studio) trong $apiProject de start API - tu seed danh muc (6 nhom, 62 chi tieu) neu DB rong." -ForegroundColor Yellow
        Write-Host "Sau do CRUD/Import qua Swagger ($ApiUrl/swagger) hoac tu FE." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}
