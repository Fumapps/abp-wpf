using Acme.BookStore.AvaloniaApp.ViewModels;
using Avalonia.Controls;

namespace Acme.BookStore.AvaloniaApp.Views;

public partial class BookIndexView : UserControl
{
    public BookIndexView()
    {
        InitializeComponent();
        
        // Auto-load books when view is loaded
        this.Loaded += (s, e) =>
        {
            if (DataContext is BookIndexViewModel viewModel)
            {
                _ = viewModel.InitialCommand?.ExecuteAsync(null);
            }
        };
    }
}
