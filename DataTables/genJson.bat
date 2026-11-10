@echo off
setlocal
pushd "%~dp0"

rem DataTables and Tools\Luban live in the repo root.
rem The Unity project is still under Client, so outputs stay unchanged.
set REPO_ROOT=..
set WORKSPACE=%REPO_ROOT%\Client
set LUBAN_DLL=%REPO_ROOT%\Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\Assets\HotFixScripts\GameMain\Config\Gen ^
    -x outputDataDir=%WORKSPACE%\Assets\AssetBundles\Config ^
    -x pathValidator.rootDir=%WORKSPACE% ^
    -x l10n.textFile.path=%CONF_ROOT%\Datas\#i18n.xlsx ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.languageFieldName=en ^
    --variant i18n.value=cn ^
    --variant i18n.value=en

set GEN_EXIT=%ERRORLEVEL%
popd

rem Unity Tools/GenCfg passes -nopause to avoid hanging on pause.
if /I not "%~1"=="-nopause" pause
exit /b %GEN_EXIT%
