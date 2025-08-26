using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using WingetWizard.Avalonia.ViewModels;

namespace WingetWizard.Avalonia.Views
{
    public partial class SearchDialog : Window
    {
        public SearchDialog()
        {
            InitializeComponent();
            
            // Set up the DataContext using dependency injection
            DataContext = App.Services.GetRequiredService<SearchDialogViewModel>();
            
            // Handle Enter key in search box
            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (searchBox != null)
            {
                searchBox.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        var viewModel = DataContext as SearchDialogViewModel;
                        viewModel?.SearchCommand.Execute(null);
                    }
                };
            }
        }
    }
}
