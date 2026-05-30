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
    public InstallerService(
        IReleaseApiService releaseApiService,
        IFileService fileService,
        IDownloadService downloadService,
        IDialogService dialogService,
        IHttpService httpService,
        IProcessService processService,
        IPlatformDetector platformDetector,
        IPlatformAdapter platformAdapter)
    {
        _releaseApiService = releaseApiService;
        _fileService = fileService;
        _downloadService = downloadService;
        _dialogService = dialogService;
        _httpService = httpService;
        _processService = processService;
        _platformDetector = platformDetector;
        _platformAdapter = platformAdapter;
    }

    /// <inheritdoc/>
    public Task<InstallerConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var installDir = _platformAdapter.GetDefaultInstallDirectory(AppName);
        var currVersion = TryGetInstalledVersion(installDir);
        var isUpdate = currVersion is not null;

        var config = new InstallerConfig
        {
            Version = GetCurrentVersion(),
            IsUpdate = isUpdate,
            IsOfflineMode = false,
            CurrVersion = currVersion,
            InstallPath = installDir,
        };

        return Task.FromResult(config);
    }

    /// <inheritdoc/>
    public async Task<DeploymentResult> DeployAsync(DeploymentConfiguration configuration, IProgress<DeploymentProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var installDir = configuration.InstallPath ?? _platformAdapter.GetDefaultInstallDirectory(AppName);
        var tempDir = _platformAdapter.GetTempDirectory();
        var packageFileName = configuration.PackageFileName ?? "UotanToolbox.zip";
        var packagePath = Path.Combine(tempDir, packageFileName);

        var pipeline = new DeploymentPipeline();

        if (!configuration.OfflineMode)
        {
            var downloadUrl = configuration.SelectedMirrorUrl ?? configuration.PackageSources.FirstOrDefault()?.Url ?? string.Empty;
            pipeline.AddStep(new DownloadStep(downloadUrl, packagePath, _downloadService));

            if (!string.IsNullOrEmpty(configuration.Sha256))
            {
                pipeline.AddStep(new VerifyStep(packagePath, configuration.Sha256, _fileService));
            }
        }

        pipeline.AddStep(new ExtractStep(packagePath, installDir, configuration.FileRules));

        if (configuration.CreateDesktopShortcut)
        {
            var exePath = FindExecutable(installDir);
            if (exePath is not null)
            {
                pipeline.AddStep(new CreateShortcutStep(AppName, exePath, _platformAdapter));
            }
        }

        if (configuration.LaunchAfterInstall)
        {
            var exePath = FindExecutable(installDir);
            if (exePath is not null)
            {
                pipeline.AddStep(new LaunchStep(exePath, _platformAdapter));
            }
        }

        var result = await pipeline.ExecuteAsync(progress, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            try { File.Delete(packagePath); } catch { }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task SelfUpdateAsync(CancellationToken cancellationToken = default)
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

        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine the current executable path.");
        var outdatedPath = Path.ChangeExtension(exePath, ".old");

        try { File.Delete(outdatedPath); } catch { }

        var newInstallerBytes = await client.GetByteArrayAsync(asset.BrowserDownloadUrl, cancellationToken).ConfigureAwait(false);

        var renameSuccess = false;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            if (attempt > 1)
            {
                await Task.Delay(100 * attempt, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                File.Move(exePath, outdatedPath);
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
                await File.WriteAllBytesAsync(exePath, newInstallerBytes, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try { File.Move(outdatedPath, exePath); } catch { }
                throw;
            }
        }
        else
        {
            await File.WriteAllBytesAsync(exePath, newInstallerBytes, cancellationToken).ConfigureAwait(false);
        }

        _platformAdapter.RequestElevation(exePath);
        Environment.Exit(0);
    }

    /// <inheritdoc/>
    public void LaunchAndExit(string? installPath = null)
    {
        var installDir = installPath ?? _platformAdapter.GetDefaultInstallDirectory(AppName);
        var exePath = FindExecutable(installDir);

        if (exePath is not null && File.Exists(exePath))
        {
            try
            {
                _platformAdapter.LaunchApplication(exePath);
            }
            catch (Exception)
            {
            }
        }

        Environment.Exit(0);
    }

    /// <inheritdoc/>
    public async Task CreateDesktopShortcutAsync(string? installPath = null, CancellationToken cancellationToken = default)
    {
        var installDir = installPath ?? _platformAdapter.GetDefaultInstallDirectory(AppName);
        var exePath = FindExecutable(installDir);

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

    private static string? TryGetInstalledVersion(string installDir)
    {
        var exePath = FindExecutable(installDir);
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

    private static string? FindExecutable(string directory)
    {
        if (!Directory.Exists(directory)) return null;

        var exeFiles = Directory.GetFiles(directory, "UotanToolbox*.exe", SearchOption.TopDirectoryOnly);
        if (exeFiles.Length > 0) return exeFiles[0];

        var allExeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
        return allExeFiles.Length > 0 ? allExeFiles[0] : null;
    }

    private static string GetCurrentVersion()
    {
        var version = Environment.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.0";
    }
}
