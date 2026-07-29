cd /d %~dp0

call path_define.bat
set "UNITY_PROJECT_PATH=%WORKSPACE%"

set "AUTO_CONTINUE=1"
call "%UNITY_PROJECT_PATH%\..\GameConfig\GenerateTool_Binary\gen_bin_client_lazyload.bat"
if errorlevel 1 (
    set "BUILD_EXIT_CODE=%ERRORLEVEL%"
    echo Table generation failed.
    goto BUILD_FINISHED
)

echo Log File: %BUILD_LOGFILE%

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_unity_with_log.ps1" -UnityEditorPath "%UNITYEDITOR_PATH%" -ProjectPath "%UNITY_PROJECT_PATH%" -LogFile "%BUILD_LOGFILE%" -ExecuteMethod "DGame.ReleaseTools.AutoBuildAndroid"

set "BUILD_EXIT_CODE=%ERRORLEVEL%"

:BUILD_FINISHED
if not "%BUILD_EXIT_CODE%"=="0" (
    echo Build failed. Check log: %BUILD_LOGFILE%
) else (
    echo Build finished. Check log: %BUILD_LOGFILE%
)

pause
exit /b %BUILD_EXIT_CODE%
