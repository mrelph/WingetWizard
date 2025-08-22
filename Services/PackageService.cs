using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UpgradeApp.Models;
using UpgradeApp.Utils;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Service class responsible for package management operations using winget
    /// Handles listing, upgrading, installing, uninstalling, and repairing packages
    /// </summary>
    public class PackageService
    {
        private static readonly HashSet<string> AllowedWingetCommands = new()
        {
            "list", "upgrade", "install", "uninstall", "repair", "search", "source"
        };
        
        private static readonly HashSet<string> AllowedParameters = new()
        {
            "--id", "--source", "--verbose", "--accept-source-agreements", 
            "--accept-package-agreements", "--silent", "--all", "--help"
        };
        
        private static readonly Regex DangerousPatternRegex = new(
            @"[;&|><$`(){}\\""']|exec|eval|system|shell|cmd|powershell\.exe|net\.exe|reg\.exe|sc\.exe|wmic\.exe",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public PackageService()
        {
        }
        /// <summary>
        /// Executes a secure winget command and returns the result
        /// </summary>
        /// <param name="command">The winget command to execute</param>
        /// <param name="arguments">Safe arguments for the command</param>
        /// <returns>The command output as a string</returns>
        private (bool Success, string Output) ExecuteSecureWingetCommand(string command, params string[] arguments)
        {
            try
            {
                // Validate command
                if (!IsValidWingetCommand(command, arguments))
                {
                                    var errorMessage = "Invalid or potentially dangerous command detected";
                System.Diagnostics.Debug.WriteLine($"Security Warning: {errorMessage} - Command: {command}");
                return (false, errorMessage);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                psi.ArgumentList.Add(command);
                foreach (var arg in arguments.Where(a => !string.IsNullOrWhiteSpace(a)))
                {
                    psi.ArgumentList.Add(arg);
                }

                System.Diagnostics.Debug.WriteLine($"Executing secure winget command: {command} with {arguments.Length} arguments");

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return (false, "Failed to start winget process");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var success = process.ExitCode == 0;
                var result = success ? output : $"{output}\n{error}".Trim();

                System.Diagnostics.Debug.WriteLine($"Winget command completed - Command: {command}, Success: {success}, ExitCode: {process.ExitCode}");

                return (success, result);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error executing winget command: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Secure winget command execution failed - Command: {command}, Error: {ex.Message}");
                return (false, errorMessage);
            }
        }

        /// <summary>
        /// Validates that a winget command and its arguments are safe
        /// </summary>
        private bool IsValidWingetCommand(string command, string[] arguments)
        {
            // Check if command is in whitelist
            if (!AllowedWingetCommands.Contains(command.ToLowerInvariant()))
            {
                return false;
            }

            // Check for dangerous patterns in command
            if (DangerousPatternRegex.IsMatch(command))
            {
                return false;
            }

            // Validate all arguments
            for (int i = 0; i < arguments.Length; i++)
            {
                var arg = arguments[i];
                
                // Skip empty arguments
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                // Check for dangerous patterns
                if (DangerousPatternRegex.IsMatch(arg))
                {
                    return false;
                }

                // If it's a parameter flag, validate it's allowed
                if (arg.StartsWith("--"))
                {
                    if (!AllowedParameters.Contains(arg.ToLowerInvariant()))
                    {
                        return false;
                    }
                }
                // If it's a package ID or value, validate format
                else if (i > 0 && arguments[i-1].StartsWith("--"))
                {
                    // Validate package IDs and values
                    if (!IsValidParameterValue(arguments[i-1], arg))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Validates parameter values based on the parameter type
        /// </summary>
        private bool IsValidParameterValue(string parameter, string value)
        {
            var errors = new List<string>();
            
            return parameter.ToLowerInvariant() switch
            {
                "--id" => ValidationUtils.ValidatePackageName(value, errors) != null,
                "--source" => value.ToLowerInvariant() is "winget" or "msstore" or "all",
                _ => !DangerousPatternRegex.IsMatch(value) && value.Length <= 100
            };
        }

        /// <summary>
        /// Legacy method for backward compatibility - now uses secure execution
        /// </summary>
        /// <param name="command">The PowerShell command to execute</param>
        /// <returns>The command output as a string</returns>
        [Obsolete("Use ExecuteSecureWingetCommand instead")]
        public string RunPowerShell(string command)
        {
            // Parse the legacy command format and convert to secure execution
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !parts[0].Equals("winget", StringComparison.OrdinalIgnoreCase))
            {
                return "Invalid command format";
            }

            var wingetCommand = parts[1];
            var arguments = parts.Skip(2).ToArray();
            
            var result = ExecuteSecureWingetCommand(wingetCommand, arguments);
            return result.Success ? result.Output : $"Error: {result.Output}";
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
                var arguments = new List<string>();
                if (source != "all")
                {
                    arguments.Add("--source");
                    arguments.Add(source);
                }
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("list", arguments.ToArray());
                var lines = result.Output?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
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

                    var parts = Regex.Split(line.Trim(), @"\s{2,}", RegexOptions.None, TimeSpan.FromSeconds(1));
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
                var arguments = new List<string>();
                if (source != "all")
                {
                    arguments.Add("--source");
                    arguments.Add(source);
                }
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("upgrade", arguments.ToArray());
                var lines = result.Output?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
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

                    var parts = Regex.Split(line.Trim(), @"\s{2,}", RegexOptions.None, TimeSpan.FromSeconds(1));
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
                var arguments = new List<string>
                {
                    "--id", packageId,
                    "--accept-source-agreements",
                    "--accept-package-agreements",
                    "--silent"
                };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("upgrade", arguments.ToArray());
                return (result.Success, result.Output);
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
                var arguments = new List<string>
                {
                    "--all",
                    "--accept-source-agreements",
                    "--accept-package-agreements",
                    "--silent"
                };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("upgrade", arguments.ToArray());
                return (result.Success, result.Output);
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
                var arguments = new List<string>
                {
                    "--id", packageId,
                    "--accept-source-agreements",
                    "--accept-package-agreements",
                    "--silent"
                };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("install", arguments.ToArray());
                return (result.Success, result.Output);
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
                var arguments = new List<string>
                {
                    "--id", packageId,
                    "--silent"
                };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("uninstall", arguments.ToArray());
                return (result.Success, result.Output);
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
                var arguments = new List<string>
                {
                    "--id", packageId,
                    "--accept-source-agreements",
                    "--accept-package-agreements",
                    "--silent"
                };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("repair", arguments.ToArray());
                return (result.Success, result.Output);
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
