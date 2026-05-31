using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UotanInstaller.App.Models;
using UotanInstaller.App.Services;
using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.ViewModels;

/// <summary>
/// <para>主窗口视图模型，管理安装程序的流程和所有 UI 状态。</para>
/// Main window view model that manages the installer's flow and all UI state.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IInstallerService? _installerService;
    private readonly IReleaseApiService? _releaseApiService;
    private readonly IDownloadService? _downloadService;
    private readonly IDialogService? _dialogService;
    private readonly ILocalizationService? _localizationService;
    private readonly IPlatformAdapter? _platformAdapter;
    private readonly IPlatformDetector? _platformDetector;

    private CancellationTokenSource? _installCts;

    /// <summary>
    /// <para>初始化 MainWindowViewModel 的新实例（设计时使用）。</para>
    /// Initializes a new instance of MainWindowViewModel for design-time use.
    /// </summary>
    public MainWindowViewModel() { }

    /// <summary>
    /// <para>使用依赖注入服务初始化 MainWindowViewModel 的新实例。</para>
    /// Initializes a new instance of MainWindowViewModel with dependency-injected services.
    /// </summary>
    /// <param name="installerService">
    /// <para>安装器核心服务。</para>
    /// The installer core service.
    /// </param>
    /// <param name="releaseApiService">
    /// <para>发布 API 服务。</para>
    /// The Release API service.
    /// </param>
    /// <param name="downloadService">
    /// <para>下载服务。</para>
    /// The download service.
    /// </param>
    /// <param name="dialogService">
    /// <para>对话框服务。</para>
    /// The dialog service.
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务。</para>
    /// The localization service.
    /// </param>
    /// <param name="platformAdapter">
    /// <para>平台适配器。</para>
    /// The platform adapter.
    /// </param>
    /// <param name="platformDetector">
    /// <para>平台检测器。</para>
    /// The platform detector.
    /// </param>
    public MainWindowViewModel(
        IInstallerService installerService,
        IReleaseApiService releaseApiService,
        IDownloadService downloadService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IPlatformAdapter platformAdapter,
        IPlatformDetector platformDetector)
    {
        _installerService = installerService;
        _releaseApiService = releaseApiService;
        _downloadService = downloadService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _platformAdapter = platformAdapter;
        _platformDetector = platformDetector;
    }

    #region Step Visibility

    /// <summary>
    /// <para>获取当前是否为 Eula 步骤。</para>
    /// Gets whether the current step is Eula.
    /// </summary>
    public bool IsEulaStep => CurrentStep == InstallStep.Eula;

    /// <summary>
    /// <para>获取当前是否为 ChooseMirror 步骤。</para>
    /// Gets whether the current step is ChooseMirror.
    /// </summary>
    public bool IsChooseMirrorStep => CurrentStep == InstallStep.ChooseMirror;

    /// <summary>
    /// <para>获取当前是否为 Installing 步骤。</para>
    /// Gets whether the current step is Installing.
    /// </summary>
    public bool IsInstallingStep => CurrentStep == InstallStep.Installing;

    /// <summary>
    /// <para>获取当前是否为 Finish 步骤。</para>
    /// Gets whether the current step is Finish.
    /// </summary>
    public bool IsFinishStep => CurrentStep == InstallStep.Finish;

    /// <summary>
    /// <para>获取当前是否为 AlreadyInstalled 步骤。</para>
    /// Gets whether the current step is AlreadyInstalled.
    /// </summary>
    public bool IsAlreadyInstalledStep => CurrentStep == InstallStep.AlreadyInstalled;

    #endregion

    #region Current Step

    [ObservableProperty]
    private InstallStep _currentStep = InstallStep.Eula;

    partial void OnCurrentStepChanged(InstallStep value)
    {
        OnPropertyChanged(nameof(IsEulaStep));
        OnPropertyChanged(nameof(IsChooseMirrorStep));
        OnPropertyChanged(nameof(IsInstallingStep));
        OnPropertyChanged(nameof(IsFinishStep));
        OnPropertyChanged(nameof(IsAlreadyInstalledStep));
    }

    #endregion

    #region Eula Step

    [ObservableProperty]
    private bool _isAgreeEula;

    [ObservableProperty]
    private bool _isCreateShortcut = true;

    #endregion

    #region ChooseMirror Step

    [ObservableProperty]
    private ObservableCollection<GenericPatchPackageMirror> _mirrors = [];

    [ObservableProperty]
    private GenericPatchPackageMirror? _selectedMirror;

    [ObservableProperty]
    private string _patchVersion = string.Empty;

    [ObservableProperty]
    private string _patchSha256 = string.Empty;

    [ObservableProperty]
    private bool _isLoadingMirrors;

    [ObservableProperty]
    private bool _isSpeedTesting;

    #endregion

    #region Installing Step

    [ObservableProperty]
    private double _installProgressValue;

    [ObservableProperty]
    private string _installProgressText = string.Empty;

    [ObservableProperty]
    private string _installStatusText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<InstallSubStepItem> _installSubSteps = [];

    [ObservableProperty]
    private bool _isInstallFailed;

    [ObservableProperty]
    private int _retryCount;

    #endregion

    #region Finish Step

    [ObservableProperty]
    private bool _isInstallSuccess = true;

    #endregion

    #region AlreadyInstalled Step

    [ObservableProperty]
    private string _installedVersion = string.Empty;

    #endregion

    #region Config

    [ObservableProperty]
    private string _versionText = string.Empty;

    [ObservableProperty]
    private bool _isUpdate;

    [ObservableProperty]
    private bool _isOfflineMode;

    [ObservableProperty]
    private string? _installPath;

    /// <summary>
    /// <para>获取安装按钮的显示文本。</para>
    /// Gets the display text for the install button.
    /// </summary>
    public string InstallButtonText => IsUpdate ? (_localizationService?["Update"] ?? "更新") : "安装";

    partial void OnIsUpdateChanged(bool value) => OnPropertyChanged(nameof(InstallButtonText));

    /// <summary>
    /// <para>浏览安装路径命令，打开文件夹选择对话框。</para>
    /// Browse install path command that opens a folder selection dialog.
    /// </summary>
    [RelayCommand]
    private async Task BrowseInstallPathAsync()
    {
        if (_dialogService is null) return;

        var selectedPath = await _dialogService.BrowseFolderAsync("选择安装路径", InstallPath);
        if (selectedPath is not null)
        {
            InstallPath = selectedPath;
        }
    }

    /// <summary>
    /// <para>开始安装命令，从 Eula 步骤直接进入镜像选择。</para>
    /// Start command that proceeds from the Eula step directly to mirror selection.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        CurrentStep = InstallStep.ChooseMirror;
        await LoadMirrorsAsync();
    }

    private bool CanStart => IsAgreeEula;

    partial void OnIsAgreeEulaChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task SpeedTestAsync()
    {
        if (_downloadService is null) return;

        IsSpeedTesting = true;
        try
        {
            var urls = Mirrors.Select(m => m.Url).ToList();
            var results = await _downloadService.BatchSpeedTestAsync(urls);

            foreach (var mirror in Mirrors)
            {
                if (results.TryGetValue(mirror.Url, out var speed))
                {
                    mirror.Speed = speed;
                }
            }

            var sorted = Mirrors.OrderByDescending(m => m.Speed ?? -1).ToList();

            var filtered = sorted.Where(m =>
                m.Url.Contains("github.com/", StringComparison.OrdinalIgnoreCase) ||
                (m.Speed is > 0.1)).ToList();

            if (filtered.Count == 0)
            {
                filtered = sorted;
            }

            Mirrors = new ObservableCollection<GenericPatchPackageMirror>(filtered);

            if (Mirrors.Count > 0)
            {
                var fastest = Mirrors.FirstOrDefault(m => m.Speed > 0) ?? Mirrors[0];
                SelectedMirror = fastest;
            }
        }
        catch
        {
        }
        finally
        {
            IsSpeedTesting = false;
        }
    }

    /// <summary>
    /// <para>安装命令，基于部署配置执行完整的部署流程。</para>
    /// Install command that executes the complete deployment process based on the deployment configuration.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        if (_installerService is null || _dialogService is null || SelectedMirror is null) return;

        CurrentStep = InstallStep.Installing;
        IsInstallFailed = false;
        InstallProgressValue = 0;
        InstallStatusText = _localizationService?["PrepareInstall"] ?? "准备安装...";

        InstallSubSteps =
        [
            new() { DisplayName = _localizationService?["Step_Download"] ?? "下载安装包", Kind = InstallProgressKind.Downloading },
            new() { DisplayName = _localizationService?["Step_Verify"] ?? "校验文件完整性", Kind = InstallProgressKind.Verifying },
            new() { DisplayName = _localizationService?["Step_Extract"] ?? "安装应用", Kind = InstallProgressKind.Installing },
        ];

        _installCts = new CancellationTokenSource();

        try
        {
            if (_platformDetector is not null)
            {
                try
                {
                    var ruleEngine = new DeploymentRuleEngine(_platformDetector);
                    var constraints = new[]
                    {
                        new PlatformConstraint
                        {
                            RequiredOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PlatformOS.Windows
                                       : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformOS.macOS
                                       : PlatformOS.Linux,
                            MinimumDiskSpaceBytes = 500L * 1024 * 1024,
                        },
                    };
                    var checkResult = await ruleEngine.CheckPlatformConstraintsAsync(constraints, _installCts.Token, InstallPath).ConfigureAwait(false);
                    if (!checkResult)
                    {
                        IsInstallFailed = true;
                        InstallStatusText = _localizationService?["PlatformCheckFailed"] ?? "平台检查未通过";
                        await (_dialogService?.ShowErrorAsync(_localizationService?["PlatformCheckFailed"] ?? "平台检查未通过", _localizationService?["DiskSpaceInsufficient"] ?? "磁盘空间不足") ?? Task.CompletedTask);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    IsInstallFailed = true;
                    InstallStatusText = $"{_localizationService?["PlatformCheckFailed"] ?? "平台检查未通过"}: {ex.Message}";
                    return;
                }
            }

            var configuration = new DeploymentConfiguration
            {
                AppName = "UotanToolboxNT",
                InstallPath = InstallPath,
                SelectedMirrorUrl = SelectedMirror.Url,
                Sha256 = PatchSha256,
                OfflineMode = IsOfflineMode,
                CreateDesktopShortcut = IsCreateShortcut,
                LaunchAfterInstall = false,
            };

            var progress = new Progress<DeploymentProgress>(OnDeploymentProgress);
            var result = await _installerService.DeployAsync(
                configuration,
                progress,
                _installCts.Token);

            if (result.IsSuccess)
            {
                IsInstallSuccess = true;
                CurrentStep = InstallStep.Finish;
            }
            else
            {
                IsInstallFailed = true;
                InstallStatusText = result.ErrorMessage ?? (_localizationService?["InstallFailed"] ?? "安装失败");
            }
        }
        catch (OperationCanceledException)
        {
            IsInstallFailed = true;
            InstallStatusText = _localizationService?["InstallCancelled"] ?? "安装已取消";
        }
        catch (Exception ex)
        {
            IsInstallFailed = true;
            InstallStatusText = $"{_localizationService?["InstallFailed"] ?? "安装失败"}: {ex.Message}";
            await _dialogService.ShowErrorAsync(_localizationService?["InstallFailed"] ?? "安装失败", ex.Message);
        }
        finally
        {
            _installCts?.Dispose();
            _installCts = null;
        }
    }

    private bool CanInstall => SelectedMirror is not null;

    partial void OnSelectedMirrorChanged(GenericPatchPackageMirror? value) => InstallCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// <para>启动柚坛工具箱命令。</para>
    /// Launch the UotanToolbox application command.
    /// </summary>
    [RelayCommand]
    private void LaunchApp()
    {
        _installerService?.LaunchAndExit(InstallPath);
    }

    /// <summary>
    /// <para>打开安装目录命令，使用系统文件管理器打开安装路径。</para>
    /// Open install directory command that opens the install path in the system file explorer.
    /// </summary>
    [RelayCommand]
    private async Task OpenInstallDirectoryAsync()
    {
        if (_platformAdapter is null || string.IsNullOrEmpty(InstallPath)) return;

        try
        {
            await _platformAdapter.OpenInFileExplorerAsync(InstallPath).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    /// <summary>
    /// <para>重试安装命令，最多允许重试 3 次。</para>
    /// Retry install command, allowing up to 3 retries.
    /// </summary>
    [RelayCommand]
    private async Task RetryAsync()
    {
        if (_installerService is null || _dialogService is null || SelectedMirror is null) return;

        if (RetryCount >= 3)
        {
            await _dialogService.ShowErrorAsync(
                _localizationService?["InstallFailed"] ?? "安装失败",
                "多次重试失败，请检查网络连接或联系支持。");
            return;
        }

        RetryCount++;
        await InstallAsync();
    }

    /// <summary>
    /// <para>重新安装命令，返回镜像选择步骤。</para>
    /// Reinstall command that navigates back to the mirror selection step.
    /// </summary>
    [RelayCommand]
    private void Reinstall()
    {
        CurrentStep = InstallStep.ChooseMirror;
        _ = LoadMirrorsAsync();
    }

    /// <summary>
    /// <para>初始化安装器配置。</para>
    /// Initialize the installer configuration.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_installerService is null) return;

        try
        {
            var config = await _installerService.GetConfigAsync();
            VersionText = $"v{config.Version}";
            IsUpdate = config.IsUpdate;
            IsOfflineMode = config.IsOfflineMode;
            InstallPath = string.IsNullOrEmpty(config.InstallPath) ? null : config.InstallPath;

            if (config.CurrVersion is not null)
            {
                InstalledVersion = config.CurrVersion;
                CurrentStep = InstallStep.AlreadyInstalled;
            }
        }
        catch
        {
            VersionText = _localizationService?["VersionUnavailable"] ?? "获取失败";
        }
    }

    /// <summary>
    /// <para>检查是否有新版本可用，并在用户确认后执行自更新。</para>
    /// Checks for a new version available and performs a self-update after user confirmation.
    /// </summary>
    public async Task CheckForSelfUpdateAsync()
    {
        if (_installerService is null || _dialogService is null) return;

        try
        {
            var hasUpdate = await _releaseApiService!.CheckSelfUpdateAsync(
                System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0",
                default).ConfigureAwait(false);

            if (!hasUpdate) return;

            var updateText = _localizationService?["Update"] ?? "更新";
            var confirmed = await _dialogService.ShowConfirmAsync(updateText, "检测到新版本，是否立即更新？").ConfigureAwait(false);
            if (!confirmed) return;

            await _installerService.SelfUpdateAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// <para>退出前清理临时文件。</para>
    /// Clean up temporary files before exit.
    /// </summary>
    public async Task CleanupBeforeExitAsync()
    {
        if (_installerService is null) return;

        try
        {
            await _installerService.CleanupTempFilesAsync();
        }
        catch
        {
        }
    }

    private async Task LoadMirrorsAsync()
    {
        if (_releaseApiService is null || _dialogService is null) return;

        IsLoadingMirrors = true;
        try
        {
            var patchData = await _releaseApiService.GetPatchDataAsync();
            PatchVersion = patchData.Version;
            PatchSha256 = patchData.Sha256;
            Mirrors = new ObservableCollection<GenericPatchPackageMirror>(patchData.Mirrors);

            if (Mirrors.Count > 0)
            {
                SelectedMirror = Mirrors[0];
            }

            await SpeedTestAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("加载镜像列表失败", ex.Message);
        }
        finally
        {
            IsLoadingMirrors = false;
        }
    }

    private void OnDeploymentProgress(DeploymentProgress progress)
    {
        InstallProgressValue = progress.ProgressValue;
        InstallProgressText = progress.ProgressValue > 0 ? $"{(int)(progress.ProgressValue * 100)}%" : string.Empty;
        InstallStatusText = progress.Message;

        var targetKind = progress.Kind switch
        {
            DeploymentStepKind.Download => InstallProgressKind.Downloading,
            DeploymentStepKind.Verify => InstallProgressKind.Verifying,
            DeploymentStepKind.Extract => InstallProgressKind.Installing,
            DeploymentStepKind.Configure => InstallProgressKind.Installing,
            DeploymentStepKind.CreateShortcut => InstallProgressKind.Installing,
            DeploymentStepKind.Launch => InstallProgressKind.Installing,
            _ => InstallProgressKind.Installing,
        };

        var activeIndex = -1;
        for (var i = 0; i < InstallSubSteps.Count; i++)
        {
            if (InstallSubSteps[i].Kind == targetKind)
            {
                activeIndex = i;
                break;
            }
        }

        for (var i = 0; i < InstallSubSteps.Count; i++)
        {
            var subStep = InstallSubSteps[i];
            if (i == activeIndex)
            {
                subStep.IsActive = true;
                subStep.IsCompleted = false;
                subStep.IsFailed = false;
            }
            else if (i < activeIndex)
            {
                subStep.IsCompleted = true;
                subStep.IsActive = false;
                subStep.IsFailed = false;
            }
        }

        if (progress.ProgressValue >= 1.0 && progress.Kind == DeploymentStepKind.Extract)
        {
            foreach (var subStep in InstallSubSteps)
            {
                subStep.IsCompleted = true;
                subStep.IsActive = false;
            }
        }

        OnPropertyChanged(nameof(InstallSubSteps));
    }

    /// <summary>
    /// <para>处理窗口关闭事件，根据当前步骤显示确认对话框。</para>
    /// Handles the window closing event, showing a confirmation dialog based on the current step.
    /// </summary>
    /// <returns>
    /// <para>如果用户确认退出返回 true，否则返回 false。</para>
    /// True if the user confirms to exit; otherwise false.
    /// </returns>
    public async Task<bool> HandleClosingAsync()
    {
        if (_dialogService is null) return true;

        if (CurrentStep == InstallStep.Installing)
        {
            return await _dialogService.ShowConfirmAsync("提示", "安装尚未完成，确定要退出安装吗？");
        }

        return await _dialogService.ShowConfirmAsync("提示", "确定要退出安装吗？");
    }

    #endregion
}

/// <summary>
/// <para>表示安装子步骤的 UI 项。</para>
/// Represents a UI item for an installation sub-step.
/// </summary>
public sealed class InstallSubStepItem : ObservableObject
{
    /// <summary>
    /// <para>获取或设置子步骤的显示名称。</para>
    /// Gets or sets the display name of the sub-step.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置子步骤对应的进度类型。</para>
    /// Gets or sets the progress kind corresponding to the sub-step.
    /// </summary>
    public InstallProgressKind Kind { get; set; }

    /// <summary>
    /// <para>获取或设置子步骤是否已完成。</para>
    /// Gets or sets whether the sub-step is completed.
    /// </summary>
    public bool IsCompleted { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// <para>获取或设置子步骤是否正在进行。</para>
    /// Gets or sets whether the sub-step is currently active.
    /// </summary>
    public bool IsActive { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// <para>获取或设置子步骤是否失败。</para>
    /// Gets or sets whether the sub-step has failed.
    /// </summary>
    public bool IsFailed { get; set => SetProperty(ref field, value); }
}
