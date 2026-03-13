using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
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

    public override int getColorMultiplier(IBlockReader iBlockReader, int x, int y, int z)
    {
        return 0xFFFFFF;
    }

    public static float getFluidHeightFromMeta(int meta)
    {
        if (meta >= 8)
        {
            meta = 0;
        }

        float height = (meta + 1) / 9.0F;
        return height;
    }

    public override int getTexture(int side) => side != 0 && side != 1 ? textureId + 1 : textureId;

    protected int getLiquidState(IBlockReader reader, int x, int y, int z) => reader.GetMaterial(x, y, z) != material ? -1 : reader.GetBlockMeta(x, y, z);

    protected int getLiquidDepth(IBlockReader iBlockReader, int x, int y, int z)
    {
        if (iBlockReader.GetMaterial(x, y, z) != material)
        {
            return -1;
        }

        int depth = iBlockReader.GetBlockMeta(x, y, z);
        if (depth >= 8)
        {
            depth = 0;
        }

        return depth;
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

    public override bool isSolidFace(IBlockReader iBlockReader, int x, int y, int z, int face)
    {
        Material material = iBlockReader.GetMaterial(x, y, z);
        return material == this.material ? false :
            material == Material.Ice ? false :
            face == 1 ? true : base.isSolidFace(iBlockReader, x, y, z, face);
    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        Material material = iBlockReader.GetMaterial(x, y, z);
        return material == this.material ? false :
            material == Material.Ice ? false :
            side == 1 ? true : base.isSideVisible(iBlockReader, x, y, z, side);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        return null;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Fluids;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return 0;
    }

    public override int getDroppedItemCount()
    {
        return 0;
    }

    private Vector3D<double> getFlow(IBlockReader iBlockReader, int x, int y, int z)
    {
        Vector3D<double> flowVector = new(0.0);
        int depth = getLiquidDepth(iBlockReader, x, y, z);

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

            int neighborDepth = getLiquidDepth(iBlockReader, neighborX, y, neighborZ);
            int depthDiff;
            if (neighborDepth < 0)
            {
                if (!iBlockReader.GetMaterial(neighborX, y, neighborZ).BlocksMovement)
                {
                    neighborDepth = getLiquidDepth(iBlockReader, neighborX, y - 1, neighborZ);
                    if (neighborDepth >= 0)
                    {
                        depthDiff = neighborDepth - (depth - 8);
                        flowVector += new Vector3D<double>((neighborX - x) * depthDiff, (y - y) * depthDiff, (neighborZ - z) * depthDiff);
                    }
                }
            }
            else if (neighborDepth >= 0)
            {
                depthDiff = neighborDepth - depth;
                flowVector += new Vector3D<double>((neighborX - x) * depthDiff, (y - y) * depthDiff, (neighborZ - z) * depthDiff);
            }
        }

        if (iBlockReader.GetBlockMeta(x, y, z) >= 8)
        {
            bool hasAdjacentSolid = false;
            if (hasAdjacentSolid || isSolidFace(iBlockReader, x, y, z - 1, 2))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x, y, z + 1, 3))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x - 1, y, z, 4))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x + 1, y, z, 5))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x, y + 1, z - 1, 2))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x, y + 1, z + 1, 3))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x - 1, y + 1, z, 4))
            {
                hasAdjacentSolid = true;
            }

            if (hasAdjacentSolid || isSolidFace(iBlockReader, x + 1, y + 1, z, 5))
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

    public override void applyVelocity(OnApplyVelocityEvt evt)
    {
        Vector3D<double> flowVec = getFlow(evt.Level.Reader, evt.X, evt.Y, evt.Z);
        evt.Velocity.x += flowVec.X;
        evt.Velocity.y += flowVec.Y;
        evt.Velocity.z += flowVec.Z;
    }

    public override int getTickRate()
    {
        return material == Material.Water ? 5 : material == Material.Lava ? 30 : 0;
    }

    public override float getLuminance(ILightProvider lighting, int x, int y, int z)
    {
        float luminance = lighting.GetLuminance(x, y, z);
        float luminanceAbove = lighting.GetLuminance(x, y + 1, z);
        return luminance > luminanceAbove ? luminance : luminanceAbove;
    }

    public override void onTick(OnTickEvt evt)
    {
        base.onTick(evt);
    }

    public override int getRenderLayer()
    {
        return material == Material.Water ? 1 : 0;
    }

    public override void randomDisplayTick(OnTickEvt evt)
    {
        if (material == Material.Water && Random.Shared.Next(64) == 0)
        {
            int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
            if (meta > 0 && meta < 8)
            {
                evt.Level.Broadcaster.PlaySoundAtPos(evt.X + 0.5F, evt.Y + 0.5F, evt.Z + 0.5F, "liquid.water", Random.Shared.NextSingle() * 0.25F + 12.0F / 16.0F, Random.Shared.NextSingle() * 1.0F + 0.5F);
            }
        }

        if (material == Material.Lava && evt.Level.Reader.GetMaterial(evt.X, evt.Y + 1, evt.Z) == Material.Air && !evt.Level.Reader.IsOpaque(evt.X, evt.Y + 1, evt.Z) && Random.Shared.Next(100) == 0)
        {
            double particleX = evt.X + Random.Shared.NextSingle();
            double particleY = evt.Y + BoundingBox.MaxY;
            double particleZ = evt.Z + Random.Shared.NextSingle();
            evt.Level.Broadcaster.AddParticle("lava", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
        }
    }

    public static double getFlowingAngle(IBlockReader iBlockReader, int x, int y, int z, Material material)
    {
        Vector3D<double> flowVec = new(0.0);
        if (material == Material.Water)
        {
            flowVec = ((BlockFluid)FlowingWater).getFlow(iBlockReader, x, y, z);
        }
        else if (material == Material.Lava)
        {
            flowVec = ((BlockFluid)FlowingLava).getFlow(iBlockReader, x, y, z);
        }

        return flowVec.X == 0.0D && flowVec.Z == 0.0D ? -1000.0D : Math.Atan2(flowVec.Z, flowVec.X) - Math.PI * 0.5D;
    }

    public override void onPlaced(OnPlacedEvt evt)
    {
        checkBlockCollisions(evt.Level.Reader, evt.Level.BlockWriter, evt.Level.Broadcaster, evt.X, evt.Y, evt.Z);
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        checkBlockCollisions(evt.Level.Reader, evt.Level.BlockWriter, evt.Level.Broadcaster, evt.X, evt.Y, evt.Z);
    }

    private void checkBlockCollisions(IBlockReader WorldView, WorldWriter WorldWrite, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (WorldView.GetBlockId(x, y, z) == id)
        {
            if (material == Material.Lava)
            {
                bool hasWaterAdjacent = false;
                if (hasWaterAdjacent || WorldView.GetMaterial(x, y, z - 1) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || WorldView.GetMaterial(x, y, z + 1) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || WorldView.GetMaterial(x - 1, y, z) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || WorldView.GetMaterial(x + 1, y, z) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent || WorldView.GetMaterial(x, y + 1, z) == Material.Water)
                {
                    hasWaterAdjacent = true;
                }

                if (hasWaterAdjacent)
                {
                    int var6 = WorldView.GetBlockMeta(x, y, z);
                    if (var6 == 0)
                    {
                        WorldWrite.SetBlock(x, y, z, Obsidian.id);
                    }
                    else if (var6 <= 4)
                    {
                        WorldWrite.SetBlock(x, y, z, Cobblestone.id);
                    }

                    fizz(broadcaster, x, y, z);
                }
            }
        }
    }

    protected void fizz(WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.8F);

        for (int particleIndex = 0; particleIndex < 8; ++particleIndex)
        {
            broadcaster.AddParticle("largesmoke", x + Random.Shared.NextDouble(), y + 1.2D, z + Random.Shared.NextDouble(), 0.0D, 0.0D, 0.0D);
        }
    }

    private static Vector3D<double> Normalize(Vector3D<double> vec)
    {
        double length = MathHelper.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        return length < 1.0E-4D ? new Vector3D<double>(0.0) : new Vector3D<double>(vec.X / length, vec.Y / length, vec.Z / length);
    }
}
