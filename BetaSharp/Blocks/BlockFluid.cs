using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Silk.NET.Maths;

namespace BetaSharp.Blocks;

public abstract class BlockFluid : Block
{
    protected BlockFluid(int id, Material material) : base(id, (material == Material.Lava ? 14 : 12) * 16 + 13, material)
    {
        float var3 = 0.0F;
        float var4 = 0.0F;
        setBoundingBox(0.0F + var4, 0.0F + var3, 0.0F + var4, 1.0F + var4, 1.0F + var3, 1.0F + var4);
        setTickRandomly(true);
    }

    public override int getColorMultiplier(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        return 0xFFFFFF;
    }

    public static float getFluidHeightFromMeta(int meta)
    {
        if (meta >= 8)
        {
            meta = 0;
        }

        float height = (float)(meta + 1) / 9.0F;
        return height;
    }

    public override int getTexture(int side)
    {
        return side != 0 && side != 1 ? textureId + 1 : textureId;
    }

    protected int getLiquidState(World world, int x, int y, int z)
    {
        return world.getMaterial(x, y, z) != material ? -1 : world.getBlockMeta(x, y, z);
    }

    protected int getLiquidDepth(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        if (iBlockAccess.getMaterial(x, y, z) != material)
        {
            return -1;
        }
        else
        {
            int depth = iBlockAccess.getBlockMeta(x, y, z);
            if (depth >= 8)
            {
                depth = 0;
            }

            return depth;
        }
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool hasCollision(int meta, bool allowLiquids)
    {
        return allowLiquids && meta == 0;
    }

    public override bool isSolidFace(IBlockAccess iBlockAccess, int x, int y, int z, int face)
    {
        Material material = iBlockAccess.getMaterial(x, y, z);
        return material == base.material ?
            false :
            (material == Material.Ice ? false : (face == 1 ? true : base.isSolidFace(iBlockAccess, x, y, z, face)));
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        Material material = iBlockAccess.getMaterial(x, y, z);
        return material == base.material ?
            false :
            (material == Material.Ice ? false : (side == 1 ? true : base.isSideVisible(iBlockAccess, x, y, z, side)));
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Fluids;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return 0;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    private Vector3D<double> getFlow(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        Vector3D<double> flowVector = new(0.0);
        int depth = getLiquidDepth(iBlockAccess, x, y, z);

        for (int direction = 0; direction < 4; ++direction)
        {
            int neighborX = x;
            int neighborZ = z;
            if (direction == 0)
            {
                neighborX = x - 1;
            }

            if (direction == 1)
            {
                neighborZ = z - 1;
            }

            if (direction == 2)
            {
                ++neighborX;
            }

            if (direction == 3)
            {
                ++neighborZ;
            }

            int neighborDepth = getLiquidDepth(iBlockAccess, neighborX, y, neighborZ);
            int depthDiff;
            if (neighborDepth < 0)
            {
                if (!iBlockAccess.getMaterial(neighborX, y, neighborZ).BlocksMovement)
                {
                    neighborDepth = getLiquidDepth(iBlockAccess, neighborX, y - 1, neighborZ);
                    if (neighborDepth >= 0)
                    {
                        depthDiff = neighborDepth - (depth - 8);
                        flowVector += new Vector3D<double>((double)((neighborX - x) * depthDiff), (double)((y - y) * depthDiff), (double)((neighborZ - z) * depthDiff));
                    }
                }
            }
            else if (neighborDepth >= 0)
            {
                depthDiff = neighborDepth - depth;
                flowVector += new Vector3D<double>((double)((neighborX - x) * depthDiff), (double)((y - y) * depthDiff), (double)((neighborZ - z) * depthDiff));
            }
        }

        if (iBlockAccess.getBlockMeta(x, y, z) >= 8)
        {
            bool hasAdjacentSolid = false;
            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x, y, z - 1, 2))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x, y, z + 1, 3))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x - 1, y, z, 4))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x + 1, y, z, 5))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x, y + 1, z - 1, 2))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x, y + 1, z + 1, 3))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x - 1, y + 1, z, 4))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockAccess, x + 1, y + 1, z, 5))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid)
            {
                flowVector = Normalize(flowVector) + new Vector3D<double>(0.0, -0.6, 0.0);
            }
        }

        flowVector = Normalize(flowVector);
        return flowVector;
    }

    public override void applyVelocity(World world, int x, int y, int z, Entity entity, Vec3D velocity)
    {
        Vector3D<double> flowVec = getFlow(world, x, y, z);
        velocity.x += flowVec.X;
        velocity.y += flowVec.Y;
        velocity.z += flowVec.Z;
    }

    public override int getTickRate()
    {
        return material == Material.Water ? 5 : (material == Material.Lava ? 30 : 0);
    }

    public override float getLuminance(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        float luminance = iBlockAccess.getLuminance(x, y, z);
        float luminanceAbove = iBlockAccess.getLuminance(x, y + 1, z);
        return luminance > luminanceAbove ? luminance : luminanceAbove;
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        base.onTick(world, x, y, z, random);
    }

    public override int getRenderLayer()
    {
        return material == Material.Water ? 1 : 0;
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (material == Material.Water && random.NextInt(64) == 0)
        {
            int meta = world.getBlockMeta(x, y, z);
            if (meta > 0 && meta < 8)
            {
                world.playSound((double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F), "liquid.water", random.NextFloat() * 0.25F + 12.0F / 16.0F, random.NextFloat() * 1.0F + 0.5F);
            }
        }

        if (material == Material.Lava && world.getMaterial(x, y + 1, z) == Material.Air && !world.isOpaque(x, y + 1, z) && random.NextInt(100) == 0)
        {
            double particleX = (double)(x + random.NextFloat());
            double particleY = (double)y + BoundingBox.MaxY;
            double particleZ = (double)(z + random.NextFloat());
            world.addParticle("lava", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
        }

    }

    public static double getFlowingAngle(IBlockAccess iBlockAccess, int x, int y, int z, Material material)
    {
        Vector3D<double> flowVec = new(0.0);
        if (material == Material.Water)
        {
            flowVec = ((BlockFluid)FlowingWater).getFlow(iBlockAccess, x, y, z);
        }
        else if (material == Material.Lava)
        {
            flowVec = ((BlockFluid)FlowingLava).getFlow(iBlockAccess, x, y, z);
        }

        return flowVec.X == 0.0D && flowVec.Z == 0.0D ? -1000.0D : Math.Atan2(flowVec.Z, flowVec.X) - Math.PI * 0.5D;
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        checkBlockCollisions(world, x, y, z);
    }

    public override void neighborUpdate(World world, int x, int y, int z, int var5)
    {
        checkBlockCollisions(world, x, y, z);
    }

    private void checkBlockCollisions(World world, int x, int y, int z)
    {
        if (world.getBlockId(x, y, z) == id)
        {
            if (material == Material.Lava)
            {
                bool hasWaterAdjacent = false;
                if (hasWaterAdjacent || world.getMaterial(x, y, z - 1) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || world.getMaterial(x, y, z + 1) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || world.getMaterial(x - 1, y, z) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || world.getMaterial(x + 1, y, z) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || world.getMaterial(x, y + 1, z) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent)
                {
                    int var6 = world.getBlockMeta(x, y, z);
                    if (var6 == 0)
                    {
                        world.setBlock(x, y, z, Block.Obsidian.id);
                    }
                    else if (var6 <= 4)
                    {
                        world.setBlock(x, y, z, Block.Cobblestone.id);
                    }

                    fizz(world, x, y, z);
                }
            }

        }
    }

    protected void fizz(World world, int x, int y, int z)
    {
        world.playSound(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (world.random.NextFloat() - world.random.NextFloat()) * 0.8F);

        for (int particleIndex = 0; particleIndex < 8; ++particleIndex)
        {
            world.addParticle("largesmoke", x + Random.Shared.NextDouble(), y + 1.2D, (double)z + Random.Shared.NextDouble(), 0.0D, 0.0D, 0.0D);
        }

    }

    private static Vector3D<double> Normalize(Vector3D<double> vec)
    {
        double length = (double)MathHelper.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        return length < 1.0E-4D ? new(0.0) : new(vec.X / length, vec.Y / length, vec.Z / length);
    }
}
