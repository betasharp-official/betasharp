using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class UISettingsScreen(UIContext context, UIScreen? parent) : BaseOptionsScreen(context, parent, "options.uiSettings")
{
    protected override List<OptionSection> GetOptions() => [ new(Options.UIScreenOptions) ];
}
