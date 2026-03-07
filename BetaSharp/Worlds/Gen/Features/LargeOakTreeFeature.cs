using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class LargeOakTreeFeature : Feature
{

    static readonly sbyte[] MINOR_AXES = [2, 0, 0, 1, 2, 1];
    JavaRandom random = new();
    World world;
    int[] origin = [0, 0, 0];
    int height;
    int trunkHeight;
    double trunkScale = 0.618D;
    double branchSlope = 0.381D;
    double branchLengthScale = 1.0D;
    double foliageDensity = 1.0D;
    int trunkWidth = 1;
    int maxTrunkHeight = 12;
    int foliageClusterHeight = 4;
    int[][] branches;

    void makeBranches()
    {
        trunkHeight = (int)(height * trunkScale);
        if (trunkHeight >= height)
        {
            trunkHeight = height - 1;
        }

        int var1 = (int)(1.382D + Math.Pow(foliageDensity * height / 13.0D, 2.0D));
        if (var1 < 1)
        {
            var1 = 1;
        }

        //int[][] var2 = new int[var1 * this.field_878_e][4];
        int[][] var2 = new int[var1 * height][];
        for (int i = 0; i < var2.Length; i++)
        {
            var2[i] = new int[4];
        }
        int var3 = origin[1] + height - foliageClusterHeight;
        int var4 = 1;
        int var5 = origin[1] + trunkHeight;
        int var6 = var3 - origin[1];
        var2[0][0] = origin[0];
        var2[0][1] = var3;
        var2[0][2] = origin[2];
        var2[0][3] = var5;
        --var3;

        while (true)
        {
            while (var6 >= 0)
            {
                int var7 = 0;
                float var8 = getTreeShape(var6);
                if (var8 < 0.0F)
                {
                    --var3;
                    --var6;
                }
                else
                {
                    for (double var9 = 0.5D; var7 < var1; ++var7)
                    {
                        double var11 = branchLengthScale * (double)var8 * ((double)random.NextFloat() + 0.328D);
                        double var13 = (double)random.NextFloat() * 2.0D * 3.14159D;
                        int var15 = MathHelper.Floor(var11 * Math.Sin(var13) + origin[0] + var9);
                        int var16 = MathHelper.Floor(var11 * Math.Cos(var13) + origin[2] + var9);
                        int[] var17 = [var15, var3, var16];
                        int[] var18 = [var15, var3 + foliageClusterHeight, var16];
                        if (tryBranch(var17, var18) == -1)
                        {
                            int[] var19 = [origin[0], origin[1], origin[2]];
                            double var20 = Math.Sqrt(Math.Pow(Math.Abs(origin[0] - var17[0]), 2.0D) + Math.Pow(Math.Abs(origin[2] - var17[2]), 2.0D));
                            double var22 = var20 * branchSlope;
                            if (var17[1] - var22 > var5)
                            {
                                var19[1] = var5;
                            }
                            else
                            {
                                var19[1] = (int)(var17[1] - var22);
                            }

                            if (tryBranch(var19, var17) == -1)
                            {
                                var2[var4][0] = var15;
                                var2[var4][1] = var3;
                                var2[var4][2] = var16;
                                var2[var4][3] = var19[1];
                                ++var4;
                            }
                        }
                    }

                    --var3;
                    --var6;
                }
            }

            //this.field_868_o = new int[var4][4];
            branches = new int[var4][];
            for (int i = 0; i < branches.Length; i++)
            {
                branches[i] = new int[4];
            }
            java.lang.System.arraycopy(var2, 0, branches, 0, var4);
            return;
        }
    }

    void placeCluster(int var1, int var2, int var3, float var4, sbyte var5, int var6)
    {
        int var7 = (int)((double)var4 + 0.618D);
        sbyte var8 = MINOR_AXES[var5];
        sbyte var9 = MINOR_AXES[var5 + 3];
        int[] var10 = [var1, var2, var3];
        int[] var11 = [0, 0, 0];
        int var12 = -var7;
        int var13 = -var7;

        for (var11[var5] = var10[var5]; var12 <= var7; ++var12)
        {
            var11[var8] = var10[var8] + var12;
            var13 = -var7;

            while (var13 <= var7)
            {
                double var15 = Math.Sqrt(
                    Math.Pow(Math.Abs(var12) + 0.5D, 2.0D) +
                    Math.Pow(Math.Abs(var13) + 0.5D, 2.0D)
                );

                if (var15 > (double)var4)
                {
                    ++var13;
                    continue;
                }

                var11[var9] = var10[var9] + var13;
                int var14 = world.getBlockId(var11[0], var11[1], var11[2]);

                if (var14 != 0 && var14 != 18)
                {
                    ++var13;
                    continue;
                }

                world.SetBlockWithoutNotifyingNeighbors(var11[0], var11[1], var11[2], var6);
                ++var13;
            }
        }
    }

    float getTreeShape(int var1)
    {
        if (var1 < (double)(float)height * 0.3D)
        {
            return -1.618F;
        }
        else
        {
            float var2 = height / 2.0F;
            float var3 = height / 2.0F - var1;
            float var4;
            if (var3 == 0.0F)
            {
                var4 = var2;
            }
            else if (Math.Abs(var3) >= var2)
            {
                var4 = 0.0F;
            }
            else
            {
                var4 = (float)Math.Sqrt(Math.Pow((double)Math.Abs(var2), 2.0D) - Math.Pow((double)Math.Abs(var3), 2.0D));
            }

            var4 *= 0.5F;
            return var4;
        }
    }

    float getClusterShape(int var1)
    {
        return var1 >= 0 && var1 < foliageClusterHeight ? var1 != 0 && var1 != foliageClusterHeight - 1 ? 3.0F : 2.0F : -1.0F;
    }

    void placeFoliageCluster(int var1, int var2, int var3)
    {
        int var4 = var2;

        for (int var5 = var2 + foliageClusterHeight; var4 < var5; ++var4)
        {
            float var6 = getClusterShape(var4 - var2);
            placeCluster(var1, var4, var3, var6, 1, 18);
        }

    }

    void placeBranch(int[] var1, int[] var2, int var3)
    {
        int[] var4 = [0, 0, 0];
        sbyte var5 = 0;

        sbyte var6;
        for (var6 = 0; var5 < 3; ++var5)
        {
            var4[var5] = var2[var5] - var1[var5];
            if (Math.Abs(var4[var5]) > Math.Abs(var4[var6]))
            {
                var6 = var5;
            }
        }

        if (var4[var6] != 0)
        {
            sbyte var7 = MINOR_AXES[var6];
            sbyte var8 = MINOR_AXES[var6 + 3];
            sbyte var9;
            if (var4[var6] > 0)
            {
                var9 = 1;
            }
            else
            {
                var9 = -1;
            }

            double var10 = var4[var7] / (double)var4[var6];
            double var12 = var4[var8] / (double)var4[var6];
            int[] var14 = [0, 0, 0];
            int var15 = 0;

            for (int var16 = var4[var6] + var9; var15 != var16; var15 += var9)
            {
                var14[var6] = MathHelper.Floor(var1[var6] + var15 + 0.5D);
                var14[var7] = MathHelper.Floor(var1[var7] + var15 * var10 + 0.5D);
                var14[var8] = MathHelper.Floor(var1[var8] + var15 * var12 + 0.5D);
                world.SetBlockWithoutNotifyingNeighbors(var14[0], var14[1], var14[2], var3);
            }

        }
    }

    void placeFoliage()
    {
        int var1 = 0;

        for (int var2 = branches.Length; var1 < var2; ++var1)
        {
            int var3 = branches[var1][0];
            int var4 = branches[var1][1];
            int var5 = branches[var1][2];
            placeFoliageCluster(var3, var4, var5);
        }

    }

    bool shouldPlaceBranch(int var1)
    {
        return var1 >= height * 0.2D;
    }

    void placeTrunk()
    {
        int var1 = origin[0];
        int var2 = origin[1];
        int var3 = origin[1] + trunkHeight;
        int var4 = origin[2];
        int[] var5 = [var1, var2, var4];
        int[] var6 = [var1, var3, var4];
        placeBranch(var5, var6, 17);
        if (trunkWidth == 2)
        {
            ++var5[0];
            ++var6[0];
            placeBranch(var5, var6, 17);
            ++var5[2];
            ++var6[2];
            placeBranch(var5, var6, 17);
            var5[0] += -1;
            var6[0] += -1;
            placeBranch(var5, var6, 17);
        }

    }

    void placeBranches()
    {
        int var1 = 0;
        int var2 = branches.Length;

        for (int[] var3 = [origin[0], origin[1], origin[2]]; var1 < var2; ++var1)
        {
            int[] var4 = branches[var1];
            int[] var5 = [var4[0], var4[1], var4[2]];
            var3[1] = var4[3];
            int var6 = var3[1] - origin[1];
            if (shouldPlaceBranch(var6))
            {
                placeBranch(var3, var5, 17);
            }
        }

    }

    int tryBranch(int[] var1, int[] var2)
    {
        int[] var3 = [0, 0, 0];
        sbyte var4 = 0;

        sbyte var5;
        for (var5 = 0; var4 < 3; ++var4)
        {
            var3[var4] = var2[var4] - var1[var4];
            if (Math.Abs(var3[var4]) > Math.Abs(var3[var5]))
            {
                var5 = var4;
            }
        }

        if (var3[var5] == 0)
        {
            return -1;
        }
        else
        {
            sbyte var6 = MINOR_AXES[var5];
            sbyte var7 = MINOR_AXES[var5 + 3];
            sbyte var8;
            if (var3[var5] > 0)
            {
                var8 = 1;
            }
            else
            {
                var8 = -1;
            }

            double var9 = var3[var6] / (double)var3[var5];
            double var11 = var3[var7] / (double)var3[var5];
            int[] var13 = [0, 0, 0];
            int var14 = 0;

            int var15;
            for (var15 = var3[var5] + var8; var14 != var15; var14 += var8)
            {
                var13[var5] = var1[var5] + var14;
                var13[var6] = MathHelper.Floor(var1[var6] + var14 * var9);
                var13[var7] = MathHelper.Floor(var1[var7] + var14 * var11);
                int var16 = world.getBlockId(var13[0], var13[1], var13[2]);
                if (var16 != 0 && var16 != 18)
                {
                    break;
                }
            }

            return var14 == var15 ? -1 : Math.Abs(var14);
        }
    }

    bool canPlace()
    {
        int[] var1 = [origin[0], origin[1], origin[2]];
        int[] var2 = [origin[0], origin[1] + height - 1, origin[2]];
        int var3 = world.getBlockId(origin[0], origin[1] - 1, origin[2]);
        if (var3 != 2 && var3 != 3)
        {
            return false;
        }
        else
        {
            int var4 = tryBranch(var1, var2);
            if (var4 == -1)
            {
                return true;
            }
            else if (var4 < 6)
            {
                return false;
            }
            else
            {
                height = var4;
                return true;
            }
        }
    }

    public override void prepare(double d0, double d1, double d2)
    {
        maxTrunkHeight = (int)(d0 * 12.0D);
        if (d0 > 0.5D)
        {
            foliageClusterHeight = 5;
        }

        branchLengthScale = d1;
        foliageDensity = d2;
    }

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        this.world = world;
        long var6 = rand.NextLong();
        random.SetSeed(var6);
        origin[0] = x;
        origin[1] = y;
        origin[2] = z;
        if (height == 0)
        {
            height = 5 + random.NextInt(maxTrunkHeight);
        }

        if (!canPlace())
        {
            return false;
        }
        else
        {
            makeBranches();
            placeFoliage();
            placeTrunk();
            placeBranches();
            return true;
        }
    }
}
