@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

dotnet run --project "Tools\NavMeshViewer\NavMeshViewer.csproj" -c Debug -- %*
if errorlevel 1 pause
