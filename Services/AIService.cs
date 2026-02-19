using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WingetWizard.Models;
using WingetWizard.Utils;

namespace WingetWizard.Services
{
    /// <summary>
    /// Service class responsible for AI-powered package recommendations
    /// Handles Claude AI, Perplexity API, and AWS Bedrock integration with fallback support
    /// </summary>
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _httpSemaphore;
        private readonly string _claudeApiKey;
        private readonly string _perplexityApiKey;
        private readonly string _selectedAiModel;
        private readonly bool _usePerplexity;
        private readonly string _selectedProvider;
        
        // Bedrock configuration
        private readonly string? _awsAccessKeyId;
        private readonly string? _awsSecretAccessKey;
        private readonly string? _awsRegion;
        private readonly string? _bedrockModel;
        private readonly BedrockService? _bedrockService;

        public AIService(
            string claudeApiKey, 
            string perplexityApiKey, 
            string selectedAiModel, 
            bool usePerplexity,
            string selectedProvider = "Claude",
            string? awsAccessKeyId = null,
            string? awsSecretAccessKey = null,
            string? awsRegion = null,
            string? bedrockModel = null)
        {
            _httpClient = new HttpClient();
            _httpSemaphore = new SemaphoreSlim(1, 1);
            _claudeApiKey = claudeApiKey;
            _perplexityApiKey = perplexityApiKey;
            _selectedAiModel = selectedAiModel;
            _usePerplexity = usePerplexity;
            _selectedProvider = selectedProvider;
            
            // Initialize Bedrock if configured
            _awsAccessKeyId = awsAccessKeyId;
            _awsSecretAccessKey = awsSecretAccessKey;
            _awsRegion = awsRegion ?? "us-east-1";
            _bedrockModel = bedrockModel ?? AppConstants.BEDROCK_CLAUDE_35_SONNET_V2;
            
            if (!string.IsNullOrEmpty(_awsAccessKeyId) && !string.IsNullOrEmpty(_awsSecretAccessKey))
            {
                _bedrockService = new BedrockService(_awsAccessKeyId, _awsSecretAccessKey, _awsRegion, _bedrockModel);
            }
        }
        
        /// <summary>
        /// Sanitizes API key by removing newlines and trimming whitespace
        /// </summary>
        /// <param name="apiKey">Raw API key</param>
        /// <returns>Sanitized API key</returns>
        private static string SanitizeApiKey(string? apiKey)
        {
            return apiKey?.Trim().Replace("\n", "").Replace("\r", "") ?? "";
        }
        
        /// <summary>
        /// Creates Claude request body with consistent structure
        /// </summary>
        /// <param name="content">Request content</param>
        /// <returns>Request body object</returns>
        private object CreateClaudeRequestBody(string content)
        {
            return new
            {
                model = _selectedAiModel,
                max_tokens = 2500,
                messages = new[] { new { role = "user", content } }
            };
        }

        /// <summary>
        /// Gets AI recommendation using configured provider with fallback support
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>AI-generated recommendation</returns>
        public async Task<string> GetAIRecommendationAsync(UpgradableApp app)
        {
            System.Diagnostics.Debug.WriteLine($"[AIService] Starting AI recommendation for package: {app.Name} (ID: {app.Id})");
            System.Diagnostics.Debug.WriteLine($"[AIService] Selected provider: {_selectedProvider}, Use Perplexity: {_usePerplexity}");
            
            var result = _selectedProvider switch
            {
                "Bedrock" => await GetBedrockRecommendationWithFallback(app),
                "Perplexity" => await GetPerplexityOnlyRecommendation(app),
                _ => await GetClaudeRecommendationWithFallback(app)
            };
            
            System.Diagnostics.Debug.WriteLine($"[AIService] AI recommendation completed for {app.Name}. Result length: {result?.Length ?? 0}");
            return result ?? "AI analysis failed - no result generated";
        }

        /// <summary>
        /// Gets recommendation from Bedrock with Claude fallback
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>AI-generated recommendation</returns>
        private async Task<string> GetBedrockRecommendationWithFallback(UpgradableApp app)
        {
            try
            {
                if (_bedrockService != null)
                {
                    var result = await _bedrockService.GetAIRecommendationAsync(app);
                    
                    // Check if Bedrock failed - if so, fallback to Claude
                    if (result.Contains("Bedrock AI analysis failed") || result.Contains("AWS Bedrock"))
                    {
                        return await GetClaudeRecommendationWithFallback(app);
                    }
                    
                    return result;
                }
                
                // No Bedrock configured, fallback to Claude
                return await GetClaudeRecommendationWithFallback(app);
            }
            catch (Exception)
            {
                // Bedrock failed completely, try Claude
                return await GetClaudeRecommendationWithFallback(app);
            }
        }

