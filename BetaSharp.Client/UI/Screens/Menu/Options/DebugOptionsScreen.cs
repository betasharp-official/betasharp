using BetaSharp.Client.Debug;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Screens.InGame;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class DebugOptionsScreen : BaseOptionsScreen
{
    private readonly DebugComponentsStorage _debugStorage;

    public DebugOptionsScreen(
        UIContext context,
        UIScreen? parent,
        DebugComponentsStorage debugStorage)
        : base(context, parent, "options.debugTitle")
    {
        TitleText = "Debug Options";
        _debugStorage = debugStorage;
    }

    protected override IEnumerable<GameOption> GetOptions() => Options.DebugScreenOptions;

    protected override UIElement CreateContent()
    {
        Panel list = CreateVerticalList();

        foreach (GameOption option in GetOptions())
        {
            UIElement control = CreateControlForOption(option);
            control.Style.MarginTop = 2;
            control.Style.MarginBottom = 2;
            control.Style.Width = 310;
            list.AddChild(control);
        }

        Button editorBtn = CreateButton();
        editorBtn.Text = "Edit Debug Components...";
        editorBtn.Style.MarginTop = 2;
        editorBtn.Style.MarginBottom = 2;
        editorBtn.Style.Width = 310;
        editorBtn.OnClick += (e) =>
        {
            Options.SaveOptions();
            Context.Navigator.Navigate(new DebugEditorScreen(Context, this, _debugStorage));
        };
        list.AddChild(editorBtn);

        return list;
    }
}
