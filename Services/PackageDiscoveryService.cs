using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WingetWizard.Models;
using WingetWizard.Utils;

namespace WingetWizard.Services
{
    /// <summary>
    /// Service for package discovery, search, and installation management
    /// Provides advanced search capabilities and package information
    /// </summary>
    public class PackageDiscoveryService
    {
        private readonly PackageService _packageService;
        private readonly List<PackageSearchResult> _searchResults;
        private readonly List<PackageSearchResult> _selectedPackages;
        
        public PackageDiscoveryService(PackageService packageService)
        {
            _packageService = packageService;
            _searchResults = new List<PackageSearchResult>();
            _selectedPackages = new List<PackageSearchResult>();
        }

        /// <summary>
        /// Gets the current search results
        /// </summary>
        public List<PackageSearchResult> SearchResults => _searchResults.ToList();

        /// <summary>
        /// Gets the currently selected packages for installation
        /// </summary>
        public List<PackageSearchResult> SelectedPackages => _selectedPackages.ToList();

        /// <summary>
        /// Gets the count of selected packages
        /// </summary>
        public int SelectedCount => _selectedPackages.Count;

        /// <summary>
        /// Searches for packages with advanced filtering
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="source">Source to search (optional)</param>
        /// <param name="count">Maximum results (1-1000)</param>
        /// <param name="exact">Use exact match</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Search results</returns>
        public async Task<List<PackageSearchResult>> SearchPackagesAsync(string query, string? source = null, int count = 50, bool exact = false, bool verbose = false)
        {
            try
            {
                var results = await _packageService.SearchPackagesAsync(query, source, count, exact, verbose);
                
                // Clear previous results and add new ones
                _searchResults.Clear();
                _searchResults.AddRange(results);
                
                // Check which packages are already installed
                await CheckInstalledStatusAsync();
                
                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Package search failed: {ex.Message}");
                return new List<PackageSearchResult>();
            }
        }

