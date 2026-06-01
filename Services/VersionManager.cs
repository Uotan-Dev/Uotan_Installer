using SharpCompress.Archives;
using System.Text.Json;
using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>版本管理器实现，将版本记录存储在安装目录的 versions.json 文件中。</para>
/// Version manager implementation that stores version records in a versions.json file in the installation directory.
/// </summary>
public sealed class VersionManager : IVersionManager
{
    private static readonly string VersionFileName = "versions.json";

    private readonly IReleaseApiService _releaseApiService;
    private readonly IDownloadService _downloadService;
    private readonly ILocalizationService _localizationService;
    private readonly IPlatformAdapter _platformAdapter;
    private readonly IFileService _fileService;
    private readonly IComponentManager _componentManager;

    /// <summary>
    /// <para>初始化 VersionManager 实例。</para>
    /// Initializes a new instance of VersionManager.
    /// </summary>
    /// <param name="releaseApiService">
    /// <para>发布 API 服务。</para>
    /// The release API service.
    /// </param>
    /// <param name="downloadService">
    /// <para>下载服务。</para>
    /// The download service.
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务。</para>
    /// The localization service.
    /// </param>
    /// <param name="platformAdapter">
    /// <para>平台适配器。</para>
    /// The platform adapter.
    /// </param>
    /// <param name="fileService">
    /// <para>文件服务。</para>
    /// The file service.
    /// </param>
    /// <param name="componentManager">
    /// <para>组件管理器。</para>
    /// The component manager.
    /// </param>
    public VersionManager(
        IReleaseApiService releaseApiService,
        IDownloadService downloadService,
        ILocalizationService localizationService,
        IPlatformAdapter platformAdapter,
        IFileService fileService,
        IComponentManager componentManager)
    {
        _releaseApiService = releaseApiService;
        _downloadService = downloadService;
        _localizationService = localizationService;
        _platformAdapter = platformAdapter;
        _fileService = fileService;
        _componentManager = componentManager;
    }

    /// <inheritdoc/>
    public async Task SaveVersionRecordAsync(string installPath, VersionRecord record, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(installPath, VersionFileName);
        List<VersionRecord> records;

        if (File.Exists(filePath))
        {
            try
            {
                var existingJson = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                var existing = JsonSerializer.Deserialize(existingJson, AppJsonContext.Default.VersionRecordArray);
                records = existing?.ToList() ?? [];
            }
            catch
            {
                records = [];
            }
        }
        else
        {
            records = [];
        }

        records.Add(record);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(records.ToArray(), AppJsonContext.Default.VersionRecordArray);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<VersionRecord>> GetVersionHistoryAsync(string installPath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(installPath, VersionFileName);

        if (!File.Exists(filePath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var records = JsonSerializer.Deserialize(json, AppJsonContext.Default.VersionRecordArray);
            return records ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(string installPath, string targetVersion, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var history = await GetVersionHistoryAsync(installPath, cancellationToken).ConfigureAwait(false);
        var targetRecord = history.FirstOrDefault(r => r.Version == targetVersion)
            ?? throw new InvalidOperationException($"Version record not found: {targetVersion}");

        var patchData = await _releaseApiService.GetPatchDataAsync(targetRecord.Channel, cancellationToken).ConfigureAwait(false);

        var tempDir = _platformAdapter.GetTempDirectory();
        var packagePath = Path.Combine(tempDir, "UotanToolbox_rollback.zip");

        try
        {
            progress?.Report(new DeploymentProgress
            {
                StepName = "Rollback",
                Kind = DeploymentStepKind.Download,
                ProgressValue = 0.0,
                Message = "Downloading rollback package...",
            });

            await _downloadService.DownloadFileAsync(patchData.Mirrors[0].Url, packagePath, cancellationToken: cancellationToken).ConfigureAwait(false);

            progress?.Report(new DeploymentProgress
            {
                StepName = "Rollback",
                Kind = DeploymentStepKind.Extract,
                ProgressValue = 0.5,
                Message = "Applying rollback...",
            });

            if (File.Exists(packagePath))
            {
                using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(packagePath);
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entryKey = entry.Key ?? string.Empty;
                    var fileName = Path.GetFileName(entryKey);
                    var destDir = Path.GetDirectoryName(entryKey) ?? string.Empty;
                    var fullDestDir = Path.Combine(installPath, destDir);
                    if (!Directory.Exists(fullDestDir))
                        Directory.CreateDirectory(fullDestDir);
                    var destPath = Path.Combine(fullDestDir, fileName);
                    entry.WriteToFile(destPath, new SharpCompress.Common.ExtractionOptions { Overwrite = true });
                }
            }

            progress?.Report(new DeploymentProgress
            {
                StepName = "Rollback",
                Kind = DeploymentStepKind.Extract,
                ProgressValue = 1.0,
                Message = "Rollback completed.",
            });
        }
        finally
        {
            try { if (File.Exists(packagePath)) File.Delete(packagePath); } catch { }
        }
    }
}
