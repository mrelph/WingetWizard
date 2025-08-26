using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace WingetWizard.Avalonia.Models
{
    /// <summary>
    /// Represents an AI-powered package recommendation with trust indicators
    /// </summary>
    public class AIRecommendation : INotifyPropertyChanged
    {
        /// <summary>
        /// Package name
        /// </summary>
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// Package ID for installation
        /// </summary>
        public string PackageId { get; set; } = string.Empty;

        /// <summary>
        /// AI-generated recommendation text
        /// </summary>
        public string RecommendationText { get; set; } = string.Empty;

        /// <summary>
        /// Confidence level from AI (0-100)
        /// </summary>
        public int ConfidenceLevel { get; set; } = 85;

        /// <summary>
        /// Why this package was recommended
        /// </summary>
        public string ReasonForRecommendation { get; set; } = string.Empty;

        /// <summary>
        /// Similar packages for comparison
        /// </summary>
        public string SimilarPackages { get; set; } = string.Empty;

        /// <summary>
        /// Package category/type
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether this recommendation has been dismissed
        /// </summary>
        public bool IsDismissed 
        { 
            get => _isDismissed; 
            set => SetProperty(ref _isDismissed, value); 
        }
        private bool _isDismissed;

        /// <summary>
        /// Whether this package is selected for installation
        /// </summary>
        public bool IsSelected 
        { 
            get => _isSelected; 
            set => SetProperty(ref _isSelected, value); 
        }
        private bool _isSelected;

        /// <summary>
        /// Gets the confidence level as a formatted string
        /// </summary>
        public string ConfidenceLevelText => $"{ConfidenceLevel}%";

        /// <summary>
        /// Gets the confidence level color based on value
        /// </summary>
        public string ConfidenceColor => ConfidenceLevel switch
        {
            >= 90 => "#10B981", // Green for high confidence
            >= 75 => "#F59E0B", // Orange for medium confidence
            _ => "#EF4444"       // Red for low confidence
        };

        /// <summary>
        /// Gets the confidence level description
        /// </summary>
        public string ConfidenceDescription => ConfidenceLevel switch
        {
            >= 90 => "High confidence",
            >= 75 => "Medium confidence",
            _ => "Low confidence"
        };

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