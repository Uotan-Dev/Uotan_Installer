namespace UotanInstaller.App.Services.Deployment.Steps;

/// <summary>
/// <para>创建快捷方式步骤，负责在桌面创建应用程序快捷方式。</para>
/// Create shortcut step responsible for creating an application shortcut on the desktop.
/// </summary>
public sealed class CreateShortcutStep : IDeploymentStep
{
    private readonly string _appName;
    private readonly string _targetPath;
    private readonly IPlatformAdapter _platformAdapter;
    private readonly string? _args;

    /// <summary>
    /// <para>获取步骤名称。</para>
    /// Gets the step name.
    /// </summary>
    public string Name { get; } = "创建快捷方式";

    /// <summary>
    /// <para>获取步骤类型。</para>
    /// Gets the step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; } = DeploymentStepKind.CreateShortcut;

    /// <summary>
    /// <para>获取或设置是否跳过此步骤。</para>
    /// Gets or sets whether to skip this step.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// <para>使用指定参数初始化 CreateShortcutStep 的新实例。</para>
    /// Initializes a new instance of CreateShortcutStep with the specified parameters.
    /// </summary>
    /// <param name="appName">
    /// <para>应用程序名称，用于快捷方式的显示名称。</para>
    /// The application name used as the display name for the shortcut.
    /// </param>
    /// <param name="targetPath">
    /// <para>快捷方式指向的目标可执行文件路径。</para>
    /// The target executable file path that the shortcut points to.
    /// </param>
    /// <param name="platformAdapter">
    /// <para>平台适配器实例。</para>
    /// The platform adapter instance.
    /// </param>
    /// <param name="args">
    /// <para>启动目标程序时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed when launching the target program, or null.
    /// </param>
    public CreateShortcutStep(string appName, string targetPath, IPlatformAdapter platformAdapter, string? args = null)
    {
        _appName = appName;
        _targetPath = targetPath;
        _platformAdapter = platformAdapter;
        _args = args;
    }

    /// <summary>
    /// <para>异步执行创建快捷方式步骤并报告进度。</para>
    /// Asynchronously executes the create shortcut step and reports progress.
    /// </summary>
    /// <param name="progress">
    /// <para>进度报告回调，可为 null。</para>
    /// The progress report callback, or null.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>步骤执行结果。</para>
    /// The step execution result.
    /// </returns>
    /// <exception cref="DeploymentException">
    /// <para>当创建快捷方式失败时抛出。</para>
    /// Thrown when creating the shortcut fails.
    /// </exception>
    public async Task<DeploymentStepResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new DeploymentProgress
        {
            StepName = Name,
            Kind = Kind,
            ProgressValue = 0.0,
            Message = "正在创建快捷方式...",
        });

        try
        {
            await _platformAdapter.CreateDesktopShortcutAsync(_appName, _targetPath, _args, ct).ConfigureAwait(false);

            progress?.Report(new DeploymentProgress
            {
                StepName = Name,
                Kind = Kind,
                ProgressValue = 1.0,
                Message = "快捷方式创建成功",
            });

            return new DeploymentStepResult
            {
                StepName = Name,
                Kind = Kind,
                IsSuccess = true,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DeploymentException(
                $"Failed to create desktop shortcut for {_appName}: {ex.Message}",
                Name);
        }
    }
}
