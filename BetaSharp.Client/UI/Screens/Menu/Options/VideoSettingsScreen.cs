using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class VideoSettingsScreen(UIContext context, UIScreen? parent) : BaseOptionsScreen(context, parent, "options.videoTitle")
{
    protected override IEnumerable<GameOption> GetOptions() => Options.VideoScreenOptions;

    protected override UIElement CreateContent()
    {
        TranslationStorage translationStorage = TranslationStorage.Instance;

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

        AddSection(translationStorage.TranslateKey("options.video.performance"), [
            Options.RenderDistanceOption,
            Options.FramerateLimitOption,
            Options.VSyncOption,
            Options.MsaaOption,
            Options.MipmapsOption,
            Options.AnisotropicOption,
        ]);

        AddSection(translationStorage.TranslateKey("options.video.display"), [
            Options.ViewBobbingOption,
            Options.EnvironmentAnimationOption,
            Options.ChunkFadeOption,
            Options.ShowWTHITOption,
            Options.AlternateBlocksOption
        ]);

        return root;
    }
}
