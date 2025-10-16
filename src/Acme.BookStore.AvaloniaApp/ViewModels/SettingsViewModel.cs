using System;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Acme.BookStore.AvaloniaApp.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private bool _isInitialized = false;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private ThemeVariant _currentTheme = ThemeVariant.Default;

    public SettingsViewModel()
    {
        Title = "Settings";
        InitializeViewModel();
    }

    private void InitializeViewModel()
    {
        if (_isInitialized)
            return;

        // Try to get current theme from Application
        if (Avalonia.Application.Current != null)
        {
            CurrentTheme = Avalonia.Application.Current.ActualThemeVariant;
        }

        AppVersion = $"Acme.BookStore.AvaloniaApp - {GetAssemblyVersion()}";
        _isInitialized = true;
    }

    private string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    }

    [RelayCommand]
    private void ChangeTheme(string? parameter)
    {
        if (Avalonia.Application.Current == null)
            return;

        switch (parameter)
        {
            case "theme_light":
                if (CurrentTheme == ThemeVariant.Light)
                    break;

                Avalonia.Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                CurrentTheme = ThemeVariant.Light;
                break;

            case "theme_dark":
                if (CurrentTheme == ThemeVariant.Dark)
                    break;

                Avalonia.Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                CurrentTheme = ThemeVariant.Dark;
                break;

            default:
                Avalonia.Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                CurrentTheme = ThemeVariant.Default;
                break;
        }
    }

    public bool IsLightTheme => CurrentTheme == ThemeVariant.Light;
    public bool IsDarkTheme => CurrentTheme == ThemeVariant.Dark;
}
