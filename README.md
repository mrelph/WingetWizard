# 🧿 WingetWizard - Modern AI-Enhanced Package Manager

WingetWizard is a beautifully designed, AI-powered Windows package manager featuring a Claude-inspired interface, intelligent upgrade recommendations, and comprehensive security analysis. Experience package management reimagined with modern aesthetics and professional functionality.

## ✨ Features

### 📦 Package Operations
- **🔄 Check Updates**: Scan for available package updates with security validation
- **📦 Upgrade Selected**: Update only checked packages individually with safety checks
- **🚀 Upgrade All**: Update all available packages at once with progress tracking
- **📋 List All Apps**: View complete inventory of installed software
- **📦 Install Selected**: Install new packages from checked items with validation
- **🗑️ Uninstall Selected**: Remove checked packages safely with confirmation
- **🔧 Repair Selected**: Fix corrupted or problematic installations

### 🤖 AI-Powered Features
- **Comprehensive Application Analysis**: Full software overview including purpose, developer, features, and use cases
- **Enhanced AI Prompting**: Two-stage process with Perplexity research and Claude formatting
- **Rich Markdown Reports**: Color-coded recommendations with emoji indicators and professional styling
- **Persistent AI Reports**: Individual package reports automatically saved with clickable status column links
- **Status Column Integration**: "📄 View Report" links in status column for instant access to saved reports
- **Dual AI Providers**: Perplexity (real-time web research) + Claude (professional formatting)
- **Security Assessment**: Vulnerability analysis with risk level indicators (🟢🟡🔴🟣)
- **Multiple AI Models**: Claude Sonnet 4, 3.5 Sonnet, 3.5 Haiku support
- **Intelligent Export**: Professional markdown reports with metadata and executive summaries
- **Modern Progress Tracking**: Sleek in-UI progress bar with real-time status updates
- **Report Management**: Automatic AI_Reports directory creation with timestamped files

### 🔒 Security Features
- **Command Injection Protection**: Validated winget command execution
- **Path Traversal Prevention**: Sanitized file path handling
- **Secure API Key Management**: Password-masked prompts with encrypted storage
- **Thread-Safe Operations**: Synchronized HTTP requests and UI updates
- **Input Validation**: Comprehensive parameter sanitization
- **Safe File Operations**: Protected export and logging functionality

### 📤 Export & Logging
- **📤 Professional Export**: Auto-generated filenames with timestamps and package counts
- **💾 Rich Markdown Export**: Beautifully formatted AI research reports with visual hierarchy
- **📄 Smart Logging**: Collapsible panel hidden by default for cleaner interface
- **Real-time Updates**: Comprehensive operation tracking with modern green terminal styling
- **Enhanced Help System**: Rich text help dialogs with colors, emojis, and better typography

### 🎨 Native Windows Theme Integration
- **OS Theme Respect**: Automatically detects and adapts to Windows dark/light mode settings
- **Dark Mode Window Chrome**: Native dark title bar, minimize/maximize/close buttons
- **Complete Theme Coverage**: All dialogs, controls, and UI elements respect OS theme
- **Welcome Experience**: Personalized time-based greetings with theme-appropriate colors
- **Smart Welcome Cards**: Interactive action suggestions with adaptive theming
- **Modern Typography**: Calibri font with intelligent fallbacks for enhanced readability
- **Card-Based Buttons**: Elegant spacing and theme-aware hover effects
- **Professional Colors**: Dynamic color scheme that adapts to light/dark modes
- **Minimal Progress Indicator**: Sleek in-UI progress bar with theme-appropriate colors
- **Rich Text Displays**: Theme-aware help dialogs and AI reports with proper contrast
- **Intelligent Layout**: Hidden logs panel by default, larger fonts, and generous spacing
- **Responsive Columns**: Auto-sizing columns that adapt proportionally to window changes
- **Smart Tooltips**: Helpful tooltips for all buttons when window is scaled down

## 🎨 User Experience Highlights

### 🌟 **Welcome Experience**
- **Personalized Greeting**: Time-aware welcome message with theme-appropriate colors
- **Action Cards**: Five elegant suggestion cards with adaptive theming
- **Smart Visibility**: Welcome screen appears when empty, hides when packages load
- **Native Aesthetics**: Fully integrated Windows design language with OS theme respect

### 🎯 **Modern Interactions**
- **Native Theme Integration**: Seamless adaptation to Windows dark/light mode preferences
- **In-UI Progress**: Minimal progress bar with theme-appropriate colors (no modal popups)
- **Rich Text Reports**: Theme-aware AI analysis with proper contrast and visual hierarchy
- **Dynamic Colors**: Professional palette that adapts to OS theme settings
- **Enhanced Typography**: Modern Calibri font with theme-appropriate contrast

### 📱 **Responsive Design**
- **Adaptive Layout**: Intelligent spacing and sizing for different screen sizes
- **Theme-Aware Interface**: All elements adapt to OS dark/light mode settings
- **Hidden-by-Default Logs**: Cleaner interface with theme-appropriate collapsible logging
- **Card-Based Actions**: Buttons with theme-aware hover effects and spacing
- **Professional Help**: Rich text help system with OS theme integration

