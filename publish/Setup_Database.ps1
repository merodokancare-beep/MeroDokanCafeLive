# ========================================================
# Mero Dokan Cafe - Database Setup Runner (Pure PowerShell)
# No sqlcmd.exe required - uses built-in .NET SqlClient
# ========================================================
param(
    [switch]$Silent
)

if (-not $Silent) {
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "   Mero Dokan Cafe - Automated Database Setup" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ""
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "MeroDokanCafe_Database_Setup.sql"

if (-not (Test-Path $sqlFile)) {
    if (-not $Silent) {
        Write-Host "[ERROR] Could not find SQL file: $sqlFile" -ForegroundColor Red
        Read-Host "Press Enter to exit..."
    }
    exit 1
}

# Candidate servers to try automatically
$servers = @(".", ".\SQLEXPRESS", "localhost", "(local)", "(localdb)\MSSQLLocalDB")
$connectedServer = $null
$conn = $null

foreach ($srv in $servers) {
    if (-not $Silent) { Write-Host "Testing connection to SQL Server: $srv ..." -NoNewline }
    $testConn = New-Object System.Data.SqlClient.SqlConnection("Server=$srv;Integrated Security=True;Connection Timeout=3;")
    try {
        $testConn.Open()
        if (-not $Silent) { Write-Host " [CONNECTED]" -ForegroundColor Green }
        $connectedServer = $srv
        $conn = $testConn
        break
    }
    catch {
        if (-not $Silent) { Write-Host " [FAILED]" -ForegroundColor DarkGray }
    }
}

if (-not $connectedServer) {
    if ($Silent) {
        # In silent installer mode, exit gracefully so app can auto-probe on launch
        exit 0
    }
    Write-Host ""
    Write-Host "Could not auto-connect to default instances." -ForegroundColor Yellow
    $manualSrv = Read-Host "Enter your SQL Server instance name (e.g. . or .\SQLEXPRESS or COMPUTERNAME\SQLEXPRESS)"
    if ([string]::IsNullOrWhiteSpace($manualSrv)) { $manualSrv = "." }
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=$manualSrv;Integrated Security=True;Connection Timeout=10;")
    try {
        $conn.Open()
        $connectedServer = $manualSrv
    }
    catch {
        Write-Host "[ERROR] Failed to connect to $manualSrv : $($_.Exception.Message)" -ForegroundColor Red
        Read-Host "Press Enter to exit..."
        exit 1
    }
}

Write-Host ""
Write-Host "Executing database setup on '$connectedServer'..." -ForegroundColor Cyan

$content = [System.IO.File]::ReadAllText($sqlFile)
$batches = [System.Text.RegularExpressions.Regex]::Split($content, "(?m)^\s*GO\s*$", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$count = 0
$total = $batches.Count

foreach ($batch in $batches) {
    $sqlText = $batch.Trim()
    if ($sqlText.Length -gt 0) {
        $count++
        try {
            $cmd = $conn.CreateCommand()
            $cmd.CommandTimeout = 120
            $cmd.CommandText = $sqlText
            $null = $cmd.ExecuteNonQuery()
        }
        catch {
            Write-Host "[WARNING on batch $count]: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

$conn.Close()

if (-not $Silent) {
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "   SUCCESS! Database [MeroDokanCafeDB] is ready!" -ForegroundColor Green
    Write-Host "   Default Login: admin / admin" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host ""
    Read-Host "Press Enter to exit..."
}
