using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Dimensions;

internal class PortalForcer
{
    public void MoveToPortal(World world, Entity entity)
    {
        if (!TeleportToValidPortal(world, entity))
        {
            CreatePortal(world, entity);
            TeleportToValidPortal(world, entity);
        }
    }

    public static bool TeleportToValidPortal(World world, Entity entity)
    {
        short searchRadius = 128;
        double closestDistance = -1.0D;
        int foundX = 0;
        int foundY = 0;
        int foundZ = 0;

        int entityX = MathHelper.Floor(entity.X);
        int entityZ = MathHelper.Floor(entity.Z);

        // Phase 1: Search for an existing portal
        for (int x = entityX - searchRadius; x <= entityX + searchRadius; ++x)
        {
            double dx = x + 0.5D - entity.X;

            for (int z = entityZ - searchRadius; z <= entityZ + searchRadius; ++z)
            {
                double dz = z + 0.5D - entity.Z;

                for (int y = 127; y >= 0; --y)
                {
                    if (world.Reader.GetBlockId(x, y, z) == Block.NetherPortal.ID)
                    {
                        // Walk down to the bottom obsidian block of the portal frame
                        while (world.Reader.GetBlockId(x, y - 1, z) == Block.NetherPortal.ID)
                        {
                            --y;
                        }

                        double dy = y + 0.5D - entity.Y;
                        double distanceSq = dx * dx + dy * dy + dz * dz;

                        if (closestDistance < 0.0D || distanceSq < closestDistance)
                        {
                            closestDistance = distanceSq;
                            foundX = x;
                            foundY = y;
                            foundZ = z;
                        }
                    }
                }
            }
        }

        if (closestDistance >= 0.0D)
        {
            double targetX = foundX + 0.5D;
            double targetY = foundY + 0.5D;
            double targetZ = foundZ + 0.5D;

            // Offset the player so they don't spawn inside the obsidian frame
            if (world.Reader.GetBlockId(foundX - 1, foundY, foundZ) == Block.NetherPortal.ID)
            {
                targetX -= 0.5D;
            }

            if (world.Reader.GetBlockId(foundX + 1, foundY, foundZ) == Block.NetherPortal.ID)
            {
                targetX += 0.5D;
            }

            if (world.Reader.GetBlockId(foundX, foundY, foundZ - 1) == Block.NetherPortal.ID)
            {
                targetZ -= 0.5D;
            }

            if (world.Reader.GetBlockId(foundX, foundY, foundZ + 1) == Block.NetherPortal.ID)
            {
                targetZ += 0.5D;
            }

            entity.SetPositionAndAnglesKeepPrevAngles(targetX, targetY, targetZ, entity.Yaw, 0.0F);
            entity.VelocityX = entity.VelocityY = entity.VelocityZ = 0.0D;
            return true;
        }

        return false;
    }

