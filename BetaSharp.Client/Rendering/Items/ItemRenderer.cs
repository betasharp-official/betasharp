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

    public void doRenderItem(EntityItem itemEntity, double x, double y, double z, float yaw, float tickDelta)
    {
        random.SetSeed(187L);
        ItemStack itemStack = itemEntity.stack;
        GLManager.GL.PushMatrix();
        float bobOffset = MathHelper.Sin((itemEntity.age + tickDelta) / 10.0F + itemEntity.bobPhase) * 0.1F + 0.1F;
        float spinDegrees = ((itemEntity.age + tickDelta) / 20.0F + itemEntity.bobPhase) * (180.0F / (float)Math.PI);
        byte renderCopies = 1;
        if (itemEntity.stack.Count > 1)
        {
            renderCopies = 2;
        }

        if (itemEntity.stack.Count > 5)
        {
            renderCopies = 3;
        }

        if (itemEntity.stack.Count > 20)
        {
            renderCopies = 4;
        }

        GLManager.GL.Translate((float)x, (float)y + bobOffset, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        float randomOffsetX;
        float randomOffsetY;
        float randomOffsetZ;
        if (itemStack.ItemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[itemStack.ItemId].getRenderType()))
        {
            GLManager.GL.Rotate(spinDegrees, 0.0F, 1.0F, 0.0F);
            loadTexture("/terrain.png");
            float blockScale = 0.25F;
            if (!Block.Blocks[itemStack.ItemId].isFullCube() && itemStack.ItemId != Block.Slab.id
                && Block.Blocks[itemStack.ItemId].getRenderType() != BlockRendererType.PistonBase)
            {
                blockScale = 0.5F;
            }

            GLManager.GL.Scale(blockScale, blockScale, blockScale);

            for (int copyIndex = 0; copyIndex < renderCopies; ++copyIndex)
            {
                GLManager.GL.PushMatrix();
                if (copyIndex > 0)
                {
                    randomOffsetX = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / blockScale;
                    randomOffsetY = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / blockScale;
                    randomOffsetZ = (random.NextFloat() * 2.0F - 1.0F) * 0.2F / blockScale;
                    GLManager.GL.Translate(randomOffsetX, randomOffsetY, randomOffsetZ);
                }

                BlockRenderer.RenderBlockOnInventory(
                    Block.Blocks[itemStack.ItemId],
                    itemStack.getDamage(),
                    itemEntity.getBrightnessAtEyes(tickDelta),
                    Tessellator.instance);
                GLManager.GL.PopMatrix();
            }
        }
        else
        {
            GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
            int textureIndex = itemStack.getTextureId();
            if (itemStack.ItemId < 256)
            {
                loadTexture("/terrain.png");
            }
            else
            {
                loadTexture("/gui/items.png");
            }

            Tessellator tessellator = Tessellator.instance;
            float minU = (textureIndex % 16 * 16 + 0) / 256.0F;
            float maxU = (textureIndex % 16 * 16 + 16) / 256.0F;
            float minV = (textureIndex / 16 * 16 + 0) / 256.0F;
            float maxV = (textureIndex / 16 * 16 + 16) / 256.0F;
            float quadWidth = 1.0F;
            float quadHalfWidth = 0.5F;
            float quadHalfHeight = 0.25F;
            int colorMultiplier;
            float colorRed;
            float colorGreen;
            float colorBlue;
            if (useCustomDisplayColor)
            {
                colorMultiplier = Item.ITEMS[itemStack.ItemId].getColorMultiplier(itemStack.getDamage());
                colorRed = (colorMultiplier >> 16 & 255) / 255.0F;
                colorGreen = (colorMultiplier >> 8 & 255) / 255.0F;
                colorBlue = (colorMultiplier & 255) / 255.0F;
                float brightness = itemEntity.getBrightnessAtEyes(tickDelta);
                GLManager.GL.Color4(colorRed * brightness, colorGreen * brightness, colorBlue * brightness, 1.0F);
            }

            for (int copyIndex = 0; copyIndex < renderCopies; ++copyIndex)
            {
                GLManager.GL.PushMatrix();
                if (copyIndex > 0)
                {
                    randomOffsetX = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    randomOffsetY = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    randomOffsetZ = (random.NextFloat() * 2.0F - 1.0F) * 0.3F;
                    GLManager.GL.Translate(randomOffsetX, randomOffsetY, randomOffsetZ);
                }

                GLManager.GL.Rotate(180.0F - Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
                tessellator.startDrawingQuads();
                tessellator.setNormal(0.0F, 1.0F, 0.0F);
                tessellator.addVertexWithUV((double)(0.0F - quadHalfWidth), (double)(0.0F - quadHalfHeight), 0.0D, (double)minU, (double)maxV);
                tessellator.addVertexWithUV((double)(quadWidth - quadHalfWidth), (double)(0.0F - quadHalfHeight), 0.0D, (double)maxU, (double)maxV);
                tessellator.addVertexWithUV((double)(quadWidth - quadHalfWidth), (double)(1.0F - quadHalfHeight), 0.0D, (double)maxU, (double)minV);
                tessellator.addVertexWithUV((double)(0.0F - quadHalfWidth), (double)(1.0F - quadHalfHeight), 0.0D, (double)minU, (double)minV);
                tessellator.draw();
                GLManager.GL.PopMatrix();
            }
        }

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }

    public void drawItemIntoGui(
        ITextRenderer textRenderer,
        ITextureManager textureManager,
        int itemId,
        int itemDamage,
        int textureIndex,
        int x,
        int y)
    {
        float colorBlue;
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
            int colorMultiplier = Item.ITEMS[itemId].getColorMultiplier(itemDamage);
            float colorRed = (colorMultiplier >> 16 & 255) / 255.0F;
            float colorGreen = (colorMultiplier >> 8 & 255) / 255.0F;
            colorBlue = (colorMultiplier & 255) / 255.0F;
            if (useCustomDisplayColor)
            {
                GLManager.GL.Color4(colorRed, colorGreen, colorBlue, 1.0F);
            }

            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            BlockRenderer.RenderBlockOnInventory(block, itemDamage, 1.0F, Tessellator.instance);
            GLManager.GL.PopMatrix();
        }
        else if (textureIndex >= 0)
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
            float colorRed = (colorMultiplier >> 16 & 255) / 255.0F;
            float colorGreen = (colorMultiplier >> 8 & 255) / 255.0F;
            colorBlue = (colorMultiplier & 255) / 255.0F;
            if (useCustomDisplayColor)
            {
                GLManager.GL.Color4(colorRed, colorGreen, colorBlue, 1.0F);
            }

            renderTexturedQuad(x, y, textureIndex % 16 * 16, textureIndex / 16 * 16, 16, 16);
        }
    }

    public void renderItemIntoGUI(ITextRenderer textRenderer, ITextureManager textureManager, ItemStack itemStack, int x, int y)
    {
        if (itemStack != null)
        {
            drawItemIntoGui(textRenderer, textureManager, itemStack.ItemId, itemStack.getDamage(), itemStack.getTextureId(), x, y);
        }
    }

    public void renderItemOverlayIntoGUI(ITextRenderer textRenderer, ITextureManager textureManager, ItemStack itemStack, int x, int y)
    {
        if (itemStack != null)
        {
            if (itemStack.Count > 1)
            {
                string stackCountText = itemStack.Count.ToString();
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                textRenderer.DrawStringWithShadow(
                    stackCountText,
                    x + 19 - 2 - textRenderer.GetStringWidth(stackCountText),
                    y + 6 + 3,
                    Color.White);
            }

            if (itemStack.isDamaged())
            {
                int durabilityBarWidth = (int)MathHelper.Round(13.0D - itemStack.getDamage2() * 13.0D / itemStack.getMaxDamage());
                int durabilityGreen = (int)MathHelper.Round(255.0D - itemStack.getDamage2() * 255.0D / itemStack.getMaxDamage());
                GLManager.GL.Disable(GLEnum.Lighting);
                GLManager.GL.Disable(GLEnum.DepthTest);
                GLManager.GL.Disable(GLEnum.Texture2D);
                Tessellator tessellator = Tessellator.instance;
                int durabilityBarColor = 255 - durabilityGreen << 16 | durabilityGreen << 8;
                int durabilityBarBackgroundColor = (255 - durabilityGreen) / 4 << 16 | 16128;
                renderQuad(tessellator, x + 2, y + 13, 13, 2, 0);
                renderQuad(tessellator, x + 2, y + 13, 12, 1, durabilityBarBackgroundColor);
                renderQuad(tessellator, x + 2, y + 13, durabilityBarWidth, 1, durabilityBarColor);
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
        float depth = 0.0F;
        float uScale = 1 / 256f;
        float vScale = 1 / 256f;
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV(x + 0, y + height, (double)depth, (double)((u + 0) * uScale), (double)((v + height) * vScale));
        tessellator.addVertexWithUV(x + width, y + height, (double)depth, (double)((u + width) * uScale), (double)((v + height) * vScale));
        tessellator.addVertexWithUV(x + width, y + 0, (double)depth, (double)((u + width) * uScale), (double)((v + 0) * vScale));
        tessellator.addVertexWithUV(x + 0, y + 0, (double)depth, (double)((u + 0) * uScale), (double)((v + 0) * vScale));
        tessellator.draw();
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderItem((EntityItem)target, x, y, z, yaw, tickDelta);
    }
}
