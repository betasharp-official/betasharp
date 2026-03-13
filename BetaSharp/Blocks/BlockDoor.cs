using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDoor : Block
{
    public BlockDoor(int id, Material material) : base(id, material)
    {
        textureId = 97;
        if (material == Material.Metal)
        {
            ++textureId;
        }

        float halfWidth = 0.5F;
        float height = 1.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, height, 0.5F + halfWidth);
    }

    public override int getTexture(int side, int meta)
    {
        if (side != 0 && side != 1)
        {
            int facing = setOpen(meta);
            if ((facing == 0 || facing == 2) ^ (side <= 3))
            {
                return this.textureId;
            }

            int textureIndex = facing / 2 + ((side & 1) ^ facing);
            textureIndex += (meta & 4) / 4;
            int textureId = this.textureId - (meta & 8) * 2;
            if ((textureIndex & 1) != 0)
            {
                textureId = -textureId;
            }

            return textureId;
        }

        return textureId;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Door;
    }

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, x, y, z);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getCollisionShape(world, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z) => rotate(setOpen(iBlockReader.GetBlockMeta(x, y, z)));

    public void rotate(int meta)
    {
        float thickness = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F, 1.0F);
        if (meta == 0)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 1)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvt evt) => updateDorState(evt.Level, evt.X, evt.Y, evt.Z);

    private bool updateDorState(IWorldContext world, int x, int y, int z)
    {
        if (material == Material.Metal)
        {
            return true;
        }

        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == id)
            {
                updateDorState(world, x, y - 1, z);
            }

            return true;
        }

        if (world.Reader.GetBlockId(x, y + 1, z) == id)
        {
            world.BlockWriter.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
        }

        world.BlockWriter.SetBlockMeta(x, y, z, meta ^ 4);
        world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
        world.Broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }


    public override bool onUse(OnUseEvt evt) => updateDorState(evt.Level, evt.X, evt.Y, evt.Z);

    public void setOpen(IWorldContext world, int x, int y, int z, bool open)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == id)
            {
                setOpen(world, x, y - 1, z, open);
            }
        }
        else
        {
            bool isOpen = (world.Reader.GetBlockMeta(x, y, z) & 4) > 0;
            if (isOpen != open)
            {
                if (world.Reader.GetBlockId(x, y + 1, z) == id)
                {
                    world.BlockWriter.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
                }

                world.BlockWriter.SetBlockMeta(x, y, z, meta ^ 4);
                world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
                world.Broadcaster.WorldEvent(1003, x, y, z, 0);
            }
        }
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        if ((meta & 8) != 0)
        {
            if (evt.Level.Reader.GetBlockId(evt.X, evt.Y - 1, evt.Z) != id)
            {
                evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
            }

            if (evt.BlockId > 0 && Blocks[evt.BlockId].canEmitRedstonePower())
            {
                neighborUpdate(evt);
            }
        }
        else
        {
            bool wasBroken = false;
            if (evt.Level.Reader.GetBlockId(evt.X, evt.Y + 1, evt.Z) != id)
            {
                evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
                wasBroken = true;
            }

            if (!evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z))
            {
                evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
                wasBroken = true;
                if (evt.Level.Reader.GetBlockId(evt.X, evt.Y + 1, evt.Z) == id)
                {
                    evt.Level.BlockWriter.SetBlock(evt.X, evt.Y + 1, evt.Z, 0);
                }
            }

            if (wasBroken)
            {
                if (!evt.Level.IsRemote)
                {
                    dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, meta));
                }
            }
            else if (evt.BlockId > 0 && Blocks[evt.BlockId].canEmitRedstonePower())
            {
                bool isPowered = evt.Level.Redstone.IsPowered(evt.X, evt.Y, evt.Z) || evt.Level.Redstone.IsPowered(evt.X, evt.Y + 1, evt.Z);
                setOpen(evt.Level, evt.X, evt.Y, evt.Z, isPowered);
            }
        }
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return (blockMeta & 8) != 0 ? 0 : material == Material.Metal ? Item.IronDoor.id : Item.WoodenDoor.id;
    }

    public override HitResult raycast(IBlockReader world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public int setOpen(int meta)
    {
        return (meta & 4) == 0 ? (meta - 1) & 3 : meta & 3;
    }

    public override bool canPlaceAt(CanPlaceAtCtx evt)
    {
        return evt.Y >= 127 ? false : evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) && base.canPlaceAt(evt) && base.canPlaceAt(evt);
    }

    public static bool isOpen(int meta)
    {
        return (meta & 4) != 0;
    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
