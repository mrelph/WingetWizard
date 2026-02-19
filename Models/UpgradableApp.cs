using System;

namespace WingetWizard.Models
{
    /// <summary>
    /// Data model representing a Windows package that can be upgraded
    /// Contains package metadata and AI recommendation information
    /// </summary>
    public class UpgradableApp
    {
        public string Name { get; set; } = string.Empty;        // Display name of the application
        public string Id { get; set; } = string.Empty;          // Unique package identifier
        public string Version { get; set; } = string.Empty;     // Currently installed version
        public string Available { get; set; } = string.Empty;   // Available version for upgrade
        public string Status { get; set; } = string.Empty;        // Installation/upgrade status
        public string Recommendation { get; set; } = string.Empty;// AI-generated recommendation

        public override string ToString()
        {
            return $"{Name} ({Id}) - {Version} -> {Available}";
        }
    }
}
