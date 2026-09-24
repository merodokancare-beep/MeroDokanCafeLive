@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo   Mero Dokan Cafe - Automated Database Setup
echo ========================================================
echo.

set "SCRIPT_FILE=%~dp0MeroDokanCafe_Database_Setup.sql"

if not exist "!SCRIPT_FILE!" (
    echo [ERROR] SQL script file not found: "!SCRIPT_FILE!"
    pause
    exit /b 1
)

:: 1. Search for sqlcmd in PATH
set "SQLCMD_EXE="
where sqlcmd.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    set "SQLCMD_EXE=sqlcmd.exe"
)

:: 2. Check known paths (120, 130, 140, 150, 160, Client SDK)
if "!SQLCMD_EXE!"=="" (
    for %%P in (
        "C:\Program Files\Microsoft SQL Server\120\Tools\Binn\sqlcmd.exe"
        "C:\Program Files (x86)\Microsoft SQL Server\120\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe"
        "C:\Program Files (x86)\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\130\Tools\Binn\sqlcmd.exe"
        "C:\Program Files (x86)\Microsoft SQL Server\130\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe"
        "C:\Program Files (x86)\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe"
        "C:\Program Files (x86)\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe"
        "C:\Program Files (x86)\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe"
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\110\Tools\Binn\sqlcmd.exe"
    ) do (
        if exist %%P (
            set "SQLCMD_EXE=%%~P"
            goto :found
        )
    )
)

:found
if "!SQLCMD_EXE!"=="" (
    echo [INFO] sqlcmd.exe not found. Running using PowerShell engine...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$sql = Get-Content -Raw '!SCRIPT_FILE!'; $batches = $sql -split '(?m)^\s*GO\s*$'; $conn = New-Object System.Data.SqlClient.SqlConnection('Server=.;Integrated Security=True;'); try { $conn.Open() } catch { $conn = New-Object System.Data.SqlClient.SqlConnection('Server=.\SQLEXPRESS;Integrated Security=True;'); $conn.Open() }; foreach ($b in $batches) { if ($b.Trim().Length -gt 0) { $cmd = $conn.CreateCommand(); $cmd.CommandText = $b; $cmd.ExecuteNonQuery() | Out-Null } }; $conn.Close(); Write-Host 'Database setup completed successfully!' -ForegroundColor Green"
    goto :done
)

echo Found sqlcmd at: "!SQLCMD_EXE!"
echo.
echo Attempting connection to Default instance (.) ...
"!SQLCMD_EXE!" -S . -E -i "!SCRIPT_FILE!" -b
if %ERRORLEVEL% EQU 0 goto :done

echo.
echo Attempting connection to SQL Express (.\SQLEXPRESS) ...
"!SQLCMD_EXE!" -S .\SQLEXPRESS -E -i "!SCRIPT_FILE!" -b
if %ERRORLEVEL% EQU 0 goto :done

echo.
echo [ERROR] Could not connect to SQL Server on '.' or '.\SQLEXPRESS'.
echo Please check if SQL Server service is running.
pause
exit /b 1

:done
echo.
echo ========================================================
echo   SUCCESS! Database is ready to use with Mero Dokan Cafe.
echo ========================================================
pause
