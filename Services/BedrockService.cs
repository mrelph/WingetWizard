using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;
using UpgradeApp.Models;
using UpgradeApp.Utils;
using System.Linq; // Added for Count()

namespace UpgradeApp.Services
{
    /// <summary>
    /// Service class for AWS Bedrock integration with multiple model support
    /// Provides secure access to AWS Bedrock models with IAM authentication
    /// </summary>
    public class BedrockService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKeyId;
        private readonly string _secretAccessKey;
        private readonly string _region;
        private readonly string _selectedModel;

        public BedrockService(
            string accessKeyId, 
            string secretAccessKey, 
            string region, 
            string selectedModel)
        {
            _httpClient = new HttpClient();
            _accessKeyId = accessKeyId?.Trim() ?? "";
            _secretAccessKey = secretAccessKey?.Trim() ?? "";
            _region = region?.Trim() ?? AppConstants.DEFAULT_AWS_REGION;
            _selectedModel = selectedModel?.Trim() ?? AppConstants.BEDROCK_CLAUDE_35_SONNET_V2;
        }

        /// <summary>
        /// Gets available Bedrock models dynamically from AWS API
        /// </summary>
        /// <param name="forceRefresh">Force refresh from API</param>
        /// <returns>List of available models</returns>
        public async Task<List<BedrockModelDiscoveryService.BedrockModel>> GetAvailableModelsAsync(bool forceRefresh = false)
        {
            try
            {
                var discoveryService = new BedrockModelDiscoveryService(_accessKeyId, _secretAccessKey, _region);
                return await discoveryService.GetTextModelsAsync(forceRefresh);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to discover Bedrock models - Error: {ex.Message}");
                return new List<BedrockModelDiscoveryService.BedrockModel>();
            }
        }

        /// <summary>
        /// Gets recommended models grouped by use case
        /// </summary>
        /// <returns>Dictionary of use case to recommended model</returns>
        public async Task<Dictionary<string, BedrockModelDiscoveryService.BedrockModel>> GetRecommendedModelsAsync()
        {
            try
            {
                var models = await GetAvailableModelsAsync();
                return BedrockModelDiscoveryService.GetRecommendedModels(models);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get recommended models - Error: {ex.Message}");
                return new Dictionary<string, BedrockModelDiscoveryService.BedrockModel>();
            }
        }

        /// <summary>
        /// Validates that the selected model is available in the current region
        /// </summary>
        /// <returns>True if model is available</returns>
        public async Task<bool> ValidateSelectedModelAsync()
        {
            try
            {
                var discoveryService = new BedrockModelDiscoveryService(_accessKeyId, _secretAccessKey, _region);
                return await discoveryService.IsModelAvailableAsync(_selectedModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not validate model availability - Model: {_selectedModel}, Region: {_region}, Error: {ex.Message}");
                return true; // Assume available if validation fails
            }
        }

        /// <summary>
        /// Gets AI recommendation using AWS Bedrock
        /// </summary>
        /// <param name="app">The package to analyze</param>
        /// <returns>AI-generated recommendation</returns>
        public async Task<string> GetAIRecommendationAsync(UpgradableApp app)
        {
            System.Diagnostics.Debug.WriteLine($"[BedrockService] Starting AI recommendation for package: {app.Name} (ID: {app.Id})");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] Using model: {_selectedModel}");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] AWS Region: {_region}");
            
