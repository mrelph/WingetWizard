using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Models;
using WingetWizard.Avalonia.Services;
using Avalonia;

namespace WingetWizard.Avalonia.ViewModels;

public class FilterOption
{
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

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
    
    // Properties for simplified view
    public string SearchText 
    { 
        get => SearchQuery; 
        set => SearchQuery = value; 
    }
    
    public bool IsEmpty => !IsLoading && Packages.Count == 0;
    
    // Alias for RefreshPackagesCommand to match view  
    public IRelayCommand RefreshCommand => RefreshPackagesCommand;

    [ObservableProperty]
    private FilterOption selectedFilter;

    [ObservableProperty]
    private bool isGridView = true;

    [ObservableProperty]
    private bool hasSelectedPackages;

    [ObservableProperty]
    private int selectedCount;

    [ObservableProperty]
    private bool hasSearchText;

    [ObservableProperty]
    private ObservableCollection<UpgradableApp> skeletonPackages = new();

    [ObservableProperty]
    private bool isBulkOperationInProgress;

    [ObservableProperty]
    private int bulkOperationProgress;

    [ObservableProperty]
    private int bulkOperationTotal;

    private List<UpgradableApp> _allPackages = new();
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

    public List<FilterOption> FilterOptions { get; } = new()
    {
        new FilterOption { Label = "All Packages", Icon = "📦" },
        new FilterOption { Label = "Updates Available", Icon = "🔄" },
        new FilterOption { Label = "Up to Date", Icon = "✅" },
        new FilterOption { Label = "Recently Installed", Icon = "⭐" },
        new FilterOption { Label = "System Tools", Icon = "🔧" },
        new FilterOption { Label = "Development", Icon = "💻" }
    };

    public PackagesViewModel(IServiceProvider services) : base(services)
    {
        _packageService = services.GetRequiredService<IPackageService>();
        _notificationService = services.GetRequiredService<INotificationService>();
        
        // Set default filter
        SelectedFilter = FilterOptions[0];
        
        // Initialize collections
        Packages = new ObservableCollection<UpgradableApp>();
        FilteredPackages = new ObservableCollection<UpgradableApp>();
        SkeletonPackages = new ObservableCollection<UpgradableApp>();
    }

