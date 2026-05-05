using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Maps;

namespace BetaSharp.Client.Rendering.Items;

public class HeldItemRenderer
{
    private readonly BetaSharp _game;
    private ItemStack itemToRender;
    private float equippedProgress;
    private float prevEquippedProgress;
    private readonly BlockRenderer blockRenderer = new();
    private readonly MapItemRenderer mapRenderer;

    private int field_20099_f = -1;

    public HeldItemRenderer(BetaSharp game)
    {
        _game = game;
        mapRenderer = new MapItemRenderer(game.TextRenderer, game.Options, game.TextureManager);
    }

    public void renderItem(EntityLiving entity, ItemStack item)
    {
        GLManager.GL.PushMatrix();
        if (item.ItemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[item.ItemId].getRenderType()))
        {
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/terrain.png"));
            BlockRenderer.RenderBlockOnInventory(Block.Blocks[item.ItemId], item.getDamage(), entity.GetBrightnessAtEyes(1.0F), Tessellator.instance);
        }
        else
        {
            string texPath = item.ItemId < 256 ? "/terrain.png" : "/gui/items.png";
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId(texPath));
            int tileSize = _game.TextureManager.GetAtlasTileSize(texPath);

            Tessellator tessellator = Tessellator.instance;
            int iconIndex = entity.GetItemStackTextureId(item);
            float minU = (iconIndex % 16 * 16 + 0.0F) / 256.0F;
            float maxU = (iconIndex % 16 * 16 + 15.99F) / 256.0F;
            float minV = (iconIndex / 16 * 16 + 0.0F) / 256.0F;
            float maxV = (iconIndex / 16 * 16 + 15.99F) / 256.0F;
            float quadWidth = 1.0F;
            float xOffset = 0.0F;
            float yOffset = 0.3F;
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            GLManager.GL.Translate(-xOffset, -yOffset, 0.0F);
            float itemScale = 1.5F;
            GLManager.GL.Scale(itemScale, itemScale, itemScale);
            GLManager.GL.Rotate(50.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(335.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Translate(-(15.0F / 16.0F), -(1.0F / 16.0F), 0.0F);
            float thickness = 1.0F / 16.0F;
            tessellator.startDrawingQuads();
            tessellator.setNormal(0.0F, 0.0F, 1.0F);
            tessellator.addVertexWithUV(0.0D, 0.0D, 0.0D, (double)maxU, (double)maxV);
            tessellator.addVertexWithUV((double)quadWidth, 0.0D, 0.0D, (double)minU, (double)maxV);
            tessellator.addVertexWithUV((double)quadWidth, 1.0D, 0.0D, (double)minU, (double)minV);
            tessellator.addVertexWithUV(0.0D, 1.0D, 0.0D, (double)maxU, (double)minV);
            tessellator.draw();
            tessellator.startDrawingQuads();
            tessellator.setNormal(0.0F, 0.0F, -1.0F);
            tessellator.addVertexWithUV(0.0D, 1.0D, (double)(0.0F - thickness), (double)maxU, (double)minV);
            tessellator.addVertexWithUV((double)quadWidth, 1.0D, (double)(0.0F - thickness), (double)minU, (double)minV);
            tessellator.addVertexWithUV((double)quadWidth, 0.0D, (double)(0.0F - thickness), (double)minU, (double)maxV);
            tessellator.addVertexWithUV(0.0D, 0.0D, (double)(0.0F - thickness), (double)maxU, (double)maxV);
            tessellator.draw();
            tessellator.startDrawingQuads();
            tessellator.setNormal(-1.0F, 0.0F, 0.0F);

            int sliceIndex;
            float sliceProgress;
            float sliceU;
            float sliceX;
            for (sliceIndex = 0; sliceIndex < tileSize; ++sliceIndex)
            {
                sliceProgress = sliceIndex / (float)tileSize;
                sliceU = maxU + (minU - maxU) * sliceProgress - (1.0f / (tileSize * 32.0f));
                sliceX = quadWidth * sliceProgress;
                tessellator.addVertexWithUV((double)sliceX, 0.0D, (double)(0.0F - thickness), (double)sliceU, (double)maxV);
                tessellator.addVertexWithUV((double)sliceX, 0.0D, 0.0D, (double)sliceU, (double)maxV);
                tessellator.addVertexWithUV((double)sliceX, 1.0D, 0.0D, (double)sliceU, (double)minV);
                tessellator.addVertexWithUV((double)sliceX, 1.0D, (double)(0.0F - thickness), (double)sliceU, (double)minV);
            }

            tessellator.draw();
            tessellator.startDrawingQuads();
            tessellator.setNormal(1.0F, 0.0F, 0.0F);

            for (sliceIndex = 0; sliceIndex < tileSize; ++sliceIndex)
            {
                sliceProgress = sliceIndex / (float)tileSize;
                sliceU = maxU + (minU - maxU) * sliceProgress - (1.0f / (tileSize * 32.0f));
                sliceX = quadWidth * sliceProgress + 1.0F / tileSize;
                tessellator.addVertexWithUV((double)sliceX, 1.0D, (double)(0.0F - thickness), (double)sliceU, (double)minV);
                tessellator.addVertexWithUV((double)sliceX, 1.0D, 0.0D, (double)sliceU, (double)minV);
                tessellator.addVertexWithUV((double)sliceX, 0.0D, 0.0D, (double)sliceU, (double)maxV);
                tessellator.addVertexWithUV((double)sliceX, 0.0D, (double)(0.0F - thickness), (double)sliceU, (double)maxV);
            }

            tessellator.draw();
            tessellator.startDrawingQuads();
            tessellator.setNormal(0.0F, 1.0F, 0.0F);

            for (sliceIndex = 0; sliceIndex < tileSize; ++sliceIndex)
            {
                sliceProgress = sliceIndex / (float)tileSize;
                sliceU = maxV + (minV - maxV) * sliceProgress - (1.0f / (tileSize * 32.0f));
                sliceX = quadWidth * sliceProgress + 1.0F / tileSize;
                tessellator.addVertexWithUV(0.0D, (double)sliceX, 0.0D, (double)maxU, (double)sliceU);
                tessellator.addVertexWithUV((double)quadWidth, (double)sliceX, 0.0D, (double)minU, (double)sliceU);
                tessellator.addVertexWithUV((double)quadWidth, (double)sliceX, (double)(0.0F - thickness), (double)minU, (double)sliceU);
                tessellator.addVertexWithUV(0.0D, (double)sliceX, (double)(0.0F - thickness), (double)maxU, (double)sliceU);
            }

            tessellator.draw();
            tessellator.startDrawingQuads();
            tessellator.setNormal(0.0F, -1.0F, 0.0F);

            for (sliceIndex = 0; sliceIndex < tileSize; ++sliceIndex)
            {
                sliceProgress = sliceIndex / (float)tileSize;
                sliceU = maxV + (minV - maxV) * sliceProgress - (1.0f / (tileSize * 32.0f));
                sliceX = quadWidth * sliceProgress;
                tessellator.addVertexWithUV((double)quadWidth, (double)sliceX, 0.0D, (double)minU, (double)sliceU);
                tessellator.addVertexWithUV(0.0D, (double)sliceX, 0.0D, (double)maxU, (double)sliceU);
                tessellator.addVertexWithUV(0.0D, (double)sliceX, (double)(0.0F - thickness), (double)maxU, (double)sliceU);
                tessellator.addVertexWithUV((double)quadWidth, (double)sliceX, (double)(0.0F - thickness), (double)minU, (double)sliceU);
            }

            tessellator.draw();
            GLManager.GL.Disable(GLEnum.RescaleNormal);
        }

        GLManager.GL.PopMatrix();
    }

