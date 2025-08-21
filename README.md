# 🚀 UpgradeApp - AI-Enhanced Windows Package Manager

A modern, AI-powered Windows Forms application for managing winget package updates with intelligent upgrade recommendations.

## ✨ Features

### Core Functionality
- **Package Management**: Check, upgrade, install, and uninstall Windows packages via winget
- **Batch Operations**: Upgrade multiple selected packages or all packages at once
- **Real-time Logging**: Comprehensive logging with collapsible panel
- **Export Capabilities**: Export package lists and AI research to files

### AI Integration
- **Claude AI Research**: Get detailed upgrade recommendations powered by Claude Sonnet 4
- **Security Analysis**: AI-powered security and compatibility assessments
- **Markdown Export**: Save AI research reports in structured markdown format
- **Multiple AI Models**: Support for various Claude models (Sonnet 4, Sonnet 3.5, Haiku, Opus)

### Modern UI
- **Dark Theme**: Professional dark mode interface
- **Responsive Design**: Modern, clean layout with proper spacing
- **Dual UI Modes**: Simple mode for basic users, Advanced mode for power users
- **Interactive Elements**: Hover effects and modern button styling

## 🛠️ Technical Stack

- **Framework**: .NET 6 Windows Forms
- **Language**: C# with modern syntax features
- **AI Integration**: Anthropic Claude API
- **Package Manager**: Windows Package Manager (winget)
- **Architecture**: Single-file executable with self-contained deployment

## 📋 Requirements

- Windows 10/11
- .NET 6 Runtime (or self-contained build)
- Windows Package Manager (winget)
- Anthropic API key for AI features

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd UpgradeApp
   ```

2. **Configure API Key**
   Create `config.json` in the application directory:
   ```json
   {
     "AnthropicApiKey": "your-claude-api-key-here"
   }
   ```

3. **Build and Run**
   ```bash
   dotnet build
   dotnet run
   ```

## 📖 Usage

### Basic Operations
1. **Check Updates**: Click "Check Updates" to scan for available package updates
2. **Select Packages**: Use checkboxes to select packages for upgrade
3. **Upgrade**: Click "Upgrade Selected" or "Upgrade All"
4. **AI Research**: Select packages and click "AI Research" for detailed analysis

### Advanced Features
- **Export Lists**: Export package information to text files
- **AI Analysis**: Get comprehensive upgrade recommendations with security insights
- **Logging**: View detailed operation logs in the collapsible panel
- **Settings**: Configure UI mode and AI model preferences

## 🔧 Configuration

### UI Modes
- **Simple Mode**: Basic upgrade functionality only
- **Advanced Mode**: Full feature set with AI integration and advanced controls

### AI Models
- Claude Sonnet 4 (default)
- Claude 3.5 Sonnet
- Claude 3.5 Haiku
- Claude 3 Opus

## 📁 Project Structure

```
UpgradeApp/
├── UpgradeBot.cs          # Main application code
├── UpgradeApp.csproj      # Project configuration
├── config.json           # API configuration (not in repo)
├── settings.json          # User settings (not in repo)
└── installer.wxs          # WiX installer configuration
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **Anthropic** for Claude AI integration
- **Microsoft** for Windows Package Manager
- **Community** for feedback and suggestions

## 📞 Support

For issues, questions, or feature requests, please open an issue on GitHub.

---

**Built with ❤️ using C# and Claude AI**