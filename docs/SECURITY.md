# 🔒 WingetWizard Security Documentation

## Security Overview

WingetWizard has been designed with security as a primary concern, implementing multiple layers of protection to ensure safe package management operations and secure handling of sensitive data.

## 🛡️ Security Features

### Command Injection Protection (CWE-78)

#### Implementation
- **Whitelist Validation**: All PowerShell commands are validated against a predefined whitelist
- **Parameter Sanitization**: Command parameters are sanitized to prevent injection attacks
- **Process Isolation**: Commands execute in isolated PowerShell processes with restricted permissions

#### Code Example
```csharp
public string RunPowerShell(string command)
{
    if (string.IsNullOrWhiteSpace(command)) 
        return "Command is null or empty";
    
    var validCommands = new[] { "winget list", "winget upgrade", "winget install", "winget uninstall", "winget repair" };
    if (!validCommands.Any(cmd => command.TrimStart().StartsWith(cmd, StringComparison.OrdinalIgnoreCase)))
        return "Invalid command format";
    
    // Safe execution with ProcessStartInfo.ArgumentList
}
```

### Path Traversal Prevention (CWE-22)

#### File Path Validation
- **Safe Path Creation**: All file paths are validated and sanitized
- **Directory Traversal Detection**: Prevents `../` and similar path traversal attempts
- **Restricted File Operations**: File operations are restricted to application directory and designated folders

#### Implementation
```csharp
public static string CreateSafeFileName(string fileName)
{
    if (string.IsNullOrEmpty(fileName))
        return "unnamed";

    var invalidChars = Path.GetInvalidFileNameChars();
    var safeName = fileName;

    foreach (var invalidChar in invalidChars)
    {
        safeName = safeName.Replace(invalidChar, '_');
    }

    // Additional path traversal prevention
    safeName = safeName.Replace("..", "_").Replace("/", "_").Replace("\\", "_");
    
    return safeName;
}
```

### Thread Safety (CWE-362)

#### Synchronization Mechanisms
- **SemaphoreSlim**: HTTP operations are throttled to prevent race conditions
- **Lock Objects**: Critical sections use proper locking mechanisms
- **Thread-Safe Collections**: Uses thread-safe data structures where appropriate

#### Implementation
```csharp
private readonly SemaphoreSlim _httpSemaphore = new SemaphoreSlim(1, 1);
private readonly object upgradableAppsLock = new();

// Thread-safe HTTP operations
await _httpSemaphore.WaitAsync();
try
{
    // HTTP operation
}
finally
{
    _httpSemaphore.Release();
}
```

### Information Exposure Mitigation (CWE-209)

#### Error Handling
- **Sanitized Error Messages**: Error messages don't expose sensitive system information
- **Debug vs. Production**: Different error verbosity levels for development and production
- **Secure Logging**: Logs don't contain API keys or sensitive data

#### Implementation
```csharp
catch (Exception ex)
{
    // Log detailed error for debugging (development only)
    System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}");
    
    // Return user-friendly message without sensitive details
    return "Operation failed. Please check your configuration and try again.";
}
```

### Secure API Key Storage (CWE-311)

#### Storage Security
- **Local Storage**: API keys stored locally in settings.json
- **No Version Control**: .gitignore prevents accidental key commits
- **Runtime Loading**: Keys loaded only when needed
- **Secure Transmission**: Keys only sent to official API endpoints over HTTPS

#### Key Management
```csharp
public void StoreApiKey(string keyName, string value)
{
    SetSetting(keyName, value);
    SaveSettings(); // Immediately persist to disk
}

public string GetApiKey(string keyName)
{
    return GetSetting<string>(keyName, ""); // Returns empty string if not found
}
```

## 🔐 Security Best Practices

### API Key Security

#### Storage Guidelines
1. **Local Only**: Keys are never transmitted except to official APIs
2. **File Permissions**: settings.json should have restricted read permissions
3. **Backup Security**: Exclude settings.json from cloud backups containing keys
4. **Key Rotation**: Regularly rotate API keys for enhanced security

#### Usage Guidelines
1. **Minimum Permissions**: Use API keys with minimal required permissions
2. **Monitor Usage**: Regularly check API usage for anomalous activity
3. **Revoke Compromised Keys**: Immediately revoke and replace compromised keys
4. **Environment Separation**: Use different keys for development and production

### Network Security

#### HTTPS Enforcement
- **Encrypted Connections**: All API communications use HTTPS/TLS
- **Certificate Validation**: SSL certificates are properly validated
- **No Insecure Fallback**: No fallback to HTTP connections

#### Request Security
```csharp
private async Task<string> MakeApiRequestAsync(string url, object requestBody, Dictionary<string, string> headers)
{
    using var client = new HttpClient();
    
    // Set security headers
    foreach (var header in headers)
    {
        client.DefaultRequestHeaders.Add(header.Key, header.Value);
    }
    
    // Ensure HTTPS
    if (!url.StartsWith("https://"))
    {
        throw new SecurityException("Only HTTPS connections are allowed");
    }
    
    // Make secure request
}
```

### File System Security

