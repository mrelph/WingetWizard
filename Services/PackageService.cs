using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UpgradeApp.Models;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Service class responsible for package management operations using winget
    /// Handles listing, upgrading, installing, uninstalling, and repairing packages
    /// </summary>
    public class PackageService
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
    }
}
