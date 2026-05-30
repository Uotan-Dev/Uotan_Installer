using Avalonia.Controls;
using UotanInstaller.App.ViewModels;

namespace UotanInstaller.App.Views;

/// <summary>
/// <para>主窗口代码后置，提供窗口初始化和平台特定功能。</para>
/// Main window code-behind that provides window initialization and platform-specific features.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// <para>初始化 MainWindow 的新实例。</para>
    /// Initializes a new instance of MainWindow.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            e.Cancel = true;
            var shouldClose = await vm.HandleClosingAsync();
            if (shouldClose)
            {
                Closing -= OnWindowClosing;
                await vm.CleanupBeforeExitAsync();
                Close();
            }
        }
    }
}
