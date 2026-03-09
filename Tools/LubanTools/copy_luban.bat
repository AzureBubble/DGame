@echo off
chcp 65001 >nul
setlocal

call "%~dp0path_define.bat"

echo 正在复制 Luban 文件夹...
echo 从: %SOURCE%
echo 到: %DEST%
echo.

if not exist "%SOURCE%" (
    echo 错误: 源文件夹 "%SOURCE%" 不存在！
    pause
    exit /b 1
)

if not exist "%DEST_PARENT%" (
    echo 创建目标目录: %DEST_PARENT%
    mkdir "%DEST_PARENT%"
)

robocopy "%SOURCE%" "%DEST%" /E /NFL /NDL /NJH /NJS

if %ERRORLEVEL% LEQ 7 (
    echo.
    echo 复制完成！
) else (
    echo.
    echo 复制过程中出现错误，错误代码: %ERRORLEVEL%
)

pause
