using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Mechanics;

public class Explosion
{
    private readonly IWorldContext _level;
    private readonly JavaRandom ExplosionRNG = new();
    public HashSet<BlockPos> destroyedBlockPositions = new();
    public Entity? exploder;
    public float explosionSize;
    public double explosionX;
    public double explosionY;
    public double explosionZ;
    public bool isFlaming = false;

    public Explosion(IWorldContext level, Entity? var2, double var3, double var5, double var7, float var9)
    {
        _level = level;
        exploder = var2;
        explosionSize = var9;
        explosionX = var3;
        explosionY = var5;
        explosionZ = var7;
    }

    public void doExplosionA()
    {
        float var1 = explosionSize;
        byte var2 = 16;

        int var3;
        int var4;
        int var5;
        double var15;
        double var17;
        double var19;
        for (var3 = 0; var3 < var2; ++var3)
        {
            for (var4 = 0; var4 < var2; ++var4)
            {
                for (var5 = 0; var5 < var2; ++var5)
                {
                    if (var3 == 0 || var3 == var2 - 1 || var4 == 0 || var4 == var2 - 1 || var5 == 0 || var5 == var2 - 1)
                    {
                        double var6 = var3 / (var2 - 1.0F) * 2.0F - 1.0F;
                        double var8 = var4 / (var2 - 1.0F) * 2.0F - 1.0F;
                        double var10 = var5 / (var2 - 1.0F) * 2.0F - 1.0F;
                        double var12 = Math.Sqrt(var6 * var6 + var8 * var8 + var10 * var10);
                        var6 /= var12;
                        var8 /= var12;
                        var10 /= var12;
                        float var14 = explosionSize * (0.7F + _level.Random.NextFloat() * 0.6F);
                        var15 = explosionX;
                        var17 = explosionY;
                        var19 = explosionZ;

                        for (float var21 = 0.3F; var14 > 0.0F; var14 -= var21 * (12.0F / 16.0F))
                        {
                            int var22 = MathHelper.Floor(var15);
                            int var23 = MathHelper.Floor(var17);
                            int var24 = MathHelper.Floor(var19);
                            int var25 = _level.Reader.GetBlockId(var22, var23, var24);
                            if (var25 > 0)
                            {
                                var14 -= (Block.Blocks[var25].getBlastResistance(exploder) + 0.3F) * var21;
                            }

                            if (var14 > 0.0F)
                            {
                                destroyedBlockPositions.Add(new BlockPos(var22, var23, var24));
                            }

                            var15 += var6 * var21;
                            var17 += var8 * var21;
                            var19 += var10 * var21;
                        }
                    }
                }
            }
        }

        explosionSize *= 2.0F;
        var3 = MathHelper.Floor(explosionX - explosionSize - 1.0D);
        var4 = MathHelper.Floor(explosionX + explosionSize + 1.0D);
        var5 = MathHelper.Floor(explosionY - explosionSize - 1.0D);
        int var29 = MathHelper.Floor(explosionY + explosionSize + 1.0D);
        int var7 = MathHelper.Floor(explosionZ - explosionSize - 1.0D);
        int var30 = MathHelper.Floor(explosionZ + explosionSize + 1.0D);
        List<Entity> var9 = _level.Entities.GetEntities(exploder, new Box(var3, var5, var7, var4, var29, var30));
        Vec3D var31 = new(explosionX, explosionY, explosionZ);

        for (int var11 = 0; var11 < var9.Count; ++var11)
        {
            Entity var33 = var9[var11];
            double var13 = var33.getDistance(explosionX, explosionY, explosionZ) / explosionSize;
            if (var13 <= 1.0D)
            {
                var15 = var33.x - explosionX;
                var17 = var33.y - explosionY;
                var19 = var33.z - explosionZ;
                double var39 = MathHelper.Sqrt(var15 * var15 + var17 * var17 + var19 * var19);
                var15 /= var39;
                var17 /= var39;
                var19 /= var39;
                double var40 = _level.Reader.GetVisibilityRatio(var31, var33.boundingBox);
                double var41 = (1.0D - var13) * var40;
                var33.damage(exploder, (int)((var41 * var41 + var41) / 2.0D * 8.0D * explosionSize + 1.0D));
                var33.velocityX += var15 * var41;
                var33.velocityY += var17 * var41;
                var33.velocityZ += var19 * var41;
            }
        }

        explosionSize = var1;
        List<BlockPos> var32 = new(destroyedBlockPositions);
        if (!isFlaming) return;

        for (int var34 = var32.Count - 1; var34 >= 0; --var34)
        {
            BlockPos var35 = var32[var34];
            int var36 = var35.x;
            int var37 = var35.y;
            int var16 = var35.z;
            int var38 = _level.Reader.GetBlockId(var36, var37, var16);
            int var18 = _level.Reader.GetBlockId(var36, var37 - 1, var16);
            if (var38 == 0 && Block.BlocksOpaque[var18] && ExplosionRNG.NextInt(3) == 0)
            {
                _level.Writer.SetBlock(var36, var37, var16, Block.Fire.id);
            }
        }
    }

    public void doExplosionB(bool var1)
    {
        _level.Broadcaster.PlaySoundAtPos(explosionX, explosionY, explosionZ, "random.explode", 4.0F, (1.0F + (_level.Random.NextFloat() - _level.Random.NextFloat()) * 0.2F) * 0.7F);
        List<BlockPos> var2 = new(destroyedBlockPositions);

        for (int var3 = var2.Count - 1; var3 >= 0; --var3)
        {
            BlockPos var4 = var2[var3];
            int var5 = var4.x;
            int var6 = var4.y;
            int var7 = var4.z;
            int var8 = _level.Reader.GetBlockId(var5, var6, var7);
            if (var1)
            {
                double var9 = var5 + _level.Random.NextFloat();
                double var11 = var6 + _level.Random.NextFloat();
                double var13 = var7 + _level.Random.NextFloat();
                double var15 = var9 - explosionX;
                double var17 = var11 - explosionY;
                double var19 = var13 - explosionZ;
                double var21 = MathHelper.Sqrt(var15 * var15 + var17 * var17 + var19 * var19);
                var15 /= var21;
                var17 /= var21;
                var19 /= var21;
                double var23 = 0.5D / (var21 / explosionSize + 0.1D);
                var23 *= _level.Random.NextFloat() * _level.Random.NextFloat() + 0.3F;
                var15 *= var23;
                var17 *= var23;
                var19 *= var23;
                _level.Broadcaster.AddParticle("explode", (var9 + explosionX * 1.0D) / 2.0D, (var11 + explosionY * 1.0D) / 2.0D, (var13 + explosionZ * 1.0D) / 2.0D, var15, var17, var19);
                _level.Broadcaster.AddParticle("smoke", var9, var11, var13, var15, var17, var19);
            }

            if (var8 > 0)
            {
                Block.Blocks[var8].dropStacks(new OnDropEvent(_level, var5, var6, var7, _level.Reader.GetBlockMeta(var5, var6, var7), 0.3F));
                _level.Writer.SetBlock(var5, var6, var7, 0);
                Block.Blocks[var8].onDestroyedByExplosion(new OnDestroyedByExplosionEvent(_level, var5, var6, var7));
            }
        }
    }
}
