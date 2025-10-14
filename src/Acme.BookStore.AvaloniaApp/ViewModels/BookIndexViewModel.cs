using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Acme.BookStore.Books;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
}
