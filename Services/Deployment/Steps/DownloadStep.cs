namespace UotanInstaller.App.Services.Deployment.Steps;

/// <summary>
/// <para>下载步骤，负责从指定 URL 下载安装包到本地路径。</para>
/// Download step responsible for downloading the installation package from a specified URL to a local path.
/// </summary>
public sealed class DownloadStep : IDeploymentStep
{
    private readonly string _url;
    private readonly string _targetPath;
    private readonly IDownloadService _downloadService;
    private readonly ILocalizationService _localizationService;
    private readonly int _threadCount;

    /// <summary>
    /// <para>获取步骤名称。</para>
    /// Gets the step name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <para>获取步骤类型。</para>
    /// Gets the step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; } = DeploymentStepKind.Download;

    /// <summary>
    /// <para>获取或设置是否跳过此步骤。</para>
    /// Gets or sets whether to skip this step.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// <para>使用指定参数初始化 DownloadStep 的新实例。</para>
    /// Initializes a new instance of DownloadStep with the specified parameters.
    /// </summary>
    /// <param name="url">
    /// <para>安装包的下载 URL。</para>
    /// The download URL of the installation package.
    /// </param>
    /// <param name="targetPath">
    /// <para>下载文件的本地保存路径。</para>
    /// The local save path for the downloaded file.
    /// </param>
    /// <param name="downloadService">
    /// <para>下载服务实例。</para>
    /// The download service instance.
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务实例。</para>
    /// The localization service instance.
    /// </param>
    /// <param name="threadCount">
    /// <para>下载线程数，0 表示根据处理器数量自动确定。</para>
    /// The number of download threads; 0 to auto-determine based on processor count.
    /// </param>
    public DownloadStep(string url, string targetPath, IDownloadService downloadService, ILocalizationService localizationService, int threadCount = 0)
    {
        _url = url;
        _targetPath = targetPath;
        _downloadService = downloadService;
        _localizationService = localizationService;
        _threadCount = threadCount <= 0 ? Environment.ProcessorCount : threadCount;
        Name = _localizationService["Step_Download"];
    }

    /// <summary>
    /// <para>异步执行下载步骤并报告进度。</para>
    /// Asynchronously executes the download step and reports progress.
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
    /// <exception cref="DownloadException">
    /// <para>当下载失败时抛出，包含 URL 上下文信息。</para>
    /// Thrown when the download fails, containing URL context information.
    /// </exception>
    public async Task<DeploymentStepResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct)
    {
        var lastReportTime = DateTime.UtcNow;

        var downloadProgress = new Progress<(long Downloaded, long Total)>(p =>
        {
            var now = DateTime.UtcNow;
            if ((now - lastReportTime).TotalMilliseconds < 100) return;
            lastReportTime = now;

            var progressValue = p.Total > 0 ? (double)p.Downloaded / p.Total : 0;
            progress?.Report(new DeploymentProgress
            {
                StepName = Name,
                Kind = Kind,
                ProgressValue = progressValue,
                Message = FormatProgressMessage(p.Downloaded, p.Total),
            });
        });

        try
        {
            await _downloadService.MultiThreadedDownloadAsync(_url, _targetPath, _threadCount, downloadProgress, ct).ConfigureAwait(false);

            progress?.Report(new DeploymentProgress
            {
                StepName = Name,
                Kind = Kind,
                ProgressValue = 1.0,
                Message = _localizationService["Step_DownloadComplete"],
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
            throw new DownloadException(
                $"Failed to download from {_url}: {ex.Message}",
                _url);
        }
    }

    private string FormatProgressMessage(long downloaded, long total)
    {
        var downloadedMB = downloaded / 1024.0 / 1024.0;
        var downloadingText = _localizationService["Step_Downloading"];
        if (total > 0)
        {
            var totalMB = total / 1024.0 / 1024.0;
            var percentage = (double)downloaded / total * 100;
            return $"{downloadingText} {downloadedMB:F1}/{totalMB:F1} MB ({percentage:F0}%)";
        }

        return $"{downloadingText} {downloadedMB:F1} MB";
    }
}
