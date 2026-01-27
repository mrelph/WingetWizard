@echo off
echo =========================================
echo WingetWizard MSI Installer Builder
echo =========================================
echo.

REM Check if WiX Toolset is installed
where candle.exe >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: WiX Toolset not found!
    echo.
    echo Please install WiX Toolset from:
    echo https://wixtoolset.org/releases/
    echo.
    echo After installation, add WiX to your PATH or run this script
    echo from a Visual Studio Developer Command Prompt.
    echo.
    pause
    exit /b 1
)

echo [1/4] Building application...
call dotnet publish -c Release --self-contained true -r win-x64 -p:PublishSingleFile=true -o publish

if %errorLevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo [2/4] Compiling WiX installer...
set PUBLISH_DIR=%CD%\publish\
candle.exe -dPublishDir="%PUBLISH_DIR%" installer.wxs -o installer.wixobj

if %errorLevel% neq 0 (
    echo ERROR: WiX compilation failed!
    pause
    exit /b 1
)

echo.
echo [3/4] Linking installer...
light.exe installer.wixobj -ext WixUIExtension -o dist\WingetWizard-v2.4.msi

if %errorLevel% neq 0 (
    echo ERROR: WiX linking failed!
    pause
    exit /b 1
)

echo.
echo [4/4] Cleaning up...
del installer.wixobj 2>nul
del installer.wixpdb 2>nul

echo.
echo =========================================
echo ✅ Installer built successfully!
echo =========================================
echo.
echo Installer location: dist\WingetWizard-v2.4.msi
echo.
echo To test the installer:
echo   1. Right-click the MSI file
echo   2. Select "Install"
echo   3. Follow the installation wizard
echo.
pause





