using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class DebugOptionsScreen : BaseOptionsScreen
{
    public DebugOptionsScreen(UIScreen? parent, GameOptions options)
        : base(parent, options, "options.debugTitle")
    {
        TitleText = "Debug Options";
    }

    protected override IEnumerable<GameOption> GetOptions() => Options.DebugScreenOptions;
}
