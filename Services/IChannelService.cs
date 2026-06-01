using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>发布渠道服务接口，提供渠道信息查询、版本获取和更新检测功能。</para>
/// Release channel service interface that provides channel information querying, version retrieval, and update detection.
/// </summary>
public interface IChannelService
{
    /// <summary>
    /// <para>异步获取所有可用的发布渠道列表。</para>
    /// Asynchronously gets the list of all available release channels.
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>可用发布渠道信息列表。</para>
    /// The list of available release channel information.
    /// </returns>
    Task<IReadOnlyList<ReleaseChannelInfo>> GetAvailableChannelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步获取指定发布渠道的详细信息。</para>
    /// Asynchronously gets detailed information for the specified release channel.
    /// </summary>
    /// <param name="channel">
    /// <para>要查询的发布渠道类型。</para>
    /// The release channel type to query.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>指定渠道的详细信息。</para>
    /// Detailed information for the specified channel.
    /// </returns>
    Task<ReleaseChannelInfo> GetChannelInfoAsync(ReleaseChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步获取指定发布渠道的最新版本号。</para>
    /// Asynchronously gets the latest version number for the specified release channel.
    /// </summary>
    /// <param name="channel">
    /// <para>要查询的发布渠道类型。</para>
    /// The release channel type to query.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>最新版本号字符串。</para>
    /// The latest version string.
    /// </returns>
    Task<string> GetLatestVersionAsync(ReleaseChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步检查指定发布渠道是否有可用更新。</para>
    /// Asynchronously checks whether an update is available for the specified release channel.
    /// </summary>
    /// <param name="channel">
    /// <para>要检查的发布渠道类型。</para>
    /// The release channel type to check.
    /// </param>
    /// <param name="currentVersion">
    /// <para>当前版本号。</para>
    /// The current version string.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>如果有可用更新返回 true，否则返回 false。</para>
    /// True if an update is available; otherwise false.
    /// </returns>
    Task<bool> IsChannelUpdateAvailableAsync(ReleaseChannel channel, string currentVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>根据渠道类型从 GitHub API 获取对应的发布信息。</para>
    /// Fetches the corresponding release information from the GitHub API based on the channel type.
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
    /// <para>匹配渠道的 GitHub 发布信息。</para>
    /// The GitHub release information matching the channel.
    /// </returns>
    Task<GitHubRelease> FetchReleaseByChannelAsync(ReleaseChannel channel, CancellationToken cancellationToken);
}
