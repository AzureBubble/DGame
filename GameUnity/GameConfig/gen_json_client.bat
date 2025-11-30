Cd /d %~dp0
echo %CD%

set WORKSPACE=../
set LUBAN_DLL=%WORKSPACE%\Tools\LubanTools\Luban\Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/Assets/ABAssets/Configs/Json/
set CODE_OUTPATH=%WORKSPACE%/Assets/Scripts/HotFix/GameProto/LubanConfig/

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Json\ConfigSystem.cs" "%WORKSPACE%\Assets\Scripts\HotFix\GameProto\ConfigSystem.cs"
xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Json\ExternalTypeUtil.cs" "%WORKSPACE%\Assets\Scripts\HotFix\GameProto\ExternalTypeUtil.cs"

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json2 ^
    --conf %CONF_ROOT%\luban.conf ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

