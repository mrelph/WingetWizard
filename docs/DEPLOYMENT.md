# 🚀 WingetWizard Deployment Guide

## Overview

This guide covers building, configuring, and deploying WingetWizard for both development and production environments.

## 📋 Prerequisites

### System Requirements

#### Development Environment
- **Operating System**: Windows 10/11 (version 1903 or later)
- **.NET SDK**: .NET 6.0 SDK or later
- **IDE**: Visual Studio 2022 or VS Code with C# extension
- **Git**: For version control and source management
- **PowerShell**: Version 5.1 or later (included with Windows)

#### Runtime Environment
- **Operating System**: Windows 10/11
- **.NET Runtime**: .NET 6.0 Runtime (or self-contained deployment)
- **Windows Package Manager**: winget (included with Windows 11, installable on Windows 10)
- **PowerShell**: Version 5.1 or later
- **Internet Connection**: Required for AI features and package operations

### API Requirements

#### Required APIs
- **Anthropic API**: Claude AI models (required for AI features)
  - API Key format: `sk-ant-...`
  - Minimum credits recommended: $10 for testing

#### Optional APIs
- **Perplexity API**: Real-time web research (optional but recommended)
  - API Key format: `pplx-...`
  - Enhances AI analysis with current information

## 🔧 Build Process

### Development Build

#### Clone Repository
```bash
git clone <repository-url>
cd UpgradeApp
```

#### Restore Dependencies
```bash
dotnet restore
```

#### Build Application
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

#### Run Application
```bash
dotnet run
```

### Production Build

#### Single-File Executable
```bash
# Self-contained single file (recommended)
dotnet publish -c Release --self-contained true -r win-x64 -p:PublishSingleFile=true

# Framework-dependent single file (smaller size)
dotnet publish -c Release --self-contained false -r win-x64 -p:PublishSingleFile=true
```

#### Traditional Deployment
```bash
# Framework-dependent deployment
dotnet publish -c Release -r win-x64

# Self-contained deployment
dotnet publish -c Release --self-contained true -r win-x64
```

### Build Outputs

#### Single-File Deployment
- **Location**: `bin/Release/net6.0-windows/win-x64/publish/`
- **Main File**: `UpgradeApp.exe` (~100MB self-contained, ~15MB framework-dependent)
- **Additional Files**: `Logo.ico`, configuration files

#### Traditional Deployment
- **Location**: `bin/Release/net6.0-windows/win-x64/publish/`
- **Main Files**: `UpgradeApp.exe`, `UpgradeApp.dll`, dependencies
- **Runtime**: .NET runtime included (if self-contained)

## ⚙️ Configuration

### Application Configuration

#### Project File Settings
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationIcon>Logo.ico</ApplicationIcon>
    
    <!-- Assembly Information -->
    <AssemblyTitle>WingetWizard - AI-Enhanced Package Manager</AssemblyTitle>
    <AssemblyDescription>Modern Windows package management with AI-powered recommendations</AssemblyDescription>
    <AssemblyCompany>GeekSuave Labs</AssemblyCompany>
    <AssemblyProduct>WingetWizard</AssemblyProduct>
    <AssemblyCopyright>Copyright © 2024 Mark Relph</AssemblyCopyright>
    <AssemblyVersion>2.1.0.0</AssemblyVersion>
    <FileVersion>2.1.0.0</FileVersion>
    <Version>2.1.0</Version>
    
    <!-- Publishing Options -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishTrimmed>false</PublishTrimmed>
  </PropertyGroup>
