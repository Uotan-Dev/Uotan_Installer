namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装日志服务接口，提供安装全生命周期事件记录和诊断信息导出功能。</para>
/// Installation log service interface that provides lifecycle event logging and diagnostic information export.
/// </summary>
public interface IInstallLogger
{
    /// <summary>
    /// <para>记录信息级别日志。</para>
    /// Logs an information-level message.
    /// </summary>
    /// <param name="stepName">
    /// <para>步骤名称。</para>
    /// The step name.
    /// </param>
    /// <param name="message">
    /// <para>日志消息。</para>
    /// The log message.
    /// </param>
    void LogInformation(string stepName, string message);

    /// <summary>
    /// <para>记录警告级别日志。</para>
    /// Logs a warning-level message.
    /// </summary>
    /// <param name="stepName">
    /// <para>步骤名称。</para>
    /// The step name.
    /// </param>
    /// <param name="message">
    /// <para>日志消息。</para>
    /// The log message.
    /// </param>
    void LogWarning(string stepName, string message);

    /// <summary>
    /// <para>记录错误级别日志。</para>
    /// Logs an error-level message.
    /// </summary>
    /// <param name="stepName">
    /// <para>步骤名称。</para>
    /// The step name.
    /// </param>
    /// <param name="message">
    /// <para>日志消息。</para>
    /// The log message.
    /// </param>
    /// <param name="exception">
    /// <para>关联的异常，可为 null。</para>
    /// The associated exception, or null.
    /// </param>
    void LogError(string stepName, string message, Exception? exception = null);

    /// <summary>
    /// <para>异步刷新日志到持久化存储。</para>
    /// Asynchronously flushes the log to persistent storage.
    /// </summary>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task FlushAsync();

    /// <summary>
    /// <para>异步导出诊断信息到指定路径的 ZIP 文件。</para>
    /// Asynchronously exports diagnostic information to a ZIP file at the specified path.
    /// </summary>
    /// <param name="outputPath">
    /// <para>输出 ZIP 文件路径。</para>
    /// The output ZIP file path.
    /// </param>
    /// <returns>
    /// <para>表示异步操作的 Task。</para>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task ExportDiagnosticsAsync(string outputPath);

    /// <summary>
    /// <para>初始化日志文件，在指定目录下创建日志文件。</para>
    /// Initializes the log file, creating a log file in the specified directory.
    /// </summary>
    /// <param name="logDirectory">
    /// <para>日志文件目录。</para>
    /// The log file directory.
    /// </param>
    void Initialize(string logDirectory);
}
