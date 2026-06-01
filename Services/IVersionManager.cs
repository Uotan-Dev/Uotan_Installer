using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>版本管理器接口，提供版本记录管理和回滚功能。</para>
/// Version manager interface that provides version record management and rollback functionality.
/// </summary>
public interface IVersionManager
{
    /// <summary>
    /// <para>异步保存版本安装记录。</para>
    /// Asynchronously saves a version installation record.
    /// </summary>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <param name="record">
    /// <para>版本安装记录。</para>
    /// The version installation record.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    Task SaveVersionRecordAsync(string installPath, VersionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步获取版本安装历史记录。</para>
    /// Asynchronously gets the version installation history.
    /// </summary>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>版本安装记录列表。</para>
    /// The list of version installation records.
    /// </returns>
    Task<IReadOnlyList<VersionRecord>> GetVersionHistoryAsync(string installPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步回滚到指定版本。</para>
    /// Asynchronously rolls back to the specified version.
    /// </summary>
    /// <param name="installPath">
    /// <para>安装目录路径。</para>
    /// The installation directory path.
    /// </param>
    /// <param name="targetVersion">
    /// <para>目标版本号。</para>
    /// The target version string.
    /// </param>
    /// <param name="progress">
    /// <para>部署进度回调，可为 null。</para>
    /// The deployment progress callback, or null.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    Task RollbackAsync(string installPath, string targetVersion, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default);
}