</Project>
```

#### Runtime Configuration

##### settings.json Template
```json
{
  "AnthropicApiKey": "",
  "PerplexityApiKey": "",
  "SelectedAiModel": "claude-sonnet-4-20250514",
  "UsePerplexity": false,
  "IsAdvancedMode": true,
  "VerboseLogging": false,
  "LastUpdateCheck": "",
  "WindowState": "Normal",
  "WindowLocation": "100,100",
  "WindowSize": "1200,800",
  "LogsPanelCollapsed": true
}
```

### Environment-Specific Configuration

#### Development Environment
```json
{
  "VerboseLogging": true,
  "IsAdvancedMode": true,
  "SelectedAiModel": "claude-3-5-haiku-20241022",
  "UsePerplexity": true
}
```

#### Production Environment
```json
{
  "VerboseLogging": false,
  "IsAdvancedMode": true,
  "SelectedAiModel": "claude-sonnet-4-20250514",
  "UsePerplexity": false
}
```

## 📦 Distribution Methods

### Method 1: Single Executable Distribution

#### Advantages
- **Simple Deployment**: Single file to distribute
- **No Dependencies**: Includes .NET runtime
- **Portable**: Runs on any Windows 10/11 system
- **Self-Contained**: No installation required

#### Package Contents
```
WingetWizard-v2.1/
├── UpgradeApp.exe          # Main executable (~100MB)
├── Logo.ico                # Application icon
├── settings.json.template  # Configuration template
├── README.md              # Quick start guide
└── docs/                  # Documentation folder
    ├── USER_GUIDE.md
    ├── API_REFERENCE.md
    ├── SECURITY.md
    └── DEPLOYMENT.md
```

#### Distribution Script
```powershell
# Create distribution package
$version = "2.1"
$packageName = "WingetWizard-v$version"

# Create package directory
New-Item -ItemType Directory -Path $packageName -Force

# Copy executable
Copy-Item "bin/Release/net6.0-windows/win-x64/publish/UpgradeApp.exe" $packageName/
Copy-Item "Logo.ico" $packageName/
Copy-Item "README.md" $packageName/

# Copy documentation
Copy-Item "docs" $packageName/ -Recurse

# Create settings template
@{
    "AnthropicApiKey" = ""
    "PerplexityApiKey" = ""
    "SelectedAiModel" = "claude-sonnet-4-20250514"
    "UsePerplexity" = $false
    "IsAdvancedMode" = $true
    "VerboseLogging" = $false
} | ConvertTo-Json | Out-File "$packageName/settings.json.template"

# Create ZIP package
Compress-Archive -Path $packageName -DestinationPath "$packageName.zip"
```

### Method 2: Installer Package (WiX)

#### WiX Installer Configuration
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="WingetWizard" Language="1033" Version="2.1.0.0" 
           Manufacturer="GeekSuave Labs" UpgradeCode="12345678-1234-1234-1234-123456789012">
    
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perUser" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    <MediaTemplate />
    
    <Feature Id="ProductFeature" Title="WingetWizard" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>

  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="LocalAppDataFolder">
        <Directory Id="INSTALLFOLDER" Name="WingetWizard" />
      </Directory>
    </Directory>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="MainExecutable">
        <File Source="bin/Release/net6.0-windows/win-x64/publish/UpgradeApp.exe" />
      </Component>
      <Component Id="ApplicationIcon">
        <File Source="Logo.ico" />
      </Component>
    </ComponentGroup>
  </Fragment>
</Wix>
```

#### Build Installer
```bash
# Install WiX Toolset
# Build installer
candle installer.wxs
light installer.wixobj
```

### Method 3: Microsoft Store Package (MSIX)

#### Package.appxmanifest
```xml
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
  <Identity Name="GeekSuaveLabs.WingetWizard" 
            Publisher="CN=GeekSuave Labs"
            Version="2.1.0.0" />
            
  <Properties>
    <DisplayName>WingetWizard</DisplayName>
    <PublisherDisplayName>GeekSuave Labs</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>AI-Enhanced Windows Package Manager</Description>
  </Properties>
  
  <Applications>
    <Application Id="App" Executable="UpgradeApp.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="WingetWizard" 
                          Description="Modern Windows package management with AI-powered recommendations"
                          BackgroundColor="transparent"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png">
      </uap:VisualElements>
    </Application>
  </Applications>
</Package>
```

## 🔐 Security Configuration

### Code Signing

#### Certificate Requirements
- **Type**: Code signing certificate from trusted CA
- **Format**: .pfx or .p12 file with private key
- **Validation**: Extended Validation (EV) recommended for better trust

