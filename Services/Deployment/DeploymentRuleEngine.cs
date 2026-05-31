using System.Runtime.InteropServices;

namespace UotanInstaller.App.Services.Deployment;

/// <summary>
/// <para>提供部署规则引擎的默认实现，包括安装包选择、文件规则评估及平台约束检查。</para>
/// Provides the default implementation of the deployment rule engine, including package selection, file rule evaluation, and platform constraint checking.
/// </summary>
public sealed class DeploymentRuleEngine : IDeploymentRuleEngine
{
    private readonly IPlatformDetector _platformDetector;
    private IReadOnlyList<FileDeploymentRule> _fileRules = [];

    /// <summary>
    /// <para>使用指定的平台检测器初始化 DeploymentRuleEngine 的新实例。</para>
    /// Initializes a new instance of DeploymentRuleEngine with the specified platform detector.
    /// </summary>
    /// <param name="platformDetector">
    /// <para>用于检测当前运行平台信息的平台检测器实例。</para>
    /// The platform detector instance used to detect the current runtime platform information.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <para>当 platformDetector 为 null 时抛出。</para>
    /// Thrown when platformDetector is null.
    /// </exception>
    public DeploymentRuleEngine(IPlatformDetector platformDetector)
    {
        _platformDetector = platformDetector ?? throw new ArgumentNullException(nameof(platformDetector));
    }

    /// <summary>
    /// <para>获取或设置当前引擎使用的文件部署规则列表。</para>
    /// Gets or sets the list of file deployment rules used by the current engine.
    /// </summary>
    public IReadOnlyList<FileDeploymentRule> FileRules
    {
        get => _fileRules;
        set => _fileRules = value ?? [];
    }

    /// <summary>
    /// <para>从候选安装包源中选择最适合当前平台的包。优先匹配操作系统和架构完全一致的包，其次尝试仅匹配操作系统的包。</para>
    /// Selects the most suitable package for the current platform from the candidate package sources. Prioritizes exact OS and architecture match, then falls back to OS-only match.
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
    public Task<PackageSource?> SelectPackageAsync(IEnumerable<PackageSource> sources, CancellationToken ct)
    {
        var platform = _platformDetector.Detect();
        PackageSource? osOnlyMatch = null;

        foreach (var source in sources)
        {
            ct.ThrowIfCancellationRequested();

            if (source.TargetOS != platform.OS)
                continue;

            if (source.TargetArchitecture == platform.Architecture)
                return Task.FromResult<PackageSource?>(source);

            osOnlyMatch ??= source;
        }

        return Task.FromResult(osOnlyMatch);
    }

    /// <summary>
    /// <para>根据文件路径评估适用的文件部署规则。支持扩展名匹配（*.ext）、目录匹配（prefix/*）和精确匹配。</para>
    /// Evaluates applicable file deployment rules based on the file path. Supports extension matching (*.ext), directory matching (prefix/*), and exact match.
    /// </summary>
    /// <param name="filePath">
    /// <para>要评估的文件相对路径。</para>
    /// The relative file path to evaluate.
    /// </param>
    /// <returns>
    /// <para>适用于该文件的部署规则列表；若无匹配规则则返回空列表。</para>
    /// The list of deployment rules applicable to the file; an empty list if no rules match.
    /// </returns>
    public IReadOnlyList<FileDeploymentRule> EvaluateFileRules(string filePath)
    {
        return EvaluateFileRules(filePath, _fileRules);
    }

