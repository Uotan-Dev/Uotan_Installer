using Avalonia.Controls;
using Avalonia.Platform.Storage;
using UotanInstaller.App.Views;

namespace UotanInstaller.App.Services;

/// <summary>
/// 对话框服务实现，提供Avalonia对话框封装
/// Dialog service implementation that provides Avalonia dialog wrappers
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly Func<Window?> _windowProvider;
    private readonly ILocalizationService _localizationService;

    /// <summary>
    /// <para>初始化DialogService实例</para>
    /// Initialize the DialogService instance
    /// </summary>
    /// <param name="windowProvider">
    /// <para>提供当前活动窗口的函数</para>
    /// A function that provides the current active window
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务实例</para>
    /// Localization service instance
    /// </param>
    public DialogService(Func<Window?> windowProvider, ILocalizationService localizationService)
    {
        _windowProvider = windowProvider;
        _localizationService = localizationService;
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string title, string message)
    {
        var window = _windowProvider();
        if (window is null) return;

        var dialog = new DialogWindow(_localizationService);
        await dialog.ShowDialogAsync(window, title, message, DialogButtonType.Ok, isError: true);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var window = _windowProvider();
        if (window is null) return false;

        var dialog = new DialogWindow(_localizationService);
        var result = await dialog.ShowDialogAsync(window, title, message, DialogButtonType.YesNo, isError: false);
        return result == DialogResult.Yes;
    }

    /// <inheritdoc/>
    public async Task<string?> BrowseFolderAsync(string title, string? defaultPath = null)
    {
        var window = _windowProvider();
        if (window is null) return null;

        var storageProvider = window.StorageProvider;
        if (!storageProvider.CanOpen) return null;

        var folderPickerOptions = new FolderPickerOpenOptions
        {
            Title = title,
        };

        if (!string.IsNullOrEmpty(defaultPath))
        {
            try
            {
                var suggestedFolder = await storageProvider.TryGetFolderFromPathAsync(defaultPath);
                if (suggestedFolder is not null)
                {
                    folderPickerOptions.SuggestedStartLocation = suggestedFolder;
                }
            }
            catch
            {
            }
        }

        var result = await storageProvider.OpenFolderPickerAsync(folderPickerOptions);
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }
}
