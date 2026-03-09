using BetaSharp.Blocks.Materials;
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

        ctx.Level.PauseTicking = true;

        for (horizontalOffset = 0; horizontalOffset < 2; ++horizontalOffset)
        {
            for (verticalOffset = 0; verticalOffset < 3; ++verticalOffset)
            {
                writer.SetBlock(x + extendsInZ * horizontalOffset, y + verticalOffset, z + extendsInX * horizontalOffset, NetherPortal.id);
            }
        }

        ctx.Level.PauseTicking = false;
        return true;
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        sbyte offsetX = 0;
        sbyte offsetZ = 1;
        if (ctx.Level.BlocksReader.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id || ctx.Level.BlocksReader.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id)
        {
            offsetX = 1;
            offsetZ = 0;
        }

        int portalBottomY;
        for (portalBottomY = ctx.Y; ctx.Level.BlocksReader.GetBlockId(ctx.X, portalBottomY - 1, ctx.Z) == id; --portalBottomY)
        {
        }

        if (ctx.Level.BlocksReader.GetBlockId(ctx.X, portalBottomY - 1, ctx.Z) != Obsidian.id)
        {
            ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
        else
        {
            int blocksAbove;
            for (blocksAbove = 1; blocksAbove < 4 && ctx.Level.BlocksReader.GetBlockId(ctx.X, portalBottomY + blocksAbove, ctx.Z) == id; ++blocksAbove)
            {
            }

            if (blocksAbove == 3 && ctx.Level.BlocksReader.GetBlockId(ctx.X, portalBottomY + blocksAbove, ctx.Z) == Obsidian.id)
            {
                bool hasXNeighbors = ctx.Level.BlocksReader.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) == id || ctx.Level.BlocksReader.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) == id;
                bool hasZNeighbors = ctx.Level.BlocksReader.GetBlockId(ctx.X, ctx.Y, ctx.Z - 1) == id || ctx.Level.BlocksReader.GetBlockId(ctx.X, ctx.Y, ctx.Z + 1) == id;
                if (hasXNeighbors && hasZNeighbors)
                {
                    ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
                else if ((ctx.Level.BlocksReader.GetBlockId(ctx.X + offsetX, ctx.Y, ctx.Z + offsetZ) != Obsidian.id || ctx.Level.BlocksReader.GetBlockId(ctx.X - offsetX, ctx.Y, ctx.Z - offsetZ) != id) &&
                         (ctx.Level.BlocksReader.GetBlockId(ctx.X - offsetX, ctx.Y, ctx.Z - offsetZ) != Obsidian.id || ctx.Level.BlocksReader.GetBlockId(ctx.X + offsetX, ctx.Y, ctx.Z + offsetZ) != id))
                {
                    ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
                }
            }
            else
            {
                ctx.Level.BlockWriter.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
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

    public override void onEntityCollision(OnEntityCollisionEvt ctx)
    {
        if (ctx.Entity.vehicle == null && ctx.Entity.passenger == null)
        {
            ctx.Entity.tickPortalCooldown();
        }
    }

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        if (Random.Shared.Next(100) == 0)
        {
            ctx.Level.Broadcaster.PlaySoundAtPos(ctx.X + 0.5D, ctx.Y + 0.5D, ctx.Z + 0.5D, "portal.portal", 1.0F, Random.Shared.NextSingle() * 0.4F + 0.8F);
        }

        for (int particleIndex = 0; particleIndex < 4; ++particleIndex)
        {
            double particleX = ctx.X + Random.Shared.NextSingle();
            double particleY = ctx.Y + Random.Shared.NextSingle();
            double particleZ = ctx.Z + Random.Shared.NextSingle();
            int direction = Random.Shared.Next(2) * 2 - 1;
           double velocityX = (Random.Shared.NextSingle() - 0.5D) * 0.5D;
           double velocityY = (Random.Shared.NextSingle() - 0.5D) * 0.5D;
           double velocityZ = (Random.Shared.NextSingle() - 0.5D) * 0.5D;
            if (ctx.Level.BlocksReader.GetBlockId(ctx.X - 1, ctx.Y, ctx.Z) != id && ctx.Level.BlocksReader.GetBlockId(ctx.X + 1, ctx.Y, ctx.Z) != id)
            {
                particleX = ctx.X + 0.5D + 0.25D * direction;
                velocityX = Random.Shared.NextSingle() * 2.0F * direction;
            }
            else
            {
                particleZ = ctx.Z + 0.5D + 0.25D * direction;
                velocityZ = Random.Shared.NextSingle() * 2.0F * direction;
            }

            ctx.Level.Broadcaster.AddParticle("portal", particleX, particleY, particleZ, velocityX, velocityY, velocityZ);
        }
    }
}
