using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using UpgradeApp.Models;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Provides comprehensive health check functionality for the WingetWizard application.
    /// Monitors system resources, service availability, and application health.
    /// </summary>
    public class HealthCheckService : IDisposable
    {
        private readonly SettingsService _settingsService;
        private readonly SecureSettingsService _secureSettingsService;
        private readonly Stopwatch _healthCheckTimer = new();
        private bool _disposed = false;

        // Health check thresholds
        private const long MIN_FREE_DISK_SPACE_MB = 100; // 100MB minimum free space
        private const long MAX_MEMORY_USAGE_MB = 500;    // 500MB maximum memory usage
        private const int NETWORK_TIMEOUT_MS = 5000;     // 5 second network timeout
        private const int MAX_TEMP_FILES = 100;          // Maximum temporary files

        public HealthCheckService(SettingsService settingsService, SecureSettingsService secureSettingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _secureSettingsService = secureSettingsService ?? throw new ArgumentNullException(nameof(secureSettingsService));
        }

        /// <summary>
        /// Performs a comprehensive health check of the application and system.
        /// </summary>
        /// <returns>A detailed health check result</returns>
        public async Task<HealthCheckResult> PerformHealthCheckAsync()
        {
            _healthCheckTimer.Restart();
            var result = new HealthCheckResult();

            try
            {
                // Run all health checks in parallel for better performance
                var healthCheckTasks = new[]
                {
                    CheckServicesHealthAsync(result),
                    CheckStorageHealthAsync(result),
                    CheckMemoryHealthAsync(result),
                    CheckNetworkHealthAsync(result),
                    CheckConfigurationHealthAsync(result),
                    CheckPermissionsHealthAsync(result),
                    CheckApplicationHealthAsync(result)
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
        /// Checks the health of application services and dependencies.
        /// </summary>
        private async Task CheckServicesHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Check if winget is available
                    var wingetProcess = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(wingetProcess);
                    if (process != null)
                    {
                        process.WaitForExit(5000); // 5 second timeout
                        if (process.ExitCode == 0)
                        {
                            var version = process.StandardOutput.ReadToEnd().Trim();
                            result.AddMetric("Winget Version", version);
                        }
                        else
                        {
                            result.AddIssue("Winget is not available or not functioning properly");
                        }
                    }
                    else
                    {
                        result.AddIssue("Failed to start winget process");
                    }

                    // Check PowerShell availability
                    var psProcess = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = "-Command \"$PSVersionTable.PSVersion.ToString()\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var psProc = Process.Start(psProcess);
                    if (psProc != null)
                    {
                        psProc.WaitForExit(5000);
                        if (psProc.ExitCode == 0)
                        {
                            var psVersion = psProc.StandardOutput.ReadToEnd().Trim();
                            result.AddMetric("PowerShell Version", psVersion);
                        }
                        else
                        {
                            result.AddWarning("PowerShell may not be available or functioning properly");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.AddIssue($"Service health check failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Checks storage health including disk space and file system access.
        /// </summary>
        private async Task CheckStorageHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
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
                        // Test write access
                        var testFile = Path.Combine(appPath, $"health_check_test_{Guid.NewGuid():N}.tmp");
                        try
                        {
                            File.WriteAllText(testFile, "Health check test");
                            File.Delete(testFile);
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

                    // Check for temporary files
                    var tempFiles = Directory.GetFiles(Path.GetTempPath(), "WingetWizard*").Length;
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
        /// </summary>
        private async Task CheckConfigurationHealthAsync(HealthCheckResult result)
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Starting configuration health check...");
                    
                    var settings = _settingsService.GetAllSettings();
                    result.AddMetric("Total Settings", settings.Count);
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Retrieved {settings.Count} settings from SettingsService");

                    // Check API keys
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Checking Claude API key...");
                    var claudeKey = _secureSettingsService.GetApiKey("AnthropicApiKey");
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Claude API key retrieved. Present: {!string.IsNullOrEmpty(claudeKey)}, Length: {claudeKey?.Length ?? 0}");

                    if (string.IsNullOrEmpty(claudeKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] WARNING: Claude API key is not configured");
                        result.AddWarning("Claude API key is not configured");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] Claude API key is configured");
                        result.AddMetric("Claude API Key", "Configured");
                    }

                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Checking Perplexity API key...");
                    var perplexityKey = _secureSettingsService.GetApiKey("PerplexityApiKey");
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Perplexity API key retrieved. Present: {!string.IsNullOrEmpty(perplexityKey)}, Length: {perplexityKey?.Length ?? 0}");

                    if (string.IsNullOrEmpty(perplexityKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] WARNING: Perplexity API key is not configured");
                        result.AddWarning("Perplexity API key is not configured");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] Perplexity API key is configured");
                        result.AddMetric("Perplexity API Key", "Configured");
                    }

                    // Check Bedrock credentials
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Checking Bedrock credentials...");
                    var bedrockApiKey = _secureSettingsService.GetApiKey("BedrockApiKey");
                    var awsAccessKey = _secureSettingsService.GetApiKey("aws_access_key_id");
                    var awsSecretKey = _secureSettingsService.GetApiKey("aws_secret_access_key");
                    
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Bedrock API key present: {!string.IsNullOrEmpty(bedrockApiKey)}");
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] AWS Access Key present: {!string.IsNullOrEmpty(awsAccessKey)}");
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] AWS Secret Key present: {!string.IsNullOrEmpty(awsSecretKey)}");

                    if (string.IsNullOrEmpty(bedrockApiKey) && (string.IsNullOrEmpty(awsAccessKey) || string.IsNullOrEmpty(awsSecretKey)))
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] WARNING: Bedrock credentials not configured (neither API key nor AWS credentials)");
                        result.AddWarning("Bedrock credentials not configured");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] Bedrock credentials are configured");
                        result.AddMetric("Bedrock Credentials", "Configured");
                    }

                    // Check critical settings
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Checking critical settings...");
                    var criticalSettings = new[] { "isAdvancedMode", "selectedAiModel", "verboseLogging" };
                    var missingSettings = criticalSettings.Where(s => !settings.ContainsKey(s)).ToList();
                    
                    if (missingSettings.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] WARNING: Missing critical settings: {string.Join(", ", missingSettings)}");
                        result.AddWarning($"Missing settings: {string.Join(", ", missingSettings)}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] All critical settings are present");
                    }

                    // Check settings file accessibility
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] Testing settings file write access...");
                        _settingsService.SaveSettings();
                        result.AddMetric("Settings File", "Read/Write Access OK");
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] Settings file write access test passed");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] ERROR: Cannot save settings file: {ex.Message}");
                        result.AddIssue("Cannot save settings file");
                    }

                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] Configuration health check completed successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] ERROR: Configuration health check failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[HealthCheck] ERROR: Exception type: {ex.GetType().Name}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HealthCheck] ERROR: Inner exception: {ex.InnerException.Message}");
                    }
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
                // Quick checks only
                await CheckServicesHealthAsync(result);
                await CheckStorageHealthAsync(result);
                
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

