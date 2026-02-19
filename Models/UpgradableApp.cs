using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WingetWizard.Avalonia.Models
{
    /// <summary>
    /// Represents an AI-generated insight about a package
    /// </summary>
    public class AIInsight
    {
        public string Type { get; set; } = string.Empty; // "security", "performance", "compatibility", etc.
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "info"; // "info", "warning", "error", "success"
    }

    /// <summary>
    /// Data model representing a Windows package that can be upgraded
    /// Contains package metadata and AI recommendation information
    /// </summary>
    public class UpgradableApp : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;        // Display name of the application
        public string Id { get; set; } = string.Empty;          // Unique package identifier
        public string Version { get; set; } = string.Empty;     // Currently installed version
        public string Available { get; set; } = string.Empty;   // Available version for upgrade
        public string AvailableVersion => Available;             // Alias for XAML binding compatibility
        public string Status { get; set; } = string.Empty;        // Installation/upgrade status
        public string Recommendation { get; set; } = string.Empty;// AI-generated recommendation
        public string Description { get; set; } = string.Empty;   // Package description

        /// <summary>
        /// Whether this package is selected for batch operations
        /// </summary>
        public bool IsSelected 
        { 
            get => _isSelected; 
            set => SetProperty(ref _isSelected, value); 
        }
        private bool _isSelected;

        /// <summary>
        /// Whether this package has AI insights available
        /// </summary>
        public bool HasAIInsights
        {
            get => _hasAIInsights;
            set => SetProperty(ref _hasAIInsights, value);
        }
        private bool _hasAIInsights;

        /// <summary>
        /// Collection of AI insights for this package
        /// </summary>
        public List<AIInsight> AIInsights { get; set; } = new();

        /// <summary>
        /// Whether AI insights panel is expanded
        /// </summary>
        public bool IsAIInsightsExpanded
        {
            get => _isAIInsightsExpanded;
            set => SetProperty(ref _isAIInsightsExpanded, value);
        }
        private bool _isAIInsightsExpanded;

        /// <summary>
        /// Toggles the AI insights panel expansion state
        /// </summary>
        public void ToggleAIInsights()
        {
            IsAIInsightsExpanded = !IsAIInsightsExpanded;
        }

        public override string ToString()
        {
            return $"{Name} ({Id}) - {Version} -> {Available}";
        }

        #region INotifyPropertyChanged Implementation

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

        #endregion
    }
}



