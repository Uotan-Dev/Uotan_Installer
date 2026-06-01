using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>组件管理器接口，提供组件清单解析和文件规则过滤功能。</para>
/// Component manager interface that provides component manifest parsing and file rule filtering.
/// </summary>
public interface IComponentManager
{
    /// <summary>
    /// <para>异步从 GitHub Release 的 body 中解析可用组件清单。</para>
    /// Asynchronously parses available component manifest from the GitHub Release body.
    /// </summary>
    /// <param name="releaseBody">
    /// <para>GitHub Release 的 body 文本。</para>
    /// The GitHub Release body text.
    /// </param>
    /// <param name="cancellationToken">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>可用组件列表。</para>
    /// The list of available components.
    /// </returns>
    Task<IReadOnlyList<ComponentDefinition>> ParseComponentsAsync(string releaseBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>根据选中的组件列表解析应包含的文件路径模式。</para>
    /// Resolves file path patterns that should be included based on the selected component list.
    /// </summary>
    /// <param name="selectedComponents">
    /// <para>选中的组件列表。</para>
    /// The list of selected components.
    /// </param>
    /// <returns>
    /// <para>应包含的文件路径模式列表。</para>
    /// The list of file path patterns to include.
    /// </returns>
    IReadOnlyList<string> ResolveFilePatterns(IReadOnlyList<ComponentDefinition> selectedComponents);

    /// <summary>
    /// <para>判断指定文件路径是否应被包含在安装中。</para>
    /// Determines whether the specified file path should be included in the installation.
    /// </summary>
    /// <param name="filePath">
    /// <para>文件相对路径。</para>
    /// The relative file path.
    /// </param>
    /// <param name="includePatterns">
    /// <para>包含的文件模式列表。</para>
    /// The list of file patterns to include.
    /// </param>
    /// <returns>
    /// <para>如果应包含返回 true，否则返回 false。</para>
    /// True if the file should be included; otherwise false.
    /// </returns>
    bool ShouldIncludeFile(string filePath, IReadOnlyList<string> includePatterns);
}
