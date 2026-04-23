using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;

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

    public void doRenderItem(EntityItem entityItem, double x, double y, double z, float yaw, float tickDelta)
    {
        random.SetSeed(187L);
        ItemStack stack = entityItem.stack;
        GLManager.GL.PushMatrix();
        float bobOffset = MathHelper.Sin((entityItem.Age + tickDelta) / 10.0F + entityItem.bobPhase) * 0.1F + 0.1F;
        float spinAngle = ((entityItem.Age + tickDelta) / 20.0F + entityItem.bobPhase) * (180.0F / (float)Math.PI);
        byte renderCount = 1;
        if (entityItem.stack.Count > 1)
        {
            renderCount = 2;
        }

        if (entityItem.stack.Count > 5)
        {
            renderCount = 3;
        }

        if (entityItem.stack.Count > 20)
        {
            renderCount = 4;
        }

        GLManager.GL.Translate((float)x, (float)y + bobOffset, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        float minU;
        float maxU;
        float minV;
        if (stack.ItemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[stack.ItemId].getRenderType()))
        {
            GLManager.GL.Rotate(spinAngle, 0.0F, 1.0F, 0.0F);
            loadTexture("/terrain.png");
            float blockScale = 0.25F;
            if (!Block.Blocks[stack.ItemId].isFullCube() && stack.ItemId != Block.Slab.id
                && Block.Blocks[stack.ItemId].getRenderType() != BlockRendererType.PistonBase)
            {
                blockScale = 0.5F;
            }

            GLManager.GL.Scale(blockScale, blockScale, blockScale);

            for (int copyIndex = 0; copyIndex < renderCount; ++copyIndex)
            {
                GLManager.GL.PushMatrix();
                if (copyIndex > 0)
                {
                    minU = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / blockScale;
                    maxU = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / blockScale;
                    minV = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / blockScale;
                    GLManager.GL.Translate(minU, maxU, minV);
                }

                BlockRenderer.RenderBlockOnInventory(Block.Blocks[stack.ItemId], stack.getDamage(), entityItem.GetBrightnessAtEyes(tickDelta), Tessellator.instance);
                GLManager.GL.PopMatrix();
            }
        }
        else
        {
            GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
            int iconIndex = stack.getTextureId();
            if (stack.ItemId < 256)
            {
                loadTexture("/terrain.png");
            }
            else
            {
                loadTexture("/gui/items.png");
            }

            Tessellator tessellator = Tessellator.instance;
            minU = (iconIndex % 16 * 16 + 0) / 256.0F;
            maxU = (iconIndex % 16 * 16 + 16) / 256.0F;
            minV = (iconIndex / 16 * 16 + 0) / 256.0F;
            float maxV = (iconIndex / 16 * 16 + 16) / 256.0F;
            float quadWidth = 1.0F;
            float xOffset = 0.5F;
            float yOffset = 0.25F;
            int colorMultiplier;
            float red;
            float green;
            float blue;
            if (useCustomDisplayColor)
            {
                colorMultiplier = Item.ITEMS[stack.ItemId].getColorMultiplier(stack.getDamage());
                red = (colorMultiplier >> 16 & 255) / 255.0F;
                green = (colorMultiplier >> 8 & 255) / 255.0F;
                blue = (colorMultiplier & 255) / 255.0F;
                float brightness = entityItem.GetBrightnessAtEyes(tickDelta);
                GLManager.GL.Color4(red * brightness, green * brightness, blue * brightness, 1.0F);
            }

            for (colorMultiplier = 0; colorMultiplier < renderCount; ++colorMultiplier)
            {
                GLManager.GL.PushMatrix();
                if (colorMultiplier > 0)
                {
                    red = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    green = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    blue = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    GLManager.GL.Translate(red, green, blue);
                }

                GLManager.GL.Rotate(180.0F - Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
                tessellator.startDrawingQuads();
                tessellator.setNormal(0.0F, 1.0F, 0.0F);
                tessellator.addVertexWithUV((double)(0.0F - xOffset), (double)(0.0F - yOffset), 0.0D, (double)minU, (double)maxV);
                tessellator.addVertexWithUV((double)(quadWidth - xOffset), (double)(0.0F - yOffset), 0.0D, (double)maxU, (double)maxV);
                tessellator.addVertexWithUV((double)(quadWidth - xOffset), (double)(1.0F - yOffset), 0.0D, (double)maxU, (double)minV);
                tessellator.addVertexWithUV((double)(0.0F - xOffset), (double)(1.0F - yOffset), 0.0D, (double)minU, (double)minV);
                tessellator.draw();
                GLManager.GL.PopMatrix();
            }
        }

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }

    public void drawItemIntoGui(TextRenderer fontRenderer, TextureManager textureManager, int itemId, int itemDamage, int iconIndex, int x, int y)
    {
        float blue;
        if (itemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[itemId].getRenderType()))
        {
            textureManager.BindTexture(textureManager.GetTextureId("/terrain.png"));
            Block block = Block.Blocks[itemId];
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate(x - 2, y + 3, -3.0F);
            GLManager.GL.Scale(10.0F, 10.0F, 10.0F);
            GLManager.GL.Translate(1.0F, 0.5F, 1.0F);
            GLManager.GL.Scale(1.0F, 1.0F, -1.0F);
            GLManager.GL.Rotate(210.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            int itemColor = Item.ITEMS[itemId].getColorMultiplier(itemDamage);
            blue = (itemColor >> 16 & 255) / 255.0F;
            float greenChannel = (itemColor >> 8 & 255) / 255.0F;
            float blueChannel = (itemColor & 255) / 255.0F;
            if (useCustomDisplayColor)
            {
                GLManager.GL.Color4(blue, greenChannel, blueChannel, 1.0F);
            }

            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            BlockRenderer.RenderBlockOnInventory(block, itemDamage, 1.0F, Tessellator.instance);
            GLManager.GL.PopMatrix();
        }
        else if (iconIndex >= 0)
        {
            GLManager.GL.Disable(GLEnum.Lighting);
            if (itemId < 256)
            {
                textureManager.BindTexture(textureManager.GetTextureId("/terrain.png"));
            }
            else
            {
                textureManager.BindTexture(textureManager.GetTextureId("/gui/items.png"));
            }

            int colorMultiplier = Item.ITEMS[itemId].getColorMultiplier(itemDamage);
            float red = (colorMultiplier >> 16 & 255) / 255.0F;
            float green = (colorMultiplier >> 8 & 255) / 255.0F;
            blue = (colorMultiplier & 255) / 255.0F;
            if (useCustomDisplayColor)
            {
                GLManager.GL.Color4(red, green, blue, 1.0F);
            }

            renderTexturedQuad(x, y, iconIndex % 16 * 16, iconIndex / 16 * 16, 16, 16);
        }
    }

    public void renderItemIntoGUI(TextRenderer fontRenderer, TextureManager textureManager, ItemStack stack, int x, int y)
    {
        if (stack != null)
        {
            drawItemIntoGui(fontRenderer, textureManager, stack.ItemId, stack.getDamage(), stack.getTextureId(), x, y);
        }
    }

    public void renderItemOverlayIntoGUI(TextRenderer fontRenderer, TextureManager textureManager, ItemStack stack, int x, int y)
    {
        if (stack != null)
        {
            if (stack.Count > 1)
            {
                string stackText = "" + stack.Count;
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                fontRenderer.DrawStringWithShadow(stackText, x + 19 - 2 - fontRenderer.GetStringWidth(stackText), y + 6 + 3, Color.White);
            }

            if (stack.isDamaged())
            {
                int barWidth = (int)MathHelper.Round(13.0D - stack.getDamage2() * 13.0D / stack.getMaxDamage());
                int damageColor = (int)MathHelper.Round(255.0D - stack.getDamage2() * 255.0D / stack.getMaxDamage());
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                GLManager.GL.Disable(GLEnum.Texture2D);
                Tessellator tessellator = Tessellator.instance;
                int barColor = 255 - damageColor << 16 | damageColor << 8;
                int backgroundColor = (255 - damageColor) / 4 << 16 | 16128;
                renderQuad(tessellator, x + 2, y + 13, 13, 2, 0);
                renderQuad(tessellator, x + 2, y + 13, 12, 1, backgroundColor);
                renderQuad(tessellator, x + 2, y + 13, barWidth, 1, barColor);
                GLManager.GL.Enable(GLEnum.Texture2D);
                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            }

        }
    }

    private void renderQuad(Tessellator tessellator, int x, int y, int width, int height, int color)
    {
        tessellator.startDrawingQuads();
        tessellator.setColorOpaque_I(color);
        tessellator.addVertex(x + 0, y + 0, 0.0D);
        tessellator.addVertex(x + 0, y + height, 0.0D);
        tessellator.addVertex(x + width, y + height, 0.0D);
        tessellator.addVertex(x + width, y + 0, 0.0D);
        tessellator.draw();
    }

    public void renderTexturedQuad(int x, int y, int u, int v, int width, int height)
    {
        float z = 0.0F;
        float uScale = 1 / 256f;
        float vScale = 1 / 256f;
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV(x + 0, y + height, (double)z, (double)((u + 0) * uScale), (double)((v + height) * vScale));
        tessellator.addVertexWithUV(x + width, y + height, (double)z, (double)((u + width) * uScale), (double)((v + height) * vScale));
        tessellator.addVertexWithUV(x + width, y + 0, (double)z, (double)((u + width) * uScale), (double)((v + 0) * vScale));
        tessellator.addVertexWithUV(x + 0, y + 0, (double)z, (double)((u + 0) * uScale), (double)((v + 0) * vScale));
        tessellator.draw();
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderItem((EntityItem)target, x, y, z, yaw, tickDelta);
    }
}
