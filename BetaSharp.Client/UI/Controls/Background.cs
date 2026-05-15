using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public enum BackgroundType
{
    Dirt,
    World,
    GameOver
}

public class Background : FullscreenElement
{
    public BackgroundType Type { get; set; } = BackgroundType.Dirt;
    public string TexturePath { get; set; } = "/gui/background.png";
    public float Scale { get; set; } = 32.0f;

    public Background() { }

    public Background(BackgroundType type)
    {
        Type = type;
    }

    public override void Render(UIRenderer renderer)
    {
        if (Type == BackgroundType.World)
        {
            renderer.DrawGradientRect(0, 0, ComputedWidth, ComputedHeight, Guis.Color.WorldBackgroundDark, Guis.Color.WorldBackground);
        }
        else if (Type == BackgroundType.GameOver)
        {
            renderer.DrawGradientRect(0, 0, ComputedWidth, ComputedHeight, Guis.Color.GameOverBackgroundDarkRed, Guis.Color.GameOverBackgroundRed);
        }
        else
        {
            TextureHandle texture = renderer.TextureManager.GetTextureId(TexturePath);
            renderer.DrawRepeatingTexture(texture, 0, 0, ComputedWidth, ComputedHeight, Scale);
        }
        base.Render(renderer);
    }
}
