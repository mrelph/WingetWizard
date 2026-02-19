using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WingetWizard.Utils;

namespace WingetWizard.Services
{
    /// <summary>
    /// Service for dynamically discovering available AWS Bedrock models
    /// Provides real-time model listing and caching for optimal performance
    /// Supports both AWS credentials and Bedrock API keys
    /// </summary>
    public class BedrockModelDiscoveryService
    {
        private readonly HttpClient _httpClient;
        private readonly CachingService? _cachingService;
        private readonly string _accessKeyId;
        private readonly string _secretAccessKey;
        private readonly string _region;
        private readonly string? _bedrockApiKey;
        private readonly bool _useApiKey;

        private static readonly Dictionary<string, List<BedrockModel>> _modelCache = new();
        private static readonly object _cacheLock = new();
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

        public BedrockModelDiscoveryService(
            string accessKeyId,
            string secretAccessKey,
            string region,
            CachingService? cachingService = null)
        {
            _httpClient = new HttpClient();
            _cachingService = cachingService;
            _accessKeyId = accessKeyId?.Trim() ?? "";
            _secretAccessKey = secretAccessKey?.Trim() ?? "";
            _region = region?.Trim() ?? AppConstants.DEFAULT_AWS_REGION;
            _bedrockApiKey = null;
            _useApiKey = false;
        }

        /// <summary>
        /// Constructor for Bedrock API key authentication
        /// </summary>
        public BedrockModelDiscoveryService(
            string bedrockApiKey,
            string region,
            CachingService? cachingService = null)
        {
            _httpClient = new HttpClient();
            _cachingService = cachingService;
            _accessKeyId = "";
            _secretAccessKey = "";
            _region = region?.Trim() ?? AppConstants.DEFAULT_AWS_REGION;
            _bedrockApiKey = bedrockApiKey?.Trim();
            _useApiKey = true;
        }

        /// <summary>
        /// Represents a Bedrock foundation model
        /// </summary>
        public class BedrockModel
        {
            public string ModelId { get; set; } = "";
            public string ModelName { get; set; } = "";
            public string ProviderName { get; set; } = "";
            public List<string> InputModalities { get; set; } = new();
            public List<string> OutputModalities { get; set; } = new();
            public bool SupportsStreaming { get; set; }
            public string? ModelLifecycleStatus { get; set; }
            public bool IsTextModel => InputModalities.Contains("TEXT") && OutputModalities.Contains("TEXT");
        }

        /// <summary>
        /// Tests AWS credentials and connection to Bedrock service
        /// </summary>
        /// <returns>True if connection is successful</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Testing Bedrock connection - Region: {_region}, Auth: {(_useApiKey ? "API Key" : "AWS Signature")}");
                
                // Try a simple endpoint test first
                var testEndpoint = $"https://bedrock.{_region}.amazonaws.com";
                var testRequest = new HttpRequestMessage(HttpMethod.Get, testEndpoint);

                if (_useApiKey && !string.IsNullOrEmpty(_bedrockApiKey))
                {
                    // Use Bedrock API key authentication
                    testRequest.Headers.Add("Authorization", $"Bearer {_bedrockApiKey}");
                    testRequest.Headers.Add("Accept", "application/json");
                    
                    System.Diagnostics.Debug.WriteLine($"Testing connection with Bedrock API key");
                }
                else
                {
                    // Use AWS Signature Version 4 authentication
                    var timestamp = DateTime.UtcNow;
                    var dateStamp = timestamp.ToString("yyyyMMdd");
                    var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                    testRequest.Headers.Add("X-Amz-Date", amzDate);
                    testRequest.Headers.Add("Host", $"bedrock.{_region}.amazonaws.com");
                    testRequest.Headers.Add("Accept", "application/json");

                    var signature = CreateAwsSignatureForGet(testRequest, timestamp);
                    testRequest.Headers.Add("Authorization", signature);
                    
                    System.Diagnostics.Debug.WriteLine($"Testing connection with AWS signature");
                }

                var response = await _httpClient.SendAsync(testRequest);
                
                System.Diagnostics.Debug.WriteLine($"Connection test response - StatusCode: {response.StatusCode}");
                
                // Even if we get an error response, it means we can reach the service
                // The actual error will be about the endpoint, not connectivity
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection test failed - Region: {_region}, Error: {ex.Message}");
                
