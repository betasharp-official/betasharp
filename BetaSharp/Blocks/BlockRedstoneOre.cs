using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockRedstoneOre : Block
{

    private bool lit;

    public BlockRedstoneOre(int id, int textureId, bool lit) : base(id, textureId, Material.Stone)
    {
        if (lit)
        {
            setTickRandomly(true);
        }

        this.lit = lit;
    }

    public override int getTickRate()
    {
        return 30;
    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        light(world, x, y, z);
        base.onBlockBreakStart(world, x, y, z, player);
    }

    public override void onSteppedOn(World world, int x, int y, int z, Entity entity)
    {
        light(world, x, y, z);
        base.onSteppedOn(world, x, y, z, entity);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        light(world, x, y, z);
        return base.onUse(world, x, y, z, player);
    }

    private void light(World world, int x, int y, int z)
    {
        spawnParticles(world, x, y, z);
        if (id == Block.RedstoneOre.id)
        {
            world.SetBlock(x, y, z, Block.LitRedstoneOre.id);
        }

    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (id == Block.LitRedstoneOre.id)
        {
            world.SetBlock(x, y, z, Block.RedstoneOre.id);
        }

    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.Redstone.id;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 4 + random.NextInt(2);
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (lit)
        {
            spawnParticles(world, x, y, z);
        }

    }

    private void spawnParticles(World world, int x, int y, int z)
    {
        JavaRandom random = world.random;
        double faceOffset = 1.0D / 16.0D;

        for (int direction = 0; direction < 6; ++direction)
        {
            double particleX = (double)((float)x + random.NextFloat());
            double particleY = (double)((float)y + random.NextFloat());
            double particleZ = (double)((float)z + random.NextFloat());
            if (direction == 0 && !world.IsOpaque(x, y + 1, z))
            {
                particleY = (double)(y + 1) + faceOffset;
            }

            if (direction == 1 && !world.IsOpaque(x, y - 1, z))
            {
                particleY = (double)(y + 0) - faceOffset;
            }

            if (direction == 2 && !world.IsOpaque(x, y, z + 1))
            {
                particleZ = (double)(z + 1) + faceOffset;
            }

            if (direction == 3 && !world.IsOpaque(x, y, z - 1))
            {
                particleZ = (double)(z + 0) - faceOffset;
            }

            if (direction == 4 && !world.IsOpaque(x + 1, y, z))
            {
                particleX = (double)(x + 1) + faceOffset;
            }

            if (direction == 5 && !world.IsOpaque(x - 1, y, z))
            {
                particleX = (double)(x + 0) - faceOffset;
            }

            if (particleX < (double)x || particleX > (double)(x + 1) || particleY < 0.0D || particleY > (double)(y + 1) || particleZ < (double)z || particleZ > (double)(z + 1))
            {
                world.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }

    }
}
