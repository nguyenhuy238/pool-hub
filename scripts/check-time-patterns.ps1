$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$checks = @(
    @{
        Name = "Do not use local server time in backend"
        Pattern = "DateTime\.Now|DateTime\.Today|DateTimeOffset\.Now"
        Paths = @("PoolHub.API", "PoolHub.Services", "PoolHub.Core", "PoolHub.Infrastructure")
    },
    @{
        Name = "Do not manually add Vietnam timezone offset"
        Pattern = "AddHours\(7\)"
        Paths = @("PoolHub.API", "PoolHub.Services", "PoolHub.Core", "PoolHub.Infrastructure", "frontend/src")
    },
    @{
        Name = "Do not use inclusive end-of-day timestamps"
        Pattern = "23:59:59|AddDays\(1\)\.AddTicks\(-1\)"
        Paths = @("PoolHub.API", "PoolHub.Services", "PoolHub.Core", "PoolHub.Infrastructure", "frontend/src")
    },
    @{
        Name = "Do not filter UTC business timestamps with .Date"
        Pattern = "StartTimeUtc\.Date|EndTimeUtc\.Date|StartedAtUtc\.Date|EndedAtUtc\.Date|IssuedAtUtc.*\.Date|PaidAtUtc.*\.Date|CreatedAtUtc.*\.Date"
        Paths = @("PoolHub.API", "PoolHub.Services", "PoolHub.Core")
    }
)

$failed = $false
Push-Location $root
try {
    foreach ($check in $checks) {
        $matches = & rg -n -S --glob "!**/Migrations/**" --glob "!**/Seed/**" --glob "!**/bin/**" --glob "!**/obj/**" --glob "!**/.next/**" $check.Pattern $check.Paths 2>$null
        if ($LASTEXITCODE -eq 0) {
            $failed = $true
            Write-Host "[$($check.Name)]" -ForegroundColor Red
            $matches | ForEach-Object { Write-Host $_ }
            Write-Host ""
        }
    }
}
finally {
    Pop-Location
}

if ($failed) {
    Write-Error "Unsafe time-handling patterns were found. Use IClock, BusinessTime, and frontend dateTime helpers."
}

Write-Host "Time pattern guard passed."
