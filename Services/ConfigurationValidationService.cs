using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WingetWizard.Utils;

namespace WingetWizard.Services
{
    /// <summary>
    /// Provides comprehensive configuration validation for the WingetWizard application.
    /// Validates all settings, API keys, and configuration files with detailed error reporting.
    /// </summary>
    public class ConfigurationValidationService
    {
        private readonly SettingsService _settingsService;
        private readonly SecureSettingsService _secureSettingsService;
        private readonly List<string> _validationErrors = new();
        private readonly List<string> _validationWarnings = new();

        public ConfigurationValidationService(SettingsService settingsService, SecureSettingsService secureSettingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _secureSettingsService = secureSettingsService ?? throw new ArgumentNullException(nameof(secureSettingsService));
        }

        /// <summary>
        /// Performs comprehensive configuration validation.
        /// </summary>
        /// <returns>Configuration validation result with detailed information</returns>
        public ConfigurationValidationResult ValidateConfiguration()
        {
            _validationErrors.Clear();
            _validationWarnings.Clear();

            try
            {
                // Validate core settings
                ValidateCoreSettings();
                
                // Validate API configuration
                ValidateApiConfiguration();
                
                // Validate file paths and permissions
                ValidateFilePaths();
                
                // Validate application dependencies
                ValidateDependencies();
                
                // Validate security settings
                ValidateSecuritySettings();
                
                // Validate performance settings
                ValidatePerformanceSettings();
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Configuration validation failed with exception: {ex.Message}");
            }

            return new ConfigurationValidationResult
            {
                IsValid = _validationErrors.Count == 0,
                Errors = new List<string>(_validationErrors),
                Warnings = new List<string>(_validationWarnings),
                ValidationTimestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Validates core application settings.
        /// </summary>
        private void ValidateCoreSettings()
        {
            try
            {
                var settings = _settingsService.GetAllSettings();
                
                // Check if settings file exists and is readable
                if (settings == null || settings.Count == 0)
                {
                    _validationErrors.Add("Settings file is empty or corrupted");
                    return;
                }

                // Validate required core settings
                var requiredSettings = new Dictionary<string, string?>
                {
                    { "isAdvancedMode", _settingsService.GetSetting("isAdvancedMode", true).ToString() },
                    { "selectedAiModel", _settingsService.GetSetting("selectedAiModel", "claude-sonnet-4-20250514") },
                    { "verboseLogging", _settingsService.GetSetting("verboseLogging", false).ToString() }
                };

                if (!ValidationUtils.ValidateRequiredFields(requiredSettings, _validationErrors))
                {
                    return; // Stop validation if required fields are missing
                }

                // Validate boolean settings
                var advancedMode = ValidationUtils.ValidateBooleanInput(requiredSettings["isAdvancedMode"], _validationErrors);
                var verboseLogging = ValidationUtils.ValidateBooleanInput(requiredSettings["verboseLogging"], _validationErrors);

                if (advancedMode == null || verboseLogging == null)
                {
                    return; // Stop validation if boolean parsing failed
                }

                // Validate AI model selection
                var aiModel = requiredSettings["selectedAiModel"];
                if (!string.IsNullOrEmpty(aiModel))
                {
                    var validModels = new[] { "claude-sonnet-4-20250514", "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20240307" };
                    if (!validModels.Contains(aiModel))
                    {
                        _validationWarnings.Add($"AI model '{aiModel}' may not be supported. Valid models: {string.Join(", ", validModels)}");
                    }
                }

                // Validate primary LLM provider setting
                var primaryLLMProvider = _secureSettingsService.GetApiKey("PrimaryLLMProvider");
                if (!string.IsNullOrEmpty(primaryLLMProvider))
                {
                    var validProviders = new[] { "Anthropic (Claude Direct)", "AWS Bedrock" };
                    if (!validProviders.Contains(primaryLLMProvider))
                    {
                        _validationWarnings.Add($"Primary LLM provider '{primaryLLMProvider}' is not recognized. Valid providers: {string.Join(", ", validProviders)}");
                    }
                }
                else
                {
                    _validationWarnings.Add("Primary LLM provider is not configured. Defaulting to Anthropic Claude.");
                }

                // Validate settings count
                if (settings.Count < 3)
                {
                    _validationWarnings.Add($"Expected at least 3 settings, found {settings.Count}");
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Core settings validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates API configuration and keys.
        /// </summary>
        private void ValidateApiConfiguration()
        {
            try
            {
                // Validate Claude API key
                var claudeKey = _secureSettingsService.GetApiKey("AnthropicApiKey");
                var validatedClaudeKey = ValidationUtils.ValidateApiKey(claudeKey, _validationErrors);
                
                if (validatedClaudeKey != null)
                {
                    // Additional Claude API key validation
                    if (validatedClaudeKey.Length < 50)
                    {
                        _validationWarnings.Add("Claude API key appears to be shorter than expected. Please verify the key is correct.");
                    }
                }

                // Validate Perplexity API key
                var perplexityKey = _secureSettingsService.GetApiKey("PerplexityApiKey");
                var validatedPerplexityKey = ValidationUtils.ValidateApiKey(perplexityKey, _validationErrors);
                
                if (validatedPerplexityKey != null)
                {
                    // Additional Perplexity API key validation
                    if (validatedPerplexityKey.Length < 40)
                    {
                        _validationWarnings.Add("Perplexity API key appears to be shorter than expected. Please verify the key is correct.");
                    }
                }

                // Validate Bedrock credentials
                var (bedrockAccessKey, bedrockSecretKey, bedrockRegion, bedrockModel) = _secureSettingsService.GetBedrockCredentials();
                var bedrockApiKey = _secureSettingsService.GetApiKey("BedrockApiKey");
                
                var hasBedrockCredentials = !string.IsNullOrEmpty(bedrockApiKey) || 
                                         (!string.IsNullOrEmpty(bedrockAccessKey) && !string.IsNullOrEmpty(bedrockSecretKey));
                
                if (hasBedrockCredentials)
                {
                    if (!string.IsNullOrEmpty(bedrockApiKey))
                    {
                        // Validate Bedrock API key
                        var validatedBedrockKey = ValidationUtils.ValidateApiKey(bedrockApiKey, _validationErrors);
                        if (validatedBedrockKey != null && validatedBedrockKey.Length < 20)
                        {
                            _validationWarnings.Add("Bedrock API key appears to be shorter than expected. Please verify the key is correct.");
                        }
                    }
                    else if (!string.IsNullOrEmpty(bedrockAccessKey) && !string.IsNullOrEmpty(bedrockSecretKey))
                    {
                        // Validate AWS credentials
                        if (bedrockAccessKey.Length < 20)
                        {
                            _validationWarnings.Add("AWS Access Key ID appears to be shorter than expected. Please verify the key is correct.");
                        }
                        if (bedrockSecretKey.Length < 40)
                        {
                            _validationWarnings.Add("AWS Secret Access Key appears to be shorter than expected. Please verify the key is correct.");
                        }
                        if (string.IsNullOrEmpty(bedrockRegion))
                        {
                            _validationWarnings.Add("AWS region is not configured for Bedrock access.");
                        }
                    }
                }

                // Check if at least one API key is configured
                var hasAnyApiKey = !string.IsNullOrEmpty(claudeKey) || !string.IsNullOrEmpty(perplexityKey) || hasBedrockCredentials;
                if (!hasAnyApiKey)
                {
                    _validationWarnings.Add("No API keys are configured. AI features will not be available.");
                }

                // Validate API endpoints (basic format check)
                var apiEndpoints = new[]
                {
                    ("Claude API", "https://api.anthropic.com"),
                    ("Perplexity API", "https://api.perplexity.ai")
                };

                foreach (var (name, endpoint) in apiEndpoints)
                {
                    var validatedEndpoint = ValidationUtils.ValidateUrl(endpoint, _validationErrors);
                    if (validatedEndpoint == null)
                    {
                        _validationErrors.Add($"Invalid {name} endpoint: {endpoint}");
                    }
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"API configuration validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates file paths and permissions.
        /// </summary>
        private void ValidateFilePaths()
        {
            try
            {
                var appPath = Application.StartupPath;
                
                // Validate application directory
                if (!Directory.Exists(appPath))
                {
                    _validationErrors.Add($"Application directory does not exist: {appPath}");
                    return;
                }

                // Test application directory access
                try
                {
                    var testFile = Path.Combine(appPath, $"config_validation_test_{Guid.NewGuid():N}.tmp");
                    File.WriteAllText(testFile, "Configuration validation test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    _validationErrors.Add($"Cannot write to application directory: {ex.Message}");
                }

                // Validate AI Reports directory
                var reportsPath = Path.Combine(appPath, "AI_Reports");
                if (Directory.Exists(reportsPath))
                {
                    try
                    {
                        var reportCount = Directory.GetFiles(reportsPath, "*.md").Length;
                        if (reportCount > 1000)
                        {
                            _validationWarnings.Add($"Large number of AI reports found ({reportCount}). Consider cleanup for performance.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _validationWarnings.Add($"Cannot access AI Reports directory: {ex.Message}");
                    }
                }
                else
                {
                    _validationWarnings.Add("AI Reports directory does not exist. It will be created when needed.");
                }

                // Validate settings file
                var settingsPath = Path.Combine(appPath, "settings.json");
                if (File.Exists(settingsPath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(settingsPath);
                        if (fileInfo.Length > 1024 * 1024) // 1MB
                        {
                            _validationWarnings.Add($"Settings file is unusually large ({fileInfo.Length / 1024}KB). Consider review.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _validationWarnings.Add($"Cannot access settings file: {ex.Message}");
                    }
                }
                else
                {
                    _validationWarnings.Add("Settings file does not exist. Default settings will be used.");
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"File path validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates application dependencies.
        /// </summary>
        private void ValidateDependencies()
        {
            try
            {
                // Check winget availability
                try
                {
                    var wingetProcess = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(wingetProcess);
                    if (process != null)
                    {
                        process.WaitForExit(5000);
                        if (process.ExitCode != 0)
                        {
                            _validationErrors.Add("Winget is not available or not functioning properly");
                        }
                    }
                    else
                    {
                        _validationErrors.Add("Failed to start winget process");
                    }
                }
                catch (Exception ex)
                {
                    _validationErrors.Add($"Winget dependency check failed: {ex.Message}");
                }

                // Check PowerShell availability
                try
                {
                    var psProcess = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = "-Command \"$PSVersionTable.PSVersion.ToString()\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(psProcess);
                    if (process != null)
                    {
                        process.WaitForExit(5000);
                        if (process.ExitCode != 0)
                        {
                            _validationWarnings.Add("PowerShell may not be available or functioning properly");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _validationWarnings.Add($"PowerShell dependency check failed: {ex.Message}");
                }

                // Check .NET runtime
                try
                {
                    var dotnetVersion = Environment.Version.ToString();
                    if (!dotnetVersion.StartsWith("6.") && !dotnetVersion.StartsWith("7.") && !dotnetVersion.StartsWith("8."))
                    {
                        _validationWarnings.Add($"Unexpected .NET version: {dotnetVersion}. .NET 6+ is recommended.");
                    }
                }
                catch (Exception ex)
                {
                    _validationWarnings.Add($"Runtime version check failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Dependency validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates security-related settings.
        /// </summary>
        private void ValidateSecuritySettings()
        {
            try
            {
                // Check if running with appropriate permissions
                try
                {
                    using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    var isElevated = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                    
                    if (!isElevated)
                    {
                        _validationWarnings.Add("Not running with elevated permissions. Some package operations may fail.");
                    }
                }
                catch (Exception ex)
                {
                    _validationWarnings.Add($"Permission check failed: {ex.Message}");
                }

                // Check for sensitive information in settings
                var settings = _settingsService.GetAllSettings();
                var sensitiveKeys = new[] { "password", "secret", "token", "key" };
                
                foreach (var setting in settings)
                {
                    if (sensitiveKeys.Any(key => setting.Key.ToLowerInvariant().Contains(key)))
                    {
                        if (setting.Value != null && setting.Value.ToString()?.Length > 0)
                        {
                            _validationWarnings.Add($"Sensitive setting '{setting.Key}' is configured. Ensure proper security measures.");
                        }
                    }
                }

                // Validate file permissions
                var appPath = Application.StartupPath;
                try
                {
                    var directoryInfo = new DirectoryInfo(appPath);
                    var permissions = directoryInfo.Attributes;
                    
                    if ((permissions & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        _validationWarnings.Add("Application directory is read-only. Some features may not work properly.");
                    }
                }
                catch (Exception ex)
                {
                    _validationWarnings.Add($"Permission validation failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Security validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates performance-related settings.
        /// </summary>
        private void ValidatePerformanceSettings()
        {
            try
            {
                // Check verbose logging setting
                var verboseLogging = _settingsService.GetSetting("verboseLogging", false);
                if (verboseLogging)
                {
                    _validationWarnings.Add("Verbose logging is enabled. This may impact performance.");
                }

                // Check advanced mode setting
                var advancedMode = _settingsService.GetSetting("isAdvancedMode", true);
                if (advancedMode)
                {
                    _validationWarnings.Add("Advanced mode is enabled. This may increase memory usage.");
                }

                // Validate memory settings
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var workingSetMB = currentProcess.WorkingSet64 / 1024 / 1024;
                
                if (workingSetMB > 500)
                {
                    _validationWarnings.Add($"High memory usage detected: {workingSetMB}MB. Consider restarting the application.");
                }

                // Check disk space
                try
                {
                    var driveInfo = new DriveInfo(Path.GetPathRoot(Application.StartupPath) ?? "C:");
                    var freeSpaceMB = driveInfo.AvailableFreeSpace / 1024 / 1024;
                    
                    if (freeSpaceMB < 100)
                    {
                        _validationErrors.Add($"Low disk space: {freeSpaceMB}MB available. Minimum 100MB required.");
                    }
                    else if (freeSpaceMB < 500)
                    {
                        _validationWarnings.Add($"Low disk space: {freeSpaceMB}MB available. Consider freeing up space.");
                    }
                }
                catch (Exception ex)
                {
                    _validationWarnings.Add($"Disk space check failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Performance validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a detailed validation report.
        /// </summary>
        /// <returns>Formatted validation report</returns>
        public string GetValidationReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("Configuration Validation Report");
            report.AppendLine("=============================");
            report.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Status: {(_validationErrors.Count == 0 ? "Valid" : "Invalid")}");
            report.AppendLine($"Errors: {_validationErrors.Count}, Warnings: {_validationWarnings.Count}");
            report.AppendLine();

            if (_validationErrors.Count > 0)
            {
                report.AppendLine("Critical Errors:");
                report.AppendLine("---------------");
                foreach (var error in _validationErrors)
                {
                    report.AppendLine($"❌ {error}");
                }
                report.AppendLine();
            }

            if (_validationWarnings.Count > 0)
            {
                report.AppendLine("Warnings:");
                report.AppendLine("---------");
                foreach (var warning in _validationWarnings)
                {
                    report.AppendLine($"⚠️ {warning}");
                }
                report.AppendLine();
            }

            if (_validationErrors.Count == 0 && _validationWarnings.Count == 0)
            {
                report.AppendLine("✅ Configuration is valid with no issues found.");
            }
            else if (_validationErrors.Count == 0)
            {
                report.AppendLine("✅ Configuration is valid but has warnings.");
            }
            else
            {
                report.AppendLine("❌ Configuration has critical errors that must be resolved.");
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Represents the result of a configuration validation operation.
    /// </summary>
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// Gets or sets whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets the collection of validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets the timestamp when validation was performed.
        /// </summary>
        public DateTime ValidationTimestamp { get; set; }

        /// <summary>
        /// Gets the total number of issues found.
        /// </summary>
        public int TotalIssues => Errors.Count + Warnings.Count;
    }
}
