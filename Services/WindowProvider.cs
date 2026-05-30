using Avalonia.Controls;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>提供当前活动窗口引用的持有者，用于 DialogService 获取窗口实例。</para>
/// Holds a reference to the current active window for DialogService to obtain the window instance.
/// </summary>
public sealed class WindowProvider
{
    /// <summary>
    /// <para>获取或设置当前活动窗口。</para>
    /// Gets or sets the current active window.
    /// </summary>
    public Window? Window { get; set; }
}
