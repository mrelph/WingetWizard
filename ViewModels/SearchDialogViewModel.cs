using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Models;
using WingetWizard.Avalonia.Services;

namespace WingetWizard.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for the search and install dialog
    /// </summary>
    public partial class SearchDialogViewModel : ViewModelBase
    {
        private readonly IPackageService _packageService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PackageSearchResult> searchResults = new();

        [ObservableProperty]
        private bool isSearching;

        [ObservableProperty]
        private string statusMessage = "Ready to search";

        [ObservableProperty]
        private string resultsCountMessage = "Enter a search term to find packages";

        [ObservableProperty]
        private bool canInstall = false;

        public SearchDialogViewModel() : base(null!)
        {
            // Get services from the application's service provider
            var services = App.Services;
            _packageService = services.GetRequiredService<IPackageService>();
            _notificationService = services.GetRequiredService<INotificationService>();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                await _notificationService.ShowErrorAsync("Search Required", "Please enter a search term.");
                return;
            }

            IsSearching = true;
            StatusMessage = $"Searching for '{SearchQuery}'...";
            ResultsCountMessage = "Searching...";

            try
            {
                var results = await _packageService.SearchPackagesAsync(SearchQuery, null, 100, false, false);
                
                SearchResults.Clear();
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                if (results.Count == 0)
                {
                    ResultsCountMessage = "No packages found. Try a different search term.";
                    StatusMessage = "No packages found. Try a different search term.";
                }
                else
                {
                    ResultsCountMessage = $"Found {results.Count} package(s)";
                    StatusMessage = $"Search completed. Found {results.Count} package(s).";
                }
                
                // Update install button state after search
                UpdateInstallButtonState();
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync("Search Error", 
                    $"Search failed: {ex.Message}\n\nTry checking your internet connection and winget installation.");
                ResultsCountMessage = "Search failed";
                StatusMessage = $"Search failed: {ex.Message}";
                SearchResults.Clear();
                UpdateInstallButtonState();
            }
            finally
            {
                IsSearching = false;
                if (SearchResults.Count == 0)
                {
                    StatusMessage = "Ready to search";
                }
            }
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var result in SearchResults)
            {
                result.IsSelected = true;
            }
            UpdateInstallButtonState();
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var result in SearchResults)
            {
                result.IsSelected = false;
            }
            UpdateInstallButtonState();
        }

        // Method to handle individual package selection changes
        public void OnPackageSelectionChanged()
        {
            UpdateInstallButtonState();
        }

        [RelayCommand]
        private async Task InstallSelectedAsync()
        {
            var selectedPackages = SearchResults.Where(r => r.IsSelected).Select(r => r.Id).ToList();

            if (selectedPackages.Count == 0)
            {
                await _notificationService.ShowErrorAsync("No Selection", "Please select packages to install.");
                return;
            }

            var result = await _notificationService.ShowConfirmationAsync("Confirm Installation",
                $"Install {selectedPackages.Count} selected package(s)?\n\nThis may take several minutes depending on package sizes.");

            if (result)
            {
                StatusMessage = "Installing selected packages...";
                
                try
                {
                    var installResult = await _packageService.InstallMultiplePackagesAsync(selectedPackages, false);
                    
                    if (installResult.Success)
                    {
                        await _notificationService.ShowSuccessAsync("Installation Complete", 
                            $"Installation completed successfully!\n\nInstalled {selectedPackages.Count} package(s).");
                        
                        // Clear selection after successful installation
                        foreach (var searchResult in SearchResults)
                        {
                            searchResult.IsSelected = false;
                        }
                        UpdateInstallButtonState();
                        StatusMessage = "Installation completed successfully";
                    }
                    else
                    {
                        await _notificationService.ShowErrorAsync("Installation Error", 
                            $"Installation failed: {installResult.Message}");
                        StatusMessage = "Installation failed";
                    }
                }
                catch (Exception ex)
                {
                    await _notificationService.ShowErrorAsync("Installation Error", 
                        $"Installation failed: {ex.Message}");
                    StatusMessage = "Installation error occurred";
                }
                finally
                {
                    // Keep the current status message
                }
            }
        }

        private void UpdateInstallButtonState()
        {
            var hasSelection = SearchResults.Any(r => r.IsSelected);
            CanInstall = hasSelection;
        }

        partial void OnSearchResultsChanged(ObservableCollection<PackageSearchResult> value)
        {
            UpdateInstallButtonState();
        }

        partial void OnSearchQueryChanged(string value)
        {
            // Clear results when search query changes
            SearchResults.Clear();
            UpdateInstallButtonState();
        }
    }
}
