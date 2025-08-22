using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WingetWizard.Models;

namespace WingetWizard.Services
{
    /// <summary>
    /// Provides debounced search and filtering functionality for package lists.
    /// Implements real-time search with configurable delay to optimize performance.
    /// </summary>
    public class SearchFilterService : IDisposable
    {
        private readonly System.Threading.Timer _debounceTimer;
        private readonly object _searchLock = new();
        private readonly int _defaultDebounceMs;
        private readonly int _maxSearchResults;
        private readonly int _minSearchLength;

        // Search and filtering constants
        private const int DefaultDebounceMs = 300;
        private const int MaxSearchResults = 500;
        private const int MinSearchLength = 2;
        private const int MaxDebounceMs = 2000;
        private const int MinDebounceMs = 100;

        private string _lastSearchTerm = string.Empty;
        private CancellationTokenSource? _currentSearchCancellation;
        private bool _disposed = false;

        public SearchFilterService(int debounceMs = DefaultDebounceMs, int maxResults = MaxSearchResults, int minLength = MinSearchLength)
        {
            _defaultDebounceMs = Math.Clamp(debounceMs, MinDebounceMs, MaxDebounceMs);
            _maxSearchResults = Math.Max(100, maxResults);
            _minSearchLength = Math.Max(1, minLength);
            
            _debounceTimer = new System.Threading.Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Event raised when search results are ready.
        /// </summary>
        public event EventHandler<SearchResultsEventArgs>? SearchResultsReady;

        /// <summary>
        /// Event raised when search is started.
        /// </summary>
        public event EventHandler<SearchStartedEventArgs>? SearchStarted;

        /// <summary>
        /// Event raised when search is cancelled.
        /// </summary>
        public event EventHandler<SearchCancelledEventArgs>? SearchCancelled;

        /// <summary>
        /// Performs a debounced search operation.
        /// </summary>
        /// <param name="searchTerm">The search term</param>
        /// <param name="allPackages">Complete list of packages to search</param>
        /// <param name="searchOptions">Optional search configuration</param>
        public void DebouncedSearch(string searchTerm, IEnumerable<UpgradableApp> allPackages, SearchOptions? searchOptions = null)
        {
            if (_disposed) return;

            var options = searchOptions ?? new SearchOptions();
            
            // Cancel previous search if still running
            _currentSearchCancellation?.Cancel();
            _currentSearchCancellation = new CancellationTokenSource();

            // Reset the debounce timer
            _debounceTimer.Change(_defaultDebounceMs, Timeout.Infinite);

            // Store search parameters for when timer elapses
            lock (_searchLock)
            {
                _lastSearchTerm = searchTerm;
            }

            // Raise search started event
            SearchStarted?.Invoke(this, new SearchStartedEventArgs
            {
                SearchTerm = searchTerm,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Performs an immediate search without debouncing.
        /// </summary>
        /// <param name="searchTerm">The search term</param>
        /// <param name="allPackages">Complete list of packages to search</param>
        /// <param name="searchOptions">Optional search configuration</param>
        /// <returns>Search results</returns>
        public async Task<SearchResults> SearchImmediateAsync(string searchTerm, IEnumerable<UpgradableApp> allPackages, SearchOptions? searchOptions = null)
        {
            if (_disposed) return new SearchResults();

            var options = searchOptions ?? new SearchOptions();
            var results = await PerformSearchAsync(searchTerm, allPackages, options, CancellationToken.None);
            
            return results;
        }

        /// <summary>
        /// Filters packages by multiple criteria.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="filterCriteria">Filter criteria</param>
        /// <returns>Filtered packages</returns>
        public async Task<List<UpgradableApp>> FilterPackagesAsync(IEnumerable<UpgradableApp> allPackages, FilterCriteria filterCriteria)
        {
            if (_disposed) return new List<UpgradableApp>();

            return await Task.Run(() =>
            {
                // Single enumeration - convert to list once and work with that
                var packages = allPackages.ToList();
                IEnumerable<UpgradableApp> filtered = packages;

                // Apply all filters in a single chain to avoid multiple enumerations
                if (!string.IsNullOrWhiteSpace(filterCriteria.NameFilter))
                {
                    filtered = filtered.Where(pkg => 
                        pkg.Name.Contains(filterCriteria.NameFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filterCriteria.IdFilter))
                {
                    filtered = filtered.Where(pkg => 
                        pkg.Id.Contains(filterCriteria.IdFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filterCriteria.VersionFilter))
                {
                    filtered = filtered.Where(pkg => 
                        pkg.Version.Contains(filterCriteria.VersionFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filterCriteria.StatusFilter))
                {
                    filtered = filtered.Where(pkg => 
                        pkg.Status.Contains(filterCriteria.StatusFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filterCriteria.SourceFilter))
                {
                    filtered = filtered.Where(pkg => 
                        pkg.Status.Contains(filterCriteria.SourceFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (filterCriteria.HasUpgradeAvailable.HasValue)
                {
                    filtered = filtered.Where(pkg => 
                        !string.IsNullOrWhiteSpace(pkg.Available) && 
                        pkg.Available != pkg.Version);
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(filterCriteria.SortBy))
                {
                    filtered = ApplySorting(filtered, filterCriteria.SortBy, filterCriteria.SortDescending);
                }

                // Apply result limit and materialize once at the end
                if (filterCriteria.MaxResults > 0)
                {
                    return filtered.Take(filterCriteria.MaxResults).ToList();
                }

                return filtered.ToList();
            });
        }

        /// <summary>
        /// Gets search suggestions based on partial input.
        /// </summary>
        /// <param name="partialInput">Partial search input</param>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="maxSuggestions">Maximum number of suggestions</param>
        /// <returns>List of search suggestions</returns>
        public async Task<List<string>> GetSearchSuggestionsAsync(string partialInput, IEnumerable<UpgradableApp> allPackages, int maxSuggestions = 10)
        {
            if (_disposed || string.IsNullOrWhiteSpace(partialInput) || partialInput.Length < _minSearchLength)
                return new List<string>();

            return await Task.Run(() =>
            {
                var packages = allPackages.ToList();
                var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Get package name suggestions
                var nameSuggestions = packages
                    .Where(pkg => pkg.Name.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                    .Select(pkg => pkg.Name)
                    .Take(maxSuggestions / 2);

                suggestions.UnionWith(nameSuggestions);

                // Get package ID suggestions
                var idSuggestions = packages
                    .Where(pkg => pkg.Id.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                    .Select(pkg => pkg.Id)
                    .Take(maxSuggestions / 2);

                suggestions.UnionWith(idSuggestions);

                // Get version suggestions
                var versionSuggestions = packages
                    .Where(pkg => pkg.Version.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                    .Select(pkg => pkg.Version)
                    .Take(maxSuggestions / 4);

                suggestions.UnionWith(versionSuggestions);

                return suggestions.Take(maxSuggestions).ToList();
            });
        }

        /// <summary>
        /// Gets advanced search statistics.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <returns>Search statistics</returns>
        public async Task<SearchStatistics> GetSearchStatisticsAsync(IEnumerable<UpgradableApp> allPackages)
        {
            if (_disposed) return new SearchStatistics();

            return await Task.Run(() =>
            {
                var packages = allPackages.ToList();
                
                return new SearchStatistics
                {
                    TotalPackages = packages.Count,
                    PackagesWithUpdates = packages.Count(pkg => !string.IsNullOrWhiteSpace(pkg.Available) && pkg.Available != pkg.Version),
                    PackagesBySource = packages.GroupBy(pkg => pkg.Status).ToDictionary(g => g.Key, g => g.Count()),
                    AverageNameLength = packages.Average(pkg => pkg.Name.Length),
                    LongestPackageName = packages.Max(pkg => pkg.Name.Length),
                    ShortestPackageName = packages.Min(pkg => pkg.Name.Length)
                };
            });
        }

        /// <summary>
        /// Handles the debounce timer elapsed event.
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private async void OnDebounceTimerElapsed(object? state)
        {
            if (_disposed) return;

            string searchTerm;
            lock (_searchLock)
            {
                searchTerm = _lastSearchTerm;
            }

            // Check if search term meets minimum length requirement
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < _minSearchLength)
            {
                // Return all packages for short/empty search terms
                SearchResultsReady?.Invoke(this, new SearchResultsEventArgs
                {
                    SearchTerm = searchTerm,
                    Results = new SearchResults(),
                    Timestamp = DateTime.Now
                });
                return;
            }

            try
            {
                            // Perform the actual search
            var results = await PerformSearchAsync(searchTerm, new List<UpgradableApp>(), new SearchOptions(), _currentSearchCancellation?.Token ?? CancellationToken.None);
                
                // Raise results ready event
                SearchResultsReady?.Invoke(this, new SearchResultsEventArgs
                {
                    SearchTerm = searchTerm,
                    Results = results,
                    Timestamp = DateTime.Now
                });
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled
                SearchCancelled?.Invoke(this, new SearchCancelledEventArgs
                {
                    SearchTerm = searchTerm,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // Search failed
                SearchResultsReady?.Invoke(this, new SearchResultsEventArgs
                {
                    SearchTerm = searchTerm,
                    Results = new SearchResults { Error = ex.Message },
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Performs the actual search operation.
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="allPackages">Packages to search</param>
        /// <param name="options">Search options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results</returns>
        private async Task<SearchResults> PerformSearchAsync(string searchTerm, IEnumerable<UpgradableApp> allPackages, SearchOptions options, CancellationToken cancellationToken)
        {

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packages = allPackages.ToList();
                var results = new List<UpgradableApp>();

                // Perform search based on options
                if (options.SearchInNames)
                {
                    var nameResults = packages.Where(pkg => 
                        pkg.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                    results.AddRange(nameResults);
                }

                if (options.SearchInIds)
                {
                    var idResults = packages.Where(pkg => 
                        pkg.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                    results.AddRange(idResults);
                }

                if (options.SearchInVersions)
                {
                    var versionResults = packages.Where(pkg => 
                        pkg.Version.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                    results.AddRange(versionResults);
                }

                if (options.SearchInStatus)
                {
                    var statusResults = packages.Where(pkg => 
                        pkg.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                    results.AddRange(statusResults);
                }

                // Remove duplicates and apply result limit
                var distinctResults = results.Distinct().Take(_maxSearchResults).ToList();

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(options.SortBy))
                {
                    distinctResults = ApplySorting(distinctResults, options.SortBy, options.SortDescending);
                }

                return new SearchResults
                {
                    Items = distinctResults,
                    TotalFound = distinctResults.Count,
                    SearchTerm = searchTerm,
                    SearchOptions = options
                };
            }, cancellationToken);
        }

        /// <summary>
        /// Applies sorting to the results.
        /// </summary>
        /// <param name="packages">Packages to sort</param>
        /// <param name="sortBy">Sort field</param>
        /// <param name="descending">Sort direction</param>
        /// <returns>Sorted packages</returns>
        private static List<UpgradableApp> ApplySorting(IEnumerable<UpgradableApp> packages, string sortBy, bool descending)
        {
            var sorted = sortBy.ToLowerInvariant() switch
            {
                "name" => packages.OrderBy(pkg => pkg.Name),
                "id" => packages.OrderBy(pkg => pkg.Id),
                "version" => packages.OrderBy(pkg => pkg.Version),
                "available" => packages.OrderBy(pkg => pkg.Available),
                "status" => packages.OrderBy(pkg => pkg.Status),
                _ => packages.OrderBy(pkg => pkg.Name)
            };

            return descending ? sorted.Reverse().ToList() : sorted.ToList();
        }

        /// <summary>
        /// Disposes of the service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _debounceTimer?.Dispose();
                _currentSearchCancellation?.Cancel();
                _currentSearchCancellation?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Configuration options for search operations.
    /// </summary>
    public class SearchOptions
    {
        /// <summary>
        /// Gets or sets whether to search in package names.
        /// </summary>
        public bool SearchInNames { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to search in package IDs.
        /// </summary>
        public bool SearchInIds { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to search in package versions.
        /// </summary>
        public bool SearchInVersions { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to search in package status.
        /// </summary>
        public bool SearchInStatus { get; set; } = false;

        /// <summary>
        /// Gets or sets the field to sort by.
        /// </summary>
        public string SortBy { get; set; } = "name";

        /// <summary>
        /// Gets or sets whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use fuzzy matching.
        /// </summary>
        public bool UseFuzzyMatching { get; set; } = false;

        /// <summary>
        /// Gets or sets the fuzzy matching threshold.
        /// </summary>
        public double FuzzyThreshold { get; set; } = 0.8;
    }

    /// <summary>
    /// Filter criteria for package filtering.
    /// </summary>
    public class FilterCriteria
    {
        /// <summary>
        /// Gets or sets the name filter.
        /// </summary>
        public string? NameFilter { get; set; }

        /// <summary>
        /// Gets or sets the ID filter.
        /// </summary>
        public string? IdFilter { get; set; }

        /// <summary>
        /// Gets or sets the version filter.
        /// </summary>
        public string? VersionFilter { get; set; }

        /// <summary>
        /// Gets or sets the status filter.
        /// </summary>
        public string? StatusFilter { get; set; }

        /// <summary>
        /// Gets or sets the source filter.
        /// </summary>
        public string? SourceFilter { get; set; }

        /// <summary>
        /// Gets or sets whether to filter for packages with available upgrades.
        /// </summary>
        public bool? HasUpgradeAvailable { get; set; }

        /// <summary>
        /// Gets or sets the field to sort by.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of results.
        /// </summary>
        public int MaxResults { get; set; } = 0;
    }

    /// <summary>
    /// Represents search results.
    /// </summary>
    public class SearchResults
    {
        /// <summary>
        /// Gets or sets the search results.
        /// </summary>
        public List<UpgradableApp> Items { get; set; } = new();

        /// <summary>
        /// Gets or sets the total number of items found.
        /// </summary>
        public int TotalFound { get; set; }

        /// <summary>
        /// Gets or sets the search term used.
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the search options used.
        /// </summary>
        public SearchOptions? SearchOptions { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during search.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets whether the search was successful.
        /// </summary>
        public bool IsSuccessful => string.IsNullOrEmpty(Error);
    }

    /// <summary>
    /// Event arguments for search results ready event.
    /// </summary>
    public class SearchResultsEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the search term.
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the search results.
        /// </summary>
        public SearchResults Results { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp when results were ready.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for search started event.
    /// </summary>
    public class SearchStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the search term.
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when search started.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for search cancelled event.
    /// </summary>
    public class SearchCancelledEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the search term.
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when search was cancelled.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Provides search statistics.
    /// </summary>
    public class SearchStatistics
    {
        /// <summary>
        /// Gets or sets the total number of packages.
        /// </summary>
        public int TotalPackages { get; set; }

        /// <summary>
        /// Gets or sets the number of packages with available updates.
        /// </summary>
        public int PackagesWithUpdates { get; set; }

        /// <summary>
        /// Gets or sets the packages grouped by source.
        /// </summary>
        public Dictionary<string, int> PackagesBySource { get; set; } = new();

        /// <summary>
        /// Gets or sets the average package name length.
        /// </summary>
        public double AverageNameLength { get; set; }

        /// <summary>
        /// Gets or sets the longest package name length.
        /// </summary>
        public int LongestPackageName { get; set; }

        /// <summary>
        /// Gets or sets the shortest package name length.
        /// </summary>
        public int ShortestPackageName { get; set; }
    }
}
