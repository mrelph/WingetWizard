using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using UpgradeApp.Utils;

namespace UpgradeApp.Services
{
    public class SecureSettingsService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new();

        public SecureSettingsService()
        {
            _settingsFilePath = Path.Combine(Application.StartupPath, "secure_settings.json");
            System.Diagnostics.Debug.WriteLine($"SecureSettingsService using file: {_settingsFilePath}");
        }

        public void SaveApiKey(string key, string value)
        {
            try
            {
                lock (_lock)
                {
                    var encryptedValue = EncryptString(value);
                    var settings = LoadSettings();
                    settings[key] = encryptedValue;
                    SaveSettings(settings);
                    
                    System.Diagnostics.Debug.WriteLine($"API key encrypted and saved successfully - KeyName: {key}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save encrypted API key - KeyName: {key}, Error: {ex.Message}");
                throw;
            }
        }

        public string? GetApiKey(string key)
        {
            try
            {
                lock (_lock)
                {
                    var settings = LoadSettings();
                    if (settings.TryGetValue(key, out var encryptedValue))
                    {
                        var decryptedValue = DecryptString(encryptedValue);
                        System.Diagnostics.Debug.WriteLine($"API key retrieved and decrypted successfully - KeyName: {key}");
                        return decryptedValue;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve encrypted API key - KeyName: {key}, Error: {ex.Message}");
                return null;
            }
        }

        public void RemoveApiKey(string key)
        {
            try
            {
                lock (_lock)
                {
                    var settings = LoadSettings();
                    if (settings.Remove(key))
                    {
                        SaveSettings(settings);
                        System.Diagnostics.Debug.WriteLine($"API key removed successfully - KeyName: {key}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to remove API key - KeyName: {key}, Error: {ex.Message}");
                throw;
            }
        }

        public bool HasApiKey(string key)
        {
            try
            {
                lock (_lock)
                {
                    var settings = LoadSettings();
                    return settings.ContainsKey(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check API key existence - KeyName: {key}, Error: {ex.Message}");
                return false;
            }
        }

        private string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainTextBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to encrypt string using DPAPI - Error: {ex.Message}");
                throw new InvalidOperationException("Failed to encrypt sensitive data", ex);
            }
        }

        private string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to decrypt string using DPAPI - Error: {ex.Message}");
                throw new InvalidOperationException("Failed to decrypt sensitive data", ex);
            }
        }

        private Dictionary<string, string> LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Secure settings file does not exist: {_settingsFilePath}");
                    return new Dictionary<string, string>();
                }

                var jsonContent = File.ReadAllText(_settingsFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new Dictionary<string, string>();
                }

                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                var result = settings ?? new Dictionary<string, string>();
                System.Diagnostics.Debug.WriteLine($"Loaded {result.Count} secure settings from: {_settingsFilePath}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings file - FilePath: {_settingsFilePath}, Error: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        private void SaveSettings(Dictionary<string, string> settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_settingsFilePath, jsonContent);
                System.Diagnostics.Debug.WriteLine($"Secure settings saved to: {_settingsFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings file - FilePath: {_settingsFilePath}, Error: {ex.Message}");
                throw;
            }
        }

        public void ClearAllApiKeys()
        {
            try
            {
                lock (_lock)
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        File.Delete(_settingsFilePath);
                        System.Diagnostics.Debug.WriteLine("All API keys cleared from secure storage");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear all API keys - Error: {ex.Message}");
                throw;
            }
        }

        public bool ValidateEncryption()
        {
            try
            {
                const string testData = "test_encryption_validation";
                var encrypted = EncryptString(testData);
                var decrypted = DecryptString(encrypted);
                
                var isValid = testData.Equals(decrypted, StringComparison.Ordinal);
                
                System.Diagnostics.Debug.WriteLine($"Encryption validation completed - IsValid: {isValid}");
                
                return isValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption validation failed - Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves AWS Bedrock credentials securely
        /// </summary>
        /// <param name="accessKeyId">AWS Access Key ID</param>
        /// <param name="secretAccessKey">AWS Secret Access Key</param>
        /// <param name="region">AWS Region</param>
        /// <param name="selectedModel">Selected Bedrock model</param>
        public void SaveBedrockCredentials(string accessKeyId, string secretAccessKey, string region, string selectedModel)
        {
            try
            {
                SaveApiKey("aws_access_key_id", accessKeyId);
                SaveApiKey("aws_secret_access_key", secretAccessKey);
                SaveApiKey("aws_region", region);
                SaveApiKey("bedrock_model", selectedModel);
                
                System.Diagnostics.Debug.WriteLine($"AWS Bedrock credentials saved successfully - Region: {region}, Model: {selectedModel}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save AWS Bedrock credentials - Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves AWS Bedrock credentials
        /// </summary>
        /// <returns>Tuple of AWS credentials or null if not configured</returns>
        public (string? AccessKeyId, string? SecretAccessKey, string? Region, string? Model) GetBedrockCredentials()
        {
            try
            {
                var accessKeyId = GetApiKey("aws_access_key_id");
                var secretAccessKey = GetApiKey("aws_secret_access_key");
                var region = GetApiKey("aws_region") ?? AppConstants.DEFAULT_AWS_REGION;
                var model = GetApiKey("bedrock_model") ?? AppConstants.BEDROCK_CLAUDE_35_SONNET_V2;
                
                System.Diagnostics.Debug.WriteLine($"AWS Bedrock credentials retrieved - HasAccessKey: {!string.IsNullOrEmpty(accessKeyId)}, Region: {region}, Model: {model}");
                
                return (accessKeyId, secretAccessKey, region, model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve AWS Bedrock credentials - Error: {ex.Message}");
                return (null, null, null, null);
            }
        }

        /// <summary>
        /// Checks if AWS Bedrock is configured
        /// </summary>
        /// <returns>True if AWS credentials are available</returns>
        public bool HasBedrockCredentials()
        {
            var (accessKeyId, secretAccessKey, _, _) = GetBedrockCredentials();
            return !string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey);
        }

        /// <summary>
        /// Removes all AWS Bedrock credentials
        /// </summary>
        public void ClearBedrockCredentials()
        {
            try
            {
                RemoveApiKey("aws_access_key_id");
                RemoveApiKey("aws_secret_access_key");
                RemoveApiKey("aws_region");
                RemoveApiKey("bedrock_model");
                
                System.Diagnostics.Debug.WriteLine("AWS Bedrock credentials cleared successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear AWS Bedrock credentials - Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets available Bedrock models using stored credentials
        /// </summary>
        /// <param name="forceRefresh">Force refresh from API</param>
        /// <returns>List of available models or empty list if no credentials</returns>
        public async Task<List<BedrockModelDiscoveryService.BedrockModel>> GetAvailableBedrockModelsAsync(bool forceRefresh = false)
        {
            try
            {
                var (accessKeyId, secretAccessKey, region, _) = GetBedrockCredentials();
                
                if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey))
                {
                    System.Diagnostics.Debug.WriteLine("No Bedrock credentials available for model discovery");
                    return new List<BedrockModelDiscoveryService.BedrockModel>();
                }

                var discoveryService = new BedrockModelDiscoveryService(accessKeyId, secretAccessKey, region ?? AppConstants.DEFAULT_AWS_REGION);
                var models = await discoveryService.GetTextModelsAsync(forceRefresh);
                
                System.Diagnostics.Debug.WriteLine($"Successfully retrieved available Bedrock models - ModelCount: {models.Count}, Region: {region}");
                
                return models;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get available Bedrock models - Error: {ex.Message}");
                return new List<BedrockModelDiscoveryService.BedrockModel>();
            }
        }

        /// <summary>
        /// Gets recommended Bedrock models for different use cases
        /// </summary>
        /// <returns>Dictionary of use case to recommended model</returns>
        public async Task<Dictionary<string, BedrockModelDiscoveryService.BedrockModel>> GetRecommendedBedrockModelsAsync()
        {
            try
            {
                var models = await GetAvailableBedrockModelsAsync();
                return BedrockModelDiscoveryService.GetRecommendedModels(models);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get recommended Bedrock models - Error: {ex.Message}");
                return new Dictionary<string, BedrockModelDiscoveryService.BedrockModel>();
            }
        }

        /// <summary>
        /// Validates the currently configured Bedrock model
        /// </summary>
        /// <returns>True if model is available in the configured region</returns>
        public async Task<bool> ValidateBedrockModelAsync()
        {
            try
            {
                var (accessKeyId, secretAccessKey, region, model) = GetBedrockCredentials();
                
                if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey) || string.IsNullOrEmpty(model))
                {
                    return false;
                }

                var discoveryService = new BedrockModelDiscoveryService(accessKeyId, secretAccessKey, region ?? AppConstants.DEFAULT_AWS_REGION);
                var isAvailable = await discoveryService.IsModelAvailableAsync(model);
                
                System.Diagnostics.Debug.WriteLine($"Bedrock model validation completed - Model: {model}, Region: {region}, IsAvailable: {isAvailable}");
                
                return isAvailable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to validate Bedrock model - Error: {ex.Message}");
                return false;
            }
        }
    }
}