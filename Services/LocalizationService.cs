using System.Globalization;
using System.Xml.Linq;

namespace UotanInstaller.App.Services;

/// <summary>
/// <para>本地化服务实现，提供多语言支持</para>
/// Localization service implementation that provides multi-language support
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, string> _strings = new();
    private string _currentLanguage = "chs";

    /// <inheritdoc/>
    public string CurrentLanguage => _currentLanguage;

    /// <inheritdoc/>
    public event Action<string>? LanguageChanged;

    /// <summary>
    /// <para>初始化LocalizationService实例</para>
    /// Initialize the LocalizationService instance
    /// </summary>
    public LocalizationService()
    {
        SetLanguage(GetSystemLanguage());
    }

    private static string GetSystemLanguage()
    {
        try
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return culture switch
            {
                "zh" => "chs",
                "ja" => "ja",
                "en" => "en",
                _ => "chs"
            };
        }
        catch
        {
            return "chs";
        }
    }

    /// <inheritdoc/>
    public void SetLanguage(string language)
    {
        _currentLanguage = language;
        _strings.Clear();

        try
        {
            var assembly = typeof(LocalizationService).Assembly;
            var resourceName = $"Resources.i18n.{language}.axaml";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                System.Diagnostics.Debug.WriteLine(
                    $"本地化资源未找到: {resourceName}, 可用资源: {available}");
                return;
            }

            var doc = XDocument.Load(stream);
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml";

            foreach (var element in doc.Descendants())
            {
                if (element.Name.LocalName == "String")
                {
                    var keyAttr = element.Attribute(xns + "Key");
                    if (keyAttr != null)
                    {
                        var key = keyAttr.Value;
                        var value = element.Value;
                        if (!string.IsNullOrEmpty(key))
                        {
                            _strings[key] = value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载本地化资源失败: {ex.Message}");
        }

        LanguageChanged?.Invoke(language);
    }

    /// <inheritdoc/>
    public string GetLocalizedString(string key)
    {
        return _strings.TryGetValue(key, out var value) ? value : key;
    }

    /// <inheritdoc/>
    public string FormatLocalizedString(string key, params object[] args)
    {
        var format = GetLocalizedString(key);
        return string.Format(format, args);
    }

    /// <inheritdoc/>
    public string this[string key] => GetLocalizedString(key);
}