    public void renderItemInFirstPerson(float tickDelta)
    {
        float equipProgress = prevEquippedProgress + (equippedProgress - prevEquippedProgress) * tickDelta;
        ClientPlayerEntity player = _game.Player;
        float pitch = player.PrevPitch + (player.Pitch - player.PrevPitch) * tickDelta;
        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(pitch, 1.0F, 0.0F, 0.0F);
        GLManager.GL.Rotate(player.PrevYaw + (player.Yaw - player.PrevYaw) * tickDelta, 0.0F, 1.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();
        ItemStack heldStack = itemToRender;
        float brightness = _game.World.GetLuminance(MathHelper.Floor(player.X), MathHelper.Floor(player.Y), MathHelper.Floor(player.Z));
        float red;
        float sineSwing;
        float sqrtSwing;
        if (itemToRender != null)
        {
            int itemColor = Item.ITEMS[itemToRender.ItemId].getColorMultiplier(itemToRender.getDamage());
            red = (itemColor >> 16 & 255) / 255.0F;
            sineSwing = (itemColor >> 8 & 255) / 255.0F;
            sqrtSwing = (itemColor & 255) / 255.0F;
            GLManager.GL.Color4(brightness * red, brightness * sineSwing, brightness * sqrtSwing, 1.0F);
        }
        else
        {
            GLManager.GL.Color4(brightness, brightness, brightness, 1.0F);
        }

        float baseScale;
        if (itemToRender != null && itemToRender.ItemId == Item.Map.id)
        {
            GLManager.GL.PushMatrix();
            baseScale = 0.8F;
            float swingProgress = player.GetSwingProgress(tickDelta);
            sineSwing = MathHelper.Sin(swingProgress * (float)Math.PI);
            sqrtSwing = MathHelper.Sin(MathHelper.Sqrt(swingProgress) * (float)Math.PI);
            GLManager.GL.Translate(-sqrtSwing * 0.4F, MathHelper.Sin(MathHelper.Sqrt(swingProgress) * (float)Math.PI * 2.0F) * 0.2F, -sineSwing * 0.2F);
            swingProgress = 1.0F - pitch / 45.0F + 0.1F;
            if (swingProgress < 0.0F)
            {
                swingProgress = 0.0F;
            }

            if (swingProgress > 1.0F)
            {
                swingProgress = 1.0F;
            }

            swingProgress = -MathHelper.Cos(swingProgress * (float)Math.PI) * 0.5F + 0.5F;
            GLManager.GL.Translate(0.0F, 0.0F * baseScale - (1.0F - equipProgress) * 1.2F - swingProgress * 0.5F + 0.04F, -0.9F * baseScale);
            GLManager.GL.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(swingProgress * -85.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            bindSkinTexture();

            for (int i = 0; i < 2; i++)
            {
                int handSide = i * 2 - 1;
                GLManager.GL.PushMatrix();
                GLManager.GL.Translate(-0.0F, -0.6F, 1.1F * handSide);
                GLManager.GL.Rotate(-45 * handSide, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(-90.0F, 0.0F, 0.0F, 1.0F);
                GLManager.GL.Rotate(59.0F, 0.0F, 0.0F, 1.0F);
                GLManager.GL.Rotate(-65 * handSide, 0.0F, 1.0F, 0.0F);
                EntityRenderer playerRendererBase = EntityRenderDispatcher.Instance.GetEntityRenderObject(_game.Player);
                PlayerEntityRenderer playerRenderer = (PlayerEntityRenderer)playerRendererBase;
                float armScale = 1.0F;
                GLManager.GL.Scale(armScale, armScale, armScale);
                playerRenderer.DrawFirstPersonHand();
                GLManager.GL.PopMatrix();
            }

            sineSwing = player.GetSwingProgress(tickDelta);
            sqrtSwing = MathHelper.Sin(sineSwing * sineSwing * (float)Math.PI);
            float secondarySwing = MathHelper.Sin(MathHelper.Sqrt(sineSwing) * (float)Math.PI);
            GLManager.GL.Rotate(-sqrtSwing * 20.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-secondarySwing * 20.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(-secondarySwing * 80.0F, 1.0F, 0.0F, 0.0F);
            sineSwing = 0.38F;
            GLManager.GL.Scale(sineSwing, sineSwing, sineSwing);
            GLManager.GL.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(180.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Translate(-1.0F, -1.0F, 0.0F);
            sqrtSwing = (1 / 64f);
            GLManager.GL.Scale(sqrtSwing, sqrtSwing, sqrtSwing);
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/misc/mapbg.png"));
            Tessellator tessellator = Tessellator.instance;
            GLManager.GL.Normal3(0.0F, 0.0F, -1.0F);
            tessellator.startDrawingQuads();
            byte mapBorder = 7;
            tessellator.addVertexWithUV(0 - mapBorder, 128 + mapBorder, 0.0D, 0.0D, 1.0D);
            tessellator.addVertexWithUV(128 + mapBorder, 128 + mapBorder, 0.0D, 1.0D, 1.0D);
            tessellator.addVertexWithUV(128 + mapBorder, 0 - mapBorder, 0.0D, 1.0D, 0.0D);
            tessellator.addVertexWithUV(0 - mapBorder, 0 - mapBorder, 0.0D, 0.0D, 0.0D);
            tessellator.draw();
            MapState mapState = ItemMap.getMapState(itemToRender.getDamage(), _game.World);
            mapRenderer.render(_game.Player, _game.TextureManager, mapState);
            GLManager.GL.PopMatrix();
        }
        else if (itemToRender != null)
        {
            GLManager.GL.PushMatrix();
            baseScale = 0.8F;
            red = player.GetSwingProgress(tickDelta);
            sineSwing = MathHelper.Sin(red * (float)Math.PI);
            sqrtSwing = MathHelper.Sin(MathHelper.Sqrt(red) * (float)Math.PI);
            GLManager.GL.Translate(-sqrtSwing * 0.4F, MathHelper.Sin(MathHelper.Sqrt(red) * (float)Math.PI * 2.0F) * 0.2F, -sineSwing * 0.2F);
            GLManager.GL.Translate(0.7F * baseScale, -0.65F * baseScale - (1.0F - equipProgress) * 0.6F, -0.9F * baseScale);
            GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            red = player.GetSwingProgress(tickDelta);
            sineSwing = MathHelper.Sin(red * red * (float)Math.PI);
            sqrtSwing = MathHelper.Sin(MathHelper.Sqrt(red) * (float)Math.PI);
            GLManager.GL.Rotate(-sineSwing * 20.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-sqrtSwing * 20.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(-sqrtSwing * 80.0F, 1.0F, 0.0F, 0.0F);
            red = 0.4F;
            GLManager.GL.Scale(red, red, red);
            if (itemToRender.getItem().isHandheldRod())
            {
                GLManager.GL.Rotate(180.0F, 0.0F, 1.0F, 0.0F);
            }

            renderItem(player, itemToRender);
            GLManager.GL.PopMatrix();
        }
        else
        {
            GLManager.GL.PushMatrix();
            baseScale = 0.8F;
            red = player.GetSwingProgress(tickDelta);
            sineSwing = MathHelper.Sin(red * (float)Math.PI);
            sqrtSwing = MathHelper.Sin(MathHelper.Sqrt(red) * (float)Math.PI);
            GLManager.GL.Translate(-sqrtSwing * 0.3F, MathHelper.Sin(MathHelper.Sqrt(red) * (float)Math.PI * 2.0F) * 0.4F, -sineSwing * 0.4F);
            GLManager.GL.Translate(0.8F * baseScale, -(12.0F / 16.0F) * baseScale - (1.0F - equipProgress) * 0.6F, -0.9F * baseScale);
            GLManager.GL.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            red = player.GetSwingProgress(tickDelta);
            sineSwing = MathHelper.Sin(red * red * (float)Math.PI);
            sqrtSwing = MathHelper.Sin(MathHelper.Sqrt(red) * (float)Math.PI);
            GLManager.GL.Rotate(sqrtSwing * 70.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-sineSwing * 20.0F, 0.0F, 0.0F, 1.0F);
            bindSkinTexture();
            GLManager.GL.Translate(-1.0F, 3.6F, 3.5F);
            GLManager.GL.Rotate(120.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(200.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(-135.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Scale(1.0F, 1.0F, 1.0F);
            GLManager.GL.Translate(5.6F, 0.0F, 0.0F);
            EntityRenderer playerRendererBase2 = EntityRenderDispatcher.Instance.GetEntityRenderObject(_game.Player);
            PlayerEntityRenderer playerRenderer2 = (PlayerEntityRenderer)playerRendererBase2;
            sqrtSwing = 1.0F;
            GLManager.GL.Scale(sqrtSwing, sqrtSwing, sqrtSwing);
            playerRenderer2.DrawFirstPersonHand();
            GLManager.GL.PopMatrix();
        }

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        Lighting.turnOff();
    }

    public void renderOverlays(float tickDelta)
    {
        GLManager.GL.Disable(GLEnum.AlphaTest);
        int blockX;
        if (_game.Player.IsOnFire)
        {
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/terrain.png"));
            renderFireInFirstPerson(tickDelta);
        }

        if (_game.Player.IsInsideWall())
        {
            blockX = MathHelper.Floor(_game.Player.X);
            int blockY = MathHelper.Floor(_game.Player.Y);
            int blockZ = MathHelper.Floor(_game.Player.Z);
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/terrain.png"));
            int blockId = _game.World.Reader.GetBlockId(blockX, blockY, blockZ);
            if (_game.World.Reader.ShouldSuffocate(blockX, blockY, blockZ))
            {
                renderInsideOfBlock(tickDelta, Block.Blocks[blockId].GetTexture(Side.North));
            }
            else
            {
                for (int sampleIndex = 0; sampleIndex < 8; ++sampleIndex)
                {
                    float offsetX = ((sampleIndex >> 0) % 2 - 0.5F) * _game.Player.Width * 0.9F;
                    float offsetY = ((sampleIndex >> 1) % 2 - 0.5F) * _game.Player.Height * 0.2F;
                    float offsetZ = ((sampleIndex >> 2) % 2 - 0.5F) * _game.Player.Width * 0.9F;
                    int sampleX = MathHelper.Floor(blockX + offsetX);
                    int sampleY = MathHelper.Floor(blockY + offsetY);
                    int sampleZ = MathHelper.Floor(blockZ + offsetZ);
                    if (_game.World.Reader.ShouldSuffocate(sampleX, sampleY, sampleZ))
                    {
                        blockId = _game.World.Reader.GetBlockId(sampleX, sampleY, sampleZ);
                    }
                }
            }

            if (Block.Blocks[blockId] != null)
            {
                renderInsideOfBlock(tickDelta, Block.Blocks[blockId].GetTexture(Side.North));
            }
        }

        if (_game.Player.IsInFluid(Material.Water))
        {
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/misc/water.png"));
            renderWarpedTextureOverlay(tickDelta);
        }

        GLManager.GL.Enable(GLEnum.AlphaTest);
    }

    private void renderInsideOfBlock(float tickDelta, int textureId)
    {
        Tessellator tessellator = Tessellator.instance;
        _game.Player.GetBrightnessAtEyes(tickDelta);
        float brightness = 0.1F;
        GLManager.GL.Color4(brightness, brightness, brightness, 0.5F);
        GLManager.GL.PushMatrix();
        float minX = -1.0F;
        float maxX = 1.0F;
        float minY = -1.0F;
        float maxY = 1.0F;
        float z = -0.5F;
        float uvInset = (1 / 128f);
        float minU = textureId % 16 / 256.0F - uvInset;
        float maxU = (textureId % 16 + 15.99F) / 256.0F + uvInset;
        float minV = textureId / 16 / 256.0F - uvInset;
        float maxV = (textureId / 16 + 15.99F) / 256.0F + uvInset;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV((double)minX, (double)minY, (double)z, (double)maxU, (double)maxV);
        tessellator.addVertexWithUV((double)maxX, (double)minY, (double)z, (double)minU, (double)maxV);
        tessellator.addVertexWithUV((double)maxX, (double)maxY, (double)z, (double)minU, (double)minV);
        tessellator.addVertexWithUV((double)minX, (double)maxY, (double)z, (double)maxU, (double)minV);
        tessellator.draw();
        GLManager.GL.PopMatrix();
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
    }

    private void renderWarpedTextureOverlay(float tickDelta)
    {
        Tessellator tessellator = Tessellator.instance;
        float brightness = _game.Player.GetBrightnessAtEyes(tickDelta);
        GLManager.GL.Color4(brightness, brightness, brightness, 0.5F);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.PushMatrix();
        float uvScale = 4.0F;
        float minX = -1.0F;
        float maxX = 1.0F;
        float minY = -1.0F;
        float maxY = 1.0F;
        float z = -0.5F;
        float uOffset = -_game.Player.Yaw / 64.0F;
        float vOffset = _game.Player.Pitch / 64.0F;
        tessellator.startDrawingQuads();
        tessellator.addVertexWithUV((double)minX, (double)minY, (double)z, (double)(uvScale + uOffset), (double)(uvScale + vOffset));
        tessellator.addVertexWithUV((double)maxX, (double)minY, (double)z, (double)(0.0F + uOffset), (double)(uvScale + vOffset));
        tessellator.addVertexWithUV((double)maxX, (double)maxY, (double)z, (double)(0.0F + uOffset), (double)(0.0F + vOffset));
        tessellator.addVertexWithUV((double)minX, (double)maxY, (double)z, (double)(uvScale + uOffset), (double)(0.0F + vOffset));
        tessellator.draw();
        GLManager.GL.PopMatrix();
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Disable(GLEnum.Blend);
    }

    private void renderFireInFirstPerson(float tickDelta)
    {
        Tessellator tessellator = Tessellator.instance;
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 0.9F);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        float quadSize = 1.0F;

        for (int layerIndex = 0; layerIndex < 2; ++layerIndex)
        {
            GLManager.GL.PushMatrix();
            int fireTexture = Block.Fire.TextureId + layerIndex * 16;
            int textureU = (fireTexture & 15) << 4;
            int textureV = fireTexture & 240;
            float minU = textureU / 256.0F;
            float maxU = (textureU + 15.99F) / 256.0F;
            float minV = textureV / 256.0F;
            float maxV = (textureV + 15.99F) / 256.0F;
            float minX = (0.0F - quadSize) / 2.0F;
            float maxX = minX + quadSize;
            float minY = 0.0F - quadSize / 2.0F;
            float maxY = minY + quadSize;
            float z = -0.5F;
            GLManager.GL.Translate(-(layerIndex * 2 - 1) * 0.24F, -0.3F, 0.0F);
            GLManager.GL.Rotate((layerIndex * 2 - 1) * 10.0F, 0.0F, 1.0F, 0.0F);
            tessellator.startDrawingQuads();
            tessellator.addVertexWithUV((double)minX, (double)minY, (double)z, (double)maxU, (double)maxV);
            tessellator.addVertexWithUV((double)maxX, (double)minY, (double)z, (double)minU, (double)maxV);
            tessellator.addVertexWithUV((double)maxX, (double)maxY, (double)z, (double)minU, (double)minV);
            tessellator.addVertexWithUV((double)minX, (double)maxY, (double)z, (double)maxU, (double)minV);
            tessellator.draw();
            GLManager.GL.PopMatrix();
        }

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Disable(GLEnum.Blend);
    }

    public void updateEquippedItem()
    {
        prevEquippedProgress = equippedProgress;
        ClientPlayerEntity player = _game.Player;
        ItemStack heldStack = player.Inventory.ItemInHand;
        bool sameItem = field_20099_f == player.Inventory.SelectedSlot && heldStack == itemToRender;
        if (itemToRender == null && heldStack == null)
        {
            sameItem = true;
        }

        if (heldStack != null && itemToRender != null && heldStack != itemToRender && heldStack.ItemId == itemToRender.ItemId && heldStack.getDamage() == itemToRender.getDamage())
        {
            itemToRender = heldStack;
            sameItem = true;
        }

        float maxStep = 0.4F;
        float targetProgress = sameItem ? 1.0F : 0.0F;
        float progressDelta = targetProgress - equippedProgress;
        if (progressDelta < -maxStep)
        {
            progressDelta = -maxStep;
        }

        if (progressDelta > maxStep)
        {
            progressDelta = maxStep;
        }

        equippedProgress += progressDelta;
        if (equippedProgress < 0.1F)
        {
            itemToRender = heldStack;
            field_20099_f = player.Inventory.SelectedSlot;
        }

    }

    public void ResetEquippedProgress()
    {
        equippedProgress = 0.0F;
    }

    private void bindSkinTexture()
    {
        var skinHandle = EntityRenderDispatcher.Instance.SkinManager?.GetTextureHandle(_game.Player?.Name);
        if (skinHandle != null)
        {
            skinHandle.Bind();
            return;
        }

        _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId(_game.Player.GetTexture()));
    }
}