        /// <summary>
        /// Gets recommendation from Claude with Bedrock fallback
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>AI-generated recommendation</returns>
        private async Task<string> GetClaudeRecommendationWithFallback(UpgradableApp app)
        {
            try
            {
                // Try Claude first with enhanced prompting
                if (_usePerplexity)
                {
                    // Two-stage process: Perplexity research + Claude formatting
                    var researchData = await GetPerplexityResearchAsync(app);
                    var claudeResult = await FormatReportWithClaudeAsync(app, researchData);
                    
                    // If Claude formatting failed but we have research data, return that
                    if (claudeResult.Contains("Claude API Error") && !researchData.Contains("Perplexity API"))
                    {
                        if (_bedrockService != null)
                        {
                            var bedrockFallback = await _bedrockService.GetAIRecommendationAsync(app);
                            if (!bedrockFallback.Contains("Bedrock AI analysis failed"))
                                return bedrockFallback;
                        }
                        return $"**Research Data (Perplexity only):**\n\n{researchData}";
                    }
                    
                    return claudeResult;
                }
                else
                {
                    // Direct Claude analysis
                    var claudeResult = await GetClaudeRecommendationAsync(app);
                    
                    // If Claude failed, try Bedrock
                    if (claudeResult.Contains("Claude API Error") && _bedrockService != null)
                    {
                        var bedrockFallback = await _bedrockService.GetAIRecommendationAsync(app);
                        if (!bedrockFallback.Contains("Bedrock AI analysis failed"))
                            return bedrockFallback;
                    }
                    
                    return claudeResult;
                }
            }
            catch (Exception ex)
            {
                // All Claude attempts failed, try Bedrock if available
                if (_bedrockService != null)
                {
                    try
                    {
                        return await _bedrockService.GetAIRecommendationAsync(app);
                    }
                    catch (Exception)
                    {
                        return $"All AI services failed. Please check your configuration and try again. Error: {ex.Message}";
                    }
                }
                
                return $"AI analysis failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets recommendation using Perplexity only
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>AI-generated recommendation</returns>
        private async Task<string> GetPerplexityOnlyRecommendation(UpgradableApp app)
        {
            var result = await GetPerplexityResearchAsync(app);
            
            // If Perplexity failed, try fallback options
            if (result.Contains("Perplexity API"))
            {
                if (_bedrockService != null)
                {
                    var bedrockResult = await _bedrockService.GetAIRecommendationAsync(app);
                    if (!bedrockResult.Contains("Bedrock AI analysis failed"))
                        return bedrockResult;
                }
                
                // Last resort: try Claude
                if (!string.IsNullOrEmpty(_claudeApiKey))
                {
                    return await GetClaudeRecommendationAsync(app);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets recommendation from Claude AI
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>Claude AI recommendation</returns>
        private async Task<string> GetClaudeRecommendationAsync(UpgradableApp app)
        {
            System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] Starting Claude recommendation for package: {app.Name}");
            
            if (string.IsNullOrEmpty(_claudeApiKey)) 
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] ERROR: Claude API key not configured");
                return "Claude API key not configured";
            }

            System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] Claude API key configured, length: {_claudeApiKey.Length}");
            System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] Using model: {_selectedAiModel}");

            var requestBody = CreateClaudeRequestBody(CreateSoftwareResearchPrompt(app.Name, app.Id, app.Version, app.Available));
            System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] Request body created, prompt length: {CreateSoftwareResearchPrompt(app.Name, app.Id, app.Version, app.Available).Length}");

            var headers = new Dictionary<string, string>
            {
                ["x-api-key"] = SanitizeApiKey(_claudeApiKey),
                ["anthropic-version"] = "2023-06-01"
            };

