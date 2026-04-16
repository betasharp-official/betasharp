using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class Image : UIElement
{
    public TextureHandle? Texture { get; set; }

    public float? U { get; set; }
    public float? V { get; set; }
    public float? UWidth { get; set; }
    public float? VHeight { get; set; }

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        if (Texture != null)
        {
            props.Add($"Texture:  Id={Texture.Id}  {Texture.Source}");
            if (U.HasValue && V.HasValue && UWidth.HasValue && VHeight.HasValue)
            {
                props.Add($"UV:       ({U:F1}, {V:F1})  {UWidth:F1}×{VHeight:F1}");
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
            if (U.HasValue && V.HasValue && UWidth.HasValue && VHeight.HasValue)
            {
                renderer.DrawTexturedModalRect(Texture, 0, 0, U.Value, V.Value, UWidth.Value, VHeight.Value);
            }
            else
            {
                renderer.DrawTexture(Texture, 0, 0, ComputedWidth, ComputedHeight);
            }
        }

        base.Render(renderer);
    }
}
