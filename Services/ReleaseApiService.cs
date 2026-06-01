using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>GitHub Release API 服务实现，通过 GitHub Releases API 获取 UotanToolboxNT 的发布信息</para>
/// GitHub Release API service implementation that fetches UotanToolboxNT release information via GitHub Releases API
/// </summary>
public sealed class ReleaseApiService : IReleaseApiService
{
    private const string GitHubApiBaseUrl = "https://api.github.com/repos/Uotan-Dev/UotanToolboxNT";
    private const string GitHubReleasesDownloadUrl = "https://github.com/Uotan-Dev/UotanToolboxNT/releases/download";

    private readonly IHttpService _httpService;
    private readonly IGitHubMirrorService _gitHubMirrorService;
    private readonly IChannelService _channelService;

    /// <summary>
    /// <para>初始化 ReleaseApiService 实例</para>
    /// Initialize the ReleaseApiService instance
    /// </summary>
    /// <param name="httpService">
    /// <para>HTTP 服务</para>
    /// The HTTP service
    /// </param>
    /// <param name="gitHubMirrorService">
    /// <para>GitHub 镜像站点服务</para>
    /// The GitHub mirror site service
    /// </param>
    /// <param name="channelService">
    /// <para>发布渠道服务</para>
    /// The channel service
    /// </param>
    public ReleaseApiService(IHttpService httpService, IGitHubMirrorService gitHubMirrorService, IChannelService channelService)
    {
        _httpService = httpService;
        _gitHubMirrorService = gitHubMirrorService;
        _channelService = channelService;
    }

