using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UpgradeApp.Services
{
    /// <summary>
    /// Provides comprehensive performance metrics collection and monitoring for the WingetWizard application.
    /// Tracks system resources, operation performance, and application health metrics.
    /// </summary>
    public class PerformanceMetricsService : IDisposable
    {
        private readonly Dictionary<string, PerformanceMetric> _metrics = new();
        private readonly Dictionary<string, List<long>> _operationTimings = new();
        private readonly Dictionary<string, List<double>> _memoryUsage = new();
        private readonly Dictionary<string, List<double>> _cpuUsage = new();
        private readonly Stopwatch _startupTimer;
        private readonly PerformanceCounter? _cpuCounter;
        private bool _disposed = false;

        // Performance thresholds
        private const int MaxOperationHistory = 100;
        private const int MaxMemoryHistory = 50;
        private const int MaxCpuHistory = 50;
        private const double MemoryWarningThresholdMB = 500;
        private const double CpuWarningThresholdPercent = 80;
        private const long OperationWarningThresholdMs = 5000;

        public PerformanceMetricsService()
        {
            _startupTimer = Stopwatch.StartNew();
            
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
            catch
            {
                // CPU counter may not be available in some environments
                _cpuCounter = null;
            }

            // Initialize default metrics
            InitializeDefaultMetrics();
        }

        /// <summary>
        /// Initializes default performance metrics.
        /// </summary>
        private void InitializeDefaultMetrics()
        {
            AddMetric("ApplicationStartTime", DateTime.Now);
            AddMetric("ApplicationUptime", "0s");
            AddMetric("TotalOperations", 0);
            AddMetric("SuccessfulOperations", 0);
            AddMetric("FailedOperations", 0);
            AddMetric("AverageOperationTime", 0.0);
            AddMetric("PeakMemoryUsage", 0.0);
            AddMetric("CurrentMemoryUsage", 0.0);
            AddMetric("PeakCpuUsage", 0.0);
            AddMetric("CurrentCpuUsage", 0.0);
            AddMetric("DiskOperations", 0);
            AddMetric("NetworkOperations", 0);
            AddMetric("CacheHitRate", 0.0);
            AddMetric("ErrorRate", 0.0);
        }

        /// <summary>
        /// Adds or updates a performance metric.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Metric value</param>
        public void AddMetric(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            var metric = new PerformanceMetric
            {
                Name = name,
                Value = value,
                LastUpdated = DateTime.Now,
                UpdateCount = _metrics.ContainsKey(name) ? _metrics[name].UpdateCount + 1 : 1
            };

            _metrics[name] = metric;
        }

        /// <summary>
        /// Adds or updates a numeric performance metric (avoids boxing).
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Numeric metric value</param>
        public void AddMetric(string name, int value)
        {
            AddMetric(name, (object)value);
        }

        /// <summary>
        /// Adds or updates a numeric performance metric (avoids boxing).
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Numeric metric value</param>
        public void AddMetric(string name, long value)
        {
            AddMetric(name, (object)value);
        }

        /// <summary>
        /// Adds or updates a numeric performance metric (avoids boxing).
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Numeric metric value</param>
        public void AddMetric(string name, double value)
        {
            AddMetric(name, (object)value);
        }

        /// <summary>
        /// Adds or updates a string performance metric (avoids object cast).
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">String metric value</param>
        public void AddMetric(string name, string value)
        {
            AddMetric(name, (object)value);
        }

        /// <summary>
        /// Records the start of an operation for timing measurement.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Operation ID for tracking</returns>
        public string StartOperation(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                return string.Empty;

            var operationId = $"{operationName}_{DateTime.Now:HHmmss}_{Guid.NewGuid():N}";
            var stopwatch = Stopwatch.StartNew();
            
            if (!_operationTimings.ContainsKey(operationName))
            {
                _operationTimings[operationName] = new List<long>();
            }

            // Store the stopwatch for this operation
            _metrics[$"Operation_{operationId}"] = new PerformanceMetric
            {
                Name = $"Operation_{operationId}",
                Value = stopwatch,
                LastUpdated = DateTime.Now,
                UpdateCount = 1
            };

            return operationId;
        }

        /// <summary>
        /// Records the completion of an operation and calculates timing metrics.
        /// </summary>
        /// <param name="operationId">Operation ID returned from StartOperation</param>
        /// <param name="success">Whether the operation was successful</param>
        public void EndOperation(string operationId, bool success = true)
        {
            if (string.IsNullOrWhiteSpace(operationId) || !_metrics.ContainsKey($"Operation_{operationId}"))
                return;

            var metric = _metrics[$"Operation_{operationId}"];
            if (metric.Value is Stopwatch stopwatch)
            {
                stopwatch.Stop();
                var durationMs = stopwatch.ElapsedMilliseconds;

                // Extract operation name from ID
                var operationName = operationId.Split('_')[0];
                
                // Store timing in operation history
                if (_operationTimings.ContainsKey(operationName))
                {
                    var timings = _operationTimings[operationName];
                    timings.Add(durationMs);
                    
                    // Keep only recent history
                    if (timings.Count > MaxOperationHistory)
                    {
                        timings.RemoveAt(0);
                    }
                }

                // Update operation statistics
                UpdateOperationStatistics(operationName, durationMs, success);

                // Check for performance warnings
                if (durationMs > OperationWarningThresholdMs)
                {
                    AddMetric($"SlowOperation_{operationName}", $"Operation took {durationMs}ms (threshold: {OperationWarningThresholdMs}ms)");
                }

                // Clean up operation metric
                _metrics.Remove($"Operation_{operationId}");
            }
        }

        /// <summary>
        /// Updates operation statistics based on completion data.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="durationMs">Operation duration in milliseconds</param>
        /// <param name="success">Whether the operation was successful</param>
        private void UpdateOperationStatistics(string operationName, long durationMs, bool success)
        {
            var totalOps = GetMetricValue<int>("TotalOperations");
            var successfulOps = GetMetricValue<int>("SuccessfulOperations");
            var failedOps = GetMetricValue<int>("FailedOperations");

            AddMetric("TotalOperations", totalOps + 1);
            
            if (success)
            {
                AddMetric("SuccessfulOperations", successfulOps + 1);
            }
            else
            {
                AddMetric("FailedOperations", failedOps + 1);
            }

            // Calculate average operation time
            var currentAvg = GetMetricValue<double>("AverageOperationTime");
            var newAvg = (currentAvg * totalOps + durationMs) / (totalOps + 1);
            AddMetric("AverageOperationTime", Math.Round(newAvg, 2));

            // Update error rate
            var errorRate = (double)(failedOps + (success ? 0 : 1)) / (totalOps + 1) * 100;
            AddMetric("ErrorRate", Math.Round(errorRate, 2));
        }

        /// <summary>
        /// Records a disk operation for performance tracking.
        /// </summary>
        /// <param name="operationType">Type of disk operation (read/write/delete)</param>
        /// <param name="fileSize">Size of file in bytes</param>
        /// <param name="durationMs">Operation duration in milliseconds</param>
        public void RecordDiskOperation(string operationType, long fileSize, long durationMs)
        {
            var diskOps = GetMetricValue<int>("DiskOperations");
            AddMetric("DiskOperations", diskOps + 1);

            // Record operation-specific metrics
            AddMetric($"Disk_{operationType}_Count", GetMetricValue<int>($"Disk_{operationType}_Count") + 1);
            AddMetric($"Disk_{operationType}_TotalSize", GetMetricValue<long>($"Disk_{operationType}_TotalSize") + fileSize);
            AddMetric($"Disk_{operationType}_TotalTime", GetMetricValue<long>($"Disk_{operationType}_TotalTime") + durationMs);

            // Calculate average file size and operation time
            var count = GetMetricValue<int>($"Disk_{operationType}_Count");
            var totalSize = GetMetricValue<long>($"Disk_{operationType}_TotalSize");
            var totalTime = GetMetricValue<long>($"Disk_{operationType}_TotalTime");

            AddMetric($"Disk_{operationType}_AvgFileSize", Math.Round((double)totalSize / count, 2));
            AddMetric($"Disk_{operationType}_AvgTime", Math.Round((double)totalTime / count, 2));
        }

        /// <summary>
        /// Records a network operation for performance tracking.
        /// </summary>
        /// <param name="operationType">Type of network operation (API call/download/upload)</param>
        /// <param name="dataSize">Size of data in bytes</param>
        /// <param name="durationMs">Operation duration in milliseconds</param>
        /// <param name="success">Whether the operation was successful</param>
        public void RecordNetworkOperation(string operationType, long dataSize, long durationMs, bool success)
        {
            var networkOps = GetMetricValue<int>("NetworkOperations");
            AddMetric("NetworkOperations", networkOps + 1);

            // Record operation-specific metrics
            var successKey = success ? "Success" : "Failure";
            AddMetric($"Network_{operationType}_{successKey}_Count", GetMetricValue<int>($"Network_{operationType}_{successKey}_Count") + 1);
            AddMetric($"Network_{operationType}_TotalData", GetMetricValue<long>($"Network_{operationType}_TotalData") + dataSize);
            AddMetric($"Network_{operationType}_TotalTime", GetMetricValue<long>($"Network_{operationType}_TotalTime") + durationMs);

            // Calculate throughput
            if (durationMs > 0)
            {
                var throughputKBps = (dataSize / 1024.0) / (durationMs / 1000.0);
                AddMetric($"Network_{operationType}_ThroughputKBps", Math.Round(throughputKBps, 2));
            }
        }

        /// <summary>
        /// Records cache performance metrics.
        /// </summary>
        /// <param name="cacheName">Name of the cache</param>
        /// <param name="hit">Whether it was a cache hit</param>
        /// <param name="size">Size of cached data in bytes</param>
        public void RecordCacheOperation(string cacheName, bool hit, long size = 0)
        {
            var hitKey = hit ? "Hits" : "Misses";
            AddMetric($"Cache_{cacheName}_{hitKey}", GetMetricValue<int>($"Cache_{cacheName}_{hitKey}") + 1);

            if (size > 0)
            {
                AddMetric($"Cache_{cacheName}_TotalSize", GetMetricValue<long>($"Cache_{cacheName}_TotalSize") + size);
            }

            // Calculate cache hit rate
            var hits = GetMetricValue<int>($"Cache_{cacheName}_Hits");
            var misses = GetMetricValue<int>($"Cache_{cacheName}_Misses");
            var total = hits + misses;

            if (total > 0)
            {
                var hitRate = (double)hits / total * 100;
                AddMetric($"Cache_{cacheName}_HitRate", Math.Round(hitRate, 2));
            }

            // Update global cache hit rate
            var globalHits = GetMetricValue<int>("Cache_Global_Hits");
            var globalMisses = GetMetricValue<int>("Cache_Global_Misses");
            var globalTotal = globalHits + globalMisses;

            if (globalTotal > 0)
            {
                var globalHitRate = (double)globalHits / globalTotal * 100;
                AddMetric("CacheHitRate", Math.Round(globalHitRate, 2));
            }
        }

        /// <summary>
        /// Collects current system performance metrics.
        /// </summary>
        public void CollectSystemMetrics()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentTime = DateTime.Now;

                // Update application uptime
                var uptime = _startupTimer.Elapsed;
                AddMetric("ApplicationUptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");

                // Memory metrics
                var workingSetMB = currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                var privateMB = currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0;
                var virtualMB = currentProcess.VirtualMemorySize64 / 1024.0 / 1024.0;

                AddMetric("CurrentMemoryUsage", Math.Round(workingSetMB, 2));
                AddMetric("PrivateMemoryUsage", Math.Round(privateMB, 2));
                AddMetric("VirtualMemoryUsage", Math.Round(virtualMB, 2));

                // Track peak memory usage
                var peakMemory = GetMetricValue<double>("PeakMemoryUsage");
                if (workingSetMB > peakMemory)
                {
                    AddMetric("PeakMemoryUsage", Math.Round(workingSetMB, 2));
                }

                // Store memory history
                if (!_memoryUsage.ContainsKey("WorkingSet"))
                {
                    _memoryUsage["WorkingSet"] = new List<double>();
                }
                _memoryUsage["WorkingSet"].Add(workingSetMB);
                if (_memoryUsage["WorkingSet"].Count > MaxMemoryHistory)
                {
                    _memoryUsage["WorkingSet"].RemoveAt(0);
                }

                // CPU metrics
                if (_cpuCounter != null)
                {
                    try
                    {
                        var cpuPercent = _cpuCounter.NextValue();
                        AddMetric("CurrentCpuUsage", Math.Round(cpuPercent, 2));

                        // Track peak CPU usage
                        var peakCpu = GetMetricValue<double>("PeakCpuUsage");
                        if (cpuPercent > peakCpu)
                        {
                            AddMetric("PeakCpuUsage", Math.Round(cpuPercent, 2));
                        }

                        // Store CPU history
                        if (!_cpuUsage.ContainsKey("Processor"))
                        {
                            _cpuUsage["Processor"] = new List<double>();
                        }
                        _cpuUsage["Processor"].Add(cpuPercent);
                        if (_cpuUsage["Processor"].Count > MaxCpuHistory)
                        {
                            _cpuUsage["Processor"].RemoveAt(0);
                        }

                        // Check for performance warnings
                        if (cpuPercent > CpuWarningThresholdPercent)
                        {
                            AddMetric("HighCpuUsage", $"CPU usage is {cpuPercent}% (threshold: {CpuWarningThresholdPercent}%)");
                        }
                    }
                    catch
                    {
                        // CPU counter may become unavailable
                        AddMetric("CurrentCpuUsage", "Unavailable");
                    }
                }

                // Check for memory warnings
                if (workingSetMB > MemoryWarningThresholdMB)
                {
                    AddMetric("HighMemoryUsage", $"Memory usage is {workingSetMB}MB (threshold: {MemoryWarningThresholdMB}MB)");
                }

                // Process metrics
                AddMetric("ThreadCount", currentProcess.Threads.Count);
                AddMetric("HandleCount", currentProcess.HandleCount);
                AddMetric("ProcessPriority", currentProcess.BasePriority);

                // GC metrics
                var gcMemory = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                AddMetric("GCTotalMemory", Math.Round(gcMemory, 2));
                AddMetric("GCGen0Count", GC.CollectionCount(0));
                AddMetric("GCGen1Count", GC.CollectionCount(1));
                AddMetric("GCGen2Count", GC.CollectionCount(2));
            }
            catch (Exception ex)
            {
                AddMetric("SystemMetricsError", ex.Message);
            }
        }

        /// <summary>
        /// Gets a metric value with type conversion.
        /// </summary>
        /// <typeparam name="T">Expected type of the metric value</typeparam>
        /// <param name="name">Metric name</param>
        /// <param name="defaultValue">Default value if metric doesn't exist or conversion fails</param>
        /// <returns>Metric value or default value</returns>
        public T GetMetricValue<T>(string name, T defaultValue = default!)
        {
            if (!_metrics.ContainsKey(name))
                return defaultValue;

            try
            {
                var value = _metrics[name].Value;
                if (value is T typedValue)
                    return typedValue;

                // Try to convert the value
                if (typeof(T) == typeof(int) && int.TryParse(value.ToString(), out var intValue))
                    return (T)(object)intValue;
                if (typeof(T) == typeof(double) && double.TryParse(value.ToString(), out var doubleValue))
                    return (T)(object)doubleValue;
                if (typeof(T) == typeof(long) && long.TryParse(value.ToString(), out var longValue))
                    return (T)(object)longValue;
                if (typeof(T) == typeof(bool) && bool.TryParse(value.ToString(), out var boolValue))
                    return (T)(object)boolValue;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets all performance metrics as a collection.
        /// </summary>
        /// <returns>Dictionary of all metrics</returns>
        public Dictionary<string, PerformanceMetric> GetAllMetrics()
        {
            return new Dictionary<string, PerformanceMetric>(_metrics);
        }

        /// <summary>
        /// Gets operation timing statistics.
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Operation timing statistics</returns>
        public OperationTimingStats GetOperationTimingStats(string operationName)
        {
            if (!_operationTimings.ContainsKey(operationName))
            {
                return new OperationTimingStats { OperationName = operationName };
            }

            var timings = _operationTimings[operationName];
            if (timings.Count == 0)
            {
                return new OperationTimingStats { OperationName = operationName };
            }

            return new OperationTimingStats
            {
                OperationName = operationName,
                TotalOperations = timings.Count,
                AverageTimeMs = Math.Round(timings.Average(), 2),
                MinTimeMs = timings.Min(),
                MaxTimeMs = timings.Max(),
                MedianTimeMs = GetMedian(timings),
                P95TimeMs = GetPercentile(timings, 95),
                P99TimeMs = GetPercentile(timings, 99)
            };
        }

        /// <summary>
        /// Gets memory usage history.
        /// </summary>
        /// <returns>Memory usage history data</returns>
        public Dictionary<string, List<double>> GetMemoryHistory()
        {
            return new Dictionary<string, List<double>>(_memoryUsage);
        }

        /// <summary>
        /// Gets CPU usage history.
        /// </summary>
        /// <returns>CPU usage history data</returns>
        public Dictionary<string, List<double>> GetCpuHistory()
        {
            return new Dictionary<string, List<double>>(_cpuUsage);
        }

        /// <summary>
        /// Generates a comprehensive performance report.
        /// </summary>
        /// <returns>Formatted performance report</returns>
        public string GeneratePerformanceReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("Performance Metrics Report");
            report.AppendLine("=========================");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Application Uptime: {GetMetricValue<string>("ApplicationUptime")}");
            report.AppendLine();

            // System Resources
            report.AppendLine("System Resources:");
            report.AppendLine("-----------------");
            report.AppendLine($"Current Memory: {GetMetricValue<double>("CurrentMemoryUsage")}MB");
            report.AppendLine($"Peak Memory: {GetMetricValue<double>("PeakMemoryUsage")}MB");
            report.AppendLine($"Current CPU: {GetMetricValue<double>("CurrentCpuUsage")}%");
            report.AppendLine($"Peak CPU: {GetMetricValue<double>("PeakCpuUsage")}%");
            report.AppendLine($"Threads: {GetMetricValue<int>("ThreadCount")}");
            report.AppendLine($"Handles: {GetMetricValue<int>("HandleCount")}");
            report.AppendLine();

            // Operation Statistics
            report.AppendLine("Operation Statistics:");
            report.AppendLine("---------------------");
            report.AppendLine($"Total Operations: {GetMetricValue<int>("TotalOperations")}");
            report.AppendLine($"Successful: {GetMetricValue<int>("SuccessfulOperations")}");
            report.AppendLine($"Failed: {GetMetricValue<int>("FailedOperations")}");
            report.AppendLine($"Average Time: {GetMetricValue<double>("AverageOperationTime")}ms");
            report.AppendLine($"Error Rate: {GetMetricValue<double>("ErrorRate")}%");
            report.AppendLine();

            // Performance Metrics
            report.AppendLine("Performance Metrics:");
            report.AppendLine("---------------------");
            report.AppendLine($"Disk Operations: {GetMetricValue<int>("DiskOperations")}");
            report.AppendLine($"Network Operations: {GetMetricValue<int>("NetworkOperations")}");
            report.AppendLine($"Cache Hit Rate: {GetMetricValue<double>("CacheHitRate")}%");
            report.AppendLine();

            // Operation Timings
            if (_operationTimings.Count > 0)
            {
                report.AppendLine("Operation Timings:");
                report.AppendLine("------------------");
                foreach (var operation in _operationTimings.Keys)
                {
                    var stats = GetOperationTimingStats(operation);
                    if (stats.TotalOperations > 0)
                    {
                        report.AppendLine($"{operation}:");
                        report.AppendLine($"  Count: {stats.TotalOperations}");
                        report.AppendLine($"  Average: {stats.AverageTimeMs}ms");
                        report.AppendLine($"  Min: {stats.MinTimeMs}ms");
                        report.AppendLine($"  Max: {stats.MaxTimeMs}ms");
                        report.AppendLine($"  P95: {stats.P95TimeMs}ms");
                        report.AppendLine();
                    }
                }
            }

            return report.ToString();
        }

        /// <summary>
        /// Calculates the median value from a list of numbers.
        /// </summary>
        /// <param name="values">List of values</param>
        /// <returns>Median value</returns>
        private static double GetMedian(List<long> values)
        {
            if (values.Count == 0) return 0;
            
            var sorted = values.OrderBy(x => x).ToList();
            var count = sorted.Count;
            var mid = count / 2;
            
            if (count % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2.0;
            else
                return sorted[mid];
        }

        /// <summary>
        /// Calculates the nth percentile from a list of numbers.
        /// </summary>
        /// <param name="values">List of values</param>
        /// <param name="percentile">Percentile to calculate (0-100)</param>
        /// <returns>Percentile value</returns>
        private static long GetPercentile(List<long> values, int percentile)
        {
            if (values.Count == 0) return 0;
            
            var sorted = values.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cpuCounter?.Dispose();
                _startupTimer?.Stop();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a performance metric with metadata.
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// Gets or sets the metric name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metric value.
        /// </summary>
        public object Value { get; set; } = new();

        /// <summary>
        /// Gets or sets when the metric was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets how many times the metric has been updated.
        /// </summary>
        public int UpdateCount { get; set; }
    }

    /// <summary>
    /// Represents timing statistics for an operation.
    /// </summary>
    public class OperationTimingStats
    {
        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of operations.
        /// </summary>
        public int TotalOperations { get; set; }

        /// <summary>
        /// Gets or sets the average operation time in milliseconds.
        /// </summary>
        public double AverageTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum operation time in milliseconds.
        /// </summary>
        public long MinTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum operation time in milliseconds.
        /// </summary>
        public long MaxTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the median operation time in milliseconds.
        /// </summary>
        public double MedianTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the 95th percentile operation time in milliseconds.
        /// </summary>
        public long P95TimeMs { get; set; }

        /// <summary>
        /// Gets or sets the 99th percentile operation time in milliseconds.
        /// </summary>
        public long P99TimeMs { get; set; }
    }
}
