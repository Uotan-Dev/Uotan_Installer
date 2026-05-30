using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using UotanInstaller.App.Services;
using UotanInstaller.App.ViewModels;
using UotanInstaller.App.Views;

namespace UotanInstaller.App;

/// <summary>
/// <para>应用程序入口类，负责 DI 容器配置和主窗口创建。</para>
/// Application entry class responsible for DI container configuration and main window creation.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public IServiceProvider? Services => _serviceProvider;

    /// <summary>
    /// <para>初始化应用程序的 XAML 资源。</para>
    /// Initializes the application's XAML resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// <para>在框架初始化完成后创建主窗口并设置 DataContext。</para>
    /// Creates the main window and sets the DataContext after framework initialization completes.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = ConfigureServices();

            var mainWindow = new MainWindow();

            var windowProvider = services.GetRequiredService<WindowProvider>();
            windowProvider.Window = mainWindow;

            var viewModel = services.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = viewModel;

            desktop.MainWindow = mainWindow;

            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _ = viewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception is null) return;

        await ShowExceptionDialogAsync(exception);
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        var exception = e.Exception?.InnerException ?? e.Exception;
        if (exception is null) return;

        await ShowExceptionDialogAsync(exception);
    }

    private async Task ShowExceptionDialogAsync(Exception exception)
    {
        var message = exception.Message;
        if (exception.InnerException is not null)
        {
            message += $"\n\n内部异常: {exception.InnerException.Message}";
        }

        try
        {
            if (_serviceProvider is not null)
            {
                var dialogService = _serviceProvider.GetService<IDialogService>();
                if (dialogService is not null)
                {
                    await dialogService.ShowErrorAsync("未处理的异常", message);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// <para>配置依赖注入服务容器。</para>
    /// Configures the dependency injection service container.
    /// </summary>
    /// <returns>
    /// <para>配置完成的 ServiceProvider 实例。</para>
    /// The configured ServiceProvider instance.
    /// </returns>
    private ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<WindowProvider>();
        services.AddUotanServices();

        _serviceProvider = services.BuildServiceProvider();
        return _serviceProvider;
    }
}
