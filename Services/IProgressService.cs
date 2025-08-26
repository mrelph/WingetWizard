using System.Collections.ObjectModel;
using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Interface for tracking operation progress
/// </summary>
public interface IProgressService
{
    ObservableCollection<OperationProgress> ActiveOperations { get; }
    
    Task<string> StartOperationAsync(string packageName, string operation);
    Task UpdateProgressAsync(string operationId, int percentage, string status);
    Task CompleteOperationAsync(string operationId, bool success, string message = "");
    Task CancelOperationAsync(string operationId);
    void ClearCompletedOperations();
}

