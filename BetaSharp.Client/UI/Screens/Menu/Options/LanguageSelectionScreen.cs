using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class LanguageSelectionScreen(UIContext context, UIScreen? parent) : BaseOptionsScreen(context, parent, "options.language")
{
    protected override IEnumerable<GameOption> GetOptions() => Options.LanguageOptions;

    protected override UIElement CreateContent()
    {
        Panel root = CreateVerticalList();

        void AddSection(string name, IEnumerable<GameOption> sectionOptions)
        {
            root.AddChild(CreateSectionHeader(name));
            Panel grid = CreateTwoColumnList();
            foreach (GameOption option in sectionOptions)
            {
                UIElement control = CreateControlForOption(option);
                control.Style.Width = 150;
                control.Style.MarginTop = 2;
                control.Style.MarginBottom = 2;
                control.Style.MarginLeft = 4;
                control.Style.MarginRight = 4;
                grid.AddChild(control);
            }
            root.AddChild(grid);
        }

        AddSection("Game restart required!", [
            Options.LanguageOption,
        ]);

        return root;
    }
}
