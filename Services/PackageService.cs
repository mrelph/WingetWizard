using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WingetWizard.Models;
using WingetWizard.Utils;

namespace WingetWizard.Services
{
    /// <summary>
    /// Service class responsible for package management operations using winget
    /// Handles listing, upgrading, installing, uninstalling, and repairing packages
    /// </summary>
    public class PackageService
    {
        private static readonly HashSet<string> AllowedWingetCommands = new()
        {
            "list", "upgrade", "install", "uninstall", "repair", "search", "source", "show"
        };
        
        private static readonly HashSet<string> AllowedParameters = new()
        {
            "--id", "--source", "--verbose", "--accept-source-agreements", 
            "--accept-package-agreements", "--silent", "--all", "--help",
            "--count", "--exact", "--query", "-q"
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
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System)
                };
                
                System.Diagnostics.Debug.WriteLine($"Process start info - FileName: {psi.FileName}");
                System.Diagnostics.Debug.WriteLine($"Process start info - WorkingDirectory: {psi.WorkingDirectory}");
                System.Diagnostics.Debug.WriteLine($"Process start info - UseShellExecute: {psi.UseShellExecute}");
                System.Diagnostics.Debug.WriteLine($"Process start info - RedirectStandardOutput: {psi.RedirectStandardOutput}");
                System.Diagnostics.Debug.WriteLine($"Process start info - RedirectStandardError: {psi.RedirectStandardError}");

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
                System.Diagnostics.Debug.WriteLine($"Winget command output length: {output.Length}");
                System.Diagnostics.Debug.WriteLine($"Winget command error length: {error.Length}");
                System.Diagnostics.Debug.WriteLine($"Winget command output preview: {output.Substring(0, Math.Min(200, output.Length))}");
                if (error.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Winget command error preview: {error.Substring(0, Math.Min(200, error.Length))}");
                }

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
            System.Diagnostics.Debug.WriteLine("=== IMPROVED WINGET-BASED SEARCH ===");
            System.Diagnostics.Debug.WriteLine($"Search term: '{query}'");
            
