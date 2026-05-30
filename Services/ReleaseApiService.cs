using System.Runtime.InteropServices;
using System.Text.Json;
using UotanInstaller.App.Models;

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
    public ReleaseApiService(IHttpService httpService, IGitHubMirrorService gitHubMirrorService)
    {
        _httpService = httpService;
        _gitHubMirrorService = gitHubMirrorService;
    }

    /// <inheritdoc/>
    public async Task<GenericPatchData> GetPatchDataAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpService.Client;
        var url = $"{GitHubApiBaseUrl}/releases/latest";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var release = JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubRelease)
            ?? throw new InvalidOperationException("Failed to deserialize GitHub release response.");

        var version = release.TagName ?? throw new InvalidOperationException("Release tag name is null.");

        var mirrors = new List<GenericPatchPackageMirror>();
        var arch = RuntimeInformation.OSArchitecture;
        var archSuffix = arch == Architecture.Arm64 ? "arm64" : "x64";

        foreach (var asset in release.Assets ?? [])
        {
            if (asset.Name is null || asset.BrowserDownloadUrl is null) continue;

            if (asset.Name.Contains("Installer", StringComparison.OrdinalIgnoreCase) ||
                asset.Name.Contains("Deployment", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (asset.Name.Contains($"Windows_{archSuffix}", StringComparison.OrdinalIgnoreCase))
            {
                mirrors.Add(new GenericPatchPackageMirror
                {
                    Url = asset.BrowserDownloadUrl,
                    MirrorName = $"UotanToolbox Windows {archSuffix}",
                });
            }
        }

        if (mirrors.Count == 0)
        {
            mirrors.Add(new GenericPatchPackageMirror
            {
                Url = $"{GitHubReleasesDownloadUrl}/{version}/UotanToolbox_Windows_x64_{version}.zip",
                MirrorName = "UotanToolbox Windows x64",
            });
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
            a.Name.Contains($"Windows_{archSuffix}", StringComparison.OrdinalIgnoreCase) &&
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
    public async Task<bool> CheckSelfUpdateAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        var client = _httpService.Client;
        var url = $"{GitHubApiBaseUrl}/releases/latest";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var release = JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubRelease)
            ?? throw new InvalidOperationException("Failed to deserialize GitHub release response.");

        var latestVersion = WindowsVersion.Parse(release.TagName ?? "0.0.0.0");
        var current = WindowsVersion.Parse(currentVersion);
        return current < latestVersion;
    }
}
