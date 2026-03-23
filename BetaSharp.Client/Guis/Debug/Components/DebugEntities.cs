using System.ComponentModel;
using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("Entities")]
[Description("Shows entities stats.")]
public class DebugEntities : DebugComponent
{
    public DebugEntities() { }

    public override void Draw(DebugContext ctx)
    {
        WorldRenderer render = ctx.Game.terrainRenderer;
        ctx.String("Rendered Entities: " + render.countEntitiesRendered + "/" + render.countEntitiesTotal);
        ctx.String("Hidden Entities: " + render.countEntitiesHidden + ", Not in view: " + (render.countEntitiesTotal - render.countEntitiesHidden - render.countEntitiesRendered));
    }

    public override DebugComponent Duplicate()
    {
        return new DebugEntities()
        {
            Right = Right
        };
    }
}
