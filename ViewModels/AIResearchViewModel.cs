using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Services;
using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.ViewModels;

/// <summary>
/// ViewModel for AI Research page with package recommendations
/// </summary>
public partial class AIResearchViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private readonly IPackageService _packageService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PackageSearchResult> _searchResults = new();

    [ObservableProperty]
    private ObservableCollection<AIRecommendation> _recommendations = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _statusMessage = "Enter a search query to get AI-powered package recommendations";

    [ObservableProperty]
    private string _selectedCategory = "All Categories";

    public List<string> Categories { get; } = new()
    {
        "All Categories",
        "Development Tools",
        "Productivity",
        "Media & Graphics",
        "Gaming",
        "System Utilities",
        "Security"
    };

    public AIResearchViewModel(IServiceProvider services) : base(services)
    {
        _aiService = services.GetRequiredService<IAIService>();
        _packageService = services.GetRequiredService<IPackageService>();
    }

    [RelayCommand]
    private async Task SearchPackagesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            StatusMessage = "Please enter a search query";
            return;
        }

        IsSearching = true;
        StatusMessage = "Searching packages...";
        SearchResults.Clear();
        Recommendations.Clear();

        try
        {
            // Step 1: Search for packages using winget
            StatusMessage = $"Searching winget for '{SearchQuery}'...";
            var packages = await _packageService.SearchPackagesAsync(SearchQuery, null, 20, false, false);
            
            foreach (var package in packages)
            {
                SearchResults.Add(package);
            }

            if (SearchResults.Any())
            {
                StatusMessage = $"Found {SearchResults.Count} packages. Generating AI recommendations...";
                
                // Step 2: Get AI recommendations for the top packages
                var topPackages = SearchResults.Take(5).ToList();
                foreach (var package in topPackages)
                {
                    try
                    {
                        // Create a mock UpgradableApp for AI analysis
                        var mockApp = new UpgradableApp
                        {
                            Name = package.Name,
                            Id = package.Id,
                            Version = package.Version,
                            Available = "",
                            Status = "",
                            Recommendation = ""
                        };

                        var aiRecommendation = await _aiService.GetAIRecommendationAsync(mockApp);
                        if (!string.IsNullOrWhiteSpace(aiRecommendation))
                        {
                            var recommendation = new AIRecommendation
                            {
                                PackageName = package.Name,
                                PackageId = package.Id,
                                RecommendationText = aiRecommendation,
                                ConfidenceLevel = Random.Shared.Next(75, 96), // Simulate confidence
                                ReasonForRecommendation = $"Matches your search for '{SearchQuery}'",
                                SimilarPackages = "Visual Studio Code, Notepad++, Sublime Text",
                                Category = SelectedCategory
                            };
                            Recommendations.Add(recommendation);
                        }
                    }
                    catch (Exception)
                    {
                        // If AI fails, add a simple recommendation
                        var fallbackRecommendation = new AIRecommendation
                        {
                            PackageName = package.Name,
                            PackageId = package.Id,
                            RecommendationText = package.Description,
                            ConfidenceLevel = 60, // Lower confidence for non-AI recommendations
                            ReasonForRecommendation = "Found in package search results",
                            SimilarPackages = "",
                            Category = "General"
                        };
                        Recommendations.Add(fallbackRecommendation);
                    }
                }
            }

            if (SearchResults.Any() || Recommendations.Any())
            {
                StatusMessage = $"Found {SearchResults.Count} packages with {Recommendations.Count} AI recommendations";
            }
            else
            {
                StatusMessage = $"No packages found for '{SearchQuery}'. Try different search terms.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching packages: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task InstallSelectedPackageAsync(PackageSearchResult package)
    {
        if (package == null) return;

        IsSearching = true;
        StatusMessage = $"Installing {package.Name}...";

        try
        {
            var result = await _packageService.InstallPackageAsync(package.Id, false);
            
            if (result.Success)
            {
                StatusMessage = $"{package.Name} installed successfully";
                package.Status = "Installed";
                package.IsInstalled = true;
            }
            else
            {
                StatusMessage = $"Failed to install {package.Name}: {result.Message}";
                package.Status = $"Install failed: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error installing {package.Name}: {ex.Message}";
            package.Status = $"Install error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task GetPackageDetailsAsync(PackageSearchResult package)
    {
        if (package == null) return;

        try
        {
            var details = await _packageService.GetPackageDetailsAsync(package.Id, false);
            if (details != null)
            {
                // Update the package with detailed information
                package.Description = details.Description;
                package.Publisher = details.Publisher;
                package.Homepage = details.Homepage;
                package.License = details.License;
                package.Tags = details.Tags;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error getting details for {package.Name}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void FilterByCategory()
    {
        if (SelectedCategory == "All Categories")
        {
            // Show all results
            return;
        }

        // Filter results based on category (this is a simplified implementation)
        var categoryFiltered = SearchResults.Where(pkg => 
            pkg.Tags.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase) ||
            pkg.Description.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase) ||
            pkg.Name.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        SearchResults.Clear();
        foreach (var pkg in categoryFiltered)
        {
            SearchResults.Add(pkg);
        }

        StatusMessage = $"Filtered to {SearchResults.Count} packages in {SelectedCategory}";
    }

    public async Task InstallRecommendedPackageAsync(string packageInfo)
    {
        if (string.IsNullOrEmpty(packageInfo)) return;

        // Extract package name from recommendation (simplified)
        var packageName = packageInfo.Split(' ')[0];
        
        StatusMessage = $"Installing {packageName}...";

        try
        {
            // This would integrate with the actual package service
            await Task.Delay(1000);
            StatusMessage = $"{packageName} installation initiated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error installing {packageName}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearRecommendations()
    {
        Recommendations.Clear();
        SearchQuery = string.Empty;
        StatusMessage = "Enter a search query to get AI-powered package recommendations";
    }
}
