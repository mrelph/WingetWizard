using System;

namespace WingetWizard.Models
{
    /// <summary>
    /// Represents a package search result from winget
    /// </summary>
    public class PackageSearchResult
    {
        /// <summary>
        /// Package name as displayed
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Package identifier (e.g., Microsoft.VisualStudioCode)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Package version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// How the search matched (Moniker, Tag, etc.)
        /// </summary>
        public string Match { get; set; } = string.Empty;

        /// <summary>
        /// Source of the package (winget, msstore, etc.)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Publisher/author of the package
        /// </summary>
        public string Publisher { get; set; } = string.Empty;

        /// <summary>
        /// Package description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Homepage URL
        /// </summary>
        public string Homepage { get; set; } = string.Empty;

        /// <summary>
        /// License information
        /// </summary>
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with the package
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Whether the package is already installed
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Whether the package is selected for installation
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Installation status message
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Creates a PackageSearchResult from winget search output line
        /// </summary>
        /// <param name="searchLine">Line from winget search output</param>
        /// <returns>Parsed PackageSearchResult or null if parsing fails</returns>
        public static PackageSearchResult? FromSearchLine(string searchLine)
        {
            if (string.IsNullOrWhiteSpace(searchLine))
                return null;

            try
            {
                var line = searchLine.Trim();
                System.Diagnostics.Debug.WriteLine($"Parsing line: '{line}'");
                
                if (line.Length < 10)
                {
                    System.Diagnostics.Debug.WriteLine($"Line too short: {line.Length} characters");
                    return null;
                }

                // Skip separator lines and headers
                if (line.StartsWith("---") || line.All(c => c == '-' || c == ' ') ||
                    line.Contains("Name") && line.Contains("Id") && line.Contains("Version"))
                {
                    System.Diagnostics.Debug.WriteLine("Skipping separator/header line");
                    return null;
                }

                // Improved parsing logic - handle fixed-width format better
                // Expected format: Name   Id   Version   Match   Source
                // Use regex to split on 2 or more spaces, but be more flexible
                var parts = System.Text.RegularExpressions.Regex.Split(line, @"\s{2,}");
                
                System.Diagnostics.Debug.WriteLine($"Split into {parts.Length} parts: [{string.Join("] [", parts)}]");
                
                // Be more flexible with column count - minimum 3 (Name, Id, Version)
                if (parts.Length < 3)
                {
                    System.Diagnostics.Debug.WriteLine($"Not enough columns found: {parts.Length} (minimum 3)");
                    return null;
                }

                var result = new PackageSearchResult
                {
                    Name = parts[0].Trim(),
                    Id = parts[1].Trim(),
                    Version = parts.Length > 2 ? parts[2].Trim() : "Unknown",
                    Match = parts.Length > 3 ? parts[3].Trim() : "",
                    Source = parts.Length > 4 ? parts[4].Trim() : "winget"
                };

                // Validate that we have meaningful data
                if (string.IsNullOrWhiteSpace(result.Name) || string.IsNullOrWhiteSpace(result.Id))
                {
                    System.Diagnostics.Debug.WriteLine("Name or ID is empty, skipping");
                    return null;
                }

                // Additional validation - check if the ID looks like a proper package ID
                if (!result.Id.Contains('.') && !result.Id.Contains('\\'))
                {
                    // This might be a continuation of the name field
                    // Try alternative parsing approach
                    System.Diagnostics.Debug.WriteLine($"ID '{result.Id}' doesn't look like a package ID, trying alternative parsing");
                    
                    // Find the first part that looks like a proper package ID (contains dots)
                    var packageIdIndex = -1;
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Contains('.') || (parts[i].Length > 5 && char.IsUpper(parts[i][0])))
                        {
                            packageIdIndex = i;
                            break;
                        }
                    }
                    
                    if (packageIdIndex > 0 && packageIdIndex < parts.Length - 2)
                    {
                        // Reconstruct with proper package ID
                        result.Name = string.Join(" ", parts.Take(packageIdIndex)).Trim();
                        result.Id = parts[packageIdIndex].Trim();
                        result.Version = packageIdIndex + 1 < parts.Length ? parts[packageIdIndex + 1].Trim() : "Unknown";
                        result.Match = packageIdIndex + 2 < parts.Length ? parts[packageIdIndex + 2].Trim() : "";
                        result.Source = packageIdIndex + 3 < parts.Length ? parts[packageIdIndex + 3].Trim() : "winget";
                        
                        System.Diagnostics.Debug.WriteLine($"Alternative parsing: Name='{result.Name}', ID='{result.Id}'");
                    }
                }

                // Final validation
                if (string.IsNullOrWhiteSpace(result.Name) || string.IsNullOrWhiteSpace(result.Id))
                {
                    System.Diagnostics.Debug.WriteLine("Final validation failed - Name or ID is empty");
                    return null;
                }

                // Clean up empty values
                if (string.IsNullOrEmpty(result.Source))
                    result.Source = "winget";
                if (string.IsNullOrEmpty(result.Version))
                    result.Version = "Unknown";

                System.Diagnostics.Debug.WriteLine($"Successfully parsed: Name='{result.Name}', ID='{result.Id}', Version='{result.Version}', Match='{result.Match}', Source='{result.Source}'");

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception parsing line: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a PackageSearchResult from winget show output
        /// </summary>
        /// <param name="showOutput">Output from winget show command</param>
        /// <returns>Parsed PackageSearchResult with detailed information</returns>
        public static PackageSearchResult FromShowOutput(string showOutput)
        {
            var result = new PackageSearchResult();
            
            if (string.IsNullOrWhiteSpace(showOutput))
                return result;

            var lines = showOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("Name:"))
                    result.Name = trimmedLine.Substring(5).Trim();
                else if (trimmedLine.StartsWith("Version:"))
                    result.Version = trimmedLine.Substring(8).Trim();
                else if (trimmedLine.StartsWith("Publisher:"))
                    result.Publisher = trimmedLine.Substring(10).Trim();
                else if (trimmedLine.StartsWith("Description:"))
                    result.Description = trimmedLine.Substring(12).Trim();
                else if (trimmedLine.StartsWith("Homepage:"))
                    result.Homepage = trimmedLine.Substring(9).Trim();
                else if (trimmedLine.StartsWith("License:"))
                    result.License = trimmedLine.Substring(8).Trim();
                else if (trimmedLine.StartsWith("Tags:"))
                    result.Tags = trimmedLine.Substring(5).Trim();
            }

            return result;
        }

        public override string ToString()
        {
            return $"{Name} ({Id}) - {Version}";
        }
    }
}
