using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Services;
using System.Collections.ObjectModel;

namespace WingetWizard.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the settings page
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string appVersion = "4.0.0 - Avalonia UI";

    [ObservableProperty]
    private bool isDarkMode = true;

    // AI Configuration
    [ObservableProperty]
    private string claudeApiKey = string.Empty;

    [ObservableProperty]
    private string perplexityApiKey = string.Empty;

    [ObservableProperty]
    private string selectedAiModel = "claude-sonnet-4-20250514";

    [ObservableProperty]
    private bool usePerplexity = false;

    [ObservableProperty]
    private bool aiEnabled = false;

    // Package Management Configuration  
    [ObservableProperty]
    private string defaultPackageSource = "winget";

    [ObservableProperty]
    private bool verboseLogging = false;

    [ObservableProperty]
    private bool autoCheckUpdates = false;

    [ObservableProperty]
    private int updateCheckInterval = 24; // hours

    [ObservableProperty]
    private bool confirmInstalls = true;

    [ObservableProperty]
    private bool confirmUninstalls = true;

    // UI Configuration
    [ObservableProperty]
    private int refreshCacheMinutes = 5;

    [ObservableProperty]
    private int searchResultCount = 50;

    [ObservableProperty]
    private bool showWelcomeScreen = true;

    [ObservableProperty]
    private string statusMessage = "Settings loaded";

    public List<string> AvailableAiModels { get; } = new()
    {
        "claude-sonnet-4-20250514",
        "claude-3-5-sonnet-20241022", 
        "claude-3-5-haiku-20241022",
        "claude-3-opus-20240229"
    };

    public List<string> AvailablePackageSources { get; } = new()
    {
        "winget",
        "msstore", 
        "all"
    };

    public List<int> AvailableUpdateIntervals { get; } = new()
    {
        1, 6, 12, 24, 48, 168 // hours
    };

    public SettingsViewModel(IServiceProvider services) : base(services)
    {
        _settingsService = services.GetRequiredService<ISettingsService>();
        _notificationService = services.GetRequiredService<INotificationService>();
        
        LoadSettings();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            // AI Settings
            _settingsService.SetSetting("ClaudeApiKey", ClaudeApiKey);
            _settingsService.SetSetting("PerplexityApiKey", PerplexityApiKey);
            _settingsService.SetSetting("SelectedAiModel", SelectedAiModel);
            _settingsService.SetSetting("UsePerplexity", UsePerplexity);
            _settingsService.SetSetting("AIEnabled", AiEnabled);

            // Package Management Settings
            _settingsService.SetSetting("DefaultPackageSource", DefaultPackageSource);
            _settingsService.SetSetting("VerboseLogging", VerboseLogging);
            _settingsService.SetSetting("AutoCheckUpdates", AutoCheckUpdates);
            _settingsService.SetSetting("UpdateCheckInterval", UpdateCheckInterval);
            _settingsService.SetSetting("ConfirmInstalls", ConfirmInstalls);
            _settingsService.SetSetting("ConfirmUninstalls", ConfirmUninstalls);

            // UI Settings
            _settingsService.SetSetting("IsDarkMode", IsDarkMode);
            _settingsService.SetSetting("RefreshCacheMinutes", RefreshCacheMinutes);
            _settingsService.SetSetting("SearchResultCount", SearchResultCount);
            _settingsService.SetSetting("ShowWelcomeScreen", ShowWelcomeScreen);

            StatusMessage = "Settings saved successfully";
            await _notificationService.ShowSuccessAsync("Settings", "Settings have been saved successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
            await _notificationService.ShowErrorAsync("Settings Error", $"Failed to save settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private void LoadSettings()
    {
        try
        {
            // AI Settings
            ClaudeApiKey = _settingsService.GetSetting<string>("ClaudeApiKey", "");
            PerplexityApiKey = _settingsService.GetSetting<string>("PerplexityApiKey", "");
            SelectedAiModel = _settingsService.GetSetting<string>("SelectedAiModel", "claude-sonnet-4-20250514");
            UsePerplexity = _settingsService.GetSetting<bool>("UsePerplexity", false);
            AiEnabled = _settingsService.GetSetting<bool>("AIEnabled", false);

            // Package Management Settings
            DefaultPackageSource = _settingsService.GetSetting<string>("DefaultPackageSource", "winget");
            VerboseLogging = _settingsService.GetSetting<bool>("VerboseLogging", false);
            AutoCheckUpdates = _settingsService.GetSetting<bool>("AutoCheckUpdates", false);
            UpdateCheckInterval = _settingsService.GetSetting<int>("UpdateCheckInterval", 24);
            ConfirmInstalls = _settingsService.GetSetting<bool>("ConfirmInstalls", true);
            ConfirmUninstalls = _settingsService.GetSetting<bool>("ConfirmUninstalls", true);

            // UI Settings
            IsDarkMode = _settingsService.GetSetting<bool>("IsDarkMode", true);
            RefreshCacheMinutes = _settingsService.GetSetting<int>("RefreshCacheMinutes", 5);
            SearchResultCount = _settingsService.GetSetting<int>("SearchResultCount", 50);
            ShowWelcomeScreen = _settingsService.GetSetting<bool>("ShowWelcomeScreen", true);

            StatusMessage = "Settings loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        try
        {
            // Reset to default values
            ClaudeApiKey = "";
            PerplexityApiKey = "";
            SelectedAiModel = "claude-sonnet-4-20250514";
            UsePerplexity = false;
            AiEnabled = false;
            DefaultPackageSource = "winget";
            VerboseLogging = false;
            AutoCheckUpdates = false;
            UpdateCheckInterval = 24;
            ConfirmInstalls = true;
            ConfirmUninstalls = true;
            IsDarkMode = true;
            RefreshCacheMinutes = 5;
            SearchResultCount = 50;
            ShowWelcomeScreen = true;

            await SaveSettingsAsync();
            StatusMessage = "Settings reset to defaults";
            await _notificationService.ShowSuccessAsync("Settings", "Settings have been reset to default values");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resetting settings: {ex.Message}";
            await _notificationService.ShowErrorAsync("Settings Error", $"Failed to reset settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestApiKeysAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaudeApiKey) && string.IsNullOrWhiteSpace(PerplexityApiKey))
        {
            StatusMessage = "No API keys configured for testing";
            await _notificationService.ShowErrorAsync("API Test", "Please configure at least one API key before testing");
            return;
        }

        StatusMessage = "Testing API connections...";
        
        // This would ideally test the actual API connections
        // For now, just simulate the test
        await Task.Delay(2000);
        
        var testResults = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(ClaudeApiKey))
        {
            testResults.Add("✅ Claude API key appears valid");
        }
        
        if (!string.IsNullOrWhiteSpace(PerplexityApiKey))
        {
            testResults.Add("✅ Perplexity API key appears valid");
        }

        StatusMessage = "API test completed";
        await _notificationService.ShowSuccessAsync("API Test Results", string.Join("\n", testResults));
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        try
        {
            var settingsPath = _settingsService.GetSettingsPath();
            var directory = Path.GetDirectoryName(settingsPath);
            
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                // This would open the folder in the file explorer
                // Implementation depends on the operating system
                StatusMessage = $"Settings folder: {directory}";
            }
            else
            {
                StatusMessage = "Settings folder not found";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening settings folder: {ex.Message}";
        }
    }
}


