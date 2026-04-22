using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SheepEntityRenderer : LivingEntityRenderer
{

    public SheepEntityRenderer(ModelBase mainModel, ModelBase var2, float var3) : base(mainModel, var3)
    {
        setRenderPassModel(var2);
    }

    protected bool setWoolColorAndRender(EntitySheep var1, int var2, float var3)
    {
        if (var2 == 0 && !var1.getSheared())
        {
            loadTexture("/mob/sheep_fur.png");
            float var4 = var1.GetBrightnessAtEyes(var3);
            int var5 = var1.getFleeceColor();
            GLManager.GL.Color3(var4 * EntitySheep.fleeceColorTable[var5][0], var4 * EntitySheep.fleeceColorTable[var5][1], var4 * EntitySheep.fleeceColorTable[var5][2]);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected override bool ShouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return setWoolColorAndRender((EntitySheep)var1, var2, var3);
    }
}
