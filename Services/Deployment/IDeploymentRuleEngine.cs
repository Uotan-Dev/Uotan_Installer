using System.Runtime.InteropServices;

namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>定义文件部署规则，用于控制文件在安装过程中的目标位置和覆盖行为。</para>
/// Defines a file deployment rule that controls the target location and overwrite behavior of files during installation.
/// </summary>
public sealed class FileDeploymentRule
{
    /// <summary>
    /// <para>获取或初始化文件匹配模式，例如 "*.config"。</para>
    /// Gets or initializes the file matching pattern, e.g. "*.config".
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化目标子目录的相对路径。</para>
    /// Gets or initializes the relative path of the target subdirectory.
    /// </summary>
    public string TargetSubdirectory { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化是否覆盖已存在的同名文件。</para>
    /// Gets or initializes whether to overwrite existing files with the same name.
    /// </summary>
    public bool Overwrite { get; init; }
}

/// <summary>
/// <para>定义平台约束条件，用于验证当前运行环境是否满足部署要求。</para>
/// Defines platform constraints used to verify whether the current runtime environment meets deployment requirements.
/// </summary>
public sealed class PlatformConstraint
{
    /// <summary>
    /// <para>获取或初始化要求的操作系统平台。</para>
    /// Gets or initializes the required operating system platform.
    /// </summary>
    public PlatformOS RequiredOS { get; init; }

    /// <summary>
    /// <para>获取或初始化所需的最低操作系统版本，可为 null 表示无版本要求。</para>
    /// Gets or initializes the minimum required OS version; null means no version requirement.
    /// </summary>
    public string? MinimumVersion { get; init; }

    /// <summary>
    /// <para>获取或初始化所需的最低磁盘空间字节数。</para>
    /// Gets or initializes the minimum required disk space in bytes.
    /// </summary>
    public long MinimumDiskSpaceBytes { get; init; }
}

/// <summary>
/// <para>定义安装包的下载源信息，包含 URL、校验值及目标平台等元数据。</para>
/// Defines the download source information for an installation package, including URL, checksum, and target platform metadata.
/// </summary>
public sealed class PackageSource
{
    /// <summary>
    /// <para>获取或初始化安装包的下载 URL。</para>
    /// Gets or initializes the download URL of the package.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// <para>获取或初始化安装包的 SHA256 校验值，可为 null 表示不校验。</para>
    /// Gets or initializes the SHA256 checksum of the package; null means no verification.
    /// </summary>
    public string? Sha256 { get; init; }

    /// <summary>
    /// <para>获取或初始化此安装包面向的目标操作系统。</para>
    /// Gets or initializes the target operating system for this package.
    /// </summary>
    public PlatformOS TargetOS { get; init; }

    /// <summary>
    /// <para>获取或初始化此安装包面向的目标处理器架构。</para>
    /// Gets or initializes the target processor architecture for this package.
    /// </summary>
    public Architecture TargetArchitecture { get; init; }

    /// <summary>
    /// <para>获取或初始化安装包的归档格式，例如 "zip"、"tar.gz"，可为 null 表示自动检测。</para>
    /// Gets or initializes the archive format of the package, e.g. "zip", "tar.gz"; null means auto-detect.
    /// </summary>
    public string? ArchiveFormat { get; init; }
}

/// <summary>
/// <para>提供部署规则引擎能力，包括安装包选择、文件规则评估及平台约束检查。</para>
/// Provides deployment rule engine capabilities including package selection, file rule evaluation, and platform constraint checking.
/// </summary>
public interface IDeploymentRuleEngine
{
    /// <summary>
    /// <para>从候选安装包源中选择最适合当前平台的包。</para>
    /// Selects the most suitable package for the current platform from the candidate package sources.
    /// </summary>
    /// <param name="sources">
    /// <para>候选安装包源集合。</para>
    /// The collection of candidate package sources.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>匹配的安装包源，若无匹配则返回 null。</para>
    /// The matched package source, or null if no match is found.
    /// </returns>
    Task<PackageSource?> SelectPackageAsync(IEnumerable<PackageSource> sources, CancellationToken ct);

    /// <summary>
    /// <para>根据文件路径评估适用的文件部署规则。</para>
    /// Evaluates applicable file deployment rules based on the file path.
    /// </summary>
    /// <param name="filePath">
    /// <para>要评估的文件路径。</para>
    /// The file path to evaluate.
    /// </param>
    /// <returns>
    /// <para>适用于该文件的部署规则列表。</para>
    /// The list of deployment rules applicable to the file.
    /// </returns>
    IReadOnlyList<FileDeploymentRule> EvaluateFileRules(string filePath);

    /// <summary>
    /// <para>异步检查当前平台是否满足所有给定的约束条件。</para>
    /// Asynchronously checks whether the current platform satisfies all given constraints.
    /// </summary>
    /// <param name="constraints">
    /// <para>要检查的平台约束条件集合。</para>
    /// The collection of platform constraints to check.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>若满足所有约束返回 true，否则返回 false。</para>
    /// True if all constraints are satisfied; otherwise false.
    /// </returns>
    Task<bool> CheckPlatformConstraintsAsync(IEnumerable<PlatformConstraint> constraints, CancellationToken ct);
}