## 🛠️ Technical Stack

- **Framework**: .NET 6 Windows Forms with native OS theme integration
- **Architecture**: Modular service-based architecture with dependency injection
- **Typography**: Calibri font family with intelligent fallback system  
- **Design Language**: Native Windows interface with full OS theme respect
- **Theme System**: Windows API integration for dark mode window chrome
- **AI Integration**: Two-stage prompting with comprehensive application analysis
- **Progress Indicators**: Minimal in-UI progress bar with theme-appropriate colors
- **Rich Text Rendering**: Theme-aware markdown display with proper contrast
- **Deployment**: Single-file executable with self-contained deployment
- **Security**: Thread-safe operations, input validation, secure storage
- **Services**: AIService, PackageService, ReportService, SettingsService
- **Models**: UpgradableApp data model for package representation
- **Utils**: FileUtils for safe file operations and path validation

## 📋 Requirements

- Windows 10/11
- .NET 6 Runtime (or self-contained build)
- Windows Package Manager (winget)
- API keys for AI features:
  - Anthropic API key for Claude models
  - Perplexity API key for real-time web research (optional)
- Administrator privileges recommended for package operations

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd UpgradeApp
   ```

2. **Build and Run**
   ```bash
   dotnet build
   dotnet run
   ```

3. **Configure API Keys**
   - Launch the application
   - Click "⚙️ Settings" button
   - Enter API keys when prompted (securely stored in settings.json)
   - Choose AI provider: Claude or Perplexity
   - Select AI model and UI mode preferences

## 📖 Usage

### 🌟 **Getting Started Experience**
1. **Welcome Screen**: Greeted with personalized message in your OS theme colors
2. **Quick Actions**: Click theme-aware suggestion cards or use toolbar buttons
3. **Smart Interface**: Logs hidden by default with native theme integration
4. **Progress Feedback**: Theme-appropriate progress bar shows operation status

### 📦 **Package Operations**
1. **🔄 Check Updates**: Scan for available package updates with sleek progress indicator
2. **📋 List All Apps**: View complete software inventory in modern interface  
3. **Select Packages**: Use checkboxes to select multiple packages for batch operations
4. **📦 Upgrade Selected**: Update only checked packages with real-time status
5. **🚀 Upgrade All**: Update all available packages with comprehensive progress tracking
6. **📦 Install Selected**: Install new packages with validation and confirmation
7. **🗑️ Uninstall Selected**: Remove packages safely with confirmation dialogs
8. **🔧 Repair Selected**: Fix corrupted installations with detailed logging

### 🤖 **Enhanced AI Research Workflow**
1. **Check Updates**: Populate the upgrade list with sleek progress indicator
2. **Select Packages**: Choose packages using improved checkboxes in modern interface
3. **AI Analysis**: Click "🤖 AI Research" for comprehensive application and upgrade analysis
4. **Two-Stage Process**: Perplexity researches facts, Claude formats professional reports
5. **Rich Reports**: Review color-coded reports with emoji indicators and visual hierarchy
6. **Professional Export**: Save beautifully formatted markdown reports with metadata
7. **Individual Reports**: Automatic saving of individual package reports in AI_Reports directory
8. **Status Links**: Click "📄 View Report" in status column to instantly open saved reports
9. **Executive Summary**: Get recommendation counts and professional formatting
10. **Persistent Access**: Reports remain accessible even after closing the application

### 📤 Export & Configuration
- **📤 Export**: Save package lists and AI research to validated file paths
- **📄 Logs**: Toggle collapsible logging panel with real-time updates
- **⚙️ Settings**: Configure UI mode, API keys, and AI providers securely
- **AI Settings**: Choose between Claude and Perplexity with model selection
- **Source Selection**: winget, msstore, or all sources with validation
- **Debug Logging**: Enhanced diagnostic information for troubleshooting

### 🔒 Security Features
- **Secure API Key Storage**: Keys stored encrypted in settings.json
- **Command Validation**: All winget commands validated before execution
- **Path Sanitization**: File operations protected against traversal attacks
- **Thread Safety**: Synchronized operations prevent race conditions
- **Error Handling**: Comprehensive exception management with logging

## 🔧 Configuration

### UI Modes
- **Simple Mode**: Basic upgrade functionality with essential security features
- **Advanced Mode**: Full feature set with AI integration and advanced controls

### AI Configuration
- **Claude Models**: Sonnet 4 (default), 3.5 Sonnet, 3.5 Haiku, 3 Opus
- **Perplexity**: Real-time web research with Sonar model
- **API Keys**: Securely configured through password-masked dialogs
- **Provider Selection**: Switch between Claude and Perplexity in settings
- **Request Throttling**: Thread-safe HTTP client with semaphore-based limiting

### Security Settings
- **API Key Management**: Secure storage with encryption in settings.json
- **Command Validation**: Whitelist-based winget command filtering
- **File Path Validation**: Protection against directory traversal attacks
- **Logging Level**: Configurable debug and operational logging
- **Thread Safety**: Synchronized operations for multi-threaded stability

## 📁 Project Structure

```
UpgradeApp/
├── MainForm.cs            # Modern UI with service integration
├── Models/                # Data models and entities
│   └── UpgradableApp.cs   # Package data model
├── Services/              # Business logic services
│   ├── PackageService.cs  # Package management operations
│   ├── AIService.cs       # AI integration and recommendations
│   ├── ReportService.cs   # AI report management
│   └── SettingsService.cs # Configuration and API keys
├── Utils/                 # Utility classes
│   └── FileUtils.cs       # File operation helpers
├── docs/                  # Comprehensive documentation
│   ├── README.md          # Documentation hub and index
│   ├── USER_GUIDE.md      # End-user guide and tutorials
│   ├── API_REFERENCE.md   # Detailed API documentation
│   ├── PROJECT_STRUCTURE.md # Architecture and organization
│   ├── SECURITY.md        # Security features and best practices
│   └── DEPLOYMENT.md      # Build and deployment guide
├── UpgradeApp.csproj      # Project configuration
├── settings.json          # Secure user settings (auto-generated)
├── AI_Reports/            # Individual AI research reports (auto-generated)
│   ├── PackageName1_YYYYMMDD_HHMMSS.md
│   ├── PackageName2_YYYYMMDD_HHMMSS.md
│   └── ...
├── installer.wxs          # WiX installer configuration
├── README.md             # This documentation
└── DOCUMENTATION.md      # Comprehensive technical documentation
```

### 🏗️ Modular Architecture

#### **Models Layer**
- **`UpgradableApp.cs`**: Clean data model representing Windows packages with properties for Name, ID, Version, Status, and AI recommendations

#### **Services Layer** 
- **`PackageService.cs`**: Handles all winget operations including listing, upgrading, installing, uninstalling, and repairing packages
- **`AIService.cs`**: Manages AI integration with Claude and Perplexity APIs for intelligent package recommendations
- **`ReportService.cs`**: Handles AI report generation, saving, and management with markdown formatting
- **`SettingsService.cs`**: Manages application configuration, API keys, and user preferences with secure storage

#### **UI Layer**
- **`MainForm.cs`**: Modern Windows Forms interface using service classes with Claude-inspired design
- **`SpinningProgressForm.cs`**: Custom progress dialog with animated WingetWizard logo and status updates

#### **Utilities Layer**
- **`FileUtils.cs`**: Common file operations including safe reading/writing, directory management, and path validation

### 🔧 Key Architectural Benefits
- **Separation of Concerns**: Business logic separated from UI code
- **Single Responsibility**: Each service has a focused, well-defined purpose  
- **Dependency Injection**: Services are injected into the main form for better testability
- **Thread Safety**: Proper async/await patterns throughout all services
- **Error Handling**: Comprehensive try-catch blocks in all service methods
- **Modern Patterns**: Following contemporary C# best practices and design patterns

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes following security best practices
4. Test thoroughly including security scenarios
5. Ensure all security validations pass
6. Submit a pull request with security impact assessment

### Security Guidelines
- Follow secure coding practices
- Validate all user inputs
- Use parameterized commands
- Implement proper error handling
- Test for common vulnerabilities (OWASP Top 10)

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **Anthropic** for Claude AI integration and security guidance
- **Perplexity** for real-time web research capabilities
- **Microsoft** for Windows Package Manager and .NET security features
- **Security Community** for vulnerability research and best practices
- **Contributors** for code reviews and security improvements

## 📞 Support & Documentation

### 📚 Comprehensive Documentation
WingetWizard includes extensive documentation to help you get the most out of the application:

- **[Documentation Hub](docs/README.md)** - Central index for all documentation
- **[User Guide](docs/USER_GUIDE.md)** - Complete tutorial and help for end users
- **[API Reference](docs/API_REFERENCE.md)** - Detailed technical documentation
- **[Security Guide](docs/SECURITY.md)** - Security features and best practices
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Build and installation instructions

### 🆘 Getting Help
- **Built-in Help**: Click the **❓ Help** button in the application for quick reference
- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Ask questions and share tips
- **Documentation**: Check the comprehensive guides in the `docs/` directory

### 💡 Quick Help Topics
- **Installation Issues**: See [Deployment Guide](docs/DEPLOYMENT.md#troubleshooting)
- **API Configuration**: Check [User Guide - AI Setup](docs/USER_GUIDE.md#ai-configuration)
- **Package Problems**: Review [User Guide - Troubleshooting](docs/USER_GUIDE.md#troubleshooting)
- **Security Questions**: Read [Security Documentation](docs/SECURITY.md)

---

## 🔒 Security Notice

WingetWizard has undergone comprehensive security hardening including:
- **CWE-78**: Command injection prevention
- **CWE-22**: Path traversal protection  
- **CWE-362**: Thread safety implementation
- **CWE-209**: Information exposure mitigation
- **CWE-311**: Secure API key storage

For security issues, please report responsibly through GitHub issues.

---

**Built with ❤️ and 🔒 by Mark Relph (GeekSuave Labs) using Q Developer, Claude and Cursor**  
**v2.1 - Now with Native OS Theme Support & Dark Mode Window Chrome! WingetWizard makes secure package management magical! 🧿**