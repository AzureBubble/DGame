Cd /d %~dp0
echo %CD%

set WORKSPACE=../
set LUBAN_DLL=./Tools/LubanTools/Luban/Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/GameUnity/Assets/BundleAssets/Configs/Bytes/
set CODE_OUTPATH=%WORKSPACE%/GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/

dotnet %LUBAN_DLL% ^
    -t server^
    -c cs-bin ^
    -d bin^
    --conf %CONF_ROOT%\luban.conf ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

