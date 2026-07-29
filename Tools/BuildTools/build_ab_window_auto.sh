#!/usr/bin/env bash

cd "$(dirname "$0")"

source ./path_define.sh

bash "${WORKSPACE}/../GameConfig/GenerateTool_Binary/gen_bin_client_lazyload.sh" || exit 1

echo "========================================"
echo "Building Windows AssetBundle (Auto Version)"
echo "========================================"
echo "Log File: ${BUILD_LOGFILE}"

"${UNITYEDITOR_PATH}/Unity" \
  -projectPath "${WORKSPACE}" \
  -batchmode \
  -quit \
  -logFile "${BUILD_LOGFILE}" \
  -executeMethod DGame.ReleaseTools.BuildWindowsAB \
  "-CustomArgs:Language=en_US;${WORKSPACE}"

status=$?

if [[ ${status} -ne 0 ]]; then
  echo "Build failed. Check log: ${BUILD_LOGFILE}"
else
  echo "Build finished. Check log: ${BUILD_LOGFILE}"
fi

if [[ -f "${BUILD_LOGFILE}" ]]; then
  cat "${BUILD_LOGFILE}"
fi

exit ${status}
