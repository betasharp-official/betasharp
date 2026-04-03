using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class DebugOptionsScreen : BaseOptionsScreen
{
    public DebugOptionsScreen(
        UIContext context,
        UIScreen? parent)
        : base(context, parent, "options.debugTitle")
    {
        TitleText = "Debug Options";
    }

    protected override IEnumerable<GameOption> GetOptions() => Options.DebugScreenOptions;
}
