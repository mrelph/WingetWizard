using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WingetWizard.Models;

namespace WingetWizard.Services
{
    /// <summary>
    /// Service class responsible for AI report management and export operations
    /// Handles saving individual package reports and generating comprehensive markdown exports
    /// </summary>
    public class ReportService
    {
        private readonly string _reportsDirectory;
        private readonly Dictionary<string, string> _savedReports;

        public ReportService(string reportsDirectory)
        {
            // Validate and sanitize the reports directory path
            if (string.IsNullOrWhiteSpace(reportsDirectory))
                throw new ArgumentException("Reports directory cannot be null or empty");
            
            var fullPath = Path.GetFullPath(reportsDirectory);
            var basePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Reports directory must be within application directory");
            
            _reportsDirectory = fullPath;
            _savedReports = new Dictionary<string, string>();
            InitializeReportsDirectory();
        }

        /// <summary>
        /// Gets the path to a saved report for a package
        /// </summary>
        /// <param name="packageName">Name or ID of the package</param>
        /// <returns>Report file path if exists, null otherwise</returns>
        public string? GetReportPath(string packageName)
        {
            return _savedReports.TryGetValue(packageName, out var path) ? path : null;
        }

        /// <summary>
        /// Checks if a report exists for a package
        /// </summary>
        /// <param name="packageName">Name or ID of the package</param>
        /// <returns>True if report exists</returns>
        public bool HasReport(string packageName)
        {
            return _savedReports.ContainsKey(packageName);
        }

