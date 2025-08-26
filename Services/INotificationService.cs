namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Interface for notification services
/// </summary>
public interface INotificationService
{
    Task ShowSuccessAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
}

