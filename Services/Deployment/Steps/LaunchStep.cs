namespace UotanInstaller.App.Services.Deployment.Steps;

/// <summary>
/// <para>启动应用步骤，负责在安装完成后启动目标应用程序。</para>
/// Launch application step responsible for launching the target application after installation.
/// </summary>
public sealed class LaunchStep : IDeploymentStep
{
    private readonly string _exePath;
    private readonly IPlatformAdapter _platformAdapter;
    private readonly string? _args;

    /// <summary>
    /// <para>获取步骤名称。</para>
    /// Gets the step name.
    /// </summary>
    public string Name { get; } = "启动应用";

    /// <summary>
    /// <para>获取步骤类型。</para>
    /// Gets the step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; } = DeploymentStepKind.Launch;

    /// <summary>
    /// <para>获取或设置是否跳过此步骤。</para>
    /// Gets or sets whether to skip this step.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// <para>使用指定参数初始化 LaunchStep 的新实例。</para>
    /// Initializes a new instance of LaunchStep with the specified parameters.
    /// </summary>
    /// <param name="exePath">
    /// <para>要启动的可执行文件路径。</para>
    /// The executable file path to launch.
    /// </param>
    /// <param name="platformAdapter">
    /// <para>平台适配器实例。</para>
    /// The platform adapter instance.
    /// </param>
    /// <param name="args">
    /// <para>启动时传递的命令行参数，可为 null。</para>
    /// The command-line arguments passed at launch, or null.
    /// </param>
    public LaunchStep(string exePath, IPlatformAdapter platformAdapter, string? args = null)
    {
        _exePath = exePath;
        _platformAdapter = platformAdapter;
        _args = args;
    }

    /// <summary>
    /// <para>异步执行启动应用步骤并报告进度。</para>
    /// Asynchronously executes the launch application step and reports progress.
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
    /// <para>当启动应用失败时抛出。</para>
    /// Thrown when launching the application fails.
    /// </exception>
    public Task<DeploymentStepResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new DeploymentProgress
        {
            StepName = Name,
            Kind = Kind,
            ProgressValue = 0.0,
            Message = "正在启动应用...",
        });

        try
        {
            _platformAdapter.LaunchApplication(_exePath, _args);

            progress?.Report(new DeploymentProgress
            {
                StepName = Name,
                Kind = Kind,
                ProgressValue = 1.0,
                Message = "应用已启动",
            });

            return Task.FromResult(new DeploymentStepResult
            {
                StepName = Name,
                Kind = Kind,
                IsSuccess = true,
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DeploymentException(
                $"Failed to launch application {_exePath}: {ex.Message}",
                Name);
        }
    }
}
