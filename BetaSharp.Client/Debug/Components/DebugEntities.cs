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
        WorldRenderer render = ctx.Game.WorldRenderer;
        yield return new DebugRowData("Rendered Entities: " + render.CountEntitiesRendered + "/" + render.CountEntitiesTotal);
        yield return new DebugRowData("Hidden Entities: " + render.CountEntitiesHidden + ", Not in view: " + (render.CountEntitiesTotal - render.CountEntitiesHidden - render.CountEntitiesRendered));
    }

    public override DebugComponent Duplicate()
    {
        return new DebugEntities()
        {
            Right = Right
        };
    }
}
