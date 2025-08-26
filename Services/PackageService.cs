using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services
{
    /// <summary>
    /// Service class responsible for package management operations using winget
    /// Handles listing, upgrading, installing, uninstalling, and repairing packages
    /// </summary>
    public class PackageService : IPackageService
    {
        /// <summary>
        /// Executes a PowerShell command and returns the result
        /// </summary>
        /// <param name="command">The PowerShell command to execute</param>
        /// <returns>The command output as a string</returns>
        public string RunPowerShell(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) 
                return "Command is null or empty";
            
            var validCommands = new[] { "winget list", "winget upgrade", "winget install", "winget uninstall", "winget repair" };
            if (!validCommands.Any(cmd => command.TrimStart().StartsWith(cmd, StringComparison.OrdinalIgnoreCase)))
                return "Invalid command format";
            
            var psi = new ProcessStartInfo 
            { 
                FileName = "powershell.exe", 
                RedirectStandardOutput = true, 
                UseShellExecute = false, 
                CreateNoWindow = true 
            };
            psi.ArgumentList.Add("-Command"); 
            psi.ArgumentList.Add(command);
            
            using var process = Process.Start(psi);
            return process?.StandardOutput.ReadToEnd() ?? "Process failed";
        }

        /// <summary>
        /// Lists all installed applications using winget
        /// </summary>
        /// <param name="source">Package source (winget, msstore, all)</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>List of installed applications</returns>
        public async Task<List<UpgradableApp>> ListAllAppsAsync(string source, bool verbose)
        {
            return await Task.Run(() =>
            {
                var sourceParam = source == "all" ? "" : $"--source {source}";
                var command = $"winget list {sourceParam}{(verbose ? " --verbose" : "")}";
                
                var output = RunPowerShell(command);
                var lines = output?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var apps = new List<UpgradableApp>();
                bool headerFound = false;
                
                foreach (var line in lines)
                {
                    if (!headerFound)
                    {
                        if (line.Trim().StartsWith("Name") && line.Contains("Id") && line.Contains("Version"))
                        {
                            headerFound = true;
                        }
                        continue;
                    }
                    
                    if (line.Trim().Length == 0 || line.StartsWith("-")) 
                        continue;

                    var parts = Regex.Split(line.Trim(), @"\s{2,}");
                    if (parts.Length >= 3)
                    {
                        var app = new UpgradableApp
                        {
                            Name = parts[0],
                            Id = parts[1],
                            Version = parts[2],
                            Available = "",
                            Status = "",
                            Recommendation = ""
                        };
                        apps.Add(app);
                    }
                }
                
                return apps;
            });
        }

        /// <summary>
        /// Checks for available package updates using winget
        /// </summary>
        /// <param name="source">Package source (winget, msstore, all)</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>List of upgradable applications</returns>
        public async Task<List<UpgradableApp>> CheckForUpdatesAsync(string source, bool verbose)
        {
            return await Task.Run(() =>
            {
                var sourceParam = source == "all" ? "" : $"--source {source}";
                var command = $"winget upgrade {sourceParam}{(verbose ? " --verbose" : "")}";
                
                var output = RunPowerShell(command);
                var lines = output?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var apps = new List<UpgradableApp>();
                bool headerFound = false;
                
                foreach (var line in lines)
                {
                    if (!headerFound)
                    {
                        if (line.Trim().StartsWith("Name") && line.Contains("Id") && line.Contains("Version"))
                        {
                            headerFound = true;
                        }
                        continue;
                    }
                    
                    if (line.Trim().Length == 0 || line.StartsWith("-")) 
                        continue;

                    var parts = Regex.Split(line.Trim(), @"\s{2,}");
                    if (parts.Length >= 4)
                    {
                        var name = parts[0];
                        var id = parts[1];
                        var currentVer = parts[2];
                        var availableVer = parts[3];
                        
                        // Skip if available version looks like a source name
                        if (availableVer.ToLower().Contains("winget") || availableVer.ToLower().Contains("msstore"))
                        {
                            if (parts.Length > 4) 
                                availableVer = parts[4];
                            else 
                                continue; // Skip this entry if we can't find proper version
                        }
                        
                        var app = new UpgradableApp
                        {
                            Name = name,
                            Id = id,
                            Version = currentVer,
                            Available = availableVer,
                            Status = "",
                            Recommendation = ""
                        };
                        apps.Add(app);
                    }
                }
                
                return apps;
            });
        }

        /// <summary>
        /// Upgrades a specific package
        /// </summary>
        /// <param name="packageId">The package ID to upgrade</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Success status and result message</returns>
        public async Task<(bool Success, string Message)> UpgradePackageAsync(string packageId, bool verbose)
        {
            return await Task.Run(() =>
            {
                var command = $"winget upgrade --id \"{packageId}\" --accept-source-agreements --accept-package-agreements --silent{(verbose ? " --verbose" : "")}";
                var result = RunPowerShell(command);
                var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
                
                return (success, result);
            });
        }

        /// <summary>
        /// Upgrades all available packages
        /// </summary>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Success status and result message</returns>
        public async Task<(bool Success, string Message)> UpgradeAllPackagesAsync(bool verbose)
        {
            return await Task.Run(() =>
            {
                var verboseParam = verbose ? " --verbose" : "";
                var command = $"winget upgrade --all --accept-source-agreements --accept-package-agreements --silent{verboseParam}";
                var result = RunPowerShell(command);
                var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
                
                return (success, result);
            });
        }

        /// <summary>
        /// Installs a specific package
        /// </summary>
        /// <param name="packageId">The package ID to install</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Success status and result message</returns>
        public async Task<(bool Success, string Message)> InstallPackageAsync(string packageId, bool verbose)
        {
            return await Task.Run(() =>
            {
                var command = $"winget install --id \"{packageId}\" --accept-source-agreements --accept-package-agreements --silent{(verbose ? " --verbose" : "")}";
                var result = RunPowerShell(command);
                var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
                
                return (success, result);
            });
        }

        /// <summary>
        /// Uninstalls a specific package
        /// </summary>
        /// <param name="packageId">The package ID to uninstall</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Success status and result message</returns>
        public async Task<(bool Success, string Message)> UninstallPackageAsync(string packageId, bool verbose)
        {
            return await Task.Run(() =>
            {
                var command = $"winget uninstall --id \"{packageId}\" --silent{(verbose ? " --verbose" : "")}";
                var result = RunPowerShell(command);
                var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
                
                return (success, result);
            });
        }

        /// <summary>
        /// Repairs a specific package
        /// </summary>
        /// <param name="packageId">The package ID to repair</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Success status and result message</returns>
        public async Task<(bool Success, string Message)> RepairPackageAsync(string packageId, bool verbose)
        {
            return await Task.Run(() =>
            {
                var command = $"winget repair --id \"{packageId}\" --accept-source-agreements --accept-package-agreements --silent{(verbose ? " --verbose" : "")}";
                var result = RunPowerShell(command);
                var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
                
                return (success, result);
            });
        }

        /// <summary>
        /// Exports package list to text format
        /// </summary>
        /// <param name="packages">List of packages to export</param>
        /// <returns>Formatted text content</returns>
        public string ExportPackageList(List<UpgradableApp> packages)
        {
            var content = new StringBuilder();
            content.AppendLine("WINGETWIZARD PACKAGE UPGRADE LIST");
            content.AppendLine(new string('=', 50));
            content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Total packages: {packages.Count}");
            content.AppendLine();
            
            foreach (var app in packages)
            {
                content.AppendLine($"Name: {app.Name}");
                content.AppendLine($"ID: {app.Id}");
                content.AppendLine($"Current: {app.Version}");
                content.AppendLine($"Available: {app.Available}");
                if (!string.IsNullOrEmpty(app.Status)) 
                    content.AppendLine($"Status: {app.Status}");
                if (!string.IsNullOrEmpty(app.Recommendation)) 
                    content.AppendLine($"AI Recommendation: {app.Recommendation}");
                content.AppendLine(new string('-', 30));
            }
            
            return content.ToString();
        }

        /// <summary>
        /// Searches for packages using winget
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="source">Source to search (optional)</param>
        /// <param name="count">Maximum number of results (1-1000)</param>
        /// <param name="exact">Use exact match</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>List of search results</returns>
        public async Task<List<PackageSearchResult>> SearchPackagesAsync(string query, string? source = null, int count = 50, bool exact = false, bool verbose = false)
        {
            return await Task.Run(() =>
            {
                var arguments = new List<string>
                {
                    "-q", query,
                    "--accept-source-agreements"
                };

                if (!string.IsNullOrWhiteSpace(source))
                {
                    arguments.Add("--source");
                    arguments.Add(source);
                }

                if (count > 0 && count <= 1000)
                {
                    arguments.Add("--count");
                    arguments.Add(count.ToString());
                }

                if (exact)
                {
                    arguments.Add("--exact");
                }

                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var command = $"winget search {string.Join(" ", arguments)}";
                var result = RunPowerShell(command);
                
                if (string.IsNullOrEmpty(result) || result.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    return new List<PackageSearchResult>();
                }

                var cleanedOutput = CleanWingetOutput(result);
                var results = ParseWingetSearchOutput(cleanedOutput);
                
                return results;
            });
        }

        /// <summary>
        /// Gets detailed information about a specific package
        /// </summary>
        /// <param name="packageId">Package ID to get details for</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Detailed package information</returns>
        public async Task<PackageSearchResult?> GetPackageDetailsAsync(string packageId, bool verbose = false)
        {
            return await Task.Run(() =>
            {
                var verboseParam = verbose ? " --verbose" : "";
                var command = $"winget show {packageId}{verboseParam}";
                var result = RunPowerShell(command);
                
                if (string.IsNullOrEmpty(result) || result.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                
                return PackageSearchResult.FromShowOutput(result);
            });
        }

        /// <summary>
        /// Installs multiple packages
        /// </summary>
        /// <param name="packageIds">List of package IDs to install</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Success status and result message</returns>
        public async Task<(bool Success, string Message)> InstallMultiplePackagesAsync(List<string> packageIds, bool verbose = false)
        {
            return await Task.Run(() =>
            {
                if (packageIds == null || packageIds.Count == 0)
                {
                    return (false, "No packages specified for installation");
                }
                
                var verboseParam = verbose ? " --verbose" : "";
                
                // Install packages one by one for better error handling
                var results = new List<string>();
                var successCount = 0;
                
                foreach (var packageId in packageIds)
                {
                    var command = $"winget install --id \"{packageId}\" --accept-source-agreements --accept-package-agreements --silent{verboseParam}";
                    var result = RunPowerShell(command);
                    
                    if (!result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase))
                    {
                        successCount++;
                        results.Add($"✅ {packageId}: Success");
                    }
                    else
                    {
                        results.Add($"❌ {packageId}: {result}");
                    }
                }
                
                var message = $"Installation completed. {successCount}/{packageIds.Count} packages installed successfully.\n\n{string.Join("\n", results)}";
                return (successCount > 0, message);
            });
        }

        /// <summary>
        /// Cleans ANSI escape sequences and progress bars from winget output
        /// </summary>
        /// <param name="output">Raw winget output</param>
        /// <returns>Cleaned output suitable for parsing</returns>
        private static string CleanWingetOutput(string output)
        {
            if (string.IsNullOrEmpty(output))
                return string.Empty;
            
            // Remove ANSI escape sequences
            var ansiRegex = new Regex(@"\x1B\[[0-9;]*[a-zA-Z]");
            var cleaned = ansiRegex.Replace(output, "");
            
            // Remove progress bar characters and related patterns
            var progressRegex = new Regex(@"[█▒░]+");
            cleaned = progressRegex.Replace(cleaned, "");
            
            // Remove percentage indicators
            var percentRegex = new Regex(@"\d+%");
            cleaned = percentRegex.Replace(cleaned, "");
            
            // Handle carriage returns - take only the final state after \r
            var lines = cleaned.Split('\n');
            var cleanedLines = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip lines that are just progress indicators, but preserve table structure
                if (string.IsNullOrWhiteSpace(trimmedLine) ||
                    trimmedLine.Contains("Downloading") ||
                    trimmedLine.Contains("Installing") ||
                    trimmedLine.Contains("Progress") ||
                    trimmedLine.StartsWith("[") ||
                    (trimmedLine.All(c => c == ' ' || c == '=' || c == '|') && !trimmedLine.All(c => c == '-' || c == ' ')))
                {
                    continue;
                }
                
                // Preserve header lines, separator lines (dashes), and data lines
                // Skip only the first two empty/progress lines we typically see
                if ((trimmedLine == "-" || trimmedLine.All(c => c == ' ')) && cleanedLines.Count < 2)
                {
                    continue;
                }
                
                if (trimmedLine.Contains('\r'))
                {
                    // Take the last part after the final \r
                    var parts = trimmedLine.Split('\r');
                    var finalPart = parts[parts.Length - 1].Trim();
                    if (!string.IsNullOrWhiteSpace(finalPart))
                        cleanedLines.Add(finalPart);
                }
                else
                {
                    cleanedLines.Add(trimmedLine);
                }
            }
            
            return string.Join("\n", cleanedLines);
        }
        
        /// <summary>
        /// Parses winget search output into PackageSearchResult objects
        /// </summary>
        /// <param name="cleanedOutput">Cleaned winget search output</param>
        /// <returns>List of parsed search results</returns>
        private static List<PackageSearchResult> ParseWingetSearchOutput(string cleanedOutput)
        {
            var results = new List<PackageSearchResult>();
            
            if (string.IsNullOrWhiteSpace(cleanedOutput))
            {
                return results;
            }
            
            var lines = cleanedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            bool foundHeader = false;
            bool foundSeparator = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                    
                // Look for header line containing "Name" and "Id"
                if (!foundHeader && trimmedLine.Contains("Name") && trimmedLine.Contains("Id") && trimmedLine.Contains("Version"))
                {
                    foundHeader = true;
                    continue;
                }
                
                // Look for separator line (dashes)
                if (foundHeader && !foundSeparator && trimmedLine.All(c => c == '-' || c == ' '))
                {
                    foundSeparator = true;
                    continue;
                }
                
                // Only parse data lines after we've found both header and separator
                if (foundHeader && foundSeparator)
                {
                    var packageResult = PackageSearchResult.FromSearchLine(trimmedLine);
                    if (packageResult != null)
                    {
                        results.Add(packageResult);
                    }
                }
            }
            
            return results;
        }
    }
}



