using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class ItemRecord : Item
{
    public readonly string RecordName;

    public ItemRecord(int id, string recordName) : base(id)
    {
        RecordName = recordName;
        MaxCount = 1;
    }

    public override bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (world.Reader.GetBlockId(x, y, z) != Block.Jukebox.id || world.Reader.GetBlockMeta(x, y, z) != 0)
        {
            return false;
        }

        if (world.IsRemote)
        {
            return true;
        }

        BlockJukeBox.insertRecord(world, x, y, z, Id);
        world.Broadcaster.WorldEvent(1005, x, y, z, Id);
        itemStack.ConsumeItem(entityPlayer);
        return true;

    }
}
