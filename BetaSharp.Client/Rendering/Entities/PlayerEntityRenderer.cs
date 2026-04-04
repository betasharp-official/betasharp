using BetaSharp.Blocks;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class PlayerEntityRenderer : LivingEntityRenderer
{

    private readonly ModelBiped _modelBipedMain;
    private readonly ModelBiped _modelArmorChestplate = new(1.0F);
    private readonly ModelBiped _modelArmor = new(0.5F);
    private static readonly string[] s_armorFilenamePrefix = ["cloth", "chain", "iron", "diamond", "gold"];

    public PlayerEntityRenderer() : base(new ModelBiped(0.0F), 0.5F)
    {
        _modelBipedMain = (ModelBiped)mainModel;
    }

    protected bool SetArmorModel(EntityPlayer player, int var2, float var3)
    {
        ItemStack var4 = player.inventory.ArmorItemBySlot(3 - var2);
        if (var4 != null)
        {
            Item var5 = var4.getItem();
            if (var5 is ItemArmor var6)
            {
                loadTexture("/armor/" + s_armorFilenamePrefix[var6.renderIndex] + "_" + (var2 == 2 ? 2 : 1) + ".png");
                ModelBiped var7 = var2 == 2 ? _modelArmor : _modelArmorChestplate;
                var7.bipedHead.visible = var2 == 0;
                var7.bipedHeadwear.visible = var2 == 0;
                var7.bipedBody.visible = var2 == 1 || var2 == 2;
                var7.bipedRightArm.visible = var2 == 1;
                var7.bipedLeftArm.visible = var2 == 1;
                var7.bipedRightLeg.visible = var2 == 2 || var2 == 3;
                var7.bipedLeftLeg.visible = var2 == 2 || var2 == 3;
                setRenderPassModel(var7);
                return true;
            }
        }

        return false;
    }

    public void RenderPlayer(EntityPlayer var1, double var2, double var4, double var6, float var8, float var9)
    {
        ItemStack var10 = var1.inventory.GetItemInHand();
        _modelArmorChestplate.field_1278_i = _modelArmor.field_1278_i = _modelBipedMain.field_1278_i = var10 != null;
        _modelArmorChestplate.isSneak = _modelArmor.isSneak = _modelBipedMain.isSneak = var1.isSneaking();
        double var11 = var4 - var1.standingEyeHeight;
        if (var1.isSneaking() && var1 is not ClientPlayerEntity)
        {
            var11 -= 0.125D;
        }

        base.DoRenderLiving(var1, var2, var11, var6, var8, var9);
        _modelArmorChestplate.isSneak = _modelArmor.isSneak = _modelBipedMain.isSneak = false;
        _modelArmorChestplate.field_1278_i = _modelArmor.field_1278_i = _modelBipedMain.field_1278_i = false;
    }

    protected void RenderName(EntityPlayer var1, double var2, double var4, double var6)
    {
        if (Dispatcher.Options.HideGUI && var1 != Dispatcher.CameraEntity)
        {
            float var8 = 1.6F;
            float var9 = (float)(1.0D / 60.0D) * var8;
            float var10 = var1.getDistance(Dispatcher.CameraEntity);
            float var11 = var1.isSneaking() ? 32.0F : 64.0F;
            if (var10 < var11)
            {
                string var12 = var1.name;
                if (!var1.isSneaking())
                {
                    if (var1.isSleeping())
                    {
                        renderLivingLabel(var1, var12, var2, var4 - 1.5D, var6, 64);
                    }
                    else
                    {
                        renderLivingLabel(var1, var12, var2, var4, var6, 64);
                    }
                }
                else
                {
                    TextRenderer var13 = TextRenderer;
                    GLManager.GL.PushMatrix();
                    GLManager.GL.Translate((float)var2 + 0.0F, (float)var4 + 2.3F, (float)var6);
                    GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
                    GLManager.GL.Rotate(-Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
                    GLManager.GL.Rotate(Dispatcher.PlayerViewX, 1.0F, 0.0F, 0.0F);
                    GLManager.GL.Scale(-var9, -var9, var9);
                    GLManager.GL.Disable(GLEnum.Lighting);
                    GLManager.GL.Translate(0.0F, 0.25F / var9, 0.0F);
                    GLManager.GL.DepthMask(false);
                    GLManager.GL.Enable(GLEnum.Blend);
                    GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                    Tessellator var14 = Tessellator.instance;
                    GLManager.GL.Disable(GLEnum.Texture2D);
                    var14.startDrawingQuads();
                    int var15 = var13.GetStringWidth(var12) / 2;
                    var14.setColorRGBA_F(0.0F, 0.0F, 0.0F, 0.25F);
                    var14.addVertex(-var15 - 1, -1.0D, 0.0D);
                    var14.addVertex(-var15 - 1, 8.0D, 0.0D);
                    var14.addVertex(var15 + 1, 8.0D, 0.0D);
                    var14.addVertex(var15 + 1, -1.0D, 0.0D);
                    var14.draw();
                    GLManager.GL.Enable(GLEnum.Texture2D);
                    GLManager.GL.DepthMask(true);
                    var13.DrawString(var12, -var13.GetStringWidth(var12) / 2, 0, Color.WhiteAlpha20);
                    GLManager.GL.Enable(GLEnum.Lighting);
                    GLManager.GL.Disable(GLEnum.Blend);
                    GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
                    GLManager.GL.PopMatrix();
                }
            }
        }

    }

    protected void RenderSpecials(EntityPlayer var1, float var2)
    {
        ItemStack var3 = var1.inventory.ArmorItemBySlot(3);
        if (var3 != null && var3.getItem().id < 256)
        {
            GLManager.GL.PushMatrix();
            _modelBipedMain.bipedHead.transform(1.0F / 16.0F);
            if (BlockRenderer.IsSideLit(Block.Blocks[var3.itemId].getRenderType()))
            {
                float var4 = 10.0F / 16.0F;
                GLManager.GL.Translate(0.0F, -0.25F, 0.0F);
                GLManager.GL.Rotate(180.0F, 0.0F, 1.0F, 0.0F);
                GLManager.GL.Scale(var4, -var4, var4);
            }

            Dispatcher.HeldItemRenderer.renderItem(var1, var3);
            GLManager.GL.PopMatrix();
        }

        float var5;
        if (var1.name.Equals("deadmau5") && LoadDownloadableImageTexture(var1.name, null))
        {
            for (int var19 = 0; var19 < 2; ++var19)
            {
                var5 = var1.prevYaw + (var1.yaw - var1.prevYaw) * var2 - (var1.lastBodyYaw + (var1.bodyYaw - var1.lastBodyYaw) * var2);
                float var6 = var1.prevPitch + (var1.pitch - var1.prevPitch) * var2;
                GLManager.GL.PushMatrix();
                GLManager.GL.Rotate(var5, 0.0F, 1.0F, 0.0F);
                GLManager.GL.Rotate(var6, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Translate(6.0F / 16.0F * (var19 * 2 - 1), 0.0F, 0.0F);
                GLManager.GL.Translate(0.0F, -(6.0F / 16.0F), 0.0F);
                GLManager.GL.Rotate(-var6, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(-var5, 0.0F, 1.0F, 0.0F);
                float var7 = 4.0F / 3.0F;
                GLManager.GL.Scale(var7, var7, var7);
                _modelBipedMain.renderEars(1.0F / 16.0F);
                GLManager.GL.PopMatrix();
            }
        }

        if (LoadDownloadableImageTexture(var1.playerCloakUrl, null))
        {
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate(0.0F, 0.0F, 2.0F / 16.0F);
            double var20 = var1.prevCapeX + (var1.capeX - var1.prevCapeX) * (double)var2 - (var1.prevX + (var1.x - var1.prevX) * (double)var2);
            double var22 = var1.prevCapeY + (var1.capeY - var1.prevCapeY) * (double)var2 - (var1.prevY + (var1.y - var1.prevY) * (double)var2);
            double var8 = var1.prevCapeZ + (var1.capeZ - var1.prevCapeZ) * (double)var2 - (var1.prevZ + (var1.z - var1.prevZ) * (double)var2);
            float var10 = var1.lastBodyYaw + (var1.bodyYaw - var1.lastBodyYaw) * var2;
            double var11 = (double)MathHelper.Sin(var10 * (float)Math.PI / 180.0F);
            double var13 = (double)-MathHelper.Cos(var10 * (float)Math.PI / 180.0F);
            float var15 = (float)var22 * 10.0F;
            if (var15 < -6.0F)
            {
                var15 = -6.0F;
            }

            if (var15 > 32.0F)
            {
                var15 = 32.0F;
            }

            float var16 = (float)(var20 * var11 + var8 * var13) * 100.0F;
            float var17 = (float)(var20 * var13 - var8 * var11) * 100.0F;
            if (var16 < 0.0F)
            {
                var16 = 0.0F;
            }

            float var18 = var1.prevStepBobbingAmount + (var1.stepBobbingAmount - var1.prevStepBobbingAmount) * var2;
            var15 += MathHelper.Sin((var1.prevHorizontalSpeed + (var1.horizontalSpeed - var1.prevHorizontalSpeed) * var2) * 6.0F) * 32.0F * var18;
            if (var1.isSneaking())
            {
                var15 += 25.0F;
            }

            GLManager.GL.Rotate(6.0F + var16 / 2.0F + var15, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(var17 / 2.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(-var17 / 2.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(180.0F, 0.0F, 1.0F, 0.0F);
            _modelBipedMain.renderCloak(1.0F / 16.0F);
            GLManager.GL.PopMatrix();
        }

        ItemStack var21 = var1.inventory.GetItemInHand();
        if (var21 != null)
        {
            GLManager.GL.PushMatrix();
            _modelBipedMain.bipedRightArm.transform(1.0F / 16.0F);
            GLManager.GL.Translate(-(1.0F / 16.0F), 7.0F / 16.0F, 1.0F / 16.0F);
            if (var1.fishHook != null)
            {
                var21 = new ItemStack(Item.Stick);
            }

            if (var21.itemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[var21.itemId].getRenderType()))
            {
                var5 = 0.5F;
                GLManager.GL.Translate(0.0F, 3.0F / 16.0F, -(5.0F / 16.0F));
                var5 *= 12.0F / 16.0F;
                GLManager.GL.Rotate(20.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
                GLManager.GL.Scale(var5, -var5, var5);
            }
            else if (Item.ITEMS[var21.itemId].isHandheld())
            {
                var5 = 10.0F / 16.0F;
                if (Item.ITEMS[var21.itemId].isHandheldRod())
                {
                    GLManager.GL.Rotate(180.0F, 0.0F, 0.0F, 1.0F);
                    GLManager.GL.Translate(0.0F, -(2.0F / 16.0F), 0.0F);
                }

                GLManager.GL.Translate(0.0F, 3.0F / 16.0F, 0.0F);
                GLManager.GL.Scale(var5, -var5, var5);
                GLManager.GL.Rotate(-100.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            }
            else
            {
                var5 = 6.0F / 16.0F;
                GLManager.GL.Translate(0.25F, 3.0F / 16.0F, -(3.0F / 16.0F));
                GLManager.GL.Scale(var5, var5, var5);
                GLManager.GL.Rotate(60.0F, 0.0F, 0.0F, 1.0F);
                GLManager.GL.Rotate(-90.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(20.0F, 0.0F, 0.0F, 1.0F);
            }

            Dispatcher.HeldItemRenderer.renderItem(var1, var21);
            GLManager.GL.PopMatrix();
        }

    }

    protected void func_186_b(EntityPlayer var1, float var2)
    {
        float var3 = 15.0F / 16.0F;
        GLManager.GL.Scale(var3, var3, var3);
    }

    public void DrawFirstPersonHand()
    {
        _modelBipedMain.onGround = 0.0F;
        _modelBipedMain.setRotationAngles(0.0F, 0.0F, 0.0F, 0.0F, 0.0F, 1.0F / 16.0F);
        _modelBipedMain.bipedRightArm.render(1.0F / 16.0F);
    }

    protected void func_22016_b(EntityPlayer var1, double var2, double var4, double var6)
    {
        if (var1.isAlive() && var1.isSleeping())
        {
            base.Func_22012_b(var1, var2 + var1.sleepOffsetX, var4 + var1.sleepOffsetY, var6 + var1.sleepOffsetZ);
        }
        else
        {
            base.Func_22012_b(var1, var2, var4, var6);
        }

    }

    protected void func_22017_a(EntityPlayer var1, float var2, float var3, float var4)
    {
        if (var1.isAlive() && var1.isSleeping())
        {
            GLManager.GL.Rotate(var1.getSleepingRotation(), 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(getDeathMaxRotation(var1), 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(270.0F, 0.0F, 1.0F, 0.0F);
        }
        else
        {
            base.RotateCorpse(var1, var2, var3, var4);
        }

    }

    protected override void PassSpecialRender(EntityLiving var1, double var2, double var4, double var6)
    {
        RenderName((EntityPlayer)var1, var2, var4, var6);
    }

    protected override void PreRenderCallback(EntityLiving var1, float var2)
    {
        func_186_b((EntityPlayer)var1, var2);
    }

    protected override bool ShouldRenderPass(EntityLiving var1, int var2, float var3)
    {
        return SetArmorModel((EntityPlayer)var1, var2, var3);
    }

    protected override void RenderMore(EntityLiving var1, float var2)
    {
        RenderSpecials((EntityPlayer)var1, var2);
    }

    protected override void RotateCorpse(EntityLiving var1, float var2, float var3, float var4)
    {
        func_22017_a((EntityPlayer)var1, var2, var3, var4);
    }

    protected override void Func_22012_b(EntityLiving var1, double var2, double var4, double var6)
    {
        func_22016_b((EntityPlayer)var1, var2, var4, var6);
    }

    public override void DoRenderLiving(EntityLiving var1, double var2, double var4, double var6, float var8, float var9)
    {
        RenderPlayer((EntityPlayer)var1, var2, var4, var6, var8, var9);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        RenderPlayer((EntityPlayer)target, x, y, z, yaw, tickDelta);
    }
}
