using Avalonia.Controls;
using Avalonia.Controls.Templates;
using UotanInstaller.App.ViewModels;
using UotanInstaller.App.Views;

namespace UotanInstaller.App;

/// <summary>
/// <para>视图定位器，将 ViewModel 映射到对应的 View。</para>
/// View locator that maps ViewModels to their corresponding Views.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is MainWindowViewModel)
        {
            return new MainWindow();
        }

        return new TextBlock { Text = "Not Found: " + param?.GetType().Name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