            // Use the improved winget approach directly for now
            // PowerShell approach disabled until JSON output is properly implemented
            return await SearchPackagesWithWingetAsync(query, source, count, exact, verbose);
        }

        private async Task<List<PackageSearchResult>> SearchPackagesWithPowerShellAsync(string query, string? source, int count, bool exact, bool verbose)
        {
            try
            {
                var psCommand = $@"
                    try {{
                        # Try to use Microsoft.WinGet.Client if available
                        if (Get-Module -ListAvailable -Name Microsoft.WinGet.Client) {{
                            Import-Module Microsoft.WinGet.Client
                            $results = Find-WinGetPackage -Query '{query}' -Count {count}
                            $results | ForEach-Object {{
                                @{{
                                    Name = $_.Name
                                    Id = $_.Id
                                    Version = $_.Version
                                    Source = $_.Source
                                    Publisher = $_.Publisher
                                    Description = $_.Description
                                }}
                            }} | ConvertTo-Json
                        }} else {{
                            # Fallback to winget with clean output
                            $output = winget search -q '{query}' --accept-source-agreements 2>&1
                            $cleanOutput = $output | Where-Object {{ $_ -notmatch '^\[.*\]$' -and $_ -notmatch '^[█▒░]+$' -and $_ -notmatch '^\d+%$' }}
                            $cleanOutput | ConvertTo-Json
                        }}
                    }} catch {{
                        Write-Error $_.Exception.Message
                    }}";

                System.Diagnostics.Debug.WriteLine("Executing PowerShell command...");
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{psCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to start PowerShell process");
                    return new List<PackageSearchResult>();
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                System.Diagnostics.Debug.WriteLine($"PowerShell execution completed - ExitCode: {process.ExitCode}");
                System.Diagnostics.Debug.WriteLine($"PowerShell output length: {output.Length}");
                System.Diagnostics.Debug.WriteLine($"PowerShell output preview: {output.Substring(0, Math.Min(200, output.Length))}");

                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                {
                    System.Diagnostics.Debug.WriteLine($"PowerShell failed or no output: {error}");
                    return new List<PackageSearchResult>();
                }

                // Try to parse as JSON first
                try
                {
                    System.Diagnostics.Debug.WriteLine("=== ATTEMPTING JSON PARSING ===");
                    var results = ParseJsonSearchResults(output);
                    System.Diagnostics.Debug.WriteLine($"PowerShell JSON parsing successful: {results.Count} results");
                    
                    // Debug each result
                    for (int i = 0; i < results.Count; i++)
                    {
                        var result = results[i];
                        System.Diagnostics.Debug.WriteLine($"Result {i}: Name='{result.Name}', ID='{result.Id}', Version='{result.Version}', Source='{result.Source}'");
                    }
                    
                    return results;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON parsing failed: {ex.Message}, falling back to text parsing");
                    
                    // If not JSON, treat as plain text
                    var lines = output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    System.Diagnostics.Debug.WriteLine($"Split into {lines.Count} text lines");
                    
                    var results = new List<PackageSearchResult>();
                    
                    foreach (var line in lines)
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing text line: '{line}'");
                        var result = PackageSearchResult.FromSearchLine(line);
                        if (result != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Parsed result: Name='{result.Name}', ID='{result.Id}', Version='{result.Version}'");
                            results.Add(result);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to parse line: '{line}'");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"PowerShell text parsing successful: {results.Count} results");
                    return results;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PowerShell search error: {ex.Message}");
                return new List<PackageSearchResult>();
            }
        }

        private async Task<List<PackageSearchResult>> SearchPackagesWithWingetAsync(string query, string? source, int count, bool exact, bool verbose)
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

                System.Diagnostics.Debug.WriteLine($"Winget fallback - Arguments: {string.Join(" ", arguments)}");
                
                var result = ExecuteSecureWingetCommand("search", arguments.ToArray());
                if (!result.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"Winget search failed: {result.Output}");
                    return new List<PackageSearchResult>();
                }

                System.Diagnostics.Debug.WriteLine($"Raw winget output received, length: {result.Output?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine("First 500 characters of raw output:");
                System.Diagnostics.Debug.WriteLine(result.Output?.Substring(0, Math.Min(500, result.Output?.Length ?? 0)));
                
                var cleanedOutput = CleanWingetOutput(result.Output ?? "");
                System.Diagnostics.Debug.WriteLine($"Cleaned output length: {cleanedOutput.Length}");
                System.Diagnostics.Debug.WriteLine("Cleaned output:");
                System.Diagnostics.Debug.WriteLine(cleanedOutput);
                
                var results = ParseWingetSearchOutput(cleanedOutput);
                System.Diagnostics.Debug.WriteLine($"Final parsed results: {results.Count}");
                
                return results;
            });
        }



        /// <summary>
        /// Tests PowerShell execution to verify it's working
        /// </summary>
        public async Task<bool> TestPowerShellExecutionAsync()
        {
            try
            {
                var psCommand = "Write-Output 'PowerShell is working'";
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{psCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return process.ExitCode == 0 && output.Contains("PowerShell is working");
            }
            catch
            {
                return false;
            }
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
            
            System.Diagnostics.Debug.WriteLine($"Raw output length: {output.Length}");
            System.Diagnostics.Debug.WriteLine($"Raw output preview: {output.Substring(0, Math.Min(200, output.Length))}");
            
            // Remove ANSI escape sequences
            var ansiRegex = new System.Text.RegularExpressions.Regex(@"\x1B\[[0-9;]*[a-zA-Z]");
            var cleaned = ansiRegex.Replace(output, "");
            
            // Remove progress bar characters and related patterns
            var progressRegex = new System.Text.RegularExpressions.Regex(@"[█▒░]+");
            cleaned = progressRegex.Replace(cleaned, "");
            
            // Remove percentage indicators
            var percentRegex = new System.Text.RegularExpressions.Regex(@"\d+%");
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
            
            var result = string.Join("\n", cleanedLines);
            System.Diagnostics.Debug.WriteLine($"Cleaned output length: {result.Length}");
            System.Diagnostics.Debug.WriteLine($"Cleaned output preview: {result.Substring(0, Math.Min(200, result.Length))}");
            
            return result;
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
                System.Diagnostics.Debug.WriteLine("No cleaned output to parse");
                return results;
            }
            
            var lines = cleanedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            System.Diagnostics.Debug.WriteLine($"Processing {lines.Length} lines");
            
            bool foundHeader = false;
            bool foundSeparator = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                System.Diagnostics.Debug.WriteLine($"Processing line: '{trimmedLine}'");
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                    
                // Look for header line containing "Name" and "Id"
                if (!foundHeader && trimmedLine.Contains("Name") && trimmedLine.Contains("Id") && trimmedLine.Contains("Version"))
                {
                    System.Diagnostics.Debug.WriteLine("Found header line");
                    foundHeader = true;
                    continue;
                }
                
                // Look for separator line (dashes)
                if (foundHeader && !foundSeparator && trimmedLine.All(c => c == '-' || c == ' '))
                {
                    System.Diagnostics.Debug.WriteLine("Found separator line");
                    foundSeparator = true;
                    continue;
                }
                
                // Only parse data lines after we've found both header and separator
                if (foundHeader && foundSeparator)
                {
                    var packageResult = PackageSearchResult.FromSearchLine(trimmedLine);
                    if (packageResult != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully parsed package: {packageResult.Name} ({packageResult.Id})");
                        results.Add(packageResult);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to parse line: '{trimmedLine}'");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping line (header: {foundHeader}, separator: {foundSeparator}): '{trimmedLine}'");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Parsed {results.Count} packages from {lines.Length} lines");
            return results;
        }
        
        /// <summary>
        /// Parses JSON search results from winget output
        /// </summary>
        /// <param name="jsonOutput">JSON output from winget search</param>
        /// <returns>List of parsed search results</returns>
        private static List<PackageSearchResult> ParseJsonSearchResults(string jsonOutput)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== JSON PARSING DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"JSON input length: {jsonOutput.Length}");
                System.Diagnostics.Debug.WriteLine($"JSON input preview: {jsonOutput.Substring(0, Math.Min(300, jsonOutput.Length))}");
                
                var results = new List<PackageSearchResult>();
                using var document = System.Text.Json.JsonDocument.Parse(jsonOutput);
                var root = document.RootElement;
                
                System.Diagnostics.Debug.WriteLine($"Root element type: {root.ValueKind}");
                System.Diagnostics.Debug.WriteLine($"Root element properties: {string.Join(", ", root.EnumerateObject().Select(p => p.Name))}");
                
                // Try different JSON structures
                if (root.TryGetProperty("Data", out var dataElement) && dataElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine("Found 'Data' array property");
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var result = new PackageSearchResult
                        {
                            Name = GetJsonProperty(item, "Name") ?? "",
                            Id = GetJsonProperty(item, "Id") ?? "",
                            Version = GetJsonProperty(item, "Version") ?? "",
                            Source = GetJsonProperty(item, "Source") ?? "winget",
                            Publisher = GetJsonProperty(item, "Publisher") ?? "",
                            Description = GetJsonProperty(item, "Description") ?? ""
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"Parsed JSON item: Name='{result.Name}', ID='{result.Id}', Version='{result.Version}'");
                        
                        if (!string.IsNullOrEmpty(result.Name) && !string.IsNullOrEmpty(result.Id))
                        {
                            results.Add(result);
                            System.Diagnostics.Debug.WriteLine($"Added valid result: {result.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipped invalid result - Name: '{result.Name}', ID: '{result.Id}'");
                        }
                    }
                }
                else if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine("Root is an array, processing directly");
                    foreach (var item in root.EnumerateArray())
                    {
                        var result = new PackageSearchResult
                        {
                            Name = GetJsonProperty(item, "Name") ?? "",
                            Id = GetJsonProperty(item, "Id") ?? "",
                            Version = GetJsonProperty(item, "Version") ?? "",
                            Source = GetJsonProperty(item, "Source") ?? "winget",
                            Publisher = GetJsonProperty(item, "Publisher") ?? "",
                            Description = GetJsonProperty(item, "Description") ?? ""
                        };
                        
                        if (!string.IsNullOrEmpty(result.Name) && !string.IsNullOrEmpty(result.Id))
                        {
                            results.Add(result);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No recognized JSON structure found");
                }
                
                System.Diagnostics.Debug.WriteLine($"JSON parsing completed: {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"JSON parsing stack trace: {ex.StackTrace}");
                return new List<PackageSearchResult>();
            }
        }
        
        /// <summary>
        /// Helper method to safely extract JSON property values
        /// </summary>
        /// <param name="element">JSON element</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <returns>Property value or null if not found</returns>
        private static string? GetJsonProperty(System.Text.Json.JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == System.Text.Json.JsonValueKind.String ? property.GetString() : property.ToString();
            }
            return null;
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
                var arguments = new List<string> { packageId };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                var result = ExecuteSecureWingetCommand("show", arguments.ToArray());
                
                if (!result.Success)
                {
                    return null;
                }
                
                return PackageSearchResult.FromShowOutput(result.Output);
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
                
                var arguments = new List<string>
                {
                    "--accept-source-agreements",
                    "--accept-package-agreements",
                    "--silent"
                };
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }
                
                // Add package IDs with --id flag for each
                foreach (var packageId in packageIds)
                {
                    arguments.Add("--id");
                    arguments.Add(packageId);
                }
                
                var result = ExecuteSecureWingetCommand("install", arguments.ToArray());
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
