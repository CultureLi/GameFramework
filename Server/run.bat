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
echo  Config:  %~dp0..\Client\Assets\StreamingAssets\Config
echo  Scene:   OpenWorld
echo  MapData: %~dp0MapData\OpenWorld
echo  NavMesh: %~dp0MapData\OpenWorld\navmesh.rnav
echo  Press Ctrl+C to stop
echo ==========================================
"%~dp0publish\server\GameServer.exe" --config "%~dp0..\Client\Assets\StreamingAssets\Config" --scene OpenWorld %*

echo.
echo Server stopped.
pause
