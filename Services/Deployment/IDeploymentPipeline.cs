namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>封装整个部署管线的执行结果。</para>
/// Encapsulates the execution result of the entire deployment pipeline.
/// </summary>
public sealed class DeploymentResult
{
    /// <summary>
    /// <para>获取或初始化部署是否全部成功。</para>
    /// Gets or initializes whether the deployment succeeded entirely.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// <para>获取或初始化已成功完成的步骤结果列表。</para>
    /// Gets or initializes the list of successfully completed step results.
    /// </summary>
    public IReadOnlyList<DeploymentStepResult> CompletedSteps { get; init; } = [];

    /// <summary>
    /// <para>获取或初始化失败的步骤结果，全部成功时为 null。</para>
    /// Gets or initializes the failed step result; null when all steps succeeded.
    /// </summary>
    public DeploymentStepResult? FailedStep { get; init; }

    /// <summary>
    /// <para>获取或初始化错误消息，成功时为 null。</para>
    /// Gets or initializes the error message; null when successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// <para>获取或初始化部署管线执行的总耗时。</para>
    /// Gets or initializes the total elapsed time of the deployment pipeline execution.
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}

/// <summary>
/// <para>定义部署管线的编排能力，支持按序添加步骤并统一执行。</para>
/// Defines the orchestration capability of a deployment pipeline, supporting sequential step addition and unified execution.
/// </summary>
public interface IDeploymentPipeline
{
    /// <summary>
    /// <para>向管线中添加一个部署步骤。</para>
    /// Adds a deployment step to the pipeline.
    /// </summary>
    /// <param name="step">
    /// <para>要添加的部署步骤。</para>
    /// The deployment step to add.
    /// </param>
    void AddStep(IDeploymentStep step);

    /// <summary>
    /// <para>异步执行管线中的所有部署步骤，并报告整体进度。</para>
    /// Asynchronously executes all deployment steps in the pipeline and reports overall progress.
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
    /// <para>部署管线的执行结果。</para>
    /// The execution result of the deployment pipeline.
    /// </returns>
    Task<DeploymentResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct);
}
