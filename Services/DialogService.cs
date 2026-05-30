using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>对话框服务实现，提供Avalonia对话框封装</para>
/// Dialog service implementation that provides Avalonia dialog wrappers
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly Func<Window?> _windowProvider;

    /// <summary>
    /// <para>初始化DialogService实例</para>
    /// Initialize the DialogService instance
    /// </summary>
    /// <param name="windowProvider">
    /// <para>提供当前活动窗口的函数</para>
    /// A function that provides the current active window
    /// </param>
    public DialogService(Func<Window?> windowProvider)
    {
        _windowProvider = windowProvider;
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string title, string message)
    {
        var window = _windowProvider();
        if (window is null) return;

        await ShowDialogAsync(window, title, message, MessageBoxButtons.Ok, isError: true);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var window = _windowProvider();
        if (window is null) return false;

        var result = await ShowDialogAsync(window, title, message, MessageBoxButtons.YesNo, isError: false);
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

    private static async Task<DialogResult> ShowDialogAsync(
        Window owner, string title, string message, MessageBoxButtons buttons, bool isError)
    {
        var tcs = new TaskCompletionSource<DialogResult>();

        var surfaceBrush = owner.Background ?? new SolidColorBrush(Color.FromRgb(243, 243, 243));
        var surfaceVariantBrush = TryGetResourceBrush("SurfaceVariantBrush") ?? new SolidColorBrush(Color.FromRgb(232, 232, 232));
        var textPrimaryBrush = TryGetResourceBrush("TextPrimaryBrush") ?? Brushes.Black;
        var textSecondaryBrush = TryGetResourceBrush("TextSecondaryBrush") ?? new SolidColorBrush(Color.FromRgb(97, 97, 97));
        var borderBrush = TryGetResourceBrush("BorderBrush") ?? new SolidColorBrush(Color.FromRgb(224, 224, 224));
        var brandBrush = TryGetResourceBrush("BrandBrush") ?? new SolidColorBrush(Color.FromRgb(232, 93, 38));

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = surfaceBrush,
            FontFamily = owner.FontFamily,
            Icon = owner.Icon,
        };

        var iconChar = "\uE921";

        var contentPanel = new StackPanel
        {
            Margin = new Thickness(24, 24, 24, 0),
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
        };

        var iconBorder = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = brandBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = iconChar,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            Foreground = textPrimaryBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };

        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(titleBlock);

        var messageBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = textSecondaryBrush,
            Margin = new Thickness(0, 0, 0, 8),
        };

        contentPanel.Children.Add(headerPanel);
        contentPanel.Children.Add(messageBlock);

        var buttonBorder = new Border
        {
            Background = surfaceVariantBrush,
            Padding = new Thickness(24, 16),
            Margin = new Thickness(0, 16, 0, 0),
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };

        void AddSecondaryButton(string label, DialogResult result)
        {
            var btn = new Button
            {
                Content = label,
                MinWidth = 90,
                MinHeight = 36,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(20, 8),
                Foreground = textPrimaryBrush,
                Background = surfaceBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                FontWeight = FontWeight.Normal,
                FontSize = 14,
            };

            btn.Click += (_, _) =>
            {
                tcs.TrySetResult(result);
                dialog.Close();
            };
            buttonPanel.Children.Add(btn);
        }

        void AddPrimaryButton(string label, DialogResult result)
        {
            var btn = new Button
            {
                Content = label,
                MinWidth = 90,
                MinHeight = 36,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(20, 8),
                Foreground = Brushes.White,
                Background = brandBrush,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
            };

            btn.Click += (_, _) =>
            {
                tcs.TrySetResult(result);
                dialog.Close();
            };
            buttonPanel.Children.Add(btn);
        }

        if (buttons == MessageBoxButtons.Ok)
        {
            AddPrimaryButton("确定", DialogResult.Ok);
        }
        else if (buttons == MessageBoxButtons.YesNo)
        {
            AddSecondaryButton("否", DialogResult.No);
            AddPrimaryButton("是", DialogResult.Yes);
        }

        buttonBorder.Child = buttonPanel;

        var dialogStack = new StackPanel { Spacing = 0 };
        dialogStack.Children.Add(contentPanel);
        dialogStack.Children.Add(buttonBorder);

        dialog.Content = dialogStack;

        await dialog.ShowDialog(owner);

        return await tcs.Task;
    }

    private static IBrush? TryGetResourceBrush(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }
        return null;
    }

    private enum DialogResult { Ok, Yes, No, Cancel }
    private enum MessageBoxButtons { Ok, YesNo }
}
