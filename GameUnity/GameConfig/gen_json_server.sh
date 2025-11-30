#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../)"
export LUBAN_DLL="${WORKSPACE}/Tools/LubanTools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export DATA_OUTPATH="${WORKSPACE}/Assets/ABAssets/Configs/Json/"
export CODE_OUTPATH="${WORKSPACE}/Assets/Scripts/HotFix/GameProto/LubanConfig/"

dotnet "${LUBAN_DLL}" \
    -t server \
    -c cs-simple-json \
    -d json2 \
    --conf "${CONF_ROOT}/luban.conf" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}"
