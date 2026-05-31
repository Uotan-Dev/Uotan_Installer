using SharpCompress.Archives;
using SharpCompress.Common;

namespace UotanInstaller.App.Services.Deployment.Steps;

/// <summary>
/// <para>解压步骤，负责将归档文件解压到目标目录，支持文件部署规则和路径遍历防护。</para>
/// Extraction step responsible for extracting an archive to a target directory, supporting file deployment rules and path traversal protection.
/// </summary>
public sealed class ExtractStep : IDeploymentStep
{
    private readonly string _archivePath;
    private readonly string _targetDirectory;
    private readonly IReadOnlyList<FileDeploymentRule>? _fileRules;
    private readonly ILocalizationService _localizationService;

    /// <summary>
    /// <para>获取步骤名称。</para>
    /// Gets the step name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <para>获取步骤类型。</para>
    /// Gets the step kind.
    /// </summary>
    public DeploymentStepKind Kind { get; } = DeploymentStepKind.Extract;

    /// <summary>
    /// <para>获取或设置是否跳过此步骤。</para>
    /// Gets or sets whether to skip this step.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// <para>使用指定参数初始化 ExtractStep 的新实例。</para>
    /// Initializes a new instance of ExtractStep with the specified parameters.
    /// </summary>
    /// <param name="archivePath">
    /// <para>归档文件路径（支持 .zip 和 .tar.gz 格式）。</para>
    /// The archive file path (supports .zip and .tar.gz formats).
    /// </param>
    /// <param name="targetDirectory">
    /// <para>解压目标目录路径。</para>
    /// The target directory path for extraction.
    /// </param>
    /// <param name="localizationService">
    /// <para>本地化服务实例。</para>
    /// The localization service instance.
    /// </param>
    /// <param name="fileRules">
    /// <para>文件部署规则列表，可为 null。匹配规则的文件将被解压到指定的子目录中。</para>
    /// The list of file deployment rules, or null. Files matching a rule will be extracted to the specified subdirectory.
    /// </param>
    public ExtractStep(string archivePath, string targetDirectory, ILocalizationService localizationService, IReadOnlyList<FileDeploymentRule>? fileRules = null)
    {
        _archivePath = archivePath;
        _targetDirectory = targetDirectory;
        _localizationService = localizationService;
        _fileRules = fileRules;
        Name = _localizationService["Step_Extract"];
    }

    /// <summary>
    /// <para>异步执行解压步骤并报告进度。</para>
    /// Asynchronously executes the extraction step and reports progress.
    /// </summary>
    /// <param name="progress">
    /// <para>进度报告回调，可为 null。</para>
    /// The progress report callback, or null.
    /// </param>
    /// <param name="ct">
    /// <para>取消令牌。</para>
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// <para>步骤执行结果。</para>
    /// The step execution result.
    /// </returns>
    /// <exception cref="ExtractionException">
    /// <para>当解压失败时抛出，包含归档文件路径信息。</para>
    /// Thrown when extraction fails, containing the archive file path information.
    /// </exception>
    public async Task<DeploymentStepResult> ExecuteAsync(IProgress<DeploymentProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new DeploymentProgress
        {
            StepName = Name,
            Kind = Kind,
            ProgressValue = 0.0,
            Message = _localizationService["Step_ExtractPrepare"],
        });

        try
        {
            await Task.Run(() =>
            {
                var normalizedTarget = Path.GetFullPath(_targetDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (!Directory.Exists(normalizedTarget))
                {
                    Directory.CreateDirectory(normalizedTarget);
                }

                using var archive = ArchiveFactory.OpenArchive(_archivePath);
                var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
                var totalEntries = entries.Count;

                if (totalEntries == 0)
                {
                    return;
                }

                var extractedCount = 0;
                var lastReportTime = DateTime.UtcNow;

                foreach (var entry in entries)
                {
                    ct.ThrowIfCancellationRequested();

                    var entryKey = entry.Key ?? string.Empty;
                    var fileName = Path.GetFileName(entryKey);

                    var destinationDirectory = ResolveDestinationDirectory(normalizedTarget, entryKey, fileName);
                    var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, fileName));

                    if (!destinationPath.StartsWith(normalizedTarget + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(destinationPath, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    var overwrite = ShouldOverwrite(fileName);
                    entry.WriteToFile(destinationPath, new ExtractionOptions { Overwrite = overwrite });

                    extractedCount++;
                    var now = DateTime.UtcNow;
                    if ((now - lastReportTime).TotalMilliseconds >= 100 || extractedCount == totalEntries)
                    {
                        lastReportTime = now;
                        var progressValue = (double)extractedCount / totalEntries;
                        var extractRunningText = _localizationService["Step_ExtractRunning"];
                        progress?.Report(new DeploymentProgress
                        {
                            StepName = Name,
                            Kind = Kind,
                            ProgressValue = progressValue,
                            Message = $"{extractRunningText} ({extractedCount}/{totalEntries})",
                        });
                    }
                }
            }, ct).ConfigureAwait(false);

            progress?.Report(new DeploymentProgress
            {
                StepName = Name,
                Kind = Kind,
                ProgressValue = 1.0,
                Message = _localizationService["Step_ExtractComplete"],
            });

            return new DeploymentStepResult
            {
                StepName = Name,
                Kind = Kind,
                IsSuccess = true,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ExtractionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ExtractionException(
                $"Failed to extract archive {_archivePath}: {ex.Message}",
                _archivePath);
        }
    }

    private string ResolveDestinationDirectory(string normalizedTarget, string entryKey, string fileName)
    {
        if (_fileRules is null || _fileRules.Count == 0)
        {
            var entryDir = Path.GetDirectoryName(entryKey);
            return string.IsNullOrEmpty(entryDir)
                ? normalizedTarget
                : Path.Combine(normalizedTarget, entryDir);
        }

        foreach (var rule in _fileRules)
        {
            if (MatchesPattern(fileName, rule.Pattern))
            {
                var entryDir = Path.GetDirectoryName(entryKey);
                var basePath = string.IsNullOrEmpty(entryDir)
                    ? normalizedTarget
                    : Path.Combine(normalizedTarget, entryDir);
                return string.IsNullOrEmpty(rule.TargetSubdirectory)
                    ? basePath
                    : Path.Combine(basePath, rule.TargetSubdirectory);
            }
        }

        var defaultEntryDir = Path.GetDirectoryName(entryKey);
        return string.IsNullOrEmpty(defaultEntryDir)
            ? normalizedTarget
            : Path.Combine(normalizedTarget, defaultEntryDir);
    }

    private bool ShouldOverwrite(string fileName)
    {
        if (_fileRules is null) return true;

        foreach (var rule in _fileRules)
        {
            if (MatchesPattern(fileName, rule.Pattern))
            {
                return rule.Overwrite;
            }
        }

        return true;
    }

    private static bool MatchesPattern(string fileName, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(fileName))
            return false;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);

        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
