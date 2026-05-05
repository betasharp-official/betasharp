using BetaSharp.Client.Options;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class UISettingsScreen(UIContext context, UIScreen? parent) : BaseOptionsScreen(context, parent, "UI Settings")
{
    protected override List<OptionSection> GetOptions() => [ new(Options.UIScreenOptions) ];
}
