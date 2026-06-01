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

    /// <summary>
    /// <para>在指定安装目录中查找应用程序的可执行文件路径。</para>
    /// Finds the application executable file path in the specified installation directory.
    /// </summary>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的任务，结果为可执行文件路径；若未找到则返回 null。</para>
    /// A task representing the asynchronous operation, with the result being the executable file path; or null if not found.
    /// </returns>
    Task<string?> FindExecutableAsync(string installPath);

    /// <summary>
    /// <para>使用系统文件管理器打开指定目录。</para>
    /// Opens the specified directory using the system file explorer.
    /// </summary>
    /// <param name="path">
    /// <para>要打开的目录路径。</para>
    /// The directory path to open.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task OpenInFileExplorerAsync(string path);

    /// <summary>
    /// <para>异步注册 URL 协议处理器。</para>
    /// Asynchronously registers a URL protocol handler.
    /// </summary>
    /// <param name="scheme">
    /// <para>URL 协议方案（如 "uotan"）。</para>
    /// The URL protocol scheme (e.g. "uotan").
    /// </param>
    /// <param name="exePath">
    /// <para>处理协议请求的可执行文件路径。</para>
    /// The executable file path that handles the protocol request.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task RegisterProtocolHandlerAsync(string scheme, string exePath, CancellationToken ct);

    /// <summary>
    /// <para>异步将指定目录添加到系统 PATH 环境变量。</para>
    /// Asynchronously adds the specified directory to the system PATH environment variable.
    /// </summary>
    /// <param name="directory">
    /// <para>要添加的目录路径。</para>
    /// The directory path to add.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task AddToSystemPathAsync(string directory, CancellationToken ct);

    /// <summary>
    /// <para>异步从系统 PATH 环境变量中移除指定目录。</para>
    /// Asynchronously removes the specified directory from the system PATH environment variable.
    /// </summary>
    /// <param name="directory">
    /// <para>要移除的目录路径。</para>
    /// The directory path to remove.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task RemoveFromSystemPathAsync(string directory, CancellationToken ct);

    /// <summary>
    /// <para>获取指定应用程序的数据目录路径。</para>
    /// Gets the data directory path for the specified application.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称。</para>
    /// The application name.
    /// </param>
    /// <returns>
    /// <para>应用数据目录的绝对路径。</para>
    /// The absolute path of the application data directory.
    /// </returns>
    string GetAppDataDirectory(string appName);

    /// <summary>
    /// <para>异步获取指定安装路径中应用程序的已安装版本号。</para>
    /// Asynchronously gets the installed version number of the application at the specified installation path.
    /// </summary>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的任务，结果为版本号字符串；若未安装则返回 null。</para>
    /// A task representing the asynchronous operation, with the result being the version string; or null if not installed.
    /// </returns>
    Task<string?> GetInstalledVersionAsync(string installPath);
}
