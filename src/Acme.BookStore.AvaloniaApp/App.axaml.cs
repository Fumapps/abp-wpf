using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Acme.BookStore.AvaloniaApp.ViewModels;
using Acme.BookStore.AvaloniaApp.Views;
using Acme.BookStore.Books;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Volo.Abp;

namespace Acme.BookStore.AvaloniaApp;

public partial class App : Application
{
    private IHost? _host;
    private IAbpApplicationWithInternalServiceProvider? _abpApplication;
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Initialize logging
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Async(c => c.File("Logs/logs.txt"))
                .CreateLogger();

            try
            {
                Log.Information("Starting Avalonia host.");

                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Create and initialize ABP application
                _abpApplication = await AbpApplicationFactory.CreateAsync<BookStoreAvaloniaModule>(options =>
                {
                    options.Services.ReplaceConfiguration(configuration);
                    options.UseAutofac();
                    options.Services.AddLogging(c => c.AddSerilog(dispose: true));
                    
                    // Register Views and ViewModels
                    options.Services.AddSingleton<MainWindow>();
                    options.Services.AddTransient<MainWindowViewModel>();
                    options.Services.AddTransient<DashboardViewModel>();
                    options.Services.AddTransient<DataViewModel>();
                    options.Services.AddTransient<SettingsViewModel>();
                    options.Services.AddTransient<BookIndexViewModel>();
                    options.Services.AddTransient<BookEditViewModel>();
                    
                    // Register application services
                    options.Services.AddSingleton<IBooksAppService, BooksAppService>();
                });

                await _abpApplication.InitializeAsync();
                Services = _abpApplication.ServiceProvider;

                // Create main window with DI
                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();
                
                desktop.MainWindow = mainWindow;
                
                desktop.Exit += OnExit;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_abpApplication != null)
        {
            await _abpApplication.ShutdownAsync();
            _abpApplication.Dispose();
        }
        
        _host?.Dispose();
        Log.CloseAndFlush();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}