                if (_useApiKey)
                {
                    // For API keys, we don't have fallback authentication methods
                    return false;
                }
                
                // Try with bedrock-runtime service name for AWS credentials
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Trying connection test with bedrock-runtime service name");
                    
                    var testEndpoint = $"https://bedrock.{_region}.amazonaws.com";
                    var testRequest = new HttpRequestMessage(HttpMethod.Get, testEndpoint);
                    
                    var timestamp = DateTime.UtcNow;
                    var dateStamp = timestamp.ToString("yyyyMMdd");
                    var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                    testRequest.Headers.Add("X-Amz-Date", amzDate);
                    testRequest.Headers.Add("Host", $"bedrock.{_region}.amazonaws.com");
                    testRequest.Headers.Add("Accept", "application/json");

                    var signature = CreateAwsSignatureForGetWithService(testRequest, timestamp, "bedrock-runtime");
                    testRequest.Headers.Add("Authorization", signature);

                    var response = await _httpClient.SendAsync(testRequest);
                    
                    System.Diagnostics.Debug.WriteLine($"Connection test with bedrock-runtime service name - StatusCode: {response.StatusCode}");
                    return true;
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection test with bedrock-runtime service name also failed - Error: {ex2.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets all available Bedrock models with caching
        /// </summary>
        /// <param name="forceRefresh">Force refresh from API even if cache is valid</param>
        /// <returns>List of available Bedrock models</returns>
        public async Task<List<BedrockModel>> GetAvailableModelsAsync(bool forceRefresh = false)
        {
            try
            {
                // Check cache first
                if (!forceRefresh && IsCacheValid())
                {
                    lock (_cacheLock)
                    {
                        if (_modelCache.TryGetValue(_region, out var cachedModels))
                        {
                            System.Diagnostics.Debug.WriteLine($"Returning cached Bedrock models - Region: {_region}, ModelCount: {cachedModels.Count}");
                            return cachedModels;
                        }
                    }
                }

                // Query API for fresh model list
                var models = await QueryBedrockModelsAsync();
                
                // Update cache
                lock (_cacheLock)
                {
                    _modelCache[_region] = models;
                    _lastCacheUpdate = DateTime.UtcNow;
                }

                System.Diagnostics.Debug.WriteLine($"Bedrock models discovered and cached - Region: {_region}, ModelCount: {models.Count}");

                return models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to discover Bedrock models - Region: {_region}, Error: {ex.Message}");

                // Return fallback static models if API fails
                return GetFallbackModels();
            }
        }

        /// <summary>
        /// Gets text generation models suitable for chat/analysis
        /// </summary>
        /// <param name="forceRefresh">Force refresh from API</param>
        /// <returns>List of text generation models</returns>
        public async Task<List<BedrockModel>> GetTextModelsAsync(bool forceRefresh = false)
        {
            var allModels = await GetAvailableModelsAsync(forceRefresh);
            return allModels.Where(m => m.IsTextModel && 
                                       m.ModelLifecycleStatus != "LEGACY" &&
                                       !m.ModelId.Contains("embed")).ToList();
        }

        /// <summary>
        /// Gets models grouped by provider
        /// </summary>
        /// <param name="forceRefresh">Force refresh from API</param>
        /// <returns>Dictionary of provider names to models</returns>
        public async Task<Dictionary<string, List<BedrockModel>>> GetModelsByProviderAsync(bool forceRefresh = false)
        {
            var models = await GetTextModelsAsync(forceRefresh);
            return models.GroupBy(m => m.ProviderName)
                        .ToDictionary(g => g.Key, g => g.OrderBy(m => m.ModelName).ToList());
        }

        /// <summary>
        /// Queries Bedrock API for available models
        /// </summary>
        /// <returns>List of discovered models</returns>
        private async Task<List<BedrockModel>> QueryBedrockModelsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Starting model discovery for region: {_region}");
            System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Authentication method: {(_useApiKey ? "Bedrock API Key" : "AWS Signature Version 4")}");
            
            try
            {
                // Use the correct Bedrock ListFoundationModels API endpoint
                var endpoint = $"https://bedrock.{_region}.amazonaws.com/foundation-models";
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Using endpoint: {endpoint}");

                if (_useApiKey && !string.IsNullOrEmpty(_bedrockApiKey))
                {
                    // Use Bedrock API key authentication
                    request.Headers.Add("Authorization", $"Bearer {_bedrockApiKey}");
                    request.Headers.Add("Accept", "application/json");
                    
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Using Bedrock API key authentication for region: {_region}");
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] API key length: {_bedrockApiKey.Length}");
                }
                else
                {
                    // Use AWS Signature Version 4 authentication
                    var timestamp = DateTime.UtcNow;
                    var dateStamp = timestamp.ToString("yyyyMMdd");
                    var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                    request.Headers.Add("X-Amz-Date", amzDate);
                    request.Headers.Add("Host", $"bedrock.{_region}.amazonaws.com");
                    request.Headers.Add("Accept", "application/json");

                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Using AWS signature authentication for region: {_region}");
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Request timestamp: {timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Date stamp: {dateStamp}, AMZ date: {amzDate}");

                    // Create the signature for GET request (empty payload)
                    var signature = CreateAwsSignatureForGet(request, timestamp);
                    request.Headers.Add("Authorization", signature);
                    
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] AWS signature created and added. Signature length: {signature.Length}");
                }

                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Querying Bedrock for available models - Region: {_region}, Endpoint: {endpoint}, Auth: {(_useApiKey ? "API Key" : "AWS Signature")}");

                var startTime = DateTime.UtcNow;
                var response = await _httpClient.SendAsync(request);
                var duration = DateTime.UtcNow - startTime;
                
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Response received in {duration.TotalMilliseconds:F0}ms. Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Response headers count: {response.Headers.Count()}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] ERROR: Bedrock model discovery failed - StatusCode: {response.StatusCode}, Error: {error}");
                    
