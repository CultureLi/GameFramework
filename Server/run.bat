@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

if not exist "publish\server\GameServer.exe" (
    echo [ERROR] publish\server\GameServer.exe not found.
    echo Run build.bat first.
    pause
    exit /b 1
)

echo ==========================================
echo  GameServer
echo  Default: 0.0.0.0:19000
echo  Press Ctrl+C to stop
echo ==========================================
"%~dp0publish\server\GameServer.exe" %*

echo.
echo Server stopped.
pause
