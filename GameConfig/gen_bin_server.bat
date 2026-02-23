Cd /d %~dp0
echo %CD%

set WORKSPACE=../
set LUBAN_DLL=./Tools/LubanTools/Luban/Luban.dll
set CONF_ROOT=.
set BINARY_DATA_OUTPATH=%WORKSPACE%/GameServer/Configs/Binary/
set JSON_DATA_OUTPATH=%WORKSPACE%/GameServer/Configs/Json/
set CODE_OUTPATH=%WORKSPACE%/GameServer/Server/Entity/Generate/Config/

dotnet %LUBAN_DLL% ^
    -t server^
    -c cs-bin ^
    -d bin^
    -d json^
    --conf %CONF_ROOT%\luban.conf ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x bin.outputDataDir=%BINARY_DATA_OUTPATH% ^
    -x json.outputDataDir=%JSON_DATA_OUTPATH% ^
    -x outputSaver.bin.cleanUpOutputDir=0 ^
    -x outputSaver.json.cleanUpOutputDir=0 ^
    -x outputSaver.cs-bin.cleanUpOutputDir=0
pause

