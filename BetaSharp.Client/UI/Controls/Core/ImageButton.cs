using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.UI.Controls.Core;

public class ImageButton : UIElement
{
    public TextureHandle Texture { get; set; }

    public float? U { get; set; }
    public float? V { get; set; }
    public float? UWidth { get; set; }
    public float? VHeight { get; set; } 
    public Action ClickSound;


    public ImageButton(Action clickSound)
    {
        Style.Width = 20;
        Style.Height = 20;

        OnClick += (e) =>
        {
            if (Enabled)
            {
                clickSound();
            }
        };

        OnMouseEnter += (e) =>
        {
            IsHovered = true;
            e.Handled = true;
        };

        OnMouseLeave += (e) =>
        {
            IsHovered = false;
            e.Handled = true;
        };
    }

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        if (Texture != null)
        {
            props.Add($"Texture:  Id={Texture.Id}  {Texture.Texture?.Source ?? "null"}");
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
        int hoverState = !Enabled ? 0 : (IsHovered ? 2 : 1);

        TextureHandle texture = renderer.TextureManager.GetTextureId("/gui/gui.png");

        // Use fixed UV height of 20 to avoid reading into the next button in the spritesheet
        float uvHeight = 20;
        float vStart = 46 + hoverState * 20;

        renderer.DrawTexturedModalRect(texture, 0, 0, 0, vStart, ComputedWidth / 2, ComputedHeight, ComputedWidth / 2, uvHeight);
        renderer.DrawTexturedModalRect(texture, ComputedWidth / 2, 0, 200 - ComputedWidth / 2, vStart, ComputedWidth / 2, ComputedHeight, ComputedWidth / 2, uvHeight);

        if (Texture != null)
        {
            renderer.DrawTexture(Texture, 4, 4, ComputedWidth - 8, ComputedHeight - 8);
        }

        base.Render(renderer);
    }
}
