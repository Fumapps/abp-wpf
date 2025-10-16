using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Acme.BookStore.AvaloniaApp.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty]
    private int _counter = 0;

    public DashboardViewModel()
    {
        Title = "Dashboard";
    }

    [RelayCommand]
    private void CounterIncrement()
    {
        Counter++;
    }
}
