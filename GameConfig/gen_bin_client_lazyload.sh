#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../)"
export LUBAN_DLL="./Tools/LubanTools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export DATA_OUTPATH="${WORKSPACE}/GameUnity/Assets/BundleAssets/Configs/Binary/"
export CODE_OUTPATH="${WORKSPACE}/GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/"

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
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}"
