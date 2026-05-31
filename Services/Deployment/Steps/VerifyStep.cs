namespace UotanInstaller.App.Services.Deployment.Steps;

/// <summary>
/// <para>校验步骤，负责验证下载文件的 SHA256 完整性。</para>
/// Verification step responsible for validating the SHA256 integrity of a downloaded file.
/// </summary>
public sealed class VerifyStep : IDeploymentStep
{
    private readonly string _filePath;
    private readonly string _expectedSha256;
    private readonly IFileService _fileService;
    private readonly ILocalizationService _localizationService;

    /// <summary>
    /// <para>获取步骤名称。</para>
    /// Gets the step name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <para>获取步骤类型。</para>
    /// Gets the step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; } = DeploymentStepKind.Verify;

    /// <summary>
    /// <para>获取或设置是否跳过此步骤。</para>
    /// Gets or sets whether to skip this step.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// <para>使用指定参数初始化 VerifyStep 的新实例。</para>
    /// Initializes a new instance of VerifyStep with the specified parameters.
    /// </summary>
    /// <param name="filePath">
    /// <para>要校验的文件路径。</para>
    /// The file path to verify.
    /// </param>
    /// <param name="expectedSha256">
    /// <para>期望的 SHA256 哈希值（小写十六进制字符串）。</para>
    /// The expected SHA256 hash value (lowercase hexadecimal string).
    /// </param>
    /// <param name="fileService">
    /// <para>文件服务实例。</para>
    /// The file service instance.
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务实例。</para>
    /// The localization service instance.
    /// </param>
    public VerifyStep(string filePath, string expectedSha256, IFileService fileService, ILocalizationService localizationService)
    {
        _filePath = filePath;
        _expectedSha256 = expectedSha256;
        _fileService = fileService;
        _localizationService = localizationService;
        Name = _localizationService["Step_Verify"];
    }

    /// <summary>
    /// <para>异步执行校验步骤并报告进度。</para>
    /// Asynchronously executes the verification step and reports progress.
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
    /// <exception cref="VerificationException">
    /// <para>当文件校验失败时抛出，包含期望和实际的哈希值及文件路径信息。</para>
    /// Thrown when file verification fails, containing the expected and actual hash values and file path information.
    /// </exception>
    public async Task<DeploymentStepResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new DeploymentProgress
        {
            StepName = Name,
            Kind = Kind,
            ProgressValue = 0.0,
            Message = _localizationService["Step_VerifyRunning"],
        });

        var actualHash = await _fileService.ComputeSha256Async(_filePath, ct).ConfigureAwait(false);

        if (!string.Equals(actualHash, _expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new VerificationException(
                $"File verification failed for {_filePath}. Expected: {_expectedSha256}, Actual: {actualHash}",
                _expectedSha256,
                actualHash,
                _filePath);
        }

        progress?.Report(new DeploymentProgress
        {
            StepName = Name,
            Kind = Kind,
            ProgressValue = 1.0,
            Message = _localizationService["Step_VerifyComplete"],
        });

        return new DeploymentStepResult
        {
            StepName = Name,
            Kind = Kind,
            IsSuccess = true,
        };
    }
}
