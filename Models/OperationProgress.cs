namespace WingetWizard.Avalonia.Models;

/// <summary>
/// Model for tracking operation progress
/// </summary>
public class OperationProgress
{
    public string OperationId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // Install, Uninstall, Update
    public int ProgressPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }
}

