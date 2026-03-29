using System.ComponentModel;
using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Entities")]
[Description("Shows entities stats.")]
public class DebugEntities : DebugComponent
{
    public DebugEntities() { }

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        WorldRenderer render = ctx.Game.terrainRenderer;
        yield return new DebugRowData("Rendered Entities: " + render.countEntitiesRendered + "/" + render.countEntitiesTotal);
        yield return new DebugRowData("Hidden Entities: " + render.countEntitiesHidden + ", Not in view: " + (render.countEntitiesTotal - render.countEntitiesHidden - render.countEntitiesRendered));
    }

    public override DebugComponent Duplicate()
    {
        return new DebugEntities()
        {
            Right = Right
        };
    }
}
