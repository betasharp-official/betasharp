using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class AudioSettingsScreen : BaseOptionsScreen
{
    public AudioSettingsScreen(UIContext context, UIScreen? parent)
        : base(context, parent, "options.audioTitle")
    {
        TitleText = "Audio Settings";
    }

    protected override IEnumerable<GameOption> GetOptions() => Options.AudioScreenOptions;
}
