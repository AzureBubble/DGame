Cd /d %~dp0
echo %CD%

set WORKSPACE=../
set LUBAN_DLL=%WORKSPACE%\Tools\LubanTools\Luban\Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/Assets/ABAssets/Configs/Json/
set CODE_OUTPATH=%WORKSPACE%/Assets/Scripts/HotFix/GameProto/LubanConfig/

dotnet %LUBAN_DLL% ^
    -t server^
    -c cs-simple-json ^
    -d json2 ^
    --conf %CONF_ROOT%\luban.conf ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

