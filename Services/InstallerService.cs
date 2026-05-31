using System.Text.Json;
using UotanInstaller.App.Models;
using UotanInstaller.App.Services.Deployment;
using UotanInstaller.App.Services.Deployment.Steps;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装器核心服务实现，协调所有其他服务完成安装流程</para>
/// Installer core service implementation that coordinates all other services to complete the installation process
/// </summary>
public sealed class InstallerService : IInstallerService
{
    private const string AppName = "UotanToolbox";

    private readonly IReleaseApiService _releaseApiService;
    private readonly IFileService _fileService;
    private readonly IDownloadService _downloadService;
    private readonly IDialogService _dialogService;
    private readonly IHttpService _httpService;
    private readonly IProcessService _processService;
    private readonly IPlatformDetector _platformDetector;
    private readonly IPlatformAdapter _platformAdapter;
    private readonly ILocalizationService _localizationService;
    private readonly IGitHubMirrorService _gitHubMirrorService;

    /// <summary>
    /// <para>初始化 InstallerService 实例</para>
    /// Initialize the InstallerService instance
    /// </summary>
    /// <param name="releaseApiService">
    /// <para>API 服务</para>
    /// The API service
    /// </param>
    /// <param name="fileService">
    /// <para>文件服务</para>
    /// The file service
    /// </param>
    /// <param name="downloadService">
    /// <para>下载服务</para>
    /// The download service
    /// </param>
    /// <param name="dialogService">
    /// <para>对话框服务</para>
    /// The dialog service
    /// </param>
    /// <param name="httpService">
    /// <para>HTTP 服务</para>
    /// The HTTP service
    /// </param>
    /// <param name="processService">
    /// <para>进程管理服务</para>
    /// The process management service
    /// </param>
    /// <param name="platformDetector">
    /// <para>平台检测器</para>
    /// The platform detector
    /// </param>
    /// <param name="platformAdapter">
    /// <para>平台适配器</para>
    /// The platform adapter
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务</para>
    /// The localization service
    /// </param>
    /// <param name="gitHubMirrorService">
    /// <para>GitHub 镜像服务</para>
    /// The GitHub mirror service
    /// </param>
    public InstallerService(
        IReleaseApiService releaseApiService,
        IFileService fileService,
        IDownloadService downloadService,
        IDialogService dialogService,
        IHttpService httpService,
        IProcessService processService,
        IPlatformDetector platformDetector,
        IPlatformAdapter platformAdapter,
        ILocalizationService localizationService,
        IGitHubMirrorService gitHubMirrorService)
    {
        _releaseApiService = releaseApiService;
        _fileService = fileService;
        _downloadService = downloadService;
        _dialogService = dialogService;
        _httpService = httpService;
        _processService = processService;
        _platformDetector = platformDetector;
        _platformAdapter = platformAdapter;
        _localizationService = localizationService;
        _gitHubMirrorService = gitHubMirrorService;
    }

    /// <inheritdoc/>
    public async Task<InstallerConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var installDir = _platformAdapter.GetDefaultInstallDirectory(AppName);
        var currVersion = await TryGetInstalledVersionAsync(installDir).ConfigureAwait(false);
        var isUpdate = currVersion is not null;

        var config = new InstallerConfig
        {
            Version = GetCurrentVersion(),
            IsUpdate = isUpdate,
            IsOfflineMode = false,
            CurrVersion = currVersion,
            InstallPath = installDir,
        };

