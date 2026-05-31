using System.Diagnostics;

namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>部署管线实现，按序编排多个部署步骤的执行，支持跳过、进度报告和错误处理。</para>
/// Deployment pipeline implementation that orchestrates the sequential execution of multiple deployment steps, supporting skip, progress reporting, and error handling.
/// </summary>
public sealed class DeploymentPipeline : IDeploymentPipeline
{
    private readonly ILocalizationService _localizationService;
    private readonly List<IDeploymentStep> _steps = [];

    /// <summary>
    /// <para>使用本地化服务初始化 DeploymentPipeline 的新实例。</para>
    /// Initializes a new instance of DeploymentPipeline with the localization service.
    /// </summary>
    /// <param name="localizationService">
    /// <para>本地化服务实例。</para>
    /// The localization service instance.
    /// </param>
    public DeploymentPipeline(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// <para>向管线中添加一个部署步骤。</para>
    /// Adds a deployment step to the pipeline.
    /// </summary>
    /// <param name="step">
    /// <para>要添加的部署步骤。</para>
    /// The deployment step to add.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <para>当 step 为 null 时抛出。</para>
    /// Thrown when step is null.
    /// </exception>
    public void AddStep(IDeploymentStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
    }

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
    public async Task<DeploymentResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var completedSteps = new List<DeploymentStepResult>();

        if (_steps.Count == 0)
        {
            stopwatch.Stop();
            return new DeploymentResult
            {
                IsSuccess = true,
                CompletedSteps = completedSteps,
                Elapsed = stopwatch.Elapsed,
            };
        }

        for (var i = 0; i < _steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var step = _steps[i];
            var overallProgress = (double)i / _steps.Count;

            if (step.Skip)
            {
                var skippedResult = new DeploymentStepResult
                {
                    StepName = step.Name,
                    Kind = step.Kind,
                    IsSuccess = true,
                };
                completedSteps.Add(skippedResult);

                progress?.Report(new DeploymentProgress
                {
                    StepName = step.Name,
                    Kind = step.Kind,
                    ProgressValue = overallProgress,
                    Message = _localizationService["Step_Skipped"],
                });

                continue;
            }

            var executingText = _localizationService["Step_Executing"];
            progress?.Report(new DeploymentProgress
            {
                StepName = step.Name,
                Kind = step.Kind,
                ProgressValue = overallProgress,
                Message = $"{executingText} {step.Name}",
            });

            DeploymentStepResult result;

            try
            {
                var stepProgress = new Progress<DeploymentProgress>(p =>
                {
                    var stepContribution = 1.0 / _steps.Count;
                    var combinedProgress = overallProgress + p.ProgressValue * stepContribution;
                    progress?.Report(new DeploymentProgress
                    {
                        StepName = p.StepName,
                        Kind = p.Kind,
                        ProgressValue = combinedProgress,
                        Message = p.Message,
                    });
                });

                result = await step.ExecuteAsync(stepProgress, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var cancelledText = _localizationService["InstallCancelled"];
                return new DeploymentResult
                {
                    IsSuccess = false,
                    CompletedSteps = completedSteps,
                    FailedStep = new DeploymentStepResult
                    {
                        StepName = step.Name,
                        Kind = step.Kind,
                        IsSuccess = false,
                        ErrorMessage = cancelledText,
                    },
                    ErrorMessage = cancelledText,
                    Elapsed = stopwatch.Elapsed,
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var errorMessage = ex is DeploymentException dex ? dex.Message : ex.Message;
                return new DeploymentResult
                {
                    IsSuccess = false,
                    CompletedSteps = completedSteps,
                    FailedStep = new DeploymentStepResult
                    {
                        StepName = step.Name,
                        Kind = step.Kind,
                        IsSuccess = false,
                        ErrorMessage = errorMessage,
                        Error = ex,
                    },
                    ErrorMessage = errorMessage,
                    Elapsed = stopwatch.Elapsed,
                };
            }

            completedSteps.Add(result);

            var completedText = _localizationService["Step_Completed"];
            progress?.Report(new DeploymentProgress
            {
                StepName = step.Name,
                Kind = step.Kind,
                ProgressValue = (double)(i + 1) / _steps.Count,
                Message = $"{step.Name} {completedText}",
            });
        }

        stopwatch.Stop();

        return new DeploymentResult
        {
            IsSuccess = true,
            CompletedSteps = completedSteps,
            Elapsed = stopwatch.Elapsed,
        };
    }
}
