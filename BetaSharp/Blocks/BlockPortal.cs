using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockPortal : BlockBreakable
{
    public BlockPortal(int id, int textureId) : base(id, textureId, Material.NetherPortal, false)
    {
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        float thickness;
        float halfExtent;
        if (iBlockReader.GetBlockId(x - 1, y, z) != id && iBlockReader.GetBlockId(x + 1, y, z) != id)
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

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public bool create(IBlockReader reader, IBlockWrite writer, int x, int y, int z)
    {
        sbyte extendsInZ = 0;
        sbyte extendsInX = 0;
        if (reader.GetBlockId(x - 1, y, z) == Obsidian.id || reader.GetBlockId(x + 1, y, z) == Obsidian.id)
        {
            extendsInZ = 1;
        }

        if (reader.GetBlockId(x, y, z - 1) == Obsidian.id || reader.GetBlockId(x, y, z + 1) == Obsidian.id)
        {
            extendsInX = 1;
        }

        if (extendsInZ == extendsInX)
        {
            return false;
        }

        if (reader.GetBlockId(x - extendsInZ, y, z - extendsInX) == 0)
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
                if ((horizontalOffset != -1 && horizontalOffset != 2) || (verticalOffset != -1 && verticalOffset != 3))
                {
                    int blockId = reader.GetBlockId(x + extendsInZ * horizontalOffset, y + verticalOffset, z + extendsInX * horizontalOffset);
                    if (isFrame)
                    {
                        if (blockId != Obsidian.id)
                        {
                            return false;
                        }
                    }
                    else if (blockId != 0 && blockId != Fire.id)
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
                writer.SetBlock(x + extendsInZ * horizontalOffset, y + verticalOffset, z + extendsInX * horizontalOffset, NetherPortal.id);
            }
        }

        world.pauseTicking = false;
        return true;
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        sbyte offsetX = 0;
        sbyte offsetZ = 1;
        if (ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id || ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id)
        {
            offsetX = 1;
            offsetZ = 0;
        }

        int portalBottomY;
        for (portalBottomY = ctx.Y; ctx.WorldRead.GetBlockId(ctx.X, portalBottomY - 1, ctx.Z) == id; --portalBottomY)
        {
        }

        if (ctx.WorldRead.GetBlockId(ctx.X, portalBottomY - 1, ctx.Z) != Obsidian.id)
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
        else
        {
            int blocksAbove;
            for (blocksAbove = 1; blocksAbove < 4 && ctx.WorldRead.GetBlockId(ctx.X, portalBottomY + blocksAbove, ctx.Z) == id; ++blocksAbove)
            {
            }

            if (blocksAbove == 3 && ctx.WorldRead.GetBlockId(ctx.X, portalBottomY + blocksAbove, ctx.Z) == Obsidian.id)
            {
                bool hasXNeighbors = ctx.WorldRead.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id || ctx.WorldRead.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id;
                bool hasZNeighbors = ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1) == id || ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1) == id;
                if (hasXNeighbors && hasZNeighbors)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else if ((ctx.WorldRead.GetBlockId(ctx.X + offsetX, ctx.Y, ctx.Z + offsetZ) != Obsidian.id || ctx.WorldRead.GetBlockId(ctx.X - offsetX, ctx.Y, ctx.Z - offsetZ) != id) &&
                         (ctx.WorldRead.GetBlockId(ctx.X - offsetX, ctx.Y, ctx.Z - offsetZ) != Obsidian.id || ctx.WorldRead.GetBlockId(ctx.X + offsetX, ctx.Y, ctx.Z + offsetZ) != id))
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
            }
            else
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (iBlockReader.GetBlockId(x, y, z) == id)
        {
            return false;
        }

        bool edgeWest = iBlockReader.GetBlockId(x - 1, y, z) == id && iBlockReader.GetBlockId(x - 2, y, z) != id;
        bool edgeEast = iBlockReader.GetBlockId(x + 1, y, z) == id && iBlockReader.GetBlockId(x + 2, y, z) != id;
        bool edgeNorth = iBlockReader.GetBlockId(x, y, z - 1) == id && iBlockReader.GetBlockId(x, y, z - 2) != id;
        bool edgeSouth = iBlockReader.GetBlockId(x, y, z + 1) == id && iBlockReader.GetBlockId(x, y, z + 2) != id;
        bool extendsInX = edgeWest || edgeEast;
        bool extendsInZ = edgeNorth || edgeSouth;
        return extendsInX && side == 4 ? true : extendsInX && side == 5 ? true : extendsInZ && side == 2 ? true : extendsInZ && side == 3;
    }

    public override int getDroppedItemCount() => 0;

    public override int getRenderLayer() => 1;

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
            world.playSound(x + 0.5D, y + 0.5D, z + 0.5D, "portal.portal", 1.0F, random.NextFloat() * 0.4F + 0.8F);
        }

        for (int particleIndex = 0; particleIndex < 4; ++particleIndex)
        {
            double particleX = x + random.NextFloat();
            double particleY = y + random.NextFloat();
            double particleZ = z + random.NextFloat();
            double velocityX = 0.0D;
            double velocityY = 0.0D;
            double velocityZ = 0.0D;
            int direction = random.NextInt(2) * 2 - 1;
            velocityX = (random.NextFloat() - 0.5D) * 0.5D;
            velocityY = (random.NextFloat() - 0.5D) * 0.5D;
            velocityZ = (random.NextFloat() - 0.5D) * 0.5D;
            if (world.getBlockId(x - 1, y, z) != id && world.getBlockId(x + 1, y, z) != id)
            {
                particleX = x + 0.5D + 0.25D * direction;
                velocityX = random.NextFloat() * 2.0F * direction;
            }
            else
            {
                particleZ = z + 0.5D + 0.25D * direction;
                velocityZ = random.NextFloat() * 2.0F * direction;
            }

            world.addParticle("portal", particleX, particleY, particleZ, velocityX, velocityY, velocityZ);
        }
    }
}
