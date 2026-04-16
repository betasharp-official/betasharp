using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Maps;

namespace BetaSharp.Client.Rendering.Items;

public class HeldItemRenderer : IHeldItemRenderer
{
    private readonly BetaSharp _game;
    private readonly ILegacyFixedFunctionApi _sceneRenderBackend;
    private ItemStack itemToRender;
    private float equippedProgress;
    private float prevEquippedProgress;
    private readonly BlockRenderer blockRenderer = new();
    private readonly MapItemRenderer mapRenderer;

    private int field_20099_f = -1;

    public HeldItemRenderer(BetaSharp game)
    {
        _game = game;
        _sceneRenderBackend = game.LegacyFixedFunctionApi;
        mapRenderer = new MapItemRenderer(game.TextRenderer, game.Options, game.TextureManager);
    }

    public void renderItem(EntityLiving entity, ItemStack item)
    {
        _sceneRenderBackend.PushMatrix();
        if (item.ItemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[item.ItemId].getRenderType()))
        {
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/terrain.png"));
            BlockRenderer.RenderBlockOnInventory(Block.Blocks[item.ItemId], item.getDamage(),
                entity.getBrightnessAtEyes(1.0F), Tessellator.instance);
        }
        else
        {
            string texPath = item.ItemId < 256 ? "/terrain.png" : "/gui/items.png";
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId(texPath));
            int tileSize = _game.TextureManager.GetAtlasTileSize(texPath);

