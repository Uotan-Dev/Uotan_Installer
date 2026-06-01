using SharpCompress.Archives;
using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>增量更新服务实现，通过 GitHub Release assets 查找增量更新包并应用。</para>
/// Delta update service implementation that finds and applies delta update packages via GitHub Release assets.
/// </summary>
public sealed class DeltaUpdateService : IDeltaUpdateService
{
    private readonly IReleaseApiService _releaseApiService;
    private readonly IDownloadService _downloadService;
    private readonly IPlatformAdapter _platformAdapter;
    private readonly ILocalizationService _localizationService;
    private readonly IFileService _fileService;

    /// <summary>
    /// <para>初始化 DeltaUpdateService 实例。</para>
    /// Initializes a new instance of DeltaUpdateService.
    /// </summary>
    /// <param name="releaseApiService">
    /// <para>发布 API 服务。</para>
    /// The release API service.
    /// </param>
    /// <param name="downloadService">
    /// <para>下载服务。</para>
    /// The download service.
    /// </param>
    /// <param name="platformAdapter">
    /// <para>平台适配器。</para>
    /// The platform adapter.
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务。</para>
    /// The localization service.
    /// </param>
    /// <param name="fileService">
    /// <para>文件服务。</para>
    /// The file service.
    /// </param>
    public DeltaUpdateService(
        IReleaseApiService releaseApiService,
        IDownloadService downloadService,
        IPlatformAdapter platformAdapter,
        ILocalizationService localizationService,
        IFileService fileService)
    {
        _releaseApiService = releaseApiService;
        _downloadService = downloadService;
        _platformAdapter = platformAdapter;
        _localizationService = localizationService;
        _fileService = fileService;
    }

    /// <inheritdoc/>
    public async Task<DeltaUpdateInfo?> CheckDeltaUpdateAvailableAsync(string currentVersion, string targetVersion, CancellationToken cancellationToken = default)
    {
        var releases = await _releaseApiService.GetReleasesByChannelAsync(ReleaseChannel.Release, cancellationToken).ConfigureAwait(false);

        var targetRelease = releases.FirstOrDefault(r =>
            string.Equals(r.TagName, targetVersion, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.TagName, $"v{targetVersion}", StringComparison.OrdinalIgnoreCase));

        if (targetRelease?.Assets is null)
            return null;

        var normalizedCurrent = currentVersion.TrimStart('v', 'V');
        var normalizedTarget = targetVersion.TrimStart('v', 'V');

        GitHubReleaseAsset? deltaAsset = null;

        foreach (var asset in targetRelease.Assets)
        {
            if (asset.Name is null) continue;

            if (asset.Name.Equals($"delta-{normalizedCurrent}-to-{normalizedTarget}.zip", StringComparison.OrdinalIgnoreCase) ||
                asset.Name.Equals($"delta-{normalizedCurrent}-{normalizedTarget}.zip", StringComparison.OrdinalIgnoreCase))
            {
                deltaAsset = asset;
                break;
            }
        }

        if (deltaAsset is null || deltaAsset.BrowserDownloadUrl is null)
            return null;

        return new DeltaUpdateInfo
        {
            DownloadUrl = deltaAsset.BrowserDownloadUrl,
            FromVersion = normalizedCurrent,
            ToVersion = normalizedTarget,
            Size = deltaAsset.Size,
            Checksum = deltaAsset.Digest,
        };
    }

    /// <inheritdoc/>
    public async Task ApplyDeltaUpdateAsync(DeltaUpdateInfo deltaInfo, string installPath, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var tempDir = _platformAdapter.GetTempDirectory();
        var deltaPackagePath = Path.Combine(tempDir, $"delta-{deltaInfo.FromVersion}-{deltaInfo.ToVersion}.zip");

        try
        {
            progress?.Report(new DeploymentProgress
            {
                StepName = "DeltaUpdate",
                Kind = DeploymentStepKind.Download,
                ProgressValue = 0.0,
                Message = _localizationService["Step_Downloading"],
            });

            await _downloadService.DownloadFileAsync(deltaInfo.DownloadUrl, deltaPackagePath, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(deltaInfo.Checksum))
            {
                var digestParts = deltaInfo.Checksum.Split(':');
                if (digestParts.Length == 2)
                {
                    var expectedHash = digestParts[1];
                    var actualHash = await _fileService.ComputeSha256Async(deltaPackagePath, cancellationToken).ConfigureAwait(false);
                    if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new DeploymentException($"Delta update verification failed. Expected: {expectedHash}, Actual: {actualHash}");
                    }
                }
            }

            progress?.Report(new DeploymentProgress
            {
                StepName = "DeltaUpdate",
                Kind = DeploymentStepKind.Extract,
                ProgressValue = 0.5,
                Message = _localizationService["Step_ExtractRunning"],
            });

            var extractDir = Path.Combine(tempDir, $"delta-extract-{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDir);

            try
            {
                using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(deltaPackagePath);
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entryKey = entry.Key ?? string.Empty;
                    var fileName = Path.GetFileName(entryKey);
                    var destDir = Path.GetDirectoryName(entryKey) ?? string.Empty;
                    var fullDestDir = Path.Combine(extractDir, destDir);
                    if (!Directory.Exists(fullDestDir))
                        Directory.CreateDirectory(fullDestDir);
                    entry.WriteToFile(Path.Combine(fullDestDir, fileName), new SharpCompress.Common.ExtractionOptions { Overwrite = true });
                }

                foreach (var file in Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relativePath = Path.GetRelativePath(extractDir, file);
                    var targetPath = Path.Combine(installPath, relativePath);
                    var targetFileDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetFileDir) && !Directory.Exists(targetFileDir))
                        Directory.CreateDirectory(targetFileDir);
                    File.Copy(file, targetPath, true);
                }
            }
            finally
            {
                try { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true); } catch { }
            }

            progress?.Report(new DeploymentProgress
            {
                StepName = "DeltaUpdate",
                Kind = DeploymentStepKind.Extract,
                ProgressValue = 1.0,
                Message = _localizationService["Step_ExtractComplete"],
            });
        }
        finally
        {
            try { if (File.Exists(deltaPackagePath)) File.Delete(deltaPackagePath); } catch { }
        }
    }
}
