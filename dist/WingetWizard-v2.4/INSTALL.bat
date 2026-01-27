@echo off
echo =========================================
echo WingetWizard v2.4 Installation
echo =========================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with administrator privileges...
) else (
    echo This installer can run without administrator privileges.
    echo For system-wide installation, run as administrator.
)

echo.
echo Choose installation type:
echo 1. Current user only (recommended)
echo 2. System-wide installation (requires admin)
echo.
set /p choice="Enter your choice (1 or 2): "

if "%choice%"=="1" (
    set "INSTALL_PATH=%USERPROFILE%\AppData\Local\Programs\WingetWizard"
    echo Installing to user directory: %INSTALL_PATH%
) else if "%choice%"=="2" (
    set "INSTALL_PATH=%ProgramFiles%\WingetWizard"
    echo Installing to system directory: %INSTALL_PATH%
) else (
    echo Invalid choice. Installing to user directory.
    set "INSTALL_PATH=%USERPROFILE%\AppData\Local\Programs\WingetWizard"
)

echo.
echo Creating installation directory...
if not exist "%INSTALL_PATH%" mkdir "%INSTALL_PATH%"

echo Copying files...
copy /Y "WingetWizard.exe" "%INSTALL_PATH%\"
copy /Y "*.dll" "%INSTALL_PATH%\" 2>nul
copy /Y "WinGetLogo.png" "%INSTALL_PATH%\" 2>nul
copy /Y "config.json.example" "%INSTALL_PATH%\" 2>nul
copy /Y "README.txt" "%INSTALL_PATH%\" 2>nul

echo.
echo Creating desktop shortcut...
set "SHORTCUT=%USERPROFILE%\Desktop\WingetWizard.lnk"
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT%'); $Shortcut.TargetPath = '%INSTALL_PATH%\WingetWizard.exe'; $Shortcut.WorkingDirectory = '%INSTALL_PATH%'; $Shortcut.Description = 'AI-Enhanced Package Manager'; $Shortcut.Save()"

echo.
echo ✅ Installation completed successfully!
echo.
echo Installation path: %INSTALL_PATH%
echo Desktop shortcut: %SHORTCUT%
echo.
echo To start WingetWizard:
echo 1. Double-click the desktop shortcut
echo 2. Or run: "%INSTALL_PATH%\WingetWizard.exe"
echo.
set /p launch="Would you like to launch WingetWizard now? (y/n): "
if /i "%launch%"=="y" (
    echo Launching WingetWizard...
    start "" "%INSTALL_PATH%\WingetWizard.exe"
)

echo.
echo Installation complete! Press any key to exit.
pause >nul

