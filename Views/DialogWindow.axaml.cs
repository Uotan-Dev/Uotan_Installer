using Avalonia.Controls;
using Avalonia.Input;

namespace UotanInstaller.App.Views;

public enum DialogResult { Ok, Yes, No, Cancel }

public enum DialogButtonType { Ok, YesNo }

public partial class DialogWindow : Window
{
    private TaskCompletionSource<DialogResult>? _tcs;

    public DialogWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Closed += OnClosed;
    }

    public async Task<DialogResult> ShowDialogAsync(
        Window owner, string title, string message,
        DialogButtonType buttonType, bool isError)
    {
        _tcs = new TaskCompletionSource<DialogResult>();

        Title = title;
        FontFamily = owner.FontFamily;
        Icon = owner.Icon;

        TitleText.Text = title;
        MessageText.Text = message;

        ErrorIcon.IsVisible = isError;
        ConfirmIcon.IsVisible = !isError;

        AddButtons(buttonType);

        await ShowDialog(owner);
        return await _tcs.Task;
    }

    private void AddButtons(DialogButtonType buttonType)
    {
        ButtonPanel.Children.Clear();

        if (buttonType == DialogButtonType.Ok)
        {
            var btn = CreatePrimaryButton("确定", DialogResult.Ok);
            DockPanel.SetDock(btn, Dock.Right);
            ButtonPanel.Children.Add(btn);
        }
        else
        {
            var secondaryBtn = CreateSecondaryButton("否", DialogResult.No);
            DockPanel.SetDock(secondaryBtn, Dock.Left);
            ButtonPanel.Children.Add(secondaryBtn);

            var primaryBtn = CreatePrimaryButton("是", DialogResult.Yes);
            DockPanel.SetDock(primaryBtn, Dock.Right);
            ButtonPanel.Children.Add(primaryBtn);
        }
    }

    private Button CreatePrimaryButton(string label, DialogResult result)
    {
        var btn = new Button
        {
            Content = label,
            Classes = { "accent" },
        };
        btn.Click += (_, _) => CloseWithResult(result);
        return btn;
    }

    private Button CreateSecondaryButton(string label, DialogResult result)
    {
        var btn = new Button
        {
            Content = label,
            Classes = { "secondary" },
        };
        btn.Click += (_, _) => CloseWithResult(result);
        return btn;
    }

    private void CloseWithResult(DialogResult result)
    {
        _tcs?.TrySetResult(result);
        Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseWithResult(DialogResult.Cancel);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _tcs?.TrySetResult(DialogResult.Cancel);
        KeyDown -= OnKeyDown;
        Closed -= OnClosed;
    }
}
