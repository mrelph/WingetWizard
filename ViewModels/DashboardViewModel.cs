using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WingetWizard.Avalonia.Services;

namespace WingetWizard.Avalonia.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IPackageService _packageService;
    private readonly IReportService _reportService;
    
    [ObservableProperty] private string _welcomeMessage = "Welcome to WingetWizard";
    [ObservableProperty] private int _installedCount = 0;
    [ObservableProperty] private int _updatesCount = 0;
    [ObservableProperty] private int _aiReportsCount = 0;
    [ObservableProperty] private ObservableCollection<ActivityItem> _recentActivity = new();
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _statusMessage = "Ready";
    
    public DashboardViewModel() : this(null!, null!, null!)
    {
        // Design-time constructor
        if (Design.IsDesignMode)
        {
            WelcomeMessage = GetWelcomeMessage();
            InstalledCount = 127;
            UpdatesCount = 8;
            AiReportsCount = 15;
            LoadSampleActivity();
        }
    }
    
    public DashboardViewModel(IPackageService packageService, IReportService reportService, IServiceProvider services) : base(services)
    {
        _packageService = packageService;
        _reportService = reportService;
        
        WelcomeMessage = GetWelcomeMessage();
        LoadDashboardDataCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
        CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdatesAsync);
        AIResearchCommand = new RelayCommand(NavigateToAIResearch);
        ViewPackagesCommand = new RelayCommand(NavigateToPackages);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        ViewAllActivityCommand = new RelayCommand(ViewAllActivity);
    }
    
    public IAsyncRelayCommand LoadDashboardDataCommand { get; }
    public IAsyncRelayCommand CheckUpdatesCommand { get; }
    public IRelayCommand AIResearchCommand { get; }
    public IRelayCommand ViewPackagesCommand { get; }
    public IAsyncRelayCommand ExportReportCommand { get; }
    public IRelayCommand ViewAllActivityCommand { get; }
    
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
            if (_packageService != null)
            {
                var packages = await _packageService.ListAllAppsAsync("winget", false);
                InstalledCount = packages.Count();
                
                var updates = await _packageService.CheckForUpdatesAsync("winget", false);
                UpdatesCount = updates.Count();
            }
            
            if (_reportService != null)
            {
                // Load AI reports count - would need to implement in ReportService
                // AIReportsCount = await _reportService.GetReportsCountAsync();
            }
            
            await LoadRecentActivityAsync();
        }
        catch (Exception ex)
        {
            // Handle error gracefully
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
        }
    }
    
    private async Task LoadRecentActivityAsync()
    {
        // This would load actual recent activity from a service
        // For now, using sample data
        await Task.Delay(100); // Simulate async operation
        LoadSampleActivity();
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
        // Navigate to updates page or trigger update check
        // This would be handled by the main navigation system
        await Task.Delay(100);
    }
    
    private void NavigateToAIResearch()
    {
        // Navigate to AI Research page
        // This would be handled by the main navigation system
    }
    
    private void NavigateToPackages()
    {
        // Navigate to Packages page
        // This would be handled by the main navigation system
    }
    
    private async Task ExportReportAsync()
    {
        // Export current package report
        await Task.Delay(100);
    }
    
    private void ViewAllActivity()
    {
        // Navigate to full activity log
    }
}

public class ActivityItem
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string TimeAgo { get; set; } = "";
}