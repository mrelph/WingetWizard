# WingetWizard MSI Installer

## Overview

This directory contains the complete WiX-based MSI installer for WingetWizard v2.4. The installer provides a professional Windows installation experience with proper integration into the Windows ecosystem.

## Quick Start

### Building the Installer

1. **Install Prerequisites:**
   - WiX Toolset: https://wixtoolset.org/releases/
   - .NET 6.0 SDK

2. **Run the Build Script:**
   ```batch
   build-installer.bat
   ```

3. **Output:**
   - The MSI installer will be created at: `dist\WingetWizard-v2.4.msi`

## Installer Features

### ✅ Complete Package
- Main executable (WingetWizard.exe)
- All required DLLs and dependencies
- Application resources (logo, config template)
- Self-contained (no .NET runtime required)

### ✅ Windows Integration
- **Start Menu Shortcut**: Automatically created
- **Desktop Shortcut**: Optional (can be disabled during install)
- **Add/Remove Programs**: Proper uninstall support
- **Registry Entries**: For Windows integration

### ✅ User Experience
- Minimal installation wizard
- Installation directory selection
- Progress indicators
- Clean uninstall process

## Installation

### For End Users

1. Download `WingetWizard-v2.4.msi`
2. Double-click the MSI file
3. Follow the installation wizard
4. Application will be installed to `C:\Program Files\WingetWizard\`

### Silent Installation (Enterprise)

```batch
msiexec /i WingetWizard-v2.4.msi /quiet /norestart
```

### Uninstallation

**Method 1: Windows Settings**
1. Settings → Apps → Apps & features
2. Find "WingetWizard"
3. Click "Uninstall"

**Method 2: Command Line**
```batch
msiexec /x WingetWizard-v2.4.msi /quiet
```

## Technical Details

### Product Information
- **Product Name**: WingetWizard
- **Version**: 2.4.0.0
- **Manufacturer**: GeekSuave Labs
- **Upgrade Code**: 12345678-1234-1234-1234-123456789012

### Installation Scope
- **Per-Machine**: Installs to `C:\Program Files\WingetWizard\`
- **Requires**: Administrator privileges

### Files Included
- `WingetWizard.exe` - Main application
- `D3DCompiler_47_cor3.dll` - DirectX compiler
- `PenImc_cor3.dll` - Pen input
- `PresentationNative_cor3.dll` - WPF native
- `vcruntime140_cor3.dll` - Visual C++ runtime
- `wpfgfx_cor3.dll` - WPF graphics
- `WinGetLogo.png` - Application logo
- `config.json.example` - Configuration template

## Build Process

The build script (`build-installer.bat`) performs these steps:

1. **Build Application**: Compiles and publishes the .NET application
2. **Compile WiX**: Compiles the installer definition (installer.wxs)
3. **Link Installer**: Creates the final MSI file
4. **Cleanup**: Removes temporary build files

## Troubleshooting

### WiX Not Found
- Install WiX Toolset from https://wixtoolset.org/releases/
- Add WiX to PATH, or use Visual Studio Developer Command Prompt

### Build Errors
- Ensure .NET 6.0 SDK is installed
- Verify all files exist in the publish directory
- Check WiX Toolset installation

### Installation Issues
- Run installer as Administrator
- Check Windows Defender/antivirus settings
- Verify MSI file integrity

## Version History

- **v2.4.0.0**: Initial MSI installer release
  - Complete Windows integration
  - Start Menu and Desktop shortcuts
  - Proper uninstall support

## Support

For issues or questions:
- Check `BUILD_INSTALLER.md` for detailed build instructions
- Review `docs/DEPLOYMENT.txt` for deployment options
- Report issues on GitHub

