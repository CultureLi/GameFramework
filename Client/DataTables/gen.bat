set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin cs-lazyload-bin ^
    -d bin bin-offsetlength ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\Assets\HotFixScripts\GameMain\Config\Gen ^
    -x outputDataDir=%WORKSPACE%\Assets\StreamingAssets\Config ^
    --genOffsetTables ItemSummary ResourceSummary ^
    -x pathValidator.rootDir=%WORKSPACE%\Client ^
    --i18nTable i18n