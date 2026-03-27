using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class AudioSettingsScreen : BaseOptionsScreen
{
    public AudioSettingsScreen(UIScreen? parent, GameOptions options)
        : base(parent, options, "options.audioTitle")
    {
        TitleText = "Audio Settings";
    }

    protected override IEnumerable<GameOption> GetOptions() => Options.AudioScreenOptions;
}
