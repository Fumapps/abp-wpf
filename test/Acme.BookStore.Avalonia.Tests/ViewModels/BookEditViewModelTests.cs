using System;
using System.Threading.Tasks;
using Acme.BookStore.AvaloniaApp.ViewModels;
using Acme.BookStore.Books;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acme.BookStore.Avalonia.Tests.ViewModels
{
    /// <summary>
    /// Tests for BookEditViewModel following Given_When_Then conventions.
    /// DSL format: "BookName (Type) -> Price EUR @ PublishDate"
    /// Example: "1984 (ScienceFiction) -> 19.99 EUR @ 2024-01-15"
    /// </summary>
    public class BookEditViewModelTests
    {
        private readonly Mock<IBooksAppService> _mockBookService;
        private readonly Mock<ILogger<BookEditViewModel>> _mockLogger;
        private BookEditViewModel _sut;

        public BookEditViewModelTests()
        {
            _mockBookService = new Mock<IBooksAppService>();
            _mockLogger = new Mock<ILogger<BookEditViewModel>>();
            _sut = CreateViewModel();
        }

        #region Initialization Tests

        [Fact]
        public void Given_NoBook_When_Initialize_Then_CreateModeWithDefaults()
        {
            WhenInitializeWithoutBook();
            
            ThenTitleIs("Add New Book");
            ThenNameIsEmpty();
            ThenTypeIs(BookType.Undefined);
            ThenPriceIs(0m);
            ThenIsEditMode(false);
            ThenNoError();
            ThenDialogResultIsFalse();
        }

        [Fact]
        public void Given_ExistingBook_When_Initialize_Then_EditModeWithBookData()
        {
            var book = ParseBookDto("1984 (ScienceFiction) -> 29.99 EUR @ 2024-01-15");
            
            WhenInitializeWithBook(book);
            
            ThenTitleIs("Edit Book");
            ThenNameIs("1984");
            ThenTypeIs(BookType.ScienceFiction);
            ThenPriceIs(29.99m);
            ThenPublishDateIs(new DateTime(2024, 1, 15));
            ThenIsEditMode(true);
            ThenNoError();
        }

        [Fact]
        public void Given_BookWithAllTypes_When_GetBookTypes_Then_ReturnsAllEnumValues()
        {
            WhenGetBookTypes();
            
            ThenContainsTypes(BookType.Undefined, BookType.Adventure, BookType.Biography,
                             BookType.Dystopia, BookType.Fantastic, BookType.Horror,
                             BookType.Science, BookType.ScienceFiction, BookType.Poetry);
        }

        #endregion

        #region Validation Tests

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Given_EmptyOrWhitespaceName_When_Save_Then_ValidationError(string invalidName)
        {
            WithCreateMode();
            WithName(invalidName);
            WithValidPrice();
            
            await WhenSave();
            
            ThenErrorMessageContains("validation errors");
            ThenDialogResultIsFalse();
            ThenServiceNotCalled();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(0.001)]
        [InlineData(10001)]
        public async Task Given_InvalidPrice_When_Save_Then_ValidationError(decimal invalidPrice)
        {
            WithCreateMode();
            WithValidName();
            WithPrice(invalidPrice);
            
            await WhenSave();
            
            ThenErrorMessageIs("Please fix validation errors before saving.");
            ThenDialogResultIsFalse();
            ThenServiceNotCalled();
        }

        [Fact]
        public async Task Given_AllFieldsValid_When_Save_Then_NoValidationError()
        {
            WithCreateMode();
            WithValidName();
            WithValidPrice();
            WithPublishDate(default);  // Default date is actually valid (DateTime.MinValue)
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenNoError();
            ThenDialogResultIsTrue();
            ThenCreateServiceCalled(1);
        }

        [Fact]
        public async Task Given_LongBookName_When_Save_Then_AcceptsValidLongName()
        {
            WithCreateMode();
            WithName("A Very Long Book Title That Should Still Be Accepted Because It Is Valid");
            WithValidPrice();
            WithValidPublishDate();
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenNoError();
            ThenDialogResultIsTrue();
            ThenCreateServiceCalled(1);
        }

        #endregion

        #region Create Mode Tests

        [Fact]
        public async Task Given_ValidBook_When_SaveInCreateMode_Then_CallsCreateService()
        {
            WithCreateMode();
            WithBook("Animal Farm (Dystopia) -> 15.50 EUR @ 2023-08-10");
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenCreateServiceCalledWith("Animal Farm", BookType.Dystopia, 15.50m, new DateTime(2023, 8, 10));
            ThenDialogResultIsTrue();
            ThenNoError();
        }

        [Fact]
        public async Task Given_MinimumValidPrice_When_SaveInCreateMode_Then_Succeeds()
        {
            WithCreateMode();
            WithBook("Cheap Book (Poetry) -> 0.01 EUR @ 2024-01-01");
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenCreateServiceCalledWith("Cheap Book", BookType.Poetry, 0.01m, new DateTime(2024, 1, 1));
            ThenDialogResultIsTrue();
        }

        [Fact]
        public async Task Given_MaximumValidPrice_When_SaveInCreateMode_Then_Succeeds()
        {
            WithCreateMode();
            WithBook("Expensive Book (Science) -> 10000 EUR @ 2024-12-31");
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenCreateServiceCalledWith("Expensive Book", BookType.Science, 10000m, new DateTime(2024, 12, 31));
            ThenDialogResultIsTrue();
        }

        [Fact]
        public async Task Given_CreateInProgress_When_Save_Then_IsSavingFlagSet()
        {
            WithCreateMode();
            WithValidBook();
            var wasSaving = false;
            MockCreateWithCallback(() => wasSaving = _sut.IsSaving);
            
            await WhenSave();
            
            ThenWasSaving(wasSaving);
            ThenNotSavingAnymore();
        }

        #endregion

        #region Edit Mode Tests

        [Fact]
        public async Task Given_ExistingBook_When_UpdateAndSave_Then_CallsUpdateService()
        {
            var bookId = Guid.NewGuid();
            var original = WithExistingBook(bookId, "Original Title (Horror) -> 20.00 EUR @ 2023-05-10");
            WhenInitializeWithBook(original);
            WithName("Updated Title");
            WithPrice(25.99m);
            MockSuccessfulUpdate(bookId);
            
            await WhenSave();
            
            ThenUpdateServiceCalledWith(bookId, "Updated Title", BookType.Horror, 25.99m);
            ThenDialogResultIsTrue();
            ThenNoError();
        }

        [Fact]
        public async Task Given_ExistingBook_When_ChangeOnlyPrice_Then_UpdatesOnlyPrice()
        {
            var bookId = Guid.NewGuid();
            var original = WithExistingBook(bookId, "Same Title (Adventure) -> 30.00 EUR @ 2024-03-20");
            WhenInitializeWithBook(original);
            WithPrice(35.50m);
            MockSuccessfulUpdate(bookId);
            
            await WhenSave();
            
            ThenUpdateServiceCalledWith(bookId, "Same Title", BookType.Adventure, 35.50m);
        }

        [Fact]
        public async Task Given_ExistingBook_When_ChangeTypeAndDate_Then_UpdatesAllFields()
        {
            var bookId = Guid.NewGuid();
            var original = WithExistingBook(bookId, "The Book (Science) -> 40.00 EUR @ 2022-01-01");
            WhenInitializeWithBook(original);
            WithType(BookType.Fantastic);
            WithPublishDate(new DateTime(2024, 06, 15));
            MockSuccessfulUpdate(bookId);
            
            await WhenSave();
            
            ThenUpdateServiceCalledWith(bookId, "The Book", BookType.Fantastic, 40.00m, new DateTime(2024, 06, 15));
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Given_CreateFails_When_Save_Then_ShowsErrorMessage()
        {
            WithCreateMode();
            WithValidBook();
            MockCreateFailure("Database connection failed");
            
            await WhenSave();
            
            ThenErrorMessageContains("Database connection failed");
            ThenDialogResultIsFalse();
        }

        [Fact]
        public async Task Given_UpdateFails_When_Save_Then_ShowsErrorMessage()
        {
            var bookId = Guid.NewGuid();
            var book = WithExistingBook(bookId, "Book (Biography) -> 18.00 EUR @ 2024-02-14");
            WhenInitializeWithBook(book);
            MockUpdateFailure(bookId, "Concurrent update detected");
            
            await WhenSave();
            
            ThenErrorMessageContains("Concurrent update detected");
            ThenDialogResultIsFalse();
        }

        [Fact]
        public async Task Given_NetworkTimeout_When_Save_Then_ShowsTimeoutError()
        {
            WithCreateMode();
            WithValidBook();
            MockCreateFailure("The operation has timed out");
            
            await WhenSave();
            
            ThenErrorMessageContains("timed out");
        }

        [Fact]
        public async Task Given_PreviousError_When_ValidSave_Then_ClearsError()
        {
            WithCreateMode();
            WithInvalidName();
            await WhenSave();
            ThenErrorMessageContains("validation");
            
            WithValidName();
            WithValidPrice();
            WithValidPublishDate();
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenNoError();
            ThenDialogResultIsTrue();
        }

        #endregion

        #region Command Tests

        [Fact]
        public void Given_DialogOpen_When_Cancel_Then_ClosesDialog()
        {
            WhenCancel();
            
            ThenDialogResultIsFalse();  // Cancel sets false, View.axaml.cs closes on any DialogResult change
        }

        [Fact]
        public async Task Given_ValidationError_When_TrySave_Then_SaveCommandExecutes()
        {
            WithCreateMode();
            WithInvalidName();
            
            var canExecute = _sut.SaveCommand.CanExecute(null);
            await WhenSave();
            
            canExecute.Should().BeTrue("SaveCommand should be executable even with validation errors");
            ThenErrorMessageContains("validation");
        }

        [Fact]
        public void Given_Initialized_When_CheckCancelCommand_Then_AlwaysExecutable()
        {
            WithCreateMode();
            
            var canExecute = _sut.CancelCommand.CanExecute(null);
            
            canExecute.Should().BeTrue("CancelCommand should always be executable");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Given_SpecialCharactersInName_When_Save_Then_Succeeds()
        {
            WithCreateMode();
            WithName("Book: The \"Quote\" & Special <Chars>");
            WithValidPrice();
            WithValidPublishDate();
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenNoError();
            ThenDialogResultIsTrue();
        }

        [Fact]
        public async Task Given_FuturePublishDate_When_Save_Then_Succeeds()
        {
            WithCreateMode();
            WithValidName();
            WithValidPrice();
            WithPublishDate(DateTime.Now.AddYears(1));
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenNoError();
            ThenDialogResultIsTrue();
        }

        [Fact]
        public async Task Given_VeryOldPublishDate_When_Save_Then_Succeeds()
        {
            WithCreateMode();
            WithValidName();
            WithValidPrice();
            WithPublishDate(new DateTime(1800, 1, 1));
            MockSuccessfulCreate();
            
            await WhenSave();
            
            ThenNoError();
        }

        #endregion

        #region Helper Methods - Setup (With...)

        private BookEditViewModel CreateViewModel() =>
            new(_mockBookService.Object, _mockLogger.Object);

        private void WithCreateMode()
        {
            _sut.Initialize();
        }

        private void WithName(string name)
        {
            _sut.Name = name ?? "";
        }

        private void WithValidName()
        {
            _sut.Name = "Test Book";
        }

        private void WithInvalidName()
        {
            _sut.Name = "";
        }

        private void WithType(BookType type)
        {
            _sut.SelectedType = type;
        }

        private void WithPrice(decimal price)
        {
            _sut.Price = price;
        }

        private void WithValidPrice()
        {
            _sut.Price = 19.99m;
        }

        private void WithPublishDate(DateTime date)
        {
            _sut.PublishDate = date;
        }

        private void WithValidPublishDate()
        {
            _sut.PublishDate = new DateTime(2024, 1, 1);
        }

        private void WithValidBook()
        {
            WithValidName();
            WithType(BookType.Adventure);
            WithValidPrice();
            WithValidPublishDate();
        }

        /// <summary>
        /// DSL Parser: "BookName (Type) -> Price EUR @ PublishDate"
        /// Example: "1984 (ScienceFiction) -> 19.99 EUR @ 2024-01-15"
        /// </summary>
        private void WithBook(string dsl)
        {
            var book = ParseBook(dsl);
            _sut.Name = book.Name;
            _sut.SelectedType = book.Type;
            _sut.Price = (decimal)book.Price;
            _sut.PublishDate = book.PublishDate;
        }

        private BookDto ParseBookDto(string dsl) => ParseBook(dsl);

        private BookDto WithExistingBook(Guid id, string dsl)
        {
            var book = ParseBook(dsl);
            book.Id = id;
            return book;
        }

        /// <summary>
        /// Parses DSL format: "BookName (Type) -> Price EUR @ PublishDate"
        /// Tolerant to whitespace, defaults: Type=Undefined, Price=0, Date=today
        /// </summary>
        private BookDto ParseBook(string dsl)
        {
            try
            {
                // Parse: "BookName (Type) -> Price EUR @ Date"
                var partsStep1 = dsl.Split(new[] { '(' }, 2);
                var name = partsStep1[0].Trim();
                
                var partsStep2 = partsStep1[1].Split(new[] { ')' }, 2);
                var typeStr = partsStep2[0].Trim();
                
                var partsStep3 = partsStep2[1].Split(new[] { "->" }, StringSplitOptions.None);
                var priceAndDate = partsStep3.Length > 1 ? partsStep3[1] : "";
                
                var partsStep4 = priceAndDate.Split(new[] { "EUR" }, StringSplitOptions.None);
                var priceStr = partsStep4[0].Trim();
                
                var partsStep5 = partsStep4.Length > 1 ? partsStep4[1].Split(new[] { '@' }) : new[] { "", DateTime.Now.ToString("yyyy-MM-dd") };
                var dateStr = partsStep5.Length > 1 ? partsStep5[1].Trim() : DateTime.Now.ToString("yyyy-MM-dd");

                return new BookDto
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Type = Enum.Parse<BookType>(typeStr),
                    Price = float.Parse(priceStr, System.Globalization.CultureInfo.InvariantCulture),
                    PublishDate = DateTime.Parse(dateStr)
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid book DSL: '{dsl}'. Expected format: 'Name (Type) -> Price EUR @ YYYY-MM-DD'", ex);
            }
        }

        #endregion

        #region Helper Methods - Actions (When...)

        private void WhenInitializeWithoutBook()
        {
            _sut.Initialize();
        }

        private void WhenInitializeWithBook(BookDto book)
        {
            _sut.Initialize(book);
        }

        private void WhenGetBookTypes()
        {
            // Just accessing the property
            _ = _sut.BookTypes;
        }

        private async Task WhenSave()
        {
            await _sut.SaveCommand.ExecuteAsync(null);
        }

        private void WhenCancel()
        {
            _sut.CancelCommand.Execute(null);
        }

        #endregion

        #region Helper Methods - Mocks

        private void MockSuccessfulCreate()
        {
            _mockBookService
                .Setup(s => s.CreateAsync(It.IsAny<BookCreateDto>()))
                .ReturnsAsync((BookCreateDto dto) => new BookDto
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Type = dto.Type,
                    Price = dto.Price,
                    PublishDate = dto.PublishDate
                });
        }

        private void MockSuccessfulUpdate(Guid bookId)
        {
            _mockBookService
                .Setup(s => s.UpdateAsync(bookId, It.IsAny<BookUpdateDto>()))
                .ReturnsAsync((Guid id, BookUpdateDto dto) => new BookDto
                {
                    Id = id,
                    Name = dto.Name,
                    Type = dto.Type,
                    Price = dto.Price,
                    PublishDate = dto.PublishDate
                });
        }

        private void MockCreateWithCallback(Action callback)
        {
            _mockBookService
                .Setup(s => s.CreateAsync(It.IsAny<BookCreateDto>()))
                .Returns(async () =>
                {
                    callback();
                    await Task.Delay(10);
                    return new BookDto { Id = Guid.NewGuid() };
                });
        }

        private void MockCreateFailure(string errorMessage)
        {
            _mockBookService
                .Setup(s => s.CreateAsync(It.IsAny<BookCreateDto>()))
                .ThrowsAsync(new Exception(errorMessage));
        }

        private void MockUpdateFailure(Guid bookId, string errorMessage)
        {
            _mockBookService
                .Setup(s => s.UpdateAsync(bookId, It.IsAny<BookUpdateDto>()))
                .ThrowsAsync(new Exception(errorMessage));
        }

        #endregion

        #region Helper Methods - Assertions (Then...)

        private void ThenTitleIs(string expected)
        {
            _sut.Title.Should().Be(expected);
        }

        private void ThenNameIs(string expected)
        {
            _sut.Name.Should().Be(expected);
        }

        private void ThenNameIsEmpty()
        {
            _sut.Name.Should().BeEmpty();
        }

        private void ThenTypeIs(BookType expected)
        {
            _sut.SelectedType.Should().Be(expected);
        }

        private void ThenPriceIs(decimal expected)
        {
            _sut.Price.Should().BeApproximately(expected, 0.001m);
        }

        private void ThenPublishDateIs(DateTime expected)
        {
            _sut.PublishDate.Should().Be(expected);
        }

        private void ThenIsEditMode(bool expected)
        {
            _sut.IsEditMode.Should().Be(expected);
        }

        private void ThenNoError()
        {
            _sut.ErrorMessage.Should().BeNullOrEmpty();
        }

        private void ThenErrorMessageIs(string expected)
        {
            _sut.ErrorMessage.Should().Be(expected);
        }

        private void ThenErrorMessageContains(string expected)
        {
            _sut.ErrorMessage.Should().Contain(expected);
        }

        private void ThenDialogResultIsTrue()
        {
            _sut.DialogResult.Should().BeTrue();
        }

        private void ThenDialogResultIsFalse()
        {
            _sut.DialogResult.Should().BeFalse();
        }

        private void ThenContainsTypes(params BookType[] types)
        {
            var bookTypes = _sut.BookTypes;
            foreach (var type in types)
            {
                bookTypes.Should().Contain(type);
            }
            bookTypes.Count.Should().Be(Enum.GetValues(typeof(BookType)).Length);
        }

        private void ThenServiceNotCalled()
        {
            _mockBookService.Verify(s => s.CreateAsync(It.IsAny<BookCreateDto>()), Times.Never);
            _mockBookService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<BookUpdateDto>()), Times.Never);
        }

        private void ThenCreateServiceCalled(int times)
        {
            _mockBookService.Verify(s => s.CreateAsync(It.IsAny<BookCreateDto>()), Times.Exactly(times));
        }

        private void ThenCreateServiceCalledWith(string name, BookType type, decimal price, DateTime publishDate)
        {
            _mockBookService.Verify(s => s.CreateAsync(It.Is<BookCreateDto>(dto =>
                dto.Name == name &&
                dto.Type == type &&
                Math.Abs(dto.Price - (float)price) < 0.01f &&
                dto.PublishDate == publishDate
            )), Times.Once);
        }

        private void ThenUpdateServiceCalledWith(Guid bookId, string name, BookType type, decimal price, DateTime? publishDate = null)
        {
            _mockBookService.Verify(s => s.UpdateAsync(bookId, It.Is<BookUpdateDto>(dto =>
                dto.Name == name &&
                dto.Type == type &&
                Math.Abs(dto.Price - (float)price) < 0.01f &&
                (publishDate == null || dto.PublishDate == publishDate.Value)
            )), Times.Once);
        }

        private void ThenWasSaving(bool wasSaving)
        {
            wasSaving.Should().BeTrue("IsSaving should be true during save operation");
        }

        private void ThenNotSavingAnymore()
        {
            _sut.IsSaving.Should().BeFalse("IsSaving should be false after save completes");
        }

        #endregion
    }
}
