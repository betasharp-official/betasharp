using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Entities;

namespace BetaSharp.Client.UI.Controls.HUD;

/// <summary>
/// Display the current coordinates for a entity.
/// </summary>
/// <param name="getEntity">Function to get the entity to show</param>
/// <param name="showCoordinates">Function to see if it should be rendered</param>
public class CoordinatesDisplay(Func<Entity?> getEntity, Func<bool> showCoordinates) : UIElement
{
    public override void Render(UIRenderer renderer)
    {
        if (!showCoordinates()) return;

        Entity? entity = getEntity();
        if (entity == null) return;

        int x = (int)Math.Floor(entity.X);
        int y = (int)Math.Floor(entity.Y);
        int z = (int)Math.Floor(entity.Z);

        renderer.DrawText($"Position: {x}, {y}, {z}", 0, 0, Color.White, shadow: true);
    }
}
