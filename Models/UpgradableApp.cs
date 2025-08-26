using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WingetWizard.Avalonia.Models
{
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

        /// <summary>
        /// Whether this package is selected for batch operations
        /// </summary>
        public bool IsSelected 
        { 
            get => _isSelected; 
            set => SetProperty(ref _isSelected, value); 
        }
        private bool _isSelected;

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



