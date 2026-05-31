namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>表示部署步骤的类型。</para>
/// Represents the kind of a deployment step.
/// </summary>
public enum DeploymentStepKind
{
    /// <summary>
    /// <para>下载步骤。</para>
    /// Download step.
    /// </summary>
    Download,

    /// <summary>
    /// <para>校验步骤。</para>
    /// Verify step.
    /// </summary>
    Verify,

    /// <summary>
    /// <para>解压步骤。</para>
    /// Extract step.
    /// </summary>
    Extract,

    /// <summary>
    /// <para>配置步骤。</para>
    /// Configure step.
    /// </summary>
    Configure,

    /// <summary>
    /// <para>创建快捷方式步骤。</para>
    /// Create shortcut step.
    /// </summary>
    CreateShortcut,

    /// <summary>
    /// <para>启动应用步骤。</para>
    /// Launch application step.
    /// </summary>
    Launch,

    /// <summary>
    /// <para>自定义步骤。</para>
    /// Custom step.
    /// </summary>
    Custom,
}

/// <summary>
/// <para>封装单个部署步骤的执行结果。</para>
/// Encapsulates the execution result of a single deployment step.
/// </summary>
public sealed class DeploymentStepResult
{
    /// <summary>
    /// <para>获取或初始化步骤名称。</para>
    /// Gets or initializes the step name.
    /// </summary>
    public string StepName { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化步骤类型。</para>
    /// Gets or initializes the step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; init; }

    /// <summary>
    /// <para>获取或初始化步骤是否执行成功。</para>
    /// Gets or initializes whether the step executed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// <para>获取或初始化错误消息，成功时为 null。</para>
    /// Gets or initializes the error message; null when successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// <para>获取或初始化步骤执行期间抛出的异常，成功时为 null。</para>
    /// Gets or initializes the exception thrown during step execution; null when successful.
    /// </summary>
    public Exception? Error { get; init; }
}

/// <summary>
/// <para>定义可执行的部署步骤，每个步骤可独立运行并报告进度。</para>
/// Defines an executable deployment step that can run independently and report progress.
/// </summary>
public interface IDeploymentStep
{
    /// <summary>
    /// <para>获取步骤名称。</para>
    /// Gets the step name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// <para>获取步骤类型。</para>
    /// Gets the step kind.
    /// </summary>
    DeploymentStepKind Kind { get; }

    /// <summary>
    /// <para>获取或设置是否跳过此步骤。</para>
    /// Gets or sets whether to skip this step.
    /// </summary>
    bool Skip { get; set; }

    /// <summary>
    /// <para>异步执行部署步骤并报告进度。</para>
    /// Asynchronously executes the deployment step and reports progress.
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
    Task<DeploymentStepResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct);
}

/// <summary>
/// <para>表示部署步骤的进度信息。</para>
/// Represents progress information for a deployment step.
/// </summary>
public sealed class DeploymentProgress
{
    /// <summary>
    /// <para>获取或初始化当前步骤名称。</para>
    /// Gets or initializes the current step name.
    /// </summary>
    public string StepName { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化当前步骤类型。</para>
    /// Gets or initializes the current step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; init; }

    /// <summary>
    /// <para>获取或初始化进度值，范围为 0.0 到 1.0。</para>
    /// Gets or initializes the progress value, ranging from 0.0 to 1.0.
    /// </summary>
    public double ProgressValue { get; init; }

    /// <summary>
    /// <para>获取或初始化进度描述消息。</para>
    /// Gets or initializes the progress description message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化当前步骤是否已完成。</para>
    /// Gets or initializes whether the current step has completed.
    /// </summary>
    public bool IsStepCompleted { get; init; }
}
