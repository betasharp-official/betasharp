using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class ItemRecord : Item
{

    public readonly string recordName;

    public ItemRecord(int id, string recordName) : base(id)
    {
        this.recordName = recordName;
        maxCount = 1;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (world.Reader.GetBlockId(x, y, z) == Block.Jukebox.ID && world.Reader.GetBlockMeta(x, y, z) == 0)
        {
            if (world.IsRemote)
            {
                return true;
            }
            else
            {
                BlockJukeBox.InsertRecord(world, x, y, z, id);
                world.Broadcaster.WorldEvent(1005, x, y, z, id);
                itemStack.ConsumeItem(entityPlayer);
                return true;
            }
        }
        else
        {
            return false;
        }
    }
}
