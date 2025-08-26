using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Models;
using WingetWizard.Avalonia.Services;
using Avalonia;

namespace WingetWizard.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the packages page
/// </summary>
public partial class PackagesViewModel : ViewModelBase
{
    private readonly IPackageService _packageService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<UpgradableApp> packages = new();

    [ObservableProperty]
    private ObservableCollection<UpgradableApp> filteredPackages = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string selectedFilter = "All Packages";

    private List<UpgradableApp> _allPackages = new();
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

    public List<string> FilterOptions { get; } = new()
    {
        "All Packages",
        "Updates Available", 
        "Up to Date"
    };

    public PackagesViewModel(IServiceProvider services) : base(services)
    {
        _packageService = services.GetRequiredService<IPackageService>();
        _notificationService = services.GetRequiredService<INotificationService>();
    }

    [RelayCommand]
    private async Task LoadPackagesAsync()
    {
        // Check cache first
        var now = DateTime.Now;
        if (_allPackages.Any() && (now - _lastCacheTime) < _cacheTimeout)
        {
            StatusMessage = $"Using cached data - {Packages.Count} packages";
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading packages...";
        
        try
        {
            var apps = await _packageService.ListAllAppsAsync("all", false);
            _allPackages = apps;
            _lastCacheTime = now;
            
            Packages.Clear();
            foreach (var app in apps)
            {
                Packages.Add(app);
            }
            ApplyFilters();
            StatusMessage = $"Loaded {Packages.Count} packages";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading packages: {ex.Message}";
            await _notificationService.ShowErrorAsync("Load Error", $"Failed to load packages: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPackagesAsync()
    {
        // Force refresh by clearing cache
        _lastCacheTime = DateTime.MinValue;
        await LoadPackagesAsync();
    }

    [RelayCommand]
    private async Task UpgradeAllAsync()
    {
        IsLoading = true;
        StatusMessage = "Upgrading all packages...";
        
        try
        {
            var result = await _packageService.UpgradeAllPackagesAsync(false);
            StatusMessage = result.Success ? "All packages upgraded successfully" : $"Upgrade failed: {result.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error upgrading packages: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_allPackages == null || !_allPackages.Any())
            return;

        var filtered = _allPackages.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered.Where(app => 
                app.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                app.Id.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        // Apply status filter
        filtered = SelectedFilter switch
        {
            "Updates Available" => filtered.Where(app => !string.IsNullOrEmpty(app.Available)),
            "Up to Date" => filtered.Where(app => string.IsNullOrEmpty(app.Available)),
            _ => filtered
        };

        // Update the packages collection
        Packages.Clear();
        foreach (var app in filtered)
        {
            Packages.Add(app);
        }

        StatusMessage = $"Showing {Packages.Count} of {_allPackages.Count} packages";
    }

    public async Task InstallPackageAsync(UpgradableApp package)
    {
        if (package == null) return;

        StatusMessage = $"Installing {package.Name}...";

        try
        {
            var result = await _packageService.InstallPackageAsync(package.Id, false);
            if (result.Success)
            {
                StatusMessage = $"{package.Name} installed successfully";
                await _notificationService.ShowSuccessAsync("Installation Complete", $"{package.Name} has been installed successfully");
            }
            else
            {
                StatusMessage = $"Failed to install {package.Name}: {result.Message}";
                await _notificationService.ShowErrorAsync("Installation Failed", $"Failed to install {package.Name}: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error installing {package.Name}: {ex.Message}";
            await _notificationService.ShowErrorAsync("Installation Error", $"Error installing {package.Name}: {ex.Message}");
        }
    }

    public async Task UninstallPackageAsync(UpgradableApp package)
    {
        if (package == null) return;

        StatusMessage = $"Uninstalling {package.Name}...";

        try
        {
            var result = await _packageService.UninstallPackageAsync(package.Id, false);
            if (result.Success)
            {
                StatusMessage = $"{package.Name} uninstalled successfully";
                await _notificationService.ShowSuccessAsync("Uninstall Complete", $"{package.Name} has been uninstalled successfully");
                Packages.Remove(package);
                _allPackages.Remove(package);
            }
            else
            {
                StatusMessage = $"Failed to uninstall {package.Name}: {result.Message}";
                await _notificationService.ShowErrorAsync("Uninstall Failed", $"Failed to uninstall {package.Name}: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error uninstalling {package.Name}: {ex.Message}";
            await _notificationService.ShowErrorAsync("Uninstall Error", $"Error uninstalling {package.Name}: {ex.Message}");
        }
    }

    public async Task UpgradePackageAsync(UpgradableApp package)
    {
        if (package == null) return;

        StatusMessage = $"Updating {package.Name}...";

        try
        {
            var result = await _packageService.UpgradePackageAsync(package.Id, false);
            if (result.Success)
            {
                StatusMessage = $"{package.Name} updated successfully";
                await _notificationService.ShowSuccessAsync("Update Complete", $"{package.Name} has been updated successfully");
                // Clear the available version to show it's up to date
                package.Available = string.Empty;
                ApplyFilters(); // Refresh the filtered view
            }
            else
            {
                StatusMessage = $"Failed to update {package.Name}: {result.Message}";
                await _notificationService.ShowErrorAsync("Update Failed", $"Failed to update {package.Name}: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating {package.Name}: {ex.Message}";
            await _notificationService.ShowErrorAsync("Update Error", $"Error updating {package.Name}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UpgradePackageCommand(UpgradableApp package)
    {
        await UpgradePackageAsync(package);
    }

    [RelayCommand]
    private async Task InstallPackageCommand(UpgradableApp package)
    {
        await InstallPackageAsync(package);
    }

    [RelayCommand]
    private async Task UninstallPackageCommand(UpgradableApp package)
    {
        await UninstallPackageAsync(package);
    }

    [RelayCommand]
    private async Task SearchAndInstallAsync()
    {
        try
        {
            var searchDialog = new Views.SearchDialog();
            // In Avalonia, we need to find the main window differently
            var mainWindow = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
            if (mainWindow != null)
            {
                await searchDialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync("Error", $"Failed to open search dialog: {ex.Message}");
        }
    }
}
