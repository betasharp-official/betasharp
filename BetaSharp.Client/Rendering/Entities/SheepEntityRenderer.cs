using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SheepEntityRenderer : LivingEntityRenderer
{

    public SheepEntityRenderer(ModelBase mainModel, ModelBase furModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
        setRenderPassModel(furModel);
    }

    protected bool setWoolColorAndRender(EntitySheep sheepEntity, int renderPass, float tickDelta)
    {
        if (renderPass == 0 && !sheepEntity.IsSheared)
        {
            loadTexture("/mob/sheep_fur.png");
            float brightness = sheepEntity.GetBrightnessAtEyes(tickDelta);
            int fleeceColor = sheepEntity.FleeceColor;
            GLManager.GL.Color3(brightness * EntitySheep.FleeceColorTable[fleeceColor][0], brightness * EntitySheep.FleeceColorTable[fleeceColor][1], brightness * EntitySheep.FleeceColorTable[fleeceColor][2]);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected override bool ShouldRenderPass(EntityLiving entity, int renderPass, float tickDelta)
    {
        return setWoolColorAndRender((EntitySheep)entity, renderPass, tickDelta);
    }
}
