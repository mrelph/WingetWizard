using CommunityToolkit.Mvvm.ComponentModel;

namespace WingetWizard.Avalonia.ViewModels;

/// <summary>
/// Base class for all ViewModels providing common functionality
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    protected readonly IServiceProvider _services;

    protected ViewModelBase(IServiceProvider services)
    {
        _services = services;
    }
}


