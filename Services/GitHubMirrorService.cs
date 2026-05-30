using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>GitHub 镜像站点服务实现，提供内置镜像站点列表和镜像 URL 生成功能</para>
/// GitHub mirror site service implementation that provides built-in mirror site list and mirror URL generation
/// </summary>
public sealed class GitHubMirrorService : IGitHubMirrorService
{
    private static readonly List<GitHubMirrorSite> MirrorSites =
    [
        new() { Name = "GitHub 直连", BaseUrl = string.Empty, IsEnabled = true },
        new() { Name = "h233", BaseUrl = "https://gh.h233.eu.org/https://github.com", IsEnabled = true },
        new() { Name = "rapidgit", BaseUrl = "https://rapidgit.jjda.de5.net/https://github.com", IsEnabled = true },
        new() { Name = "ddlc", BaseUrl = "https://gh.ddlc.top/https://github.com", IsEnabled = true },
        new() { Name = "gh-proxy.org", BaseUrl = "https://gh-proxy.org/https://github.com", IsEnabled = true },
        new() { Name = "cdn.gh-proxy.org", BaseUrl = "https://cdn.gh-proxy.org/https://github.com", IsEnabled = true },
        new() { Name = "edgeone", BaseUrl = "https://edgeone.gh-proxy.org/https://github.com", IsEnabled = true },
        new() { Name = "isteed", BaseUrl = "https://cors.isteed.cc/github.com", IsEnabled = true },
        new() { Name = "ghproxy.it", BaseUrl = "https://ghproxy.it/https://github.com", IsEnabled = true },
        new() { Name = "boki.moe", BaseUrl = "https://github.boki.moe/https://github.com", IsEnabled = true },
        new() { Name = "jasonzeng", BaseUrl = "https://gh.jasonzeng.dev/https://github.com", IsEnabled = true },
        new() { Name = "monlor", BaseUrl = "https://gh.monlor.com/https://github.com", IsEnabled = true },
        new() { Name = "tbedu", BaseUrl = "https://github.tbedu.top/https://github.com", IsEnabled = true },
        new() { Name = "geekery", BaseUrl = "https://github.geekery.cn/https://github.com", IsEnabled = true },
        new() { Name = "ednovas", BaseUrl = "https://github.ednovas.xyz/https://github.com", IsEnabled = true },
        new() { Name = "geekertao", BaseUrl = "https://ghfile.geekertao.top/https://github.com", IsEnabled = true },
        new() { Name = "keleyaa", BaseUrl = "https://ghp.keleyaa.com/https://github.com", IsEnabled = true },
        new() { Name = "chjina", BaseUrl = "https://gh.chjina.com/https://github.com", IsEnabled = true },
        new() { Name = "hwinzniej", BaseUrl = "https://ghpxy.hwinzniej.top/https://github.com", IsEnabled = true },
        new() { Name = "crashmc", BaseUrl = "https://cdn.crashmc.com/https://github.com", IsEnabled = true },
        new() { Name = "yylx", BaseUrl = "https://git.yylx.win/https://github.com", IsEnabled = true },
        new() { Name = "mrhjx", BaseUrl = "https://gitproxy.mrhjx.cn/https://github.com", IsEnabled = true },
        new() { Name = "cxkpro", BaseUrl = "https://ghproxy.cxkpro.top/https://github.com", IsEnabled = true },
        new() { Name = "xxooo", BaseUrl = "https://gh.xxooo.cf/https://github.com", IsEnabled = true },
        new() { Name = "idayer", BaseUrl = "https://gh.idayer.com/https://github.com", IsEnabled = true },
        new() { Name = "npee", BaseUrl = "https://down.npee.cn/?https://github.com", IsEnabled = true },
        new() { Name = "ihtw", BaseUrl = "https://raw.ihtw.moe/github.com", IsEnabled = true },
        new() { Name = "xixu", BaseUrl = "https://xget.xi-xu.me/gh", IsEnabled = true },
        new() { Name = "zwy", BaseUrl = "https://gh.zwy.one/https://github.com", IsEnabled = true },
        new() { Name = "monkeyray", BaseUrl = "https://ghproxy.monkeyray.net/https://github.com", IsEnabled = true },
        new() { Name = "ghproxy.net", BaseUrl = "https://ghproxy.net/https://github.com", IsEnabled = true },
        new() { Name = "ghfast", BaseUrl = "https://ghfast.top/https://github.com", IsEnabled = true },
        new() { Name = "wget.la", BaseUrl = "https://wget.la/https://github.com", IsEnabled = true },
    ];

    /// <inheritdoc/>
    public IReadOnlyList<GitHubMirrorSite> GetMirrorSites()
    {
        return MirrorSites.Where(s => s.IsEnabled).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public string CreateMirrorUrl(GitHubMirrorSite mirrorSite, string originalUrl)
    {
        if (string.IsNullOrEmpty(mirrorSite.BaseUrl))
        {
            return originalUrl;
        }

        if (mirrorSite.BaseUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var path = originalUrl.Replace("https://github.com", "", StringComparison.OrdinalIgnoreCase);
            return $"{mirrorSite.BaseUrl}{path}";
        }

        return $"{mirrorSite.BaseUrl}{originalUrl}";
    }
}
