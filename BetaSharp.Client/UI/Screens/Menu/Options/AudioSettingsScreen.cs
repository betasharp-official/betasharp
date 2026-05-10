using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class AudioSettingsScreen : BaseOptionsScreen
{
    public AudioSettingsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "options.audioTitle")
    {
        TitleText = TranslationStorage.Instance.TranslateKey("options.audioTitle");
    }

    protected override List<OptionSection> GetOptions() => [new(Options.AudioScreenOptions)];
}
