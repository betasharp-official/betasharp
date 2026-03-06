using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemHoe : Item
{

    public ItemHoe(int id, EnumToolMaterial enumToolMaterial) : base(id)
    {
        maxCount = 1;
        setMaxDamage(enumToolMaterial.getMaxUses());
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        int targetBlockId = world.getBlockId(x, y, z);
        int blockAbove = world.getBlockId(x, y + 1, z);
        if ((meta == 0 || blockAbove != 0 || targetBlockId != Block.GrassBlock.id) && targetBlockId != Block.Dirt.id)
        {
            return false;
        }
        else
        {
            Block block = Block.Farmland;
            world.playSound((double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F), block.soundGroup.StepSound, (block.soundGroup.Volume + 1.0F) / 2.0F, block.soundGroup.Pitch * 0.8F);
            if (world.IsRemote)
            {
                return true;
            }
            else
            {
                world.setBlock(x, y, z, block.id);
                itemStack.damageItem(1, entityPlayer);
                return true;
            }
        }
    }

    public override bool isHandheld()
    {
        return true;
    }
}
