@echo off
setlocal
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0Installer_Comptes_Allies.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
pause
exit /b %RESULT%
