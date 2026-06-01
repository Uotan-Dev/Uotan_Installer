namespace UotanInstaller.App.Services.Deployment;

using UotanInstaller.App.Models;

/// <summary>
/// <para>封装部署流程的完整配置信息。</para>
/// Encapsulates the complete configuration for the deployment process.
/// </summary>
public sealed class DeploymentConfiguration
{
    /// <summary>
    /// <para>获取或初始化应用程序名称，默认为空字符串。</para>
    /// Gets or initializes the application name; defaults to an empty string.
    /// </summary>
    public string AppName { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化自定义安装路径，为 null 时使用平台默认路径。</para>
    /// Gets or initializes the custom installation path; null to use the platform default path.
    /// </summary>
    public string? InstallPath { get; init; }

    /// <summary>
    /// <para>获取或初始化安装包文件名，为 null 时从下载 URL 推断。</para>
    /// Gets or initializes the package file name; null to infer from the download URL.
    /// </summary>
    public string? PackageFileName { get; init; }

    /// <summary>
    /// <para>获取或初始化安装包下载源列表，默认为空列表。</para>
    /// Gets or initializes the list of package download sources; defaults to an empty list.
    /// </summary>
    public List<PackageSource> PackageSources { get; init; } = [];

    /// <summary>
    /// <para>获取或初始化文件部署规则列表，默认为空列表。</para>
    /// Gets or initializes the list of file deployment rules; defaults to an empty list.
    /// </summary>
    public List<FileDeploymentRule> FileRules { get; init; } = [];

    /// <summary>
    /// <para>获取或初始化平台约束条件列表，默认为空列表。</para>
    /// Gets or initializes the list of platform constraints; defaults to an empty list.
    /// </summary>
    public List<PlatformConstraint> PlatformConstraints { get; init; } = [];

    /// <summary>
    /// <para>获取或初始化是否创建桌面快捷方式，默认为 true。</para>
    /// Gets or initializes whether to create a desktop shortcut; defaults to true.
    /// </summary>
    public bool CreateDesktopShortcut { get; init; } = true;

    /// <summary>
    /// <para>获取或初始化安装完成后是否启动应用，默认为 false。</para>
    /// Gets or initializes whether to launch the application after installation; defaults to false.
    /// </summary>
    public bool LaunchAfterInstall { get; init; }

    /// <summary>
    /// <para>获取或初始化是否启用离线安装模式，默认为 false。</para>
    /// Gets or initializes whether to enable offline installation mode; defaults to false.
    /// </summary>
    public bool OfflineMode { get; init; }

    /// <summary>
    /// <para>获取或初始化选定的镜像下载 URL，为 null 时自动选择。</para>
    /// Gets or initializes the selected mirror download URL; null to auto-select.
    /// </summary>
    public string? SelectedMirrorUrl { get; init; }

    /// <summary>
    /// <para>获取或初始化安装包的 SHA256 校验值，为 null 时不校验。</para>
    /// Gets or initializes the SHA256 checksum of the package; null to skip verification.
    /// </summary>
    public string? Sha256 { get; init; }

    /// <summary>
    /// <para>获取或初始化发布渠道类型，默认为正式版。</para>
    /// Gets or initializes the release channel type; defaults to Release.
    /// </summary>
    public ReleaseChannel Channel { get; init; } = ReleaseChannel.Release;
}
