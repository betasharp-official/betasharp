using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public enum BackgroundType
{
    Dirt,
    World,
    GameOver,
    Panorama
}

public class Background : FullscreenElement
{
    public BackgroundType Type { get; set; } = BackgroundType.Dirt;

    public string TexturePath { get; set; } = "/gui/background.png";
    public float Scale { get; set; } = 32.0f;
    public float PanoramaScale { get; set; } = 2.5f;

    public string[] PanoramaFaces { get; set; } =
    {
        "/gui/panorama/panorama_0.png",
        "/gui/panorama/panorama_1.png",
        "/gui/panorama/panorama_2.png",
        "/gui/panorama/panorama_3.png",
        "/gui/panorama/panorama_4.png",
        "/gui/panorama/panorama_5.png"
    };

    public float PanoramaRotation { get; set; } = 0f;

    public Background() { }

    public Background(BackgroundType type)
    {
        Type = type;
    }

    public override void Render(UIRenderer renderer)
    {
        if (Type == BackgroundType.Panorama)
        {
            PanoramaRotation += 0.00005f;

            renderer.DrawPanorama(
                PanoramaFaces
                    .Select(x => renderer.TextureManager.GetTextureId(x))
                    .ToArray(),
                PanoramaRotation,
                ComputedWidth,
                ComputedHeight,
                PanoramaScale
            );

            renderer.DrawGradientRect(
                0,
                0,
                ComputedWidth,
                ComputedHeight,
                new Color(0, 0, 0, 120),
                new Color(0, 0, 0, 180)
            );
        }
        else if (Type == BackgroundType.World)
        {
            renderer.DrawGradientRect(
                0,
                0,
                ComputedWidth,
                ComputedHeight,
                Color.WorldBackgroundDark,
                Color.WorldBackground
            );
        }
        else if (Type == BackgroundType.GameOver)
        {
            renderer.DrawGradientRect(
                0,
                0,
                ComputedWidth,
                ComputedHeight,
                Color.GameOverBackgroundDarkRed,
                Color.GameOverBackgroundRed
            );
        }
        else
        {
            TextureHandle texture = renderer.TextureManager.GetTextureId(TexturePath);

            renderer.DrawRepeatingTexture(
                texture,
                0,
                0,
                ComputedWidth,
                ComputedHeight,
                Scale
            );
        }

        base.Render(renderer);
    }
}
