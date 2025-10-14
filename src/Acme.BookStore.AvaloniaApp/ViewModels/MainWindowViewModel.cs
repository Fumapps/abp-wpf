using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.BookStore.AvaloniaApp.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        [ObservableProperty]
        private object? _currentView;

        public MainWindowViewModel()
        {
            Title = "Acme BookStore";
            // Start with Books view
            NavigateToBooksCommand.Execute(null);
        }

        [RelayCommand]
        private void NavigateToBooks()
        {
            if (App.Services != null)
            {
                var viewModel = App.Services.GetRequiredService<BookIndexViewModel>();
                CurrentView = new Views.BookIndexView { DataContext = viewModel };
                // Initialize the view model
                viewModel.InitialCommand?.Execute(null);
            }
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            if (App.Services != null)
            {
                var viewModel = App.Services.GetRequiredService<DashboardViewModel>();
                CurrentView = new Views.DashboardView { DataContext = viewModel };
            }
        }

        [RelayCommand]
        private void NavigateToData()
        {
            if (App.Services != null)
            {
                var viewModel = App.Services.GetRequiredService<DataViewModel>();
                CurrentView = new Views.DataView { DataContext = viewModel };
            }
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            if (App.Services != null)
            {
                var viewModel = App.Services.GetRequiredService<SettingsViewModel>();
                CurrentView = new Views.SettingsView { DataContext = viewModel };
            }
        }
    }
}
