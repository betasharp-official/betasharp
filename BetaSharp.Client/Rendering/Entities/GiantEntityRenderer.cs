using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class GiantEntityRenderer : LivingEntityRenderer
{

    private readonly float scale;

    public GiantEntityRenderer(ModelBase mainModel, float var2, float var3) : base(mainModel, var2 * var3)
    {
        scale = var3;
    }

    protected void preRenderScale(EntityGiantZombie var1, float var2)
    {
        Scene.Scale(scale, scale, scale);
    }

    protected override void PreRenderCallback(EntityLiving var1, float var2)
    {
        preRenderScale((EntityGiantZombie)var1, var2);
    }
}
