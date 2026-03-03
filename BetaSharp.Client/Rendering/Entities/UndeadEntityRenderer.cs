using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Client.Rendering.Entities;

public class UndeadEntityRenderer : LivingEntityRenderer
{

    protected ModelBiped modelBipedMain;

    public UndeadEntityRenderer(ModelBiped mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
        modelBipedMain = mainModel;
    }

    protected override void renderMore(EntityLiving var1, float var2)
    {
        ItemStack var3 = var1.getHeldItem();
        if (var3 != null)
        {
            GLManager.GL.PushMatrix();
            modelBipedMain.bipedRightArm.transform(1.0F / 16.0F);
            GLManager.GL.Translate(-(1.0F / 16.0F), 7.0F / 16.0F, 1.0F / 16.0F);
            float var4;
            if (var3.itemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[var3.itemId].getRenderType()))
            {
                var4 = 0.5F;
                GLManager.GL.Translate(0.0F, 3.0F / 16.0F, -(5.0F / 16.0F));
                var4 *= 12.0F / 16.0F;
                GLManager.GL.Rotate(20.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
                GLManager.GL.Scale(var4, -var4, var4);
            }
            else if (Item.ITEMS[var3.itemId].isHandheld())
            {
                var4 = 10.0F / 16.0F;
                GLManager.GL.Translate(0.0F, 3.0F / 16.0F, 0.0F);
                GLManager.GL.Scale(var4, -var4, var4);
                GLManager.GL.Rotate(-100.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            }
            else
            {
                var4 = 6.0F / 16.0F;
                GLManager.GL.Translate(0.25F, 3.0F / 16.0F, -(3.0F / 16.0F));
                GLManager.GL.Scale(var4, var4, var4);
                GLManager.GL.Rotate(60.0F, 0.0F, 0.0F, 1.0F);
                GLManager.GL.Rotate(-90.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(20.0F, 0.0F, 0.0F, 1.0F);
            }

            Dispatcher.heldItemRenderer.renderItem(var1, var3);
            GLManager.GL.PopMatrix();
        }

    }
}
