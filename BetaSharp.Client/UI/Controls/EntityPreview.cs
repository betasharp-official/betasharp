using BetaSharp.Client.UI.Rendering;
using BetaSharp.Entities;

namespace BetaSharp.Client.UI.Controls;

public class EntityPreview(Func<UIScreen?> getScreen) : UIElement
{
    public Entity? Entity { get; set; }
    public float Scale { get; set; } = 30.0f;
    public bool LookAtCursor { get; set; } = true;

    public override void Render(UIRenderer renderer)
    {
        if (Entity != null)
        {
            float mouseX = 0;
            float mouseY = 0;

            if (LookAtCursor)
            {
                UIScreen? screen = getScreen();
                if (screen != null)
                {
                    mouseX = screen.MouseX;
                    mouseY = screen.MouseY;
                }
            }

            renderer.DrawEntity(Entity, ComputedWidth / 2, ComputedHeight, Scale, mouseX, mouseY);
        }

        base.Render(renderer);
    }
}
