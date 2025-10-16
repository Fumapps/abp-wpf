using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Acme.BookStore.Books;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Acme.BookStore.AvaloniaApp.ViewModels
{
    public partial class BookEditViewModel : ObservableValidator
    {
        private readonly IBooksAppService _bookAppService;
        private readonly ILogger<BookEditViewModel>? _logger;
        private Guid? _bookId;

        [ObservableProperty]
        private string _title = "Add New Book";

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Book name is required")]
        private string _name = string.Empty;

        [ObservableProperty]
        private BookType _selectedType = BookType.Undefined;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Publish date is required")]
        private DateTime _publishDate = DateTime.Now;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10000")]
        private decimal _price = 0;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _dialogResult;

        // For testing without DI
        public BookEditViewModel() : this(null!, null)
        {
        }

        public BookEditViewModel(IBooksAppService bookAppService, ILogger<BookEditViewModel>? logger)
        {
            _bookAppService = bookAppService;
            _logger = logger;
            Title = "Add New Book";
        }

        public List<BookType> BookTypes => Enum.GetValues(typeof(BookType)).Cast<BookType>().ToList();

        public bool IsEditMode => _bookId.HasValue;

        public void Initialize(BookDto? book = null)
        {
            if (book != null)
            {
                _bookId = book.Id;
                Name = book.Name;
                SelectedType = book.Type;
                PublishDate = book.PublishDate;
                Price = (decimal)book.Price;
                Title = "Edit Book";
            }
            else
            {
                _bookId = null;
                Name = string.Empty;
                SelectedType = BookType.Undefined;
                PublishDate = DateTime.Now;
                Price = 0;
                Title = "Add New Book";
            }

            ErrorMessage = null;
            DialogResult = false;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            // Validate
            ValidateAllProperties();
            if (HasErrors)
            {
                ErrorMessage = "Please fix validation errors before saving.";
                return;
            }

            IsSaving = true;
            ErrorMessage = null;

            try
            {
                if (IsEditMode)
                {
                    var updateDto = new BookUpdateDto
                    {
                        Name = Name,
                        Type = SelectedType,
                        PublishDate = PublishDate,
                        Price = (float)Price
                    };

                    await _bookAppService.UpdateAsync(_bookId!.Value, updateDto);
                    _logger?.LogInformation($"Book '{Name}' updated successfully");
                }
                else
                {
                    var createDto = new BookCreateDto
                    {
                        Name = Name,
                        Type = SelectedType,
                        PublishDate = PublishDate,
                        Price = (float)Price
                    };

                    await _bookAppService.CreateAsync(createDto);
                    _logger?.LogInformation($"Book '{Name}' created successfully");
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving book");
                ErrorMessage = $"Error saving book: {ex.Message}";
                DialogResult = false;
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
        }
    }
}
