using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>增量更新服务接口，提供增量更新检测和应用功能。</para>
/// Delta update service interface that provides delta update detection and application.
/// </summary>
public interface IDeltaUpdateService
{
    /// <summary>
    /// <para>异步检查是否存在从当前版本到目标版本的增量更新包。</para>
    /// Asynchronously checks whether a delta update package exists from the current version to the target version.
    /// </summary>
    /// <param name="currentVersion">
    /// <para>当前版本号。</para>
    /// The current version string.
    /// </param>
    /// <param name="targetVersion">
    /// <para>目标版本号。</para>
    /// The target version string.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>增量更新包信息，如果不存在则返回 null。</para>
    /// The delta update package information, or null if not available.
    /// </returns>
    Task<DeltaUpdateInfo?> CheckDeltaUpdateAvailableAsync(string currentVersion, string targetVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步应用增量更新包。</para>
    /// Asynchronously applies a delta update package.
    /// </summary>
    /// <param name="deltaInfo">
    /// <para>增量更新包信息。</para>
    /// The delta update package information.
    /// </param>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <param name="progress">
    /// <para>部署进度回调，可为 null。</para>
    /// The deployment progress callback, or null.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    Task ApplyDeltaUpdateAsync(DeltaUpdateInfo deltaInfo, string installPath, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default);
}
