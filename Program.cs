using Avalonia;

namespace UotanInstaller.App;

/// <summary>
/// <para>应用程序启动入口，配置 Avalonia 构建器和 Windows 特定选项。</para>
/// Application startup entry point that configures the Avalonia builder and Windows-specific options.
/// </summary>
sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var message = e.ExceptionObject is Exception ex
                ? $"{ex.Message}\n\n{ex.StackTrace}"
                : e.ExceptionObject?.ToString() ?? "Unknown error";

            ShowFatalError($"Unhandled Exception\n\n{message}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            var message = e.Exception?.InnerException?.Message ?? e.Exception?.Message ?? "Unknown task error";
            ShowFatalError($"Unobserved Task Exception\n\n{message}");
            e.SetObserved();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            if (ex.InnerException is not null)
            {
                message += $"\n\nInner Exception: {ex.InnerException.Message}";
            }

            message += $"\n\n{ex.StackTrace}";
            ShowFatalError($"Application Crashed\n\n{message}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// <para>将致命错误信息写入标准错误流并以非零退出码终止进程。此方法可在任何平台上运行，不依赖任何平台特定的原生 API。</para>
    /// Writes a fatal error message to the standard error stream and terminates the process with a non-zero exit code. This method runs on any platform without relying on platform-specific native APIs.
    /// </summary>
    /// <param name="message">
    /// <para>要显示的错误信息。</para>
    /// The error message to display.
    /// </param>
    private static void ShowFatalError(string message)
    {
        Console.Error.WriteLine("=== 柚坛工具箱部署器 - 致命错误 ===");
        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }
}
