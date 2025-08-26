namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Interface for settings management
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value by key
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if setting doesn't exist</param>
    /// <returns>The setting value or default</returns>
    T GetSetting<T>(string key, T defaultValue = default(T)!);

    /// <summary>
    /// Sets a setting value by key
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    void SetSetting<T>(string key, T value);

    /// <summary>
    /// Gets the settings file path
    /// </summary>
    /// <returns>The settings file path</returns>
    string GetSettingsPath();
}


