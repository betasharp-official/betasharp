namespace BetaSharp.Stats;

public static class StatCollector
{
    private static readonly TranslationStorage _translationStorage = TranslationStorage.Instance;

    public static string TranslateToLocal(string key) => _translationStorage.TranslateKey(key);

    public static string TranslateToLocalFormatted(string key, params object[] args) => _translationStorage.TranslateKeyFormat(key, args);
}
