@echo off
echo ========================================
echo   图片压缩工具 - 2025-06-20版
echo ========================================
echo.

cd /d "%~dp0"

echo 正在编译项目...
dotnet build --configuration Release

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

start "" "bin\Release\net10.0-windows\ImageCompressor.exe"

echo.
echo 程序已启动！
pause
