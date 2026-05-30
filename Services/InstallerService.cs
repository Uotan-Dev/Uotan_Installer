using SharpCompress.Archives;
using SharpCompress.Common;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装器核心服务实现，协调所有其他服务完成安装流程</para>
/// Installer core service implementation that coordinates all other services to complete the installation process
/// </summary>
public sealed class InstallerService : IInstallerService
{
    private const string AppName = "UotanToolbox";
    private const string DefaultInstallDir = "UotanToolboxNT";
    private const string PackageFileName = "UotanToolbox.zip";

    private readonly IReleaseApiService _releaseApiService;
    private readonly IFileService _fileService;
    private readonly IDownloadService _downloadService;
    private readonly IDialogService _dialogService;
    private readonly IHttpService _httpService;
    private readonly IProcessService _processService;

    /// <summary>
    /// <para>初始化 InstallerService 实例</para>
    /// Initialize the InstallerService instance
    /// </summary>
    /// <param name="releaseApiService">
    /// <para>API 服务</para>
    /// The API service
    /// </param>
    /// <param name="processService">
    /// <para>进程管理服务</para>
    /// The process management service
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
    public InstallerService(
        IReleaseApiService releaseApiService,
        IProcessService processService,
        IFileService fileService,
        IDownloadService downloadService,
        IDialogService dialogService,
        IHttpService httpService)
    {
        _releaseApiService = releaseApiService;
        _processService = processService;
        _fileService = fileService;
        _downloadService = downloadService;
        _dialogService = dialogService;
        _httpService = httpService;
    }

    /// <inheritdoc/>
    public async Task<InstallerConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var installDir = GetInstallDirectory();
        var currVersion = TryGetInstalledVersion(installDir);
        var isUpdate = currVersion is not null;

        return new InstallerConfig
        {
            Version = GetCurrentVersion(),
            IsUpdate = isUpdate,
            IsOfflineMode = false,
            CurrVersion = currVersion,
            InstallPath = GetInstallDirectory(),
        };
    }

    /// <inheritdoc/>
    public async Task<bool> StartInstallAsync(string mirrorUrl, string sha256, bool offlineMode, string? installPath = null, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var installDir = installPath ?? GetInstallDirectory();
            var tempDir = Path.GetTempPath();
            var packagePath = Path.Combine(tempDir, PackageFileName);

            if (!offlineMode)
            {
                progress?.Report(new InstallProgress { Kind = InstallProgressKind.Downloading, Message = "正在下载安装包..." });

                var downloadProgress = new Progress<(long Downloaded, long Total)>(p =>
                {
                    var percentage = p.Total > 0 ? (double)p.Downloaded / p.Total : 0;
                    progress?.Report(new InstallProgress { Kind = InstallProgressKind.Downloading, Message = "正在下载...", ProgressValue = percentage });
                });

                await _downloadService.MultiThreadedDownloadAsync(mirrorUrl, packagePath, Environment.ProcessorCount, downloadProgress, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(sha256))
                {
                    progress?.Report(new InstallProgress { Kind = InstallProgressKind.Verifying, Message = "正在校验文件完整性..." });
                    var actualHash = await _fileService.ComputeSha256Async(packagePath, cancellationToken).ConfigureAwait(false);
                    if (!string.Equals(actualHash, sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        progress?.Report(new InstallProgress { Kind = InstallProgressKind.Failed, Message = "文件校验失败！" });
                        return false;
                    }
                }
            }

            progress?.Report(new InstallProgress { Kind = InstallProgressKind.Installing, Message = "正在安装应用..." });

            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.OpenArchive(packagePath);
                var totalEntries = archive.Entries.Count(e => !e.IsDirectory);
                var extractedCount = 0;

                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var destinationPath = Path.GetFullPath(Path.Combine(installDir, entry.Key ?? string.Empty));

                    if (!destinationPath.StartsWith(installDir, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var dir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    entry.WriteToFile(destinationPath, new ExtractionOptions { Overwrite = true });
                    extractedCount++;

                    var percentage = (double)extractedCount / totalEntries;
                    progress?.Report(new InstallProgress { Kind = InstallProgressKind.Installing, Message = "正在解压文件...", ProgressValue = percentage });
                }
            }, cancellationToken).ConfigureAwait(false);

            try { File.Delete(packagePath); } catch { }

            progress?.Report(new InstallProgress { Kind = InstallProgressKind.Completed, Message = "安装完成" });
            return true;
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new InstallProgress { Kind = InstallProgressKind.Failed, Message = "安装已取消" });
            return false;
        }
        catch (Exception ex)
        {
            progress?.Report(new InstallProgress { Kind = InstallProgressKind.Failed, Message = ex.Message });
            return false;
        }
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

        _processService.RunElevated(exePath);
        Environment.Exit(0);
    }

    /// <inheritdoc/>
    public void LaunchAndExit(string? installPath = null)
    {
        var installDir = installPath ?? GetInstallDirectory();
        var exePath = FindExecutable(installDir);

        if (exePath is not null && File.Exists(exePath))
        {
            try
            {
                _processService.RunNormal(exePath);
            }
            catch (Exception)
            {
            }
        }

        Environment.Exit(0);
    }

    /// <inheritdoc/>
    public void CreateDesktopShortcut(string? installPath = null)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var lnkPath = Path.Combine(desktopPath, "柚坛工具箱.lnk");
        var installDir = installPath ?? GetInstallDirectory();
        var exePath = FindExecutable(installDir);

        if (exePath is null) return;

        if (!Directory.Exists(desktopPath))
        {
            Directory.CreateDirectory(desktopPath);
        }

        CreateShellShortcut(lnkPath, exePath);
    }

    /// <inheritdoc/>
    public async Task CleanupTempFilesAsync(CancellationToken cancellationToken = default)
    {
        var tempDir = Path.GetTempPath();
        await _fileService.CleanupTempFilesAsync(tempDir, PackageFileName, cancellationToken).ConfigureAwait(false);
    }

    private static string GetInstallDirectory()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(programFiles, DefaultInstallDir);
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

    private static void CreateShellShortcut(string lnkPath, string targetPath)
    {
        var shellLink = (IShellLinkW)new ShellLink();
        shellLink.SetPath(targetPath);
        shellLink.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);

        var persistFile = (IPersistFile)shellLink;
        persistFile.Save(lnkPath, false);

        Marshal.ReleaseComObject(persistFile);
        Marshal.ReleaseComObject(shellLink);
    }
}

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
[CoClass(typeof(ShellLinkClass))]
internal interface ShellLink : IShellLinkW;

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
internal class ShellLinkClass;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal interface IShellLinkW
{
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
    void GetIDList(out IntPtr ppidl);
    void SetIDList(IntPtr pidl);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    void GetHotkey(out short pwHotkey);
    void SetHotkey(short wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
    void Resolve(IntPtr hwnd, uint fFlags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("0000010b-0000-0000-C000-000000000046")]
internal interface IPersistFile
{
    void GetClassID(out Guid pClassID);
    void IsDirty();
    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
    void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}