        return config;
    }

    /// <inheritdoc/>
    public async Task<DeploymentResult> DeployAsync(DeploymentConfiguration configuration, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var installDir = configuration.InstallPath ?? _platformAdapter.GetDefaultInstallDirectory(AppName);
        var tempDir = _platformAdapter.GetTempDirectory();
        var packageFileName = configuration.PackageFileName ?? "UotanToolbox.zip";
        var packagePath = Path.Combine(tempDir, packageFileName);

        var pipeline = new DeploymentPipeline(_localizationService);

        if (!configuration.OfflineMode)
        {
            var downloadUrl = configuration.SelectedMirrorUrl ?? configuration.PackageSources.FirstOrDefault()?.Url ?? string.Empty;
            pipeline.AddStep(new DownloadStep(downloadUrl, packagePath, _downloadService, _localizationService));

            if (!string.IsNullOrEmpty(configuration.Sha256))
            {
                pipeline.AddStep(new VerifyStep(packagePath, configuration.Sha256, _fileService, _localizationService));
            }
        }

        pipeline.AddStep(new ExtractStep(packagePath, installDir, _localizationService, configuration.FileRules));

        if (configuration.CreateDesktopShortcut)
        {
            var exePath = await _platformAdapter.FindExecutableAsync(installDir).ConfigureAwait(false);
            if (exePath is not null)
            {
                pipeline.AddStep(new CreateShortcutStep(AppName, exePath, _platformAdapter, _localizationService));
            }
        }

        if (configuration.LaunchAfterInstall)
        {
            var exePath = await _platformAdapter.FindExecutableAsync(installDir).ConfigureAwait(false);
            if (exePath is not null)
            {
                pipeline.AddStep(new LaunchStep(exePath, _platformAdapter, _localizationService));
            }
        }

        var result = await pipeline.ExecuteAsync(progress, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            try { File.Delete(packagePath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Warning: Failed to delete package file: {ex.Message}"); }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task SelfUpdateAsync(IProgress<(long Downloaded, long Total)>? downloadProgress = null, CancellationToken cancellationToken = default)
    {
        var hasUpdate = await _releaseApiService.CheckSelfUpdateAsync(GetCurrentVersion(), cancellationToken).ConfigureAwait(false);
        if (!hasUpdate) return;

        var client = _httpService.Client;
        var url = "https://api.github.com/repos/Uotan-Dev/UotanToolboxNT/releases/latest";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var release = JsonSerializer.Deserialize(json, AppJsonContext.Default.GitHubRelease);

        var asset = release?.Assets?.FirstOrDefault(a =>
            a.Name?.Contains("Deployment", StringComparison.OrdinalIgnoreCase) == true ||
            a.Name?.Contains("Installer", StringComparison.OrdinalIgnoreCase) == true);

        if (asset?.BrowserDownloadUrl is null) return;

        var downloadUrl = asset.BrowserDownloadUrl;
        var mirrors = _gitHubMirrorService.GetMirrorSites();
        var enabledMirror = mirrors.FirstOrDefault(m => m.IsEnabled);
        if (enabledMirror is not null)
        {
            var mirrorUrl = _gitHubMirrorService.CreateMirrorUrl(enabledMirror, downloadUrl);
            if (!string.IsNullOrEmpty(mirrorUrl)) downloadUrl = mirrorUrl;
        }

        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine the current executable path.");
        var backupPath = exePath + ".bak";
        var tempDownloadPath = exePath + ".tmp";

        try { File.Delete(backupPath); } catch { }
        try { File.Delete(tempDownloadPath); } catch { }

        using (var downloadResponse = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
        {
            downloadResponse.EnsureSuccessStatusCode();

            var totalBytes = downloadResponse.Content.Headers.ContentLength ?? 0;
            using var contentStream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var fileStream = new FileStream(tempDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalDownloaded = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalDownloaded += bytesRead;

                if (totalBytes > 0)
                {
                    downloadProgress?.Report((totalDownloaded, totalBytes));
                }
            }
        }

        if (asset.Digest is not null)
        {
            var digestParts = asset.Digest.Split(':');
            if (digestParts.Length == 2)
            {
                var expectedHash = digestParts[1];
                var actualHash = await _fileService.ComputeSha256Async(tempDownloadPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(tempDownloadPath); } catch { }
                    throw new VerificationException(
                        $"Self-update verification failed. Expected: {expectedHash}, Actual: {actualHash}",
                        expectedHash,
                        actualHash,
                        tempDownloadPath);
                }
            }
        }

        try { File.Delete(backupPath); } catch { }

        var renameSuccess = false;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            if (attempt > 1)
            {
                await Task.Delay(100 * attempt, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                File.Move(exePath, backupPath);
                renameSuccess = true;
                break;
            }
            catch (FileNotFoundException)
            {
                renameSuccess = true;
                break;
            }
            catch (Exception)
            {
            }
        }

        if (renameSuccess)
        {
            try
            {
                File.Move(tempDownloadPath, exePath);
            }
            catch
            {
                try { File.Move(backupPath, exePath); } catch { }
                throw;
            }

            try { File.Delete(backupPath); } catch { }
        }
        else
        {
            try
            {
                File.Move(tempDownloadPath, exePath);
            }
            catch
            {
                try { File.Delete(tempDownloadPath); } catch { }
                throw;
            }
        }

        _platformAdapter.RequestElevation(exePath);
        Environment.Exit(0);
    }

    /// <inheritdoc/>
    public void LaunchAndExit(string? installPath = null)
    {
        var installDir = installPath ?? _platformAdapter.GetDefaultInstallDirectory(AppName);
        var exePath = _platformAdapter.FindExecutableAsync(installDir).GetAwaiter().GetResult();

        if (exePath is not null && File.Exists(exePath))
        {
            try
            {
                _platformAdapter.LaunchApplication(exePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to launch application: {ex.Message}");
                var launchFailedText = _localizationService["LaunchFailed"];
                try
                {
                    _dialogService.ShowErrorAsync(launchFailedText, ex.Message).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }
        }

        Environment.Exit(0);
    }

    /// <inheritdoc/>
    public async Task CreateDesktopShortcutAsync(string? installPath = null, CancellationToken cancellationToken = default)
    {
        var installDir = installPath ?? _platformAdapter.GetDefaultInstallDirectory(AppName);
        var exePath = await _platformAdapter.FindExecutableAsync(installDir).ConfigureAwait(false);

        if (exePath is not null)
        {
            await _platformAdapter.CreateDesktopShortcutAsync(AppName, exePath, ct: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task CleanupTempFilesAsync(CancellationToken cancellationToken = default)
    {
        var tempDir = _platformAdapter.GetTempDirectory();
        await _fileService.CleanupTempFilesAsync(tempDir, "UotanToolbox.zip", cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> TryGetInstalledVersionAsync(string installDir)
    {
        var exePath = await _platformAdapter.FindExecutableAsync(installDir).ConfigureAwait(false);
        if (exePath is null || !File.Exists(exePath)) return null;

        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
            return versionInfo.FileVersion;
        }
        catch
        {
            return null;
        }
    }

    private static string GetCurrentVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is not null)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        return "0.0.0.0";
    }
}
