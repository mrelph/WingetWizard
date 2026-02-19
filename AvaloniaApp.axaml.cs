using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.ViewModels;
using WingetWizard.Avalonia.Services;
using System.IO;
using System;

namespace WingetWizard.Avalonia;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        try
        {
            // Create debug log
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] === AVALONIA APP INITIALIZE ===\n");
            
            AvaloniaXamlLoader.Load(this);
            
            // Configure services
            var services = new ServiceCollection();
            try
            {
                ConfigureServices(services);
                Services = services.BuildServiceProvider();
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Services configured successfully\n");
            }
            catch (Exception serviceEx)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Service configuration error: {serviceEx.Message}\n");
                // Create minimal services as fallback
                Services = services.BuildServiceProvider();
            }
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Avalonia app initialized successfully\n");
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] INIT ERROR: {ex.Message}\n");
            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Framework initialization completed\n");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Creating MainWindow...\n");
                
                try
                {
                    var viewModel = Services.GetRequiredService<MainViewModel>();
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] MainViewModel created successfully\n");
                    
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = viewModel,
                    };
                }
                catch (Exception vmEx)
                {
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] MainViewModel creation failed: {vmEx.Message}\n");
                    
                    // Create MainWindow without ViewModel as fallback
                    desktop.MainWindow = new MainWindow
                    {
                        Title = "WingetWizard - AI-Enhanced Package Manager (No ViewModel)"
                    };
                }
                
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow created successfully\n");
            }
            else
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR: ApplicationLifetime is not desktop style\n");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] ApplicationLifetime type: {ApplicationLifetime?.GetType().Name ?? "null"}\n");
            }

            base.OnFrameworkInitializationCompleted();
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] App startup completed!\n");
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] STARTUP ERROR: {ex.Message}\n");
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register existing services (migrated from WinUI project)
        services.AddSingleton<IPackageService, PackageService>();
        services.AddSingleton<IAIService, AIService>();
        services.AddSingleton<IReportService>(provider => 
        {
            var reportsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WingetWizard", "Reports");
            return new ReportService(reportsDir);
        });
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IProgressService, ProgressService>();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>(provider =>
            new DashboardViewModel(
                provider.GetRequiredService<IPackageService>(),
                provider.GetRequiredService<IReportService>(),
                provider.GetRequiredService<IAIService>(),
                provider));
        services.AddTransient<PackagesViewModel>(provider => new PackagesViewModel(provider));
        services.AddTransient<UpdatesViewModel>(provider => new UpdatesViewModel(provider));
        services.AddTransient<AIResearchViewModel>(provider => new AIResearchViewModel(provider));
        services.AddTransient<SettingsViewModel>(provider => new SettingsViewModel(provider));
        services.AddTransient<SearchDialogViewModel>();
    }
}