            System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] Making API request to Claude...");
            var result = await MakeApiRequestAsync("https://api.anthropic.com/v1/messages", requestBody, headers,
                result => result.GetProperty("content")[0].GetProperty("text").GetString() ?? "No recommendation available",
                "Claude");
            
            System.Diagnostics.Debug.WriteLine($"[AIService] [Claude] Claude API request completed. Response length: {result?.Length ?? 0}");
            return result ?? "Claude API request failed - no response generated";
        }

        /// <summary>
        /// Gets raw research data from Perplexity AI
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>Raw research data from Perplexity</returns>
        private async Task<string> GetPerplexityResearchAsync(UpgradableApp app)
        {
            System.Diagnostics.Debug.WriteLine($"[AIService] [Perplexity] Starting Perplexity research for package: {app.Name}");
            
            if (string.IsNullOrEmpty(_perplexityApiKey)) 
            {
                System.Diagnostics.Debug.WriteLine($"[AIService] [Perplexity] ERROR: Perplexity API key not configured");
                return "Perplexity API key not configured";
            }

            System.Diagnostics.Debug.WriteLine($"[AIService] [Perplexity] Perplexity API key configured, length: {_perplexityApiKey.Length}");
            System.Diagnostics.Debug.WriteLine($"[AIService] [Perplexity] Using model: sonar");

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
                ["Authorization"] = $"Bearer {SanitizeApiKey(_perplexityApiKey)}"
            };

            System.Diagnostics.Debug.WriteLine($"[AIService] [Perplexity] Making API request to Perplexity...");
            var result = await MakeApiRequestAsync("https://api.perplexity.ai/chat/completions", requestBody, headers,
                result => result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No research data available",
                "Perplexity");
            
            System.Diagnostics.Debug.WriteLine($"[AIService] [Perplexity] Perplexity API request completed. Response length: {result?.Length ?? 0}");
            return result ?? "Perplexity API request failed - no response generated";
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

            var requestBody = CreateClaudeRequestBody(CreateFormattingPrompt(app, researchData));

            var headers = new Dictionary<string, string>
            {
                ["x-api-key"] = SanitizeApiKey(_claudeApiKey),
                ["anthropic-version"] = "2023-06-01"
            };

            return await MakeApiRequestAsync("https://api.anthropic.com/v1/messages", requestBody, headers,
                result => result.GetProperty("content")[0].GetProperty("text").GetString() ?? researchData,
                "Claude");
        }

        /// <summary>
        /// Makes an HTTP API request with proper error handling, retry logic, and rate limiting
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
            const int maxRetries = 3;
            const int baseDelayMs = 1000;

            System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] Starting {providerName} API request to: {url}");
            System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] Request attempt 1 of {maxRetries + 1}");

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Acquiring HTTP semaphore...");
                    await _httpSemaphore.WaitAsync();
                    
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Setting up request headers...");
                        _httpClient.DefaultRequestHeaders.Clear();
                        foreach (var header in headers)
                        {
                            var headerValue = header.Key.ToLowerInvariant() == "authorization" ? 
                                $"Bearer {header.Value.Substring(0, Math.Min(10, header.Value.Length))}..." : 
                                header.Value;
                            _httpClient.DefaultRequestHeaders.Add(header.Key, headerValue);
                            System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Header: {header.Key} = {headerValue}");
                        }

                        var jsonContent = JsonSerializer.Serialize(requestBody);
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Request body length: {jsonContent.Length} characters");
                        
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Sending POST request...");
                        var startTime = DateTime.UtcNow;
                        
                        var response = await _httpClient.PostAsync(url,
                            new StringContent(jsonContent, Encoding.UTF8, "application/json"));
                        
                        var duration = DateTime.UtcNow - startTime;
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Response received in {duration.TotalMilliseconds:F0}ms. Status: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Success! Response length: {responseContent.Length} characters");
                            
                            try
                            {
                                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                                var parsedResult = responseParser(result);
                                System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Response parsed successfully. Parsed length: {parsedResult?.Length ?? 0}");
                                return parsedResult ?? $"{providerName} response parsing returned null";
                            }
                            catch (Exception parseEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: ERROR parsing response: {parseEx.Message}");
                                return $"{providerName} response parsing error: {parseEx.Message}";
                            }
                        }

                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Error response. Status: {response.StatusCode}, Content length: {errorContent.Length}");
                        
                        // Handle specific error codes
                        if ((int)response.StatusCode == 429 || (int)response.StatusCode == 529)
                        {
                            // Rate limiting (429) or service overloaded (529)
                            if (attempt < maxRetries)
                            {
                                var delay = baseDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                                System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Rate limited (429/529). Waiting {delay}ms before retry...");
                                await Task.Delay(delay);
                                continue; // Retry the request
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] All attempts exhausted due to rate limiting");
                            return GetFriendlyErrorMessage(response.StatusCode, providerName, errorContent);
                        }

                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Non-retryable error. Returning error message.");
                        return $"{providerName} API Error {response.StatusCode}: {errorContent}";
                    }
                    finally
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Releasing HTTP semaphore");
                        _httpSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Exception occurred: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Exception type: {ex.GetType().Name}");
                    
                    if (attempt < maxRetries)
                    {
                        var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                        System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] Attempt {attempt + 1}: Waiting {delay}ms before retry...");
                        await Task.Delay(delay);
                        continue; // Retry on exception
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] All attempts exhausted due to exceptions");
                    return $"{providerName} API error: {ex.Message}";
                }
            }

            System.Diagnostics.Debug.WriteLine($"[AIService] [HTTP] [{providerName}] All {maxRetries + 1} attempts failed");
            return $"{providerName} API failed after {maxRetries + 1} attempts";
        }

        /// <summary>
        /// Provides user-friendly error messages for common API errors
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="providerName">AI provider name</param>
        /// <param name="errorContent">Raw error content</param>
        /// <returns>User-friendly error message</returns>
        private static string GetFriendlyErrorMessage(System.Net.HttpStatusCode statusCode, string providerName, string errorContent)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests => // 429
                    $"⚠️ **{providerName} Rate Limit Exceeded**\n\n" +
                    "The API is receiving too many requests. This is temporary and should resolve shortly.\n\n" +
                    "**Recommendation**: Wait a few minutes and try again, or use a smaller batch of packages for analysis.\n\n" +
                    "**Technical Details**: Rate limiting helps ensure fair access to AI services for all users.",
                
                (System.Net.HttpStatusCode)529 => // Service Overloaded
                    $"⚠️ **{providerName} Service Temporarily Overloaded**\n\n" +
                    "The AI service is experiencing high demand and is temporarily overloaded.\n\n" +
                    "**Recommendation**: \n" +
                    "- ✅ Try again in a few minutes\n" +
                    "- ✅ Analyze fewer packages at once\n" +
                    "- ✅ Use off-peak hours for large batches\n\n" +
                    "**Status**: This is a temporary service condition, not an error with your configuration.\n\n" +
                    "**Alternative**: Consider switching to Perplexity in Settings if Claude is consistently overloaded.",
                
                System.Net.HttpStatusCode.Unauthorized => // 401
                    $"🔑 **{providerName} Authentication Error**\n\n" +
                    "Your API key appears to be invalid or expired.\n\n" +
                    "**Action Required**: Please check your API key in Settings and ensure it's correctly entered.",
                
                System.Net.HttpStatusCode.PaymentRequired => // 402
                    $"💳 **{providerName} Account Issue**\n\n" +
                    "Your account may have exceeded usage limits or requires payment.\n\n" +
                    "**Action Required**: Check your account status on the {providerName} dashboard.",
                
                _ => $"{providerName} API Error {statusCode}: {errorContent}"
            };
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
            var safeSoftwareName = System.Net.WebUtility.HtmlEncode(softwareName);
            var safePackageId = System.Net.WebUtility.HtmlEncode(packageId);
            var safeCurrentVersion = System.Net.WebUtility.HtmlEncode(currentVersion);
            var safeNewVersion = System.Net.WebUtility.HtmlEncode(newVersion);
            
            return $@"Research the software application {safeSoftwareName} (package: {safePackageId}) and its upgrade from version {safeCurrentVersion} to {safeNewVersion}.

