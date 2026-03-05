using BetaSharp.Blocks;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Rendering;

public class ParticleManager
{
    protected World worldObj;
    private readonly List<EntityFX>[] _fxLayers = new List<EntityFX>[4];
    private readonly TextureManager _renderer;
    private readonly JavaRandom _rand = new();

    public ParticleManager(World var1, TextureManager var2)
    {
        if (var1 != null)
        {
            worldObj = var1;
        }

        _renderer = var2;

        for (int i = 0; i < 4; i++)
        {
            _fxLayers[i] = new List<EntityFX>();
        }

    }

    public void addEffect(EntityFX var1)
    {
        int var2 = var1.getFXLayer();
        if (_fxLayers[var2].Count >= 4000)
        {
            _fxLayers[var2].RemoveAt(0);
        }

        _fxLayers[var2].Add(var1);
    }

    public void updateEffects()
    {
        for (int var1 = 0; var1 < 4; ++var1)
        {
            for (int var2 = 0; var2 < _fxLayers[var1].Count; ++var2)
            {
                EntityFX var3 = _fxLayers[var1][var2];
                var3.tick();
                if (var3.dead)
                {
                    _fxLayers[var1].RemoveAt(var2--);
                }
            }
        }

    }

    public void renderParticles(Entity var1, float var2)
    {
        float var3 = MathHelper.Cos(var1.yaw * (float)Math.PI / 180.0F);
        float var4 = MathHelper.Sin(var1.yaw * (float)Math.PI / 180.0F);
        float var5 = -var4 * MathHelper.Sin(var1.pitch * (float)Math.PI / 180.0F);
        float var6 = var3 * MathHelper.Sin(var1.pitch * (float)Math.PI / 180.0F);
        float var7 = MathHelper.Cos(var1.pitch * (float)Math.PI / 180.0F);
        EntityFX.interpPosX = var1.lastTickX + (var1.x - var1.lastTickX) * (double)var2;
        EntityFX.interpPosY = var1.lastTickY + (var1.y - var1.lastTickY) * (double)var2;
        EntityFX.interpPosZ = var1.lastTickZ + (var1.z - var1.lastTickZ) * (double)var2;

        for (int var8 = 0; var8 < 3; ++var8)
        {
            if (_fxLayers[var8].Count != 0)
            {
                TextureHandle texture = null;
                if (var8 == 0) texture = _renderer.GetTextureId("/particles.png");
                if (var8 == 1) texture = _renderer.GetTextureId("/terrain.png");
                if (var8 == 2) texture = _renderer.GetTextureId("/gui/items.png");

                _renderer.BindTexture(texture);
                Tessellator var10 = Tessellator.instance;
                var10.startDrawingQuads();

                for (int var11 = 0; var11 < _fxLayers[var8].Count; ++var11)
                {
                    EntityFX var12 = _fxLayers[var8][var11];
                    var12.renderParticle(var10, var2, var3, var7, var4, var5, var6);
                }

                var10.draw();
            }
        }

    }

    public void func_1187_b(Entity var1, float var2)
    {
        byte var3 = 3;
        if (_fxLayers[var3].Count != 0)
        {
            Tessellator var4 = Tessellator.instance;

            for (int var5 = 0; var5 < _fxLayers[var3].Count; ++var5)
            {
                EntityFX var6 = _fxLayers[var3][var5];
                var6.renderParticle(var4, var2, 0.0F, 0.0F, 0.0F, 0.0F, 0.0F);
            }

        }
    }

    public void clearEffects(World var1)
    {
        worldObj = var1;

        for (int var2 = 0; var2 < 4; ++var2)
        {
            _fxLayers[var2].Clear();
        }

    }

    public void addBlockDestroyEffects(int var1, int var2, int var3, int var4, int var5)
    {
        if (var4 != 0)
        {
            Block var6 = Block.Blocks[var4];
            byte var7 = 4;

            for (int var8 = 0; var8 < var7; ++var8)
            {
                for (int var9 = 0; var9 < var7; ++var9)
                {
                    for (int var10 = 0; var10 < var7; ++var10)
                    {
                        double var11 = var1 + (var8 + 0.5D) / var7;
                        double var13 = var2 + (var9 + 0.5D) / var7;
                        double var15 = var3 + (var10 + 0.5D) / var7;
                        int var17 = _rand.NextInt(6);
                        addEffect((new EntityDiggingFX(worldObj, var11, var13, var15, var11 - (double)var1 - 0.5D, var13 - (double)var2 - 0.5D, var15 - (double)var3 - 0.5D, var6, var17, var5)).func_4041_a(var1, var2, var3));
                    }
                }
            }

        }
    }

    public void addBlockHitEffects(int var1, int var2, int var3, int var4)
    {
        int var5 = worldObj.getBlockId(var1, var2, var3);
        if (var5 != 0)
        {
            Block var6 = Block.Blocks[var5];
            Box blockBB = var6.BoundingBox;
            float var7 = 0.1F;
            double var8 = var1 + _rand.NextDouble() * (blockBB.MaxX - blockBB.MinX - (var7 * 2.0F)) + var7 + blockBB.MinX;
            double var10 = var2 + _rand.NextDouble() * (blockBB.MaxY - blockBB.MinY - (var7 * 2.0F)) + var7 + blockBB.MinY;
            double var12 = var3 + _rand.NextDouble() * (blockBB.MaxZ - blockBB.MinZ - (var7 * 2.0F)) + var7 + blockBB.MinZ;
            if (var4 == 0)
            {
                var10 = var2 + blockBB.MinY - var7;
            }

            if (var4 == 1)
            {
                var10 = var2 + blockBB.MaxY + var7;
            }

            if (var4 == 2)
            {
                var12 = var3 + blockBB.MinZ - var7;
            }

            if (var4 == 3)
            {
                var12 = var3 + blockBB.MaxZ + var7;
            }

            if (var4 == 4)
            {
                var8 = var1 + blockBB.MinX - var7;
            }

            if (var4 == 5)
            {
                var8 = var1 + blockBB.MaxX + var7;
            }

            addEffect((new EntityDiggingFX(worldObj, var8, var10, var12, 0.0D, 0.0D, 0.0D, var6, var4, worldObj.getBlockMeta(var1, var2, var3))).func_4041_a(var1, var2, var3).scaleVelocity(0.2F).scaleSize(0.6F));
        }
    }

    public string getStatistics()
    {
        return "" + (_fxLayers[0].Count + _fxLayers[1].Count + _fxLayers[2].Count);
    }
}