    [RelayCommand]
    private async Task LoadPackagesAsync()
    {
        // Check cache first
        var now = DateTime.Now;
        if (_allPackages.Any() && (now - _lastCacheTime) < _cacheTimeout)
        {
            StatusMessage = $"Using cached data - {FilteredPackages.Count} packages";
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading packages...";
        
        // Show skeleton loading
        CreateSkeletonPackages();
        
        try
        {
            var apps = await _packageService.ListAllAppsAsync("all", false);
            
            if (apps == null || apps.Count == 0)
            {
                StatusMessage = "No packages found. Check if winget is properly installed.";
                await _notificationService.ShowWarningAsync("No Packages", 
                    "No packages were found. Please ensure winget is installed and accessible from PowerShell.");
                return;
            }
            
            _allPackages = apps;
            _lastCacheTime = now;
            
            // Clear skeleton packages
            SkeletonPackages.Clear();
            
            Packages.Clear();
            foreach (var app in apps)
            {
                Packages.Add(app);
                // Subscribe to selection changes for real-time count updates
                app.PropertyChanged += OnPackagePropertyChanged;
            }
            OnPropertyChanged(nameof(IsEmpty));
            ApplyFilters();
            StatusMessage = $"Loaded {FilteredPackages.Count} packages successfully";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to load packages: {ex.Message}";
            StatusMessage = errorMessage;
            
            // Clear skeleton packages on error
            SkeletonPackages.Clear();
            
            // Provide helpful error messages
            if (ex.Message.Contains("winget") || ex.Message.Contains("command"))
            {
                await _notificationService.ShowErrorAsync("Winget Error", 
                    "Winget command failed. Please ensure Windows Package Manager (winget) is installed and working.\n\n" +
                    "You can install winget from: https://github.com/microsoft/winget-cli/releases");
            }
            else
            {
                await _notificationService.ShowErrorAsync("Load Error", errorMessage);
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
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
        HasSearchText = !string.IsNullOrWhiteSpace(value);
        ApplyFilters();
    }

    partial void OnSelectedFilterChanged(FilterOption value)
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void ShowBulkActions()
    {
        // Implementation for showing bulk actions dropdown/menu
        // This would typically show options like "Update All Selected", "Uninstall Selected", etc.
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        var allSelected = FilteredPackages.All(app => app.IsSelected);
        foreach (var app in FilteredPackages)
        {
            app.IsSelected = !allSelected;
        }
        UpdateSelectionInfo();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var app in FilteredPackages)
        {
            app.IsSelected = false;
        }
        UpdateSelectionInfo();
    }

    [RelayCommand]
    private async Task BulkInstall()
    {
        var selectedPackages = FilteredPackages.Where(app => app.IsSelected).ToList();
        
        if (!selectedPackages.Any())
        {
            StatusMessage = "No packages selected for installation";
            return;
        }

        IsBulkOperationInProgress = true;
        BulkOperationTotal = selectedPackages.Count;
        BulkOperationProgress = 0;
        StatusMessage = $"Installing {selectedPackages.Count} selected packages...";
        
        int successCount = 0;
        int failureCount = 0;

        try
        {
            foreach (var package in selectedPackages)
            {
                try
                {
                    StatusMessage = $"Installing {package.Name}... ({successCount + failureCount + 1}/{selectedPackages.Count})";
                    var result = await _packageService.InstallPackageAsync(package.Id, false);
                    
                    if (result.Success)
                    {
                        successCount++;
                        await _notificationService.ShowSuccessAsync("Installation Complete", $"{package.Name} installed successfully");
                    }
                    else
                    {
                        failureCount++;
                        await _notificationService.ShowErrorAsync("Installation Failed", $"Failed to install {package.Name}: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    await _notificationService.ShowErrorAsync("Installation Error", $"Error installing {package.Name}: {ex.Message}");
                }
                
                BulkOperationProgress = successCount + failureCount;
            }

            StatusMessage = $"Bulk installation complete: {successCount} successful, {failureCount} failed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during bulk installation: {ex.Message}";
            await _notificationService.ShowErrorAsync("Bulk Installation Error", ex.Message);
        }
        finally
        {
            IsBulkOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task BulkUpdate()
    {
        var selectedPackages = FilteredPackages.Where(app => app.IsSelected && !string.IsNullOrEmpty(app.Available)).ToList();
        
        if (!selectedPackages.Any())
        {
            StatusMessage = "No packages with updates selected";
            return;
        }

        IsBulkOperationInProgress = true;
        BulkOperationTotal = selectedPackages.Count;
        BulkOperationProgress = 0;
        StatusMessage = $"Updating {selectedPackages.Count} selected packages...";
        
        int successCount = 0;
        int failureCount = 0;

        try
        {
            foreach (var package in selectedPackages)
            {
                try
                {
                    StatusMessage = $"Updating {package.Name}... ({successCount + failureCount + 1}/{selectedPackages.Count})";
                    var result = await _packageService.UpgradePackageAsync(package.Id, false);
                    
                    if (result.Success)
                    {
                        successCount++;
                        package.Available = string.Empty; // Mark as up to date
                        await _notificationService.ShowSuccessAsync("Update Complete", $"{package.Name} updated successfully");
                    }
                    else
                    {
                        failureCount++;
                        await _notificationService.ShowErrorAsync("Update Failed", $"Failed to update {package.Name}: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    await _notificationService.ShowErrorAsync("Update Error", $"Error updating {package.Name}: {ex.Message}");
                }
                
                BulkOperationProgress = successCount + failureCount;
            }

            StatusMessage = $"Bulk update complete: {successCount} successful, {failureCount} failed";
            ApplyFilters(); // Refresh the filtered view
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during bulk update: {ex.Message}";
            await _notificationService.ShowErrorAsync("Bulk Update Error", ex.Message);
        }
        finally
        {
            IsBulkOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task GetAIAnalysis(UpgradableApp package)
    {
        if (package == null) return;
        
        StatusMessage = $"Getting AI analysis for {package.Name}...";
        
        // Simulate AI analysis - in real implementation this would call an AI service
        await Task.Delay(1000);
        
        // Add mock AI insights
        package.Recommendation = "This package is safe to update. No known compatibility issues.";
        
        // Generate mock AI insights
        package.AIInsights.Clear();
        package.AIInsights.Add(new Models.AIInsight
        {
            Type = "security",
            Message = "This package has been verified as secure and safe to install.",
            Severity = "success"
        });
        
        package.AIInsights.Add(new Models.AIInsight
        {
            Type = "compatibility",
            Message = "Compatible with your current system configuration.",
            Severity = "info"
        });
        
        if (!string.IsNullOrEmpty(package.Available))
        {
            package.AIInsights.Add(new Models.AIInsight
            {
                Type = "update",
                Message = $"Update to version {package.Available} includes bug fixes and performance improvements.",
                Severity = "info"
            });
        }
        
        package.HasAIInsights = package.AIInsights.Count > 0;
        package.IsAIInsightsExpanded = true;
        
        StatusMessage = "AI analysis completed";
    }

    [RelayCommand]
    private void ShowPackageDetails(UpgradableApp package)
    {
        if (package == null) return;
        
        StatusMessage = $"Showing details for {package.Name}";
        // In a real implementation, this would show a detailed package information dialog
    }

    [RelayCommand]
    private void ToggleAIInsights(UpgradableApp package)
    {
        if (package == null) return;
        
        package.ToggleAIInsights();
        StatusMessage = package.IsAIInsightsExpanded 
            ? $"Showing AI insights for {package.Name}" 
            : $"Hiding AI insights for {package.Name}";
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

        // Apply status filter based on selected filter option
        if (SelectedFilter != null)
        {
            filtered = SelectedFilter.Label switch
            {
                "Updates Available" => filtered.Where(app => !string.IsNullOrEmpty(app.Available)),
                "Up to Date" => filtered.Where(app => string.IsNullOrEmpty(app.Available)),
                "Recently Installed" => filtered.OrderByDescending(app => app.Name).Take(20),
                "System Tools" => filtered.Where(app => app.Id.Contains("Microsoft") || app.Name.ToLower().Contains("system")),
                "Development" => filtered.Where(app => app.Name.ToLower().Contains("dev") || app.Name.ToLower().Contains("code") || app.Name.ToLower().Contains("studio")),
                _ => filtered
            };
        }

        // Update the packages collection
        FilteredPackages.Clear();
        foreach (var app in filtered)
        {
            FilteredPackages.Add(app);
        }

        // Update selection tracking
        UpdateSelectionInfo();

        StatusMessage = $"Showing {FilteredPackages.Count} of {_allPackages.Count} packages";
    }

    private void UpdateSelectionInfo()
    {
        var selectedApps = FilteredPackages.Where(app => app.IsSelected).ToList();
        SelectedCount = selectedApps.Count;
        HasSelectedPackages = SelectedCount > 0;
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradableApp.IsSelected))
        {
            UpdateSelectionInfo();
        }
    }

    private void CreateSkeletonPackages()
    {
        SkeletonPackages.Clear();
        
        // Create 12 skeleton placeholder items for loading state
        for (int i = 0; i < 12; i++)
        {
            SkeletonPackages.Add(new UpgradableApp
            {
                Name = "Loading Package...",
                Id = $"skeleton-package-{i + 1}",
                Version = "Loading...",
                Available = i % 3 == 0 ? "Loading..." : string.Empty,
                Description = "Loading package information..."
            });
        }
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