#### Safe File Operations
1. **Path Validation**: All file paths are validated before use
2. **Permission Checks**: Verify write permissions before file operations
3. **Atomic Operations**: File operations are atomic where possible
4. **Cleanup**: Temporary files are properly cleaned up

#### Directory Security
```csharp
public static bool EnsureDirectoryExists(string path)
{
    try
    {
        // Validate path is within allowed directories
        if (!IsPathSafe(path))
        {
            return false;
        }
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return true;
    }
    catch (Exception ex)
    {
        // Log error without exposing sensitive path information
        System.Diagnostics.Debug.WriteLine($"Failed to create directory: {ex.Message}");
        return false;
    }
}
```

## 🚨 Security Monitoring

### Logging Security

#### What's Logged
- **Operation Results**: Success/failure of operations
- **Error Messages**: Sanitized error information
- **Performance Metrics**: Timing and resource usage
- **User Actions**: High-level user interactions

#### What's NOT Logged
- **API Keys**: Never logged in any form
- **Sensitive Data**: Personal information or system details
- **Full Exception Details**: In production builds
- **File Paths**: Potentially sensitive path information

### Audit Trail

#### Tracked Events
1. **API Key Changes**: When keys are added/removed/modified
2. **Configuration Changes**: Settings modifications
3. **Package Operations**: Install/uninstall/upgrade operations
4. **File Operations**: Report generation and exports
5. **Error Events**: Security-relevant errors and failures

## 🔧 Security Configuration

### Recommended Settings

#### Application Settings
```json
{
    "VerboseLogging": false,           // Disable in production
    "IsAdvancedMode": true,           // Enable for full security features
    "ValidateCommands": true,         // Always validate commands
    "RestrictFileOperations": true,   // Restrict file operations to safe paths
    "RequireHttps": true              // Enforce HTTPS for all API calls
}
```

#### File Permissions
- **settings.json**: Read/write for user only (600)
- **AI_Reports/**: Read/write for user only (700)
- **Application Directory**: Standard application permissions

### Environment Security

#### Development Environment
1. **Separate API Keys**: Use different keys for development
2. **Debug Logging**: Enable verbose logging for troubleshooting
3. **Test Data**: Use non-production data for testing
4. **Secure Development**: Follow secure coding practices

#### Production Environment
1. **Minimal Logging**: Disable verbose logging
2. **Restricted Permissions**: Run with minimal required permissions
3. **Regular Updates**: Keep dependencies and runtime updated
4. **Monitoring**: Monitor for security events and anomalies

## 🚫 Known Limitations

### Current Security Limitations

1. **Local Storage**: API keys stored in plain text locally (mitigated by file permissions)
2. **PowerShell Execution**: Requires PowerShell execution capability
3. **Network Dependency**: Requires internet access for AI features
4. **Windows-Specific**: Security model tied to Windows security features

### Planned Security Enhancements

1. **Key Encryption**: Encrypt API keys at rest using Windows DPAPI
2. **Certificate Pinning**: Pin certificates for API endpoints
3. **Audit Logging**: Enhanced security event logging
4. **Sandboxing**: Run package operations in sandboxed environment

## 🆘 Security Incident Response

### If API Keys Are Compromised

1. **Immediate Actions**:
   - Revoke compromised keys at the provider
   - Remove keys from settings.json
   - Generate new API keys
   - Update application configuration

2. **Investigation**:
   - Check API usage logs for unauthorized activity
   - Review system logs for security events
   - Determine scope and impact of compromise

3. **Recovery**:
   - Update to new API keys
   - Monitor for continued unauthorized access
   - Implement additional security measures if needed

### If System Is Compromised

1. **Immediate Actions**:
   - Disconnect from network if actively compromised
   - Revoke all API keys
   - Change all related passwords

2. **Assessment**:
   - Scan system for malware
   - Review system and application logs
   - Determine extent of compromise

3. **Recovery**:
   - Clean or restore system from backup
   - Reinstall WingetWizard from trusted source
   - Reconfigure with new API keys

## 📋 Security Checklist

### Pre-Deployment Security Review

- [ ] All API keys are properly secured
- [ ] File permissions are correctly set
- [ ] Verbose logging is disabled for production
- [ ] All dependencies are up to date
- [ ] Security features are enabled
- [ ] Error handling doesn't expose sensitive information
- [ ] Network communications use HTTPS
- [ ] Input validation is comprehensive

### Regular Security Maintenance

- [ ] Rotate API keys regularly
- [ ] Update dependencies and runtime
- [ ] Review and clean log files
- [ ] Monitor API usage for anomalies
- [ ] Backup security configurations
- [ ] Test security features functionality
- [ ] Review file permissions
- [ ] Validate security settings

## 📞 Security Support

### Reporting Security Issues

1. **GitHub Issues**: For non-sensitive security bugs
2. **Direct Contact**: For sensitive vulnerabilities
3. **Responsible Disclosure**: Follow responsible disclosure practices

### Security Resources

- **OWASP Guidelines**: Follow OWASP security best practices
- **Microsoft Security**: Windows and .NET security documentation
- **API Provider Security**: Anthropic and Perplexity security guidelines

---

**Security Version**: 2.1  
**Last Security Review**: January 2025  
**Next Review**: April 2025  
**Security Contact**: Available through GitHub Issues