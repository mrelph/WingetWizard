# 📁 WingetWizard Project Structure

## Overview

WingetWizard follows a modular, service-based architecture that separates concerns and promotes maintainability, testability, and scalability. This document provides a comprehensive overview of the project organization.

## 🏗️ Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    WingetWizard Application                 │
├─────────────────────────────────────────────────────────────┤
│  Presentation Layer (UI)                                   │
│  ├── MainForm.cs (Primary Interface)                       │
│  ├── SpinningProgressForm.cs (Custom Progress Dialogs)     │
│  └── Settings Dialogs (Configuration UI)                   │
├─────────────────────────────────────────────────────────────┤
│  Business Logic Layer (Services)                           │
│  ├── AIService.cs (AI Integration)                         │
│  ├── PackageService.cs (Package Operations)                │
│  ├── ReportService.cs (Report Management)                  │
│  └── SettingsService.cs (Configuration)                    │
├─────────────────────────────────────────────────────────────┤
│  Data Layer (Models)                                       │
│  └── UpgradableApp.cs (Package Data Model)                 │
├─────────────────────────────────────────────────────────────┤
│  Utility Layer (Utils)                                     │
│  └── FileUtils.cs (File Operations & Helpers)              │
├─────────────────────────────────────────────────────────────┤
│  External Dependencies                                      │
│  ├── Windows Package Manager (winget)                      │
│  ├── Anthropic Claude API                                  │
│  ├── Perplexity API                                        │
│  └── .NET 6 Windows Forms                                  │
└─────────────────────────────────────────────────────────────┘
```

## 📂 Directory Structure

```
UpgradeApp/
├── 📁 Root Files
│   ├── MainForm.cs              # Main application form and entry point
│   ├── UpgradeApp.csproj        # Project configuration and dependencies
│   ├── README.md                # Project overview and quick start
│   ├── DOCUMENTATION.md         # Comprehensive technical documentation
│   ├── Logo.ico                 # Application icon
│   └── installer.wxs            # WiX installer configuration
│
├── 📁 Models/                   # Data models and entities
│   └── UpgradableApp.cs         # Package data model
│
├── 📁 Services/                 # Business logic services
│   ├── AIService.cs             # AI integration and recommendations
│   ├── PackageService.cs        # Package management operations
│   ├── ReportService.cs         # AI report generation and management
│   └── SettingsService.cs       # Configuration and API key management
│
├── 📁 Utils/                    # Utility classes and helpers
│   └── FileUtils.cs             # File operations and path validation
│
├── 📁 docs/                     # Documentation files
│   ├── API_REFERENCE.md         # Detailed API documentation
│   ├── USER_GUIDE.md            # End-user guide and tutorials
│   ├── SECURITY.md              # Security features and best practices
│   ├── DEPLOYMENT.md            # Build and deployment guide
│   └── PROJECT_STRUCTURE.md     # This file
│
├── 📁 bin/                      # Build outputs (auto-generated)
│   ├── Debug/                   # Debug build artifacts
│   └── Release/                 # Release build artifacts
│
├── 📁 obj/                      # Build intermediate files (auto-generated)
│   ├── Debug/                   # Debug intermediate files
│   └── Release/                 # Release intermediate files
│
└── 📁 Runtime Files (auto-generated)
    ├── settings.json            # User configuration and API keys
    ├── secure_settings.json     # Encrypted settings (if implemented)
    └── AI_Reports/              # Individual package analysis reports
        ├── PackageName1_YYYYMMDD_HHMMSS.md
        ├── PackageName2_YYYYMMDD_HHMMSS.md
        └── ...
