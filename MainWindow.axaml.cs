using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using WingetWizard.Avalonia.ViewModels;
using WingetWizard.Avalonia.Views;

namespace WingetWizard.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Load Dashboard by default
        LoadDashboardPage();
        
        // Log window creation
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow constructor completed successfully with Dashboard\n");
        }
        catch { /* Ignore logging errors */ }
    }
    
    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // Update active state
            UpdateNavigationState(button);
            
            var contentTitle = this.FindControl<TextBlock>("ContentTitle");
            var statusText = this.FindControl<TextBlock>("StatusText");
            var mainContentControl = this.FindControl<ContentControl>("MainContentControl");
            
            switch (button.Name)
            {
                case "DashboardBtn":
                    LoadDashboardPage();
                    break;
                case "PackagesBtn":
                    LoadPackagesPage();
                    break;
                case "UpdatesBtn":
                    LoadUpdatesPage();
                    break;
                case "AIBtn":
                    LoadAIResearchPage();
                    break;
                case "SettingsBtn":
                    LoadSettingsPage();
                    break;
                default:
                    LoadDashboardPage();
                    break;
            }
            
            // Log navigation
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Enhanced Navigation: {button.Name}\n");
            }
            catch { /* Ignore logging errors */ }
        }
    }

    private void LoadDashboardPage()
    {
        try
        {
            var mainContentControl = this.FindControl<ContentControl>("MainContentControl");
            if (mainContentControl != null)
            {
                var dashboardPage = new Views.DashboardPage();
                var dashboardViewModel = App.Services.GetRequiredService<DashboardViewModel>();
                dashboardPage.DataContext = dashboardViewModel;
                mainContentControl.Content = dashboardPage;
                
                // Load dashboard data
                _ = dashboardViewModel.LoadDashboardDataCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            // Log error and show fallback content
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Error loading DashboardPage: {ex.Message}\n");
            }
            catch { /* Ignore logging errors */ }
        }
    }

    private void LoadPackagesPage()
    {
        try
        {
            var mainContentControl = this.FindControl<ContentControl>("MainContentControl");
            if (mainContentControl != null)
            {
                var packagesPage = new Views.PackagesPage();
                var packagesViewModel = App.Services.GetRequiredService<PackagesViewModel>();
                packagesPage.DataContext = packagesViewModel;
                mainContentControl.Content = packagesPage;
            }
        }
        catch (Exception ex)
        {
            // Log error and show fallback content
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Error loading PackagesPage: {ex.Message}\n");
            }
            catch { /* Ignore logging errors */ }
        }
    }

    private void LoadUpdatesPage()
    {
        try
        {
            var mainContentControl = this.FindControl<ContentControl>("MainContentControl");
            if (mainContentControl != null)
            {
                var updatesPage = new Views.UpdatesPage();
                var updatesViewModel = App.Services.GetRequiredService<UpdatesViewModel>();
                updatesPage.DataContext = updatesViewModel;
                mainContentControl.Content = updatesPage;
            }
        }
        catch (Exception ex)
        {
            // Log error
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Error loading UpdatesPage: {ex.Message}\n");
            }
            catch { /* Ignore logging errors */ }
        }
    }

    private void LoadAIResearchPage()
    {
        try
        {
            var mainContentControl = this.FindControl<ContentControl>("MainContentControl");
            if (mainContentControl != null)
            {
                var aiResearchPage = new Views.AIResearchPage();
                var aiResearchViewModel = App.Services.GetRequiredService<AIResearchViewModel>();
                aiResearchPage.DataContext = aiResearchViewModel;
                mainContentControl.Content = aiResearchPage;
            }
        }
        catch (Exception ex)
        {
            // Log error
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Error loading AIResearchPage: {ex.Message}\n");
            }
            catch { /* Ignore logging errors */ }
        }
    }

    private void LoadSettingsPage()
    {
        try
        {
            var mainContentControl = this.FindControl<ContentControl>("MainContentControl");
            if (mainContentControl != null)
            {
                var settingsPage = new Views.SettingsPage();
                var settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();
                settingsPage.DataContext = settingsViewModel;
                mainContentControl.Content = settingsPage;
            }
        }
        catch (Exception ex)
        {
            // Log error
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Error loading SettingsPage: {ex.Message}\n");
            }
            catch { /* Ignore logging errors */ }
        }
    }
    
    private void UpdateNavigationState(Button activeButton)
    {
        // Remove active class from all navigation buttons
        var navButtons = new[] { "DashboardBtn", "PackagesBtn", "UpdatesBtn", "AIBtn", "SettingsBtn" };
        
        foreach (var btnName in navButtons)
        {
            var btn = this.FindControl<Button>(btnName);
            if (btn != null)
            {
                // Update classes based on active state
                if (btn == activeButton)
                {
                    btn.Classes.Clear();
                    btn.Classes.Add("primary-button");
                }
                else
                {
                    btn.Classes.Clear();
                    btn.Classes.Add("secondary-button");
                }
            }
        }
    }
}