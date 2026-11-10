#!/usr/bin/env bash
# 与 gen.bat 等价：配置表在仓库根目录的 DataTables，Luban 工具在 Tools/Luban，
# Unity 工程在 Client，代码/数据输出位置与 Windows 版本保持一致。
set -euo pipefail

DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$DIR/.."
WORKSPACE="$REPO_ROOT/Client"
LUBAN_DLL="$REPO_ROOT/Tools/Luban/Luban.dll"
CONF_ROOT="$DIR"

dotnet "$LUBAN_DLL" \
    -t client \
    -c cs-bin cs-lazyload-bin \
    -d bin bin-offsetlength \
    --conf "$CONF_ROOT/luban.conf" \
    -x "outputCodeDir=$WORKSPACE/Assets/HotFixScripts/GameMain/Config/Gen" \
    -x "outputDataDir=$WORKSPACE/Assets/StreamingAssets/Config" \
    -x "pathValidator.rootDir=$WORKSPACE" \
    --genOffsetTables ItemSummaryCfg ResourceSummaryCfg \
    --i18nTable I18nCfg \
    --genCodeExclude I18nCfg LanguageCfg