        /// <summary>
        /// Gets detailed information about a package
        /// </summary>
        /// <param name="packageId">Package ID</param>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Detailed package information</returns>
        public async Task<PackageSearchResult?> GetPackageDetailsAsync(string packageId, bool verbose = false)
        {
            try
            {
                return await _packageService.GetPackageDetailsAsync(packageId, verbose);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get package details: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Selects a package for installation
        /// </summary>
        /// <param name="package">Package to select</param>
        public void SelectPackage(PackageSearchResult package)
        {
            if (package != null && !_selectedPackages.Any(p => p.Id == package.Id))
            {
                package.IsSelected = true;
                _selectedPackages.Add(package);
            }
        }

        /// <summary>
        /// Deselects a package
        /// </summary>
        /// <param name="package">Package to deselect</param>
        public void DeselectPackage(PackageSearchResult package)
        {
            if (package != null)
            {
                package.IsSelected = false;
                _selectedPackages.RemoveAll(p => p.Id == package.Id);
            }
        }

        /// <summary>
        /// Selects all packages in search results
        /// </summary>
        public void SelectAllPackages()
        {
            foreach (var package in _searchResults)
            {
                if (!package.IsInstalled)
                {
                    SelectPackage(package);
                }
            }
        }

        /// <summary>
        /// Deselects all packages
        /// </summary>
        public void DeselectAllPackages()
        {
            foreach (var package in _selectedPackages)
            {
                package.IsSelected = false;
            }
            _selectedPackages.Clear();
        }

        /// <summary>
        /// Installs all selected packages
        /// </summary>
        /// <param name="verbose">Enable verbose logging</param>
        /// <returns>Installation results</returns>
        public async Task<(bool Success, string Message)> InstallSelectedPackagesAsync(bool verbose = false)
        {
            if (_selectedPackages.Count == 0)
            {
                return (false, "No packages selected for installation");
            }

            try
            {
                var packageIds = _selectedPackages.Select(p => p.Id).ToList();
                var result = await _packageService.InstallMultiplePackagesAsync(packageIds, verbose);
                
                if (result.Success)
                {
                    // Update status for installed packages
                    foreach (var package in _selectedPackages)
                    {
                        package.Status = "✅ Installed";
                        package.IsInstalled = true;
                    }
                    
                    // Clear selection
                    DeselectAllPackages();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Installation failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                return (false, errorMessage);
            }
        }

        /// <summary>
        /// Filters search results by various criteria
        /// </summary>
        /// <param name="filterText">Text to filter by</param>
        /// <param name="filterBy">What to filter by (name, id, publisher, tags)</param>
        /// <returns>Filtered results</returns>
        public List<PackageSearchResult> FilterResults(string filterText, string filterBy = "name")
        {
            if (string.IsNullOrWhiteSpace(filterText))
                return _searchResults.ToList();

            var filterLower = filterText.ToLowerInvariant();
            
            return _searchResults.Where(package =>
            {
                switch (filterBy.ToLowerInvariant())
                {
                    case "id":
                        return package.Id.ToLowerInvariant().Contains(filterLower);
                    case "publisher":
                        return package.Publisher.ToLowerInvariant().Contains(filterLower);
                    case "tags":
                        return package.Tags.ToLowerInvariant().Contains(filterLower);
                    case "name":
                    default:
                        return package.Name.ToLowerInvariant().Contains(filterLower);
                }
            }).ToList();
        }

        /// <summary>
        /// Sorts search results by various criteria
        /// </summary>
        /// <param name="sortBy">What to sort by (name, version, publisher)</param>
        /// <param name="ascending">Sort order</param>
        /// <returns>Sorted results</returns>
        public List<PackageSearchResult> SortResults(string sortBy = "name", bool ascending = true)
        {
            var sorted = sortBy.ToLowerInvariant() switch
            {
                "version" => ascending ? 
                    _searchResults.OrderBy(p => p.Version).ToList() : 
                    _searchResults.OrderByDescending(p => p.Version).ToList(),
                "publisher" => ascending ? 
                    _searchResults.OrderBy(p => p.Publisher).ToList() : 
                    _searchResults.OrderByDescending(p => p.Publisher).ToList(),
                _ => ascending ? 
                    _searchResults.OrderBy(p => p.Name).ToList() : 
                    _searchResults.OrderByDescending(p => p.Name).ToList()
            };
            
            return sorted;
        }

        /// <summary>
        /// Checks which packages are already installed
        /// </summary>
        private async Task CheckInstalledStatusAsync()
        {
            try
            {
                // Get list of installed packages
                var installedPackages = await _packageService.ListAllAppsAsync("winget", false);
                
                foreach (var searchResult in _searchResults)
                {
                    searchResult.IsInstalled = installedPackages.Any(p => p.Id == searchResult.Id);
                    if (searchResult.IsInstalled)
                    {
                        searchResult.Status = "✅ Already Installed";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check installed status: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all search results and selections
        /// </summary>
        public void ClearResults()
        {
            _searchResults.Clear();
            _selectedPackages.Clear();
        }

        /// <summary>
        /// Gets popular packages for quick access
        /// </summary>
        /// <returns>List of popular packages</returns>
        public async Task<List<PackageSearchResult>> GetPopularPackagesAsync()
        {
            var popularQueries = new[] { "vscode", "chrome", "firefox", "7zip", "notepad++", "git", "python", "nodejs" };
            var popularPackages = new List<PackageSearchResult>();
            
            foreach (var query in popularQueries)
            {
                try
                {
                    var results = await _packageService.SearchPackagesAsync(query, null, 1, false, false);
                    if (results.Count > 0)
                    {
                        popularPackages.Add(results[0]);
                    }
                }
                catch
                {
                    // Continue with next query
                }
            }
            
            return popularPackages;
        }
    }
}
