# Building the WingetWizard MSI Installer

## Prerequisites

1. **WiX Toolset** - Download and install from https://wixtoolset.org/releases/
   - After installation, ensure `candle.exe` and `light.exe` are in your PATH
   - Or use Visual Studio Developer Command Prompt which includes WiX

2. **.NET 6.0 SDK** - Required for building the application

## Quick Build

Simply run the build script:

```batch
build-installer.bat
```

This will:
1. Build the application in Release mode
2. Publish as self-contained single-file executable
3. Compile the WiX installer
4. Create the MSI file in `dist\WingetWizard-v2.4.msi`

## Manual Build Steps

If you prefer to build manually:

### Step 1: Build the Application
```batch
dotnet publish -c Release --self-contained true -r win-x64 -p:PublishSingleFile=true -o publish
```

### Step 2: Compile WiX Installer
```batch
candle.exe -dPublishDir="%CD%\publish\" installer.wxs -o installer.wixobj
```

### Step 3: Link Installer
```batch
light.exe installer.wixobj -ext WixUIExtension -o dist\WingetWizard-v2.4.msi
```

### Step 4: Clean Up
```batch
del installer.wixobj
del installer.wixpdb
```

## Installer Features

The MSI installer includes:

- ✅ **Complete Application Files**
  - WingetWizard.exe (main executable)
  - All required DLLs (D3DCompiler, PresentationNative, etc.)
  - WinGetLogo.png (application logo)
  - config.json.example (configuration template)

- ✅ **Windows Integration**
  - Start Menu shortcut
  - Desktop shortcut (optional)
  - Proper uninstall support via Add/Remove Programs
  - Registry entries for proper Windows integration

- ✅ **User Experience**
  - Minimal installation wizard
  - Installation directory selection
  - Progress indicators
  - Clean uninstall

## Installation

Users can install by:
1. Double-clicking the MSI file
2. Following the installation wizard
3. The application will be installed to `C:\Program Files\WingetWizard\`

## Uninstallation

Users can uninstall by:
1. Going to Settings → Apps → Apps & features
2. Finding "WingetWizard"
3. Clicking "Uninstall"

Or:
1. Start Menu → WingetWizard → Uninstall WingetWizard

## Troubleshooting

### WiX Not Found
- Install WiX Toolset from https://wixtoolset.org/releases/
- Add WiX to your PATH, or use Visual Studio Developer Command Prompt

### Build Fails
- Ensure .NET 6.0 SDK is installed
- Check that all files exist in the publish directory
- Verify WiX Toolset is properly installed

### Installer Won't Run
- Ensure you have administrator privileges
- Check Windows Defender/antivirus isn't blocking the installer
- Verify the MSI file isn't corrupted

## File Structure

```
dist/
  └── WingetWizard-v2.4.msi    (Final installer)

publish/
  ├── WingetWizard.exe
  ├── *.dll (supporting libraries)
  └── WinGetLogo.png
```

## Version Information

- **Product Version**: 2.4.0.0
- **Manufacturer**: GeekSuave Labs
- **Upgrade Code**: 12345678-1234-1234-1234-123456789012

The installer supports automatic upgrades - installing a newer version will automatically uninstall the old version.