```

## 🔧 Component Descriptions

### Root Level Files

#### `MainForm.cs`
- **Purpose**: Primary application interface and entry point
- **Responsibilities**:
  - UI event handling and user interaction
  - Service dependency injection and orchestration
  - Window management and layout
  - Progress indication and status updates
- **Dependencies**: All service classes, Windows Forms
- **Key Features**: Claude-inspired design, native OS theme integration

#### `UpgradeApp.csproj`
- **Purpose**: Project configuration and build settings
- **Contains**:
  - Target framework (.NET 6 Windows)
  - Package dependencies (System.Text.Json)
  - Assembly metadata and versioning
  - Build and publish configurations

### Models Layer

#### `UpgradableApp.cs`
```csharp
public class UpgradableApp
{
    public string Name { get; set; }           // Display name
    public string Id { get; set; }             // Unique identifier
    public string Version { get; set; }        // Current version
    public string Available { get; set; }      // Available version
    public string Status { get; set; }         // Current status
    public string Recommendation { get; set; } // AI recommendation
}
```

**Purpose**: Clean data model for package representation
- **Encapsulation**: Properties with getters and setters
- **Serialization**: Compatible with JSON serialization
- **Display**: Provides ToString() for readable output
- **Usage**: Shared across all application layers

### Services Layer

#### `AIService.cs`
**Purpose**: AI integration and recommendation engine

**Key Methods**:
```csharp
public async Task<string> GetAIRecommendationAsync(UpgradableApp app)
private async Task<string> GetPerplexityResearchAsync(UpgradableApp app)
private async Task<string> FormatReportWithClaudeAsync(UpgradableApp app, string researchData)
```

**Features**:
- Two-stage AI processing (research + formatting)
- Support for multiple AI providers (Claude, Perplexity)
- Thread-safe HTTP operations with semaphore throttling
- Structured 7-section report generation

#### `PackageService.cs`
**Purpose**: Core package management functionality

**Key Methods**:
```csharp
public async Task<List<UpgradableApp>> ListAllAppsAsync(string source, bool verbose)
public async Task<List<UpgradableApp>> CheckForUpdatesAsync(string source, bool verbose)
public async Task<string> UpgradePackageAsync(string packageId, bool verbose)
public string RunPowerShell(string command)
```

**Features**:
- Secure PowerShell command execution
- Command whitelist validation
- Support for multiple package sources
- Comprehensive error handling

#### `ReportService.cs`
**Purpose**: AI report generation and file management

**Key Methods**:
```csharp
public string CreateMarkdownContent(List<(UpgradableApp, string)> recommendations, bool usePerplexity, string selectedAiModel)
public void SaveIndividualPackageReports(List<(UpgradableApp, string)> recommendations)
public string? GetReportPath(string packageName)
```

**Features**:
- Professional markdown report generation
- Individual package report persistence
- Automatic directory management
- Report retrieval and tracking

#### `SettingsService.cs`
**Purpose**: Application configuration and settings management

**Key Methods**:
```csharp
public T GetSetting<T>(string key, T defaultValue = default(T)!)
public void SetSetting<T>(string key, T value)
public string GetApiKey(string keyName)
public void StoreApiKey(string keyName, string value)
```

**Features**:
- Type-safe setting retrieval
- JSON-based persistence
- Secure API key storage
- Automatic settings file management

### Utils Layer

#### `FileUtils.cs`
**Purpose**: File operations and security utilities

**Key Methods**:
```csharp
public static bool EnsureDirectoryExists(string path)
public static bool SafeWriteText(string filePath, string content)
public static string SafeReadText(string filePath, string defaultContent = "")
public static string CreateSafeFileName(string fileName)
```

**Features**:
- Path traversal protection
- Safe file operations with error handling
- Directory creation and management
- Filename sanitization

### Documentation Layer

#### `docs/` Directory
- **API_REFERENCE.md**: Comprehensive API documentation
- **USER_GUIDE.md**: End-user tutorials and help
- **SECURITY.md**: Security features and best practices
- **DEPLOYMENT.md**: Build and deployment instructions
- **PROJECT_STRUCTURE.md**: This architectural overview

## 🔄 Data Flow

### Package Operation Flow
```
User Action → MainForm → PackageService → PowerShell → winget → Results
                ↓
            Progress Updates → UI Status Bar
```

### AI Analysis Flow
```
Package Selection → MainForm → AIService → Perplexity API (Research)
                                    ↓
                                Claude API (Formatting) → ReportService → File System
                                    ↓
                            Formatted Report → MainForm → User Display
