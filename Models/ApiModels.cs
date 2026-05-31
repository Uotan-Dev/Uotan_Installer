using System.Text.Json.Serialization;

namespace UotanInstaller.App.Models;

/// <summary>
/// <para>通用补丁数据</para>
/// Generic patch data
/// </summary>
public sealed class GenericPatchData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("mirrors")]
    public List<GenericPatchPackageMirror> Mirrors { get; set; } = [];

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;
}

/// <summary>
/// <para>通用补丁包镜像</para>
/// Generic patch package mirror
/// </summary>
public sealed class GenericPatchPackageMirror
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("mirror_name")]
    public string MirrorName { get; set; } = string.Empty;

    public double? Speed { get; set; }

    public string DisplayUrl
    {
        get
        {
            if (string.IsNullOrEmpty(Url)) return string.Empty;
            try
            {
                return new Uri(Url).Host;
            }
            catch
            {
                return Url;
            }
        }
    }
}

/// <summary>
/// <para>安装器配置</para>
/// Installer configuration
/// </summary>
public sealed class InstallerConfig
{
    public string Version { get; set; } = string.Empty;

    public bool IsUpdate { get; set; }

    public bool IsOfflineMode { get; set; }

    public string? CurrVersion { get; set; }

    /// <summary>
    /// <para>获取或设置安装路径。</para>
    /// Gets or sets the installation path.
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;
}

/// <summary>
/// <para>安装进度信息</para>
/// Install progress information
/// </summary>
public sealed class InstallProgress
{
    public InstallProgressKind Kind { get; set; }

    public string Message { get; set; } = string.Empty;

    public double ProgressValue { get; set; }
}

/// <summary>
/// <para>安装进度类型</para>
/// Install progress kind
/// </summary>
public enum InstallProgressKind
{
    Downloading,
    Verifying,
    Installing,
    Completed,
    Failed,
}

/// <summary>
/// <para>语义化版本信息</para>
/// Semantic version information
/// </summary>
public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public ulong Major { get; }
    public ulong Minor { get; }
    public ulong Build { get; }
    public ulong Revision { get; }

    public SemanticVersion(ulong major, ulong minor, ulong build, ulong revision)
    {
        Major = major;
        Minor = minor;
        Build = build;
        Revision = revision;
    }

    public static SemanticVersion Parse(string versionString)
    {
        if (versionString.Length > 0 && (versionString[0] == 'v' || versionString[0] == 'V')) versionString = versionString[1..];
        var parts = versionString.Split('.');
        var major = parts.Length > 0 && ulong.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && ulong.TryParse(parts[1], out var mi) ? mi : 0;
        var build = parts.Length > 2 && ulong.TryParse(parts[2], out var b) ? b : 0;
        var revision = parts.Length > 3 && ulong.TryParse(parts[3], out var r) ? r : 0;
        return new SemanticVersion(major, minor, build, revision);
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        int c = Major.CompareTo(other.Major);
        if (c != 0) return c;
        c = Minor.CompareTo(other.Minor);
        if (c != 0) return c;
        c = Build.CompareTo(other.Build);
        if (c != 0) return c;
        return Revision.CompareTo(other.Revision);
    }

    public bool Equals(SemanticVersion? other)
    {
        if (other is null) return false;
        return Major == other.Major && Minor == other.Minor && Build == other.Build && Revision == other.Revision;
    }

    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Build, Revision);

    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Major}.{Minor}.{Build}.{Revision}";
}

/// <summary>
/// <para>表示 GitHub 镜像站点配置信息。</para>
/// Represents a GitHub mirror site configuration.
/// </summary>
public sealed class GitHubMirrorSite
{
    /// <summary>
    /// <para>获取或设置镜像站点的名称。</para>
    /// Gets or sets the name of the mirror site.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置镜像站点的基础 URL。</para>
    /// Gets or sets the base URL of the mirror site.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置镜像站点是否启用。</para>
    /// Gets or sets whether the mirror site is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// <para>GitHub Release 数据模型</para>
/// GitHub Release data model
/// </summary>
public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset>? Assets { get; set; }
}

/// <summary>
/// <para>GitHub Release 资源数据模型</para>
/// GitHub Release asset data model
/// </summary>
public sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }
}