    /// <inheritdoc/>
    public async Task<GenericPatchData> GetPatchDataAsync(ReleaseChannel channel = ReleaseChannel.Release, CancellationToken cancellationToken = default)
    {
        var release = await _channelService.FetchReleaseByChannelAsync(channel, cancellationToken).ConfigureAwait(false);

        var version = release.TagName ?? throw new InvalidOperationException("Release tag name is null.");

        var currentPlatform = DetectCurrentPlatform();
        var arch = RuntimeInformation.OSArchitecture;
        var archSuffix = arch == Architecture.Arm64 ? "arm64" : "x64";
        var platformPrefix = GetPlatformAssetPrefix(currentPlatform);

        var mirrors = new List<GenericPatchPackageMirror>();

        foreach (var asset in release.Assets ?? [])
        {
            if (asset.Name is null || asset.BrowserDownloadUrl is null) continue;

            if (asset.Name.Contains("Installer", StringComparison.OrdinalIgnoreCase) ||
                asset.Name.Contains("Deployment", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsPlatformAssetMatch(asset.Name, currentPlatform, archSuffix))
            {
                mirrors.Add(new GenericPatchPackageMirror
                {
                    Url = asset.BrowserDownloadUrl,
                    MirrorName = $"UotanToolbox {platformPrefix} {archSuffix}",
                });
            }
        }

        if (mirrors.Count == 0)
        {
            throw new PlatformNotSupportedDeploymentException(
                $"No matching package found for platform '{currentPlatform}' with architecture '{archSuffix}'.",
                currentPlatform);
        }

        var mirrorSites = _gitHubMirrorService.GetMirrorSites();
        var additionalMirrors = new List<GenericPatchPackageMirror>();

        foreach (var mirror in mirrors.ToList())
        {
            if (!mirror.Url.Contains("github.com", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var site in mirrorSites)
            {
                if (string.IsNullOrEmpty(site.BaseUrl)) continue;

                var mirrorUrl = _gitHubMirrorService.CreateMirrorUrl(site, mirror.Url);
                additionalMirrors.Add(new GenericPatchPackageMirror
                {
                    Url = mirrorUrl,
                    MirrorName = $"{mirror.MirrorName} ({site.Name})",
                });
            }
        }

        mirrors.AddRange(additionalMirrors);

        var sha256 = string.Empty;
        var matchedAsset = release.Assets?.FirstOrDefault(a =>
            a.Name is not null &&
            IsPlatformAssetMatch(a.Name, currentPlatform, archSuffix) &&
            !a.Name.Contains("Installer", StringComparison.OrdinalIgnoreCase) &&
            !a.Name.Contains("Deployment", StringComparison.OrdinalIgnoreCase) &&
            a.Digest is not null);

        if (matchedAsset?.Digest is not null)
        {
            var digestParts = matchedAsset.Digest.Split(':');
            if (digestParts.Length == 2)
            {
                sha256 = digestParts[1];
            }
        }

        return new GenericPatchData
        {
            Version = version,
            Sha256 = sha256,
            Mirrors = mirrors,
        };
    }

    /// <inheritdoc/>
    public async Task<bool> CheckSelfUpdateAsync(string currentVersion, ReleaseChannel channel = ReleaseChannel.Release, CancellationToken cancellationToken = default)
    {
        var release = await _channelService.FetchReleaseByChannelAsync(channel, cancellationToken).ConfigureAwait(false);
        var latestVersion = SemanticVersion.Parse(release.TagName ?? "0.0.0.0");
        var current = SemanticVersion.Parse(currentVersion);
        return current < latestVersion;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GitHubRelease>> GetReleasesByChannelAsync(ReleaseChannel channel, CancellationToken cancellationToken = default)
    {
        var client = _httpService.Client;

        if (channel == ReleaseChannel.Release || channel == ReleaseChannel.PreRelease)
        {
            var url = $"{GitHubApiBaseUrl}/releases?per_page=20";
            var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var releases = JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubReleaseArray)
                ?? throw new InvalidOperationException("Failed to deserialize GitHub releases response.");

            return channel == ReleaseChannel.PreRelease
                ? releases.Where(r => r.Prerelease && !r.Draft).ToList()
                : releases.Where(r => !r.Prerelease && !r.Draft).ToList();
        }

        var singleRelease = await _channelService.FetchReleaseByChannelAsync(channel, cancellationToken).ConfigureAwait(false);
        return new List<GitHubRelease> { singleRelease };
    }

    /// <summary>
    /// <para>检测当前运行平台的操作系统类型。</para>
    /// Detects the operating system type of the current runtime platform.
    /// </summary>
    /// <returns>
    /// <para>当前平台对应的 PlatformOS 枚举值。</para>
    /// The PlatformOS enum value corresponding to the current platform.
    /// </returns>
    private static PlatformOS DetectCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return PlatformOS.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformOS.macOS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return PlatformOS.Linux;
        return PlatformOS.Unknown;
    }

    /// <summary>
    /// <para>根据平台类型获取资源名称中使用的平台前缀字符串。</para>
    /// Gets the platform prefix string used in asset names based on the platform type.
    /// </summary>
    /// <param name="platform">
    /// <para>操作系统平台类型。</para>
    /// The operating system platform type.
    /// </param>
    /// <returns>
    /// <para>资源名称中的平台前缀字符串。</para>
    /// The platform prefix string in asset names.
    /// </returns>
    private static string GetPlatformAssetPrefix(PlatformOS platform) => platform switch
    {
        PlatformOS.Windows => "Windows",
        PlatformOS.macOS => "macOS",
        PlatformOS.Linux => "Linux",
        _ => "Unknown",
    };

    /// <summary>
    /// <para>判断资源名称是否匹配当前平台和架构后缀。</para>
    /// Determines whether the asset name matches the current platform and architecture suffix.
    /// </summary>
    /// <param name="assetName">
    /// <para>资源文件名称。</para>
    /// The asset file name.
    /// </param>
    /// <param name="platform">
    /// <para>目标操作系统平台类型。</para>
    /// The target operating system platform type.
    /// </param>
    /// <param name="archSuffix">
    /// <para>架构后缀，如 "x64" 或 "arm64"。</para>
    /// The architecture suffix, e.g. "x64" or "arm64".
    /// </param>
    /// <returns>
    /// <para>如果资源名称匹配当前平台和架构，返回 true；否则返回 false。</para>
    /// True if the asset name matches the current platform and architecture; otherwise false.
    /// </returns>
    private static bool IsPlatformAssetMatch(string assetName, PlatformOS platform, string archSuffix)
    {
        var suffix = $"_{archSuffix}";
        return platform switch
        {
            PlatformOS.Windows => assetName.Contains($"Windows{suffix}", StringComparison.OrdinalIgnoreCase),
            PlatformOS.macOS => assetName.Contains($"macOS{suffix}", StringComparison.OrdinalIgnoreCase) ||
                                assetName.Contains($"Darwin{suffix}", StringComparison.OrdinalIgnoreCase) ||
                                assetName.Contains($"Mac{suffix}", StringComparison.OrdinalIgnoreCase),
            PlatformOS.Linux => assetName.Contains($"Linux{suffix}", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