```

### Settings Management Flow
```
User Configuration → MainForm → SettingsService → settings.json
                                        ↓
                                Application Startup → Settings Load → Service Configuration
```

## 🏛️ Design Patterns

### Dependency Injection
```csharp
public MainForm()
{
    // Service initialization with dependency injection
    _settingsService = new SettingsService();
    _packageService = new PackageService();
    _reportService = new ReportService(reportsPath);
    _aiService = new AIService(apiKeys, model, provider);
}
```

### Service Layer Pattern
- **Separation of Concerns**: Business logic isolated from UI
- **Single Responsibility**: Each service has one focused purpose
- **Dependency Injection**: Services injected into presentation layer

### Repository Pattern (Implicit)
- **SettingsService**: Acts as repository for configuration data
- **ReportService**: Manages report persistence and retrieval
- **FileUtils**: Provides data access abstraction

### Factory Pattern (Planned)
```csharp
public interface IAIServiceFactory
{
    IAIService CreateAIService(string provider, string apiKey, string model);
}
```

## 🔗 Dependencies

### External Dependencies
```xml
<PackageReference Include="System.Text.Json" Version="8.0.5" />
```

### System Dependencies
- **.NET 6 Windows Forms**: UI framework
- **PowerShell**: Command execution
- **Windows Package Manager**: Package operations
- **HTTP Client**: API communications

### API Dependencies
- **Anthropic Claude API**: AI analysis and formatting
- **Perplexity API**: Real-time research and information

## 📊 Metrics and Statistics

### Codebase Statistics
- **Total Files**: ~15 source files
- **Lines of Code**: ~2,500 lines (estimated)
- **Services**: 4 core services
- **Models**: 1 primary data model
- **Utilities**: 1 utility class

### Architecture Metrics
- **Coupling**: Low (services are loosely coupled)
- **Cohesion**: High (each class has single responsibility)
- **Testability**: High (services can be unit tested independently)
- **Maintainability**: High (clear separation of concerns)

## 🚀 Extensibility Points

### Adding New Services
1. Create new service class in `Services/` directory
2. Implement service interface (if applicable)
3. Add dependency injection in `MainForm.cs`
4. Update documentation

### Adding New AI Providers
1. Extend `AIService.cs` with new provider methods
2. Add provider configuration to `SettingsService.cs`
3. Update UI settings dialog
4. Add provider-specific documentation

### Adding New Package Sources
1. Extend `PackageService.cs` with new source support
2. Update command validation logic
3. Add source selection to UI
4. Update user documentation

### Adding New Export Formats
1. Extend `ReportService.cs` with new format methods
2. Add format selection to export dialog
3. Update file extension handling
4. Document new format capabilities

## 🔒 Security Considerations

### File System Security
- **Path Validation**: All file paths validated in `FileUtils`
- **Directory Restrictions**: Operations limited to application directory
- **Permission Checks**: File permissions verified before operations

### Command Execution Security
- **Whitelist Validation**: Only approved commands allowed
- **Parameter Sanitization**: Command parameters cleaned
- **Process Isolation**: Commands run in isolated processes

### API Security
- **HTTPS Only**: All API communications encrypted
- **Key Storage**: API keys stored locally only
- **No Logging**: Sensitive data excluded from logs

## 📈 Future Architecture Plans

### Planned Enhancements
1. **Plugin Architecture**: Support for third-party extensions
2. **Database Integration**: Persistent storage for historical data
3. **Web API**: REST endpoints for external integration
4. **Caching Layer**: Performance optimization for repeated operations

### Scalability Improvements
1. **Async Operations**: Full async/await implementation
2. **Parallel Processing**: Concurrent package operations
3. **Background Services**: Scheduled tasks and monitoring
4. **Resource Management**: Memory and CPU optimization

---

**Architecture Version**: 2.1  
**Last Updated**: January 2025  
**Architecture Type**: Service-Based Modular Design  
**Documentation Status**: Complete