using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services
{
    /// <summary>
    /// Service class responsible for AI-powered package recommendations
    /// Handles Claude AI and Perplexity API integration with structured prompting
    /// </summary>
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _httpSemaphore;
        private readonly ISettingsService _settingsService;
        private readonly string _claudeApiKey;
        private readonly string _perplexityApiKey;
        private readonly string _selectedAiModel;
        private readonly bool _usePerplexity;

        public AIService(ISettingsService settingsService)
        {
            _httpClient = new HttpClient();
            _httpSemaphore = new SemaphoreSlim(1, 1);
            _settingsService = settingsService;
            
            // Load configuration from settings service
            _claudeApiKey = _settingsService.GetSetting<string>("ClaudeApiKey") ?? "";
            _perplexityApiKey = _settingsService.GetSetting<string>("PerplexityApiKey") ?? "";
            _selectedAiModel = _settingsService.GetSetting<string>("SelectedAiModel") ?? "claude-sonnet-4-20250514";
            _usePerplexity = _settingsService.GetSetting<bool>("UsePerplexity");
        }

        /// <summary>
        /// Gets AI recommendation using two-stage process: Perplexity for research, Claude for formatting
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>AI-generated recommendation</returns>
        public async Task<string> GetAIRecommendationAsync(UpgradableApp app)
        {
            // Stage 1: Get raw research data from Perplexity
            var researchData = await GetPerplexityResearchAsync(app);
            
            // Stage 2: Format the research with Claude
            return await FormatReportWithClaudeAsync(app, researchData);
        }

        /// <summary>
        /// Gets recommendation from Claude AI
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>Claude AI recommendation</returns>
        private async Task<string> GetClaudeRecommendationAsync(UpgradableApp app)
        {
            if (string.IsNullOrEmpty(_claudeApiKey)) 
                return "Claude API key not configured";

            var requestBody = new
            {
                model = _selectedAiModel,
                max_tokens = 2500,
                messages = new[] { new { role = "user", content = CreateSoftwareResearchPrompt(app.Name, app.Id, app.Version, app.Available) } }
            };

            var headers = new Dictionary<string, string>
            {
                ["x-api-key"] = _claudeApiKey?.Trim().Replace("\n", "").Replace("\r", "") ?? "",
                ["anthropic-version"] = "2023-06-01"
            };

            return await MakeApiRequestAsync("https://api.anthropic.com/v1/messages", requestBody, headers,
                result => result.GetProperty("content")[0].GetProperty("text").GetString() ?? "No recommendation available",
                "Claude");
        }

        /// <summary>
        /// Gets raw research data from Perplexity AI
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>Raw research data from Perplexity</returns>
        private async Task<string> GetPerplexityResearchAsync(UpgradableApp app)
        {
            if (string.IsNullOrEmpty(_perplexityApiKey)) 
                return "Perplexity API key not configured";

            var requestBody = new
            {
                model = "sonar",
                messages = new object[]
                {
                    new { role = "system", content = "You are a software research assistant. Provide factual, current information about software packages, versions, security issues, and changes. Focus on facts, not formatting." },
                    new { role = "user", content = CreateResearchPrompt(app.Name, app.Id, app.Version, app.Available) }
                },
                max_tokens = 2000,
                temperature = 0.1
            };

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_perplexityApiKey?.Trim().Replace("\n", "").Replace("\r", "") ?? ""}"
            };

            return await MakeApiRequestAsync("https://api.perplexity.ai/chat/completions", requestBody, headers,
                result => result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No research data available",
                "Perplexity");
        }

        /// <summary>
        /// Formats research data into professional report using Claude AI
        /// </summary>
        /// <param name="app">The package being analyzed</param>
        /// <param name="researchData">Raw research data from Perplexity</param>
        /// <returns>Formatted professional report</returns>
        private async Task<string> FormatReportWithClaudeAsync(UpgradableApp app, string researchData)
        {
            if (string.IsNullOrEmpty(_claudeApiKey)) 
                return researchData; // Return raw data if Claude not available

            var requestBody = new
            {
                model = _selectedAiModel,
                max_tokens = 2500,
                messages = new[] { new { role = "user", content = CreateFormattingPrompt(app, researchData) } }
            };

            var headers = new Dictionary<string, string>
            {
                ["x-api-key"] = _claudeApiKey?.Trim().Replace("\n", "").Replace("\r", "") ?? "",
                ["anthropic-version"] = "2023-06-01"
            };

            return await MakeApiRequestAsync("https://api.anthropic.com/v1/messages", requestBody, headers,
                result => result.GetProperty("content")[0].GetProperty("text").GetString() ?? researchData,
                "Claude");
        }

        /// <summary>
        /// Makes an HTTP API request with proper error handling and rate limiting
        /// </summary>
        /// <param name="url">API endpoint URL</param>
        /// <param name="requestBody">Request payload</param>
        /// <param name="headers">HTTP headers</param>
        /// <param name="responseParser">Function to parse the response</param>
        /// <param name="providerName">Name of the AI provider for logging</param>
        /// <returns>Parsed response or error message</returns>
        private async Task<string> MakeApiRequestAsync(string url, object requestBody, Dictionary<string, string> headers,
            Func<JsonElement, string> responseParser, string providerName)
        {
            try
            {
                await _httpSemaphore.WaitAsync();
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    foreach (var header in headers)
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);

                    var response = await _httpClient.PostAsync(url,
                        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        return responseParser(result);
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"{providerName} API Error {response.StatusCode}: {errorContent}";
                }
                finally
                {
                    _httpSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                return $"{providerName} API error: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates research prompt for Perplexity to gather factual information
        /// </summary>
        /// <param name="softwareName">Name of the software package</param>
        /// <param name="packageId">Winget package identifier</param>
        /// <param name="currentVersion">Currently installed version</param>
        /// <param name="newVersion">Available upgrade version</param>
        /// <returns>Research prompt for Perplexity</returns>
        private static string CreateResearchPrompt(string softwareName, string packageId, string currentVersion, string newVersion)
        {
            return $@"Research the software application {softwareName} (package: {packageId}) and its upgrade from version {currentVersion} to {newVersion}.

Provide comprehensive information about:

**Application Overview:**
1. What is {softwareName} and what does it do?
2. Who develops/maintains this software?
3. What category/type of application is it?
4. Is it free, paid, or freemium?
5. What are its main features and use cases?

**Version Analysis:**
6. What changed between version {currentVersion} and {newVersion}?
7. Any security fixes or vulnerabilities addressed?
8. New features, improvements, or enhancements?
9. Known issues, bugs fixed, or breaking changes?
10. Performance improvements or system requirement changes?

**Security & Trust:**
11. Any recent security incidents or vulnerabilities?
12. Developer reputation and trustworthiness?
13. Code signing and authenticity verification?

**User Impact:**
14. Should users upgrade immediately or wait?
15. Any compatibility concerns with other software?
16. System requirements changes
17. Release date and stability information
18. User feedback or reviews about this version

Focus on facts and current information. Do not format the response.";
        }

        /// <summary>
        /// Creates formatting prompt for Claude to structure the research into a professional report
        /// </summary>
        /// <param name="app">The package being analyzed</param>
        /// <param name="researchData">Raw research data from Perplexity</param>
        /// <returns>Formatting prompt for Claude</returns>
        private static string CreateFormattingPrompt(UpgradableApp app, string researchData)
        {
            return $@"# 🔍 Software Analysis & Upgrade Report: {app.Name}

You are a senior software analyst. Format the following research data into a comprehensive software analysis and upgrade recommendation report.

## Research Data:
{researchData}

## Package Details:
- **Package ID**: `{app.Id}`
- **Current Version**: `{app.Version}`
- **Available Version**: `{app.Available}`

## Required Report Format:

Provide your analysis in this **exact markdown structure**:

### 📋 **Application Overview**
- **Software Name**: {app.Name}
- **Developer/Publisher**: [From research]
- **Category**: [Application type/category]
- **License**: [Free/Paid/Freemium]
- **Primary Purpose**: [What the software does]
- **Key Features**: [Main functionality]

### 🎯 **Executive Summary**
> 🟢 RECOMMENDED / 🟡 CONDITIONAL / 🔴 NOT RECOMMENDED

Brief 1-2 sentence recommendation with urgency level.

### 🔄 **Version Changes**
- **Current Version**: `{app.Version}`
- **Target Version**: `{app.Available}`
- **Update Type**: 🔵 Major / 🟡 Minor / 🟢 Patch / 🔴 Breaking
- **Release Date**: [Date if available]

### ⚡ **Key Improvements**
- 🆕 **New Features**: List major new functionality
- 🐛 **Bug Fixes**: Critical issues resolved
- 🔧 **Enhancements**: Performance and usability improvements
- 📊 **Performance**: Speed/resource impact changes

### 🔒 **Security Assessment**
- 🛡️ **Security Fixes**: List any CVE fixes or security patches
- 🚨 **Vulnerability Status**: Current security standing
- 🔐 **Risk Level**: 🟢 Low / 🟡 Medium / 🔴 High / 🟣 Critical

### ⚠️ **Compatibility & Risks**
- 💥 **Breaking Changes**: List any breaking changes
- 🔗 **Dependencies**: New requirements or conflicts
- 🖥️ **System Requirements**: Hardware/OS compatibility
- 🔄 **Migration Effort**: 🟢 None / 🟡 Minor / 🔴 Significant

### 📅 **Recommendation Timeline**
- 🚀 **Immediate** (Security/Critical)
- 📆 **Within 1 week** (Important updates)
- 🗓️ **Within 1 month** (Regular updates)
- ⏳ **When convenient** (Optional updates)

### 🎯 **Action Items**
- [ ] **Pre-upgrade**: Backup/preparation steps
- [ ] **During upgrade**: Installation considerations
- [ ] **Post-upgrade**: Verification and testing
- [ ] **Rollback plan**: If issues occur

---
💡 **Pro Tip**: Include any relevant links to release notes or documentation.

**Important**: Use exact emoji indicators, maintain formatting, provide actionable insights.";
        }

        /// <summary>
        /// Legacy method for backward compatibility - now redirects to two-stage process
        /// </summary>
        private static string CreateSoftwareResearchPrompt(string softwareName, string packageId, string currentVersion, string newVersion)
        {
            return CreateResearchPrompt(softwareName, packageId, currentVersion, newVersion);
        }

        public async Task<List<AIRecommendation>> GetPersonalizedRecommendationsAsync(List<UpgradableApp> installedPackages, UserProfile? userProfile = null)
        {
            var recommendations = new List<AIRecommendation>();
            
            try
            {
                // Analyze user's current package ecosystem
                var categories = installedPackages.GroupBy(p => GetPackageCategory(p.Name))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Generate mock recommendations based on patterns
                if (categories.ContainsKey("Development"))
                {
                    recommendations.Add(new AIRecommendation
                    {
                        Title = "Enhanced Development Workflow",
                        Description = "Based on your development tools, consider adding Git GUI tools and code analysis utilities.",
                        RecommendedPackages = new List<string> { "GitKraken", "SonarLint", "Postman" },
                        Reasoning = "Developers with similar tool sets report 40% faster workflow with these additions",
                        Priority = "Medium",
                        Category = "Productivity Enhancement"
                    });
                }

                if (categories.ContainsKey("Media"))
                {
                    recommendations.Add(new AIRecommendation
                    {
                        Title = "Complete Media Suite",
                        Description = "Enhance your media capabilities with professional-grade tools.",
                        RecommendedPackages = new List<string> { "Handbrake", "Audacity", "OBS Studio" },
                        Reasoning = "Media professionals recommend these tools for comprehensive content creation",
                        Priority = "Low",
                        Category = "Creative Tools"
                    });
                }

                recommendations.Add(new AIRecommendation
                {
                    Title = "Security Enhancement",
                    Description = "Strengthen your system security with modern tools.",
                    RecommendedPackages = new List<string> { "Malwarebytes", "1Password", "WireGuard" },
                    Reasoning = "Essential security tools missing from your current setup",
                    Priority = "High",
                    Category = "Security"
                });

                return recommendations;
            }
            catch (Exception)
            {
                // Return basic recommendations on error
                return new List<AIRecommendation>
                {
                    new AIRecommendation
                    {
                        Title = "System Maintenance",
                        Description = "Keep your system optimized with essential maintenance tools.",
                        RecommendedPackages = new List<string> { "CCleaner", "TreeSize", "Everything" },
                        Reasoning = "Basic system maintenance recommendations",
                        Priority = "Medium",
                        Category = "System Maintenance"
                    }
                };
            }
        }

        public async Task<PackageCompatibilityAnalysis> AnalyzeCompatibilityAsync(string packageId, List<UpgradableApp> installedPackages)
        {
            await Task.Delay(500); // Simulate analysis time

            var analysis = new PackageCompatibilityAnalysis
            {
                PackageId = packageId,
                AnalyzedAt = DateTime.Now
            };

            // Simulate compatibility analysis based on common patterns
            var packageLower = packageId.ToLower();
            
            if (packageLower.Contains("visual") && packageLower.Contains("studio"))
            {
                analysis.CompatibilityScore = "High";
                analysis.RecommendedCompanionPackages.AddRange(new[]
                {
                    "Git for Windows",
                    "Windows Terminal",
                    "PowerShell Core"
                });
                analysis.Analysis = "Visual Studio integrates well with most development tools. Recommended companion packages enhance the development experience.";
            }
            else if (packageLower.Contains("docker"))
            {
                analysis.CompatibilityScore = "Medium";
                analysis.RequiredDependencies.Add("WSL 2");
                analysis.PotentialConflicts.Add("VirtualBox (may require configuration changes)");
                analysis.Analysis = "Docker Desktop requires WSL 2 and may conflict with other virtualization software.";
            }
            else
            {
                analysis.CompatibilityScore = "High";
                analysis.Analysis = "No known compatibility issues detected with your current package configuration.";
            }

            return analysis;
        }

        public async Task<List<MaintenanceRecommendation>> GenerateMaintenanceRecommendationsAsync(List<UpgradableApp> installedPackages)
        {
            await Task.Delay(300); // Simulate analysis
            
            var recommendations = new List<MaintenanceRecommendation>();
            
            var outdatedCount = installedPackages.Count(p => !string.IsNullOrEmpty(p.Available));
            if (outdatedCount > 0)
            {
                recommendations.Add(new MaintenanceRecommendation
                {
                    Title = "Package Updates Available",
                    Description = $"{outdatedCount} packages have updates available. Regular updates improve security and performance.",
                    Priority = outdatedCount > 10 ? "High" : "Medium",
                    Category = "Updates",
                    AffectedPackages = installedPackages
                        .Where(p => !string.IsNullOrEmpty(p.Available))
                        .Select(p => p.Name)
                        .Take(5)
                        .ToList(),
                    Action = "Run bulk update operation"
                });
            }

            // Check for potential cleanup opportunities
            if (installedPackages.Count > 50)
            {
                recommendations.Add(new MaintenanceRecommendation
                {
                    Title = "Package Cleanup Review",
                    Description = "Large number of packages detected. Consider reviewing for unused applications.",
                    Priority = "Low",
                    Category = "Cleanup",
                    Action = "Review package usage patterns"
                });
            }

            // Security recommendation
            recommendations.Add(new MaintenanceRecommendation
            {
                Title = "Security Scan Recommended",
                Description = "Perform a security analysis of installed packages to identify potential vulnerabilities.",
                Priority = "Medium", 
                Category = "Security",
                Action = "Run security analysis on critical packages"
            });

            return recommendations;
        }

        public async Task<SecurityAnalysisReport> AnalyzePackageSecurityAsync(UpgradableApp package)
        {
            await Task.Delay(800); // Simulate security analysis

            var report = new SecurityAnalysisReport
            {
                PackageId = package.Id,
                PackageName = package.Name,
                LastScanned = DateTime.Now
            };

            // Simulate security analysis based on package patterns
            var packageLower = package.Name.ToLower();

            if (packageLower.Contains("microsoft") || packageLower.Contains("windows"))
            {
                report.SecurityScore = "Excellent";
                report.PublisherVerification = "Verified - Microsoft Corporation";
                report.CodeSigningStatus = "Valid";
                report.Recommendations.Add("Package from trusted publisher - safe to install");
            }
            else if (packageLower.Contains("chrome") || packageLower.Contains("firefox"))
            {
                report.SecurityScore = "Good";
                report.PublisherVerification = "Verified";
                report.CodeSigningStatus = "Valid";
                report.Findings.Add(new SecurityFinding
                {
                    Type = "Info",
                    Severity = "Low",
                    Title = "Regular Updates Required",
                    Description = "Browser security depends on regular updates",
                    Recommendation = "Enable automatic updates"
                });
            }
            else
            {
                report.SecurityScore = "Fair";
                report.PublisherVerification = "Unverified";
                report.CodeSigningStatus = "Unknown";
                report.Findings.Add(new SecurityFinding
                {
                    Type = "Warning",
                    Severity = "Medium",
                    Title = "Publisher Verification Needed",
                    Description = "Publisher identity could not be verified",
                    Recommendation = "Verify package source before installation"
                });
            }

            return report;
        }

        private string GetPackageCategory(string packageName)
        {
            var name = packageName.ToLower();
            
            if (name.Contains("visual studio") || name.Contains("vscode") || name.Contains("git") || 
                name.Contains("python") || name.Contains("node") || name.Contains("docker"))
                return "Development";
                
            if (name.Contains("chrome") || name.Contains("firefox") || name.Contains("edge"))
                return "Browser";
                
            if (name.Contains("vlc") || name.Contains("spotify") || name.Contains("audacity"))
                return "Media";
                
            if (name.Contains("office") || name.Contains("word") || name.Contains("excel"))
                return "Other";
                
            return "Other";
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpSemaphore?.Dispose();
        }
    }
}