        /// <summary>
        /// Initializes the reports directory and loads existing reports
        /// </summary>
        private void InitializeReportsDirectory()
        {
            try
            {
                // Create reports directory if it doesn't exist
                if (!Directory.Exists(_reportsDirectory))
                {
                    Directory.CreateDirectory(_reportsDirectory);
                }

                // Load existing reports
                LoadExistingReports();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - service should be resilient
                System.Diagnostics.Debug.WriteLine($"Failed to initialize reports directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads existing report files from the reports directory
        /// </summary>
        private void LoadExistingReports()
        {
            try
            {
                var reportFiles = Directory.GetFiles(_reportsDirectory, "*.md");
                foreach (var file in reportFiles)
                {
                    var fileName = Path.GetFileName(file);
                    // Extract package ID from filename (format: PackageID_YYYYMMDD_HHMMSS.md)
                    var parts = fileName.Split('_');
                    if (parts.Length >= 3)
                    {
                        var packageId = parts[0];
                        _savedReports[packageId] = file;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load existing reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates comprehensive markdown content for AI research report
        /// </summary>
        /// <param name="recommendations">List of AI recommendations for packages</param>
        /// <param name="usePerplexity">Whether Perplexity was used as AI provider</param>
        /// <param name="selectedAiModel">Selected Claude AI model</param>
        /// <returns>Formatted markdown content</returns>
        public string CreateMarkdownContent(List<(UpgradableApp app, string recommendation)> recommendations, bool usePerplexity, string selectedAiModel)
        {
            var content = new StringBuilder();

            // Header with metadata
            content.AppendLine("# 🧿 WingetWizard AI Research Report");
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();
            content.AppendLine("## 📊 **Report Metadata**");
            content.AppendLine();
            content.AppendLine($"- **🕒 Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"- **📦 Packages Analyzed**: {recommendations.Count}");
            content.AppendLine($"- **🤖 AI Provider**: Perplexity (Research) + Claude {selectedAiModel} (Formatting)");
                            content.AppendLine($"- **⚙️ Tool**: WingetWizard v2.4 by GeekSuave Labs");
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();

            // Summary section
            content.AppendLine("## 🎯 **Executive Summary**");
            content.AppendLine();
            var recommendedCount = 0;
            var conditionalCount = 0;
            var notRecommendedCount = 0;

            foreach (var (app, recommendation) in recommendations)
            {
                if (recommendation.Contains("🟢 RECOMMENDED") && !recommendation.Contains("🟡") && !recommendation.Contains("🔴"))
                    recommendedCount++;
                else if (recommendation.Contains("🟡 CONDITIONAL"))
                    conditionalCount++;
                else if (recommendation.Contains("🔴 NOT RECOMMENDED"))
                    notRecommendedCount++;
            }

            content.AppendLine($"- 🟢 **Recommended Updates**: {recommendedCount}");
            content.AppendLine($"- 🟡 **Conditional Updates**: {conditionalCount}");
            content.AppendLine($"- 🔴 **Not Recommended**: {notRecommendedCount}");
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();

            // Individual package analyses
            content.AppendLine("## 📦 **Package Analysis**");
            content.AppendLine();

            foreach (var (app, recommendation) in recommendations)
            {
                content.AppendLine($"### 🔍 **{app.Name}**");
                content.AppendLine();
                content.AppendLine($"**📋 Package Details**");
                content.AppendLine($"- **Package ID**: `{app.Id}`");
                content.AppendLine($"- **Current Version**: `{app.Version}`");
                content.AppendLine($"- **Available Version**: `{app.Available}`");
                content.AppendLine($"- **Analysis Date**: {DateTime.Now:yyyy-MM-dd}");
                content.AppendLine();

                // Add the AI recommendation
                content.AppendLine("**🤖 AI Analysis**");
                content.AppendLine();
                content.AppendLine(recommendation);
                content.AppendLine();
                content.AppendLine("---");
                content.AppendLine();
            }

            // Footer
            content.AppendLine("## 📄 **Report Footer**");
            content.AppendLine();
            content.AppendLine("> 💡 **Disclaimer**: This report is generated by AI analysis and should be reviewed by qualified IT personnel before implementing upgrades.");
            content.AppendLine(">");
            content.AppendLine("> 🔒 **Security Note**: Always verify security updates through official channels and test in non-production environments first.");
            content.AppendLine(">");
                            content.AppendLine($"> 🧿 **Generated by**: WingetWizard v2.4 - AI-Enhanced Package Management Tool");
            content.AppendLine($"> 🏢 **Developed by**: GeekSuave Labs | Mark Relph");
            content.AppendLine($"> 📅 **Report Date**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return content.ToString();
        }

        /// <summary>
        /// Saves individual package reports from the full report content
        /// </summary>
        /// <param name="fullReportContent">Complete report content to parse</param>
        /// <returns>Number of package reports saved</returns>
        public int SaveIndividualPackageReports(string fullReportContent)
        {
            try
            {
                // Parse the full report to extract individual package sections
                var lines = fullReportContent.Split('\n');
                var currentPackage = "";
                var packageContent = new StringBuilder();
                var inPackageSection = false;
                var packagesFound = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("### 🔍 **") && line.Contains("**"))
                    {
                        // Save previous package if exists
                        if (!string.IsNullOrEmpty(currentPackage) && packageContent.Length > 0)
                        {
                            SavePackageReport(currentPackage, packageContent.ToString());
                            packagesFound++;
                        }

                        // Start new package
                        currentPackage = line.Replace("### 🔍 **", "").Replace("**", "").Trim();
                        packageContent.Clear();
                        packageContent.AppendLine("# 🧿 WingetWizard AI Research Report");
                        packageContent.AppendLine();
                        packageContent.AppendLine($"## 📦 **{currentPackage}**");
                        packageContent.AppendLine();
                        inPackageSection = true;
                    }
                    else if (inPackageSection && line.StartsWith("### 🔍 **"))
                    {
                        // Another package section started, save current one
                        if (!string.IsNullOrEmpty(currentPackage) && packageContent.Length > 0)
                        {
                            SavePackageReport(currentPackage, packageContent.ToString());
                            packagesFound++;
                        }

                        // Start new package
                        currentPackage = line.Replace("### 🔍 **", "").Replace("**", "").Trim();
                        packageContent.Clear();
                        packageContent.AppendLine("# 🧿 WingetWizard AI Research Report");
                        packageContent.AppendLine();
                        packageContent.AppendLine($"## 📦 **{currentPackage}**");
                        packageContent.AppendLine();
                    }
                    else if (inPackageSection)
                    {
                        packageContent.AppendLine(line);
                    }
                }

                // Save the last package
                if (!string.IsNullOrEmpty(currentPackage) && packageContent.Length > 0)
                {
                    SavePackageReport(currentPackage, packageContent.ToString());
                    packagesFound++;
                }

                return packagesFound;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save individual package reports: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Saves a single package report to file
        /// </summary>
        /// <param name="packageName">Name of the package</param>
        /// <param name="content">Report content to save</param>
        /// <returns>True if saved successfully</returns>
        private bool SavePackageReport(string packageName, string content)
        {
            try
            {
                // Create a safe filename
                var safeName = string.Join("_", packageName.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                File.WriteAllText(filePath, content, Encoding.UTF8);

                // Store the report path for later access
                _savedReports[packageName] = filePath;

                return true;
            }
            catch (Exception ex)
            {
                var safePackageName = System.Net.WebUtility.HtmlEncode(packageName);
                System.Diagnostics.Debug.WriteLine($"Failed to save package report for {safePackageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all saved report paths
        /// </summary>
        /// <returns>Dictionary of package names to report paths</returns>
        public Dictionary<string, string> GetAllReportPaths()
        {
            return new Dictionary<string, string>(_savedReports);
        }
    }
}
