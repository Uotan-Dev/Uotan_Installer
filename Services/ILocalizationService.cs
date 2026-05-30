namespace UotanInstaller.App.Services;

/// <summary>
/// <para>本地化服务接口，提供多语言支持</para>
/// Localization service interface that provides multi-language support
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// <para>获取指定键的本地化字符串</para>
    /// Get the localized string for the specified key
    /// </summary>
    /// <param name="key">
    /// <para>本地化键</para>
    /// The localization key
    /// </param>
    /// <returns>
    /// <para>本地化后的字符串</para>
    /// The localized string
    /// </returns>
    string GetLocalizedString(string key);

    /// <summary>
    /// <para>使用参数格式化本地化字符串</para>
    /// Format a localized string with parameters
    /// </summary>
    /// <param name="key">
    /// <para>本地化键</para>
    /// The localization key
    /// </param>
    /// <param name="args">
    /// <para>格式化参数</para>
    /// The format arguments
    /// </param>
    /// <returns>
    /// <para>格式化后的本地化字符串</para>
    /// The formatted localized string
    /// </returns>
    string FormatLocalizedString(string key, params object[] args);

    /// <summary>
    /// <para>设置当前语言</para>
    /// Set the current language
    /// </summary>
    /// <param name="language">
    /// <para>语言代码，如 "chs"、"en"</para>
    /// The language code, e.g. "chs", "en"
    /// </param>
    void SetLanguage(string language);

    /// <summary>
    /// <para>获取当前语言代码</para>
    /// Get the current language code
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// <para>语言变更事件</para>
    /// Language changed event
    /// </summary>
    event Action<string>? LanguageChanged;

    /// <summary>
    /// <para>通过索引器获取本地化字符串</para>
    /// Get localized string via indexer
    /// </summary>
    /// <param name="key">
    /// <para>本地化键</para>
    /// The localization key
    /// </param>
    /// <returns>
    /// <para>本地化后的字符串</para>
    /// The localized string
    /// </returns>
    string this[string key] { get; }
}
