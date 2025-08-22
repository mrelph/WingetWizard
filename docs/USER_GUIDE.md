# 📖 WingetWizard User Guide

## 🌟 Getting Started

### First Launch

When you launch WingetWizard for the first time, you'll be greeted with:

1. **Welcome Screen**: A personalized greeting based on the time of day
2. **Action Cards**: Five suggested actions to get you started
3. **Clean Interface**: Logs are hidden by default for a cleaner experience

### Initial Setup

#### 1. Configure API Keys (Required for AI Features)

1. Click the **⚙️ Settings** button in the toolbar
2. Enter your API keys:
   - **Anthropic API Key**: For Claude AI analysis (required)
   - **Perplexity API Key**: For real-time web research (optional)
3. Choose your AI provider and model preferences
4. Click **Save** to store your configuration securely

#### 2. Choose UI Mode

- **Simple Mode**: Basic package operations with essential features
- **Advanced Mode**: Full feature set with AI integration and detailed controls

---

## 📦 Package Management

### Checking for Updates

1. Click **🔄 Check Updates** or use the welcome card
2. Select your package source:
   - **winget**: Windows Package Manager packages
   - **msstore**: Microsoft Store apps
   - **all**: Both sources combined
3. Wait for the scan to complete (progress bar shows status)
4. Review the list of available updates

### Viewing All Installed Apps

1. Click **📋 List All Apps**
2. Choose your preferred source
3. Browse through your complete software inventory
4. Use the list to identify packages for operations

### Upgrading Packages

#### Upgrade Selected Packages
1. Check the boxes next to packages you want to update
2. Click **📦 Upgrade Selected**
3. Monitor progress in the logs panel (click **📄 Logs** to expand)

#### Upgrade All Packages
1. Click **🚀 Upgrade All** for a complete system update
2. Confirm the operation when prompted
3. Monitor the bulk update progress

### Installing New Packages

1. Use **📋 List All Apps** to find available packages
2. Check the boxes for packages you want to install
3. Click **📦 Install Selected**
4. Follow any installation prompts

### Uninstalling Packages

1. Select packages to remove using checkboxes
2. Click **🗑️ Uninstall Selected**
3. Confirm the removal when prompted
4. Review the uninstallation results

### Repairing Packages

1. Select packages with issues using checkboxes
2. Click **🔧 Repair Selected**
3. Wait for the repair process to complete
4. Check the logs for detailed repair information

---

## 🤖 AI-Powered Analysis

### Getting AI Recommendations

#### Two-Stage AI Process
1. **Research Stage**: Perplexity gathers current information about packages
2. **Formatting Stage**: Claude creates professional analysis reports

#### Running AI Analysis
1. First, run **🔄 Check Updates** to populate the package list
2. Select packages for analysis using checkboxes
3. Click **🤖 AI Research** to start the analysis
4. Wait for the comprehensive analysis to complete

### Understanding AI Reports

#### Report Sections
Each AI report contains 7 structured sections:

1. **🎯 Executive Summary**
   - Overall recommendation (🟢 Recommended, 🟡 Caution, 🔴 Not Recommended)
   - Key points and priority level

2. **🔄 Version Changes**
   - What's new in the available version
   - Type of update (major, minor, patch, security)

3. **⚡ Key Improvements**
   - New features and enhancements
   - Performance improvements
   - Bug fixes

4. **🔒 Security Assessment**
   - Security vulnerabilities addressed
   - Risk level indicators
   - Security recommendations

5. **⚠️ Compatibility & Risks**
   - Potential breaking changes
   - System compatibility
   - Migration considerations

6. **📅 Timeline Recommendations**
   - Urgency level for updates
   - Suggested timing for upgrades

7. **🎯 Action Items**
   - Specific steps to take
   - Checklist format for easy follow-up

### Accessing Saved Reports

#### Status Column Links
- Look for **📄 View Report** links in the Status column
- Click to instantly open saved reports
- Reports open in your default markdown viewer

#### AI_Reports Directory
- Individual reports are automatically saved
- File format: `PackageName_YYYYMMDD_HHMMSS.md`
- Located in the AI_Reports folder next to the application

---

## 📤 Export and Reporting

### Exporting Package Lists

1. Click **📤 Export** after running any package operation
2. Choose your save location
3. Select file format (typically markdown for AI reports)
4. The export includes:
   - Package inventory
   - AI recommendations (if generated)
   - Executive summary
   - Metadata and timestamps

### Comprehensive AI Reports

AI exports include:
- **Executive Summary**: Overview of all recommendations
- **Individual Package Analysis**: Detailed analysis for each package
- **Metadata**: Generation timestamp, AI models used, package counts
- **Professional Formatting**: Color-coded recommendations with emoji indicators

---

## 🔧 Settings and Configuration

### AI Configuration

#### Provider Selection
- **Claude**: Knowledge-based analysis using training data
- **Perplexity**: Real-time web research with current information
- **Dual Mode**: Use both for comprehensive analysis

