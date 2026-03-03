using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Rendering.Items;

public class ItemRenderer : EntityRenderer
{
    private readonly JavaRandom random = new();
    public bool useCustomDisplayColor = true;

    public ItemRenderer()
    {
        ShadowRadius = 0.15F;
        ShadowStrength = 12.0F / 16.0F;
    }

    public void doRenderItem(EntityItem var1, double var2, double var4, double var6, float var8, float var9)
    {
        random.SetSeed(187L);
        ItemStack var10 = var1.stack;
        GLManager.GL.PushMatrix();
        float var11 = MathHelper.Sin((var1.age + var9) / 10.0F + var1.bobPhase) * 0.1F + 0.1F;
        float var12 = ((var1.age + var9) / 20.0F + var1.bobPhase) * (180.0F / (float)Math.PI);
        byte var13 = 1;
        if (var1.stack.count > 1)
        {
            var13 = 2;
        }

        if (var1.stack.count > 5)
        {
            var13 = 3;
        }

        if (var1.stack.count > 20)
        {
            var13 = 4;
        }

        GLManager.GL.Translate((float)var2, (float)var4 + var11, (float)var6);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        float var16;
        float var17;
        float var18;
        if (var10.itemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[var10.itemId].getRenderType()))
        {
            GLManager.GL.Rotate(var12, 0.0F, 1.0F, 0.0F);
            loadTexture("/terrain.png");
            float var28 = 0.25F;
            if (!Block.Blocks[var10.itemId].isFullCube() && var10.itemId != Block.Slab.id
                && Block.Blocks[var10.itemId].getRenderType() != BlockRendererType.PistonBase)
            {
                var28 = 0.5F;
            }

            GLManager.GL.Scale(var28, var28, var28);

            for (int var29 = 0; var29 < var13; ++var29)
            {
                GLManager.GL.PushMatrix();
                if (var29 > 0)
                {
                    var16 = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / var28;
                    var17 = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / var28;
                    var18 = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / var28;
                    GLManager.GL.Translate(var16, var17, var18);
                }

                BlockRenderer.RenderBlockOnInventory(Block.Blocks[var10.itemId], var10.getDamage(), var1.getBrightnessAtEyes(var9), Tessellator.instance);
                GLManager.GL.PopMatrix();
            }
        }
        else
        {
            GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
            int var14 = var10.getTextureId();
            if (var10.itemId < 256)
            {
                loadTexture("/terrain.png");
            }
            else
            {
                loadTexture("/gui/items.png");
            }

            Tessellator var15 = Tessellator.instance;
            var16 = (var14 % 16 * 16 + 0) / 256.0F;
            var17 = (var14 % 16 * 16 + 16) / 256.0F;
            var18 = (var14 / 16 * 16 + 0) / 256.0F;
            float var19 = (var14 / 16 * 16 + 16) / 256.0F;
            float var20 = 1.0F;
            float var21 = 0.5F;
            float var22 = 0.25F;
            int var23;
            float var24;
            float var25;
            float var26;
            if (useCustomDisplayColor)
            {
                var23 = Item.ITEMS[var10.itemId].getColorMultiplier(var10.getDamage());
                var24 = (var23 >> 16 & 255) / 255.0F;
                var25 = (var23 >> 8 & 255) / 255.0F;
                var26 = (var23 & 255) / 255.0F;
                float var27 = var1.getBrightnessAtEyes(var9);
                GLManager.GL.Color4(var24 * var27, var25 * var27, var26 * var27, 1.0F);
            }

            for (var23 = 0; var23 < var13; ++var23)
            {
                GLManager.GL.PushMatrix();
                if (var23 > 0)
                {
                    var24 = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    var25 = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    var26 = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    GLManager.GL.Translate(var24, var25, var26);
                }

                GLManager.GL.Rotate(180.0F - Dispatcher.playerViewY, 0.0F, 1.0F, 0.0F);
                var15.startDrawingQuads();
                var15.setNormal(0.0F, 1.0F, 0.0F);
                var15.addVertexWithUV((double)(0.0F - var21), (double)(0.0F - var22), 0.0D, (double)var16, (double)var19);
                var15.addVertexWithUV((double)(var20 - var21), (double)(0.0F - var22), 0.0D, (double)var17, (double)var19);
                var15.addVertexWithUV((double)(var20 - var21), (double)(1.0F - var22), 0.0D, (double)var17, (double)var18);
                var15.addVertexWithUV((double)(0.0F - var21), (double)(1.0F - var22), 0.0D, (double)var16, (double)var18);
                var15.draw();
                GLManager.GL.PopMatrix();
            }
        }

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }

    public void drawItemIntoGui(TextRenderer var1, TextureManager var2, int var3, int var4, int var5, int var6, int var7)
    {
        float var11;
        if (var3 < 256 && BlockRenderer.IsSideLit(Block.Blocks[var3].getRenderType()))
        {
            var2.BindTexture(var2.GetTextureId("/terrain.png"));
            Block var14 = Block.Blocks[var3];
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate(var6 - 2, var7 + 3, -3.0F);
            GLManager.GL.Scale(10.0F, 10.0F, 10.0F);
            GLManager.GL.Translate(1.0F, 0.5F, 1.0F);
            GLManager.GL.Scale(1.0F, 1.0F, -1.0F);
            GLManager.GL.Rotate(210.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            int var15 = Item.ITEMS[var3].getColorMultiplier(var4);
            var11 = (var15 >> 16 & 255) / 255.0F;
            float var12 = (var15 >> 8 & 255) / 255.0F;
            float var13 = (var15 & 255) / 255.0F;
            if (useCustomDisplayColor)
            {
                GLManager.GL.Color4(var11, var12, var13, 1.0F);
            }

            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            BlockRenderer.RenderBlockOnInventory(var14, var4, 1.0F, Tessellator.instance);
            GLManager.GL.PopMatrix();
        }
        else if (var5 >= 0)
        {
            GLManager.GL.Disable(GLEnum.Lighting);
            if (var3 < 256)
            {
                var2.BindTexture(var2.GetTextureId("/terrain.png"));
            }
            else
            {
                var2.BindTexture(var2.GetTextureId("/gui/items.png"));
            }

            int var8 = Item.ITEMS[var3].getColorMultiplier(var4);
            float var9 = (var8 >> 16 & 255) / 255.0F;
            float var10 = (var8 >> 8 & 255) / 255.0F;
            var11 = (var8 & 255) / 255.0F;
            if (useCustomDisplayColor)
            {
                GLManager.GL.Color4(var9, var10, var11, 1.0F);
            }

            renderTexturedQuad(var6, var7, var5 % 16 * 16, var5 / 16 * 16, 16, 16);
            GLManager.GL.Enable(GLEnum.Lighting);
        }

        GLManager.GL.Enable(GLEnum.CullFace);
    }

    public void renderItemIntoGUI(TextRenderer var1, TextureManager var2, ItemStack var3, int var4, int var5)
    {
        if (var3 != null)
        {
            drawItemIntoGui(var1, var2, var3.itemId, var3.getDamage(), var3.getTextureId(), var4, var5);
        }
    }

    public void renderItemOverlayIntoGUI(TextRenderer var1, TextureManager var2, ItemStack var3, int var4, int var5)
    {
        if (var3 != null)
        {
            if (var3.count > 1)
            {
                string var6 = "" + var3.count;
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                var1.DrawStringWithShadow(var6, var4 + 19 - 2 - var1.GetStringWidth(var6), var5 + 6 + 3, Color.White);
                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.DepthTest);
            }

            if (var3.isDamaged())
            {
                int var11 = (int)MathHelper.Round(13.0D - var3.getDamage2() * 13.0D / var3.getMaxDamage());
                int var7 = (int)MathHelper.Round(255.0D - var3.getDamage2() * 255.0D / var3.getMaxDamage());
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                GLManager.GL.Disable(GLEnum.Texture2D);
                Tessellator var8 = Tessellator.instance;
                int var9 = 255 - var7 << 16 | var7 << 8;
                int var10 = (255 - var7) / 4 << 16 | 16128;
                renderQuad(var8, var4 + 2, var5 + 13, 13, 2, 0);
                renderQuad(var8, var4 + 2, var5 + 13, 12, 1, var10);
                renderQuad(var8, var4 + 2, var5 + 13, var11, 1, var9);
                GLManager.GL.Enable(GLEnum.Texture2D);
                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.DepthTest);
                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            }

        }
    }

    private void renderQuad(Tessellator var1, int var2, int var3, int var4, int var5, int var6)
    {
        var1.startDrawingQuads();
        var1.setColorOpaque_I(var6);
        var1.addVertex(var2 + 0, var3 + 0, 0.0D);
        var1.addVertex(var2 + 0, var3 + var5, 0.0D);
        var1.addVertex(var2 + var4, var3 + var5, 0.0D);
        var1.addVertex(var2 + var4, var3 + 0, 0.0D);
        var1.draw();
    }

    public void renderTexturedQuad(int var1, int var2, int var3, int var4, int var5, int var6)
    {
        float var7 = 0.0F;
        float var8 = 1 / 256f;
        float var9 = 1 / 256f;
        Tessellator var10 = Tessellator.instance;
        var10.startDrawingQuads();
        var10.addVertexWithUV(var1 + 0, var2 + var6, (double)var7, (double)((var3 + 0) * var8), (double)((var4 + var6) * var9));
        var10.addVertexWithUV(var1 + var5, var2 + var6, (double)var7, (double)((var3 + var5) * var8), (double)((var4 + var6) * var9));
        var10.addVertexWithUV(var1 + var5, var2 + 0, (double)var7, (double)((var3 + var5) * var8), (double)((var4 + 0) * var9));
        var10.addVertexWithUV(var1 + 0, var2 + 0, (double)var7, (double)((var3 + 0) * var8), (double)((var4 + 0) * var9));
        var10.draw();
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderItem((EntityItem)target, x, y, z, yaw, tickDelta);
    }
}