    /// <summary>
    /// <para>异步检查当前平台是否满足所有给定的约束条件，包括操作系统、最低版本和最低磁盘空间要求。</para>
    /// Asynchronously checks whether the current platform satisfies all given constraints, including OS, minimum version, and minimum disk space requirements.
    /// </summary>
    /// <param name="constraints">
    /// <para>要检查的平台约束条件集合。</para>
    /// The collection of platform constraints to check.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <param name="installPath">
    /// <para>目标安装路径，用于确定检查磁盘空间的目标分区；为 null 时使用系统默认分区。</para>
    /// The target installation path used to determine the partition for disk space checking; null uses the system default partition.
    /// </param>
    /// <returns>
    /// <para>若满足所有约束返回 true，否则返回 false。</para>
    /// True if all constraints are satisfied; otherwise false.
    /// </returns>
    public Task<bool> CheckPlatformConstraintsAsync(IEnumerable<PlatformConstraint> constraints, CancellationToken ct, string? installPath = null)
    {
        var platform = _platformDetector.Detect();

        foreach (var constraint in constraints)
        {
            ct.ThrowIfCancellationRequested();

            if (constraint.RequiredOS != PlatformOS.Unknown && constraint.RequiredOS != platform.OS)
                return Task.FromResult(false);

            if (constraint.MinimumVersion is not null && !IsVersionSatisfied(platform.Version, constraint.MinimumVersion))
                return Task.FromResult(false);

            if (constraint.MinimumDiskSpaceBytes > 0 && !CheckDiskSpace(constraint.MinimumDiskSpaceBytes, installPath))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    internal static IReadOnlyList<FileDeploymentRule> EvaluateFileRules(string filePath, IReadOnlyList<FileDeploymentRule> rules)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];

        var normalizedPath = filePath.Replace('\\', '/');
        var matches = new List<FileDeploymentRule>();

        foreach (var rule in rules)
        {
            if (string.IsNullOrEmpty(rule.Pattern))
                continue;

            var normalizedPattern = rule.Pattern.Replace('\\', '/');

            if (MatchesPattern(normalizedPath, normalizedPattern))
                matches.Add(rule);
        }

        return matches;
    }

    internal static bool MatchesPattern(string path, string pattern)
    {
        if (pattern.StartsWith("*."))
        {
            var ext = pattern.Substring(1);
            return path.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith("/*"))
        {
            var dir = pattern.Substring(0, pattern.Length - 2);
            if (path.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
            {
                if (path.Length == dir.Length)
                    return true;

                return path.Length > dir.Length && path[dir.Length] == '/';
            }

            return false;
        }

        return string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsVersionSatisfied(string currentVersion, string minimumVersion)
    {
        if (string.IsNullOrEmpty(currentVersion))
            return false;

        var currentParts = ParseVersionParts(currentVersion);
        var minimumParts = ParseVersionParts(minimumVersion);
        var maxLen = Math.Max(currentParts.Count, minimumParts.Count);

        for (var i = 0; i < maxLen; i++)
        {
            var current = i < currentParts.Count ? currentParts[i] : 0;
            var minimum = i < minimumParts.Count ? minimumParts[i] : 0;

            if (current > minimum) return true;
            if (current < minimum) return false;
        }

        return true;
    }

    internal static List<int> ParseVersionParts(string version)
    {
        var parts = new List<int>();
        var start = 0;

        for (var i = 0; i <= version.Length; i++)
        {
            if (i == version.Length || version[i] == '.' || !char.IsDigit(version[i]))
            {
                if (i > start && int.TryParse(version.Substring(start, i - start), out var num))
                    parts.Add(num);

                if (i < version.Length && !char.IsDigit(version[i]) && version[i] != '.')
                    break;

                start = i + 1;
            }
        }

        return parts;
    }

    internal static bool CheckDiskSpace(long minimumBytes, string? installPath = null)
    {
        try
        {
            var rootPath = GetRootPath(installPath);
            var drive = new DriveInfo(rootPath);
            return drive.IsReady && drive.AvailableFreeSpace >= minimumBytes;
        }
        catch
        {
            return false;
        }
    }

    private static string GetRootPath(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return fullPath.Substring(0, 2) + "\\";
                }

                return "/";
            }
            catch
            {
            }
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\" : "/";
    }
}
