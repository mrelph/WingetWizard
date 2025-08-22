# 📚 WingetWizard API Reference

## Services Layer

### AIService

**Purpose**: Manages AI-powered package analysis and recommendations using Claude AI and Perplexity APIs.

#### Constructor
```csharp
public AIService(string claudeApiKey, string perplexityApiKey, string selectedAiModel, bool usePerplexity)
```

#### Key Methods

##### `GetAIRecommendationAsync(UpgradableApp app)`
- **Purpose**: Gets comprehensive AI recommendation using two-stage process
- **Parameters**: `UpgradableApp app` - Package to analyze
- **Returns**: `Task<string>` - AI-generated recommendation in markdown format
- **Process**: 
  1. Perplexity research for current data
  2. Claude formatting for professional presentation

##### `GetPerplexityResearchAsync(UpgradableApp app)` (Private)
- **Purpose**: Gets raw research data from Perplexity API
- **Returns**: Factual information about the package

##### `FormatReportWithClaudeAsync(UpgradableApp app, string researchData)` (Private)
- **Purpose**: Formats research data into structured 7-section report
- **Sections**: Executive Summary, Version Changes, Key Improvements, Security Assessment, Compatibility & Risks, Timeline Recommendations, Action Items

---

### PackageService

**Purpose**: Handles all Windows Package Manager (winget) operations including listing, upgrading, installing, and uninstalling packages.

#### Key Methods

##### `ListAllAppsAsync(string source, bool verbose)`
- **Purpose**: Lists all installed applications
- **Parameters**: 
  - `source`: Package source ("winget", "msstore", "all")
  - `verbose`: Enable detailed logging
- **Returns**: `Task<List<UpgradableApp>>` - List of installed packages

##### `CheckForUpdatesAsync(string source, bool verbose)`
- **Purpose**: Checks for available package updates
- **Returns**: `Task<List<UpgradableApp>>` - List of packages with available updates

##### `UpgradePackageAsync(string packageId, bool verbose)`
- **Purpose**: Upgrades a specific package
- **Parameters**: 
  - `packageId`: Unique package identifier
  - `verbose`: Enable detailed logging
- **Returns**: `Task<string>` - Operation result

##### `InstallPackageAsync(string packageId, bool verbose)`
- **Purpose**: Installs a new package
- **Returns**: `Task<string>` - Installation result

##### `UninstallPackageAsync(string packageId, bool verbose)`
- **Purpose**: Uninstalls a package
- **Returns**: `Task<string>` - Uninstallation result

##### `RepairPackageAsync(string packageId, bool verbose)`
- **Purpose**: Repairs a corrupted package installation
- **Returns**: `Task<string>` - Repair result

##### `RunPowerShell(string command)`
- **Purpose**: Executes PowerShell commands with security validation
- **Security**: Validates commands against whitelist before execution
- **Returns**: `string` - Command output

---

### ReportService

**Purpose**: Manages AI report generation, saving, and retrieval with markdown formatting and file organization.

#### Constructor
```csharp
public ReportService(string reportsDirectory)
```

#### Key Methods

##### `GetReportPath(string packageName)`
- **Purpose**: Gets the file path for a saved package report
- **Returns**: `string?` - Report file path or null if not found

##### `HasReport(string packageName)`
- **Purpose**: Checks if a report exists for a package
- **Returns**: `bool` - True if report exists

##### `CreateMarkdownContent(List<(UpgradableApp, string)> recommendations, bool usePerplexity, string selectedAiModel)`
- **Purpose**: Creates comprehensive markdown report with metadata
- **Features**: Executive summary, package analysis, metadata, timestamps
- **Returns**: `string` - Formatted markdown content

##### `SaveIndividualPackageReports(List<(UpgradableApp, string)> recommendations)`
- **Purpose**: Saves individual reports for each package
- **File Format**: `PackageID_YYYYMMDD_HHMMSS.md`
- **Directory**: Automatically creates AI_Reports directory

##### `LoadExistingReports()` (Private)
- **Purpose**: Loads existing report files on service initialization
- **Behavior**: Populates internal report tracking dictionary

---

### SettingsService

**Purpose**: Manages application configuration, API keys, and user preferences with secure storage.

#### Key Methods

##### `GetSetting<T>(string key, T defaultValue)`
- **Purpose**: Retrieves a setting value with type safety
- **Type Support**: bool, string, int
- **Returns**: Setting value or default if not found

##### `SetSetting<T>(string key, T value)`
- **Purpose**: Sets a setting value
- **Persistence**: Automatically saves to settings.json

##### `GetApiKey(string keyName)`
- **Purpose**: Retrieves API key by name
- **Security**: Keys stored locally in settings.json
- **Returns**: `string` - API key or empty string

##### `StoreApiKey(string keyName, string value)`
- **Purpose**: Stores an API key securely
- **Keys**: "AnthropicApiKey", "PerplexityApiKey"

