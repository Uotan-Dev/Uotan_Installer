using Microsoft.Extensions.DependencyInjection;
using UotanInstaller.App.Services.Deployment;
using UotanInstaller.App.Services.Deployment.Platforms;
using UotanInstaller.App.ViewModels;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>服务集合扩展方法，提供所有应用服务的统一DI注册</para>
/// Service collection extension methods that provide unified DI registration for all application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// <para>注册所有柚坛安装器服务到依赖注入容器</para>
    /// Register all Uotan installer services into the dependency injection container
    /// </summary>
    /// <param name="services">
    /// <para>服务集合</para>
    /// The service collection
    /// </param>
    /// <returns>
    /// <para>注册后的服务集合，支持链式调用</para>
    /// The service collection after registration, supporting fluent chaining
    /// </returns>
    public static IServiceCollection AddUotanServices(this IServiceCollection services)
    {
        services.AddSingleton<IGitHubMirrorService, GitHubMirrorService>();
        services.AddSingleton<IHttpService, HttpService>();
        services.AddSingleton<IInstallLogger, InstallLogger>();
        services.AddSingleton<IChannelService, ChannelService>();
        services.AddSingleton<IReleaseApiService, ReleaseApiService>();
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IDialogService>(sp =>
            new DialogService(() => sp.GetRequiredService<WindowProvider>().Window, sp.GetRequiredService<ILocalizationService>()));
        services.AddSingleton<IPlatformDetector, PlatformDetector>();
        services.AddSingleton<IPlatformAdapter>(sp =>
            PlatformAdapterFactory.Create(sp.GetRequiredService<IPlatformDetector>()));
        services.AddSingleton<IDeploymentRuleEngine, DeploymentRuleEngine>();
        services.AddSingleton<IComponentManager, ComponentManager>();
        services.AddSingleton<IVersionManager, VersionManager>();
        services.AddSingleton<IDeltaUpdateService, DeltaUpdateService>();
        services.AddSingleton<IInstallerService, InstallerService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<MainWindowViewModel>();
        return services;
    }
}
