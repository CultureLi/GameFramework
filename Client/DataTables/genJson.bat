set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\Assets\HotFixScripts\GameMain\Config\Gen ^
    -x outputDataDir=%WORKSPACE%\Assets\BundleRes\Config ^
    -x pathValidator.rootDir=%WORKSPACE%\Client ^
    -x l10n.textFile.path=%CONF_ROOT%\Datas\#i18n.xlsx ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.languageFieldName=en ^
    --variant i18n.value=cn ^
    --variant i18n.value=en
pause