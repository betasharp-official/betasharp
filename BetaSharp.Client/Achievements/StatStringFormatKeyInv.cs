using BetaSharp.Client.Input;

namespace BetaSharp.Client.Achievements;

public class StatStringFormatKeyInv(BetaSharp game) : AchievementStatFormatter
{
    private readonly BetaSharp _game = game;
    private static readonly TranslationStorage s_localizedName = TranslationStorage.Instance;

    public string formatString(string key)
    {
        return s_localizedName.TranslateKeyFormat(key, Keyboard.getKeyName(_game.options.KeyBindings[6].keyCode));
    }
}
