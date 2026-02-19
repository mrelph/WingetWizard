using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Interface for AI-related services with advanced intelligence features
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Gets AI recommendation using two-stage process: Perplexity for research, Claude for formatting
    /// </summary>
    /// <param name="app">The package to analyze</param>
    /// <returns>AI-generated recommendation</returns>
    Task<string> GetAIRecommendationAsync(UpgradableApp app);

    /// <summary>
    /// Gets personalized package recommendations based on user's current packages
    /// </summary>
    /// <param name="installedPackages">List of currently installed packages</param>
    /// <param name="userProfile">User preferences and usage patterns</param>
    /// <returns>List of recommended packages with reasoning</returns>
    Task<List<AIRecommendation>> GetPersonalizedRecommendationsAsync(List<UpgradableApp> installedPackages, UserProfile? userProfile = null);

    /// <summary>
    /// Analyzes package compatibility and dependencies
    /// </summary>
    /// <param name="packageId">Package to analyze</param>
    /// <param name="installedPackages">Currently installed packages</param>
    /// <returns>Compatibility analysis with potential conflicts</returns>
    Task<PackageCompatibilityAnalysis> AnalyzeCompatibilityAsync(string packageId, List<UpgradableApp> installedPackages);

    /// <summary>
    /// Generates automated maintenance recommendations
    /// </summary>
    /// <param name="installedPackages">Current package state</param>
    /// <returns>Maintenance recommendations with priority levels</returns>
    Task<List<MaintenanceRecommendation>> GenerateMaintenanceRecommendationsAsync(List<UpgradableApp> installedPackages);

    /// <summary>
    /// Provides security analysis for packages
    /// </summary>
    /// <param name="package">Package to analyze</param>
    /// <returns>Security analysis report</returns>
    Task<SecurityAnalysisReport> AnalyzePackageSecurityAsync(UpgradableApp package);
}


