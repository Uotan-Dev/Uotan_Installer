using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Globalization;

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
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return culture switch
        {
            "zh" => "chs",
            "ja" => "ja",
            "en" => "en",
            _ => "chs"
        };
    }

    /// <inheritdoc/>
    public void SetLanguage(string language)
    {
        _currentLanguage = language;
        _strings.Clear();

        try
        {
            var uri = new Uri($"avares://UotanInstaller.App/Resources/i18n/{language}.axaml");
            var resources = AvaloniaXamlLoader.Load(uri) as ResourceDictionary;

            if (resources != null)
            {
                foreach (var kvp in resources)
                {
                    if (kvp.Key is string key && kvp.Value is string value)
                    {
                        _strings[key] = value;
                    }
                }
            }
        }
        catch
        {
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