            try
            {
                if (string.IsNullOrEmpty(_accessKeyId) || string.IsNullOrEmpty(_secretAccessKey))
                {
                    System.Diagnostics.Debug.WriteLine($"[BedrockService] ERROR: AWS Bedrock credentials not configured");
                    System.Diagnostics.Debug.WriteLine($"[BedrockService] Access Key ID present: {!string.IsNullOrEmpty(_accessKeyId)}");
                    System.Diagnostics.Debug.WriteLine($"[BedrockService] Secret Access Key present: {!string.IsNullOrEmpty(_secretAccessKey)}");
                    return "AWS Bedrock credentials not configured";
                }

                System.Diagnostics.Debug.WriteLine($"[BedrockService] AWS credentials configured. Access Key ID length: {_accessKeyId.Length}");
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Creating analysis prompt for {app.Name}...");

                var prompt = CreateSoftwareAnalysisPrompt(app.Name, app.Id, app.Version, app.Available);
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Analysis prompt created. Length: {prompt.Length} characters");

                var requestBody = CreateBedrockRequestBody(prompt);
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Bedrock request body created");

                var endpoint = $"https://bedrock-runtime.{_region}.amazonaws.com/model/{_selectedModel}/invoke";
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Making signed request to endpoint: {endpoint}");

                var startTime = DateTime.UtcNow;
                var response = await MakeSignedBedrockRequestAsync(endpoint, requestBody);
                var duration = DateTime.UtcNow - startTime;
                
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Bedrock request completed in {duration.TotalMilliseconds:F0}ms");
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Response received. Length: {response?.Length ?? 0} characters");
                System.Diagnostics.Debug.WriteLine($"[BedrockService] Bedrock AI recommendation generated successfully - Model: {_selectedModel}, Package: {app.Id}");

                return response;
            }
            catch (Exception ex)
            {
                var error = $"Bedrock AI analysis failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[BedrockService] ERROR: Bedrock AI request failed - Model: {_selectedModel}, Package: {app.Id}");
                System.Diagnostics.Debug.WriteLine($"[BedrockService] ERROR: Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[BedrockService] ERROR: Exception message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BedrockService] ERROR: Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"[BedrockService] ERROR: Stack trace: {ex.StackTrace}");
                return error;
            }
        }

        /// <summary>
        /// Creates comprehensive software analysis prompt for Bedrock models
        /// </summary>
        /// <param name="softwareName">Name of the software</param>
        /// <param name="packageId">Package identifier</param>
        /// <param name="currentVersion">Current version</param>
        /// <param name="newVersion">Available version</param>
        /// <returns>Structured prompt for AI analysis</returns>
        private static string CreateSoftwareAnalysisPrompt(string softwareName, string packageId, string currentVersion, string newVersion)
        {
            var safeName = ValidationUtils.ValidateSecureInput(softwareName, "softwareName", new List<string>()) ?? "Unknown";
            var safeId = ValidationUtils.ValidateSecureInput(packageId, "packageId", new List<string>()) ?? "Unknown";
            var safeCurrentVersion = ValidationUtils.ValidateSecureInput(currentVersion, "version", new List<string>()) ?? "Unknown";
            var safeNewVersion = ValidationUtils.ValidateSecureInput(newVersion, "version", new List<string>()) ?? "Unknown";

            return $@"You are a senior software analyst. Analyze the following software upgrade scenario and provide a comprehensive recommendation.

## Software Details:
- **Name**: {safeName}
- **Package ID**: {safeId}
- **Current Version**: {safeCurrentVersion}
- **Available Version**: {safeNewVersion}

## Analysis Requirements:

Provide your analysis in this exact markdown structure:

### 📋 **Application Overview**
- **Software Name**: {safeName}
- **Developer/Publisher**: [Research required]
- **Category**: [Application type]
- **License**: [Free/Paid/Freemium]
- **Primary Purpose**: [What the software does]
- **Key Features**: [Main functionality]

### 🎯 **Executive Summary**
> 🟢 RECOMMENDED / 🟡 CONDITIONAL / 🔴 NOT RECOMMENDED

Brief 1-2 sentence recommendation with urgency level.

### 🔄 **Version Changes**
- **Current Version**: `{safeCurrentVersion}`
- **Target Version**: `{safeNewVersion}`
- **Update Type**: 🔵 Major / 🟡 Minor / 🟢 Patch / 🔴 Breaking
- **Release Date**: [Date if known]

### ⚡ **Key Improvements**
- 🆕 **New Features**: List major new functionality
- 🐛 **Bug Fixes**: Critical issues resolved
- 🔧 **Enhancements**: Performance and usability improvements
- 📊 **Performance**: Speed/resource impact changes

### 🔒 **Security Assessment**
- 🛡️ **Security Updates**: Vulnerabilities fixed
- 🚨 **Risk Level**: 🟢 Low / 🟡 Medium / 🔴 High / 🟣 Critical
- 🔍 **CVE Information**: Any CVEs addressed
- 📋 **Security Notes**: Additional security considerations

### ⚠️ **Risk Analysis**
- 🔧 **Compatibility**: System and software compatibility
- 📱 **Dependencies**: Required updates or conflicts
- 🔄 **Rollback**: Ease of reverting if issues occur
- ⏱️ **Timing**: Best time to apply update

### 📊 **Recommendation Details**
- **Priority Level**: 🔴 Urgent / 🟡 Medium / 🟢 Low
- **Action Timeline**: Immediate / This Week / This Month / When Convenient
- **Prerequisites**: Any required preparations
- **Best Practices**: Implementation recommendations

### 📝 **Additional Notes**
- Any special considerations
- Alternative solutions if applicable
- Links to release notes or documentation

Focus on security, stability, and practical business impact. Be specific about risks and benefits.";
        }

        /// <summary>
        /// Creates request body for different Bedrock models
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <returns>Request body object</returns>
        private object CreateBedrockRequestBody(string prompt)
        {
            return _selectedModel switch
            {
                var model when model.StartsWith("anthropic.claude") => new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 4000,
                    messages = new[] { new { role = "user", content = prompt } }
                },
                
                var model when model.StartsWith("meta.llama") => new
                {
                    prompt = prompt,
                    max_gen_len = 4000,
                    temperature = 0.1,
                    top_p = 0.9
                },
                
                var model when model.StartsWith("amazon.titan") => new
                {
                    inputText = prompt,
                    textGenerationConfig = new
                    {
                        maxTokenCount = 4000,
                        stopSequences = new string[0],
                        temperature = 0.1,
                        topP = 0.9
                    }
                },
                
                _ => new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 4000,
                    messages = new[] { new { role = "user", content = prompt } }
                }
            };
        }

        /// <summary>
        /// Makes a signed HTTP request to AWS Bedrock
        /// </summary>
        /// <param name="endpoint">API endpoint URL</param>
        /// <param name="requestBody">Request payload</param>
        /// <returns>Response content</returns>
        private async Task<string> MakeSignedBedrockRequestAsync(string endpoint, object requestBody)
        {
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Starting signed Bedrock request to: {endpoint}");
            
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Request payload serialized. Length: {jsonPayload.Length} characters, {payloadBytes.Length} bytes");

            var uri = new Uri(endpoint);
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            // Add AWS Signature Version 4 authentication
            var timestamp = DateTime.UtcNow;
            var dateStamp = timestamp.ToString("yyyyMMdd");
            var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Request timestamp: {timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Date stamp: {dateStamp}, AMZ date: {amzDate}");

            // Add required headers
            request.Headers.Add("X-Amz-Date", amzDate);
            request.Headers.Add("Host", uri.Host);
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Added headers - X-Amz-Date: {amzDate}, Host: {uri.Host}");

            // Create the signature
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Creating AWS Signature Version 4...");
            var signature = CreateAwsSignature(request, payloadBytes, timestamp);
            request.Headers.Add("Authorization", signature);
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Authorization header added. Signature length: {signature.Length}");

            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Making signed Bedrock API request - Endpoint: {uri.Host}, Model: {_selectedModel}");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Request method: {request.Method}, URI: {request.RequestUri}");

            var startTime = DateTime.UtcNow;
            var response = await _httpClient.SendAsync(request);
            var duration = DateTime.UtcNow - startTime;
            
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Response received in {duration.TotalMilliseconds:F0}ms. Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Response headers count: {response.Headers.Count()}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Response content length: {responseContent.Length} characters");

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Success! Parsing Bedrock response...");
                var parsedResponse = ParseBedrockResponse(responseContent);
                System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Response parsed successfully. Parsed length: {parsedResponse?.Length ?? 0}");
                return parsedResponse;
            }

            var error = $"Bedrock API Error {response.StatusCode}: {responseContent}";
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] ERROR: {error}");
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Getting friendly error message...");
            var friendlyError = GetFriendlyBedrockError(response.StatusCode, responseContent);
            System.Diagnostics.Debug.WriteLine($"[BedrockService] [HTTP] Friendly error message: {friendlyError}");
            return friendlyError;
        }

        /// <summary>
        /// Creates AWS Signature Version 4 for Bedrock authentication
        /// </summary>
        /// <param name="request">HTTP request message</param>
        /// <param name="payloadBytes">Request payload bytes</param>
        /// <param name="timestamp">Request timestamp</param>
        /// <returns>Authorization header value</returns>
        private string CreateAwsSignature(HttpRequestMessage request, byte[] payloadBytes, DateTime timestamp)
        {
            var algorithm = "AWS4-HMAC-SHA256";
            var serviceName = AppConstants.BEDROCK_SERVICE_NAME;
            var dateStamp = timestamp.ToString("yyyyMMdd");
            var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

            // Create canonical request
            var canonicalUri = request.RequestUri?.AbsolutePath ?? "/";
            var canonicalQueryString = request.RequestUri?.Query?.TrimStart('?') ?? "";
            var canonicalHeaders = $"host:{request.RequestUri?.Host}\nx-amz-date:{amzDate}\n";
            var signedHeaders = "host;x-amz-date";
            var payloadHash = ComputeSha256Hash(payloadBytes);

            var canonicalRequest = $"{request.Method}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

            // Create string to sign
            var credentialScope = $"{dateStamp}/{_region}/{serviceName}/aws4_request";
            var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(Encoding.UTF8.GetBytes(canonicalRequest))}";

            // Calculate signature
            var signingKey = GetSignatureKey(_secretAccessKey, dateStamp, _region, serviceName);
            var signature = ComputeHmacSha256(signingKey, stringToSign);

            // Create authorization header
            return $"{algorithm} Credential={_accessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        }

        /// <summary>
        /// Computes SHA256 hash of input bytes
        /// </summary>
        /// <param name="input">Input bytes</param>
        /// <returns>Hex-encoded hash</returns>
        private static string ComputeSha256Hash(byte[] input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(input);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Computes HMAC-SHA256 hash
        /// </summary>
        /// <param name="key">Signing key</param>
        /// <param name="data">Data to sign</param>
        /// <returns>Hex-encoded signature</returns>
        private static string ComputeHmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Generates AWS signing key for Signature Version 4
        /// </summary>
        /// <param name="secretKey">AWS secret access key</param>
        /// <param name="dateStamp">Date stamp</param>
        /// <param name="regionName">AWS region</param>
        /// <param name="serviceName">AWS service name</param>
        /// <returns>Signing key bytes</returns>
        private static byte[] GetSignatureKey(string secretKey, string dateStamp, string regionName, string serviceName)
        {
            var kDate = ComputeHmacSha256Bytes(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
            var kRegion = ComputeHmacSha256Bytes(kDate, regionName);
            var kService = ComputeHmacSha256Bytes(kRegion, serviceName);
            var kSigning = ComputeHmacSha256Bytes(kService, "aws4_request");
            return kSigning;
        }

        /// <summary>
        /// Computes HMAC-SHA256 and returns bytes
        /// </summary>
        /// <param name="key">Signing key</param>
        /// <param name="data">Data to sign</param>
        /// <returns>Hash bytes</returns>
        private static byte[] ComputeHmacSha256Bytes(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Parses response from different Bedrock models
        /// </summary>
        /// <param name="responseContent">Raw response content</param>
        /// <returns>Extracted text response</returns>
        private string ParseBedrockResponse(string responseContent)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                return _selectedModel switch
                {
                    var model when model.StartsWith("anthropic.claude") =>
                        jsonResponse.GetProperty("content")[0].GetProperty("text").GetString() ?? "No response",
                    
                    var model when model.StartsWith("meta.llama") =>
                        jsonResponse.GetProperty("generation").GetString() ?? "No response",
                    
                    var model when model.StartsWith("amazon.titan") =>
                        jsonResponse.GetProperty("results")[0].GetProperty("outputText").GetString() ?? "No response",
                    
                    _ => jsonResponse.ToString()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse Bedrock response - Error: {ex.Message}");
                return $"Failed to parse Bedrock response: {ex.Message}";
            }
        }

        /// <summary>
        /// Provides user-friendly error messages for Bedrock errors
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="errorContent">Error response content</param>
        /// <returns>User-friendly error message</returns>
        private static string GetFriendlyBedrockError(System.Net.HttpStatusCode statusCode, string errorContent)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => // 401
                    "🔑 **AWS Bedrock Authentication Error**\n\n" +
                    "Your AWS credentials appear to be invalid or expired.\n\n" +
                    "**Action Required**: Please check your AWS Access Key ID and Secret Access Key in Settings.",
                
                System.Net.HttpStatusCode.Forbidden => // 403
                    "🚫 **AWS Bedrock Access Denied**\n\n" +
                    "Your AWS credentials don't have permission to access Bedrock models.\n\n" +
                    "**Action Required**: Ensure your AWS IAM user has the `bedrock:InvokeModel` permission.",
                
                System.Net.HttpStatusCode.BadRequest => // 400
                    "⚠️ **AWS Bedrock Request Error**\n\n" +
                    "The request format or model selection may be incorrect.\n\n" +
                    "**Recommendation**: Try switching to a different Bedrock model in Settings.",
                
                System.Net.HttpStatusCode.TooManyRequests => // 429
                    "⚠️ **AWS Bedrock Rate Limit Exceeded**\n\n" +
                    "Too many requests to Bedrock. This will resolve shortly.\n\n" +
                    "**Recommendation**: Wait a few minutes before trying again.",
                
                _ => $"AWS Bedrock Error {statusCode}: {errorContent}"
            };
        }

        /// <summary>
        /// Disposes of the HTTP client
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}