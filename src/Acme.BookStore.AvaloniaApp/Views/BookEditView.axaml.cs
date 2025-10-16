using Acme.BookStore.AvaloniaApp.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Acme.BookStore.AvaloniaApp.Views
{
    public partial class BookEditView : Window
    {
        public BookEditView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public BookEditView(BookEditViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // Listen to DialogResult changes to close the window
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.DialogResult) && viewModel.DialogResult)
                {
                    Close();
                }
            };
        }
    }
}
