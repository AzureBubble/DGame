@echo off
cd /d %~dp0
call path_define.bat
set "UNITY_PROJECT_PATH=%WORKSPACE%"

@REM set VERSION=1.0

set "VERSION="
echo ========================================
echo Please input Android AssetBundle version:
set /p VERSION=Version^> 

if "%VERSION%"=="" (
    echo Version cannot be empty.
    pause
    exit /b 1
)

set "AUTO_CONTINUE=1"
call "%UNITY_PROJECT_PATH%\..\GameConfig\GenerateTool_Binary\gen_bin_client_lazyload.bat"
if errorlevel 1 (
    set "BUILD_EXIT_CODE=%ERRORLEVEL%"
    echo Table generation failed.
    goto BUILD_FINISHED
)

echo ========================================
echo Building Android AssetBundle (Manual Version: %VERSION%)
echo ========================================
echo Log File: %BUILD_LOGFILE%

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_unity_with_log.ps1" -UnityEditorPath "%UNITYEDITOR_PATH%" -ProjectPath "%UNITY_PROJECT_PATH%" -LogFile "%BUILD_LOGFILE%" -ExecuteMethod "DGame.ReleaseTools.BuildAndroidWithVersion" -Version "%VERSION%"

set "BUILD_EXIT_CODE=%ERRORLEVEL%"

:BUILD_FINISHED
if not "%BUILD_EXIT_CODE%"=="0" (
    echo Build failed. Check log: %BUILD_LOGFILE%
) else (
    echo Build finished. Check log: %BUILD_LOGFILE%
)

pause
exit /b %BUILD_EXIT_CODE%
