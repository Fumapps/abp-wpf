using System;
using System.Collections.Generic;
using Acme.BookStore.AvaloniaApp.Models;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Acme.BookStore.AvaloniaApp.ViewModels;

public partial class DataViewModel : BaseViewModel
{
    private bool _isInitialized = false;

    [ObservableProperty]
    private IEnumerable<DataColor> _colors = Array.Empty<DataColor>();

    public DataViewModel()
    {
        Title = "Data";
        InitializeViewModel();
    }

    private void InitializeViewModel()
    {
        if (_isInitialized)
            return;

        var random = new Random();
        var colorCollection = new List<DataColor>();

        for (int i = 0; i < 8192; i++)
        {
            colorCollection.Add(new DataColor
            {
                Color = new SolidColorBrush(Color.FromArgb(
                    200,
                    (byte)random.Next(0, 250),
                    (byte)random.Next(0, 250),
                    (byte)random.Next(0, 250)))
            });
        }

        Colors = colorCollection;
        _isInitialized = true;
    }
}
