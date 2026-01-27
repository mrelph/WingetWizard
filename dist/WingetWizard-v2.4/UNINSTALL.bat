@echo off
echo =========================================
echo WingetWizard v2.4 Uninstall
echo =========================================
echo.

echo This will remove WingetWizard from your system.
echo Your configuration files and AI reports will be preserved.
echo.
set /p confirm="Are you sure you want to uninstall? (y/n): "
if /i not "%confirm%"=="y" (
    echo Uninstall cancelled.
    pause
    exit /b 0
)

echo.
echo Detecting installation...

REM Check common installation paths
set "USER_PATH=%USERPROFILE%\AppData\Local\Programs\WingetWizard"
set "SYSTEM_PATH=%ProgramFiles%\WingetWizard"
set "INSTALL_PATH="

if exist "%USER_PATH%\WingetWizard.exe" (
    set "INSTALL_PATH=%USER_PATH%"
    echo Found user installation: %USER_PATH%
) else if exist "%SYSTEM_PATH%\WingetWizard.exe" (
    set "INSTALL_PATH=%SYSTEM_PATH%"
    echo Found system installation: %SYSTEM_PATH%
) else (
    echo WingetWizard installation not found in standard locations.
    echo You may need to manually delete the installation folder.
    pause
    exit /b 1
)

echo.
echo Closing WingetWizard if running...
taskkill /F /IM WingetWizard.exe >nul 2>&1

echo Removing desktop shortcut...
if exist "%USERPROFILE%\Desktop\WingetWizard.lnk" (
    del "%USERPROFILE%\Desktop\WingetWizard.lnk"
)

echo Removing installation files...
if exist "%INSTALL_PATH%" (
    rmdir /S /Q "%INSTALL_PATH%"
    if exist "%INSTALL_PATH%" (
        echo Warning: Some files could not be removed. You may need to delete them manually.
        echo Path: %INSTALL_PATH%
    ) else (
        echo ✅ Installation files removed successfully.
    )
)

echo.
echo Note: Configuration files and AI reports in your Documents folder are preserved.
echo You can manually delete the 'AI_Reports' folder if you no longer need it.
echo.
echo ✅ Uninstall completed!
echo.
pause

