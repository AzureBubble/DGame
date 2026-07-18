#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

source ./path_define.sh

cp -R "${CONF_ROOT}/CustomTemplate/Client/Bin/ConfigSystem.cs" \
   "${WORKSPACE}/GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs"
cp -R "${CONF_ROOT}/CustomTemplate/Client/Bin/ExternalTypeUtil.cs" \
    "${WORKSPACE}/GameUnity/Assets/Scripts/HotFix/GameProto/ExternalTypeUtil.cs"

# 前置：把含 #xxx-xxx sheet 的表拆成独立 # 文件，供 Luban 原生 auto-import 成表
# python "${CONF_ROOT}/Tools/split_sheets.py" --datas "${CONF_ROOT}/Datas"

dotnet "${LUBAN_DLL}" \
    -t client \
    -c cs-bin \
    -d bin \
    -d json \
    --conf "${CONF_ROOT}/luban.conf" \
    --customTemplateDir "${CONF_ROOT}/CustomTemplate/Client/CustomTemplate_Client" \
    -x tableImporter.name=default \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CLIENT_CODE_OUTPATH}" \
    -x bin.outputDataDir="${CLIENT_BIN_DATA_OUTPATH}" \
    -x json.outputDataDir="${CLIENT_JSON_DATA_OUTPATH}" \
    -x outputSaver.bin.cleanUpOutputDir=1 \
    -x outputSaver.json.cleanUpOutputDir=1 \
    -x outputSaver.cs-bin.cleanUpOutputDir=1

# 后置：清理本次生成的临时拆分文件，保持 Datas 干净
# python "${CONF_ROOT}/Tools/split_sheets.py" --datas "${CONF_ROOT}/Datas" --clean
