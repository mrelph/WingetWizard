# 🧿 WingetWizard - AI-Enhanced Package Manager

**AI-Powered Windows Package Management with Claude and AWS Bedrock Integration**

WingetWizard is a sophisticated Windows Forms application that combines the power of winget with advanced AI capabilities, providing intelligent package management, upgrade recommendations, and comprehensive system analysis.

## ✨ Key Features

### 🤖 **Advanced AI Integration**
- **Configurable Primary/Fallback LLM Providers**: Choose between Anthropic Claude Direct API or AWS Bedrock as your primary LLM
- **Automatic Fallback**: Seamless fallback to secondary provider if primary fails
- **Two-Stage AI Analysis**: Perplexity provides research data, primary LLM generates professional reports
- **Multiple Authentication Methods**: Support for both Bedrock API keys and full AWS credentials

### 🎨 **Modern UI & Theme Integration**
- **Native Windows Theme Support**: Automatically detects and matches your OS dark/light mode preference
- **Dark Mode Window Chrome**: Title bar, minimize/maximize/close buttons match system appearance
- **Claude-Inspired Design**: Modern, sophisticated interface with professional aesthetics
- **Responsive Layout**: Auto-sizing columns and adaptive UI elements

### 📦 **Comprehensive Package Management**
- **Multi-Source Support**: winget, Microsoft Store, and combined sources
- **AI-Powered Research**: Get intelligent upgrade recommendations and security analysis
- **Batch Operations**: Upgrade, install, uninstall, and repair multiple packages
- **Export & Reporting**: Professional markdown reports with metadata and executive summaries

### 🔧 **Advanced Features**
- **Health Monitoring**: System health checks and performance metrics
- **Configuration Validation**: Automatic validation of API keys and settings
- **Virtualization Support**: Efficient handling of large package lists
- **Caching System**: Optimized performance with intelligent caching

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET 6.0 Runtime
- winget (usually pre-installed on Windows 10/11)

### Installation
1. Download the latest release from the [Releases](https://github.com/yourusername/WingetWizard/releases) page
2. Extract the ZIP file to your preferred location
3. Run `WingetWizard.exe`

### First-Time Setup
1. **Configure Primary LLM Provider**:
   - Go to ⚙️ Settings → AI Settings
   - Select your preferred primary LLM (Anthropic Claude or AWS Bedrock)
   - The other provider will automatically serve as fallback

2. **Add Required Credentials**:
   - **If Anthropic is Primary**: Add your Claude API key
   - **If Bedrock is Primary**: Add either Bedrock API key OR AWS credentials
   - **Perplexity API Key**: Required for research data (used by all configurations)

3. **Test Connection**: Use the 🔍 button to verify your Bedrock connection

## 🔑 Authentication Options

### Anthropic Claude Direct API
- **API Key**: Get from [Anthropic Console](https://console.anthropic.com/)
- **Models**: Claude 3.5 Sonnet, Claude 3.5 Haiku, Claude Sonnet 4
- **Use Case**: High-quality text generation and analysis

### AWS Bedrock
- **Option 1**: Bedrock API Key (Recommended)
  - Get from AWS Console → Bedrock → API Keys
  - Simpler than full AWS credentials
- **Option 2**: Full AWS Credentials
  - Access Key ID + Secret Access Key + Region
  - More control but requires IAM setup

### Perplexity AI
- **API Key**: Get from [Perplexity Console](https://www.perplexity.ai/settings/api)
- **Use Case**: Research and data gathering for package analysis

## 📋 Usage Guide

### Basic Operations
1. **List All Apps**: View your complete software inventory
2. **Check Updates**: Scan for available package upgrades
3. **AI Research**: Get intelligent recommendations for selected packages
4. **Batch Operations**: Select multiple packages for upgrade/install/uninstall

### AI Research Process
1. Select packages for analysis
2. Click "🤖 AI Research"
3. Perplexity gathers research data
4. Primary LLM generates professional report
5. Fallback to secondary LLM if needed
6. Individual reports saved to `AI_Reports` folder

### Theme Integration
- **Automatic Detection**: App automatically matches your Windows theme
- **Dark Mode**: Complete dark theme with native window chrome
- **Light Mode**: Clean, professional light theme
- **Consistent Theming**: All dialogs and controls follow system appearance

## 🏗️ Architecture

### Service Layer
- **PackageService**: Core winget operations
- **AIService**: Multi-provider AI integration with fallback
- **BedrockModelDiscoveryService**: Dynamic model discovery
- **HealthCheckService**: System monitoring and diagnostics
- **PerformanceMetricsService**: Performance tracking and optimization
- **ConfigurationValidationService**: Settings and API validation
- **SecureSettingsService**: Encrypted credential storage
- **ReportService**: AI report generation and management
- **SettingsService**: Configuration management
- **CachingService**: Multi-tier caching system
- **SearchFilterService**: Advanced search and filtering
- **VirtualizationService**: Large dataset handling

### Thread Safety
- **Lock-Free Operations**: Efficient concurrent package management
- **Async/Await**: Non-blocking UI operations
- **Service Isolation**: Independent service instances for stability

## 🔧 Configuration

### Settings File
- **Location**: `settings.json` in application directory
- **Primary LLM**: Stored in secure settings
- **API Keys**: Encrypted and stored securely
- **UI Preferences**: Theme, advanced mode, logging settings

### Environment Variables
- **BEDROCK_API_KEY**: Alternative to UI configuration
- **ANTHROPIC_API_KEY**: Claude API key
- **PERPLEXITY_API_KEY**: Research API key

## 📊 Performance Features

### Caching System
- **Memory Cache**: Fast access to frequently used data
- **Disk Cache**: Persistent storage for offline access
- **Auto-Cleanup**: Automatic cache management and cleanup

### Virtualization
- **Large List Support**: Efficient handling of 1000+ packages
- **Memory Optimization**: Minimal memory footprint
- **Smooth Scrolling**: Responsive UI even with large datasets

## 🚨 Troubleshooting

### Common Issues
1. **Bedrock Connection Failed**
   - Verify API key or AWS credentials
   - Check region selection
   - Ensure network connectivity

2. **No Models Available**
   - Try different AWS regions
   - Check IAM permissions for Bedrock
   - Use Bedrock API key instead of full credentials

3. **Theme Not Applied**
   - Restart the application
   - Check Windows theme settings
   - Verify .NET 6.0 runtime installation

### Debug Information
- **Logs**: View detailed logs in the application
- **Debug Output**: Check Visual Studio Output window
- **Health Check**: Use the health check feature in settings

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup
1. Clone the repository
2. Install .NET 6.0 SDK
3. Open in Visual Studio 2022 or VS Code
4. Build and run the project

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Anthropic**: Claude AI models and API
- **AWS**: Bedrock service and infrastructure
- **Perplexity**: Research and data gathering
- **Microsoft**: winget package manager
- **Community**: Contributors and feedback

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/WingetWizard/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/WingetWizard/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/WingetWizard/wiki)

---

**Built with ❤️ using .NET 6.0, Windows Forms, and cutting-edge AI technology**

*WingetWizard v2.1 - The intelligent way to manage Windows packages*