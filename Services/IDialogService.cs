namespace UotanInstaller.App.Services;

/// <summary>
/// <para>对话框服务接口，提供Avalonia对话框封装</para>
/// Dialog service interface that provides Avalonia dialog wrappers
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// <para>显示错误对话框</para>
    /// Show an error dialog
    /// </summary>
    /// <param name="title">
    /// <para>对话框标题</para>
    /// The dialog title
    /// </param>
    /// <param name="message">
    /// <para>对话框消息</para>
    /// The dialog message
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// <para>显示确认对话框</para>
    /// Show a confirmation dialog
    /// </summary>
    /// <param name="title">
    /// <para>对话框标题</para>
    /// The dialog title
    /// </param>
    /// <param name="message">
    /// <para>对话框消息</para>
    /// The dialog message
    /// </param>
    /// <returns>
    /// <para>用户点击确认返回true，否则返回false</para>
    /// True if the user confirmed; otherwise false
    /// </returns>
    Task<bool> ShowConfirmAsync(string title, string message);

    /// <summary>
    /// <para>打开文件夹浏览对话框。</para>
    /// Open a folder browser dialog.
    /// </summary>
    /// <param name="title">
    /// <para>对话框标题。</para>
    /// The dialog title.
    /// </param>
    /// <param name="defaultPath">
    /// <para>默认打开的路径，可为 null。</para>
    /// The default path to open, or null.
    /// </param>
    /// <returns>
    /// <para>用户选择的文件夹路径，取消时返回 null。</para>
    /// The selected folder path, or null if cancelled.
    /// </returns>
    Task<string?> BrowseFolderAsync(string title, string? defaultPath = null);
}
