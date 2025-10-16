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
    public class BookEditViewModelTests
    {
        private readonly Mock<IBooksAppService> _mockBookService;
        private readonly Mock<ILogger<BookEditViewModel>> _mockLogger;
        private readonly BookEditViewModel _viewModel;

        public BookEditViewModelTests()
        {
            _mockBookService = new Mock<IBooksAppService>();
            _mockLogger = new Mock<ILogger<BookEditViewModel>>();
            _viewModel = new BookEditViewModel(_mockBookService.Object, _mockLogger.Object);
        }

        [Fact]
        public void Initialize_WithoutBook_SetsDefaultValues()
        {
            // Act
            _viewModel.Initialize();

            // Assert
            _viewModel.Title.Should().Be("Add New Book");
            _viewModel.Name.Should().BeEmpty();
            _viewModel.SelectedType.Should().Be(BookType.Undefined);
            _viewModel.Price.Should().Be(0);
            _viewModel.IsEditMode.Should().BeFalse();
            _viewModel.ErrorMessage.Should().BeNull();
            _viewModel.DialogResult.Should().BeFalse();
        }

        [Fact]
        public void Initialize_WithBook_SetsBookValues()
        {
            // Arrange
            var book = new BookDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Book",
                Type = BookType.ScienceFiction,
                PublishDate = new DateTime(2024, 1, 15),
                Price = 29.99f
            };

            // Act
            _viewModel.Initialize(book);

            // Assert
            _viewModel.Title.Should().Be("Edit Book");
            _viewModel.Name.Should().Be("Test Book");
            _viewModel.SelectedType.Should().Be(BookType.ScienceFiction);
            _viewModel.PublishDate.Should().Be(new DateTime(2024, 1, 15));
            _viewModel.Price.Should().Be(29.99m);
            _viewModel.IsEditMode.Should().BeTrue();
            _viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void BookTypes_ReturnsAllEnumValues()
        {
            // Act
            var bookTypes = _viewModel.BookTypes;

            // Assert
            bookTypes.Should().Contain(BookType.Undefined);
            bookTypes.Should().Contain(BookType.Adventure);
            bookTypes.Should().Contain(BookType.ScienceFiction);
            bookTypes.Should().Contain(BookType.Horror);
            bookTypes.Count.Should().Be(Enum.GetValues(typeof(BookType)).Length);
        }

        [Fact]
        public async Task SaveAsync_WithEmptyName_SetsValidationError()
        {
            // Arrange
            _viewModel.Initialize();
            _viewModel.Name = "";

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            _viewModel.ErrorMessage.Should().Be("Please fix validation errors before saving.");
            _viewModel.DialogResult.Should().BeFalse();
            _mockBookService.Verify(s => s.CreateAsync(It.IsAny<BookCreateDto>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_CreateMode_CallsCreateAsync()
        {
            // Arrange
            _viewModel.Initialize();
            _viewModel.Name = "New Test Book";
            _viewModel.SelectedType = BookType.Adventure;
            _viewModel.PublishDate = new DateTime(2024, 10, 15);
            _viewModel.Price = 19.99m;

            var createdBook = new BookDto
            {
                Id = Guid.NewGuid(),
                Name = "New Test Book",
                Type = BookType.Adventure,
                PublishDate = new DateTime(2024, 10, 15),
                Price = 19.99f
            };

            _mockBookService
                .Setup(s => s.CreateAsync(It.IsAny<BookCreateDto>()))
                .ReturnsAsync(createdBook);

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            _mockBookService.Verify(s => s.CreateAsync(It.Is<BookCreateDto>(dto =>
                dto.Name == "New Test Book" &&
                dto.Type == BookType.Adventure &&
                dto.PublishDate == new DateTime(2024, 10, 15) &&
                dto.Price == 19.99f
            )), Times.Once);

            _viewModel.DialogResult.Should().BeTrue();
            _viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task SaveAsync_EditMode_CallsUpdateAsync()
        {
            // Arrange
            var bookId = Guid.NewGuid();
            var existingBook = new BookDto
            {
                Id = bookId,
                Name = "Original Book",
                Type = BookType.Horror,
                PublishDate = new DateTime(2023, 5, 10),
                Price = 15.50f
            };

            _viewModel.Initialize(existingBook);
            _viewModel.Name = "Updated Book";
            _viewModel.Price = 25.99m;

            var updatedBook = new BookDto
            {
                Id = bookId,
                Name = "Updated Book",
                Type = BookType.Horror,
                PublishDate = new DateTime(2023, 5, 10),
                Price = 25.99f
            };

            _mockBookService
                .Setup(s => s.UpdateAsync(bookId, It.IsAny<BookUpdateDto>()))
                .ReturnsAsync(updatedBook);

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            _mockBookService.Verify(s => s.UpdateAsync(bookId, It.Is<BookUpdateDto>(dto =>
                dto.Name == "Updated Book" &&
                dto.Type == BookType.Horror &&
                dto.Price == 25.99f
            )), Times.Once);

            _viewModel.DialogResult.Should().BeTrue();
            _viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task SaveAsync_ServiceThrowsException_SetsErrorMessage()
        {
            // Arrange
            _viewModel.Initialize();
            _viewModel.Name = "Test Book";
            _viewModel.SelectedType = BookType.Biography;
            _viewModel.Price = 12.50m;

            _mockBookService
                .Setup(s => s.CreateAsync(It.IsAny<BookCreateDto>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            _viewModel.ErrorMessage.Should().Contain("Database connection failed");
            _viewModel.DialogResult.Should().BeFalse();
        }

        [Fact]
        public async Task SaveAsync_SetsIsSavingDuringExecution()
        {
            // Arrange
            _viewModel.Initialize();
            _viewModel.Name = "Test Book";
            _viewModel.Price = 10m;

            bool wasSaving = false;
            _mockBookService
                .Setup(s => s.CreateAsync(It.IsAny<BookCreateDto>()))
                .Returns(async () =>
                {
                    wasSaving = _viewModel.IsSaving;
                    await Task.Delay(10);
                    return new BookDto { Id = Guid.NewGuid(), Name = "Test Book" };
                });

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            wasSaving.Should().BeTrue("IsSaving should be true during execution");
            _viewModel.IsSaving.Should().BeFalse("IsSaving should be false after execution");
        }

        [Fact]
        public void Cancel_SetDialogResultToFalse()
        {
            // Act
            _viewModel.CancelCommand.Execute(null);

            // Assert
            _viewModel.DialogResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10001)]
        public async Task SaveAsync_WithInvalidPrice_SetsValidationError(decimal invalidPrice)
        {
            // Arrange
            _viewModel.Initialize();
            _viewModel.Name = "Test Book";
            _viewModel.Price = invalidPrice;

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            _viewModel.ErrorMessage.Should().Be("Please fix validation errors before saving.");
            _viewModel.DialogResult.Should().BeFalse();
            _mockBookService.Verify(s => s.CreateAsync(It.IsAny<BookCreateDto>()), Times.Never);
        }
    }
}
