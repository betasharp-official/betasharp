using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Silk.NET.Maths;

namespace BetaSharp.Blocks;

public abstract class BlockFluid : Block
{
    protected BlockFluid(int id, Material material) : base(id, (material == Material.Lava ? 14 : 12) * 16 + 13, material)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
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

    private int getLiquidDepth(IBlockReader iBlockReader, int x, int y, int z)
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
        Material mat = iBlockReader.GetMaterial(x, y, z);
        return mat != material && (mat != Material.Ice && (face == 1 || base.isSolidFace(iBlockReader, x, y, z, face)));
    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        Material mat = iBlockReader.GetMaterial(x, y, z);
        return mat != material && (mat != Material.Ice && (side == 1 || base.isSideVisible(iBlockReader, x, y, z, side)));
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
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
            switch (direction)
            {
                case 0:
                    neighborX = x - 1;
                    break;
                case 1:
                    neighborZ = z - 1;
                    break;
                case 2:
                    ++neighborX;
                    break;
                case 3:
                    ++neighborZ;
                    break;
            }

            int neighborDepth = getLiquidDepth(iBlockReader, neighborX, y, neighborZ);
            int depthDiff;
            if (neighborDepth < 0)
            {
                if (iBlockReader.GetMaterial(neighborX, y, neighborZ).BlocksMovement) continue;
                neighborDepth = getLiquidDepth(iBlockReader, neighborX, y - 1, neighborZ);
                if (neighborDepth < 0) continue;
                depthDiff = neighborDepth - (depth - 8);
            }
            else
            {
                depthDiff = neighborDepth - depth;
            }

            flowVector += new Vector3D<double>((neighborX - x) * depthDiff, (y - y) * depthDiff, (neighborZ - z) * depthDiff);
        }

        if (iBlockReader.GetBlockMeta(x, y, z) >= 8)
        {
            bool hasAdjacentSolid =
                isSolidFace(iBlockReader, x, y, z - 1, 2) ||
                isSolidFace(iBlockReader, x, y, z + 1, 3) ||
                isSolidFace(iBlockReader, x - 1, y, z, 4) ||
                isSolidFace(iBlockReader, x + 1, y, z, 5) ||
                isSolidFace(iBlockReader, x, y + 1, z - 1, 2) ||
                isSolidFace(iBlockReader, x, y + 1, z + 1, 3) ||
                isSolidFace(iBlockReader, x - 1, y + 1, z, 4) ||
                isSolidFace(iBlockReader, x + 1, y + 1, z, 5);

            if (hasAdjacentSolid)
            {
                flowVector = Normalize(flowVector) + new Vector3D<double>(0.0, -0.6, 0.0);
            }
        }

        flowVector = Normalize(flowVector);
        return flowVector;
    }

    public override Vec3D applyVelocity(OnApplyVelocityEvent @event)
    {
        Vector3D<double> flowVec = getFlow(@event.World.Reader, @event.X, @event.Y, @event.Z);
        return new Vec3D(flowVec.X, flowVec.Y, flowVec.Z);
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

    public override void onTick(OnTickEvent @event)
    {
        base.onTick(@event);
    }

    public override int getRenderLayer()
    {
        return material == Material.Water ? 1 : 0;
    }

    public override void randomDisplayTick(OnTickEvent @event)
    {
        if (material == Material.Water && Random.Shared.Next(64) == 0)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if (meta is > 0 and < 8)
            {
                @event.World.Broadcaster.PlaySoundAtPos(
                    @event.X + 0.5F,
                    @event.Y + 0.5F,
                    @event.Z + 0.5F,
                    "liquid.water",
                    Random.Shared.NextSingle() * 0.25F + 12.0F / 16.0F,
                    Random.Shared.NextSingle() * 1.0F + 0.5F
                );
            }
        }

        if (material == Material.Lava &&
            @event.World.Reader.GetMaterial(@event.X, @event.Y + 1, @event.Z) == Material.Air &&
            !@event.World.Reader.IsOpaque(@event.X, @event.Y + 1, @event.Z) && Random.Shared.Next(100) == 0)
        {
            double particleX = @event.X + Random.Shared.NextSingle();
            double particleY = @event.Y + BoundingBox.MaxY;
            double particleZ = @event.Z + Random.Shared.NextSingle();
            @event.World.Broadcaster.AddParticle("lava", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
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

        return flowVec is { X: 0.0D, Z: 0.0D } ? -1000.0D : Math.Atan2(flowVec.Z, flowVec.X) - Math.PI * 0.5D;
    }

    public override void onPlaced(OnPlacedEvent @event)
    {
        checkBlockCollisions(@event.World.Reader, @event.World.Writer, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        checkBlockCollisions(@event.World.Reader, @event.World.Writer, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);
    }

    private void checkBlockCollisions(IBlockReader reader, IBlockWriter writer, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (reader.GetBlockId(x, y, z) != id) return;

        if (material != Material.Lava) return;

        bool hasWaterAdjacent =
            reader.GetMaterial(x, y, z - 1) == Material.Water ||
            reader.GetMaterial(x, y, z + 1) == Material.Water ||
            reader.GetMaterial(x - 1, y, z) == Material.Water ||
            reader.GetMaterial(x + 1, y, z) == Material.Water ||
            reader.GetMaterial(x, y + 1, z) == Material.Water;

        if (!hasWaterAdjacent) return;

        int meta = reader.GetBlockMeta(x, y, z);
        switch (meta)
        {
            case 0:
                writer.SetBlock(x, y, z, Obsidian.id);
                break;
            default:
                writer.SetBlock(x, y, z, Cobblestone.id);
                break;
        }

        fizz(broadcaster, x, y, z);
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
