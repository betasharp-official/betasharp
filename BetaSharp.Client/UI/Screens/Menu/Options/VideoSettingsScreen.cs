using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class VideoSettingsScreen(UIContext context, UIScreen? parent) : BaseOptionsScreen(context, parent, "options.videoTitle")
{
    protected override List<OptionSection> GetOptions() => [
        new(TranslationStorage.Instance.TranslateKey("options.video.performance"), [
            Options.RenderDistanceOption,
            Options.FramerateLimitOption,
            Options.VSyncOption,
            Options.MsaaOption,
            Options.MipmapsOption,
            Options.AnisotropicOption,
        ]),

        new(TranslationStorage.Instance.TranslateKey("options.video.display"), [
            Options.ViewBobbingOption,
            Options.EnvironmentAnimationOption,
            Options.ChunkFadeOption,
            Options.AlternateBlocksOption
        ])
    ];
}
