using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Secure settings service for managing sensitive data like API keys.
    /// Uses Windows DPAPI for encryption at rest.
    /// </summary>
    public class SecureSettingsService : IDisposable
    {
        private readonly string _secureSettingsPath;
        private readonly Dictionary<string, string> _secureCache;
        private readonly object _lock = new object();
        private bool _disposed = false;

        // Known secure setting keys for validation
        private static readonly HashSet<string> ValidApiKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AnthropicApiKey",
            "PerplexityApiKey", 
            "BedrockApiKey",
            "aws_access_key_id",
            "aws_secret_access_key",
            "aws_session_token"
        };

        public SecureSettingsService()
        {
            _secureSettingsPath = Path.Combine(Application.StartupPath, "secure_settings.json");
            _secureCache = new Dictionary<string, string>();
            LoadSecureSettings();
        }

        /// <summary>
        /// Gets an API key by name with input validation.
        /// </summary>
        /// <param name="keyName">API key name (validated against allowlist)</param>
        /// <returns>Decrypted API key value or empty string if not found</returns>
        public string GetApiKey(string keyName)
        {
            // Input validation - prevent injection attacks
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return string.Empty;
            }

            // Validate against known key names to prevent unauthorized access
            if (!ValidApiKeys.Contains(keyName))
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] WARNING: Attempted access to invalid key: {keyName}");
                return string.Empty;
            }

            lock (_lock)
            {
                if (_secureCache.TryGetValue(keyName, out var encryptedValue))
                {
                    try
                    {
                        return DecryptValue(encryptedValue);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SecureSettings] Failed to decrypt key {keyName}: {ex.Message}");
                        return string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Stores an API key with encryption.
        /// </summary>
        /// <param name="keyName">API key name (validated against allowlist)</param>
        /// <param name="value">API key value to encrypt and store</param>
        public void StoreApiKey(string keyName, string value)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(keyName))
            {
                throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
            }

            if (!ValidApiKeys.Contains(keyName))
            {
                throw new ArgumentException($"Invalid API key name: {keyName}", nameof(keyName));
            }

            if (string.IsNullOrEmpty(value))
            {
                // Remove the key if value is empty
                RemoveApiKey(keyName);
                return;
            }

            try
            {
                var encryptedValue = EncryptValue(value);
                
                lock (_lock)
                {
                    _secureCache[keyName] = encryptedValue;
                }

                SaveSecureSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to store API key {keyName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Removes an API key from secure storage.
        /// </summary>
        /// <param name="keyName">API key name to remove</param>
        public void RemoveApiKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName) || !ValidApiKeys.Contains(keyName))
            {
                return;
            }

            lock (_lock)
            {
                if (_secureCache.Remove(keyName))
                {
                    SaveSecureSettings();
                }
            }
        }

        /// <summary>
        /// Checks if an API key exists and is configured.
        /// </summary>
        /// <param name="keyName">API key name to check</param>
        /// <returns>True if key exists and has a non-empty value</returns>
        public bool HasApiKey(string keyName)
        {
            if (!ValidApiKeys.Contains(keyName))
            {
                return false;
            }

            var value = GetApiKey(keyName);
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Gets all configured API key names (not values).
        /// </summary>
        /// <returns>List of configured key names</returns>
        public IReadOnlyList<string> GetConfiguredKeys()
        {
            lock (_lock)
            {
                var configuredKeys = new List<string>();
                foreach (var keyName in ValidApiKeys)
                {
                    if (_secureCache.ContainsKey(keyName))
                    {
                        configuredKeys.Add(keyName);
                    }
                }
                return configuredKeys.AsReadOnly();
            }
        }

        /// <summary>
        /// Clears all API keys from secure storage.
        /// </summary>
        public void ClearAllApiKeys()
        {
            lock (_lock)
            {
                _secureCache.Clear();
            }
            SaveSecureSettings();
        }

        /// <summary>
        /// Loads secure settings from encrypted file.
        /// </summary>
        private void LoadSecureSettings()
        {
            try
            {
                if (!File.Exists(_secureSettingsPath))
                {
                    return; // No secure settings file exists yet
                }

                var encryptedJson = File.ReadAllText(_secureSettingsPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(encryptedJson))
                {
                    return;
                }

                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(encryptedJson);
                if (settings != null)
                {
                    lock (_lock)
                    {
                        _secureCache.Clear();
                        foreach (var kvp in settings)
                        {
                            // Only load valid API keys
                            if (ValidApiKeys.Contains(kvp.Key))
                            {
                                _secureCache[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Failed to load secure settings: {ex.Message}");
                // Continue with empty cache - don't throw to prevent app startup failure
            }
        }

        /// <summary>
        /// Saves secure settings to encrypted file.
        /// </summary>
        private void SaveSecureSettings()
        {
            try
            {
                Dictionary<string, string> toSave;
                lock (_lock)
                {
                    toSave = new Dictionary<string, string>(_secureCache);
                }

                var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_secureSettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_secureSettingsPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save secure settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Encrypts a value using Windows DPAPI.
        /// </summary>
        /// <param name="plainText">Plain text to encrypt</param>
        /// <returns>Base64 encoded encrypted value</returns>
        private static string EncryptValue(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Failed to encrypt value: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a value using Windows DPAPI.
        /// </summary>
        /// <param name="encryptedText">Base64 encoded encrypted value</param>
        /// <returns>Decrypted plain text</returns>
        private static string DecryptValue(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Failed to decrypt value: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    // Clear sensitive data from memory
                    _secureCache.Clear();
                }
                _disposed = true;
            }
        }
    }
}