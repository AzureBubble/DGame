#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../)"
export LUBAN_DLL="./Tools/LubanTools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export BINARY_DATA_OUTPATH="${WORKSPACE}/GameServer/Configs/Binary/"
export JSON_DATA_OUTPATH="${WORKSPACE}/GameServer/Configs/Json/"
export CODE_OUTPATH="${WORKSPACE}/GameServer/Server/Entity/Generate/Config/"

dotnet "${LUBAN_DLL}" \
    -t server \
    -c cs-bin \
    -d bin \
    -d json \
    --conf "${CONF_ROOT}/luban.conf" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x bin.outputDataDir="${BINARY_DATA_OUTPATH}" \
    -x json.outputDataDir="${JSON_DATA_OUTPATH}" \
    -x outputSaver.bin.cleanUpOutputDir=0 \
    -x outputSaver.json.cleanUpOutputDir=0 \
    -x outputSaver.cs-bin.cleanUpOutputDir=0
