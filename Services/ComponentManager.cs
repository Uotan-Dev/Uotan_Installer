using System.Text.Json;
using UotanInstaller.App.Models;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>组件管理器实现，从 GitHub Release body 解析组件清单并提供文件过滤功能。</para>
/// Component manager implementation that parses component manifest from GitHub Release body and provides file filtering.
/// </summary>
public sealed class ComponentManager : IComponentManager
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<ComponentDefinition>> ParseComponentsAsync(string releaseBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(releaseBody))
            return Task.FromResult<IReadOnlyList<ComponentDefinition>>([]);

        var json = ExtractComponentsJson(releaseBody);
        if (json is null)
            return Task.FromResult<IReadOnlyList<ComponentDefinition>>([]);

        var components = JsonSerializer.Deserialize(json, AppJsonContext.Default.ComponentDefinitionArray);
        if (components is null)
            return Task.FromResult<IReadOnlyList<ComponentDefinition>>([]);

        foreach (var component in components)
        {
            component.IsSelected = component.IsRequired || component.IsSelectedByDefault;
        }

        return Task.FromResult<IReadOnlyList<ComponentDefinition>>(components);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ResolveFilePatterns(IReadOnlyList<ComponentDefinition> selectedComponents)
    {
        var patterns = new List<string>();
        foreach (var component in selectedComponents)
        {
            if (component.IsSelected)
            {
                patterns.AddRange(component.FilePatterns);
            }
        }
        return patterns;
    }

    /// <inheritdoc/>
    public bool ShouldIncludeFile(string filePath, IReadOnlyList<string> includePatterns)
    {
        if (includePatterns.Count == 0)
            return true;

        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var pattern in includePatterns)
        {
            if (WildcardMatch(normalizedPath, pattern.Replace('\\', '/')))
                return true;
        }

        return false;
    }

    private static string? ExtractComponentsJson(string body)
    {
        var startMarker = "```components";
        var endMarker = "```";

        var startIndex = body.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
            return null;

        startIndex += startMarker.Length;

        var endIndex = body.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            return null;

        return body.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static bool WildcardMatch(string input, string pattern)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);

        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
