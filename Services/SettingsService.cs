using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Service class responsible for application settings management
    /// Handles loading, saving, and managing user preferences and API keys
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;

        public SettingsService()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            _settings = new Dictionary<string, object>();
            LoadSettings();
        }

        /// <summary>
        /// Gets a setting value by key
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="defaultValue">Default value if setting doesn't exist</param>
        /// <returns>Setting value or default</returns>
        public T GetSetting<T>(string key, T defaultValue = default(T)!)
        {
            if (_settings.ContainsKey(key))
            {
                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)(_settings[key] is bool boolValue ? boolValue : 
                            bool.TryParse(_settings[key]?.ToString(), out var parsed) ? parsed : (defaultValue ?? (object)false));
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        return (T)(object)(_settings[key]?.ToString() ?? defaultValue?.ToString() ?? string.Empty);
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T)(object)(_settings[key] is int intValue ? intValue : 
                            int.TryParse(_settings[key]?.ToString(), out var parsed) ? parsed : (defaultValue ?? (object)0));
                    }
                }
                catch
                {
                    // Return default value if parsing fails
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets a setting value
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        public void SetSetting<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
                
            _settings[key] = value!;
        }

        /// <summary>
        /// Gets an API key by name
        /// </summary>
        /// <param name="keyName">API key name (e.g., "AnthropicApiKey")</param>
        /// <returns>API key value or empty string</returns>
        public string GetApiKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
                
            return GetSetting<string>(keyName, "");
        }

        /// <summary>
        /// Stores an API key
        /// </summary>
        /// <param name="keyName">API key name</param>
        /// <param name="value">API key value</param>
        public void StoreApiKey(string keyName, string value)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
                
            SetSetting(keyName, value);
            SaveSettings(); // Persist changes immediately
        }

        /// <summary>
        /// Removes an API key
        /// </summary>
        /// <param name="keyName">API key name to remove</param>
        public void RemoveApiKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
                
            if (_settings.ContainsKey(keyName))
            {
                _settings.Remove(keyName);
                SaveSettings(); // Persist changes immediately
            }
        }

        /// <summary>
        /// Loads settings from the settings file
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath, Encoding.UTF8);
                    var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    
                    if (loadedSettings != null)
                    {
                        _settings.Clear();
                        foreach (var kvp in loadedSettings)
                        {
                            // Convert JsonElement to appropriate types
                            switch (kvp.Value.ValueKind)
                            {
                                case JsonValueKind.String:
                                    _settings[kvp.Key] = kvp.Value.GetString() ?? string.Empty;
                                    break;
                                case JsonValueKind.True:
                                case JsonValueKind.False:
                                    _settings[kvp.Key] = kvp.Value.GetBoolean();
                                    break;
                                case JsonValueKind.Number:
                                    if (kvp.Value.TryGetInt32(out var intValue))
                                        _settings[kvp.Key] = intValue;
                                    else
                                        _settings[kvp.Key] = kvp.Value.GetDouble();
                                    break;
                                default:
                                    _settings[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
                // Initialize with default settings
                InitializeDefaultSettings();
            }
        }

        /// <summary>
        /// Saves current settings to the settings file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Initializes default settings
        /// </summary>
        private void InitializeDefaultSettings()
        {
            _settings = new Dictionary<string, object>
            {
                ["isAdvancedMode"] = true,
                ["selectedAiModel"] = "claude-sonnet-4-20250514",
                ["usePerplexity"] = true,
                ["verboseLogging"] = false
            };
        }

        /// <summary>
        /// Resets all API keys
        /// </summary>
        public void ResetApiKeys()
        {
            var keysToRemove = new List<string>();
            foreach (var key in _settings.Keys)
            {
                if (key.EndsWith("ApiKey"))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _settings.Remove(key);
            }
        }

        /// <summary>
        /// Gets all current settings
        /// </summary>
        /// <returns>Copy of current settings</returns>
        public Dictionary<string, object> GetAllSettings()
        {
            return new Dictionary<string, object>(_settings);
        }

        /// <summary>
        /// Checks if a setting exists
        /// </summary>
        /// <param name="key">Setting key to check</param>
        /// <returns>True if setting exists</returns>
        public bool HasSetting(string key)
        {
            return _settings.ContainsKey(key);
        }

        /// <summary>
        /// Gets the settings file path
        /// </summary>
        /// <returns>Full path to settings file</returns>
        public string GetSettingsPath()
        {
            return _settingsPath;
        }
    }
}
