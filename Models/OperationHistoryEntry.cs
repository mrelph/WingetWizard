using System;

namespace WingetWizard.Models
{
    /// <summary>
    /// Represents a single operation in the history log
    /// </summary>
    public class OperationHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string OperationType { get; set; } = string.Empty; // "Upgrade", "Install", "Uninstall", "Repair", "Research"
        public string PackageName { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalPackages { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}

