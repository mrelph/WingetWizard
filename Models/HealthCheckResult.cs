using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UpgradeApp.Models
{
    /// <summary>
    /// Thread-safe result container for health check operations.
    /// Supports concurrent access from multiple health check tasks.
    /// </summary>
    public class HealthCheckResult
    {
        private readonly ConcurrentBag<string> _issues = new();
        private readonly ConcurrentBag<string> _warnings = new();
        private readonly ConcurrentDictionary<string, object> _metrics = new();

        /// <summary>
        /// Gets whether the health check passed (no issues found).
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets the duration of the health check in milliseconds.
        /// </summary>
        public long CheckDurationMs { get; set; }

        /// <summary>
        /// Gets the timestamp when the health check was performed.
        /// </summary>
        public DateTime CheckTimestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a thread-safe collection of critical issues found during the health check.
        /// </summary>
        public IReadOnlyCollection<string> Issues => _issues.ToList().AsReadOnly();

        /// <summary>
        /// Gets a thread-safe collection of warnings found during the health check.
        /// </summary>
        public IReadOnlyCollection<string> Warnings => _warnings.ToList().AsReadOnly();

        /// <summary>
        /// Gets a thread-safe dictionary of metrics collected during the health check.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metrics => 
            new Dictionary<string, object>(_metrics);

        /// <summary>
        /// Adds a critical issue to the health check result.
        /// Thread-safe operation.
        /// </summary>
        /// <param name="issue">Description of the critical issue</param>
        public void AddIssue(string issue)
        {
            if (!string.IsNullOrWhiteSpace(issue))
            {
                _issues.Add($"[{DateTime.UtcNow:HH:mm:ss}] {issue}");
            }
        }

        /// <summary>
        /// Adds a warning to the health check result.
        /// Thread-safe operation.
        /// </summary>
        /// <param name="warning">Description of the warning</param>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                _warnings.Add($"[{DateTime.UtcNow:HH:mm:ss}] {warning}");
            }
        }

        /// <summary>
        /// Adds a metric to the health check result.
        /// Thread-safe operation.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Metric value</param>
        public void AddMetric(string name, object value)
        {
            if (!string.IsNullOrWhiteSpace(name) && value != null)
            {
                _metrics.AddOrUpdate(name, value, (key, oldValue) => value);
            }
        }

        /// <summary>
        /// Gets a summary of the health check results.
        /// </summary>
        /// <returns>Formatted summary string</returns>
        public string GetSummary()
        {
            var issueCount = _issues.Count;
            var warningCount = _warnings.Count;
            var metricCount = _metrics.Count;

            return $"Health Check Summary - " +
                   $"Status: {(IsHealthy ? "HEALTHY" : "UNHEALTHY")}, " +
                   $"Issues: {issueCount}, " +
                   $"Warnings: {warningCount}, " +
                   $"Metrics: {metricCount}, " +
                   $"Duration: {CheckDurationMs}ms";
        }
    }

    /// <summary>
    /// Categories of health checks performed by the system.
    /// </summary>
    public enum HealthCheckCategory
    {
        Services,
        Storage,
        Memory,
        Network,
        Configuration,
        Permissions,
        Application
    }
}