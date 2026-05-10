using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

public class TranslationStorage
{
    private readonly ILogger _logger = Log.Instance.For<TranslationStorage>();
    private static TranslationStorage _instance = new("en_us");
    public static TranslationStorage Instance => _instance;

    private readonly Dictionary<string, string> _translateTable = new();

    private TranslationStorage(string lang)
    {
        LoadLanguageFile(lang);
    }

    public void AddTranslation(string key, string translation)
    {
        _translateTable[key] = translation;
    }

    public void SwitchLanguage(string lang)
    {
        _instance = new TranslationStorage(lang);
    }

    private void LoadLanguageFile(string assetPath)
    {
        try
        {
            var asset = AssetManager.Instance.getAsset($"lang/{assetPath}.json");
            if (asset == null)
                return;

            using JsonDocument doc = JsonDocument.Parse(asset.GetTextContent());
            FlattenJson(doc.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load language file {LanguageFile}", assetPath);
        }
    }

    private void FlattenJson(JsonElement element, string prefix = "")
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    string key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    FlattenJson(property.Value, key);
                }
                break;

            case JsonValueKind.String:
                _translateTable[prefix] = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                _translateTable[prefix] = element.ToString();
                break;

            default:
                break;
        }
    }

    public string TranslateKey(string key)
    {
        return _translateTable.TryGetValue(key, out string value) ? value : key;
    }

    public string TranslateKeyFormat(string key, params object[] values)
    {
        string str = _translateTable.TryGetValue(key, out string value) ? value : key;

        for (int i = 0; i < values.Length; i++)
        {
            str = str.Replace($"%{i + 1}$s", values[i]?.ToString() ?? string.Empty);
        }

        if (str == "%s")
            str = key + " (Failed to translate key!)";

        return str;
    }

    public string TranslateNamedKey(string key)
    {
        return _translateTable.TryGetValue($"{key}.name", out string value) ? value : "";
    }
}