            Tessellator var3 = Tessellator.instance;
            int var4 = entity.getItemStackTextureId(item);
            float var5 = (var4 % 16 * 16 + 0.0F) / 256.0F;
            float var6 = (var4 % 16 * 16 + 15.99F) / 256.0F;
            float var7 = (var4 / 16 * 16 + 0.0F) / 256.0F;
            float var8 = (var4 / 16 * 16 + 15.99F) / 256.0F;
            float var9 = 1.0F;
            float var10 = 0.0F;
            float var11 = 0.3F;
            _sceneRenderBackend.Enable(SceneRenderCapability.RescaleNormal);
            _sceneRenderBackend.Translate(-var10, -var11, 0.0F);
            float var12 = 1.5F;
            _sceneRenderBackend.Scale(var12, var12, var12);
            _sceneRenderBackend.Rotate(50.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Rotate(335.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Translate(-(15.0F / 16.0F), -(1.0F / 16.0F), 0.0F);
            float var13 = 1.0F / 16.0F;
            var3.startDrawingQuads();
            var3.setNormal(0.0F, 0.0F, 1.0F);
            var3.addVertexWithUV(0.0D, 0.0D, 0.0D, (double)var6, (double)var8);
            var3.addVertexWithUV((double)var9, 0.0D, 0.0D, (double)var5, (double)var8);
            var3.addVertexWithUV((double)var9, 1.0D, 0.0D, (double)var5, (double)var7);
            var3.addVertexWithUV(0.0D, 1.0D, 0.0D, (double)var6, (double)var7);
            var3.draw();
            var3.startDrawingQuads();
            var3.setNormal(0.0F, 0.0F, -1.0F);
            var3.addVertexWithUV(0.0D, 1.0D, (double)(0.0F - var13), (double)var6, (double)var7);
            var3.addVertexWithUV((double)var9, 1.0D, (double)(0.0F - var13), (double)var5, (double)var7);
            var3.addVertexWithUV((double)var9, 0.0D, (double)(0.0F - var13), (double)var5, (double)var8);
            var3.addVertexWithUV(0.0D, 0.0D, (double)(0.0F - var13), (double)var6, (double)var8);
            var3.draw();
            var3.startDrawingQuads();
            var3.setNormal(-1.0F, 0.0F, 0.0F);

            int var14;
            float var15;
            float var16;
            float var17;
            for (var14 = 0; var14 < tileSize; ++var14)
            {
                var15 = var14 / (float)tileSize;
                var16 = var6 + (var5 - var6) * var15 - (1.0f / (tileSize * 32.0f));
                var17 = var9 * var15;
                var3.addVertexWithUV((double)var17, 0.0D, (double)(0.0F - var13), (double)var16, (double)var8);
                var3.addVertexWithUV((double)var17, 0.0D, 0.0D, (double)var16, (double)var8);
                var3.addVertexWithUV((double)var17, 1.0D, 0.0D, (double)var16, (double)var7);
                var3.addVertexWithUV((double)var17, 1.0D, (double)(0.0F - var13), (double)var16, (double)var7);
            }

            var3.draw();
            var3.startDrawingQuads();
            var3.setNormal(1.0F, 0.0F, 0.0F);

            for (var14 = 0; var14 < tileSize; ++var14)
            {
                var15 = var14 / (float)tileSize;
                var16 = var6 + (var5 - var6) * var15 - (1.0f / (tileSize * 32.0f));
                var17 = var9 * var15 + 1.0F / tileSize;
                var3.addVertexWithUV((double)var17, 1.0D, (double)(0.0F - var13), (double)var16, (double)var7);
                var3.addVertexWithUV((double)var17, 1.0D, 0.0D, (double)var16, (double)var7);
                var3.addVertexWithUV((double)var17, 0.0D, 0.0D, (double)var16, (double)var8);
                var3.addVertexWithUV((double)var17, 0.0D, (double)(0.0F - var13), (double)var16, (double)var8);
            }

            var3.draw();
            var3.startDrawingQuads();
            var3.setNormal(0.0F, 1.0F, 0.0F);

            for (var14 = 0; var14 < tileSize; ++var14)
            {
                var15 = var14 / (float)tileSize;
                var16 = var8 + (var7 - var8) * var15 - (1.0f / (tileSize * 32.0f));
                var17 = var9 * var15 + 1.0F / tileSize;
                var3.addVertexWithUV(0.0D, (double)var17, 0.0D, (double)var6, (double)var16);
                var3.addVertexWithUV((double)var9, (double)var17, 0.0D, (double)var5, (double)var16);
                var3.addVertexWithUV((double)var9, (double)var17, (double)(0.0F - var13), (double)var5, (double)var16);
                var3.addVertexWithUV(0.0D, (double)var17, (double)(0.0F - var13), (double)var6, (double)var16);
            }

            var3.draw();
            var3.startDrawingQuads();
            var3.setNormal(0.0F, -1.0F, 0.0F);

            for (var14 = 0; var14 < tileSize; ++var14)
            {
                var15 = var14 / (float)tileSize;
                var16 = var8 + (var7 - var8) * var15 - (1.0f / (tileSize * 32.0f));
                var17 = var9 * var15;
                var3.addVertexWithUV((double)var9, (double)var17, 0.0D, (double)var5, (double)var16);
                var3.addVertexWithUV(0.0D, (double)var17, 0.0D, (double)var6, (double)var16);
                var3.addVertexWithUV(0.0D, (double)var17, (double)(0.0F - var13), (double)var6, (double)var16);
                var3.addVertexWithUV((double)var9, (double)var17, (double)(0.0F - var13), (double)var5, (double)var16);
            }

            var3.draw();
            _sceneRenderBackend.Disable(SceneRenderCapability.RescaleNormal);
        }

        _sceneRenderBackend.PopMatrix();
    }

