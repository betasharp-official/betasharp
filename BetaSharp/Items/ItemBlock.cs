using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemBlock : Item
{

    private int blockID;

    public ItemBlock(int id) : base(id)
    {
        blockID = id + 256;
        setTextureId(Block.Blocks[id + 256].getTexture(2));
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IBlockWorldContext world, int x, int y, int z, int meta)
    {
        if (world.BlocksReader.GetBlockId(x, y, z) == Block.Snow.id)
        {
            meta = 0;
        }
        else
        {
            if (meta == 0)
            {
                --y;
            }

            if (meta == 1)
            {
                ++y;
            }

            if (meta == 2)
            {
                --z;
            }

            if (meta == 3)
            {
                ++z;
            }

            if (meta == 4)
            {
                --x;
            }

            if (meta == 5)
            {
                ++x;
            }
        }

        if (itemStack.count == 0)
        {
            return false;
        }

        // Prevent overwriting existing non-replaceable blocks (fixes Lever/Button overlap)
        int existingBlockId = world.BlocksReader.GetBlockId(x, y, z);
        if (existingBlockId != 0 && !Block.Blocks[existingBlockId].material.IsReplaceable)
        {
            return false;
        }

        if (y == 127 && Block.Blocks[blockID].material.IsSolid)
        {
            return false;
        }

        Block block = Block.Blocks[blockID];
        Box? collisionBox = block.getCollisionShape(world.BlocksReader, x, y, z);
        if (collisionBox is { } box)
        {
            List<Entity> entitiesInBox = world.Entities.CollectEntitiesOfType<Entity>(box);
            if (entitiesInBox.Count > 0)
            {
                return false;
            }
        }

        if (block.canPlaceAt(new CanPlaceAtCtx(world, 0, x, y, z)))
        {
            if (world.BlockWriter.SetBlock(x, y, z, blockID, getPlacementMetadata(itemStack.getDamage())))
            {
                Block.Blocks[blockID].onPlaced(new OnPlacedEvt(world, entityPlayer, meta, meta, x, y, z));
                world.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, block.soundGroup.StepSound, (block.soundGroup.Volume + 1.0F) / 2.0F, block.soundGroup.Pitch * 0.8F);
                --itemStack.count;
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public override String getItemNameIS(ItemStack itemStack)
    {
        return Block.Blocks[blockID].getBlockName();
    }

    public override String getItemName()
    {
        return Block.Blocks[blockID].getBlockName();
    }
}
