#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

source ./path_define.sh

cp -R "${CONF_ROOT}/CustomTemplate/Client/Bin/ConfigSystem.cs" \
   "${WORKSPACE}/GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs"
cp -R "${CONF_ROOT}/CustomTemplate/Client/Bin/ExternalTypeUtil.cs" \
    "${WORKSPACE}/GameUnity/Assets/Scripts/HotFix/GameProto/ExternalTypeUtil.cs"

dotnet "${LUBAN_DLL}" \
    -t client \
    -c cs-bin \
    -d bin \
    --conf "${CONF_ROOT}/luban.conf" \
    --customTemplateDir "${CONF_ROOT}/CustomTemplate/Client/CustomTemplate_Client_LazyLoad" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CLIENT_CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}" \
    -x outputSaver.bin.cleanUpOutputDir=1 \
    -x outputSaver.json.cleanUpOutputDir=1 \
    -x outputSaver.cs-bin.cleanUpOutputDir=1