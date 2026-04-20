using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemHoe : Item
{

    public ItemHoe(int id, EnumToolMaterial enumToolMaterial) : base(id)
    {
        maxCount = 1;
        setMaxDamage(enumToolMaterial.getMaxUses());
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        int targetBlockId = world.Reader.GetBlockId(x, y, z);
        int blockAbove = world.Reader.GetBlockId(x, y + 1, z);
        if ((meta == 0 || blockAbove != 0 || targetBlockId != Block.GrassBlock.ID) && targetBlockId != Block.Dirt.ID)
        {
            return false;
        }
        else
        {
            Block block = Block.Farmland;
            world.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, block.SoundGroup.StepSound, (block.SoundGroup.Volume + 1.0F) / 2.0F, block.SoundGroup.Pitch * 0.8F);
            if (world.IsRemote)
            {
                return true;
            }
            else
            {
                world.Writer.SetBlock(x, y, z, block.ID);
                itemStack.DamageItem(1, entityPlayer);
                return true;
            }
        }
    }

    public override bool isHandheld()
    {
        return true;
    }
}