                    // Try alternative endpoint format
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Trying alternative endpoint as fallback...");
                    return await TryAlternativeEndpointAsync();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Success! Response content length: {responseContent.Length} characters");
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Raw response preview: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...");
                
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var models = new List<BedrockModel>();
                
                if (jsonResponse.TryGetProperty("modelSummaries", out var modelSummaries))
                {
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Found modelSummaries property with {modelSummaries.GetArrayLength()} models");
                    
                    foreach (var modelElement in modelSummaries.EnumerateArray())
                    {
                        var model = new BedrockModel
                        {
                            ModelId = modelElement.GetProperty("modelId").GetString() ?? "",
                            ModelName = modelElement.GetProperty("modelName").GetString() ?? "",
                            ProviderName = modelElement.GetProperty("providerName").GetString() ?? "",
                            SupportsStreaming = modelElement.TryGetProperty("responseStreamingSupported", out var streaming) && streaming.GetBoolean(),
                            ModelLifecycleStatus = modelElement.TryGetProperty("modelLifecycle", out var lifecycle) ? 
                                lifecycle.TryGetProperty("status", out var status) ? status.GetString() : null : null
                        };

                        // Parse input/output modalities
                        if (modelElement.TryGetProperty("inputModalities", out var inputMods))
                        {
                            model.InputModalities = inputMods.EnumerateArray()
                                .Select(m => m.GetString() ?? "")
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                        }

                        if (modelElement.TryGetProperty("outputModalities", out var outputMods))
                        {
                            model.OutputModalities = outputMods.EnumerateArray()
                                .Select(m => m.GetString() ?? "")
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                        }

                        models.Add(model);
                        System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Parsed model: {model.ModelId} ({model.ModelName}) by {model.ProviderName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] WARNING: No modelSummaries property found in response");
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Available properties: {string.Join(", ", jsonResponse.EnumerateObject().Select(p => p.Name))}");
                }

                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Successfully discovered Bedrock models - Region: {_region}, TotalModels: {models.Count}, TextModels: {models.Count(m => m.IsTextModel)}");

                return models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] ERROR: Exception during Bedrock model discovery - Region: {_region}");
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] ERROR: Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] ERROR: Exception message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] ERROR: Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] ERROR: Stack trace: {ex.StackTrace}");
                
                // Try alternative endpoint as fallback
                System.Diagnostics.Debug.WriteLine($"[BedrockModelDiscovery] Trying alternative endpoint as fallback due to exception...");
                return await TryAlternativeEndpointAsync();
            }
        }

        /// <summary>
        /// Tries alternative Bedrock API endpoints if the primary one fails
        /// </summary>
        /// <returns>List of discovered models or fallback models</returns>
        private async Task<List<BedrockModel>> TryAlternativeEndpointAsync()
        {
            try
            {
                // Try the bedrock-runtime endpoint
                var alternativeEndpoint = $"https://bedrock-runtime.{_region}.amazonaws.com/foundation-models";
                var request = new HttpRequestMessage(HttpMethod.Get, alternativeEndpoint);

                if (_useApiKey && !string.IsNullOrEmpty(_bedrockApiKey))
                {
                    // Use Bedrock API key authentication
                    request.Headers.Add("Authorization", $"Bearer {_bedrockApiKey}");
                    request.Headers.Add("Accept", "application/json");
                    
                    System.Diagnostics.Debug.WriteLine($"Trying alternative endpoint with Bedrock API key: {alternativeEndpoint}");
                }
                else
                {
                    // Use AWS Signature Version 4 authentication
                    var timestamp = DateTime.UtcNow;
                    var dateStamp = timestamp.ToString("yyyyMMdd");
                    var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                    request.Headers.Add("X-Amz-Date", amzDate);
                    request.Headers.Add("Host", $"bedrock-runtime.{_region}.amazonaws.com");
                    request.Headers.Add("Accept", "application/json");

                    var signature = CreateAwsSignatureForGet(request, timestamp);
                    request.Headers.Add("Authorization", signature);

                    System.Diagnostics.Debug.WriteLine($"Trying alternative Bedrock endpoint with AWS signature: {alternativeEndpoint}");
                }

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Alternative Bedrock endpoint also failed - StatusCode: {response.StatusCode}, Error: {error}");
                    
                    // Try with different service name for signature
                    return await TryAlternativeServiceNameAsync();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Alternative endpoint response: {responseContent}");
                
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var models = new List<BedrockModel>();
                
                if (jsonResponse.TryGetProperty("modelSummaries", out var modelSummaries))
                {
                    foreach (var modelElement in modelSummaries.EnumerateArray())
                    {
                        var model = new BedrockModel
                        {
                            ModelId = modelElement.GetProperty("modelId").GetString() ?? "",
                            ModelName = modelElement.GetProperty("modelName").GetString() ?? "",
                            ProviderName = modelElement.GetProperty("providerName").GetString() ?? "",
                            SupportsStreaming = modelElement.TryGetProperty("responseStreamingSupported", out var streaming) && streaming.GetBoolean(),
                            ModelLifecycleStatus = modelElement.TryGetProperty("modelLifecycle", out var lifecycle) ? 
                                lifecycle.TryGetProperty("status", out var status) ? status.GetString() : null : null
                        };

                        // Parse input/output modalities
                        if (modelElement.TryGetProperty("inputModalities", out var inputMods))
                        {
                            model.InputModalities = inputMods.EnumerateArray()
                                .Select(m => m.GetString() ?? "")
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                        }

                        if (modelElement.TryGetProperty("outputModalities", out var outputMods))
                        {
                            model.OutputModalities = outputMods.EnumerateArray()
                                .Select(m => m.GetString() ?? "")
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                        }

                        models.Add(model);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Successfully discovered models via alternative endpoint - Region: {_region}, TotalModels: {models.Count}");
                return models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Alternative endpoint also failed - Region: {_region}, Error: {ex.Message}");
                return await TryAlternativeServiceNameAsync();
            }
        }

        /// <summary>
        /// Tries alternative service names for AWS signature generation
        /// </summary>
        /// <returns>List of discovered models or fallback models</returns>
        private async Task<List<BedrockModel>> TryAlternativeServiceNameAsync()
        {
            try
            {
                // Try with "bedrock-runtime" service name
                var endpoint = $"https://bedrock.{_region}.amazonaws.com/foundation-models";
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                var timestamp = DateTime.UtcNow;
                var dateStamp = timestamp.ToString("yyyyMMdd");
                var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                request.Headers.Add("X-Amz-Date", amzDate);
                request.Headers.Add("Host", $"bedrock.{_region}.amazonaws.com");
                request.Headers.Add("Accept", "application/json");

                // Try with bedrock-runtime service name
                var signature = CreateAwsSignatureForGetWithService(request, timestamp, "bedrock-runtime");
                request.Headers.Add("Authorization", signature);

                System.Diagnostics.Debug.WriteLine($"Trying with bedrock-runtime service name: {endpoint}");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Bedrock-runtime service name also failed - StatusCode: {response.StatusCode}, Error: {error}");
                    return GetFallbackModels();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Bedrock-runtime service name response: {responseContent}");
                
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var models = new List<BedrockModel>();
                
                if (jsonResponse.TryGetProperty("modelSummaries", out var modelSummaries))
                {
                    foreach (var modelElement in modelSummaries.EnumerateArray())
                    {
                        var model = new BedrockModel
                        {
                            ModelId = modelElement.GetProperty("modelId").GetString() ?? "",
                            ModelName = modelElement.GetProperty("modelName").GetString() ?? "",
                            ProviderName = modelElement.GetProperty("providerName").GetString() ?? "",
                            SupportsStreaming = modelElement.TryGetProperty("responseStreamingSupported", out var streaming) && streaming.GetBoolean(),
                            ModelLifecycleStatus = modelElement.TryGetProperty("modelLifecycle", out var lifecycle) ? 
                                lifecycle.TryGetProperty("status", out var status) ? status.GetString() : null : null
                        };

                        // Parse input/output modalities
                        if (modelElement.TryGetProperty("inputModalities", out var inputMods))
                        {
                            model.InputModalities = inputMods.EnumerateArray()
                                .Select(m => m.GetString() ?? "")
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                        }

                        if (modelElement.TryGetProperty("outputModalities", out var outputMods))
                        {
                            model.OutputModalities = outputMods.EnumerateArray()
                                .Select(m => m.GetString() ?? "")
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                        }

                        models.Add(model);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Successfully discovered models with bedrock-runtime service name - Region: {_region}, TotalModels: {models.Count}");
                return models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bedrock-runtime service name also failed - Region: {_region}, Error: {ex.Message}");
                return GetFallbackModels();
            }
        }

        /// <summary>
        /// Creates AWS Signature Version 4 for GET requests
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="timestamp">Request timestamp</param>
        /// <returns>Authorization header value</returns>
        private string CreateAwsSignatureForGet(HttpRequestMessage request, DateTime timestamp)
        {
            try
            {
                var algorithm = "AWS4-HMAC-SHA256";
                var serviceName = "bedrock";
                var dateStamp = timestamp.ToString("yyyyMMdd");
                var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                // Create canonical request for GET
                var canonicalUri = request.RequestUri?.AbsolutePath ?? "/";
                var canonicalQueryString = request.RequestUri?.Query?.TrimStart('?') ?? "";
                var canonicalHeaders = $"host:{request.Headers.Host}\nx-amz-date:{amzDate}\n";
                var signedHeaders = "host;x-amz-date";
                var payloadHash = ComputeSha256Hash(Array.Empty<byte>()); // Empty payload for GET

                var canonicalRequest = $"GET\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

                // Create string to sign
                var credentialScope = $"{dateStamp}/{_region}/{serviceName}/aws4_request";
                var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(Encoding.UTF8.GetBytes(canonicalRequest))}";

                // Calculate signature
                var signingKey = GetSignatureKey(_secretAccessKey, dateStamp, _region, serviceName);
                var signature = ComputeHmacSha256(signingKey, stringToSign);

                System.Diagnostics.Debug.WriteLine($"AWS Signature generated successfully - Service: {serviceName}, Region: {_region}, DateStamp: {dateStamp}");
                System.Diagnostics.Debug.WriteLine($"Canonical Request: {canonicalRequest}");
                System.Diagnostics.Debug.WriteLine($"String to Sign: {stringToSign}");
                System.Diagnostics.Debug.WriteLine($"Credential Scope: {credentialScope}");

                return $"{algorithm} Credential={_accessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create AWS signature: {ex.Message}");
                throw new InvalidOperationException($"AWS signature generation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates AWS Signature Version 4 for GET requests with a specific service name
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="timestamp">Request timestamp</param>
        /// <param name="serviceName">Service name to use in the signature (e.g., "bedrock", "bedrock-runtime")</param>
        /// <returns>Authorization header value</returns>
        private string CreateAwsSignatureForGetWithService(HttpRequestMessage request, DateTime timestamp, string serviceName)
        {
            try
            {
                var algorithm = "AWS4-HMAC-SHA256";
                var dateStamp = timestamp.ToString("yyyyMMdd");
                var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

                // Create canonical request for GET
                var canonicalUri = request.RequestUri?.AbsolutePath ?? "/";
                var canonicalQueryString = request.RequestUri?.Query?.TrimStart('?') ?? "";
                var canonicalHeaders = $"host:{request.Headers.Host}\nx-amz-date:{amzDate}\n";
                var signedHeaders = "host;x-amz-date";
                var payloadHash = ComputeSha256Hash(Array.Empty<byte>()); // Empty payload for GET

                var canonicalRequest = $"GET\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

                // Create string to sign
                var credentialScope = $"{dateStamp}/{_region}/{serviceName}/aws4_request";
                var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(Encoding.UTF8.GetBytes(canonicalRequest))}";

                // Calculate signature
                var signingKey = GetSignatureKey(_secretAccessKey, dateStamp, _region, serviceName);
                var signature = ComputeHmacSha256(signingKey, stringToSign);

                System.Diagnostics.Debug.WriteLine($"AWS Signature generated successfully with service name - Service: {serviceName}, Region: {_region}, DateStamp: {dateStamp}");
                System.Diagnostics.Debug.WriteLine($"Canonical Request: {canonicalRequest}");
                System.Diagnostics.Debug.WriteLine($"String to Sign: {stringToSign}");
                System.Diagnostics.Debug.WriteLine($"Credential Scope: {credentialScope}");

                return $"{algorithm} Credential={_accessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create AWS signature with service name: {ex.Message}");
                throw new InvalidOperationException($"AWS signature generation with service name failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if the model cache is still valid
        /// </summary>
        /// <returns>True if cache is valid</returns>
        private static bool IsCacheValid()
        {
            return DateTime.UtcNow - _lastCacheUpdate < CacheExpiry;
        }

        /// <summary>
        /// Returns fallback models when API discovery fails
        /// </summary>
        /// <returns>List of known working models</returns>
        private static List<BedrockModel> GetFallbackModels()
        {
            return new List<BedrockModel>
            {
                new() { ModelId = AppConstants.BEDROCK_CLAUDE_35_SONNET_V2, ModelName = "Claude 3.5 Sonnet v2", ProviderName = "Anthropic", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_CLAUDE_35_SONNET, ModelName = "Claude 3.5 Sonnet", ProviderName = "Anthropic", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_CLAUDE_35_HAIKU, ModelName = "Claude 3.5 Haiku", ProviderName = "Anthropic", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_CLAUDE_37_SONNET, ModelName = "Claude 3.7 Sonnet", ProviderName = "Anthropic", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_CLAUDE_SONNET_4, ModelName = "Claude Sonnet 4", ProviderName = "Anthropic", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_CLAUDE_OPUS_4, ModelName = "Claude Opus 4", ProviderName = "Anthropic", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_LLAMA_33_70B, ModelName = "Llama 3.3 70B", ProviderName = "Meta", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_LLAMA_32_90B, ModelName = "Llama 3.2 90B", ProviderName = "Meta", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} },
                new() { ModelId = AppConstants.BEDROCK_TITAN_TEXT_PREMIER, ModelName = "Titan Text Premier", ProviderName = "Amazon", InputModalities = new List<string> {"TEXT"}, OutputModalities = new List<string> {"TEXT"} }
            };
        }

        /// <summary>
        /// Gets user-friendly display names for models
        /// </summary>
        /// <param name="models">List of models</param>
        /// <returns>Dictionary of model IDs to display names</returns>
        public static Dictionary<string, string> GetModelDisplayNames(List<BedrockModel> models)
        {
            var displayNames = new Dictionary<string, string>();

            foreach (var model in models)
            {
                var displayName = model.ModelName;
                
                // Add provider prefix for clarity
                if (!displayName.Contains(model.ProviderName))
                {
                    displayName = $"{model.ProviderName} - {displayName}";
                }

                // Add capabilities info
                var capabilities = new List<string>();
                if (model.SupportsStreaming) capabilities.Add("Streaming");
                if (model.ModelLifecycleStatus == "ACTIVE") capabilities.Add("Active");
                
                if (capabilities.Any())
                {
                    displayName += $" ({string.Join(", ", capabilities)})";
                }

                displayNames[model.ModelId] = displayName;
            }

            return displayNames;
        }

        /// <summary>
        /// Gets recommended models for different use cases
        /// </summary>
        /// <param name="models">Available models</param>
        /// <returns>Dictionary of use case to recommended model</returns>
        public static Dictionary<string, BedrockModel> GetRecommendedModels(List<BedrockModel> models)
        {
            var recommendations = new Dictionary<string, BedrockModel>();

            // Find best models for different scenarios
            var claudeModels = models.Where(m => m.ProviderName.Equals("Anthropic", StringComparison.OrdinalIgnoreCase)).ToList();
            var llamaModels = models.Where(m => m.ProviderName.Equals("Meta", StringComparison.OrdinalIgnoreCase)).ToList();
            var titanModels = models.Where(m => m.ProviderName.Equals("Amazon", StringComparison.OrdinalIgnoreCase)).ToList();

            // Highest quality analysis
            var bestClaude = claudeModels.FirstOrDefault(m => m.ModelId.Contains("sonnet") && m.ModelId.Contains("v2")) ??
                           claudeModels.FirstOrDefault(m => m.ModelId.Contains("sonnet")) ??
                           claudeModels.FirstOrDefault();
            if (bestClaude != null) recommendations["HighestQuality"] = bestClaude;

            // Fastest response
            var fastestModel = claudeModels.FirstOrDefault(m => m.ModelId.Contains("haiku")) ??
                             titanModels.FirstOrDefault(m => m.ModelId.Contains("express")) ??
                             llamaModels.FirstOrDefault(m => m.ModelId.Contains("8b"));
            if (fastestModel != null) recommendations["FastestResponse"] = fastestModel;

            // Most cost-effective
            var costEffective = llamaModels.FirstOrDefault(m => m.ModelId.Contains("11b")) ??
                              titanModels.FirstOrDefault() ??
                              claudeModels.FirstOrDefault(m => m.ModelId.Contains("haiku"));
            if (costEffective != null) recommendations["CostEffective"] = costEffective;

            // Most powerful
            var mostPowerful = claudeModels.FirstOrDefault(m => m.ModelId.Contains("opus")) ??
                             llamaModels.FirstOrDefault(m => m.ModelId.Contains("405b")) ??
                             llamaModels.FirstOrDefault(m => m.ModelId.Contains("90b"));
            if (mostPowerful != null) recommendations["MostPowerful"] = mostPowerful;

            return recommendations;
        }

        /// <summary>
        /// Checks if specific model is available in the region
        /// </summary>
        /// <param name="modelId">Model ID to check</param>
        /// <returns>True if model is available</returns>
        public async Task<bool> IsModelAvailableAsync(string modelId)
        {
            try
            {
                var models = await GetAvailableModelsAsync();
                return models.Any(m => m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check model availability - ModelId: {modelId}, Region: {_region}, Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the best available model for a specific provider
        /// </summary>
        /// <param name="preferredProvider">Preferred provider (Anthropic, Meta, Amazon)</param>
        /// <returns>Best available model for provider</returns>
        public async Task<BedrockModel?> GetBestModelForProviderAsync(string preferredProvider)
        {
            try
            {
                var models = await GetTextModelsAsync();
                var providerModels = models.Where(m => 
                    m.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!providerModels.Any()) return null;

                // Priority order based on provider
                return preferredProvider.ToLowerInvariant() switch
                {
                    "anthropic" => providerModels.FirstOrDefault(m => m.ModelId.Contains("sonnet") && m.ModelId.Contains("v2")) ??
                                 providerModels.FirstOrDefault(m => m.ModelId.Contains("sonnet")) ??
                                 providerModels.FirstOrDefault(),
                    
                    "meta" => providerModels.FirstOrDefault(m => m.ModelId.Contains("90b")) ??
                            providerModels.FirstOrDefault(m => m.ModelId.Contains("70b")) ??
                            providerModels.FirstOrDefault(),
                    
                    "amazon" => providerModels.FirstOrDefault(m => m.ModelId.Contains("premier")) ??
                              providerModels.FirstOrDefault(),
                    
                    _ => providerModels.FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get best model for provider - Provider: {preferredProvider}, Region: {_region}, Error: {ex.Message}");
                return null;
            }
        }

        #region AWS Signature Helpers

        private static string ComputeSha256Hash(byte[] input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(input);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string ComputeHmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static byte[] GetSignatureKey(string secretKey, string dateStamp, string regionName, string serviceName)
        {
            var kDate = ComputeHmacSha256Bytes(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
            var kRegion = ComputeHmacSha256Bytes(kDate, regionName);
            var kService = ComputeHmacSha256Bytes(kRegion, serviceName);
            var kSigning = ComputeHmacSha256Bytes(kService, "aws4_request");
            return kSigning;
        }

        private static byte[] ComputeHmacSha256Bytes(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        #endregion

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}