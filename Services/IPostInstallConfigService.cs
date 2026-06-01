namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装后配置服务接口，提供文件关联注册、URL 协议注册和 PATH 环境变量配置功能。</para>
/// Post-install configuration service interface that provides file association registration, URL protocol registration, and PATH environment variable configuration.
/// </summary>
public interface IPostInstallConfigService
{
    /// <summary>
    /// <para>异步注册文件关联。</para>
    /// Asynchronously registers file associations.
    /// </summary>
    /// <param name="fileExtension">
    /// <para>文件扩展名（如 ".uotan"）。</para>
    /// The file extension (e.g. ".uotan").
    /// </param>
    /// <param name="exePath">
    /// <para>处理该文件类型的可执行文件路径。</para>
    /// The executable file path that handles the file type.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    Task RegisterFileAssociationAsync(string fileExtension, string exePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步注册 URL 协议处理器。</para>
    /// Asynchronously registers a URL protocol handler.
    /// </summary>
    /// <param name="scheme">
    /// <para>URL 协议方案（如 "uotan"）。</para>
    /// The URL protocol scheme (e.g. "uotan").
    /// </param>
    /// <param name="exePath">
    /// <para>处理协议请求的可执行文件路径。</para>
    /// The executable file path that handles the protocol request.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    Task RegisterUrlProtocolAsync(string scheme, string exePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>异步将目录添加到系统 PATH。</para>
    /// Asynchronously adds a directory to the system PATH.
    /// </summary>
    /// <param name="directory">
    /// <para>要添加到 PATH 的目录路径。</para>
    /// The directory path to add to PATH.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    Task AddToSystemPathAsync(string directory, CancellationToken cancellationToken = default);
}
