@echo off
echo ========================================
echo   图片压缩工具 - 1.2
echo ========================================
echo.

cd /d "%~dp0"

echo 正在编译项目...
dotnet build --configuration Release -p:Platform=x64

if %errorlevel% neq 0 (
    echo.
    echo 编译失败！
    pause
    exit /b 1
)

echo.
echo ========================================
echo   启动程序...
echo ========================================
echo.

start "" "bin\Release\net48\ImageCompressor.exe"

echo.
echo 程序已启动！
pause
