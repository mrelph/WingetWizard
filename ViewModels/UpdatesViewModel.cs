using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Models;
using WingetWizard.Avalonia.Services;

namespace WingetWizard.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Updates page, handling package updates
/// </summary>
public partial class UpdatesViewModel : ViewModelBase
{
    private readonly IPackageService _packageService;

    [ObservableProperty]
    private ObservableCollection<UpgradableApp> _availableUpdates = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isUpdating;

    [ObservableProperty]
    private string _statusMessage = "Click 'Check for Updates' to scan for available updates";

    [ObservableProperty]
    private string _lastChecked = "Never";

    [ObservableProperty]
    private int _selectedUpdateCount;
    
    public bool HasNoUpdates => !IsLoading && AvailableUpdates.Count == 0;
    public bool HasUpdates => !IsLoading && AvailableUpdates.Count > 0;

    public UpdatesViewModel(IServiceProvider services) : base(services)
    {
        _packageService = services.GetRequiredService<IPackageService>();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsLoading = true;
        StatusMessage = "Scanning for available updates...";

        try
        {
            var updates = await _packageService.CheckForUpdatesAsync("all", false);
            
            AvailableUpdates.Clear();
            foreach (var update in updates.Where(u => !string.IsNullOrEmpty(u.Available)))
            {
                AvailableUpdates.Add(update);
                // Subscribe to selection changes for real-time count updates
                update.PropertyChanged += OnPackagePropertyChanged;
            }

            StatusMessage = $"Found {AvailableUpdates.Count} available updates";
            LastChecked = DateTime.Now.ToString("MMM dd, yyyy 'at' h:mm tt");
            OnPropertyChanged(nameof(HasNoUpdates));
            OnPropertyChanged(nameof(HasUpdates));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error checking for updates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoUpdates));
            OnPropertyChanged(nameof(HasUpdates));
        }
    }

    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        if (!AvailableUpdates.Any())
        {
            StatusMessage = "No updates available to install";
            return;
        }

        IsUpdating = true;
        StatusMessage = $"Updating {AvailableUpdates.Count} packages...";

        try
        {
            var result = await _packageService.UpgradeAllPackagesAsync(false);
            
            if (result.Success)
            {
                StatusMessage = "All updates installed successfully";
                // Refresh the list after successful updates
                await CheckForUpdatesAsync();
            }
            else
            {
                StatusMessage = $"Update failed: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during update: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }

    [RelayCommand]
    private async Task UpdateSelectedAsync()
    {
        var selectedPackages = AvailableUpdates.Where(app => app.IsSelected).ToList();
        
        if (!selectedPackages.Any())
        {
            StatusMessage = "No packages selected for update";
            return;
        }

        IsUpdating = true;
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
                        package.PropertyChanged -= OnPackagePropertyChanged;
                        AvailableUpdates.Remove(package);
                    }
                    else
                    {
                        failureCount++;
                        package.Status = $"Update failed: {result.Message}";
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    package.Status = $"Update error: {ex.Message}";
                }
            }

            StatusMessage = $"Update complete: {successCount} successful, {failureCount} failed";
            SelectedUpdateCount = AvailableUpdates.Count(app => app.IsSelected);
            OnPropertyChanged(nameof(HasNoUpdates));
            OnPropertyChanged(nameof(HasUpdates));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during batch update: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        var allSelected = AvailableUpdates.All(app => app.IsSelected);
        foreach (var app in AvailableUpdates)
        {
            app.IsSelected = !allSelected;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var app in AvailableUpdates)
        {
            app.IsSelected = false;
        }
        UpdateSelectedCount();
    }

    public void UpdateSelectedCount()
    {
        SelectedUpdateCount = AvailableUpdates.Count(app => app.IsSelected);
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradableApp.IsSelected))
        {
            UpdateSelectedCount();
        }
    }

    [RelayCommand]
    public async Task UpdateSinglePackageAsync(UpgradableApp package)
    {
        if (package == null) return;

        IsUpdating = true;
        StatusMessage = $"Updating {package.Name}...";

        try
        {
            var result = await _packageService.UpgradePackageAsync(package.Id, false);
            
            if (result.Success)
            {
                StatusMessage = $"{package.Name} updated successfully";
                package.PropertyChanged -= OnPackagePropertyChanged;
                AvailableUpdates.Remove(package);
                OnPropertyChanged(nameof(HasNoUpdates));
                OnPropertyChanged(nameof(HasUpdates));
            }
            else
            {
                StatusMessage = $"Failed to update {package.Name}: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating {package.Name}: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }
}
