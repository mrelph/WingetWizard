using System;
using System.Collections.Generic;

namespace WingetWizard.Models
{
    /// <summary>
    /// Represents the result of a comprehensive health check operation.
    /// Provides detailed information about system health, issues, and performance metrics.
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Gets or sets whether the overall system health is good.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets the collection of health issues found during the check.
        /// </summary>
        public List<string> Issues { get; set; } = new();

        /// <summary>
        /// Gets the collection of warnings that don't affect core functionality.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets the collection of performance and diagnostic metrics.
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp when the health check was performed.
        /// </summary>
        public DateTime CheckTimestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the duration of the health check operation in milliseconds.
        /// </summary>
        public long CheckDurationMs { get; set; }

        /// <summary>
        /// Gets the overall health status as a descriptive string.
        /// </summary>
        public string HealthStatus => IsHealthy ? "Healthy" : "Unhealthy";

        /// <summary>
        /// Gets the total number of issues and warnings combined.
        /// </summary>
        public int TotalIssues => Issues.Count + Warnings.Count;

        /// <summary>
        /// Adds a critical issue that affects system functionality.
        /// </summary>
        /// <param name="issue">The issue description</param>
        public void AddIssue(string issue)
        {
            if (!string.IsNullOrWhiteSpace(issue))
            {
                Issues.Add($"[{DateTime.Now:HH:mm:ss}] {issue}");
                IsHealthy = false;
            }
        }

        /// <summary>
        /// Adds a warning that doesn't affect core functionality.
        /// </summary>
        /// <param name="warning">The warning description</param>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add($"[{DateTime.Now:HH:mm:ss}] {warning}");
            }
        }

        /// <summary>
        /// Adds a metric value to the health check result.
        /// </summary>
        /// <param name="name">The metric name</param>
        /// <param name="value">The metric value</param>
        public void AddMetric(string name, object value)
        {
            if (!string.IsNullOrWhiteSpace(name) && value != null)
            {
                Metrics[name] = value;
            }
        }

        /// <summary>
        /// Gets a formatted summary of the health check results.
        /// </summary>
        /// <returns>A formatted string containing the health check summary</returns>
        public string GetSummary()
        {
            var summary = $"Health Check Summary - {HealthStatus}\n";
            summary += $"Timestamp: {CheckTimestamp:yyyy-MM-dd HH:mm:ss}\n";
            summary += $"Duration: {CheckDurationMs}ms\n";
            summary += $"Issues: {Issues.Count}, Warnings: {Warnings.Count}\n";
            
            if (Issues.Count > 0)
            {
                summary += "\nCritical Issues:\n";
                foreach (var issue in Issues)
                {
                    summary += $"  ❌ {issue}\n";
                }
            }

            if (Warnings.Count > 0)
            {
                summary += "\nWarnings:\n";
                foreach (var warning in Warnings)
                {
                    summary += $"  ⚠️ {warning}\n";
                }
            }

            if (Metrics.Count > 0)
            {
                summary += "\nMetrics:\n";
                foreach (var metric in Metrics)
                {
                    summary += $"  📊 {metric.Key}: {metric.Value}\n";
                }
            }

            return summary;
        }

        /// <summary>
        /// Creates a healthy result with no issues.
        /// </summary>
        /// <returns>A healthy HealthCheckResult instance</returns>
        public static HealthCheckResult Healthy()
        {
            return new HealthCheckResult { IsHealthy = true };
        }

        /// <summary>
        /// Creates an unhealthy result with the specified issue.
        /// </summary>
        /// <param name="issue">The critical issue description</param>
        /// <returns>An unhealthy HealthCheckResult instance</returns>
        public static HealthCheckResult Unhealthy(string issue)
        {
            var result = new HealthCheckResult { IsHealthy = false };
            result.AddIssue(issue);
            return result;
        }
    }

    /// <summary>
    /// Represents different categories of health checks.
    /// </summary>
    public enum HealthCheckCategory
    {
        /// <summary>
        /// Checks related to system services and dependencies.
        /// </summary>
        Services,

        /// <summary>
        /// Checks related to disk space and storage.
        /// </summary>
        Storage,

        /// <summary>
        /// Checks related to memory usage and performance.
        /// </summary>
        Memory,

        /// <summary>
        /// Checks related to network connectivity and API access.
        /// </summary>
        Network,

        /// <summary>
        /// Checks related to configuration and settings.
        /// </summary>
        Configuration,

        /// <summary>
        /// Checks related to file system permissions and access.
        /// </summary>
        Permissions
    }
}