#### Model Selection
- **Claude Sonnet 4**: Latest and most capable (default)
- **Claude 3.5 Sonnet**: Fast and efficient
- **Claude 3.5 Haiku**: Quick responses

#### API Key Management
- Keys are stored securely in `settings.json`
- Never shared or transmitted except to official APIs
- Can be updated or removed at any time

### UI Preferences

#### Mode Selection
- **Simple Mode**: Streamlined interface for basic operations
- **Advanced Mode**: Full feature access with AI integration

#### Logging Options
- **Standard Logging**: Basic operation information
- **Verbose Logging**: Detailed diagnostic information
- **Hidden by Default**: Logs panel starts collapsed for cleaner UI

---

## 🔍 Troubleshooting

### Common Issues

#### "No packages found"
- **Cause**: winget not installed or not in PATH
- **Solution**: Install Windows Package Manager from Microsoft Store
- **Alternative**: Use Windows 11 (winget included) or install manually

#### "API key not configured"
- **Cause**: Missing or invalid API keys
- **Solution**: 
  1. Go to Settings
  2. Enter valid API keys
  3. Verify keys are active and have sufficient credits

#### "Command failed to execute"
- **Cause**: Insufficient permissions or corrupted winget installation
- **Solution**:
  1. Run WingetWizard as Administrator
  2. Restart Windows Package Manager service
  3. Reinstall winget if necessary

#### "Reports not saving"
- **Cause**: Insufficient disk space or permissions
- **Solution**:
  1. Check available disk space
  2. Verify write permissions in application directory
  3. Run as Administrator if necessary

### Performance Issues

#### Slow AI Analysis
- **Cause**: API rate limits or network connectivity
- **Solution**:
  1. Check internet connection
  2. Verify API key limits/credits
  3. Reduce number of packages analyzed simultaneously

#### Slow Package Operations
- **Cause**: Large package databases or system resources
- **Solution**:
  1. Close unnecessary applications
  2. Use specific sources instead of "all"
  3. Enable verbose logging to identify bottlenecks

### Error Messages

#### "Thread-safe operation required"
- **Cause**: UI update from background thread
- **Solution**: Restart the application (usually self-resolving)

#### "File not found" errors
- **Cause**: Missing dependencies or corrupted installation
- **Solution**:
  1. Verify .NET 6 runtime is installed
  2. Reinstall WingetWizard
  3. Check antivirus exclusions

---

## 💡 Tips and Best Practices

### Efficient Package Management

1. **Regular Updates**: Run weekly update checks for security
2. **Selective Updates**: Use AI analysis to prioritize critical updates
3. **Backup First**: Create system restore points before major updates
4. **Test Updates**: Update non-critical packages first

### AI Analysis Best Practices

1. **Batch Analysis**: Analyze multiple packages together for efficiency
2. **Review Reports**: Read AI recommendations before taking action
3. **Save Reports**: Keep important analysis reports for future reference
4. **Stay Informed**: Use Perplexity mode for latest security information

### Security Recommendations

1. **API Key Security**: Never share API keys or commit them to version control
2. **Administrator Rights**: Only use when necessary for package operations
3. **Source Verification**: Prefer official package sources (winget over third-party)
4. **Regular Scans**: Monitor for security updates using AI analysis

### Performance Optimization

1. **Source Selection**: Use specific sources instead of "all" for faster operations
2. **Logging Management**: Use verbose logging only when troubleshooting
3. **Batch Operations**: Group similar operations together
4. **Resource Management**: Close unnecessary applications during bulk operations

---

## 📞 Getting Help

### Built-in Help

1. **Help Button**: Click **❓ Help** for quick reference
2. **Tooltips**: Hover over buttons for contextual information
3. **Status Messages**: Check the status bar for operation feedback

### Documentation

- **README.md**: Project overview and quick start
- **DOCUMENTATION.md**: Comprehensive technical documentation
- **API_REFERENCE.md**: Detailed service and method documentation
- **USER_GUIDE.md**: This guide for end users

### Community Support

- **GitHub Issues**: Report bugs and request features
- **Discussions**: Ask questions and share tips
- **Documentation**: Contribute improvements and corrections

---

## 🔄 Updates and Maintenance

### Updating WingetWizard

1. **Manual Updates**: Replace executable with newer version
2. **Configuration Preservation**: Settings automatically carry forward
3. **Report Compatibility**: Existing AI reports remain accessible

### Maintenance Tasks

1. **Clean Reports**: Periodically review and clean old AI reports
2. **Update API Keys**: Refresh keys before expiration
3. **Check Dependencies**: Ensure winget and .NET runtime are current
4. **Backup Settings**: Save settings.json for disaster recovery

---

**Need more help?** Check the comprehensive documentation or open an issue on GitHub.

---

**Last Updated**: January 2025  
**Version**: 2.1  
**Guide Type**: End User Documentation