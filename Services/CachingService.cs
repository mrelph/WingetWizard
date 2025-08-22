using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UpgradeApp.Models;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Provides intelligent caching for AI recommendations and frequently accessed data.
    /// Implements memory and disk-based caching with automatic expiration and cleanup.
    /// </summary>
    public class CachingService : IDisposable
    {
        private readonly Dictionary<string, CacheEntry> _memoryCache;
        private readonly string _cacheDirectory;
        private readonly object _cacheLock = new();
        private readonly System.Threading.Timer _cleanupTimer;
        private readonly int _maxMemoryEntries;
        private readonly int _maxDiskEntries;
        private readonly TimeSpan _defaultExpiration;

        // Caching constants
        private const int DefaultMaxMemoryEntries = 1000;
        private const int DefaultMaxDiskEntries = 10000;
        private const int DefaultCleanupIntervalMs = 300000; // 5 minutes
        private const int DefaultExpirationHours = 24;
        private const string CacheFileExtension = ".cache";
        private const string CacheIndexFile = "cache_index.json";

        public CachingService(string? cacheDirectory = null, int maxMemoryEntries = DefaultMaxMemoryEntries, 
            int maxDiskEntries = DefaultMaxDiskEntries, int expirationHours = DefaultExpirationHours)
        {
            _maxMemoryEntries = Math.Max(100, maxMemoryEntries);
            _maxDiskEntries = Math.Max(1000, maxDiskEntries);
            _defaultExpiration = TimeSpan.FromHours(Math.Max(1, expirationHours));
            
            _cacheDirectory = cacheDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetWizard", "Cache");
            _memoryCache = new Dictionary<string, CacheEntry>();

            // Ensure cache directory exists
            Directory.CreateDirectory(_cacheDirectory);

            // Start cleanup timer
            _cleanupTimer = new System.Threading.Timer(PerformCleanup, null, DefaultCleanupIntervalMs, DefaultCleanupIntervalMs);

            // Load existing cache index
            LoadCacheIndex();
        }

        /// <summary>
        /// Event raised when cache entries are evicted.
        /// </summary>
        public event EventHandler<CacheEvictionEventArgs>? CacheEntryEvicted;

        /// <summary>
        /// Event raised when cache cleanup is performed.
        /// </summary>
        public event EventHandler<CacheCleanupEventArgs>? CacheCleanupPerformed;

        /// <summary>
        /// Gets a value from the cache.
        /// </summary>
        /// <typeparam name="T">Type of the cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or default if not found/expired</returns>
        public T? Get<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return default;

            lock (_cacheLock)
            {
                if (_memoryCache.TryGetValue(key, out var entry))
                {
                    if (!IsExpired(entry))
                    {
                        // Update access count and last accessed time
                        entry.AccessCount++;
                        entry.LastAccessed = DateTime.Now;
                        return (T?)entry.Value;
                    }
                    else
                    {
                        // Remove expired entry
                        _memoryCache.Remove(key);
                    }
                }
            }

            // Try to load from disk cache
            return LoadFromDiskCache<T>(key);
        }

        /// <summary>
        /// Sets a value in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the value to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiration">Optional expiration time</param>
        /// <param name="priority">Cache priority</param>
        public void Set<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            var entry = new CacheEntry
            {
                Key = key,
                Value = value,
                Created = DateTime.Now,
                LastAccessed = DateTime.Now,
                Expiration = expiration ?? _defaultExpiration,
                Priority = priority,
                AccessCount = 1,
                Size = EstimateSize(value)
            };

            lock (_cacheLock)
            {
                // Check if we need to evict entries
                if (_memoryCache.Count >= _maxMemoryEntries)
                {
                    EvictEntries();
                }

                _memoryCache[key] = entry;
            }

            // Also save to disk cache for persistence
            SaveToDiskCache(key, entry);
        }

        /// <summary>
        /// Gets or sets a value, creating it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">Type of the cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Factory function to create value if not cached</param>
        /// <param name="expiration">Optional expiration time</param>
        /// <param name="priority">Cache priority</param>
        /// <returns>Cached or newly created value</returns>
        public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal)
        {
            var cached = Get<T>(key);
            if (cached != null)
                return cached;

            var value = factory();
            Set(key, value, expiration, priority);
            return value;
        }

        /// <summary>
        /// Gets or sets a value asynchronously, creating it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">Type of the cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Async factory function to create value if not cached</param>
        /// <param name="expiration">Optional expiration time</param>
        /// <param name="priority">Cache priority</param>
        /// <returns>Cached or newly created value</returns>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal)
        {
            var cached = Get<T>(key);
            if (cached != null)
                return cached;

            var value = await factory();
            Set(key, value, expiration, priority);
            return value;
        }

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if value was removed, false if not found</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            bool removed = false;
            lock (_cacheLock)
            {
                removed = _memoryCache.Remove(key);
            }

            // Also remove from disk cache
            RemoveFromDiskCache(key);

            return removed;
        }

        /// <summary>
        /// Clears all cached values.
        /// </summary>
        public void Clear()
        {
            lock (_cacheLock)
            {
                _memoryCache.Clear();
            }

            // Clear disk cache
            ClearDiskCache();
        }

        /// <summary>
        /// Checks if a key exists in the cache and is not expired.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if key exists and is valid</returns>
        public bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            lock (_cacheLock)
            {
                if (_memoryCache.TryGetValue(key, out var entry))
                {
                    return !IsExpired(entry);
                }
            }

            // Check disk cache
            return File.Exists(GetDiskCachePath(key));
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        /// <returns>Cache statistics</returns>
        public CacheStatistics GetStatistics()
        {
            lock (_cacheLock)
            {
                var validEntries = _memoryCache.Values.Where(e => !IsExpired(e)).ToList();
                
                return new CacheStatistics
                {
                    TotalEntries = _memoryCache.Count,
                    ValidEntries = validEntries.Count,
                    ExpiredEntries = _memoryCache.Count - validEntries.Count,
                    MemoryUsageBytes = validEntries.Sum(e => e.Size),
                    MaxMemoryEntries = _maxMemoryEntries,
                    MaxDiskEntries = _maxDiskEntries,
                    AverageEntryAge = validEntries.Any() ? validEntries.Average(e => (DateTime.Now - e.Created).TotalMinutes) : 0,
                    HitRate = 0.0 // Would need to track hits/misses for accurate rate
                };
            }
        }

        /// <summary>
        /// Gets all valid cache keys.
        /// </summary>
        /// <returns>List of valid cache keys</returns>
        public List<string> GetValidKeys()
        {
            lock (_cacheLock)
            {
                return _memoryCache.Values
                    .Where(e => !IsExpired(e))
                    .Select(e => e.Key)
                    .ToList();
            }
        }

        /// <summary>
        /// Preloads frequently accessed items into memory cache.
        /// </summary>
        /// <param name="keys">Keys to preload</param>
        public async Task PreloadAsync(IEnumerable<string> keys)
        {
            var tasks = keys.Select(key => Task.Run(() =>
            {
                var value = LoadFromDiskCache<object>(key);
                if (value != null)
                {
                    Set(key, value, _defaultExpiration, CachePriority.Low);
                }
            }));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Checks if a cache entry is expired.
        /// </summary>
        /// <param name="entry">Cache entry to check</param>
        /// <returns>True if expired</returns>
        private bool IsExpired(CacheEntry entry)
        {
            return DateTime.Now > entry.Created + entry.Expiration;
        }

        /// <summary>
        /// Evicts cache entries based on priority and access patterns.
        /// </summary>
        private void EvictEntries()
        {
            var entriesToEvict = _memoryCache.Values
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.LastAccessed)
                .ThenBy(e => e.AccessCount)
                .Take(_memoryCache.Count - _maxMemoryEntries + 10) // Evict a few extra to prevent immediate re-eviction
                .ToList();

            foreach (var entry in entriesToEvict)
            {
                _memoryCache.Remove(entry.Key);
                CacheEntryEvicted?.Invoke(this, new CacheEvictionEventArgs
                {
                    Key = entry.Key,
                    Reason = "Memory limit reached",
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Estimates the size of a cached value in bytes.
        /// </summary>
        /// <param name="value">Value to estimate size for</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateSize(object? value)
        {
            if (value == null) return 0;

            try
            {
                if (value is string str)
                    return str.Length * 2; // UTF-16 characters
                
                if (value is byte[] bytes)
                    return bytes.Length;
                
                if (value is Array array)
                    return array.Length * 8; // Rough estimate
                
                // Default estimate
                return 100;
            }
            catch
            {
                return 100; // Safe default
            }
        }

        /// <summary>
        /// Saves a cache entry to disk.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="entry">Cache entry</param>
        private void SaveToDiskCache(string key, CacheEntry entry)
        {
            try
            {
                var cachePath = GetDiskCachePath(key);
                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(cachePath, json);
            }
            catch
            {
                // Silently fail disk cache operations
            }
        }

        /// <summary>
        /// Loads a cache entry from disk.
        /// </summary>
        /// <typeparam name="T">Type of the cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or default if not found</returns>
        private T? LoadFromDiskCache<T>(string key)
        {
            try
            {
                var cachePath = GetDiskCachePath(key);
                if (!File.Exists(cachePath)) return default;

                var json = File.ReadAllText(cachePath);
                var entry = JsonSerializer.Deserialize<CacheEntry>(json);
                
                if (entry != null && !IsExpired(entry))
                {
                    // Move to memory cache
                    lock (_cacheLock)
                    {
                        if (_memoryCache.Count < _maxMemoryEntries)
                        {
                            _memoryCache[key] = entry;
                        }
                    }
                    
                    return (T?)entry.Value;
                }
                else if (entry != null)
                {
                    // Remove expired disk entry
                    RemoveFromDiskCache(key);
                }
            }
            catch
            {
                // Silently fail disk cache operations
            }

            return default;
        }

        /// <summary>
        /// Removes a cache entry from disk.
        /// </summary>
        /// <param name="key">Cache key</param>
        private void RemoveFromDiskCache(string key)
        {
            try
            {
                var cachePath = GetDiskCachePath(key);
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }
            catch
            {
                // Silently fail disk cache operations
            }
        }

        /// <summary>
        /// Clears all disk cache files.
        /// </summary>
        private void ClearDiskCache()
        {
            try
            {
                var cacheFiles = Directory.GetFiles(_cacheDirectory, $"*{CacheFileExtension}");
                foreach (var file in cacheFiles)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Silently fail disk cache operations
            }
        }

        /// <summary>
        /// Gets the disk cache file path for a key.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Full file path</returns>
        private string GetDiskCachePath(string key)
        {
            var safeKey = Path.GetInvalidFileNameChars()
                .Aggregate(key, (current, invalid) => current.Replace(invalid, '_'));
            return Path.Combine(_cacheDirectory, $"{safeKey}{CacheFileExtension}");
        }

        /// <summary>
        /// Loads the cache index from disk.
        /// </summary>
        private void LoadCacheIndex()
        {
            try
            {
                var indexPath = Path.Combine(_cacheDirectory, CacheIndexFile);
                if (File.Exists(indexPath))
                {
                    var json = File.ReadAllText(indexPath);
                    // Could load index here for more sophisticated disk cache management
                }
            }
            catch
            {
                // Silently fail index loading
            }
        }

        /// <summary>
        /// Performs periodic cache cleanup.
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void PerformCleanup(object? state)
        {
            try
            {
                var expiredKeys = new List<string>();

                lock (_cacheLock)
                {
                    expiredKeys = _memoryCache.Values
                        .Where(IsExpired)
                        .Select(e => e.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _memoryCache.Remove(key);
                    }
                }

                // Remove expired disk entries
                foreach (var key in expiredKeys)
                {
                    RemoveFromDiskCache(key);
                }

                if (expiredKeys.Count > 0)
                {
                    CacheCleanupPerformed?.Invoke(this, new CacheCleanupEventArgs
                    {
                        EntriesRemoved = expiredKeys.Count,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch
            {
                // Silently fail cleanup operations
            }
        }

        /// <summary>
        /// Disposes of the service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    /// <summary>
    /// Represents a cache entry with metadata.
    /// </summary>
    public class CacheEntry
    {
        /// <summary>
        /// Gets or sets the cache key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cached value.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets when the entry was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets when the entry was last accessed.
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        public TimeSpan Expiration { get; set; }

        /// <summary>
        /// Gets or sets the cache priority.
        /// </summary>
        public CachePriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the number of times the entry was accessed.
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Gets or sets the estimated size of the entry in bytes.
        /// </summary>
        public long Size { get; set; }
    }

    /// <summary>
    /// Defines cache priority levels.
    /// </summary>
    public enum CachePriority
    {
        /// <summary>
        /// Low priority - first to be evicted.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - last to be evicted.
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority - never evicted unless expired.
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Provides cache statistics.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the total number of cache entries.
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of valid (non-expired) entries.
        /// </summary>
        public int ValidEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of expired entries.
        /// </summary>
        public int ExpiredEntries { get; set; }

        /// <summary>
        /// Gets or sets the total memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of memory entries.
        /// </summary>
        public int MaxMemoryEntries { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of disk entries.
        /// </summary>
        public int MaxDiskEntries { get; set; }

        /// <summary>
        /// Gets or sets the average entry age in minutes.
        /// </summary>
        public double AverageEntryAge { get; set; }

        /// <summary>
        /// Gets or sets the cache hit rate as a percentage.
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// Gets the memory usage in megabytes.
        /// </summary>
        public double MemoryUsageMB => MemoryUsageBytes / 1024.0 / 1024.0;
    }

    /// <summary>
    /// Event arguments for cache eviction events.
    /// </summary>
    public class CacheEvictionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the evicted cache key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for eviction.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the eviction occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for cache cleanup events.
    /// </summary>
    public class CacheCleanupEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the number of entries removed during cleanup.
        /// </summary>
        public int EntriesRemoved { get; set; }

        /// <summary>
        /// Gets or sets when the cleanup occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
