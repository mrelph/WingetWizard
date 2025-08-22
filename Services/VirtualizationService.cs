using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UpgradeApp.Models;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Provides virtualization and pagination support for large package lists.
    /// Optimizes memory usage and performance when dealing with hundreds or thousands of packages.
    /// </summary>
    public class VirtualizationService
    {
        private readonly int _pageSize;
        private readonly int _virtualizationThreshold;
        private readonly Dictionary<int, List<UpgradableApp>> _pageCache;
        private readonly object _cacheLock = new();

        // Virtualization constants
        private const int DefaultPageSize = 50;
        private const int DefaultVirtualizationThreshold = 100;
        private const int MaxCachePages = 10;

        public VirtualizationService(int pageSize = DefaultPageSize, int virtualizationThreshold = DefaultVirtualizationThreshold)
        {
            _pageSize = Math.Max(10, pageSize);
            _virtualizationThreshold = Math.Max(50, virtualizationThreshold);
            _pageCache = new Dictionary<int, List<UpgradableApp>>();
        }

        /// <summary>
        /// Determines if virtualization should be enabled based on package count.
        /// </summary>
        /// <param name="totalPackages">Total number of packages</param>
        /// <returns>True if virtualization should be enabled</returns>
        public bool ShouldVirtualize(int totalPackages)
        {
            return totalPackages > _virtualizationThreshold;
        }

        /// <summary>
        /// Gets a specific page of packages with caching.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="pageNumber">Page number (0-based)</param>
        /// <returns>Page of packages and pagination info</returns>
        public async Task<VirtualizedPage<UpgradableApp>> GetPageAsync(IEnumerable<UpgradableApp> allPackages, int pageNumber)
        {
            var packages = allPackages.ToList();
            var totalPackages = packages.Count;

            if (!ShouldVirtualize(totalPackages))
            {
                return new VirtualizedPage<UpgradableApp>
                {
                    Items = packages,
                    PageNumber = 0,
                    TotalPages = 1,
                    TotalItems = totalPackages,
                    PageSize = totalPackages,
                    IsVirtualized = false
                };
            }

            var totalPages = (int)Math.Ceiling((double)totalPackages / _pageSize);
            pageNumber = Math.Max(0, Math.Min(pageNumber, totalPages - 1));

            // Check cache first
            if (_pageCache.TryGetValue(pageNumber, out var cachedPage))
            {
                return new VirtualizedPage<UpgradableApp>
                {
                    Items = cachedPage,
                    PageNumber = pageNumber,
                    TotalPages = totalPages,
                    TotalItems = totalPackages,
                    PageSize = _pageSize,
                    IsVirtualized = true
                };
            }

            // Calculate page boundaries
            var startIndex = pageNumber * _pageSize;
            var endIndex = Math.Min(startIndex + _pageSize, totalPackages);
            var pageItems = packages.Skip(startIndex).Take(_pageSize).ToList();

            // Cache the page
            CachePageAsync(pageNumber, pageItems);

            return new VirtualizedPage<UpgradableApp>
            {
                Items = pageItems,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalPackages,
                PageSize = _pageSize,
                IsVirtualized = true
            };
        }

        /// <summary>
        /// Gets packages for a specific range with virtualization support.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="startIndex">Starting index</param>
        /// <param name="count">Number of items to retrieve</param>
        /// <param name="pageNumber">Page number (0-based)</param>
        /// <returns>Range of packages</returns>
        public async Task<List<UpgradableApp>> GetRangeAsync(IEnumerable<UpgradableApp> allPackages, int startIndex, int count)
        {
            var packages = allPackages.ToList();
            var totalPackages = packages.Count;

            if (!ShouldVirtualize(totalPackages))
            {
                return packages;
            }

            // Validate range
            startIndex = Math.Max(0, startIndex);
            count = Math.Min(count, totalPackages - startIndex);

            if (count <= 0)
                return new List<UpgradableApp>();

            var rangeItems = packages.Skip(startIndex).Take(count).ToList();

            // Pre-cache adjacent pages for better performance
            var startPage = startIndex / _pageSize;
            var endPage = (startIndex + count - 1) / _pageSize;

            for (int page = startPage; page <= endPage; page++)
            {
                var pageStart = page * _pageSize;
                var pageEnd = Math.Min(pageStart + _pageSize, totalPackages);
                var pageItems = packages.Skip(pageStart).Take(pageEnd - pageStart).ToList();
                CachePageAsync(page, pageItems);
            }

            return rangeItems;
        }

        /// <summary>
        /// Searches packages with virtualization support.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="searchTerm">Search term</param>
        /// <param name="maxResults">Maximum results to return</param>
        /// <returns>Search results with pagination</returns>
        public async Task<VirtualizedPage<UpgradableApp>> SearchAsync(IEnumerable<UpgradableApp> allPackages, string searchTerm, int maxResults = 100)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetPageAsync(allPackages, 0);
            }

            var packages = allPackages.ToList();
            var searchResults = packages.Where(pkg =>
                pkg.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                pkg.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                pkg.Version.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).Take(maxResults).ToList();

            return new VirtualizedPage<UpgradableApp>
            {
                Items = searchResults,
                PageNumber = 0,
                TotalPages = 1,
                TotalItems = searchResults.Count,
                PageSize = searchResults.Count,
                IsVirtualized = false
            };
        }

        /// <summary>
        /// Filters packages by status with virtualization support.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="statusFilter">Status to filter by</param>
        /// <returns>Filtered packages</returns>
        public async Task<List<UpgradableApp>> FilterByStatusAsync(IEnumerable<UpgradableApp> allPackages, string statusFilter)
        {
            if (string.IsNullOrWhiteSpace(statusFilter))
            {
                return allPackages.ToList();
            }

            var packages = allPackages.ToList();
            var filteredPackages = packages.Where(pkg =>
                pkg.Status.Contains(statusFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            return filteredPackages;
        }

        /// <summary>
        /// Caches a page of packages for faster subsequent access.
        /// </summary>
        /// <param name="pageNumber">Page number to cache</param>
        /// <param name="pageItems">Items on the page</param>
        private void CachePageAsync(int pageNumber, List<UpgradableApp> pageItems)
        {
            lock (_cacheLock)
            {
                // Limit cache size
                if (_pageCache.Count >= MaxCachePages)
                {
                    var oldestPage = _pageCache.Keys.OrderBy(k => k).First();
                    _pageCache.Remove(oldestPage);
                }

                _pageCache[pageNumber] = pageItems;
            }
        }

        /// <summary>
        /// Clears the page cache to free memory.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _pageCache.Clear();
            }
        }

        /// <summary>
        /// Gets cache statistics for monitoring.
        /// </summary>
        /// <returns>Cache statistics</returns>
        public VirtualizationCacheStatistics GetCacheStatistics()
        {
            lock (_cacheLock)
            {
                return new VirtualizationCacheStatistics
                {
                    CachedPages = _pageCache.Count,
                    MaxCachePages = MaxCachePages,
                    CacheHitRate = 0.0 // Would need to track hits/misses for accurate rate
                };
            }
        }

        /// <summary>
        /// Preloads adjacent pages for better scrolling performance.
        /// </summary>
        /// <param name="allPackages">Complete list of packages</param>
        /// <param name="currentPage">Current page number</param>
        /// <param name="preloadCount">Number of adjacent pages to preload</param>
        public async Task PreloadAdjacentPagesAsync(IEnumerable<UpgradableApp> allPackages, int currentPage, int preloadCount = 2)
        {
            var packages = allPackages.ToList();
            var totalPages = (int)Math.Ceiling((double)packages.Count / _pageSize);

            var tasks = new List<Task>();

            for (int offset = -preloadCount; offset <= preloadCount; offset++)
            {
                var pageToLoad = currentPage + offset;
                if (pageToLoad >= 0 && pageToLoad < totalPages)
                {
                    tasks.Add(GetPageAsync(packages, pageToLoad));
                }
            }

            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Represents a virtualized page of items with pagination information.
    /// </summary>
    /// <typeparam name="T">Type of items in the page</typeparam>
    public class VirtualizedPage<T>
    {
        /// <summary>
        /// Gets or sets the items on this page.
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Gets or sets the current page number (0-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets whether virtualization is enabled for this page.
        /// </summary>
        public bool IsVirtualized { get; set; }

        /// <summary>
        /// Gets whether there are more pages after the current one.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages - 1;

        /// <summary>
        /// Gets whether there are pages before the current one.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 0;

        /// <summary>
        /// Gets the starting index of the current page.
        /// </summary>
        public int StartIndex => PageNumber * PageSize;

        /// <summary>
        /// Gets the ending index of the current page.
        /// </summary>
        public int EndIndex => Math.Min(StartIndex + PageSize, TotalItems);
    }

    /// <summary>
    /// Provides statistics about the virtualization cache.
    /// </summary>
    public class VirtualizationCacheStatistics
    {
        /// <summary>
        /// Gets or sets the number of currently cached pages.
        /// </summary>
        public int CachedPages { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of pages that can be cached.
        /// </summary>
        public int MaxCachePages { get; set; }

        /// <summary>
        /// Gets or sets the cache hit rate as a percentage.
        /// </summary>
        public double CacheHitRate { get; set; }

        /// <summary>
        /// Gets the cache utilization percentage.
        /// </summary>
        public double CacheUtilization => (double)CachedPages / MaxCachePages * 100;
    }
}
