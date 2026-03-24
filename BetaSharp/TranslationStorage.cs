using Microsoft.Extensions.Logging;

namespace BetaSharp;

public class TranslationStorage
{
    private ILogger _logger = Log.Instance.For<TranslationStorage>();
    private static readonly TranslationStorage _instance = new();
    public static TranslationStorage Instance => _instance;
    private readonly Dictionary<string, string> _translateTable = new();

    private TranslationStorage()
    {
        LoadLanguageFile("lang/en_US.lang");
        LoadLanguageFile("lang/stats_US.lang");

        AddTranslation("disconnect.genericReason", "%1$s");
        AddTranslation("key.zoom", "Zoom");
    }

    public void AddTranslation(string key, string translation)
    {
        _translateTable[key] = translation;
    }

    private void LoadLanguageFile(string assetPath)
    {
        try
        {
            var asset = AssetManager.Instance.getAsset(assetPath);
            if (asset == null) return;

            using StringReader reader = new(asset.GetTextContent());
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex != -1)
                {
                    string key = line[..separatorIndex].Trim();
                    string value = line[(separatorIndex + 1)..].Trim();
                    _translateTable[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load language file {assetPath}", ex);
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
            str = str.Replace($"%{i + 1}$s", values[i].ToString() ?? string.Empty);
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
