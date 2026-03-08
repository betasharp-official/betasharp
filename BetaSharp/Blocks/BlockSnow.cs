using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockSnow : Block
{

    public BlockSnow(int id, int textureId) : base(id, textureId, Material.SnowLayer)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
        setTickRandomly(true);
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z)
    {
        int meta = world.getBlockMeta(x, y, z) & 7;
        return meta >= 3 ?
            new Box(x + BoundingBox.MinX, y + BoundingBox.MinY, z + BoundingBox.MinZ, x + BoundingBox.MaxX, (double)((float)y + 0.5F), z + BoundingBox.MaxZ) :
            null;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int meta = iBlockReader.getBlockMeta(x, y, z) & 7;
        float height = (float)(2 * (1 + meta)) / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, height, 1.0F);
    }

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z)
    {
        int blockBelowId = world.GetBlockId(x, y - 1, z);
        return blockBelowId != 0 && Block.Blocks[blockBelowId].isOpaque() ? world.getMaterial(x, y - 1, z).BlocksMovement : false;
    }

    public override void neighborUpdate(WorldBlockView world, int x, int y, int z, int id)
    {
        breakIfCannotPlace(world, x, y, z);
    }

    private bool breakIfCannotPlace(WorldBlockView world, int x, int y, int z)
    {
        if (!canPlaceAt(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
            return false;
        }
        else
        {
            return true;
        }
    }

    public override void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        int snowballId = Item.Snowball.id;
        float spreadFactor = 0.7F;
        double offsetX = (double)(world.random.NextFloat() * spreadFactor) + (double)(1.0F - spreadFactor) * 0.5D;
        double offsetY = (double)(world.random.NextFloat() * spreadFactor) + (double)(1.0F - spreadFactor) * 0.5D;
        double offsetZ = (double)(world.random.NextFloat() * spreadFactor) + (double)(1.0F - spreadFactor) * 0.5D;
        EntityItem entityItem = new EntityItem(world, (double)x + offsetX, (double)y + offsetY, (double)z + offsetZ, new ItemStack(snowballId, 1, 0));
        entityItem.delayBeforeCanPickup = 10;
        world.SpawnEntity(entityItem);
        world.setBlock(x, y, z, 0);
        player.increaseStat(Stats.Stats.MineBlockStatArray[id], 1);
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.Snowball.id;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override void onTick(OnTickContext ctx)
    {
        if (ctx.Lighting.GetBrightness(LightType.Block, ctx.X, ctx.Y, ctx.Z) > 11)
        {
            dropStacks(ctx.WorldView, ctx.X, ctx.Y, ctx.Z, ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }

    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        return side == 1 ? true : base.isSideVisible(iBlockReader, x, y, z, side);
    }
}
