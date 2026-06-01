using UotanInstaller.App.Services.Deployment;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>安装后配置服务实现，提供文件关联注册、URL 协议注册和 PATH 环境变量配置功能。</para>
/// Post-install configuration service implementation that provides file association registration, URL protocol registration, and PATH environment variable configuration.
/// </summary>
public sealed class PostInstallConfigService : IPostInstallConfigService
{
    private readonly IPlatformAdapter _platformAdapter;

    /// <summary>
    /// <para>初始化 PostInstallConfigService 实例。</para>
    /// Initializes a new instance of PostInstallConfigService.
    /// </summary>
    /// <param name="platformAdapter">
    /// <para>平台适配器。</para>
    /// The platform adapter.
    /// </param>
    public PostInstallConfigService(IPlatformAdapter platformAdapter)
    {
        _platformAdapter = platformAdapter;
    }

    /// <inheritdoc/>
    public Task RegisterFileAssociationAsync(string fileExtension, string exePath, CancellationToken cancellationToken = default)
    {
        return _platformAdapter.RegisterProtocolHandlerAsync(fileExtension, exePath, cancellationToken);
    }

    /// <inheritdoc/>
    public Task RegisterUrlProtocolAsync(string scheme, string exePath, CancellationToken cancellationToken = default)
    {
        return _platformAdapter.RegisterProtocolHandlerAsync(scheme, exePath, cancellationToken);
    }

    /// <inheritdoc/>
    public Task AddToSystemPathAsync(string directory, CancellationToken cancellationToken = default)
    {
        return _platformAdapter.AddToSystemPathAsync(directory, cancellationToken);
    }
}
