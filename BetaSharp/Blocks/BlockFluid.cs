using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;
using Silk.NET.Maths;

namespace BetaSharp.Blocks;

public abstract class BlockFluid : Block
{
    protected BlockFluid(int id, Material material) : base(id, (material == Material.Lava ? 14 : 12) * 16 + 13, material)
    {
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        SetTickRandomly(true);
    }

    public override int GetColorMultiplier(IBlockReader iBlockReader, int x, int y, int z) => 0xFFFFFF;

    public static float GetFluidHeightFromMeta(int meta)
    {
        if (meta >= 8)
        {
            meta = 0;
        }

        float height = (meta + 1) / 9.0F;
        return height;
    }

    public override int GetTexture(Side side) => side != 0 && side != Side.Up ? TextureId + 1 : TextureId;

    protected int GetLiquidState(IBlockReader reader, int x, int y, int z) => reader.GetMaterial(x, y, z) != Material ? -1 : reader.GetBlockMeta(x, y, z);

    private int GetLiquidDepth(IBlockReader iBlockReader, int x, int y, int z)
    {
        if (iBlockReader.GetMaterial(x, y, z) != Material)
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

    public override bool IsFullCube() => false;

    public override bool IsOpaque() => false;

    public override bool HasCollision(int meta, bool allowLiquids) => allowLiquids && meta == 0;

    public override bool IsSolidFace(IBlockReader iBlockReader, int x, int y, int z, int face)
    {
        Material mat = iBlockReader.GetMaterial(x, y, z);
        return mat != Material && mat != Material.Ice && (face == 1 || base.IsSolidFace(iBlockReader, x, y, z, face));
    }

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        Material mat = iBlockReader.GetMaterial(x, y, z);
        return mat != Material && mat != Material.Ice && (side == Side.Up || base.IsSideVisible(iBlockReader, x, y, z, side));
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override BlockRendererType GetRenderType() => BlockRendererType.Fluids;

    public override int GetDroppedItemId(int blockMeta) => 0;

    public override int GetDroppedItemCount() => 0;

    private Vector3D<double> GetFlow(IBlockReader iBlockReader, int x, int y, int z)
    {
        Vector3D<double> flowVector = new(0.0);
        int depth = GetLiquidDepth(iBlockReader, x, y, z);

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

            int neighborDepth = GetLiquidDepth(iBlockReader, neighborX, y, neighborZ);
            int depthDiff;
            if (neighborDepth < 0)
            {
                if (iBlockReader.GetMaterial(neighborX, y, neighborZ).BlocksMovement)
                {
                    continue;
                }

                neighborDepth = GetLiquidDepth(iBlockReader, neighborX, y - 1, neighborZ);
                if (neighborDepth < 0)
                {
                    continue;
                }

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
                IsSolidFace(iBlockReader, x, y, z - 1, 2) ||
                IsSolidFace(iBlockReader, x, y, z + 1, 3) ||
                IsSolidFace(iBlockReader, x - 1, y, z, 4) ||
                IsSolidFace(iBlockReader, x + 1, y, z, 5) ||
                IsSolidFace(iBlockReader, x, y + 1, z - 1, 2) ||
                IsSolidFace(iBlockReader, x, y + 1, z + 1, 3) ||
                IsSolidFace(iBlockReader, x - 1, y + 1, z, 4) ||
                IsSolidFace(iBlockReader, x + 1, y + 1, z, 5);

            if (hasAdjacentSolid)
            {
                flowVector = Normalize(flowVector) + new Vector3D<double>(0.0, -0.6, 0.0);
            }
        }

        flowVector = Normalize(flowVector);
        return flowVector;
    }

    public override Vec3D ApplyVelocity(OnApplyVelocityEvent @event)
    {
        Vector3D<double> flowVec = GetFlow(@event.World.Reader, @event.X, @event.Y, @event.Z);
        return new Vec3D(flowVec.X, flowVec.Y, flowVec.Z);
    }

    public override int GetTickRate() => Material == Material.Water ? 5 : Material == Material.Lava ? 30 : 0;

    public override float GetLuminance(ILightProvider lighting, int x, int y, int z)
    {
        float luminance = lighting.GetLuminance(x, y, z);
        float luminanceAbove = lighting.GetLuminance(x, y + 1, z);
        return luminance > luminanceAbove ? luminance : luminanceAbove;
    }

    public override void OnTick(OnTickEvent @event) => base.OnTick(@event);

    public override int GetRenderLayer() => Material == Material.Water ? 1 : 0;

    public override void RandomDisplayTick(OnTickEvent @event)
    {
        if (Material == Material.Water && Random.Shared.Next(64) == 0)
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

        if (Material != Material.Lava ||
            @event.World.Reader.GetMaterial(@event.X, @event.Y + 1, @event.Z) != Material.Air ||
            @event.World.Reader.IsOpaque(@event.X, @event.Y + 1, @event.Z) || Random.Shared.Next(100) != 0)
        {
            return;
        }

        double particleX = @event.X + Random.Shared.NextSingle();
        double particleY = @event.Y + BoundingBox.MaxY;
        double particleZ = @event.Z + Random.Shared.NextSingle();
        @event.World.Broadcaster.AddParticle("lava", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
    }

    public static double GetFlowingAngle(IBlockReader iBlockReader, int x, int y, int z, Material material)
    {
        Vector3D<double> flowVec = new(0.0);
        if (material == Material.Water)
        {
            flowVec = ((BlockFluid)FlowingWater).GetFlow(iBlockReader, x, y, z);
        }
        else if (material == Material.Lava)
        {
            flowVec = ((BlockFluid)FlowingLava).GetFlow(iBlockReader, x, y, z);
        }

        return flowVec is { X: 0.0D, Z: 0.0D } ? -1000.0D : Math.Atan2(flowVec.Z, flowVec.X) - Math.PI * 0.5D;
    }

    public override void OnPlaced(OnPlacedEvent @event) => CheckBlockCollisions(@event.World.Reader, @event.World.Writer, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);

    public override void NeighborUpdate(OnTickEvent @event) => CheckBlockCollisions(@event.World.Reader, @event.World.Writer, @event.World.Broadcaster, @event.X, @event.Y, @event.Z);

    private void CheckBlockCollisions(IBlockReader reader, IBlockWriter writer, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        if (reader.GetBlockId(x, y, z) != ID)
        {
            return;
        }

        if (Material != Material.Lava)
        {
            return;
        }

        bool hasWaterAdjacent =
            reader.GetMaterial(x, y, z - 1) == Material.Water ||
            reader.GetMaterial(x, y, z + 1) == Material.Water ||
            reader.GetMaterial(x - 1, y, z) == Material.Water ||
            reader.GetMaterial(x + 1, y, z) == Material.Water ||
            reader.GetMaterial(x, y + 1, z) == Material.Water;

        if (!hasWaterAdjacent)
        {
            return;
        }

        int meta = reader.GetBlockMeta(x, y, z);
        switch (meta)
        {
            case 0:
                writer.SetBlock(x, y, z, Obsidian.ID);
                break;
            default:
                writer.SetBlock(x, y, z, Cobblestone.ID);
                break;
        }

        Fizz(broadcaster, x, y, z);
    }

    protected static void Fizz(WorldEventBroadcaster broadcaster, int x, int y, int z)
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
