using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>GitHub 镜像站点服务接口，提供内置镜像站点列表和镜像 URL 生成功能</para>
/// GitHub mirror site service interface that provides built-in mirror site list and mirror URL generation
/// </summary>
public interface IGitHubMirrorService
{
    /// <summary>
    /// <para>获取所有内置的 GitHub 镜像站点列表</para>
    /// Get all built-in GitHub mirror site list
    /// </summary>
    /// <returns>
    /// <para>镜像站点列表</para>
    /// The list of mirror sites
    /// </returns>
    IReadOnlyList<GitHubMirrorSite> GetMirrorSites();

    /// <summary>
    /// <para>通过镜像站点将 GitHub 原始 URL 转换为镜像 URL</para>
    /// Convert a GitHub original URL to a mirror URL through the mirror site
    /// </summary>
    /// <param name="mirrorSite">
    /// <para>镜像站点</para>
    /// The mirror site
    /// </param>
    /// <param name="originalUrl">
    /// <para>GitHub 原始下载 URL</para>
    /// The original GitHub download URL
    /// </param>
    /// <returns>
    /// <para>转换后的镜像 URL</para>
    /// The converted mirror URL
    /// </returns>
    string CreateMirrorUrl(GitHubMirrorSite mirrorSite, string originalUrl);
}
