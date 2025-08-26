using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Interface for package management operations
/// </summary>
public interface IPackageService
{
    Task<List<UpgradableApp>> ListAllAppsAsync(string source, bool verbose = false);
    Task<List<UpgradableApp>> CheckForUpdatesAsync(string source, bool verbose = false);
    Task<(bool Success, string Message)> UpgradePackageAsync(string packageId, bool verbose = false);
    Task<(bool Success, string Message)> UpgradeAllPackagesAsync(bool verbose = false);
    Task<(bool Success, string Message)> InstallPackageAsync(string packageId, bool verbose = false);
    Task<(bool Success, string Message)> UninstallPackageAsync(string packageId, bool verbose = false);
    Task<(bool Success, string Message)> RepairPackageAsync(string packageId, bool verbose = false);
    
    /// <summary>
    /// Searches for packages using winget
    /// </summary>
    Task<List<PackageSearchResult>> SearchPackagesAsync(string query, string? source = null, int count = 50, bool exact = false, bool verbose = false);
    
    /// <summary>
    /// Gets detailed information about a specific package
    /// </summary>
    Task<PackageSearchResult?> GetPackageDetailsAsync(string packageId, bool verbose = false);
    
    /// <summary>
    /// Installs multiple packages
    /// </summary>
    Task<(bool Success, string Message)> InstallMultiplePackagesAsync(List<string> packageIds, bool verbose = false);
    
    /// <summary>
    /// Exports package list to text format
    /// </summary>
    string ExportPackageList(List<UpgradableApp> packages);
}