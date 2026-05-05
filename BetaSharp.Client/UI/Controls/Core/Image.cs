using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;
using SixLabors.ImageSharp;

namespace BetaSharp.Client.UI.Controls.Core;

/// <summary>
/// UIElement that displays the image in a TextureHandle
/// </summary>
public class Image : UIElement
{
    public TextureHandle? Texture { get; set; }

    /// <summary>
    /// If not null, describes the rectangle in UV space of where
    /// in the TextureHandle to render the image.
    /// </summary>
    public RectangleF? UV { get; set; }

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        if (Texture != null)
        {
            props.Add($"Texture:  Id={Texture.Id}  {Texture.Texture?.Source ?? "null"}");
            if (UV is RectangleF uv)
            {
                props.Add($"UV:       ({uv.X:F1}, {uv.Y:F1})  {uv.Width:F1}×{uv.Height:F1}");
            }
        }
        else
        {
            props.Add("Texture:  null");
        }
        return props;
    }

    public override void Render(UIRenderer renderer)
    {
        if (Texture != null)
        {
            if (UV is RectangleF uv)
            {
                renderer.DrawTexturedModalRect(Texture, 0, 0, uv.X, uv.Y, uv.Width, uv.Height);
            }
            else
            {
                renderer.DrawTexture(Texture, 0, 0, ComputedWidth, ComputedHeight);
            }
        }

        base.Render(renderer);
    }
}
