using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockRail : Block
{

    private readonly bool alwaysStraight;

    public static bool isRail(World world, int x, int y, int z)
    {
        int blockId = world.getBlockId(x, y, z);
        return blockId == Block.Rail.id || blockId == Block.PoweredRail.id || blockId == Block.DetectorRail.id;
    }

    public static bool isRail(int id)
    {
        return id == Block.Rail.id || id == Block.PoweredRail.id || id == Block.DetectorRail.id;
    }

    public BlockRail(int id, int textureId, bool alwaysStraight) : base(id, textureId, Material.PistonBreakable)
    {
        this.alwaysStraight = alwaysStraight;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
    }

    public bool isAlwaysStraight()
    {
        return alwaysStraight;
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override HitResult raycast(World world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, x, y, z);
        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int meta = iBlockAccess.getBlockMeta(x, y, z);
        if (meta >= 2 && meta <= 5)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 10.0F / 16.0F, 1.0F);
        }
        else
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
        }

    }

    public override int getTexture(int side, int meta)
    {
        if (alwaysStraight)
        {
            if (id == Block.PoweredRail.id && (meta & 8) == 0)
            {
                return textureId - 16;
            }
        }
        else if (meta >= 6)
        {
            return textureId - 16;
        }

        return textureId;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.MinecartTrack;
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return world.shouldSuffocate(x, y - 1, z);
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (!world.isRemote)
        {
            updateShape(world, x, y, z, true);
        }

    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!world.isRemote)
        {
            int meta = world.getBlockMeta(x, y, z);
            int railMeta = meta;
            if (alwaysStraight)
            {
                railMeta = meta & 7;
            }

            bool shouldBreak = false;
            if (!world.shouldSuffocate(x, y - 1, z))
            {
                shouldBreak = true;
            }

            if (railMeta == 2 && !world.shouldSuffocate(x + 1, y, z))
            {
                shouldBreak = true;
            }

            if (railMeta == 3 && !world.shouldSuffocate(x - 1, y, z))
            {
                shouldBreak = true;
            }

            if (railMeta == 4 && !world.shouldSuffocate(x, y, z - 1))
            {
                shouldBreak = true;
            }

            if (railMeta == 5 && !world.shouldSuffocate(x, y, z + 1))
            {
                shouldBreak = true;
            }

            if (shouldBreak)
            {
                dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
                world.setBlock(x, y, z, 0);
            }
            else if (base.id == Block.PoweredRail.id)
            {
                bool isPowered = world.isPowered(x, y, z) || world.isPowered(x, y + 1, z);
                isPowered = isPowered || isPoweredByConnectedRails(world, x, y, z, meta, true, 0) || isPoweredByConnectedRails(world, x, y, z, meta, false, 0);
                bool stateChanged = false;
                if (isPowered && (meta & 8) == 0)
                {
                    world.setBlockMeta(x, y, z, railMeta | 8);
                    stateChanged = true;
                }
                else if (!isPowered && (meta & 8) != 0)
                {
                    world.setBlockMeta(x, y, z, railMeta);
                    stateChanged = true;
                }

                if (stateChanged)
                {
                    world.notifyNeighbors(x, y - 1, z, base.id);
                    if (railMeta == 2 || railMeta == 3 || railMeta == 4 || railMeta == 5)
                    {
                        world.notifyNeighbors(x, y + 1, z, base.id);
                    }
                }
            }
            else if (id > 0 && Block.Blocks[id].canEmitRedstonePower() && !alwaysStraight && RailLogic.GetNAdjacentTracks(new RailLogic(this, world, new Vec3i(x, y, z))) == 3)
            {
                updateShape(world, x, y, z, false);
            }

        }
    }

    private void updateShape(World world, int x, int y, int z, bool force)
    {
        if (!world.isRemote)
        {
            new RailLogic(this, world, new Vec3i(x, y, z)).UpdateState(world.isPowered(x, y, z), force);
        }
    }

    private bool isPoweredByConnectedRails(World world, int x, int y, int z, int meta, bool towardsNegative, int depth)
    {
        if (depth >= 8)
        {
            return false;
        }
        else
        {
            int shape = meta & 7;
            bool isSameY = true;
            switch (shape)
            {
                case 0:
                    if (towardsNegative)
                    {
                        ++z;
                    }
                    else
                    {
                        --z;
                    }
                    break;
                case 1:
                    if (towardsNegative)
                    {
                        --x;
                    }
                    else
                    {
                        ++x;
                    }
                    break;
                case 2:
                    if (towardsNegative)
                    {
                        --x;
                    }
                    else
                    {
                        ++x;
                        ++y;
                        isSameY = false;
                    }

                    shape = 1;
                    break;
                case 3:
                    if (towardsNegative)
                    {
                        --x;
                        ++y;
                        isSameY = false;
                    }
                    else
                    {
                        ++x;
                    }

                    shape = 1;
                    break;
                case 4:
                    if (towardsNegative)
                    {
                        ++z;
                    }
                    else
                    {
                        --z;
                        ++y;
                        isSameY = false;
                    }

                    shape = 0;
                    break;
                case 5:
                    if (towardsNegative)
                    {
                        ++z;
                        ++y;
                        isSameY = false;
                    }
                    else
                    {
                        --z;
                    }

                    shape = 0;
                    break;
            }

            return isPoweredByRail(world, x, y, z, towardsNegative, depth, shape) ? true : isSameY && isPoweredByRail(world, x, y - 1, z, towardsNegative, depth, shape);
        }
    }

    private bool isPoweredByRail(World world, int x, int y, int z, bool towardsNegative, int depth, int shape)
    {
        int blockId = world.getBlockId(x, y, z);
        if (blockId == Block.PoweredRail.id)
        {
            int meta = world.getBlockMeta(x, y, z);
            int railMeta = meta & 7;
            if (shape == 1 && (railMeta == 0 || railMeta == 4 || railMeta == 5))
            {
                return false;
            }

            if (shape == 0 && (railMeta == 1 || railMeta == 2 || railMeta == 3))
            {
                return false;
            }

            if ((meta & 8) != 0)
            {
                if (!world.isPowered(x, y, z) && !world.isPowered(x, y + 1, z))
                {
                    return isPoweredByConnectedRails(world, x, y, z, meta, towardsNegative, depth + 1);
                }

                return true;
            }
        }

        return false;
    }

    public override int getPistonBehavior()
    {
        return 0;
    }
}