#### Signing Process
```powershell
# Sign executable
signtool sign /f "certificate.pfx" /p "password" /t "http://timestamp.digicert.com" "UpgradeApp.exe"

# Verify signature
signtool verify /pa "UpgradeApp.exe"
```

### Security Hardening

#### File Permissions
```powershell
# Set restrictive permissions on settings file
icacls "settings.json" /grant:r "$env:USERNAME:(R,W)" /inheritance:r

# Set permissions on AI_Reports directory
icacls "AI_Reports" /grant:r "$env:USERNAME:(F)" /inheritance:r
```

#### Registry Settings (Optional)
```registry
[HKEY_CURRENT_USER\Software\WingetWizard]
"SecureMode"=dword:00000001
"ValidateCommands"=dword:00000001
"RestrictFileOperations"=dword:00000001
```

## 🌐 Network Configuration

### Firewall Rules

#### Windows Defender Firewall
```powershell
# Allow outbound HTTPS for API calls
New-NetFirewallRule -DisplayName "WingetWizard HTTPS Out" -Direction Outbound -Protocol TCP -LocalPort 443 -Action Allow

# Allow winget operations
New-NetFirewallRule -DisplayName "WingetWizard HTTP Out" -Direction Outbound -Protocol TCP -LocalPort 80 -Action Allow
```

### Proxy Configuration

#### Corporate Proxy Support
```csharp
// In HttpClient configuration
var handler = new HttpClientHandler()
{
    UseProxy = true,
    Proxy = new WebProxy("http://proxy.company.com:8080")
    {
        Credentials = new NetworkCredential("username", "password")
    }
};

var httpClient = new HttpClient(handler);
```

## 📊 Monitoring and Logging

### Application Logging

#### Log Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "File": {
      "Path": "logs/wingetwizard-{Date}.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  }
}
```

#### Performance Monitoring
```csharp
// Add performance counters
var performanceCounters = new Dictionary<string, PerformanceCounter>
{
    ["CPU"] = new PerformanceCounter("Processor", "% Processor Time", "_Total"),
    ["Memory"] = new PerformanceCounter("Memory", "Available MBytes"),
    ["Network"] = new PerformanceCounter("Network Interface", "Bytes Total/sec", "_Total")
};
```

## 🚀 Deployment Scenarios

### Scenario 1: Individual User Installation

#### Steps
1. Download single executable package
2. Extract to desired location (e.g., `C:\Tools\WingetWizard\`)
3. Create desktop shortcut
4. Configure API keys on first run
5. Start using the application

#### Automation Script
```powershell
# Download and install WingetWizard
$downloadUrl = "https://github.com/user/repo/releases/latest/download/WingetWizard.zip"
$installPath = "$env:LOCALAPPDATA\WingetWizard"

# Create installation directory
New-Item -ItemType Directory -Path $installPath -Force

# Download and extract
Invoke-WebRequest -Uri $downloadUrl -OutFile "$env:TEMP\WingetWizard.zip"
Expand-Archive -Path "$env:TEMP\WingetWizard.zip" -DestinationPath $installPath -Force

# Create desktop shortcut
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\WingetWizard.lnk")
$Shortcut.TargetPath = "$installPath\UpgradeApp.exe"
$Shortcut.IconLocation = "$installPath\Logo.ico"
$Shortcut.Save()

# Cleanup
Remove-Item "$env:TEMP\WingetWizard.zip"
```

### Scenario 2: Enterprise Deployment

#### Group Policy Deployment
```powershell
# Deploy via Group Policy startup script
$networkPath = "\\server\share\WingetWizard"
$localPath = "$env:ProgramFiles\WingetWizard"

# Copy files
robocopy $networkPath $localPath /E /R:3 /W:10

# Create all users desktop shortcut
$publicDesktop = "$env:PUBLIC\Desktop"
Copy-Item "$localPath\WingetWizard.lnk" $publicDesktop

# Set enterprise configuration
$enterpriseConfig = @{
    "IsAdvancedMode" = $true
    "VerboseLogging" = $false
    "UsePerplexity" = $false
    "SelectedAiModel" = "claude-sonnet-4-20250514"
} | ConvertTo-Json