##### `RemoveApiKey(string keyName)`
- **Purpose**: Removes an API key from settings

##### `SaveSettings()`
- **Purpose**: Persists all settings to JSON file
- **File**: settings.json in application directory

##### `LoadSettings()` (Private)
- **Purpose**: Loads settings from JSON file on initialization
- **Behavior**: Creates default settings if file doesn't exist

---

## Models Layer

### UpgradableApp

**Purpose**: Data model representing a Windows package with upgrade and AI recommendation information.

#### Properties

```csharp
public string Name { get; set; }           // Display name of the application
public string Id { get; set; }             // Unique package identifier  
public string Version { get; set; }        // Currently installed version
public string Available { get; set; }      // Available version for upgrade
public string Status { get; set; }         // Installation/upgrade status
public string Recommendation { get; set; } // AI-generated recommendation
```

#### Methods

##### `ToString()`
- **Purpose**: Provides readable string representation
- **Format**: `"{Name} ({Id}) - {Version} -> {Available}"`

---

## Utils Layer

### FileUtils (Static Class)

**Purpose**: Provides utility methods for file operations, validation, and safe file handling.

#### Key Methods

##### `EnsureDirectoryExists(string path)`
- **Purpose**: Safely creates directory if it doesn't exist
- **Returns**: `bool` - Success status
- **Error Handling**: Logs errors but doesn't throw exceptions

##### `SafeWriteText(string filePath, string content)`
- **Purpose**: Writes text to file with UTF-8 encoding
- **Safety**: Ensures parent directory exists
- **Returns**: `bool` - Success status

##### `SafeReadText(string filePath, string defaultContent = "")`
- **Purpose**: Reads text from file with UTF-8 encoding
- **Fallback**: Returns default content if file doesn't exist
- **Returns**: `string` - File content or default

##### `CreateSafeFileName(string fileName)`
- **Purpose**: Creates filesystem-safe filename
- **Security**: Removes invalid characters and path traversal attempts
- **Returns**: `string` - Safe filename

##### `IsValidFilePath(string filePath)`
- **Purpose**: Validates file path for security
- **Checks**: Path traversal, invalid characters, reserved names
- **Returns**: `bool` - True if path is safe

##### `GetFileExtension(string fileName)`
- **Purpose**: Extracts file extension safely
- **Returns**: `string` - File extension or empty string

##### `GenerateTimestampedFileName(string baseName, string extension)`
- **Purpose**: Creates timestamped filename for reports
- **Format**: `baseName_YYYYMMDD_HHMMSS.extension`
- **Returns**: `string` - Timestamped filename

---

## Integration Patterns

### Service Dependency Injection

```csharp
public MainForm()
{
    // Initialize services with proper dependencies
    _settingsService = new SettingsService();
    _packageService = new PackageService();
    _reportService = new ReportService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI_Reports"));
    
    // AI service requires API keys and configuration
    var claudeKey = _settingsService.GetApiKey("AnthropicApiKey");
    var perplexityKey = _settingsService.GetApiKey("PerplexityApiKey");
    var model = _settingsService.GetSetting<string>("SelectedAiModel", "claude-sonnet-4-20250514");
    var usePerplexity = _settingsService.GetSetting<bool>("UsePerplexity", false);
    
    _aiService = new AIService(claudeKey, perplexityKey, model, usePerplexity);
    
    InitializeComponent();
}
```

### Error Handling Pattern

All services follow consistent error handling:
- Try-catch blocks around external operations
- Logging to Debug output for development
- Graceful degradation when services fail
- User-friendly error messages in UI

### Thread Safety

- Services use proper async/await patterns
- HTTP operations use SemaphoreSlim for throttling
- File operations are atomic where possible
- UI updates are marshaled to main thread

---

## Configuration Files

### settings.json Structure

```json
{
    "AnthropicApiKey": "sk-ant-...",
    "PerplexityApiKey": "pplx-...",
    "SelectedAiModel": "claude-sonnet-4-20250514",
    "UsePerplexity": false,
    "IsAdvancedMode": true,
    "VerboseLogging": false,
    "LastUpdateCheck": "2025-01-21T10:30:00Z"
}
```

### AI_Reports Directory Structure

```
AI_Reports/
├── Audacity_20250121_103045.md
├── Git_20250121_103046.md
├── NVIDIA_Omniverse_Launcher_20250121_103047.md
└── ...
```

---

## Security Considerations

### API Key Management
- Keys stored locally in settings.json
- No keys in version control (.gitignore protection)
- Runtime loading with graceful degradation

### Command Execution
- Whitelist validation for PowerShell commands
- Parameter sanitization to prevent injection
- Process isolation with restricted permissions

### File Operations
- Path traversal prevention in FileUtils
- Safe filename generation
- Directory creation with proper error handling

---

**Last Updated**: January 2025  
**Version**: 2.1  
**Architecture**: Service-Based Modular Design