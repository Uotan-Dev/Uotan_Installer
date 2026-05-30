namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>提供平台相关的适配操作，包括安装目录解析、快捷方式创建、应用启动及权限提升。</para>
/// Provides platform-specific adaptation operations including install directory resolution, shortcut creation, application launching, and privilege elevation.
/// </summary>
public interface IPlatformAdapter
{
    /// <summary>
    /// <para>获取指定应用程序的默认安装目录路径。</para>
    /// Gets the default installation directory path for the specified application.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称。</para>
    /// The application name.
    /// </param>
    /// <returns>
    /// <para>默认安装目录的绝对路径。</para>
    /// The absolute path of the default installation directory.
    /// </returns>
    string GetDefaultInstallDirectory(string appName);

    /// <summary>
    /// <para>在桌面创建应用程序快捷方式。</para>
    /// Creates an application shortcut on the desktop.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称。</para>
    /// The application name.
    /// </param>
    /// <param name="targetPath">
    /// <para>快捷方式指向的目标可执行文件路径。</para>
    /// The target executable file path that the shortcut points to.
    /// </param>
    /// <param name="args">
    /// <para>启动目标程序时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed when launching the target program, or null.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task CreateDesktopShortcutAsync(string appName, string targetPath, string? args = null, CancellationToken ct = default);

    /// <summary>
    /// <para>启动指定的应用程序。</para>
    /// Launches the specified application.
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件的路径。</para>
    /// The executable file path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    void LaunchApplication(string exePath, string? args = null);

    /// <summary>
    /// <para>以提升的权限（管理员/root）启动指定程序。</para>
    /// Launches the specified program with elevated privileges (administrator/root).
    /// </summary>
    /// <param name="exePath">
    /// <para>可执行文件的路径。</para>
    /// The executable file path.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    void RequestElevation(string exePath, string? args = null);

    /// <summary>
    /// <para>获取当前平台的临时文件目录路径。</para>
    /// Gets the temporary file directory path for the current platform.
    /// </summary>
    /// <returns>
    /// <para>临时文件目录的绝对路径。</para>
    /// The absolute path of the temporary file directory.
    /// </returns>
    string GetTempDirectory();
}
