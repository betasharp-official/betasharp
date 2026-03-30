using System.ComponentModel;
using BetaSharp.Client.Rendering;
using BetaSharp.NBT;

namespace BetaSharp.Client.Debug.Components;

[DisplayName("Entities")]
[Description("Shows entities stats.")]
public class DebugEntities : DebugComponent
{
    public DebugEntities() { }

    [DisplayName("Show Rendered")]
    [Description("Whether to show the number of rendered entities.")]
    public bool ShowRendered { get; set; } = true;

    [DisplayName("Show Hidden")]
    [Description("Whether to show the number of hidden entities.")]
    public bool ShowHidden { get; set; } = true;

    [DisplayName("Show Not In View")]
    [Description("Whether to show the number of entities not in your view.")]
    public bool ShowNotInView { get; set; } = true;

    public override IEnumerable<DebugRowData> GetRows(DebugContext ctx)
    {
        WorldRenderer render = ctx.Game.terrainRenderer;
        if (ShowRendered) yield return new DebugRowData("Rendered Entities: " + render.countEntitiesRendered + "/" + render.countEntitiesTotal);

        string secondLine = "";
        if (ShowHidden) secondLine += "Hidden: " + render.countEntitiesHidden;
        if (ShowNotInView)
        {
            if (secondLine.Length > 0) secondLine += ", ";
            secondLine += "Not in view: " + (render.countEntitiesTotal - render.countEntitiesHidden - render.countEntitiesRendered);
        }

        yield return new DebugRowData(secondLine);
    }

    public override DebugComponent Duplicate()
    {
        return new DebugEntities()
        {
            Right = Right,

            ShowRendered = ShowRendered,
            ShowHidden = ShowHidden,
            ShowNotInView = ShowNotInView
        };
    }

    public override void writeNBT(NBTTagCompound nbt)
    {
        nbt.SetBoolean("ShowRendered", ShowRendered);
        nbt.SetBoolean("ShowHidden", ShowHidden);
        nbt.SetBoolean("ShowNotInView", ShowNotInView);
    }

    public override void readNBT(NBTTagCompound nbt)
    {
        ShowRendered  = nbt.GetBoolean("ShowRendered");
        ShowHidden = nbt.GetBoolean("ShowHidden");
        ShowNotInView = nbt.GetBoolean("ShowNotInView");
    }
}
