namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>部署过程中发生的通用异常，可关联到特定步骤名称。</para>
/// General exception that occurs during deployment, which can be associated with a specific step name.
/// </summary>
public class DeploymentException : Exception
{
    /// <summary>
    /// <para>获取关联的部署步骤名称，可为 null。</para>
    /// Gets the associated deployment step name; can be null.
    /// </summary>
    public string? StepName { get; }

    /// <summary>
    /// <para>获取异常消息，优先返回步骤名称与消息的组合。</para>
    /// Gets the exception message, preferring a combination of step name and message.
    /// </summary>
    public override string Message => StepName is not null
        ? $"[{StepName}] {base.Message}"
        : base.Message;

    /// <summary>
    /// <para>使用指定错误消息初始化 DeploymentException 的新实例。</para>
    /// Initializes a new instance of DeploymentException with the specified error message.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    public DeploymentException(string message) : base(message) { }

    /// <summary>
    /// <para>使用指定错误消息和内部异常初始化 DeploymentException 的新实例。</para>
    /// Initializes a new instance of DeploymentException with the specified error message and inner exception.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    /// <param name="inner">
    /// <para>导致当前异常的内部异常。</para>
    /// The inner exception that caused the current exception.
    /// </param>
    public DeploymentException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// <para>使用指定错误消息和步骤名称初始化 DeploymentException 的新实例。</para>
    /// Initializes a new instance of DeploymentException with the specified error message and step name.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    /// <param name="stepName">
    /// <para>关联的部署步骤名称，可为 null。</para>
    /// The associated deployment step name; can be null.
    /// </param>
    public DeploymentException(string message, string? stepName) : base(message)
    {
        StepName = stepName;
    }
}

/// <summary>
/// <para>下载过程中发生的异常，包含请求 URL 和 HTTP 状态码信息。</para>
/// Exception that occurs during download, containing the request URL and HTTP status code information.
/// </summary>
public sealed class DownloadException : DeploymentException
{
    /// <summary>
    /// <para>获取下载请求的 URL，可为 null。</para>
    /// Gets the URL of the download request; can be null.
    /// </summary>
    public string? Url { get; }

    /// <summary>
    /// <para>获取 HTTP 响应状态码，可为 null。</para>
    /// Gets the HTTP response status code; can be null.
    /// </summary>
    public int? HttpStatusCode { get; }

    /// <summary>
    /// <para>使用指定错误消息、URL 和 HTTP 状态码初始化 DownloadException 的新实例。</para>
    /// Initializes a new instance of DownloadException with the specified error message, URL, and HTTP status code.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    /// <param name="url">
    /// <para>下载请求的 URL，可为 null。</para>
    /// The URL of the download request; can be null.
    /// </param>
    /// <param name="httpStatusCode">
    /// <para>HTTP 响应状态码，可为 null。</para>
    /// The HTTP response status code; can be null.
    /// </param>
    public DownloadException(string message, string? url = null, int? httpStatusCode = null) : base(message)
    {
        Url = url;
        HttpStatusCode = httpStatusCode;
    }
}

/// <summary>
/// <para>文件校验过程中发生的异常，包含期望和实际的哈希值信息。</para>
/// Exception that occurs during file verification, containing the expected and actual hash values.
/// </summary>
public sealed class VerificationException : DeploymentException
{
    /// <summary>
    /// <para>获取期望的哈希值，可为 null。</para>
    /// Gets the expected hash value; can be null.
    /// </summary>
    public string? ExpectedHash { get; }

    /// <summary>
    /// <para>获取实际的哈希值，可为 null。</para>
    /// Gets the actual hash value; can be null.
    /// </summary>
    public string? ActualHash { get; }

    /// <summary>
    /// <para>获取校验失败的文件路径，可为 null。</para>
    /// Gets the file path that failed verification; can be null.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// <para>使用指定错误消息、期望哈希值、实际哈希值和文件路径初始化 VerificationException 的新实例。</para>
    /// Initializes a new instance of VerificationException with the specified error message, expected hash, actual hash, and file path.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    /// <param name="expectedHash">
    /// <para>期望的哈希值，可为 null。</para>
    /// The expected hash value; can be null.
    /// </param>
    /// <param name="actualHash">
    /// <para>实际的哈希值，可为 null。</para>
    /// The actual hash value; can be null.
    /// </param>
    /// <param name="filePath">
    /// <para>校验失败的文件路径，可为 null。</para>
    /// The file path that failed verification; can be null.
    /// </param>
    public VerificationException(string message, string? expectedHash = null, string? actualHash = null, string? filePath = null) : base(message)
    {
        ExpectedHash = expectedHash;
        ActualHash = actualHash;
        FilePath = filePath;
    }
}

/// <summary>
/// <para>解压缩过程中发生的异常，包含归档文件路径信息。</para>
/// Exception that occurs during extraction, containing the archive file path information.
/// </summary>
public sealed class ExtractionException : DeploymentException
{
    /// <summary>
    /// <para>获取解压失败的归档文件路径，可为 null。</para>
    /// Gets the archive file path that failed to extract; can be null.
    /// </summary>
    public string? ArchivePath { get; }

    /// <summary>
    /// <para>使用指定错误消息和归档文件路径初始化 ExtractionException 的新实例。</para>
    /// Initializes a new instance of ExtractionException with the specified error message and archive file path.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    /// <param name="archivePath">
    /// <para>解压失败的归档文件路径，可为 null。</para>
    /// The archive file path that failed to extract; can be null.
    /// </param>
    public ExtractionException(string message, string? archivePath = null) : base(message)
    {
        ArchivePath = archivePath;
    }
}

/// <summary>
/// <para>当前平台不受支持时抛出的异常，包含当前平台和所需平台信息。</para>
/// Exception thrown when the current platform is not supported, containing the current and required platform information.
/// </summary>
public sealed class PlatformNotSupportedDeploymentException : DeploymentException
{
    /// <summary>
    /// <para>获取当前运行平台的操作系统类型。</para>
    /// Gets the operating system type of the current runtime platform.
    /// </summary>
    public PlatformOS CurrentPlatform { get; }

    /// <summary>
    /// <para>获取所需的操作系统平台类型，可为 null。</para>
    /// Gets the required operating system platform type; can be null.
    /// </summary>
    public PlatformOS? RequiredPlatform { get; }

    /// <summary>
    /// <para>使用指定错误消息、当前平台和所需平台初始化 PlatformNotSupportedDeploymentException 的新实例。</para>
    /// Initializes a new instance of PlatformNotSupportedDeploymentException with the specified error message, current platform, and required platform.
    /// </summary>
    /// <param name="message">
    /// <para>描述错误的消息。</para>
    /// The message that describes the error.
    /// </param>
    /// <param name="currentPlatform">
    /// <para>当前运行平台的操作系统类型。</para>
    /// The operating system type of the current runtime platform.
    /// </param>
    /// <param name="requiredPlatform">
    /// <para>所需的操作系统平台类型，可为 null。</para>
    /// The required operating system platform type; can be null.
    /// </param>
    public PlatformNotSupportedDeploymentException(string message, PlatformOS currentPlatform, PlatformOS? requiredPlatform = null) : base(message)
    {
        CurrentPlatform = currentPlatform;
        RequiredPlatform = requiredPlatform;
    }
}
