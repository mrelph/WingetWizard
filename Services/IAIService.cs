using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Interface for AI-related services
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Gets AI recommendation using two-stage process: Perplexity for research, Claude for formatting
    /// </summary>
    /// <param name="app">The package to analyze</param>
    /// <returns>AI-generated recommendation</returns>
    Task<string> GetAIRecommendationAsync(UpgradableApp app);
}


