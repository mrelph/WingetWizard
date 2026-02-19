using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WingetWizard.Avalonia.Models
{
    /// <summary>
    /// User profile for personalized AI recommendations
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> PreferredCategories { get; set; } = new();
        public List<string> DevelopmentEnvironments { get; set; } = new();
        public string ExperienceLevel { get; set; } = "Intermediate"; // Beginner, Intermediate, Advanced
        public List<string> UsagePatterns { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Package compatibility analysis result
    /// </summary>
    public class PackageCompatibilityAnalysis
    {
        public string PackageId { get; set; } = string.Empty;
        public string CompatibilityScore { get; set; } = "Unknown"; // High, Medium, Low, Incompatible
        public List<string> PotentialConflicts { get; set; } = new();
        public List<string> RequiredDependencies { get; set; } = new();
        public List<string> RecommendedCompanionPackages { get; set; } = new();
        public string Analysis { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Automated maintenance recommendation
    /// </summary>
    public class MaintenanceRecommendation : INotifyPropertyChanged
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // High, Medium, Low
        public string Category { get; set; } = string.Empty; // Updates, Security, Cleanup, Optimization
        public List<string> AffectedPackages { get; set; } = new();
        public string Action { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        private bool _isCompleted;
        public bool IsCompleted 
        { 
            get => _isCompleted; 
            set => SetProperty(ref _isCompleted, value); 
        }

        private bool _isDismissed;
        public bool IsDismissed 
        { 
            get => _isDismissed; 
            set => SetProperty(ref _isDismissed, value); 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Security analysis report for a package
    /// </summary>
    public class SecurityAnalysisReport
    {
        public string PackageId { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string SecurityScore { get; set; } = "Unknown"; // Excellent, Good, Fair, Poor, Critical
        public List<SecurityFinding> Findings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string PublisherVerification { get; set; } = "Unknown";
        public string CodeSigningStatus { get; set; } = "Unknown";
        public DateTime LastScanned { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Individual security finding
    /// </summary>
    public class SecurityFinding
    {
        public string Type { get; set; } = string.Empty; // Vulnerability, Warning, Info
        public string Severity { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Analytics data for package usage tracking
    /// </summary>
    public class PackageAnalytics
    {
        public string PackageId { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public DateTime InstallDate { get; set; }
        public DateTime LastUsed { get; set; }
        public int UsageFrequency { get; set; } // Days used per month
        public long DiskSpaceUsed { get; set; } // Bytes
        public string Category { get; set; } = string.Empty;
        public List<string> RelatedPackages { get; set; } = new();
    }

    /// <summary>
    /// System health report
    /// </summary>
    public class SystemHealthReport
    {
        public string OverallScore { get; set; } = "Good"; // Excellent, Good, Fair, Poor
        public int TotalPackages { get; set; }
        public int OutdatedPackages { get; set; }
        public int UnusedPackages { get; set; }
        public long TotalDiskSpace { get; set; }
        public long ReclaimableSpace { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Automated update schedule configuration
    /// </summary>
    public class UpdateSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Frequency { get; set; } = "Weekly"; // Daily, Weekly, Monthly
        public TimeSpan PreferredTime { get; set; } = new TimeSpan(2, 0, 0); // 2 AM
        public List<string> IncludedPackages { get; set; } = new();
        public List<string> ExcludedPackages { get; set; } = new();
        public bool AutoInstallUpdates { get; set; } = false;
        public bool NotifyBeforeUpdates { get; set; } = true;
        public DateTime NextRun { get; set; }
        public DateTime LastRun { get; set; }
    }
}