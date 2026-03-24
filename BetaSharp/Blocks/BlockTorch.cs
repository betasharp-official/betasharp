using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockTorch : Block
{
    public BlockTorch(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => SetTickRandomly(true);

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Torch;

    private bool canPlaceOn(IBlockReader world, int x, int y, int z) => world.ShouldSuffocate(x, y, z) || world.GetBlockId(x, y, z) == Fence.Id;

    public override bool CanPlaceAt(CanPlaceAtContext context) =>
        context.World.Reader.ShouldSuffocate(context.X - 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X + 1, context.Y, context.Z) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z - 1) ||
        context.World.Reader.ShouldSuffocate(context.X, context.Y, context.Z + 1) ||
        canPlaceOn(context.World.Reader, context.X, context.Y - 1, context.Z);

    public override void OnPlaced(OnPlacedEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (@event.Direction == Side.Up && canPlaceOn(@event.World.Reader, @event.X, @event.Y - 1, @event.Z))
        {
            meta = 5;
        }

        if (@event.Direction == Side.North && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
        {
            meta = 4;
        }

        if (@event.Direction == Side.South && @event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
        {
            meta = 3;
        }

        if (@event.Direction == Side.West && @event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
        {
            meta = 2;
        }

        if (@event.Direction == Side.East && @event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
        {
            meta = 1;
        }

        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
    }

    public override void OnTick(OnTickEvent @event)
    {
        base.OnTick(@event);
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0)
        {
            onPlaced(@event);
        }
    }

    private void onPlaced(OnTickEvent @event)
    {
        if (@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z))
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 1);
        }
        else if (@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z))
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 2);
        }
        else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1))
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 3);
        }
        else if (@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1))
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 4);
        }
        else if (canPlaceOn(@event.World.Reader, @event.X, @event.Y - 1, @event.Z))
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 5);
        }

        breakIfCannotPlaceAt(@event, @event.X, @event.Y, @event.Z);
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!breakIfCannotPlaceAt(@event, @event.X, @event.Y, @event.Z))
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        bool shouldDrop = !@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z) && meta == 1 ||
            !@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z) && meta == 2 ||
            !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1) && meta == 3 ||
            !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1) && meta == 4 ||
            !canPlaceOn(@event.World.Reader, @event.X, @event.Y - 1, @event.Z) && meta == 5;

        if (!shouldDrop) return;

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    private bool breakIfCannotPlaceAt(OnTickEvent @event, int x, int y, int z)
    {
        if (CanPlaceAt(new CanPlaceAtContext(@event.World, 0, x, y, z))) return true;

        DropStacks(new OnDropEvent(@event.World, x, y, z, @event.World.Reader.GetBlockMeta(x, y, z)));
        @event.World.Writer.SetBlock(x, y, z, 0);
        return false;
    }

    public override HitResult Raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        int meta = world.GetBlockMeta(x, y, z) & 7;
        float torchWidth = 0.15F;
        switch (meta)
        {
            case 1:
                SetBoundingBox(0.0F, 0.2F, 0.5F - torchWidth, torchWidth * 2.0F, 0.8F, 0.5F + torchWidth);
                break;
            case 2:
                SetBoundingBox(1.0F - torchWidth * 2.0F, 0.2F, 0.5F - torchWidth, 1.0F, 0.8F, 0.5F + torchWidth);
                break;
            case 3:
                SetBoundingBox(0.5F - torchWidth, 0.2F, 0.0F, 0.5F + torchWidth, 0.8F, torchWidth * 2.0F);
                break;
            case 4:
                SetBoundingBox(0.5F - torchWidth, 0.2F, 1.0F - torchWidth * 2.0F, 0.5F + torchWidth, 0.8F, 1.0F);
                break;
            default:
                torchWidth = 0.1F;
                SetBoundingBox(0.5F - torchWidth, 0.0F, 0.5F - torchWidth, 0.5F + torchWidth, 0.6F, 0.5F + torchWidth);
                break;
        }

        return base.Raycast(world, entities, x, y, z, startPos, endPos);
    }

    public override void RandomDisplayTick(OnTickEvent @event)
    {
        const float yOffset = 0.22F;
        const float xOffset = 0.27F;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        float flameX = @event.X + 0.5F;
        float flameY = @event.Y + 0.7F;
        float flameZ = @event.Z + 0.5F;

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
