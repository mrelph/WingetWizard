using CommunityToolkit.Mvvm.ComponentModel;

namespace WingetWizard.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the main window and navigation
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = "WingetWizard - AI-Enhanced Package Manager";

    public MainViewModel(IServiceProvider services) : base(services)
    {
    }
}


