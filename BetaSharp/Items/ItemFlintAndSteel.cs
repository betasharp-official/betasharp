using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemFlintAndSteel : Item
{
    public ItemFlintAndSteel(int id) : base(id)
    {
        MaxCount = 1;
        SetMaxDamage(64);
    }

    public override bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        switch (meta)
        {
            case 0:
                --y;
                break;
            case 1:
                ++y;
                break;
            case 2:
                --z;
                break;
            case 3:
                ++z;
                break;
            case 4:
                --x;
                break;
            case 5:
                ++x;
                break;
        }

        int blockId = world.Reader.GetBlockId(x, y, z);
        if (blockId == 0)
        {
            world.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "fire.ignite", 1.0F, itemRand.NextFloat() * 0.4F + 0.8F);
            world.Writer.SetBlock(x, y, z, Block.Fire.id);
        }

        itemStack.DamageItem(1, entityPlayer);
        return true;
    }
}