    public static bool CreatePortal(World world, Entity entity)
    {
        byte searchRadius = 16;
        double closestDistance = -1.0D;

        int entityX = MathHelper.Floor(entity.X);
        int entityY = MathHelper.Floor(entity.Y);
        int entityZ = MathHelper.Floor(entity.Z);

        int bestX = entityX;
        int bestY = entityY;
        int bestZ = entityZ;
        int bestDirection = 0;

        int randomDirection = Random.Shared.Next(4);
        int h1 = ChuckFormat.WorldHeight - 1;

        // Phase 1: Search for an optimal flat 3x4 area of solid ground
        for (int x = entityX - searchRadius; x <= entityX + searchRadius; ++x)
        {
            double dx = x + 0.5D - entity.X;

            for (int z = entityZ - searchRadius; z <= entityZ + searchRadius; ++z)
            {
                double dz = z + 0.5D - entity.Z;

                for (int y = h1; y >= 0; --y)
                {
                    if (world.Reader.IsAir(x, y, z))
                    {
                        while (y > 0 && world.Reader.IsAir(x, y - 1, z))
                        {
                            --y;
                        }

                        for (int dirOffset = randomDirection; dirOffset < randomDirection + 4; ++dirOffset)
                        {
                            int dirX = dirOffset % 2;
                            int dirZ = 1 - dirX;
                            if (dirOffset % 4 >= 2)
                            {
                                dirX = -dirX;
                                dirZ = -dirZ;
                            }

                            bool validLocation = true;
                            for (int width = 0; width < 3 && validLocation; ++width)
                            {
                                for (int widthDepth = 0; widthDepth < 4 && validLocation; ++widthDepth)
                                {
                                    for (int height = -1; height < 4 && validLocation; ++height)
                                    {
                                        int checkX = x + (widthDepth - 1) * dirX + width * dirZ;
                                        int checkY = y + height;
                                        int checkZ = z + (widthDepth - 1) * dirZ - width * dirX;

                                        if ((height < 0 && !world.Reader.GetMaterial(checkX, checkY, checkZ).IsSolid) || (height >= 0 && !world.Reader.IsAir(checkX, checkY, checkZ)))
                                        {
                                            validLocation = false;
                                        }
                                    }
                                }
                            }

                            if (validLocation)
                            {
                                double dy = y + 0.5D - entity.Y;
                                double distanceSq = dx * dx + dy * dy + dz * dz;
                                if (closestDistance < 0.0D || distanceSq < closestDistance)
                                {
                                    closestDistance = distanceSq;
                                    bestX = x;
                                    bestY = y;
                                    bestZ = z;
                                    bestDirection = dirOffset % 4;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Phase 2: If optimal location fails, settle for a tighter 1x4 area
        if (closestDistance < 0.0D)
        {
            for (int x = entityX - searchRadius; x <= entityX + searchRadius; ++x)
            {
                double dx = x + 0.5D - entity.X;

                for (int z = entityZ - searchRadius; z <= entityZ + searchRadius; ++z)
                {
                    double dz = z + 0.5D - entity.Z;

                    for (int y = h1; y >= 0; --y)
                    {
                        if (world.Reader.IsAir(x, y, z))
                        {
                            while (world.Reader.IsAir(x, y - 1, z))
                            {
                                --y;
                            }

                            for (int dirOffset = randomDirection; dirOffset < randomDirection + 2; ++dirOffset)
                            {
                                int dirX = dirOffset % 2;
                                int dirZ = 1 - dirX;

                                bool validLocation = true;
                                for (int widthDepth = 0; widthDepth < 4 && validLocation; ++widthDepth)
                                {
                                    for (int height = -1; height < 4 && validLocation; ++height)
                                    {
                                        int checkX = x + (widthDepth - 1) * dirX;
                                        int checkY = y + height;
                                        int checkZ = z + (widthDepth - 1) * dirZ;

                                        if ((height < 0 && !world.Reader.GetMaterial(checkX, checkY, checkZ).IsSolid) || (height >= 0 && !world.Reader.IsAir(checkX, checkY, checkZ)))
                                        {
                                            validLocation = false;
                                        }
                                    }
                                }

                                if (validLocation)
                                {
                                    double dy = y + 0.5D - entity.Y;
                                    double distanceSq = dx * dx + dy * dy + dz * dz;
                                    if (closestDistance < 0.0D || distanceSq < closestDistance)
                                    {
                                        closestDistance = distanceSq;
                                        bestX = x;
                                        bestY = y;
                                        bestZ = z;
                                        bestDirection = dirOffset % 2;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Phase 3: Force generation
        int finalX = bestX;
        int finalY = bestY;
        int finalZ = bestZ;

        int finalDirX = bestDirection % 2;
        int finalDirZ = 1 - finalDirX;

        if (bestDirection % 4 >= 2)
        {
            finalDirX = -finalDirX;
            finalDirZ = -finalDirZ;
        }

        // If no valid spot was found, carve one out in the sky/ground.
        if (closestDistance < 0.0D)
        {
            finalY = Math.Clamp(finalY, 70, 118);

            for (int w = -1; w <= 1; ++w)
            {
                for (int wDepth = 1; wDepth < 3; ++wDepth)
                {
                    for (int h = -1; h < 3; ++h)
                    {
                        int buildX = finalX + (wDepth - 1) * finalDirX + w * finalDirZ;
                        int buildY = finalY + h;
                        int buildZ = finalZ + (wDepth - 1) * finalDirZ - w * finalDirX;

                        bool isFloor = h < 0;
                        world.Writer.SetBlock(buildX, buildY, buildZ, isFloor ? Block.Obsidian.ID : 0);
                    }
                }
            }
        }

        // Phase 4: Construct the Obsidian Frame and spawn portal blocks
        for (int pass = 0; pass < 4; ++pass)
        {
            for (int wDepth = 0; wDepth < 4; ++wDepth)
            {
                for (int h = -1; h < 4; ++h)
                {
                    int buildX = finalX + (wDepth - 1) * finalDirX;
                    int buildY = finalY + h;
                    int buildZ = finalZ + (wDepth - 1) * finalDirZ;

                    bool isFrameEdge = wDepth == 0 || wDepth == 3 || h == -1 || h == 3;
                    world.Writer.SetBlockInternal(buildX, buildY, buildZ, isFrameEdge ? Block.Obsidian.ID : Block.NetherPortal.ID);
                }
            }

            // Block updates (lighting, neighbor checks)
            for (int wDepth = 0; wDepth < 4; ++wDepth)
            {
                for (int h = -1; h < 4; ++h)
                {
                    int buildX = finalX + (wDepth - 1) * finalDirX;
                    int buildY = finalY + h;
                    int buildZ = finalZ + (wDepth - 1) * finalDirZ;

                    world.Broadcaster.NotifyNeighbors(buildX, buildY, buildZ, world.Reader.GetBlockId(buildX, buildY, buildZ));
                }
            }
        }

        return true;
    }
}
