@echo off
echo ========================================
echo   图片压缩工具 - 1.2 双架构发布
echo ========================================
echo.

cd /d "%~dp0"

echo [1/2] 正在发布 x86 版本...
dotnet publish "ImageCompressor.csproj" -c Release -p:Platform=x86
if %errorlevel% neq 0 (
    echo.
    echo x86 发布失败！
    pause
    exit /b 1
)

echo.
echo [2/2] 正在发布 x64 版本...
dotnet publish "ImageCompressor.csproj" -c Release -p:Platform=x64
if %errorlevel% neq 0 (
    echo.
    echo x64 发布失败！
    pause
    exit /b 1
)

echo.
echo ========================================
echo   发布完成
echo ========================================
echo x86 输出目录:
echo   bin\x86\Release\net48\win-x86\publish\
echo x64 输出目录:
echo   bin\x64\Release\net48\win-x64\publish\
echo.
echo 说明:
echo   1. 32位系统请使用 x86 版本
echo   2. 64位系统优先使用 x64 版本
echo   3. 两个目录都需要整体打包分发
echo.
pause
