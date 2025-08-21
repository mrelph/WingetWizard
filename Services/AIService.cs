using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UpgradeApp.Models;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Service class responsible for AI-powered package recommendations
    /// Handles Claude AI and Perplexity API integration with structured prompting
    /// </summary>
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _httpSemaphore;
        private readonly string _claudeApiKey;
        private readonly string _perplexityApiKey;
        private readonly string _selectedAiModel;
        private readonly bool _usePerplexity;

        public AIService(string claudeApiKey, string perplexityApiKey, string selectedAiModel, bool usePerplexity)
        {
            _httpClient = new HttpClient();
            _httpSemaphore = new SemaphoreSlim(1, 1);
            _claudeApiKey = claudeApiKey;
            _perplexityApiKey = perplexityApiKey;
            _selectedAiModel = selectedAiModel;
            _usePerplexity = usePerplexity;
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
