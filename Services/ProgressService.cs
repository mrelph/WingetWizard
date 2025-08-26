using System.Collections.ObjectModel;
using WingetWizard.Avalonia.Models;

namespace WingetWizard.Avalonia.Services;

/// <summary>
/// Service for tracking operation progress
/// </summary>
public class ProgressService : IProgressService
{
    public ObservableCollection<OperationProgress> ActiveOperations { get; } = new();

    public async Task<string> StartOperationAsync(string packageName, string operation)
    {
        var operationId = Guid.NewGuid().ToString();
        var progress = new OperationProgress
        {
            OperationId = operationId,
            PackageName = packageName,
            Operation = operation,
            Status = "Starting...",
            StartTime = DateTime.Now
        };

        ActiveOperations.Add(progress);
        await Task.CompletedTask;
        return operationId;
    }

    public async Task UpdateProgressAsync(string operationId, int percentage, string status)
    {
        var operation = ActiveOperations.FirstOrDefault(op => op.OperationId == operationId);
        if (operation != null)
        {
            operation.ProgressPercentage = percentage;
            operation.Status = status;
        }
        await Task.CompletedTask;
    }

    public async Task CompleteOperationAsync(string operationId, bool success, string message = "")
    {
        var operation = ActiveOperations.FirstOrDefault(op => op.OperationId == operationId);
        if (operation != null)
        {
            operation.IsCompleted = true;
            operation.IsError = !success;
            operation.ErrorMessage = success ? string.Empty : message;
            operation.Status = success ? "Completed" : "Failed";
            operation.ProgressPercentage = success ? 100 : 0;
            operation.EndTime = DateTime.Now;
        }
        await Task.CompletedTask;
    }

    public async Task CancelOperationAsync(string operationId)
    {
        var operation = ActiveOperations.FirstOrDefault(op => op.OperationId == operationId);
        if (operation != null)
        {
            operation.IsCompleted = true;
            operation.Status = "Cancelled";
            operation.EndTime = DateTime.Now;
        }
        await Task.CompletedTask;
    }

    public void ClearCompletedOperations()
    {
        var completed = ActiveOperations.Where(op => op.IsCompleted).ToList();
        foreach (var operation in completed)
        {
            ActiveOperations.Remove(operation);
        }
    }
}

