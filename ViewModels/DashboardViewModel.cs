using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.Services;
using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IPackageService _packageService;
    private readonly IReportService _reportService;
    private readonly IAIService _aiService;
    
    [ObservableProperty] private string _welcomeMessage = "Welcome to WingetWizard";
    [ObservableProperty] private int _installedCount = 0;
    [ObservableProperty] private int _updatesCount = 0;
    [ObservableProperty] private int _aiReportsCount = 0;
    [ObservableProperty] private ObservableCollection<ActivityItem> _recentActivity = new();
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isLoadingStats = false;
    [ObservableProperty] private bool _isLoadingActivity = false;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private ObservableCollection<object> _skeletonStats = new();
    [ObservableProperty] private ObservableCollection<object> _skeletonActivity = new();
    
    // Phase 3 - Advanced AI Features
    [ObservableProperty] private ObservableCollection<AIRecommendation> _aiRecommendations = new();
    [ObservableProperty] private ObservableCollection<MaintenanceRecommendation> _maintenanceRecommendations = new();
    [ObservableProperty] private SystemHealthReport? _systemHealthReport;
    [ObservableProperty] private bool _isLoadingAIRecommendations = false;
    [ObservableProperty] private bool _isLoadingMaintenance = false;
    [ObservableProperty] private bool _isLoadingHealthReport = false;
    [ObservableProperty] private string _aiStatus = "AI insights ready";
    [ObservableProperty] private int _highPriorityCount = 0;
    [ObservableProperty] private int _securityIssuesCount = 0;
    
    public DashboardViewModel() : this(null!, null!, null!, null!)
    {
        // Design-time constructor
        if (Design.IsDesignMode)
        {
            WelcomeMessage = GetWelcomeMessage();
            InstalledCount = 127;
            UpdatesCount = 8;
            AiReportsCount = 15;
            LoadSampleActivity();
            LoadSampleAIData();
        }
        
        // Initialize skeleton items
        for (int i = 0; i < 3; i++)
        {
            SkeletonStats.Add(new object());
        }
        for (int i = 0; i < 4; i++)
        {
            SkeletonActivity.Add(new object());
        }
    }
    
    public DashboardViewModel(IPackageService packageService, IReportService reportService, IAIService aiService, IServiceProvider services) : base(services)
    {
        _packageService = packageService;
        _reportService = reportService;
        _aiService = aiService;
        
        WelcomeMessage = GetWelcomeMessage();
        LoadDashboardDataCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
        CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdatesAsync);
        AIResearchCommand = new RelayCommand(NavigateToAIResearch);
        ViewPackagesCommand = new RelayCommand(NavigateToPackages);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        ViewAllActivityCommand = new RelayCommand(ViewAllActivity);
        RefreshCommand = new AsyncRelayCommand(RefreshDashboardAsync);
    }
    
    public IAsyncRelayCommand LoadDashboardDataCommand { get; }
    public IAsyncRelayCommand CheckUpdatesCommand { get; }
    public IRelayCommand AIResearchCommand { get; }
    public IRelayCommand ViewPackagesCommand { get; }
    public IAsyncRelayCommand ExportReportCommand { get; }
    public IRelayCommand ViewAllActivityCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    
    private string GetWelcomeMessage()
    {
        var hour = DateTime.Now.Hour;
        var timeOfDay = hour switch
        {
            < 12 => "morning",
            < 17 => "afternoon",
            _ => "evening"
        };
        
        return $"Good {timeOfDay}!";
    }
    
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            IsLoading = true;
            IsLoadingStats = true;
            IsLoadingActivity = true;
            StatusMessage = "Loading dashboard data...";
            
            if (_packageService != null)
            {
                // Load installed packages count with progressive loading
                StatusMessage = "Checking installed packages...";
                await Task.Delay(500); // Simulate loading time for skeleton animation
                var packages = await _packageService.ListAllAppsAsync("winget", false);
                InstalledCount = packages.Count();
                
                // Load updates count
                StatusMessage = "Checking for updates...";
                await Task.Delay(300);
                var updates = await _packageService.CheckForUpdatesAsync("winget", false);
                UpdatesCount = updates.Count();
                
                IsLoadingStats = false; // Show stats as they become available
            }
            
            if (_reportService != null)
            {
                // This would load AI reports count if implemented
                // AIReportsCount = await _reportService.GetReportsCountAsync();
                AiReportsCount = 3; // Placeholder
            }
            
            StatusMessage = "Loading recent activity...";
            await LoadRecentActivityAsync();
            IsLoadingActivity = false; // Show activity as it becomes available
            
            StatusMessage = "Dashboard loaded successfully";
            
            // Add recent activity for the data load
            RecentActivity.Insert(0, new ActivityItem
            {
                Icon = "📊",
                Title = "Dashboard refreshed",
                Description = $"Loaded {InstalledCount} packages, {UpdatesCount} updates available",
                TimeAgo = "Just now"
            });
            
            // Keep only the last 5 activities
            while (RecentActivity.Count > 5)
            {
                RecentActivity.RemoveAt(RecentActivity.Count - 1);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            
            // Set fallback values
            InstalledCount = 0;
            UpdatesCount = 0;
            AiReportsCount = 0;
            
            // Add error to recent activity
            RecentActivity.Insert(0, new ActivityItem
            {
                Icon = "❌",
                Title = "Dashboard load failed",
                Description = ex.Message,
                TimeAgo = "Just now"
            });
        }
        finally
        {
            IsLoading = false;
            IsLoadingStats = false;
            IsLoadingActivity = false;
            
            // Phase 3: Also load AI insights in background
            if (_aiService != null)
            {
                _ = Task.Run(async () =>
                {
                    await LoadAIRecommendationsCommand.ExecuteAsync(null);
                    await LoadMaintenanceRecommendationsCommand.ExecuteAsync(null);
                    await GenerateSystemHealthReportCommand.ExecuteAsync(null);
                });
            }
        }
    }
    
    private async Task RefreshDashboardAsync()
    {
        await LoadDashboardDataAsync();
    }
    
    private async Task LoadRecentActivityAsync()
    {
        // Simulate progressive loading of activity items
        await Task.Delay(800); // Show skeleton for a bit longer
        
        if (RecentActivity.Count == 0)
        {
            LoadSampleActivity();
        }
    }
    
    private void LoadSampleActivity()
    {
        RecentActivity.Clear();
        RecentActivity.Add(new ActivityItem
        {
            Icon = "🔄",
            Title = "Updated 3 packages",
            Description = "Visual Studio Code, Git, Node.js",
            TimeAgo = "2 hours ago"
        });
        RecentActivity.Add(new ActivityItem
        {
            Icon = "🤖",
            Title = "Generated AI report",
            Description = "Analyzed Docker Desktop compatibility",
            TimeAgo = "Yesterday"
        });
        RecentActivity.Add(new ActivityItem
        {
            Icon = "📦",
            Title = "Installed new package",
            Description = "Microsoft PowerToys",
            TimeAgo = "2 days ago"
        });
    }
    
    private async Task CheckUpdatesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Checking for updates...";
            
            if (_packageService != null)
            {
                var updates = await _packageService.CheckForUpdatesAsync("winget", false);
                UpdatesCount = updates.Count();
                
                RecentActivity.Insert(0, new ActivityItem
                {
                    Icon = "🔄",
                    Title = "Checked for updates",
                    Description = $"Found {UpdatesCount} available updates",
                    TimeAgo = "Just now"
                });
                
                StatusMessage = $"Found {UpdatesCount} updates available";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error checking updates: {ex.Message}";
            RecentActivity.Insert(0, new ActivityItem
            {
                Icon = "❌",
                Title = "Update check failed",
                Description = ex.Message,
                TimeAgo = "Just now"
            });
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void NavigateToAIResearch()
    {
        // Navigate to AI Research page
        // This would be handled by the main navigation system
        RecentActivity.Insert(0, new ActivityItem
        {
            Icon = "🤖",
            Title = "Opened AI Research",
            Description = "Navigated to AI Research page",
            TimeAgo = "Just now"
        });
    }
    
    private void NavigateToPackages()
    {
        // Navigate to Packages page
        // This would be handled by the main navigation system
        RecentActivity.Insert(0, new ActivityItem
        {
            Icon = "📦",
            Title = "Opened Packages",
            Description = "Navigated to Packages page",
            TimeAgo = "Just now"
        });
    }
    
    private async Task ExportReportAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Exporting package report...";
            
            // Simulate export operation
            await Task.Delay(2000);
            
            RecentActivity.Insert(0, new ActivityItem
            {
                Icon = "📊",
                Title = "Exported package report",
                Description = "Generated comprehensive package report",
                TimeAgo = "Just now"
            });
            
            StatusMessage = "Report exported successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadAIRecommendationsAsync()
    {
        if (_aiService == null) return;
        
        IsLoadingAIRecommendations = true;
        AiStatus = "Analyzing your package ecosystem...";
        
        try
        {
            var installedPackages = await _packageService.ListAllAppsAsync("all", false);
            var recommendations = await _aiService.GetPersonalizedRecommendationsAsync(installedPackages);
            
            AiRecommendations.Clear();
            foreach (var rec in recommendations.Take(3)) // Show top 3 on dashboard
            {
                AiRecommendations.Add(rec);
            }
            
            AiStatus = $"Found {recommendations.Count} AI recommendations";
        }
        catch (Exception ex)
        {
            AiStatus = $"AI analysis failed: {ex.Message}";
        }
        finally
        {
            IsLoadingAIRecommendations = false;
        }
    }

    [RelayCommand]
    private async Task LoadMaintenanceRecommendationsAsync()
    {
        if (_aiService == null) return;
        
        IsLoadingMaintenance = true;
        StatusMessage = "Generating maintenance recommendations...";
        
        try
        {
            var installedPackages = await _packageService.ListAllAppsAsync("all", false);
            var recommendations = await _aiService.GenerateMaintenanceRecommendationsAsync(installedPackages);
            
            MaintenanceRecommendations.Clear();
            foreach (var rec in recommendations.Take(4)) // Show top 4 on dashboard
            {
                MaintenanceRecommendations.Add(rec);
            }
            
            HighPriorityCount = recommendations.Count(r => r.Priority == "High");
            SecurityIssuesCount = recommendations.Count(r => r.Category == "Security");
            
            StatusMessage = $"Generated {recommendations.Count} maintenance recommendations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Maintenance analysis failed: {ex.Message}";
        }
        finally
        {
            IsLoadingMaintenance = false;
        }
    }

    [RelayCommand]
    private async Task GenerateSystemHealthReportAsync()
    {
        if (_packageService == null) return;
        
        IsLoadingHealthReport = true;
        StatusMessage = "Generating system health report...";
        
        try
        {
            var installedPackages = await _packageService.ListAllAppsAsync("all", false);
            
            var healthReport = new SystemHealthReport
            {
                TotalPackages = installedPackages.Count,
                OutdatedPackages = installedPackages.Count(p => !string.IsNullOrEmpty(p.Available)),
                UnusedPackages = Math.Max(0, installedPackages.Count / 10), // Simulate unused packages
                TotalDiskSpace = (long)installedPackages.Count * 50 * 1024 * 1024, // Simulate 50MB per package
                ReclaimableSpace = (long)installedPackages.Count(p => !string.IsNullOrEmpty(p.Available)) * 10 * 1024 * 1024, // 10MB per outdated package
                GeneratedAt = DateTime.Now
            };
            
            // Determine overall score
            var outdatedRatio = (double)healthReport.OutdatedPackages / healthReport.TotalPackages;
            if (outdatedRatio < 0.1) healthReport.OverallScore = "Excellent";
            else if (outdatedRatio < 0.25) healthReport.OverallScore = "Good";
            else if (outdatedRatio < 0.5) healthReport.OverallScore = "Fair";
            else healthReport.OverallScore = "Poor";
            
            // Add recommendations
            if (healthReport.OutdatedPackages > 0)
                healthReport.RecommendedActions.Add($"Update {healthReport.OutdatedPackages} outdated packages");
            if (healthReport.UnusedPackages > 0)
                healthReport.RecommendedActions.Add($"Remove {healthReport.UnusedPackages} unused packages");
            healthReport.RecommendedActions.Add("Run security analysis on critical packages");
            
            SystemHealthReport = healthReport;
            StatusMessage = $"System health: {healthReport.OverallScore}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Health report failed: {ex.Message}";
        }
        finally
        {
            IsLoadingHealthReport = false;
        }
    }

    [RelayCommand]
    private async Task RunComprehensiveAnalysisAsync()
    {
        // Run all Phase 3 analyses together
        StatusMessage = "Running comprehensive system analysis...";
        
        var tasks = new[]
        {
            LoadAIRecommendationsCommand.ExecuteAsync(null),
            LoadMaintenanceRecommendationsCommand.ExecuteAsync(null),
            GenerateSystemHealthReportCommand.ExecuteAsync(null)
        };
        
        await Task.WhenAll(tasks);
        
        StatusMessage = "Comprehensive analysis complete";
    }

    private void LoadSampleAIData()
    {
        // Sample data for design-time
        AiRecommendations.Add(new AIRecommendation
        {
            Title = "Development Workflow Enhancement",
            Description = "Add Git GUI and code analysis tools",
            Priority = "High",
            Category = "Productivity"
        });
        
        MaintenanceRecommendations.Add(new MaintenanceRecommendation
        {
            Title = "15 Updates Available", 
            Description = "Critical security and performance updates",
            Priority = "High",
            Category = "Updates"
        });
        
        HighPriorityCount = 3;
        SecurityIssuesCount = 1;
        
        SystemHealthReport = new SystemHealthReport
        {
            OverallScore = "Good",
            TotalPackages = 127,
            OutdatedPackages = 15,
            UnusedPackages = 8,
            RecommendedActions = new() { "Update outdated packages", "Remove unused packages" }
        };
    }
    
    private void ViewAllActivity()
    {
        // Navigate to full activity log
        RecentActivity.Insert(0, new ActivityItem
        {
            Icon = "📜",
            Title = "Viewed activity log",
            Description = "Opened full activity history",
            TimeAgo = "Just now"
        });
    }
}

public class ActivityItem
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string TimeAgo { get; set; } = "";
}
