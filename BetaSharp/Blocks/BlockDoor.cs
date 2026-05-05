using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
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

    public BlockDoor(int id, Material material) : base(id, material)
    {
        TextureId = BlockTextures.DoorWood;
        if (material == Material.Metal) TextureId = BlockTextures.DoorIron;
        setBoundingBox(0.5F - HalfWidth, 0.0F, 0.5F - HalfWidth, 0.5F + HalfWidth, Height, 0.5F + HalfWidth);
    }

    public override int GetTexture(Side side, int meta)
    {
        if (side is Side.Up or Side.Down)
        {
            return TextureId;
        }

        int facing = SetOpen(meta);
        if (facing is 0 or 2 ^ (side <= Side.South))
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

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Door;

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, entities, x, y, z);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getCollisionShape(world, entities, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => Rotate(SetOpen(blockReader.GetBlockMeta(x, y, z)));

    public void Rotate(int meta)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F, 1.0F);
        switch (meta)
        {
            case 0:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, Thickness);
                break;
            case 1:
                setBoundingBox(1.0F - Thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 2:
                setBoundingBox(0.0F, 0.0F, 1.0F - Thickness, 1.0F, 1.0F, 1.0F);
                break;
            case 3:
                setBoundingBox(0.0F, 0.0F, 0.0F, Thickness, 1.0F, 1.0F);
                break;
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event) => updateDorState(@event.Player, @event.World, @event.X, @event.Y, @event.Z);

    private bool updateDorState(EntityPlayer player, IWorldContext world, int x, int y, int z)
    {
        if (world.IsRemote)
        {
            return true;
        }

        if (material == Material.Metal)
        {
            return true;
        }

        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == id)
            {
                updateDorState(player, world, x, y - 1, z);
            }

            return true;
        }

        if (world.Reader.GetBlockId(x, y + 1, z) == id)
        {
            world.Writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
        }

        world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
        world.Broadcaster.WorldEvent(player, 1003, x, y, z, 0);
        return true;
    }


    public override bool onUse(OnUseEvent @event) => updateDorState(@event.Player, @event.World, @event.X, @event.Y, @event.Z);
    public static int SetOpen(int meta) => (meta & 4) == 0 ? (meta - 1) & 3 : meta & 3;

    public void SetOpen(IWorldContext world, int x, int y, int z, bool open)
    {
        if (world.IsRemote) return;

        int meta = world.Reader.GetBlockMeta(x, y, z);

        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == id)
            {
                y -= 1;
                meta = world.Reader.GetBlockMeta(x, y, z);
            }
            else
            {
                return;
            }
        }

        if (isOpen(meta) == open) return;

        if (world.Reader.GetBlockId(x, y + 1, z) == id)
        {
            world.Writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
        }

        world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
        world.Broadcaster.WorldEvent(1003, x, y, z, 0);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);

        if ((meta & 8) != 0)
        {
            if (@event.World.Reader.GetBlockId(@event.X, @event.Y - 1, @event.Z) != id)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }
            else if (@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower())
            {
                int bottomMeta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y - 1, @event.Z);
                neighborUpdate(new OnTickEvent(@event.World, @event.X, @event.Y - 1, @event.Z, bottomMeta, @event.BlockId));
            }
        }
        else
        {
            bool wasBroken = false;

            if (@event.World.Reader.GetBlockId(@event.X, @event.Y + 1, @event.Z) != id)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                wasBroken = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z))
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                wasBroken = true;
                if (@event.World.Reader.GetBlockId(@event.X, @event.Y + 1, @event.Z) == id)
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, 0);
                }
            }

            if (wasBroken)
            {
                if (!@event.World.IsRemote)
                {
                    dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, meta));
                }
            }
            else if (@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower())
            {
                bool isPowered = @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) ||
                                 @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z);

                SetOpen(@event.World, @event.X, @event.Y, @event.Z, isPowered);
            }
        }
    }

    public override int getDroppedItemId(int blockMeta) => (blockMeta & 8) != 0 ? 0 : material == Material.Metal ? Item.IronDoor.id : Item.WoodenDoor.id;

    public override HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, entities, x, y, z);
        return base.raycast(world, entities, x, y, z, startPos, endPos);
    }


    public override bool canPlaceAt(CanPlaceAtContext evt) => evt.Y < 127 &&
                                                              evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) &&
                                                              base.canPlaceAt(evt);

    public static bool isOpen(int meta) => (meta & 4) != 0;

    public override int getPistonBehavior() => 1;
}
