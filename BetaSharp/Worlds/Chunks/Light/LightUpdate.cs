using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Chunks.Light;

internal struct LightUpdate
{
    public readonly LightType lightType;

    public int minX;
    public int minY;
    public int minZ;
    public int maxX;
    public int maxY;
    public int maxZ;

    public LightUpdate(LightType var1, int var2, int var3, int var4, int var5, int var6, int var7)
    {
        lightType = var1;
        minX = var2;
        minY = var3;
        minZ = var4;
        maxX = var5;
        maxY = var6;
        maxZ = var7;
    }

    public void updateLight(World world)
    {
        int var2 = maxX - minX + 1;
        int var3 = maxY - minY + 1;
        int var4 = maxZ - minZ + 1;
        int var5 = var2 * var3 * var4;
        if (var5 > -short.MinValue)
        {
            // _logger.LogInformation("Light too large, skipping!");
        }
        else
        {
            int var6 = 0;
            int var7 = 0;
            bool var8 = false;
            bool var9 = false;

            for (int var10 = minX; var10 <= maxX; ++var10)
            {
                for (int var11 = minZ; var11 <= maxZ; ++var11)
                {
                    int var12 = var10 >> 4;
                    int var13 = var11 >> 4;
                    bool var14 = false;
                    if (var8 && var12 == var6 && var13 == var7)
                    {
                        var14 = var9;
                    }
                    else
                    {
                        var14 = world.isRegionLoaded(var10, 0, var11, 1);
                        if (var14)
                        {
                            Chunk var15 = world.GetChunk(var10 >> 4, var11 >> 4);
                            if (var15.IsEmpty())
                            {
                                var14 = false;
                            }
                        }

                        var9 = var14;
                        var6 = var12;
                        var7 = var13;
                    }

                    if (var14)
                    {
                        if (minY < 0)
                        {
                            minY = 0;
                        }

                        if (maxY >= 128)
                        {
                            maxY = 127;
                        }

                        for (int var27 = minY; var27 <= maxY; ++var27)
                        {
                            int var16 = world.getBrightness(lightType, var10, var27, var11);
                            bool var17 = false;
                            int var18 = world.getBlockId(var10, var27, var11);
                            int var19 = Block.BlockLightOpacity[var18];
                            if (var19 == 0)
                            {
                                var19 = 1;
                            }

                            int var20 = 0;
                            if (lightType == LightType.Sky)
                            {
                                if (world.isTopY(var10, var27, var11))
                                {
                                    var20 = 15;
                                }
                            }
                            else if (lightType == LightType.Block)
                            {
                                var20 = Block.BlocksLightLuminance[var18];
                            }

                            int var21;
                            int var28;
                            if (var19 >= 15 && var20 == 0)
                            {
                                var28 = 0;
                            }
                            else
                            {
                                var21 = world.getBrightness(lightType, var10 - 1, var27, var11);
                                int var22 = world.getBrightness(lightType, var10 + 1, var27, var11);
                                int var23 = world.getBrightness(lightType, var10, var27 - 1, var11);
                                int var24 = world.getBrightness(lightType, var10, var27 + 1, var11);
                                int var25 = world.getBrightness(lightType, var10, var27, var11 - 1);
                                int var26 = world.getBrightness(lightType, var10, var27, var11 + 1);
                                var28 = var21;
                                if (var22 > var21)
                                {
                                    var28 = var22;
                                }

                                if (var23 > var28)
                                {
                                    var28 = var23;
                                }

                                if (var24 > var28)
                                {
                                    var28 = var24;
                                }

                                if (var25 > var28)
                                {
                                    var28 = var25;
                                }

                                if (var26 > var28)
                                {
                                    var28 = var26;
                                }

                                var28 -= var19;
                                if (var28 < 0)
                                {
                                    var28 = 0;
                                }

                                if (var20 > var28)
                                {
                                    var28 = var20;
                                }
                            }

                            if (var16 != var28)
                            {
                                world.setLight(lightType, var10, var27, var11, var28);
                                var21 = var28 - 1;
                                if (var21 < 0)
                                {
                                    var21 = 0;
                                }

                                world.updateLight(lightType, var10 - 1, var27, var11, var21);
                                world.updateLight(lightType, var10, var27 - 1, var11, var21);
                                world.updateLight(lightType, var10, var27, var11 - 1, var21);
                                if (var10 + 1 >= maxX)
                                {
                                    world.updateLight(lightType, var10 + 1, var27, var11, var21);
                                }

                                if (var27 + 1 >= maxY)
                                {
                                    world.updateLight(lightType, var10, var27 + 1, var11, var21);
                                }

                                if (var11 + 1 >= maxZ)
                                {
                                    world.updateLight(lightType, var10, var27, var11 + 1, var21);
                                }
                            }
                        }
                    }
                }
            }

        }
    }

    public bool expand(int var1, int var2, int var3, int var4, int var5, int var6)
    {
        if (var1 >= minX && var2 >= minY && var3 >= minZ && var4 <= maxX && var5 <= maxY && var6 <= maxZ)
        {
            return true;
        }
        else
        {
            byte var7 = 1;
            if (var1 >= minX - var7 && var2 >= minY - var7 && var3 >= minZ - var7 && var4 <= maxX + var7 && var5 <= maxY + var7 && var6 <= maxZ + var7)
            {
                int var8 = maxX - minX;
                int var9 = maxY - minY;
                int var10 = maxZ - minZ;
                if (var1 > minX)
                {
                    var1 = minX;
                }

                if (var2 > minY)
                {
                    var2 = minY;
                }

                if (var3 > minZ)
                {
                    var3 = minZ;
                }

                if (var4 < maxX)
                {
                    var4 = maxX;
                }

                if (var5 < maxY)
                {
                    var5 = maxY;
                }

                if (var6 < maxZ)
                {
                    var6 = maxZ;
                }

                int var11 = var4 - var1;
                int var12 = var5 - var2;
                int var13 = var6 - var3;
                int var14 = var8 * var9 * var10;
                int var15 = var11 * var12 * var13;
                if (var15 - var14 <= 2)
                {
                    minX = var1;
                    minY = var2;
                    minZ = var3;
                    maxX = var4;
                    maxY = var5;
                    maxZ = var6;
                    return true;
                }
            }

            return false;
        }
    }
}
