using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockPortal : BlockBreakable
{

    public BlockPortal(int id, int textureId) : base(id, textureId, Material.NetherPortal, false)
    {
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        float thickness;
        float halfExtent;
        if (iBlockAccess.getBlockId(x - 1, y, z) != id && iBlockAccess.getBlockId(x + 1, y, z) != id)
        {
            thickness = 2.0F / 16.0F;
            halfExtent = 0.5F;
            setBoundingBox(0.5F - thickness, 0.0F, 0.5F - halfExtent, 0.5F + thickness, 1.0F, 0.5F + halfExtent);
        }
        else
        {
            thickness = 0.5F;
            halfExtent = 2.0F / 16.0F;
            setBoundingBox(0.5F - thickness, 0.0F, 0.5F - halfExtent, 0.5F + thickness, 1.0F, 0.5F + halfExtent);
        }

    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public bool create(World world, int x, int y, int z)
    {
        sbyte extendsInZ = 0;
        sbyte extendsInX = 0;
        if (world.getBlockId(x - 1, y, z) == Block.Obsidian.id || world.getBlockId(x + 1, y, z) == Block.Obsidian.id)
        {
            extendsInZ = 1;
        }

        if (world.getBlockId(x, y, z - 1) == Block.Obsidian.id || world.getBlockId(x, y, z + 1) == Block.Obsidian.id)
        {
            extendsInX = 1;
        }

        if (extendsInZ == extendsInX)
        {
            return false;
        }
        else
        {
            if (world.getBlockId(x - extendsInZ, y, z - extendsInX) == 0)
            {
                x -= extendsInZ;
                z -= extendsInX;
            }

            int horizontalOffset;
            int verticalOffset;
            for (horizontalOffset = -1; horizontalOffset <= 2; ++horizontalOffset)
            {
                for (verticalOffset = -1; verticalOffset <= 3; ++verticalOffset)
                {
                    bool isFrame = horizontalOffset == -1 || horizontalOffset == 2 || verticalOffset == -1 || verticalOffset == 3;
                    if (horizontalOffset != -1 && horizontalOffset != 2 || verticalOffset != -1 && verticalOffset != 3)
                    {
                        int blockId = world.getBlockId(x + extendsInZ * horizontalOffset, y + verticalOffset, z + extendsInX * horizontalOffset);
                        if (isFrame)
                        {
                            if (blockId != Block.Obsidian.id)
                            {
                                return false;
                            }
                        }
                        else if (blockId != 0 && blockId != Block.Fire.id)
                        {
                            return false;
                        }
                    }
                }
            }

            world.pauseTicking = true;

            for (horizontalOffset = 0; horizontalOffset < 2; ++horizontalOffset)
            {
                for (verticalOffset = 0; verticalOffset < 3; ++verticalOffset)
                {
                    world.setBlock(x + extendsInZ * horizontalOffset, y + verticalOffset, z + extendsInX * horizontalOffset, Block.NetherPortal.id);
                }
            }

            world.pauseTicking = false;
            return true;
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        sbyte offsetX = 0;
        sbyte offsetZ = 1;
        if (world.getBlockId(x - 1, y, z) == base.id || world.getBlockId(x + 1, y, z) == base.id)
        {
            offsetX = 1;
            offsetZ = 0;
        }

        int portalBottomY;
        for (portalBottomY = y; world.getBlockId(x, portalBottomY - 1, z) == base.id; --portalBottomY)
        {
        }

        if (world.getBlockId(x, portalBottomY - 1, z) != Block.Obsidian.id)
        {
            world.setBlock(x, y, z, 0);
        }
        else
        {
            int blocksAbove;
            for (blocksAbove = 1; blocksAbove < 4 && world.getBlockId(x, portalBottomY + blocksAbove, z) == base.id; ++blocksAbove)
            {
            }

            if (blocksAbove == 3 && world.getBlockId(x, portalBottomY + blocksAbove, z) == Block.Obsidian.id)
            {
                bool hasXNeighbors = world.getBlockId(x - 1, y, z) == base.id || world.getBlockId(x + 1, y, z) == base.id;
                bool hasZNeighbors = world.getBlockId(x, y, z - 1) == base.id || world.getBlockId(x, y, z + 1) == base.id;
                if (hasXNeighbors && hasZNeighbors)
                {
                    world.setBlock(x, y, z, 0);
                }
                else if ((world.getBlockId(x + offsetX, y, z + offsetZ) != Block.Obsidian.id || world.getBlockId(x - offsetX, y, z - offsetZ) != base.id) && (world.getBlockId(x - offsetX, y, z - offsetZ) != Block.Obsidian.id || world.getBlockId(x + offsetX, y, z + offsetZ) != base.id))
                {
                    world.setBlock(x, y, z, 0);
                }
            }
            else
            {
                world.setBlock(x, y, z, 0);
            }
        }
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (iBlockAccess.getBlockId(x, y, z) == id)
        {
            return false;
        }
        else
        {
            bool edgeWest = iBlockAccess.getBlockId(x - 1, y, z) == id && iBlockAccess.getBlockId(x - 2, y, z) != id;
            bool edgeEast = iBlockAccess.getBlockId(x + 1, y, z) == id && iBlockAccess.getBlockId(x + 2, y, z) != id;
            bool edgeNorth = iBlockAccess.getBlockId(x, y, z - 1) == id && iBlockAccess.getBlockId(x, y, z - 2) != id;
            bool edgeSouth = iBlockAccess.getBlockId(x, y, z + 1) == id && iBlockAccess.getBlockId(x, y, z + 2) != id;
            bool extendsInX = edgeWest || edgeEast;
            bool extendsInZ = edgeNorth || edgeSouth;
            return extendsInX && side == 4 ? true : (extendsInX && side == 5 ? true : (extendsInZ && side == 2 ? true : extendsInZ && side == 3));
        }
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override int getRenderLayer()
    {
        return 1;
    }

    public override void onEntityCollision(World world, int x, int y, int z, Entity entity)
    {
        if (entity.vehicle == null && entity.passenger == null)
        {
            entity.tickPortalCooldown();
        }

    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (random.NextInt(100) == 0)
        {
            world.playSound((double)x + 0.5D, (double)y + 0.5D, (double)z + 0.5D, "portal.portal", 1.0F, random.NextFloat() * 0.4F + 0.8F);
        }

        for (int particleIndex = 0; particleIndex < 4; ++particleIndex)
        {
            double particleX = (double)((float)x + random.NextFloat());
            double particleY = (double)((float)y + random.NextFloat());
            double particleZ = (double)((float)z + random.NextFloat());
            double velocityX = 0.0D;
            double velocityY = 0.0D;
            double velocityZ = 0.0D;
            int direction = random.NextInt(2) * 2 - 1;
            velocityX = ((double)random.NextFloat() - 0.5D) * 0.5D;
            velocityY = ((double)random.NextFloat() - 0.5D) * 0.5D;
            velocityZ = ((double)random.NextFloat() - 0.5D) * 0.5D;
            if (world.getBlockId(x - 1, y, z) != id && world.getBlockId(x + 1, y, z) != id)
            {
                particleX = (double)x + 0.5D + 0.25D * (double)direction;
                velocityX = (double)(random.NextFloat() * 2.0F * (float)direction);
            }
            else
            {
                particleZ = (double)z + 0.5D + 0.25D * (double)direction;
                velocityZ = (double)(random.NextFloat() * 2.0F * (float)direction);
            }

            world.addParticle("portal", particleX, particleY, particleZ, velocityX, velocityY, velocityZ);
        }

    }
}
