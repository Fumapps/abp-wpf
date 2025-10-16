using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Acme.BookStore.Books;
using Acme.BookStore.AvaloniaApp.Views;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Acme.BookStore.AvaloniaApp.ViewModels;

public partial class BookIndexViewModel : BaseViewModel
{
    private readonly IBooksAppService _bookAppService;
    private readonly ILogger<BookIndexViewModel>? _logger;

    [ObservableProperty]
    private ObservableCollection<BookDto> _books = new();

    public BookIndexViewModel()
    {
        Title = "Books";
    }

    public BookIndexViewModel(ILogger<BookIndexViewModel> logger, IBooksAppService bookAppService)
    {
        _logger = logger;
        _bookAppService = bookAppService;
        Title = "Books";
    }

    [RelayCommand]
    public async Task InitialAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            Books.Clear();

            var pagedResults = await _bookAppService.GetListAsync(new GetBooksInput());

            foreach (var bookDetails in pagedResults.Items)
            {
                Books.Add(bookDetails);
            }
            
            _logger?.LogInformation($"Found {Books.Count} books.");
        }
        catch (System.Exception ex)
        {
            _logger?.LogError(ex, "Error loading books");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateBookAsync()
    {
        try
        {
            var viewModel = App.Services?.GetRequiredService<BookEditViewModel>();
            if (viewModel == null) return;

            viewModel.Initialize();

            var dialog = new BookEditView(viewModel);
            await ShowDialog(dialog);
            
            if (viewModel.DialogResult)
            {
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening create book dialog");
        }
    }

    [RelayCommand]
    private async Task EditBookAsync(BookDto book)
    {
        try
        {
            var viewModel = App.Services?.GetRequiredService<BookEditViewModel>();
            if (viewModel == null) return;

            viewModel.Initialize(book);

            var dialog = new BookEditView(viewModel);
            await ShowDialog(dialog);
            
            if (viewModel.DialogResult)
            {
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening edit book dialog");
        }
    }

    [RelayCommand]
    private async Task DeleteBookAsync(BookDto book)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            var result = await ShowConfirmationDialog(
                mainWindow,
                "Delete Book",
                $"Are you sure you want to delete '{book.Name}'?");

            if (result)
            {
                IsBusy = true;
                await _bookAppService.DeleteAsync(book.Id);
                _logger?.LogInformation($"Book '{book.Name}' deleted successfully");
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting book");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private async Task ShowDialog(Window dialog)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow != null)
        {
            await dialog.ShowDialog(mainWindow);
        }
    }

    private async Task<bool> ShowConfirmationDialog(Window owner, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var result = false;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) => dialog.Close();

        var deleteButton = new Button { Content = "Delete", Width = 100 };
        deleteButton.Click += (s, e) =>
        {
            result = true;
            dialog.Close();
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(deleteButton);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }
}
