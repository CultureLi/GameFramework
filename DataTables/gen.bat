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
    -c cs-bin cs-lazyload-bin ^
    -d bin bin-offsetlength ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\Assets\HotFixScripts\GameMain\Config\Gen ^
    -x outputDataDir=%WORKSPACE%\Assets\StreamingAssets\Config ^
    -x pathValidator.rootDir=%WORKSPACE% ^
    --genOffsetTables ItemSummaryCfg ResourceSummaryCfg ^
    --i18nTable I18nCfg ^
    --genCodeExclude I18nCfg LanguageCfg

set GEN_EXIT=%ERRORLEVEL%
popd

rem Unity Tools/GenCfg passes -nopause to avoid hanging on pause.
if /I not "%~1"=="-nopause" pause
exit /b %GEN_EXIT%
