# Quick Start: Building the WingetWizard Installer

## Step 1: Install WiX Toolset

1. **Download WiX Toolset:**
   - Visit: https://wixtoolset.org/releases/
   - Download the latest stable version (v3.11 or later)
   - Run the installer

2. **Verify Installation:**
   - Open a new Command Prompt or PowerShell
   - Run: `candle.exe -?`
   - If you see help text, WiX is installed correctly

## Step 2: Build the Installer

Simply double-click **`build-installer.bat`** or run it from command line:

```batch
build-installer.bat
```

The script will:
1. ✅ Build the application
2. ✅ Compile the WiX installer
3. ✅ Create the MSI file
4. ✅ Output: `dist\WingetWizard-v2.4.msi`

## Step 3: Install

Double-click the MSI file to install WingetWizard on any Windows PC!

---

## Alternative: Manual Build (if WiX is in a custom location)

If WiX is installed but not in PATH:

1. **Find WiX installation:**
   - Usually at: `C:\Program Files (x86)\WiX Toolset v3.11\bin\`
   - Or: `C:\Program Files\WiX Toolset v3.11\bin\`

2. **Run from Visual Studio Developer Command Prompt:**
   - Open "Developer Command Prompt for VS"
   - Navigate to project directory
   - Run: `build-installer.bat`

3. **Or add WiX to PATH temporarily:**
   ```batch
   set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin
   build-installer.bat
   ```

## Troubleshooting

**"WiX Toolset not found"**
- Install WiX from https://wixtoolset.org/releases/
- Restart your terminal after installation

**"Build failed"**
- Ensure .NET 6.0 SDK is installed
- Check that all project files are present

**"WiX compilation failed"**
- Verify WiX is in PATH: `where candle.exe`
- Check that `installer.wxs` exists in project root





