using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

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

    /// <summary>
    /// <para>获取或设置发布渠道类型。</para>
    /// Gets or sets the release channel type.
    /// </summary>
    public ReleaseChannel Channel { get; set; } = ReleaseChannel.Release;

    /// <summary>
    /// <para>获取或设置渠道的本地化显示名称。</para>
    /// Gets or sets the localized display name of the channel.
    /// </summary>
    public string ChannelDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置可用组件列表。</para>
    /// Gets or sets the list of available components.
    /// </summary>
    public List<ComponentDefinition> AvailableComponents { get; set; } = [];
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

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }
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

/// <summary>
/// <para>表示发布渠道的类型。</para>
/// Represents the type of release channel.
/// </summary>
public enum ReleaseChannel
{
    /// <summary>
    /// <para>正式版渠道，从 GitHub latest release 获取稳定发布版本。</para>
    /// Release channel, fetching stable release versions from GitHub latest release.
    /// </summary>
    Release = 0,

    /// <summary>
    /// <para>预发布版渠道，从 GitHub prerelease releases 获取预发布版本。</para>
    /// Pre-release channel, fetching pre-release versions from GitHub prerelease releases.
    /// </summary>
    PreRelease = 1,

    /// <summary>
    /// <para>测试版渠道，从指定 beta tag 获取测试版本。</para>
    /// Beta channel, fetching beta versions from the specified beta tag.
    /// </summary>
    Beta = 2,

    /// <summary>
    /// <para>每日构建版渠道，从指定 nightly tag 获取每日构建版本。</para>
    /// Nightly channel, fetching nightly build versions from the specified nightly tag.
    /// </summary>
    Nightly = 3,
}

/// <summary>
/// <para>表示发布渠道的详细配置信息。</para>
/// Represents detailed configuration information for a release channel.
/// </summary>
public sealed class ReleaseChannelInfo
{
    /// <summary>
    /// <para>获取或设置渠道类型。</para>
    /// Gets or sets the channel type.
    /// </summary>
    public ReleaseChannel Channel { get; set; }

    /// <summary>
    /// <para>获取或设置渠道的本地化显示名称。</para>
    /// Gets or sets the localized display name of the channel.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置渠道的本地化描述。</para>
    /// Gets or sets the localized description of the channel.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置渠道对应的 API 查询端点相对路径。</para>
    /// Gets or sets the relative API endpoint path for the channel.
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置当前渠道是否可用。</para>
    /// Gets or sets whether the channel is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// <para>获取或设置渠道的稳定性等级（1-4，1最稳定）。</para>
    /// Gets or sets the stability level of the channel (1-4, 1 being the most stable).
    /// </summary>
    public int StabilityLevel { get; set; } = 1;
}

/// <summary>
/// <para>表示安装组件的定义信息。</para>
/// Represents the definition information of an installation component.
/// </summary>
public sealed partial class ComponentDefinition : ObservableObject
{
    /// <summary>
    /// <para>获取或设置组件的唯一标识符。</para>
    /// Gets or sets the unique identifier of the component.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置组件的本地化显示名称。</para>
    /// Gets or sets the localized display name of the component.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置组件的本地化描述。</para>
    /// Gets or sets the localized description of the component.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置组件的大小（字节）。</para>
    /// Gets or sets the size of the component in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// <para>获取或设置组件是否为必选组件。</para>
    /// Gets or sets whether the component is required.
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// <para>获取或设置组件是否默认选中。</para>
    /// Gets or sets whether the component is selected by default.
    /// </summary>
    [JsonPropertyName("is_selected_by_default")]
    public bool IsSelectedByDefault { get; set; } = true;

    /// <summary>
    /// <para>获取或设置组件包含的文件模式列表。</para>
    /// Gets or sets the list of file patterns included in the component.
    /// </summary>
    [JsonPropertyName("file_patterns")]
    public List<string> FilePatterns { get; set; } = [];

    /// <summary>
    /// <para>获取或设置组件是否被用户选中。</para>
    /// Gets or sets whether the component is selected by the user.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected = true;
}

/// <summary>
/// <para>表示版本安装记录。</para>
/// Represents a version installation record.
/// </summary>
public sealed class VersionRecord
{
    /// <summary>
    /// <para>获取或设置版本号。</para>
    /// Gets or sets the version string.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置安装时间（ISO 8601 格式）。</para>
    /// Gets or sets the installation time in ISO 8601 format.
    /// </summary>
    [JsonPropertyName("install_time")]
    public string InstallTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置安装渠道。</para>
    /// Gets or sets the installation channel.
    /// </summary>
    [JsonPropertyName("channel")]
    public ReleaseChannel Channel { get; set; }

    /// <summary>
    /// <para>获取或设置安装路径。</para>
    /// Gets or sets the installation path.
    /// </summary>
    [JsonPropertyName("install_path")]
    public string InstallPath { get; set; } = string.Empty;
}

/// <summary>
/// <para>表示增量更新包信息。</para>
/// Represents delta update package information.
/// </summary>
public sealed class DeltaUpdateInfo
{
    /// <summary>
    /// <para>获取或设置增量更新包的下载 URL。</para>
    /// Gets or sets the download URL of the delta update package.
    /// </summary>
    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置源版本号。</para>
    /// Gets or sets the source version string.
    /// </summary>
    [JsonPropertyName("from_version")]
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置目标版本号。</para>
    /// Gets or sets the target version string.
    /// </summary>
    [JsonPropertyName("to_version")]
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>获取或设置增量更新包的大小（字节）。</para>
    /// Gets or sets the size of the delta update package in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// <para>获取或设置增量更新包的校验值。</para>
    /// Gets or sets the checksum of the delta update package.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }
}


