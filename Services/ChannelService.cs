using System.Text.Json;
using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>发布渠道服务实现，根据渠道类型使用不同的 GitHub API 端点查询版本信息。</para>
/// Release channel service implementation that queries version information using different GitHub API endpoints based on the channel type.
/// </summary>
public sealed class ChannelService : IChannelService
{
    private const string GitHubApiBaseUrl = "https://api.github.com/repos/Uotan-Dev/UotanToolboxNT";

    private readonly IHttpService _httpService;

    /// <summary>
    /// <para>使用 HTTP 服务初始化 ChannelService 的新实例。</para>
    /// Initializes a new instance of ChannelService with the HTTP service.
    /// </summary>
    /// <param name="httpService">
    /// <para>HTTP 服务实例。</para>
    /// The HTTP service instance.
    /// </param>
    public ChannelService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ReleaseChannelInfo>> GetAvailableChannelsAsync(CancellationToken cancellationToken = default)
    {
        var channels = new List<ReleaseChannelInfo>
        {
            new()
            {
                Channel = ReleaseChannel.Release,
                DisplayName = "Release",
                Description = "Stable release",
                ApiEndpoint = "/releases/latest",
                IsAvailable = true,
                StabilityLevel = 1,
            },
            new()
            {
                Channel = ReleaseChannel.PreRelease,
                DisplayName = "Pre-Release",
                Description = "Pre-release",
                ApiEndpoint = "/releases",
                IsAvailable = true,
                StabilityLevel = 2,
            },
            new()
            {
                Channel = ReleaseChannel.Beta,
                DisplayName = "Beta",
                Description = "Beta version",
                ApiEndpoint = "/releases/tags/beta",
                IsAvailable = true,
                StabilityLevel = 3,
            },
            new()
            {
                Channel = ReleaseChannel.Nightly,
                DisplayName = "Nightly",
                Description = "Nightly build",
                ApiEndpoint = "/releases/tags/nightly",
                IsAvailable = true,
                StabilityLevel = 4,
            },
        };

        return Task.FromResult<IReadOnlyList<ReleaseChannelInfo>>(channels);
    }

    /// <inheritdoc/>
    public async Task<ReleaseChannelInfo> GetChannelInfoAsync(ReleaseChannel channel, CancellationToken cancellationToken = default)
    {
        var channels = await GetAvailableChannelsAsync(cancellationToken).ConfigureAwait(false);
        var info = channels.FirstOrDefault(c => c.Channel == channel);
        return info ?? throw new ArgumentOutOfRangeException(nameof(channel), channel, $"Unknown channel: {channel}");
    }

    /// <inheritdoc/>
    public async Task<string> GetLatestVersionAsync(ReleaseChannel channel, CancellationToken cancellationToken = default)
    {
        var release = await FetchReleaseByChannelAsync(channel, cancellationToken).ConfigureAwait(false);
        return release.TagName ?? "0.0.0.0";
    }

    /// <inheritdoc/>
    public async Task<bool> IsChannelUpdateAvailableAsync(ReleaseChannel channel, string currentVersion, CancellationToken cancellationToken = default)
    {
        var latestVersion = await GetLatestVersionAsync(channel, cancellationToken).ConfigureAwait(false);
        var latest = SemanticVersion.Parse(latestVersion);
        var current = SemanticVersion.Parse(currentVersion);
        return current < latest;
    }

    /// <inheritdoc/>
    public async Task<GitHubRelease> FetchReleaseByChannelAsync(ReleaseChannel channel, CancellationToken cancellationToken)
    {
        var client = _httpService.Client;

        return channel switch
        {
            ReleaseChannel.Release => await FetchLatestReleaseAsync(client, cancellationToken).ConfigureAwait(false),
            ReleaseChannel.PreRelease => await FetchPreReleaseAsync(client, cancellationToken).ConfigureAwait(false),
            ReleaseChannel.Beta => await FetchReleaseByTagAsync(client, "beta", cancellationToken).ConfigureAwait(false),
            ReleaseChannel.Nightly => await FetchReleaseByTagAsync(client, "nightly", cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, $"Unknown channel: {channel}"),
        };
    }

    private static async Task<GitHubRelease> FetchLatestReleaseAsync(System.Net.Http.HttpClient client, CancellationToken cancellationToken)
    {
        var url = $"{GitHubApiBaseUrl}/releases/latest";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubRelease)
            ?? throw new InvalidOperationException("Failed to deserialize GitHub release response.");
    }

    private static async Task<GitHubRelease> FetchPreReleaseAsync(System.Net.Http.HttpClient client, CancellationToken cancellationToken)
    {
        var url = $"{GitHubApiBaseUrl}/releases?per_page=10";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var releases = JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubReleaseArray)
            ?? throw new InvalidOperationException("Failed to deserialize GitHub releases response.");

        var preRelease = releases.FirstOrDefault(r => r.Prerelease && !r.Draft)
            ?? throw new InvalidOperationException("No pre-release version found.");

        return preRelease;
    }

    private static async Task<GitHubRelease> FetchReleaseByTagAsync(System.Net.Http.HttpClient client, string tag, CancellationToken cancellationToken)
    {
        var url = $"{GitHubApiBaseUrl}/releases/tags/{tag}";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return await FetchPreReleaseByTagFallbackAsync(client, tag, cancellationToken).ConfigureAwait(false);
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubRelease)
            ?? throw new InvalidOperationException("Failed to deserialize GitHub release response.");
    }

    private static async Task<GitHubRelease> FetchPreReleaseByTagFallbackAsync(System.Net.Http.HttpClient client, string tag, CancellationToken cancellationToken)
    {
        var url = $"{GitHubApiBaseUrl}/releases?per_page=20";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var releases = JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubReleaseArray)
            ?? throw new InvalidOperationException("Failed to deserialize GitHub releases response.");

        var matched = releases.FirstOrDefault(r =>
            r.Prerelease &&
            !r.Draft &&
            (r.TagName?.Contains(tag, StringComparison.OrdinalIgnoreCase) == true ||
             r.Name?.Contains(tag, StringComparison.OrdinalIgnoreCase) == true))
            ?? throw new InvalidOperationException($"No release found matching tag '{tag}'.");

        return matched;
    }
}