Provide comprehensive information about:

**Application Overview:**
1. What is {safeSoftwareName} and what does it do?
2. Who develops/maintains this software?
3. What category/type of application is it?
4. Is it free, paid, or freemium?
5. What are its main features and use cases?

**Version Analysis:**
6. What changed between version {safeCurrentVersion} and {safeNewVersion}?
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
            var safeName = System.Net.WebUtility.HtmlEncode(app.Name);
            var safeId = System.Net.WebUtility.HtmlEncode(app.Id);
            var safeVersion = System.Net.WebUtility.HtmlEncode(app.Version);
            var safeAvailable = System.Net.WebUtility.HtmlEncode(app.Available);
            
            return $@"# 🔍 Software Analysis & Upgrade Report: {safeName}

You are a senior software analyst. Format the following research data into a comprehensive software analysis and upgrade recommendation report.

## Research Data:
{researchData}

## Package Details:
- **Package ID**: `{safeId}`
- **Current Version**: `{safeVersion}`
- **Available Version**: `{safeAvailable}`

## Required Report Format:

Provide your analysis in this **exact markdown structure**:

### 📋 **Application Overview**
- **Software Name**: {safeName}
- **Developer/Publisher**: [From research]
- **Category**: [Application type/category]
- **License**: [Free/Paid/Freemium]
- **Primary Purpose**: [What the software does]
- **Key Features**: [Main functionality]

### 🎯 **Executive Summary**
> 🟢 RECOMMENDED / 🟡 CONDITIONAL / 🔴 NOT RECOMMENDED

Brief 1-2 sentence recommendation with urgency level.

### 🔄 **Version Changes**
- **Current Version**: `{safeVersion}`
- **Target Version**: `{safeAvailable}`
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
            _bedrockService?.Dispose();
        }
    }
}
