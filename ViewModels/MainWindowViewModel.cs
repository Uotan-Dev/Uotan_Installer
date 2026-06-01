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
    private readonly IChannelService? _channelService;
    private readonly IComponentManager? _componentManager;

    private CancellationTokenSource? _installCts;

    /// <summary>
    /// <para>请求关闭窗口的事件。</para>
    /// Event that requests the window to close.
    /// </summary>
    public event Action? RequestClose;

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
    /// <param name="channelService">
    /// <para>发布渠道服务。</para>
    /// The channel service.
    /// </param>
    /// <param name="componentManager">
    /// <para>组件管理器。</para>
    /// The component manager.
    /// </param>
    public MainWindowViewModel(
        IInstallerService installerService,
        IReleaseApiService releaseApiService,
        IDownloadService downloadService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IPlatformAdapter platformAdapter,
        IPlatformDetector platformDetector,
        IChannelService channelService,
        IComponentManager componentManager)
    {
        _installerService = installerService;
        _releaseApiService = releaseApiService;
        _downloadService = downloadService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _platformAdapter = platformAdapter;
        _platformDetector = platformDetector;
        _channelService = channelService;
        _componentManager = componentManager;

        if (_localizationService is not null)
        {
            _localizationService.LanguageChanged += OnLanguageChanged;
        }
    }

    private string L(string key, string fallback) => _localizationService?[key] ?? fallback;

    private void OnLanguageChanged(string _)
    {
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(EulaTitle));
        OnPropertyChanged(nameof(EulaDesc));
        OnPropertyChanged(nameof(CreateShortcutText));
        OnPropertyChanged(nameof(InstallPathText));
        OnPropertyChanged(nameof(BrowseText));
        OnPropertyChanged(nameof(AgreeEulaText));
        OnPropertyChanged(nameof(StartText));
        OnPropertyChanged(nameof(ChooseMirrorTitleText));
        OnPropertyChanged(nameof(ChooseMirrorDescText));
        OnPropertyChanged(nameof(SpeedTestText));
        OnPropertyChanged(nameof(InstallingText));
        OnPropertyChanged(nameof(RetryText));
        OnPropertyChanged(nameof(InstallCompleteText));
        OnPropertyChanged(nameof(InstallCompleteDescText));
        OnPropertyChanged(nameof(OpenInstallDirText));
        OnPropertyChanged(nameof(LaunchAppText));
        OnPropertyChanged(nameof(AlreadyInstalledText));
        OnPropertyChanged(nameof(AlreadyInstalledDescText));
        OnPropertyChanged(nameof(ReinstallText));
        OnPropertyChanged(nameof(InstallButtonText));
        OnPropertyChanged(nameof(ChannelLabelText));
        OnPropertyChanged(nameof(ChannelWarningText));
        OnPropertyChanged(nameof(ComponentSelectionText));
        OnPropertyChanged(nameof(TotalInstallSizeText));
        OnPropertyChanged(nameof(RollbackText));
        OnPropertyChanged(nameof(DeltaUpdateText));
        OnPropertyChanged(nameof(DeltaUpdateDescText));
        OnPropertyChanged(nameof(CloseText));
        OnPropertyChanged(nameof(CancelText));
        OnPropertyChanged(nameof(BackText));
        OnPropertyChanged(nameof(StepConfigText));
        OnPropertyChanged(nameof(StepDownloadText));
        OnPropertyChanged(nameof(StepInstallText));
        OnPropertyChanged(nameof(StepCompleteText));
        UpdateChannelDisplayNames();
    }

    #region Localization Properties

    public string AppTitle => L("AppTitle", "柚坛工具箱部署器");
    public string EulaTitle => L("EulaTitle", "用户协议");
    public string EulaDesc => L("EulaDesc", "请阅读并同意最终用户许可协议后继续安装。");
    public string CreateShortcutText => L("CreateShortcut", "创建桌面快捷方式");
    public string InstallPathText => L("InstallPath", "安装路径");
    public string BrowseText => L("Browse", "浏览");
    public string AgreeEulaText => L("AgreeEula", "我已阅读并同意最终用户许可协议");
    public string StartText => L("Start", "开始");
    public string ChooseMirrorTitleText => L("ChooseMirrorTitle", "选择镜像源");
    public string ChooseMirrorDescText => L("ChooseMirrorDesc", "请选择一个下载镜像源，推荐选择速度最快的镜像。");
    public string SpeedTestText => L("SpeedTest", "测速");
    public string InstallingText => L("Installing", "正在安装");
    public string RetryText => L("Retry", "重试");
    public string InstallCompleteText => L("InstallComplete", "安装完成");
    public string InstallCompleteDescText => L("InstallCompleteDesc", "UotanToolbox 已成功安装到您的设备上。");
    public string OpenInstallDirText => L("OpenInstallDir", "打开安装目录");
    public string LaunchAppText => L("LaunchApp", "启动柚坛工具箱");
    public string AlreadyInstalledText => L("AlreadyInstalled", "已安装");
    public string AlreadyInstalledDescText => L("AlreadyInstalledDesc", "UotanToolbox 已安装在您的设备上。");
    public string ReinstallText => L("Reinstall", "重新安装");

    public string ChannelLabelText => L("ChannelLabel", "发布渠道");

    /// <summary>
    /// <para>获取非正式版渠道的警告文本。</para>
    /// Gets the warning text for non-release channels.
    /// </summary>
    public string ChannelWarningText => SelectedChannel is not null && SelectedChannel.Channel != ReleaseChannel.Release
        ? L("Channel_Warning", "此渠道版本可能不稳定，仅建议测试用途。生产环境请使用正式版。")
        : string.Empty;

    /// <summary>
    /// <para>获取是否显示渠道警告。</para>
    /// Gets whether the channel warning is visible.
    /// </summary>
    public bool IsChannelWarningVisible => SelectedChannel is not null && SelectedChannel.Channel != ReleaseChannel.Release;

    /// <summary>
    /// <para>获取组件选择的本地化文本。</para>
    /// Gets the localized text for component selection.
    /// </summary>
    public string ComponentSelectionText => L("ComponentSelection", "组件选择");

    /// <summary>
    /// <para>获取格式化的总安装大小文本。</para>
    /// Gets the formatted total install size text.
    /// </summary>
    public string TotalInstallSizeText
    {
        get
        {
            var totalBytes = AvailableComponents.Where(c => c.IsSelected).Sum(c => c.Size);
            var sizeMB = totalBytes / 1024.0 / 1024.0;
            return sizeMB >= 1024 ? $"{sizeMB / 1024.0:F1} GB" : $"{sizeMB:F1} MB";
        }
    }

    /// <summary>
    /// <para>获取是否显示组件选择区域。</para>
    /// Gets whether the component selection area is visible.
    /// </summary>
    public bool HasComponents => AvailableComponents.Count > 0;

    /// <summary>
    /// <para>获取回滚按钮的本地化文本。</para>
    /// Gets the localized text for the rollback button.
    /// </summary>
    public string RollbackText => L("Rollback", "回滚到上一版本");

    /// <summary>
    /// <para>获取增量更新的本地化文本。</para>
    /// Gets the localized text for delta update.
    /// </summary>
    public string DeltaUpdateText => L("DeltaUpdate", "增量更新");

    /// <summary>
    /// <para>获取增量更新描述的本地化文本。</para>
    /// Gets the localized description text for delta update.
    /// </summary>
    public string DeltaUpdateDescText => L("DeltaUpdateDesc", "仅下载变更的文件以减少更新包大小");

    /// <summary>
    /// <para>获取关闭按钮的本地化文本。</para>
    /// Gets the localized text for the close button.
    /// </summary>
    public string CloseText => L("Close", "关闭");

    /// <summary>
    /// <para>获取取消按钮的本地化文本。</para>
    /// Gets the localized text for the cancel button.
    /// </summary>
    public string CancelText => L("Cancel", "取消");

    /// <summary>
    /// <para>获取返回按钮的本地化文本。</para>
    /// Gets the localized text for the back button.
    /// </summary>
    public string BackText => L("Back", "返回");

    /// <summary>
    /// <para>获取步骤指示器"配置"的本地化文本。</para>
    /// Gets the localized text for the "Config" step indicator.
    /// </summary>
    public string StepConfigText => L("StepConfig", "配置");

    /// <summary>
    /// <para>获取步骤指示器"下载"的本地化文本。</para>
    /// Gets the localized text for the "Download" step indicator.
    /// </summary>
    public string StepDownloadText => L("StepDownload", "下载");

    /// <summary>
    /// <para>获取步骤指示器"安装"的本地化文本。</para>
    /// Gets the localized text for the "Install" step indicator.
    /// </summary>
    public string StepInstallText => L("StepInstall", "安装");

    /// <summary>
    /// <para>获取步骤指示器"完成"的本地化文本。</para>
    /// Gets the localized text for the "Complete" step indicator.
    /// </summary>
    public string StepCompleteText => L("StepComplete", "完成");

    #endregion

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

    public bool IsStep2NotReached => !IsChooseMirrorStep && !IsStep2Completed;
    public bool IsStep3NotReached => !IsInstallingStep && !IsStep3Completed;
    public bool IsStep4NotReached => !IsFinishStep;

    public bool IsStepProgressVisible => CurrentStep is InstallStep.Eula or InstallStep.ChooseMirror or InstallStep.Installing or InstallStep.Finish;

    #endregion

    #region Current Step

    [ObservableProperty]
    private InstallStep _currentStep = InstallStep.Eula;

    /// <summary>
    /// <para>获取或设置步骤进度索引，用于步骤指示器显示。</para>
    /// Gets or sets the step progress index for the step indicator display.
    /// </summary>
    [ObservableProperty]
    private int _stepProgressIndex;

    partial void OnCurrentStepChanged(InstallStep value)
    {
        StepProgressIndex = value switch
        {
            InstallStep.Eula => 0,
            InstallStep.ChooseMirror => 1,
            InstallStep.Installing => 2,
            InstallStep.Finish => 3,
            InstallStep.AlreadyInstalled => 3,
            _ => -1,
        };

        OnPropertyChanged(nameof(IsEulaStep));
        OnPropertyChanged(nameof(IsChooseMirrorStep));
        OnPropertyChanged(nameof(IsInstallingStep));
        OnPropertyChanged(nameof(IsFinishStep));
        OnPropertyChanged(nameof(IsAlreadyInstalledStep));
        OnPropertyChanged(nameof(IsStep1Completed));
        OnPropertyChanged(nameof(IsStep2Completed));
        OnPropertyChanged(nameof(IsStep3Completed));
        OnPropertyChanged(nameof(IsStep2NotReached));
        OnPropertyChanged(nameof(IsStep3NotReached));
        OnPropertyChanged(nameof(IsStep4NotReached));
        OnPropertyChanged(nameof(IsStepProgressVisible));
    }

    /// <summary>
    /// <para>获取步骤1（配置）是否已完成。</para>
    /// Gets whether step 1 (Configure) is completed.
    /// </summary>
    public bool IsStep1Completed => StepProgressIndex > 0;

    /// <summary>
    /// <para>获取步骤2（下载）是否已完成。</para>
    /// Gets whether step 2 (Download) is completed.
    /// </summary>
    public bool IsStep2Completed => StepProgressIndex > 1;

    /// <summary>
    /// <para>获取步骤3（安装）是否已完成。</para>
    /// Gets whether step 3 (Install) is completed.
    /// </summary>
    public bool IsStep3Completed => StepProgressIndex > 2;

    #endregion

    #region Eula Step

    [ObservableProperty]
    private bool _isAgreeEula;

    [ObservableProperty]
    private bool _isCreateShortcut = true;

    #endregion

    #region Component Selection

    /// <summary>
    /// <para>获取或设置可用组件列表。</para>
    /// Gets or sets the list of available components.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ComponentDefinition> _availableComponents = [];

    /// <summary>
    /// <para>获取或设置总安装大小估算（字节）。</para>
    /// Gets or sets the estimated total install size in bytes.
    /// </summary>
    [ObservableProperty]
    private long _totalInstallSize;

    partial void OnAvailableComponentsChanged(ObservableCollection<ComponentDefinition> value)
    {
        OnPropertyChanged(nameof(HasComponents));
        SubscribeToComponentChanges();
        UpdateTotalInstallSize();
    }

    private void SubscribeToComponentChanges()
    {
        foreach (var component in AvailableComponents)
        {
            component.PropertyChanged -= OnComponentPropertyChanged;
            component.PropertyChanged += OnComponentPropertyChanged;
        }
    }

    private void OnComponentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ComponentDefinition.IsSelected))
        {
            UpdateTotalInstallSize();
        }
    }

    #endregion

    #region Channel Selection

    /// <summary>
    /// <para>获取或设置可用渠道列表。</para>
    /// Gets or sets the list of available channels.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReleaseChannelInfo> _availableChannels = [];

    /// <summary>
    /// <para>获取或设置当前选中的渠道。</para>
    /// Gets or sets the currently selected channel.
    /// </summary>
    [ObservableProperty]
    private ReleaseChannelInfo? _selectedChannel;

    partial void OnSelectedChannelChanged(ReleaseChannelInfo? value)
    {
        OnPropertyChanged(nameof(ChannelWarningText));
        OnPropertyChanged(nameof(IsChannelWarningVisible));
    }

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

    /// <summary>
    /// <para>获取或设置是否可以回滚到上一版本。</para>
    /// Gets or sets whether rollback to a previous version is available.
    /// </summary>
    [ObservableProperty]
    private bool _canRollback;

    /// <summary>
    /// <para>获取或设置上一版本号文本。</para>
    /// Gets or sets the previous version text.
    /// </summary>
    [ObservableProperty]
    private string _previousVersionText = string.Empty;

    /// <summary>
    /// <para>回滚到上一版本命令。</para>
    /// Rollback to the previous version command.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRollback))]
    private async Task RollbackAsync()
    {
        if (_installerService is null || _dialogService is null || string.IsNullOrEmpty(InstallPath)) return;

        var confirmMessage = string.Format(L("RollbackConfirm", "确定要回滚到版本 {0} 吗？"), PreviousVersionText);
        var confirmed = await _dialogService.ShowConfirmAsync(L("Rollback", "回滚到上一版本"), confirmMessage).ConfigureAwait(true);
        if (!confirmed) return;

        try
        {
            var progress = new Progress<DeploymentProgress>(p =>
            {
                InstallStatusText = p.Message;
            });

            var result = await _installerService.RollbackAsync(InstallPath, PreviousVersionText, progress).ConfigureAwait(true);
            if (result.IsSuccess)
            {
                CurrentStep = InstallStep.AlreadyInstalled;
                InstalledVersion = PreviousVersionText;
                CanRollback = false;
                PreviousVersionText = string.Empty;
                try
                {
                    var versions = await _installerService.GetInstalledVersionsAsync(InstallPath).ConfigureAwait(true);
                    if (versions.Count > 1)
                    {
                        CanRollback = true;
                        PreviousVersionText = versions[^2].Version;
                    }
                }
                catch
                {
                }
            }
            else
            {
                await _dialogService.ShowErrorAsync(L("InstallFailed", "安装失败"), result.ErrorMessage ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(L("InstallFailed", "安装失败"), ex.Message);
        }
    }

    #endregion

    #region Window Commands

    /// <summary>
    /// <para>关闭窗口命令。</para>
    /// Close window command.
    /// </summary>
    [RelayCommand]
    private void CloseWindow()
    {
        RequestClose?.Invoke();
    }

    /// <summary>
    /// <para>取消安装命令。</para>
    /// Cancel install command.
    /// </summary>
    [RelayCommand]
    private void CancelInstall()
    {
        _installCts?.Cancel();
    }

    /// <summary>
    /// <para>返回镜像选择步骤命令。</para>
    /// Navigate back to mirror selection step command.
    /// </summary>
    [RelayCommand]
    private void BackToMirrorStep()
    {
        CurrentStep = InstallStep.ChooseMirror;
    }

    [RelayCommand]
    private void BackToEulaStep()
    {
        CurrentStep = InstallStep.Eula;
    }

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
    /// <para>获取或设置是否使用增量更新。</para>
    /// Gets or sets whether to use delta updates.
    /// </summary>
    [ObservableProperty]
    private bool _useDeltaUpdate = true;

    /// <summary>
    /// <para>获取安装按钮的显示文本。</para>
    /// Gets the display text for the install button.
    /// </summary>
    public string InstallButtonText => IsUpdate ? L("Update", "更新") : L("Install", "安装");

    partial void OnIsUpdateChanged(bool value) => OnPropertyChanged(nameof(InstallButtonText));

    /// <summary>
    /// <para>浏览安装路径命令，打开文件夹选择对话框。</para>
    /// Browse install path command that opens a folder selection dialog.
    /// </summary>
    [RelayCommand]
    private async Task BrowseInstallPathAsync()
    {
        if (_dialogService is null) return;

        var selectedPath = await _dialogService.BrowseFolderAsync(L("SelectInstallPath", "选择安装路径"), InstallPath);
        if (selectedPath is not null)
        {
            var dirName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.Equals(dirName, "UotanToolbox", StringComparison.OrdinalIgnoreCase))
            {
                selectedPath = Path.Combine(selectedPath, "UotanToolbox");
            }

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
        InstallStatusText = L("PrepareInstall", "准备安装...");

        var steps = new ObservableCollection<InstallSubStepItem>
        {
            new() { DisplayName = L("Step_Download", "下载安装包"), Kind = InstallProgressKind.Downloading },
            new() { DisplayName = L("Step_Verify", "校验文件完整性"), Kind = InstallProgressKind.Verifying },
            new() { DisplayName = L("Step_Extract", "安装应用"), Kind = InstallProgressKind.Installing },
        };

        if (IsCreateShortcut)
        {
            steps.Add(new() { DisplayName = L("Step_CreateShortcut", "创建快捷方式"), Kind = InstallProgressKind.Installing });
        }

        InstallSubSteps = steps;

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
                        InstallStatusText = L("PlatformCheckFailed", "平台检查未通过");
                        await (_dialogService?.ShowErrorAsync(L("PlatformCheckFailed", "平台检查未通过"), L("DiskSpaceInsufficient", "磁盘空间不足")) ?? Task.CompletedTask);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    IsInstallFailed = true;
                    InstallStatusText = $"{L("PlatformCheckFailed", "平台检查未通过")}: {ex.Message}";
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
                Channel = SelectedChannel?.Channel ?? ReleaseChannel.Release,
                SelectedComponents = AvailableComponents.Where(c => c.IsSelected).ToList(),
                UseDeltaUpdate = UseDeltaUpdate && IsUpdate,
                CurrentVersion = IsUpdate ? InstalledVersion : null,
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
                InstallStatusText = result.ErrorMessage ?? L("InstallFailed", "安装失败");
            }
        }
        catch (OperationCanceledException)
        {
            IsInstallFailed = true;
            InstallStatusText = L("InstallCancelled", "安装已取消");
        }
        catch (Exception ex)
        {
            IsInstallFailed = true;
            InstallStatusText = $"{L("InstallFailed", "安装失败")}: {ex.Message}";
            await _dialogService.ShowErrorAsync(L("InstallFailed", "安装失败"), ex.Message);
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
                L("InstallFailed", "安装失败"),
                L("RetryLimitReached", "多次重试失败，请检查网络连接或联系支持。"));
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
        RetryCount = 0;
        IsInstallFailed = false;
        InstallProgressValue = 0;
        IsInstallSuccess = true;
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

                if (_installerService is not null && !string.IsNullOrEmpty(config.InstallPath))
                {
                    try
                    {
                        var versions = await _installerService.GetInstalledVersionsAsync(config.InstallPath).ConfigureAwait(false);
                        if (versions.Count > 1)
                        {
                            CanRollback = true;
                            var previousRecord = versions[^2];
                            PreviousVersionText = previousRecord.Version;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
            VersionText = L("VersionUnavailable", "获取失败");
        }

        if (_channelService is not null)
        {
            try
            {
                var channels = await _channelService.GetAvailableChannelsAsync();
                AvailableChannels = new ObservableCollection<ReleaseChannelInfo>(channels);
                UpdateChannelDisplayNames();
                SelectedChannel = AvailableChannels.FirstOrDefault(c => c.Channel == ReleaseChannel.Release);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// <para>检查是否有新版本可用，并在用户确认后执行自更新。</para>
    /// Checks for a new version available and performs a self-update after user confirmation.
    /// </summary>
    public async Task CheckForSelfUpdateAsync()
    {
        if (_installerService is null || _dialogService is null) return;

        if (SelectedChannel is not null && SelectedChannel.Channel != ReleaseChannel.Release) return;

        try
        {
            var hasUpdate = await _releaseApiService!.CheckSelfUpdateAsync(
                System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0",
                default).ConfigureAwait(false);

            if (!hasUpdate) return;

            var updateText = L("Update", "更新");
            var confirmed = await _dialogService.ShowConfirmAsync(updateText, L("SelfUpdateConfirm", "检测到新版本，是否立即更新？")).ConfigureAwait(false);
            if (!confirmed) return;

            await _installerService.SelfUpdateAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    #endregion

    #region Private Methods

    private void UpdateTotalInstallSize()
    {
        TotalInstallSize = AvailableComponents.Where(c => c.IsSelected).Sum(c => c.Size);
        OnPropertyChanged(nameof(TotalInstallSizeText));
    }

    private void UpdateChannelDisplayNames()
    {
        foreach (var channel in AvailableChannels)
        {
            channel.DisplayName = channel.Channel switch
            {
                ReleaseChannel.Release => L("Channel_Release", "正式版"),
                ReleaseChannel.PreRelease => L("Channel_PreRelease", "预发布版"),
                ReleaseChannel.Beta => L("Channel_Beta", "测试版"),
                ReleaseChannel.Nightly => L("Channel_Nightly", "每日构建版"),
                _ => channel.DisplayName,
            };
            channel.Description = channel.Channel switch
            {
                ReleaseChannel.Release => L("Channel_Release_Desc", "稳定发布版本，推荐大多数用户使用"),
                ReleaseChannel.PreRelease => L("Channel_PreRelease_Desc", "预发布版本，包含即将发布的最新功能"),
                ReleaseChannel.Beta => L("Channel_Beta_Desc", "测试版本，可能存在不稳定因素"),
                ReleaseChannel.Nightly => L("Channel_Nightly_Desc", "每日构建版本，包含最新的开发代码"),
                _ => channel.Description,
            };
        }
        OnPropertyChanged(nameof(AvailableChannels));
    }

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
            var patchData = await _releaseApiService.GetPatchDataAsync(SelectedChannel?.Channel ?? ReleaseChannel.Release);
            PatchVersion = patchData.Version;
            PatchSha256 = patchData.Sha256;
            Mirrors = new ObservableCollection<GenericPatchPackageMirror>(patchData.Mirrors);

            if (Mirrors.Count > 0)
            {
                SelectedMirror = Mirrors[0];
            }

            if (_componentManager is not null)
            {
                try
                {
                    var channel = SelectedChannel?.Channel ?? ReleaseChannel.Release;
                    var releases = await _releaseApiService.GetReleasesByChannelAsync(channel);
                    var latestRelease = releases.FirstOrDefault();
                    if (latestRelease?.Body is not null)
                    {
                        var components = await _componentManager.ParseComponentsAsync(latestRelease.Body);
                        AvailableComponents = new ObservableCollection<ComponentDefinition>(components);
                    }
                }
                catch
                {
                }
            }

            await SpeedTestAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(L("LoadMirrorFailed", "加载镜像列表失败"), ex.Message);
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
                if (progress.IsStepCompleted)
                {
                    subStep.IsActive = false;
                    subStep.IsCompleted = true;
                    subStep.IsFailed = false;
                }
                else
                {
                    subStep.IsActive = true;
                    subStep.IsCompleted = false;
                    subStep.IsFailed = false;
                }
            }
            else if (i < activeIndex)
            {
                subStep.IsCompleted = true;
                subStep.IsActive = false;
                subStep.IsFailed = false;
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
            return await _dialogService.ShowConfirmAsync(L("Notice", "提示"), L("ConfirmExitInstalling", "安装尚未完成，确定要退出安装吗？"));
        }

        if (CurrentStep == InstallStep.Finish ||
            CurrentStep == InstallStep.AlreadyInstalled)
        {
            return true;
        }

        return await _dialogService.ShowConfirmAsync(L("Notice", "提示"), L("ConfirmExit", "确定要退出安装吗？"));
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
    public bool IsCompleted
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusForeground));
            }
        }
    }

    /// <summary>
    /// <para>获取或设置子步骤是否正在进行。</para>
    /// Gets or sets whether the sub-step is currently active.
    /// </summary>
    public bool IsActive
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusForeground));
            }
        }
    }

    /// <summary>
    /// <para>获取或设置子步骤是否失败。</para>
    /// Gets or sets whether the sub-step has failed.
    /// </summary>
    public bool IsFailed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusForeground));
            }
        }
    }

    /// <summary>
    /// <para>获取步骤状态图标文本。</para>
    /// Gets the status icon text for the sub-step.
    /// </summary>
    public string StatusIcon => IsFailed ? "✗" : IsCompleted ? "✓" : IsActive ? "●" : "○";

    /// <summary>
    /// <para>获取步骤状态指示器的背景画刷。</para>
    /// Gets the background brush for the step status indicator.
    /// </summary>
    public Avalonia.Media.IBrush StatusBrush =>
        IsFailed ? Avalonia.Media.Brushes.IndianRed :
        IsCompleted ? Avalonia.Media.Brushes.MediumSeaGreen :
        IsActive ? Avalonia.Media.Brushes.OrangeRed :
        Avalonia.Media.Brushes.Gray;

    /// <summary>
    /// <para>获取步骤文本的前景画刷。</para>
    /// Gets the foreground brush for the step text.
    /// </summary>
    public Avalonia.Media.IBrush StatusForeground =>
        IsFailed ? Avalonia.Media.Brushes.IndianRed :
        IsCompleted ? Avalonia.Media.Brushes.MediumSeaGreen :
        IsActive ? Avalonia.Media.Brushes.OrangeRed :
        Avalonia.Media.Brushes.Gray;
}
