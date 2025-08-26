using Avalonia;
using System;
using System.IO;

namespace WingetWizard.Avalonia;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var logPath = "";
        try
        {
            // Create debug log immediately 
            logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
            
            // Clear previous log
            if (File.Exists(logPath))
                File.Delete(logPath);
                
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] === AVALONIA PROGRAM MAIN START ===\n");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] .NET Version: {Environment.Version}\n");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] OS: {Environment.OSVersion}\n");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Working Dir: {Environment.CurrentDirectory}\n");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Args: [{string.Join(", ", args)}]\n");
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Building Avalonia AppBuilder...\n");
            var appBuilder = BuildAvaloniaApp();
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] AppBuilder created successfully\n");
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Starting with ClassicDesktopLifetime...\n");
            appBuilder.StartWithClassicDesktopLifetime(args);
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Avalonia app completed normally\n");
        }
        catch (Exception ex)
        {
            try
            {
                if (string.IsNullOrEmpty(logPath))
                    logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WingetWizard_Avalonia_Debug.log");
                
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] *** PROGRAM ERROR ***\n");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Error: {ex.Message}\n");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Type: {ex.GetType().Name}\n");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Stack trace:\n{ex.StackTrace}\n");
                
                if (ex.InnerException != null)
                {
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Inner exception: {ex.InnerException.Message}\n");
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] Inner stack trace:\n{ex.InnerException.StackTrace}\n");
                }
            }
            catch { /* Ignore logging errors */ }
            
            // Also write to console
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Check debug log: {logPath}");
            throw;
        }
    }

    // Avalonia configuration, also used by the designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}