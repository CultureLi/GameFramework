@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

echo ==========================================
echo  Building GameServer (Release)
echo ==========================================

dotnet publish GameServer\GameServer.csproj -c Release -o publish\server
if errorlevel 1 goto :failed

echo.
echo ==========================================
echo  Building TestClient (Release)
echo ==========================================
dotnet publish Tools\TestClient\TestClient.csproj -c Release -o publish\testclient
if errorlevel 1 goto :failed

echo.
echo Build succeeded:
echo   publish\server\GameServer.exe
echo   publish\testclient\TestClient.exe
pause
exit /b 0

:failed
echo.
echo Build failed.
pause
exit /b 1
