using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDoor : Block
{
    private const float HalfWidth = 0.5F;
    private const float Height = 1.0F;
    private const float Thickness = 3.0F / 16.0F;
    public override bool IsOpaque => false;
    public override PistonBehavior PistonBehavior => PistonBehavior.Destroy;
    public override bool IsFullCube => false;

    public BlockDoor(int id, Material material) : base(id, material)
    {
        TextureId = 97;
        if (material == Material.Metal)
        {
            ++TextureId;
        }

        SetBoundingBox(0.5F - HalfWidth, 0.0F, 0.5F - HalfWidth, 0.5F + HalfWidth, Height, 0.5F + HalfWidth);
    }


    public override int GetTexture(Side side, int meta)
    {
        if (side is Side.Down or Side.Up) return TextureId;

        int facing = SetOpen(meta);
        if (facing is 0 or 2 ^ (side.ToInt() <= 3))
        {
            return TextureId;
        }

        int textureIndex = facing / 2 + ((side.ToInt() & 1) ^ facing);
        textureIndex += (meta & 4) / 4;
        int texture = TextureId - (meta & 8) * 2;
        if ((textureIndex & 1) != 0)
        {
            texture = -texture;
        }

        return texture;
    }


    public override BlockRendererType GetRenderType() => BlockRendererType.Door;

    public override Box GetBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        UpdateBoundingBox(world, x, y, z);
        return base.GetBoundingBox(world, entities, x, y, z);
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        UpdateBoundingBox(world, x, y, z);
        return base.GetCollisionShape(world, entities, x, y, z);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => Rotate(SetOpen(blockReader.GetBlockMeta(x, y, z)));

    private void Rotate(int meta)
    {
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F, 1.0F);
        switch (meta)
        {
            case 0:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, Thickness);
                break;
            case 1:
                SetBoundingBox(1.0F - Thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 2:
                SetBoundingBox(0.0F, 0.0F, 1.0F - Thickness, 1.0F, 1.0F, 1.0F);
                break;
            case 3:
                SetBoundingBox(0.0F, 0.0F, 0.0F, Thickness, 1.0F, 1.0F);
                break;
        }
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent @event) => UpdateDorState(@event.World, @event.X, @event.Y, @event.Z);

    private bool UpdateDorState(IWorldContext world, int x, int y, int z)
    {
        if (Material == Material.Metal) return true;

        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == Id)
            {
                UpdateDorState(world, x, y - 1, z);
            }

            return true;
        }

        if (world.Reader.GetBlockId(x, y + 1, z) == Id)
        {
            world.Writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
        }

        world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
        world.Broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }


    public override bool OnUse(OnUseEvent @event) => UpdateDorState(@event.World, @event.X, @event.Y, @event.Z);

    private void SetOpen(IWorldContext world, int x, int y, int z, bool open)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == Id)
            {
                SetOpen(world, x, y - 1, z, open);
            }
        }
        else
        {
            bool isOpen = (world.Reader.GetBlockMeta(x, y, z) & 4) > 0;
            if (isOpen == open) return;

            if (world.Reader.GetBlockId(x, y + 1, z) == Id)
            {
                world.Writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
            }

            world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
            world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
            world.Broadcaster.WorldEvent(1003, x, y, z, 0);
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) != 0)
        {
            if (@event.World.Reader.GetBlockId(@event.X, @event.Y - 1, @event.Z) != Id)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }

            if (@event.BlockId > 0 && Blocks[@event.BlockId]!.CanEmitRedstonePower)
            {
                NeighborUpdate(@event);
            }
        }
        else
        {
            bool wasBroken = false;
            if (@event.World.Reader.GetBlockId(@event.X, @event.Y + 1, @event.Z) != Id)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                wasBroken = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z))
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                wasBroken = true;
                if (@event.World.Reader.GetBlockId(@event.X, @event.Y + 1, @event.Z) == Id)
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, 0);
                }
            }

            if (wasBroken)
            {
                if (!@event.World.IsRemote)
                {
                    DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, meta));
                }
            }
            else if (@event.BlockId > 0 && Blocks[@event.BlockId]!.CanEmitRedstonePower)
            {
                bool isPowered = @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) || @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z);
                SetOpen(@event.World, @event.X, @event.Y, @event.Z, isPowered);
            }
        }
    }

    public override int GetDroppedItemId(int blockMeta) => (blockMeta & 8) != 0 ? 0 : Material == Material.Metal ? Item.IronDoor.id : Item.WoodenDoor.id;

    public override HitResult Raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        UpdateBoundingBox(world, entities, x, y, z);
        return base.Raycast(world, entities, x, y, z, startPos, endPos);
    }

    private static int SetOpen(int meta) => (meta & 4) == 0 ? (meta - 1) & 3 : meta & 3;

    public override bool CanPlaceAt(CanPlaceAtContext evt) => evt.Y < 127 &&
                                                              evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) &&
                                                              base.CanPlaceAt(evt) &&
                                                              base.CanPlaceAt(evt);

    public static bool IsOpen(int meta) => (meta & 4) != 0;
}
