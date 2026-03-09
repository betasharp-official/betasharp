using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class ItemRecord : Item
{

    public readonly String recordName;

    public ItemRecord(int id, String recordName) : base(id)
    {
        this.recordName = recordName;
        maxCount = 1;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        if (world.getBlockId(x, y, z) == Block.Jukebox.id && world.getBlockMeta(x, y, z) == 0)
        {
            if (world.IsRemote)
            {
                return true;
            }
            else
            {
                ((BlockJukeBox)Block.Jukebox).insertRecord(world, x, y, z, id);
                world.worldEvent((EntityPlayer)null, 1005, x, y, z, id);
                --itemStack.count;
                return true;
            }
        }
        else
        {
            return false;
        }
    }
}
