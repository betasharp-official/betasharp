using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPortal(int id, int textureId) : BlockBreakable(id, textureId, Material.NetherPortal, false)
{
    private const float Thickness = 2.0F / 16.0F;
    private const float HalfExtent = 0.5F;

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        if (blockReader.GetBlockId(x - 1, y, z) != id && blockReader.GetBlockId(x + 1, y, z) != id)
        {
            setBoundingBox(0.5F - Thickness, 0.0F, 0.5F - HalfExtent, 0.5F + Thickness, 1.0F, 0.5F + HalfExtent);
        }
        else
        {
            setBoundingBox(0.5F - HalfExtent, 0.0F, 0.5F - Thickness, 0.5F + HalfExtent, 1.0F, 0.5F + Thickness);
        }
    }

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public bool create(IBlockReader reader, IBlockWriter writer, int x, int y, int z)
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
                if (horizontalOffset is -1 or 2 && verticalOffset is -1 or 3) continue;

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

        for (horizontalOffset = 0; horizontalOffset < 2; ++horizontalOffset)
        {
            for (verticalOffset = 0; verticalOffset < 3; ++verticalOffset)
            {
                writer.SetBlockInternal(x + extendsInZ * horizontalOffset, y + verticalOffset, z + extendsInX * horizontalOffset, NetherPortal.id);
            }
        }

        return true;
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        sbyte offsetX = 0;
        sbyte offsetZ = 1;
        if (@event.World.Reader.GetBlockId(@event.X - 1, @event.Y, @event.Z) == id || @event.World.Reader.GetBlockId(@event.X + 1, @event.Y, @event.Z) == id)
        {
            offsetX = 1;
            offsetZ = 0;
        }

        int portalBottomY;
        for (portalBottomY = @event.Y; @event.World.Reader.GetBlockId(@event.X, portalBottomY - 1, @event.Z) == id; --portalBottomY)
        {
        }

        if (@event.World.Reader.GetBlockId(@event.X, portalBottomY - 1, @event.Z) != Obsidian.id)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            int blocksAbove;
            for (blocksAbove = 1; blocksAbove < 4 && @event.World.Reader.GetBlockId(@event.X, portalBottomY + blocksAbove, @event.Z) == id; ++blocksAbove)
            {
            }

            if (blocksAbove == 3 && @event.World.Reader.GetBlockId(@event.X, portalBottomY + blocksAbove, @event.Z) == Obsidian.id)
            {
                bool hasXNeighbors = @event.World.Reader.GetBlockId(@event.X - 1, @event.Y, @event.Z) == id || @event.World.Reader.GetBlockId(@event.X + 1, @event.Y, @event.Z) == id;
                bool hasZNeighbors = @event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z - 1) == id || @event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z + 1) == id;
                if (hasXNeighbors && hasZNeighbors)
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                }
                else if ((@event.World.Reader.GetBlockId(@event.X + offsetX, @event.Y, @event.Z + offsetZ) != Obsidian.id || @event.World.Reader.GetBlockId(@event.X - offsetX, @event.Y, @event.Z - offsetZ) != id) &&
                         (@event.World.Reader.GetBlockId(@event.X - offsetX, @event.Y, @event.Z - offsetZ) != Obsidian.id || @event.World.Reader.GetBlockId(@event.X + offsetX, @event.Y, @event.Z + offsetZ) != id))
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                }
            }
            else
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }
        }
    }

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (iBlockReader.GetBlockId(x, y, z) == id) return false;

        bool edgeWest = iBlockReader.GetBlockId(x - 1, y, z) == id && iBlockReader.GetBlockId(x - 2, y, z) != id;
        bool edgeEast = iBlockReader.GetBlockId(x + 1, y, z) == id && iBlockReader.GetBlockId(x + 2, y, z) != id;
        bool edgeNorth = iBlockReader.GetBlockId(x, y, z - 1) == id && iBlockReader.GetBlockId(x, y, z - 2) != id;
        bool edgeSouth = iBlockReader.GetBlockId(x, y, z + 1) == id && iBlockReader.GetBlockId(x, y, z + 2) != id;
        bool extendsInX = edgeWest || edgeEast;
        bool extendsInZ = edgeNorth || edgeSouth;
        return extendsInX && side == Side.West ||
               extendsInX && side == Side.East ||
               extendsInZ && side == Side.North ||
               extendsInZ && side == Side.South;
    }

    public override int getDroppedItemCount() => 0;

    public override int getRenderLayer() => 1;

    public override void onEntityCollision(OnEntityCollisionEvent @event)
    {
        if (@event.Entity.vehicle == null && @event.Entity.passenger == null)
        {
            @event.Entity.tickPortalCooldown();
        }
    }

    public override void randomDisplayTick(OnTickEvent @event)
    {
        if (Random.Shared.Next(100) == 0)
        {
            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "portal.portal", 1.0F, Random.Shared.NextSingle() * 0.4F + 0.8F);
        }

        for (int particleIndex = 0; particleIndex < 4; ++particleIndex)
        {
            double particleX = @event.X + Random.Shared.NextSingle();
            double particleY = @event.Y + Random.Shared.NextSingle();
            double particleZ = @event.Z + Random.Shared.NextSingle();
            int direction = Random.Shared.Next(2) * 2 - 1;
            double velocityX = (Random.Shared.NextSingle() - 0.5D) * 0.5D;
            double velocityY = (Random.Shared.NextSingle() - 0.5D) * 0.5D;
            double velocityZ = (Random.Shared.NextSingle() - 0.5D) * 0.5D;
            if (@event.World.Reader.GetBlockId(@event.X - 1, @event.Y, @event.Z) != id && @event.World.Reader.GetBlockId(@event.X + 1, @event.Y, @event.Z) != id)
            {
                particleX = @event.X + 0.5D + 0.25D * direction;
                velocityX = Random.Shared.NextSingle() * 2.0F * direction;
            }
            else
            {
                particleZ = @event.Z + 0.5D + 0.25D * direction;
                velocityZ = Random.Shared.NextSingle() * 2.0F * direction;
            }

            @event.World.Broadcaster.AddParticle("portal", particleX, particleY, particleZ, velocityX, velocityY, velocityZ);
        }
    }
}
