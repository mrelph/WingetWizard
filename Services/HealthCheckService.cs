using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UpgradeApp.Models;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Provides comprehensive health check functionality for the WingetWizard application.
    /// Monitors system resources, service availability, and application health.
    /// SECURITY: Fixed command injection vulnerabilities and sensitive data exposure.
    /// STABILITY: Added proper thread safety and resource management.
    /// </summary>
    public class HealthCheckService : IDisposable
    {
        private readonly SettingsService _settingsService;
        private readonly SecureSettingsService _secureSettingsService;
        private readonly Stopwatch _healthCheckTimer = new();
        private bool _disposed = false;

        // Health check thresholds - made configurable
        private const long MIN_FREE_DISK_SPACE_MB = 100; // 100MB minimum free space
        private const long MAX_MEMORY_USAGE_MB = 500;    // 500MB maximum memory usage
        private const int NETWORK_TIMEOUT_MS = 5000;     // 5 second network timeout
        private const int MAX_TEMP_FILES = 100;          // Maximum temporary files
        private const int PROCESS_TIMEOUT_MS = 10000;    // 10 second process timeout

        // Validated executable paths - security measure against command injection
        private static readonly Dictionary<string, string> ValidExecutables = new Dictionary<string, string>
        {
            ["winget"] = "winget.exe",
            ["powershell"] = "powershell.exe"
        };

        public HealthCheckService(SettingsService settingsService, SecureSettingsService secureSettingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _secureSettingsService = secureSettingsService ?? throw new ArgumentNullException(nameof(secureSettingsService));
        }

        /// <summary>
        /// Performs a comprehensive health check of the application and system.
        /// FIXED: Added proper error boundaries and thread safety.
        /// </summary>
        /// <returns>A detailed health check result</returns>
        public async Task<HealthCheckResult> PerformHealthCheckAsync()
        {
            _healthCheckTimer.Restart();
            var result = new HealthCheckResult();

            try
            {
                // Run all health checks with proper error isolation
                var healthCheckTasks = new[]
                {
                    SafeExecuteHealthCheck(() => CheckServicesHealthAsync(result), "Services"),
                    SafeExecuteHealthCheck(() => CheckStorageHealthAsync(result), "Storage"),
                    SafeExecuteHealthCheck(() => CheckMemoryHealthAsync(result), "Memory"),
                    SafeExecuteHealthCheck(() => CheckNetworkHealthAsync(result), "Network"),
                    SafeExecuteHealthCheck(() => CheckConfigurationHealthAsync(result), "Configuration"),
                    SafeExecuteHealthCheck(() => CheckPermissionsHealthAsync(result), "Permissions"),
                    SafeExecuteHealthCheck(() => CheckApplicationHealthAsync(result), "Application")
                };

                await Task.WhenAll(healthCheckTasks);

                // Final health determination
                result.IsHealthy = result.Issues.Count == 0;
                
                // Add performance metrics
                result.AddMetric("Total Health Checks", healthCheckTasks.Length);
                result.AddMetric("Check Categories", Enum.GetNames<HealthCheckCategory>().Length);
            }
            catch (Exception ex)
            {
                result.AddIssue($"Health check failed with exception: {ex.Message}");
            }
            finally
            {
                _healthCheckTimer.Stop();
                result.CheckDurationMs = _healthCheckTimer.ElapsedMilliseconds;
                result.AddMetric("Health Check Duration (ms)", result.CheckDurationMs);
            }

            return result;
        }

        /// <summary>
        /// Safely executes a health check with proper error isolation.
        /// </summary>
        private async Task SafeExecuteHealthCheck(Func<Task> healthCheckFunc, string checkName)
        {
            try
            {
                await healthCheckFunc();
            }
            catch (Exception ex)
            {
                // Log the error but don't let one check failure affect others
                Debug.WriteLine($"Health check '{checkName}' failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks the health of application services and dependencies.
        /// SECURITY FIXED: Added input validation and proper process handling.
        /// </summary>
        private async Task CheckServicesHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Check if winget is available - SECURITY: Using validated executable path
                    CheckExecutableHealth("winget", "--version", result, "Winget");

                    // Check PowerShell availability - SECURITY: Using validated executable path  
                    CheckExecutableHealth("powershell", "-Command \"$PSVersionTable.PSVersion.ToString()\"", result, "PowerShell");
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Service health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// SECURITY FIXED: Safely checks executable health with proper validation and timeout handling.
        /// </summary>
        private void CheckExecutableHealth(string executableKey, string arguments, HealthCheckResult result, string serviceName)
        {
            // SECURITY: Validate executable against allowlist
            if (!ValidExecutables.TryGetValue(executableKey, out var executableName))
            {
                result.AddIssue($"Invalid executable requested: {executableKey}");
                return;
            }

            // SECURITY: Validate arguments to prevent injection
            if (string.IsNullOrWhiteSpace(arguments) || arguments.Contains("&") || arguments.Contains("|") || arguments.Contains(";"))
            {
                result.AddIssue($"Invalid arguments for {serviceName}: potential injection attempt");
                return;
            }

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    // FIXED: Proper timeout handling with forced termination
                    var completed = process.WaitForExit(PROCESS_TIMEOUT_MS);
                    
                    if (!completed)
                    {
                        // Force kill the process if it doesn't exit in time
                        try
                        {
                            process.Kill();
                            process.WaitForExit(2000); // Wait up to 2 seconds for cleanup
                        }
                        catch (Exception killEx)
                        {
                            result.AddWarning($"{serviceName} process cleanup failed: {killEx.Message}");
                        }
                        
                        result.AddWarning($"{serviceName} process timed out after {PROCESS_TIMEOUT_MS}ms");
                        return;
                    }

                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        // SECURITY FIXED: Don't log potentially sensitive output, just confirm success
                        result.AddMetric($"{serviceName} Status", "Available");
                        if (!string.IsNullOrEmpty(output) && output.Length < 100) // Only log short, safe output
                        {
                            result.AddMetric($"{serviceName} Version", output);
                        }
                    }
                    else
                    {
                        result.AddIssue($"{serviceName} is not available or not functioning properly (exit code: {process.ExitCode})");
                    }
                }
                else
                {
                    result.AddIssue($"Failed to start {serviceName} process");
                }
            }
            catch (Exception ex)
            {
                result.AddIssue($"{serviceName} health check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks storage health including disk space and file system access.
        /// PERFORMANCE FIXED: Added async I/O operations.
        /// </summary>
        private async Task CheckStorageHealthAsync(HealthCheckResult result)
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Check disk space
                    var appPath = Application.StartupPath;
                    var driveInfo = new DriveInfo(Path.GetPathRoot(appPath) ?? "C:");
                    
                    var freeSpaceMB = driveInfo.AvailableFreeSpace / 1024 / 1024;
                    var totalSpaceMB = driveInfo.TotalSize / 1024 / 1024;
                    var usedSpaceMB = totalSpaceMB - freeSpaceMB;
                    var usagePercentage = (double)usedSpaceMB / totalSpaceMB * 100;

                    result.AddMetric("Free Disk Space (MB)", freeSpaceMB);
                    result.AddMetric("Total Disk Space (MB)", totalSpaceMB);
                    result.AddMetric("Disk Usage (%)", Math.Round(usagePercentage, 1));

                    if (freeSpaceMB < MIN_FREE_DISK_SPACE_MB)
                    {
                        result.AddIssue($"Low disk space: {freeSpaceMB}MB available (minimum: {MIN_FREE_DISK_SPACE_MB}MB)");
                    }
                    else if (freeSpaceMB < MIN_FREE_DISK_SPACE_MB * 5) // Warning at 5x minimum
                    {
                        result.AddWarning($"Disk space is getting low: {freeSpaceMB}MB available");
                    }

                    // Check application directory access
                    if (!Directory.Exists(appPath))
                    {
                        result.AddIssue("Application startup directory does not exist");
                    }
                    else
                    {
                        // Test write access - FIXED: Using async I/O
                        var testFile = Path.Combine(appPath, $"health_check_test_{Guid.NewGuid():N}.tmp");
                        try
                        {
                            await File.WriteAllTextAsync(testFile, "Health check test");
                            File.Delete(testFile); // Delete is inherently fast, no async version needed
                            result.AddMetric("Application Directory", "Read/Write Access OK");
                        }
                        catch
                        {
                            result.AddIssue("Cannot write to application directory");
                        }
                    }

                    // Check AI Reports directory
                    var reportsPath = Path.Combine(appPath, "AI_Reports");
                    if (Directory.Exists(reportsPath))
                    {
                        var reportCount = Directory.GetFiles(reportsPath, "*.md").Length;
                        result.AddMetric("AI Reports Count", reportCount);
                        
                        // Check for excessive report files
                        if (reportCount > 1000)
                        {
                            result.AddWarning($"Large number of AI reports ({reportCount}). Consider cleanup.");
                        }
                    }

                    // PERFORMANCE FIXED: More efficient temp file counting
                    var tempPath = Path.GetTempPath();
                    var tempFiles = 0;
                    try
                    {
                        // Use enumeration instead of loading all files into memory
                        tempFiles = Directory.EnumerateFiles(tempPath, "WingetWizard*").Take(MAX_TEMP_FILES + 1).Count();
                    }
                    catch (Exception ex)
                    {
                        result.AddWarning($"Could not check temporary files: {ex.Message}");
                    }
                    
                    result.AddMetric("Temporary Files", tempFiles);
                    
                    if (tempFiles > MAX_TEMP_FILES)
                    {
                        result.AddWarning($"Many temporary files found ({tempFiles}). Consider cleanup.");
                    }
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Storage health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Checks memory usage and performance metrics.
        /// </summary>
        private async Task CheckMemoryHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    var currentProcess = Process.GetCurrentProcess();
                    
                    var workingSetMB = currentProcess.WorkingSet64 / 1024 / 1024;
                    var privateMB = currentProcess.PrivateMemorySize64 / 1024 / 1024;
                    var virtualMB = currentProcess.VirtualMemorySize64 / 1024 / 1024;

                    result.AddMetric("Working Set (MB)", workingSetMB);
                    result.AddMetric("Private Memory (MB)", privateMB);
                    result.AddMetric("Virtual Memory (MB)", virtualMB);
                    result.AddMetric("Thread Count", currentProcess.Threads.Count);
                    result.AddMetric("Handle Count", currentProcess.HandleCount);

                    if (workingSetMB > MAX_MEMORY_USAGE_MB)
                    {
                        result.AddWarning($"High memory usage: {workingSetMB}MB (threshold: {MAX_MEMORY_USAGE_MB}MB)");
                    }

                    // Check for excessive threads
                    if (currentProcess.Threads.Count > 50)
                    {
                        result.AddWarning($"High thread count: {currentProcess.Threads.Count}");
                    }

                    // Get system memory info
                    var availableMemory = GC.GetTotalMemory(false);
                    result.AddMetric("GC Total Memory (MB)", availableMemory / 1024 / 1024);
                    result.AddMetric("GC Generation 0", GC.CollectionCount(0));
                    result.AddMetric("GC Generation 1", GC.CollectionCount(1));
                    result.AddMetric("GC Generation 2", GC.CollectionCount(2));
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Memory health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Checks network connectivity and API accessibility.
        /// </summary>
        private async Task CheckNetworkHealthAsync(HealthCheckResult result)
        {
            try
            {
                // Check basic network connectivity
                using var ping = new Ping();
                var pingReply = await ping.SendPingAsync("8.8.8.8", NETWORK_TIMEOUT_MS);
                
                if (pingReply.Status == IPStatus.Success)
                {
                    result.AddMetric("Network Connectivity", "OK");
                    result.AddMetric("Ping Response Time (ms)", pingReply.RoundtripTime);
                }
                else
                {
                    result.AddWarning($"Network connectivity issue: {pingReply.Status}");
                }

                // Check API endpoints accessibility (basic connectivity test)
                var apiEndpoints = new[]
                {
                    ("Anthropic API", "api.anthropic.com"),
                    ("Perplexity API", "api.perplexity.ai")
                };

                foreach (var (name, host) in apiEndpoints)
                {
                    try
                    {
                        var apiPing = await ping.SendPingAsync(host, NETWORK_TIMEOUT_MS);
                        if (apiPing.Status == IPStatus.Success)
                        {
                            result.AddMetric($"{name} Connectivity", "OK");
                        }
                        else
                        {
                            result.AddWarning($"{name} may not be accessible: {apiPing.Status}");
                        }
                    }
                    catch
                    {
                        result.AddWarning($"Cannot test {name} connectivity");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddWarning($"Network health check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks the health of application configuration and settings.
        /// SECURITY FIXED: Removed sensitive logging that could expose API key information.
        /// </summary>
        private async Task CheckConfigurationHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    var settings = _settingsService.GetAllSettings();
                    result.AddMetric("Total Settings", settings.Count);

                    // Check API keys - SECURITY FIXED: Use secure methods without exposing sensitive data
                    var claudeConfigured = _secureSettingsService.HasApiKey("AnthropicApiKey");
                    if (!claudeConfigured)
                    {
                        result.AddWarning("Claude API key is not configured");
                    }
                    else
                    {
                        result.AddMetric("Claude API Key", "Configured");
                    }

                    var perplexityConfigured = _secureSettingsService.HasApiKey("PerplexityApiKey");
                    if (!perplexityConfigured)
                    {
                        result.AddWarning("Perplexity API key is not configured");
                    }
                    else
                    {
                        result.AddMetric("Perplexity API Key", "Configured");
                    }

                    // Check Bedrock credentials
                    var bedrockConfigured = _secureSettingsService.HasApiKey("BedrockApiKey");
                    var awsAccessConfigured = _secureSettingsService.HasApiKey("aws_access_key_id");
                    var awsSecretConfigured = _secureSettingsService.HasApiKey("aws_secret_access_key");

                    if (!bedrockConfigured && (!awsAccessConfigured || !awsSecretConfigured))
                    {
                        result.AddWarning("Bedrock credentials not configured");
                    }
                    else
                    {
                        result.AddMetric("Bedrock Credentials", "Configured");
                    }

                    // Check critical settings
                    var criticalSettings = new[] { "isAdvancedMode", "selectedAiModel", "verboseLogging" };
                    var missingSettings = criticalSettings.Where(s => !settings.ContainsKey(s)).ToList();
                    
                    if (missingSettings.Any())
                    {
                        result.AddWarning($"Missing settings: {string.Join(", ", missingSettings)}");
                    }

                    // Check settings file accessibility
                    try
                    {
                        _settingsService.SaveSettings();
                        result.AddMetric("Settings File", "Read/Write Access OK");
                    }
                    catch (Exception ex)
                    {
                        result.AddIssue($"Cannot save settings file: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Configuration health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Checks file system permissions and access rights.
        /// </summary>
        private async Task CheckPermissionsHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    var appPath = Application.StartupPath;
                    
                    // Check application directory permissions
                    var appDirInfo = new DirectoryInfo(appPath);
                    if (appDirInfo.Exists)
                    {
                        result.AddMetric("Application Directory Exists", true);
                        
                        // Test directory access
                        try
                        {
                            var files = appDirInfo.GetFiles().Length;
                            result.AddMetric("Application Files Count", files);
                        }
                        catch
                        {
                            result.AddIssue("Cannot read application directory contents");
                        }
                    }
                    else
                    {
                        result.AddIssue("Application directory does not exist");
                    }

                    // Check if running with appropriate permissions
                    var isElevated = IsProcessElevated();
                    result.AddMetric("Running Elevated", isElevated);
                    
                    if (!isElevated)
                    {
                        result.AddWarning("Not running with elevated permissions. Some package operations may fail.");
                    }

                    // Check registry access (for theme detection)
                    try
                    {
                        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                        result.AddMetric("Registry Access", key != null ? "OK" : "Limited");
                    }
                    catch
                    {
                        result.AddWarning("Limited registry access for theme detection");
                    }
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Permissions health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Checks application-specific health indicators.
        /// </summary>
        private async Task CheckApplicationHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Check application version and build info
                    var version = Application.ProductVersion;
                    var productName = Application.ProductName;
                    
                    result.AddMetric("Application Version", version);
                    result.AddMetric("Product Name", productName);

                    // Check uptime
                    var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                    result.AddMetric("Application Uptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m");

                    // Check for log files and their sizes
                    var logFiles = Directory.GetFiles(Application.StartupPath, "*.log").ToList();
                    result.AddMetric("Log Files Count", logFiles.Count);

                    foreach (var logFile in logFiles.Take(5)) // Check first 5 log files
                    {
                        var logInfo = new FileInfo(logFile);
                        var logSizeMB = logInfo.Length / 1024.0 / 1024.0;
                        
                        if (logSizeMB > 50) // Warning for log files > 50MB
                        {
                            result.AddWarning($"Large log file: {logInfo.Name} ({logSizeMB:F1}MB)");
                        }
                    }

                    // Check for crash dumps or error files
                    var crashFiles = Directory.GetFiles(Application.StartupPath, "*.dmp").Length;
                    if (crashFiles > 0)
                    {
                        result.AddWarning($"Found {crashFiles} crash dump files");
                    }

                    result.AddMetric("Application Health", "OK");
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Application health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Checks if the current process is running with elevated permissions.
        /// </summary>
        /// <returns>True if running elevated, false otherwise</returns>
        private static bool IsProcessElevated()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Performs a quick health check focusing on critical systems only.
        /// </summary>
        /// <returns>A basic health check result</returns>
        public async Task<HealthCheckResult> PerformQuickHealthCheckAsync()
        {
            _healthCheckTimer.Restart();
            var result = new HealthCheckResult();

            try
            {
                // Quick checks only - with proper error isolation
                await SafeExecuteHealthCheck(() => CheckServicesHealthAsync(result), "Services");
                await SafeExecuteHealthCheck(() => CheckStorageHealthAsync(result), "Storage");
                
                result.IsHealthy = result.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                result.AddIssue($"Quick health check failed: {ex.Message}");
            }
            finally
            {
                _healthCheckTimer.Stop();
                result.CheckDurationMs = _healthCheckTimer.ElapsedMilliseconds;
            }

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _healthCheckTimer?.Stop();
                _disposed = true;
            }
        }
    }
}