    public void renderItemInFirstPerson(float var1)
    {
        float var2 = prevEquippedProgress + (equippedProgress - prevEquippedProgress) * var1;
        ClientPlayerEntity var3 = _game.Player;
        float var4 = var3.prevPitch + (var3.pitch - var3.prevPitch) * var1;
        _sceneRenderBackend.PushMatrix();
        _sceneRenderBackend.Rotate(var4, 1.0F, 0.0F, 0.0F);
        _sceneRenderBackend.Rotate(var3.prevYaw + (var3.yaw - var3.prevYaw) * var1, 0.0F, 1.0F, 0.0F);
        Lighting.turnOn();
        _sceneRenderBackend.PopMatrix();
        ItemStack var5 = itemToRender;
        float var6 =
            _game.World.GetLuminance(MathHelper.Floor(var3.x), MathHelper.Floor(var3.y), MathHelper.Floor(var3.z));
        float var8;
        float var9;
        float var10;
        if (itemToRender != null)
        {
            int var7 = Item.ITEMS[itemToRender.ItemId].getColorMultiplier(itemToRender.getDamage());
            var8 = (var7 >> 16 & 255) / 255.0F;
            var9 = (var7 >> 8 & 255) / 255.0F;
            var10 = (var7 & 255) / 255.0F;
            _sceneRenderBackend.SetColor(var6 * var8, var6 * var9, var6 * var10, 1.0F);
        }
        else
        {
            _sceneRenderBackend.SetColor(var6, var6, var6, 1.0F);
        }

        float var14;
        if (itemToRender != null && itemToRender.ItemId == Item.Map.id)
        {
            _sceneRenderBackend.PushMatrix();
            var14 = 0.8F;
            float swingProgress = var3.getSwingProgress(var1);
            var9 = MathHelper.Sin(swingProgress * (float)Math.PI);
            var10 = MathHelper.Sin(MathHelper.Sqrt(swingProgress) * (float)Math.PI);
            _sceneRenderBackend.Translate(-var10 * 0.4F,
                MathHelper.Sin(MathHelper.Sqrt(swingProgress) * (float)Math.PI * 2.0F) * 0.2F, -var9 * 0.2F);
            swingProgress = 1.0F - var4 / 45.0F + 0.1F;
            if (swingProgress < 0.0F)
            {
                swingProgress = 0.0F;
            }

            if (swingProgress > 1.0F)
            {
                swingProgress = 1.0F;
            }

            swingProgress = -MathHelper.Cos(swingProgress * (float)Math.PI) * 0.5F + 0.5F;
            _sceneRenderBackend.Translate(0.0F, 0.0F * var14 - (1.0F - var2) * 1.2F - swingProgress * 0.5F + 0.04F,
                -0.9F * var14);
            _sceneRenderBackend.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Rotate(swingProgress * -85.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Enable(SceneRenderCapability.RescaleNormal);
            bindSkinTexture();

            for (int i = 0; i < 2; i++)
            {
                int var21 = i * 2 - 1;
                _sceneRenderBackend.PushMatrix();
                _sceneRenderBackend.Translate(-0.0F, -0.6F, 1.1F * var21);
                _sceneRenderBackend.Rotate(-45 * var21, 1.0F, 0.0F, 0.0F);
                _sceneRenderBackend.Rotate(-90.0F, 0.0F, 0.0F, 1.0F);
                _sceneRenderBackend.Rotate(59.0F, 0.0F, 0.0F, 1.0F);
                _sceneRenderBackend.Rotate(-65 * var21, 0.0F, 1.0F, 0.0F);
                EntityRenderer var11 = _game.EntityRenderDispatcher.GetEntityRenderObject(_game.Player);
                PlayerEntityRenderer var12 = (PlayerEntityRenderer)var11;
                float var13 = 1.0F;
                _sceneRenderBackend.Scale(var13, var13, var13);
                var12.DrawFirstPersonHand();
                _sceneRenderBackend.PopMatrix();
            }

            var9 = var3.getSwingProgress(var1);
            var10 = MathHelper.Sin(var9 * var9 * (float)Math.PI);
            float var18 = MathHelper.Sin(MathHelper.Sqrt(var9) * (float)Math.PI);
            _sceneRenderBackend.Rotate(-var10 * 20.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Rotate(-var18 * 20.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Rotate(-var18 * 80.0F, 1.0F, 0.0F, 0.0F);
            var9 = 0.38F;
            _sceneRenderBackend.Scale(var9, var9, var9);
            _sceneRenderBackend.Rotate(90.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Rotate(180.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Translate(-1.0F, -1.0F, 0.0F);
            var10 = (1 / 64f);
            _sceneRenderBackend.Scale(var10, var10, var10);
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/misc/mapbg.png"));
            Tessellator var19 = Tessellator.instance;
            _sceneRenderBackend.SetNormal(0.0F, 0.0F, -1.0F);
            var19.startDrawingQuads();
            byte var20 = 7;
            var19.addVertexWithUV(0 - var20, 128 + var20, 0.0D, 0.0D, 1.0D);
            var19.addVertexWithUV(128 + var20, 128 + var20, 0.0D, 1.0D, 1.0D);
            var19.addVertexWithUV(128 + var20, 0 - var20, 0.0D, 1.0D, 0.0D);
            var19.addVertexWithUV(0 - var20, 0 - var20, 0.0D, 0.0D, 0.0D);
            var19.draw();
            MapState mapState = ItemMap.getMapState(itemToRender.getDamage(), _game.World);
            mapRenderer.render(_game.Player, _game.TextureManager, mapState);
            _sceneRenderBackend.PopMatrix();
        }
        else if (itemToRender != null)
        {
            _sceneRenderBackend.PushMatrix();
            var14 = 0.8F;
            var8 = var3.getSwingProgress(var1);
            var9 = MathHelper.Sin(var8 * (float)Math.PI);
            var10 = MathHelper.Sin(MathHelper.Sqrt(var8) * (float)Math.PI);
            _sceneRenderBackend.Translate(-var10 * 0.4F,
                MathHelper.Sin(MathHelper.Sqrt(var8) * (float)Math.PI * 2.0F) * 0.2F, -var9 * 0.2F);
            _sceneRenderBackend.Translate(0.7F * var14, -0.65F * var14 - (1.0F - var2) * 0.6F, -0.9F * var14);
            _sceneRenderBackend.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Enable(SceneRenderCapability.RescaleNormal);
            var8 = var3.getSwingProgress(var1);
            var9 = MathHelper.Sin(var8 * var8 * (float)Math.PI);
            var10 = MathHelper.Sin(MathHelper.Sqrt(var8) * (float)Math.PI);
            _sceneRenderBackend.Rotate(-var9 * 20.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Rotate(-var10 * 20.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Rotate(-var10 * 80.0F, 1.0F, 0.0F, 0.0F);
            var8 = 0.4F;
            _sceneRenderBackend.Scale(var8, var8, var8);
            if (itemToRender.getItem().isHandheldRod())
            {
                _sceneRenderBackend.Rotate(180.0F, 0.0F, 1.0F, 0.0F);
            }

            renderItem(var3, itemToRender);
            _sceneRenderBackend.PopMatrix();
        }
        else
        {
            _sceneRenderBackend.PushMatrix();
            var14 = 0.8F;
            var8 = var3.getSwingProgress(var1);
            var9 = MathHelper.Sin(var8 * (float)Math.PI);
            var10 = MathHelper.Sin(MathHelper.Sqrt(var8) * (float)Math.PI);
            _sceneRenderBackend.Translate(-var10 * 0.3F,
                MathHelper.Sin(MathHelper.Sqrt(var8) * (float)Math.PI * 2.0F) * 0.4F, -var9 * 0.4F);
            _sceneRenderBackend.Translate(0.8F * var14, -(12.0F / 16.0F) * var14 - (1.0F - var2) * 0.6F, -0.9F * var14);
            _sceneRenderBackend.Rotate(45.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Enable(SceneRenderCapability.RescaleNormal);
            var8 = var3.getSwingProgress(var1);
            var9 = MathHelper.Sin(var8 * var8 * (float)Math.PI);
            var10 = MathHelper.Sin(MathHelper.Sqrt(var8) * (float)Math.PI);
            _sceneRenderBackend.Rotate(var10 * 70.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Rotate(-var9 * 20.0F, 0.0F, 0.0F, 1.0F);
            bindSkinTexture();
            _sceneRenderBackend.Translate(-1.0F, 3.6F, 3.5F);
            _sceneRenderBackend.Rotate(120.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Rotate(200.0F, 1.0F, 0.0F, 0.0F);
            _sceneRenderBackend.Rotate(-135.0F, 0.0F, 1.0F, 0.0F);
            _sceneRenderBackend.Scale(1.0F, 1.0F, 1.0F);
            _sceneRenderBackend.Translate(5.6F, 0.0F, 0.0F);
            EntityRenderer var15 = _game.EntityRenderDispatcher.GetEntityRenderObject(_game.Player);
            PlayerEntityRenderer var16 = (PlayerEntityRenderer)var15;
            var10 = 1.0F;
            _sceneRenderBackend.Scale(var10, var10, var10);
            var16.DrawFirstPersonHand();
            _sceneRenderBackend.PopMatrix();
        }

        _sceneRenderBackend.Disable(SceneRenderCapability.RescaleNormal);
        Lighting.turnOff();
    }

    public void renderOverlays(float var1)
    {
        _sceneRenderBackend.Disable(SceneRenderCapability.AlphaTest);
        int var2;
        if (_game.Player.isOnFire())
        {
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/terrain.png"));
            renderFireInFirstPerson(var1);
        }

        if (_game.Player.isInsideWall())
        {
            var2 = MathHelper.Floor(_game.Player.x);
            int var3 = MathHelper.Floor(_game.Player.y);
            int var4 = MathHelper.Floor(_game.Player.z);
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/terrain.png"));
            int var6 = _game.World.Reader.GetBlockId(var2, var3, var4);
            if (_game.World.Reader.ShouldSuffocate(var2, var3, var4))
            {
                renderInsideOfBlock(var1, Block.Blocks[var6].GetTexture(Side.North));
            }
            else
            {
                for (int var7 = 0; var7 < 8; ++var7)
                {
                    float var8 = ((var7 >> 0) % 2 - 0.5F) * _game.Player.width * 0.9F;
                    float var9 = ((var7 >> 1) % 2 - 0.5F) * _game.Player.height * 0.2F;
                    float var10 = ((var7 >> 2) % 2 - 0.5F) * _game.Player.width * 0.9F;
                    int var11 = MathHelper.Floor(var2 + var8);
                    int var12 = MathHelper.Floor(var3 + var9);
                    int var13 = MathHelper.Floor(var4 + var10);
                    if (_game.World.Reader.ShouldSuffocate(var11, var12, var13))
                    {
                        var6 = _game.World.Reader.GetBlockId(var11, var12, var13);
                    }
                }
            }

            if (Block.Blocks[var6] != null)
            {
                renderInsideOfBlock(var1, Block.Blocks[var6].GetTexture(Side.North));
            }
        }

        if (_game.Player.isInFluid(Material.Water))
        {
            _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId("/misc/water.png"));
            renderWarpedTextureOverlay(var1);
        }

        _sceneRenderBackend.Enable(SceneRenderCapability.AlphaTest);
    }

    private void renderInsideOfBlock(float var1, int var2)
    {
        Tessellator var3 = Tessellator.instance;
        _game.Player.getBrightnessAtEyes(var1);
        float var4 = 0.1F;
        _sceneRenderBackend.SetColor(var4, var4, var4, 0.5F);
        _sceneRenderBackend.PushMatrix();
        float var5 = -1.0F;
        float var6 = 1.0F;
        float var7 = -1.0F;
        float var8 = 1.0F;
        float var9 = -0.5F;
        float var10 = (1 / 128f);
        float var11 = var2 % 16 / 256.0F - var10;
        float var12 = (var2 % 16 + 15.99F) / 256.0F + var10;
        float var13 = var2 / 16 / 256.0F - var10;
        float var14 = (var2 / 16 + 15.99F) / 256.0F + var10;
        var3.startDrawingQuads();
        var3.addVertexWithUV((double)var5, (double)var7, (double)var9, (double)var12, (double)var14);
        var3.addVertexWithUV((double)var6, (double)var7, (double)var9, (double)var11, (double)var14);
        var3.addVertexWithUV((double)var6, (double)var8, (double)var9, (double)var11, (double)var13);
        var3.addVertexWithUV((double)var5, (double)var8, (double)var9, (double)var12, (double)var13);
        var3.draw();
        _sceneRenderBackend.PopMatrix();
        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
    }

    private void renderWarpedTextureOverlay(float var1)
    {
        Tessellator var2 = Tessellator.instance;
        float var3 = _game.Player.getBrightnessAtEyes(var1);
        _sceneRenderBackend.SetColor(var3, var3, var3, 0.5F);
        _sceneRenderBackend.Enable(SceneRenderCapability.Blend);
        _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
        _sceneRenderBackend.PushMatrix();
        float var4 = 4.0F;
        float var5 = -1.0F;
        float var6 = 1.0F;
        float var7 = -1.0F;
        float var8 = 1.0F;
        float var9 = -0.5F;
        float var10 = -_game.Player.yaw / 64.0F;
        float var11 = _game.Player.pitch / 64.0F;
        var2.startDrawingQuads();
        var2.addVertexWithUV((double)var5, (double)var7, (double)var9, (double)(var4 + var10), (double)(var4 + var11));
        var2.addVertexWithUV((double)var6, (double)var7, (double)var9, (double)(0.0F + var10), (double)(var4 + var11));
        var2.addVertexWithUV((double)var6, (double)var8, (double)var9, (double)(0.0F + var10), (double)(0.0F + var11));
        var2.addVertexWithUV((double)var5, (double)var8, (double)var9, (double)(var4 + var10), (double)(0.0F + var11));
        var2.draw();
        _sceneRenderBackend.PopMatrix();
        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
        _sceneRenderBackend.Disable(SceneRenderCapability.Blend);
    }

    private void renderFireInFirstPerson(float var1)
    {
        Tessellator var2 = Tessellator.instance;
        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 0.9F);
        _sceneRenderBackend.Enable(SceneRenderCapability.Blend);
        _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
        float var3 = 1.0F;

        for (int var4 = 0; var4 < 2; ++var4)
        {
            _sceneRenderBackend.PushMatrix();
            int var5 = Block.Fire.TextureId + var4 * 16;
            int var6 = (var5 & 15) << 4;
            int var7 = var5 & 240;
            float var8 = var6 / 256.0F;
            float var9 = (var6 + 15.99F) / 256.0F;
            float var10 = var7 / 256.0F;
            float var11 = (var7 + 15.99F) / 256.0F;
            float var12 = (0.0F - var3) / 2.0F;
            float var13 = var12 + var3;
            float var14 = 0.0F - var3 / 2.0F;
            float var15 = var14 + var3;
            float var16 = -0.5F;
            _sceneRenderBackend.Translate(-(var4 * 2 - 1) * 0.24F, -0.3F, 0.0F);
            _sceneRenderBackend.Rotate((var4 * 2 - 1) * 10.0F, 0.0F, 1.0F, 0.0F);
            var2.startDrawingQuads();
            var2.addVertexWithUV((double)var12, (double)var14, (double)var16, (double)var9, (double)var11);
            var2.addVertexWithUV((double)var13, (double)var14, (double)var16, (double)var8, (double)var11);
            var2.addVertexWithUV((double)var13, (double)var15, (double)var16, (double)var8, (double)var10);
            var2.addVertexWithUV((double)var12, (double)var15, (double)var16, (double)var9, (double)var10);
            var2.draw();
            _sceneRenderBackend.PopMatrix();
        }

        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
        _sceneRenderBackend.Disable(SceneRenderCapability.Blend);
    }

    public void updateEquippedItem()
    {
        prevEquippedProgress = equippedProgress;
        ClientPlayerEntity var1 = _game.Player;
        ItemStack var2 = var1.inventory.GetItemInHand();
        bool var4 = field_20099_f == var1.inventory.SelectedSlot && var2 == itemToRender;
        if (itemToRender == null && var2 == null)
        {
            var4 = true;
        }

        if (var2 != null && itemToRender != null && var2 != itemToRender && var2.ItemId == itemToRender.ItemId &&
            var2.getDamage() == itemToRender.getDamage())
        {
            itemToRender = var2;
            var4 = true;
        }

        float var5 = 0.4F;
        float var6 = var4 ? 1.0F : 0.0F;
        float var7 = var6 - equippedProgress;
        if (var7 < -var5)
        {
            var7 = -var5;
        }

        if (var7 > var5)
        {
            var7 = var5;
        }

        equippedProgress += var7;
        if (equippedProgress < 0.1F)
        {
            itemToRender = var2;
            field_20099_f = var1.inventory.SelectedSlot;
        }
    }

    public void ResetEquippedProgress()
    {
        equippedProgress = 0.0F;
    }

    private void bindSkinTexture()
    {
        var skinHandle = _game.EntityRenderDispatcher.SkinManager?.GetTextureHandle(_game.Player?.name);
        if (skinHandle != null)
        {
            skinHandle.Bind();
            return;
        }

        _game.TextureManager.BindTexture(_game.TextureManager.GetTextureId(_game.Player.getTexture()));
    }
}
