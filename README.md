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
- **Enhanced AI Prompting**: Comprehensive 7-section structured analysis with intelligent formatting
- **Rich Markdown Reports**: Color-coded recommendations with emoji indicators and professional styling
- **Persistent AI Reports**: Individual package reports automatically saved with clickable status column links
- **Status Column Integration**: "📄 View Report" links in status column for instant access to saved reports
- **Dual AI Providers**: Claude AI (knowledge-based) and Perplexity (real-time web research)
- **Security Assessment**: Vulnerability analysis with risk level indicators (🟢🟡🔴🟣)
- **Multiple AI Models**: Claude Sonnet 4, 3.5 Sonnet, 3.5 Haiku, 3 Opus
- **Intelligent Export**: Professional markdown reports with metadata and executive summaries
- **Progress Tracking**: Spinning icon progress indicators with real-time status updates
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

### 🎨 Modern Claude-Inspired Interface
- **Sophisticated Design**: Claude AI-inspired dark theme with refined color palette
- **Welcome Experience**: Personalized time-based greetings ("Good evening, Mark")
- **Smart Welcome Cards**: Interactive action suggestions when no packages are loaded
- **Modern Typography**: Calibri font with intelligent fallbacks for enhanced readability
- **Card-Based Buttons**: Elegant spacing and subtle borders with sophisticated hover effects
- **Professional Colors**: Carefully curated color scheme with blues, purples, oranges, and greens
- **Spinning Progress**: Animated logo indicators centered on main window during operations
- **Rich Text Displays**: Color-coded help dialogs and AI reports with visual hierarchy
- **Intelligent Layout**: Hidden logs panel by default, larger fonts, and generous spacing
- **Responsive Columns**: Auto-sizing columns that adapt proportionally to window changes
- **Smart Tooltips**: Helpful tooltips for all buttons when window is scaled down

## 🎨 User Experience Highlights

### 🌟 **Welcome Experience**
- **Personalized Greeting**: Time-aware welcome message with user's name
- **Action Cards**: Four elegant suggestion cards for common operations
- **Smart Visibility**: Welcome screen appears when empty, hides when packages load
- **Professional Aesthetics**: Claude AI-inspired design language throughout

### 🎯 **Modern Interactions**
- **Spinning Progress**: Animated logo indicators that center perfectly on the main window
- **Rich Text Reports**: Color-coded AI analysis with emoji indicators and visual hierarchy
- **Sophisticated Colors**: Professional palette with semantic color coding
- **Enhanced Typography**: Modern Calibri font with improved readability

### 📱 **Responsive Design**
- **Adaptive Layout**: Intelligent spacing and sizing for different screen sizes
- **Hidden-by-Default Logs**: Cleaner interface with collapsible detailed logging
- **Card-Based Actions**: Buttons with sophisticated hover effects and spacing
- **Professional Help**: Rich text help system with colors, emojis, and structure

## 🛠️ Technical Stack

- **Framework**: .NET 6 Windows Forms with modern UI enhancements
- **Typography**: Calibri font family with intelligent fallback system  
- **Design Language**: Claude AI-inspired interface with sophisticated color palette
- **AI Integration**: Enhanced prompting with structured 7-section analysis
- **Progress Indicators**: Custom spinning form with animated logo and status updates
- **Rich Text Rendering**: Color-coded markdown display with emoji support
- **Architecture**: Single-file executable with self-contained deployment
- **Security**: Thread-safe operations, input validation, secure storage

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
1. **Welcome Screen**: Greeted with personalized time-based message and action cards
2. **Quick Actions**: Click suggestion cards or use toolbar buttons to begin
3. **Smart Interface**: Logs hidden by default for clean, focused experience
4. **Progress Feedback**: Enjoy spinning logo animations during operations

### 📦 **Package Operations**
1. **🔄 Check Updates**: Scan for available package updates with animated progress
2. **📋 List All Apps**: View complete software inventory in modern interface  
3. **Select Packages**: Use checkboxes to select multiple packages for batch operations
4. **📦 Upgrade Selected**: Update only checked packages with real-time status
5. **🚀 Upgrade All**: Update all available packages with comprehensive progress tracking
6. **📦 Install Selected**: Install new packages with validation and confirmation
7. **🗑️ Uninstall Selected**: Remove packages safely with confirmation dialogs
8. **🔧 Repair Selected**: Fix corrupted installations with detailed logging

### 🤖 **Enhanced AI Research Workflow**
1. **Check Updates**: Populate the upgrade list with spinning progress indicator
2. **Select Packages**: Choose packages using improved checkboxes in modern interface
3. **AI Analysis**: Click "🤖 AI Research" for comprehensive 7-section analysis
4. **Rich Reports**: Review color-coded reports with emoji indicators and visual hierarchy
5. **Professional Export**: Save beautifully formatted markdown reports with metadata
6. **Individual Reports**: Automatic saving of individual package reports in AI_Reports directory
7. **Status Links**: Click "📄 View Report" in status column to instantly open saved reports
8. **Executive Summary**: Get recommendation counts and professional formatting
9. **Persistent Access**: Reports remain accessible even after closing the application

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
├── UpgradeBot.cs          # Main application with security enhancements
├── UpgradeApp.csproj      # Project configuration
├── settings.json          # Secure user settings (auto-generated)
├── AI_Reports/            # Individual AI research reports (auto-generated)
│   ├── PackageName1_YYYYMMDD_HHMMSS.md
│   ├── PackageName2_YYYYMMDD_HHMMSS.md
│   └── ...
├── installer.wxs          # WiX installer configuration
└── README.md             # This documentation
```

### Key Components
- **UpgradeBot.cs**: Main Windows Forms application with:
  - Secure winget command execution
  - Thread-safe AI API integration
  - Protected file operations
  - Comprehensive error handling
  - Real-time logging system

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

## 📞 Support

For issues, questions, or feature requests, please open an issue on GitHub.

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

**Built with ❤️ and 🔒 by Mark Relph (GeekSuave Labs) using Q Developer, Claude and Cursor - WingetWizard makes secure package management magical! 🧿**