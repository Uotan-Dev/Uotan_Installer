using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>发布API服务接口，提供与发布服务器交互的所有API</para>
/// Release API service interface that provides all APIs for interacting with the release server
/// </summary>
public interface IReleaseApiService
{
    /// <summary>
    /// <para>获取补丁数据</para>
    /// Get patch data
    /// </summary>
    /// <param name="channel">
    /// <para>发布渠道类型，默认为正式版。</para>
    /// The release channel type; defaults to Release.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>补丁数据</para>
    /// The patch data
    /// </returns>
    Task<GenericPatchData> GetPatchDataAsync(ReleaseChannel channel = ReleaseChannel.Release, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>检查安装器自更新</para>
    /// Check for installer self-update
    /// </summary>
    /// <param name="currentVersion">
    /// <para>当前版本号</para>
    /// The current version number
    /// </param>
    /// <param name="channel">
    /// <para>发布渠道类型，默认为正式版。</para>
    /// The release channel type; defaults to Release.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌</para>
    /// Cancellation token
    /// </param>
    /// <returns>
    /// <para>如果有新版本返回true，否则返回false</para>
    /// True if a newer version is available; otherwise false
    /// </returns>
    Task<bool> CheckSelfUpdateAsync(string currentVersion, ReleaseChannel channel = ReleaseChannel.Release, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步获取指定发布渠道的发布版本列表。</para>
    /// Asynchronously gets the list of releases for the specified release channel.
    /// </summary>
    /// <param name="channel">
    /// <para>发布渠道类型。</para>
    /// The release channel type.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>指定渠道的 GitHub 发布版本列表。</para>
    /// The list of GitHub releases for the specified channel.
    /// </returns>
    Task<IReadOnlyList<GitHubRelease>> GetReleasesByChannelAsync(ReleaseChannel channel, CancellationToken cancellationToken = default);
}
