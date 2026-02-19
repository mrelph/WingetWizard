@echo off
echo ========================================
echo Building WingetWizard v2.4
echo ========================================

echo Step 1: Cleaning previous build...
dotnet clean --verbosity quiet

echo Step 2: Clearing NuGet cache...
dotnet nuget locals all --clear --verbosity quiet

echo Step 3: Restoring packages...
dotnet restore --verbosity quiet

echo Step 4: Building application...
dotnet build --configuration Release --verbosity quiet

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ BUILD FAILED! 
    echo Try running as Administrator or check for path length issues.
    pause
    exit /b 1
)

echo.
echo ✅ Build successful! Starting WingetWizard...
echo.

cd "bin\Release\net6.0-windows\win-x64"

if exist "WingetWizard.exe" (
    echo Starting WingetWizard.exe...
    start WingetWizard.exe
) else (
    echo ❌ WingetWizard.exe not found!
    echo Expected location: %CD%\WingetWizard.exe
    pause
)

echo.
echo Build and launch complete!
echo If the app didn't start, check the bin\Release folder.
pause