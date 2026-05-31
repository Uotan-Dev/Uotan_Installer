using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;

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
    /// <para>基于部署配置执行完整的部署流程，通过管线编排下载、校验、解压、创建快捷方式和启动应用等步骤</para>
    /// Execute the complete deployment process based on the deployment configuration, orchestrating steps such as download, verify, extract, create shortcut, and launch via pipeline
    /// </summary>
    /// <param name="configuration">
    /// <para>部署配置信息，包含安装路径、下载源、文件规则及步骤开关等</para>
    /// The deployment configuration containing install path, download sources, file rules, and step toggles
    /// </param>
    /// <param name="progress">
    /// <para>部署进度回调，可为 null</para>
    /// The deployment progress callback, or null
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>部署管线的执行结果，包含成功状态、已完成步骤及错误信息</para>
    /// The deployment pipeline execution result containing success status, completed steps, and error information
    /// </returns>
    Task<DeploymentResult> DeployAsync(DeploymentConfiguration configuration, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>执行安装器自更新</para>
    /// Perform an installer self-update
    /// </summary>
    /// <param name="downloadProgress">
    /// <para>下载进度回调，报告已下载字节数和总字节数。</para>
    /// Download progress callback reporting downloaded bytes and total bytes.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>异步任务</para>
    /// An asynchronous task
    /// </returns>
    Task SelfUpdateAsync(IProgress<(long Downloaded, long Total)>? downloadProgress = null, CancellationToken cancellationToken = default);

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
    /// <para>异步创建桌面快捷方式</para>
    /// Asynchronously create a desktop shortcut
    /// </summary>
    /// <param name="installPath">
    /// <para>自定义安装路径，为 null 时使用默认路径</para>
    /// Custom installation path, uses default path when null
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task</para>
    /// A task representing the asynchronous operation
    /// </returns>
    Task CreateDesktopShortcutAsync(string? installPath = null, CancellationToken cancellationToken = default);

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
