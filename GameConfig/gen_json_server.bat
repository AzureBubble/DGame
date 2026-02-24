Cd /d %~dp0
echo %CD%

set WORKSPACE=../
set LUBAN_DLL=./Tools/LubanTools/Luban/Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/GameServer/Configs/Json/
set CODE_OUTPATH=%WORKSPACE%/GameServer/Server/Entity/Generate/Config/

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Server\Json\ConfigSystem.cs" "%WORKSPACE%\GameServer\Server\Entity\Generate\ConfigSystem.cs"

dotnet %LUBAN_DLL% ^
    -t server^
    -c cs-simple-json ^
    -d json2 ^
    --conf %CONF_ROOT%\luban.conf ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