$enterpriseConfig | Out-File "$localPath\settings.json" -Encoding UTF8
```

#### SCCM Deployment Package
```xml
<!-- SCCM Application Definition -->
<Application>
  <Name>WingetWizard</Name>
  <Version>2.1</Version>
  <Publisher>GeekSuave Labs</Publisher>
  <InstallCommand>powershell.exe -ExecutionPolicy Bypass -File "install.ps1"</InstallCommand>
  <UninstallCommand>powershell.exe -ExecutionPolicy Bypass -File "uninstall.ps1"</UninstallCommand>
  <DetectionRule>
    <FileExists Path="C:\Program Files\WingetWizard\UpgradeApp.exe" />
  </DetectionRule>
</Application>
```

### Scenario 3: Portable Deployment

#### USB/Portable Drive Setup
```
WingetWizard-Portable/
├── UpgradeApp.exe
├── Logo.ico
├── portable.ini          # Indicates portable mode
├── settings.json         # Portable settings
├── AI_Reports/           # Portable reports directory
└── docs/                # Documentation
```

#### Portable Configuration
```ini
; portable.ini
[Settings]
Portable=true
DataDirectory=.\Data
SettingsFile=.\settings.json
ReportsDirectory=.\AI_Reports
```

## 🔄 Update Management

### Automatic Updates

#### Update Check Service
```csharp
public class UpdateService
{
    private const string UpdateUrl = "https://api.github.com/repos/user/repo/releases/latest";
    
    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync(UpdateUrl);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestVersion = Version.Parse(release.TagName.TrimStart('v'));
            
            return latestVersion > currentVersion;
        }
        catch
        {
            return false;
        }
    }
}
```

#### Update Notification
```csharp
if (await _updateService.CheckForUpdatesAsync())
{
    var result = MessageBox.Show(
        "A new version of WingetWizard is available. Would you like to download it?",
        "Update Available",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Information);
        
    if (result == DialogResult.Yes)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/user/repo/releases/latest",
            UseShellExecute = true
        });
    }
}
```

### Manual Updates

#### Update Process
1. Download new version
2. Close running application
3. Backup current settings
4. Replace executable
5. Restore settings
6. Restart application

#### Update Script
```powershell
# update.ps1
param(
    [string]$NewVersionPath,
    [string]$InstallPath = "$env:LOCALAPPDATA\WingetWizard"
)

# Stop application if running
Get-Process -Name "UpgradeApp" -ErrorAction SilentlyContinue | Stop-Process -Force

# Backup current settings
$settingsBackup = "$env:TEMP\wingetwizard-settings-backup.json"
Copy-Item "$InstallPath\settings.json" $settingsBackup -ErrorAction SilentlyContinue

# Replace executable
Copy-Item $NewVersionPath "$InstallPath\UpgradeApp.exe" -Force

# Restore settings
Copy-Item $settingsBackup "$InstallPath\settings.json" -ErrorAction SilentlyContinue

# Start application
Start-Process "$InstallPath\UpgradeApp.exe"

# Cleanup
Remove-Item $settingsBackup -ErrorAction SilentlyContinue
```

## 📋 Deployment Checklist

### Pre-Deployment

- [ ] Build tested and verified
- [ ] All dependencies included
- [ ] Code signed with valid certificate
- [ ] Security scan completed
- [ ] Documentation updated
- [ ] Configuration templates prepared
- [ ] Installation scripts tested
- [ ] Backup procedures documented

### Deployment

- [ ] Target environment prepared
- [ ] Network connectivity verified
- [ ] Permissions configured
- [ ] Application deployed
- [ ] Configuration applied
- [ ] Initial testing completed
- [ ] User training provided
- [ ] Support documentation available

### Post-Deployment

- [ ] Application functionality verified
- [ ] Performance monitoring enabled
- [ ] Log files configured
- [ ] Update mechanism tested
- [ ] User feedback collected
- [ ] Issues documented and resolved
- [ ] Success metrics measured
- [ ] Lessons learned documented

---

**Deployment Version**: 2.1  
**Last Updated**: January 2025  
**Deployment Type**: Multi-Platform Support  
**Support Contact**: Available through GitHub Issues