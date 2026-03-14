using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemDye : Item
{

    public static readonly String[] DyeColorNames =
    [
        "black",
        "red",
        "green",
        "brown",
        "blue",
        "purple",
        "cyan",
        "silver",
        "gray",
        "pink",
        "lime",
        "yellow",
        "lightBlue",
        "magenta",
        "orange",
        "white",
    ];
    public static readonly int[] DyeColorValues =
    [
        0x1E1B1B,
        0xB3312C,
        0x3B511A,
        0x51301A,
        0x253192,
        0x7B2FBE,
        0x287697,
        0x287697,
        0x434343,
        0xD88198,
        0x41CD34,
        0xDECF2A,
        0x6689D3,
        0xC354CD,
        0xEB8844,
        0xF0F0F0,
    ];

    public ItemDye(int id) : base(id)
    {
        setHasSubtypes(true);
        setMaxDamage(0);
    }

    public override int getTextureId(int meta)
    {
        return textureId + meta % 8 * 16 + meta / 8;
    }

    public override String getItemNameIS(ItemStack itemStack)
    {
        return base.getItemName() + "." + DyeColorNames[itemStack.getDamage()];
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (itemStack.getDamage() == 15)
        {
            int blockId = world.Reader.GetBlockId(x, y, z);
            if (blockId == Block.Sapling.id)
            {
                if (!world.IsRemote)
                {
                    ((BlockSapling)Block.Sapling).generate(world, x, y, z);
                    --itemStack.count;
                }
                return true;
            }
            if (blockId == Block.Wheat.id)
            {
                if (!world.IsRemote)
                {
                    ((BlockCrops)Block.Wheat).applyFullGrowth(world, x, y, z);
                    --itemStack.count;
                }
                return true;
            }
            if (blockId == Block.GrassBlock.id)
            {
                if (!world.IsRemote)
                {
                    --itemStack.count;

                    for (int attempt = 0; attempt < 128; ++attempt)
                    {
                        int spawnX = x;
                        int spawnY = y + 1;
                        int spawnZ = z;

                        bool validPosition = true;
                        for (int walkStep = 0; walkStep < attempt / 16 && validPosition; ++walkStep)
                        {
                            spawnX += itemRand.NextInt(3) - 1;
                            spawnY += (itemRand.NextInt(3) - 1) * itemRand.NextInt(3) / 2;
                            spawnZ += itemRand.NextInt(3) - 1;
                            if (world.Reader.GetBlockId(spawnX, spawnY - 1, spawnZ) != Block.GrassBlock.id || world.Reader.ShouldSuffocate(spawnX, spawnY, spawnZ))
                            {
                                validPosition = false;
                            }
                        }

                        if (validPosition && world.Reader.GetBlockId(spawnX, spawnY, spawnZ) == 0)
                        {
                            if (itemRand.NextInt(10) != 0)
                            {
                                world.Writer.SetBlock(spawnX, spawnY, spawnZ, Block.Grass.id, 1);
                            }
                            else if (itemRand.NextInt(3) != 0)
                            {
                                world.Writer.SetBlock(spawnX, spawnY, spawnZ, Block.Dandelion.id);
                            }
                            else
                            {
                                world.Writer.SetBlock(spawnX, spawnY, spawnZ, Block.Rose.id);
                            }
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }

    public override void useOnEntity(ItemStack itemStack, EntityLiving entityLiving)
    {
        if (entityLiving is EntitySheep)
        {
            EntitySheep sheep = (EntitySheep)entityLiving;
            int woolColor = BlockCloth.getBlockMeta(itemStack.getDamage());
            if (!sheep.getSheared() && sheep.getFleeceColor() != woolColor)
            {
                sheep.setFleeceColor(woolColor);
                --itemStack.count;
            }
        }

    }
}
