using Avalonia;
using System.Runtime.InteropServices;

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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, int type);

    private static void ShowFatalError(string message)
    {
        MessageBox(IntPtr.Zero, message, "柚坛工具箱部署器 - 致命错误", 0x10);
    }
}
