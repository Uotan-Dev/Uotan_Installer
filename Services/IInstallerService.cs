using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装器核心服务接口，协调所有其他服务完成安装流程</para>
/// Installer core service interface that coordinates all other services to complete the installation process
/// </summary>
public interface IInstallerService
{
    /// <summary>
    /// <para>获取安装配置</para>
    /// Get the installation configuration
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>安装器配置</para>
    /// The installer configuration
    /// </returns>
    Task<InstallerConfig> GetConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>执行完整的安装流程</para>
    /// Execute the complete installation process
    /// </summary>
    /// <param name="mirrorUrl">
    /// <para>镜像下载URL</para>
    /// The mirror download URL
    /// </param>
    /// <param name="sha256">
    /// <para>期望的SHA256校验值</para>
    /// The expected SHA256 checksum
    /// </param>
    /// <param name="offlineMode">
    /// <para>是否为离线安装模式</para>
    /// Whether this is an offline installation mode
    /// </param>
    /// <param name="installPath">
    /// <para>自定义安装路径，为 null 时使用默认路径</para>
    /// Custom installation path, uses default path when null
    /// </param>
    /// <param name="progress">
    /// <para>安装进度回调</para>
    /// The installation progress callback
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>如果安装成功返回true，否则返回false</para>
    /// True if installation succeeded; otherwise false
    /// </returns>
    Task<bool> StartInstallAsync(string mirrorUrl, string sha256, bool offlineMode, string? installPath = null, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>执行安装器自更新</para>
    /// Perform an installer self-update
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task SelfUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>启动柚坛工具箱并退出安装器</para>
    /// Launch the UotanToolbox application and exit the installer
    /// </summary>
    /// <param name="installPath">
    /// <para>自定义安装路径，为 null 时使用默认路径</para>
    /// Custom installation path, uses default path when null
    /// </param>
    void LaunchAndExit(string? installPath = null);

    /// <summary>
    /// <para>创建桌面快捷方式</para>
    /// Create a desktop shortcut
    /// </summary>
    /// <param name="installPath">
    /// <para>自定义安装路径，为 null 时使用默认路径</para>
    /// Custom installation path, uses default path when null
    /// </param>
    void CreateDesktopShortcut(string? installPath = null);

    /// <summary>
    /// <para>清理安装过程中产生的临时文件</para>
    /// Clean up temporary files generated during installation
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task CleanupTempFilesAsync(CancellationToken cancellationToken = default);
}
