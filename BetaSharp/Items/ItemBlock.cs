using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemBlock : Item
{
    private int blockID;

    public ItemBlock(int id) : base(id)
    {
        blockID = id + 256;
        setTextureId(Block.Blocks[id + 256].getTexture(2));
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (world.Reader.GetBlockId(x, y, z) == Block.Snow.id)
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

        int existingBlockId = world.Reader.GetBlockId(x, y, z);
        if (existingBlockId != 0 && !Block.Blocks[existingBlockId].material.IsReplaceable)
        {
            return false;
        }

        if (y == 127 && Block.Blocks[blockID].material.IsSolid)
        {
            return false;
        }

        Block block = Block.Blocks[blockID];
        Box? collisionBox = block.getCollisionShape(world.Reader, world.Entities, x, y, z);
        if (collisionBox is { } box)
        {
            List<Entity> entitiesInBox = world.Entities.CollectEntitiesOfType<Entity>(box);
            bool hasBlockingEntity = entitiesInBox.Any(entity => entity.preventEntitySpawning);
            if (hasBlockingEntity)
            {
                return false;
            }
        }

        if (block.canPlaceAt(new CanPlaceAtContext(world, meta, x, y, z)))
        {
            int placementMeta = getPlacementMetadata(itemStack.getDamage());
            if (world.Writer.SetBlockWithoutCallingOnPlaced(x, y, z, blockID, placementMeta))
            {
                Block.Blocks[blockID].onPlaced(new OnPlacedEvent(world, entityPlayer, meta, meta, x, y, z));
                world.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, block.soundGroup.StepSound, (block.soundGroup.Volume + 1.0F) / 2.0F, block.soundGroup.Pitch * 0.8F);
                itemStack.ConsumeItem(entityPlayer);
            }

            return true;
        }

        return false;
    }

    public override string getItemNameIS(ItemStack itemStack)
    {
        return Block.Blocks[blockID].getBlockName();
    }

    public override string getItemName()
    {
        return Block.Blocks[blockID].getBlockName();
    }
}
