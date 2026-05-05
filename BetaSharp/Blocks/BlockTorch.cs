using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockTorch : Block
{
    private const float TorchWidth = 0.15F;
    private const float TorchWidthGround = 0.1F;

    public BlockTorch(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => setTickRandomly(true);

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Torch;

    private static bool canPlaceOn(IBlockReader world, int x, int y, int z) => world.ShouldSuffocate(x, y, z) || world.GetBlockId(x, y, z) == Fence.id;

    private static bool TryGetHorizontalWallPickRay(EntityLiving placer, int torchX, int torchZ, out double lx, out double lz)
    {
        Vec3D look = placer.GetLook(1.0F);
        double h = Math.Sqrt((look.x * look.x) + (look.z * look.z));
        if (h >= 1e-3)
        {
            lx = look.x / h;
            lz = look.z / h;
            return true;
        }

        double vx = placer.X - (torchX + 0.5);
        double vz = placer.Z - (torchZ + 0.5);
        h = Math.Sqrt((vx * vx) + (vz * vz));
        if (h >= 1e-4)
        {
            lx = vx / h;
            lz = vz / h;
            return true;
        }

        lx = 0.0;
        lz = 0.0;
        return false;
    }

    private static int ResolveTorchMetaVanillaOrder(IBlockReader reader, int x, int y, int z)
    {
        if (reader.ShouldSuffocate(x - 1, y, z)) return 1;
        if (reader.ShouldSuffocate(x + 1, y, z)) return 2;
        if (reader.ShouldSuffocate(x, y, z - 1)) return 3;
        if (reader.ShouldSuffocate(x, y, z + 1)) return 4;
        if (canPlaceOn(reader, x, y - 1, z)) return 5;
        return -1;
    }

    private static int? TryResolveTorchMetaForDownPlacement(IBlockReader reader, int x, int y, int z, EntityLiving? placer)
    {
        bool ceiling = reader.ShouldSuffocate(x, y + 1, z);
        bool west = reader.ShouldSuffocate(x - 1, y, z);
        bool east = reader.ShouldSuffocate(x + 1, y, z);
        bool north = reader.ShouldSuffocate(x, y, z - 1);
        bool south = reader.ShouldSuffocate(x, y, z + 1);
        int wallCount = (west ? 1 : 0) + (east ? 1 : 0) + (north ? 1 : 0) + (south ? 1 : 0);
        if (ceiling && wallCount >= 2 && placer is not null)
        {
            if (!TryGetHorizontalWallPickRay(placer, x, z, out double lx, out double lz))
            {
                int v = ResolveTorchMetaVanillaOrder(reader, x, y, z);
                return v == -1 ? null : v;
            }

            const double tieEps = 1e-4;
            double westScore = west ? (lx * -1.0) + (lz * 0.0) : double.NegativeInfinity;
            double eastScore = east ? (lx * 1.0) + (lz * 0.0) : double.NegativeInfinity;
            double northScore = north ? (lx * 0.0) + (lz * -1.0) : double.NegativeInfinity;
            double southScore = south ? (lx * 0.0) + (lz * 1.0) : double.NegativeInfinity;
            double maxD = Math.Max(Math.Max(westScore, eastScore), Math.Max(northScore, southScore));

            for (int meta = 1; meta <= 4; meta++)
            {
                double d = meta switch
                {
                    1 => westScore,
                    2 => eastScore,
                    3 => northScore,
                    4 => southScore,
                    _ => double.NegativeInfinity
                };
                bool solid = meta switch
                {
                    1 => west,
                    2 => east,
                    3 => north,
                    4 => south,
                    _ => false
                };
                if (solid && Math.Abs(d - maxD) < tieEps)
                {
                    return meta;
                }
            }
        }

        int vanilla = ResolveTorchMetaVanillaOrder(reader, x, y, z);
        return vanilla == -1 ? null : vanilla;
    }

    public override bool canPlaceAt(CanPlaceAtContext context) =>
        context.World.Reader.ShouldSuffocate(context.X - 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X + 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z - 1) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z + 1) ||
        canPlaceOn(context.World.Reader, context.X, context.Y - 1, context.Z);

    public override void onPlaced(OnPlacedEvent @event)
    {
        IBlockReader reader = @event.World.Reader;
        int meta = reader.GetBlockMeta(@event.X, @event.Y, @event.Z);

        switch (@event.Direction)
        {
            case Side.Up when canPlaceOn(reader, @event.X, @event.Y - 1, @event.Z):
                meta = 5;
                break;
            case Side.North when reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1):
                meta = 4;
                break;
            case Side.South when reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1):
                meta = 3;
                break;
            case Side.West when reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z):
                meta = 2;
                break;
            case Side.East when reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z):
                meta = 1;
                break;
            case Side.Down:
                {
                    int? resolved = TryResolveTorchMetaForDownPlacement(reader, @event.X, @event.Y, @event.Z, @event.Placer);
                    if (resolved.HasValue)
                    {
                        meta = resolved.Value;
                    }

                    break;
                }
        }

        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
    }

    public override void onTick(OnTickEvent @event)
    {
        base.onTick(@event);
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0)
        {
            onPlaced(@event);
        }
    }

    private void onPlaced(OnTickEvent @event)
    {
        int resolved = ResolveTorchMetaVanillaOrder(@event.World.Reader, @event.X, @event.Y, @event.Z);
        if (resolved != -1)
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, resolved);
        }

        breakIfCannotPlaceAt(@event, @event.X, @event.Y, @event.Z);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!breakIfCannotPlaceAt(@event, @event.X, @event.Y, @event.Z)) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        bool shouldDrop = !@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z) && meta == 1 ||
                          !@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z) && meta == 2 ||
                          !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1) && meta == 3 ||
                          !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1) && meta == 4 ||
                          !canPlaceOn(@event.World.Reader, @event.X, @event.Y - 1, @event.Z) && meta == 5;

        if (!shouldDrop) return;
        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    private bool breakIfCannotPlaceAt(OnTickEvent @event, int x, int y, int z)
    {
        if (canPlaceAt(new CanPlaceAtContext(@event.World, 0, x, y, z))) return true;

        dropStacks(new OnDropEvent(@event.World, x, y, z, @event.World.Reader.GetBlockMeta(x, y, z)));
        @event.World.Writer.SetBlock(x, y, z, 0);
        return false;
    }

    public override HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        int rotation = world.GetBlockMeta(x, y, z) & 7;
        switch (rotation)
        {
            case 1:
                setBoundingBox(0.0F, 0.2F, 0.5F - TorchWidth, TorchWidth * 2.0F, 0.8F, 0.5F + TorchWidth);
                break;
            case 2:
                setBoundingBox(1.0F - TorchWidth * 2.0F, 0.2F, 0.5F - TorchWidth, 1.0F, 0.8F, 0.5F + TorchWidth);
                break;
            case 3:
                setBoundingBox(0.5F - TorchWidth, 0.2F, 0.0F, 0.5F + TorchWidth, 0.8F, TorchWidth * 2.0F);
                break;
            case 4:
                setBoundingBox(0.5F - TorchWidth, 0.2F, 1.0F - TorchWidth * 2.0F, 0.5F + TorchWidth, 0.8F, 1.0F);
                break;
            default:
                setBoundingBox(0.5F - TorchWidthGround, 0.0F, 0.5F - TorchWidthGround, 0.5F + TorchWidthGround, 0.6F, 0.5F + TorchWidthGround);
                break;
        }

        return base.raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void randomDisplayTick(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        float flameX = @event.X + 0.5F;
        float flameY = @event.Y + 0.7F;
        float flameZ = @event.Z + 0.5F;
        const float yOffset = 0.22F;
        const float xOffset = 0.27F;

        switch (meta)
        {
            case 1:
                @event.World.Broadcaster.AddParticle("smoke", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
                break;
            case 2:
                @event.World.Broadcaster.AddParticle("smoke", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
                break;
            case 3:
                @event.World.Broadcaster.AddParticle("smoke", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
                break;
            case 4:
                @event.World.Broadcaster.AddParticle("smoke", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
                break;
            default:
                @event.World.Broadcaster.AddParticle("smoke", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
                @event.World.Broadcaster.AddParticle("flame", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
                break;
        }
    }
}
