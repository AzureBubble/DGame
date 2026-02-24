Cd /d %~dp0
echo %CD%

set WORKSPACE=../
set LUBAN_DLL=./Tools/LubanTools/Luban/Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/GameUnity/Assets/BundleAssets/Configs/Binary/
set CODE_OUTPATH=%WORKSPACE%/GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Client\Bin\ConfigSystem.cs" "%WORKSPACE%\GameUnity\Assets\Scripts\HotFix\GameProto\ConfigSystem.cs"
xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Client\Bin\ExternalTypeUtil.cs" "%WORKSPACE%\GameUnity\Assets\Scripts\HotFix\GameProto\ExternalTypeUtil.cs"

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin^
    --conf %CONF_ROOT%\luban.conf ^
    --customTemplateDir %CONF_ROOT%\CustomTemplate\Client\CustomTemplate_Client_LazyLoad ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

