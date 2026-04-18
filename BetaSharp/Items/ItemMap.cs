using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Maps;

namespace BetaSharp.Items;

public class ItemMap : NetworkSyncedItem
{

    public ItemMap(int id) : base(id)
    {
        setMaxCount(1);
    }

    public static MapState getMapState(int mapId, IWorldContext world)
    {
        string mapName = "map_" + mapId;
        MapState? mapState = (MapState?)world.StateManager.LoadData(typeof(MapState), mapName);
        if (mapState == null)
        {
            mapState = new MapState(mapName);
            world.StateManager.SetData(mapName, mapState);
        }

        return mapState;
    }

    public static MapState getSavedMapState(ItemStack stack, IWorldContext world)
    {
        string mapName = "map_" + stack.getDamage();
        MapState? mapState = (MapState?)world.StateManager.LoadData(typeof(MapState), mapName);
        if (mapState == null)
        {
            stack.setDamage(world.StateManager.GetUniqueDataId("map"));
            mapState = new MapState(mapName);
            mapState.CenterX = world.Properties.SpawnX;
            mapState.CenterZ = world.Properties.SpawnZ;
            mapState.Scale = 3;
            mapState.Dimension = (sbyte)world.Dimension.Id;
            mapState.MarkDirty();
            world.StateManager.SetData(mapName, mapState);
        }

        return mapState;
    }

    public void update(IWorldContext world, Entity entity, MapState map)
    {
        if (world.Dimension.Id == map.Dimension)
        {
            short mapWidth = 128;
            short mapHeight = 128;
            int blocksPerPixel = 1 << map.Scale;
            int centerX = map.CenterX;
            int centerZ = map.CenterZ;
            int entityPosX = MathHelper.Floor(entity.X - (double)centerX) / blocksPerPixel + mapWidth / 2;
            int entityPosZ = MathHelper.Floor(entity.Z - (double)centerZ) / blocksPerPixel + mapHeight / 2;
            int scanRadius = 128 / blocksPerPixel;
            if (world.Dimension.HasCeiling)
            {
                scanRadius /= 2;
            }

            ++map.InventoryTicks;

            for (int pixelX = entityPosX - scanRadius + 1; pixelX < entityPosX + scanRadius; ++pixelX)
            {
                if ((pixelX & 15) == (map.InventoryTicks & 15))
                {
                    int minDirtyZ = 255;
                    int maxDirtyZ = 0;
                    double lastHeight = 0.0D;

                    for (int pixelZ = entityPosZ - scanRadius - 1; pixelZ < entityPosZ + scanRadius; ++pixelZ)
                    {
                        if (pixelX >= 0 && pixelZ >= -1 && pixelX < mapWidth && pixelZ < mapHeight)
                        {
                            int dx = pixelX - entityPosX;
                            int dy = pixelZ - entityPosZ;
                            bool IsOutside = dx * dx + dy * dy > (scanRadius - 2) * (scanRadius - 2);
                            int worldX = (centerX / blocksPerPixel + pixelX - mapWidth / 2) * blocksPerPixel;
                            int worldZ = (centerZ / blocksPerPixel + pixelZ - mapHeight / 2) * blocksPerPixel;
                            byte redSum = 0;
                            byte greenSum = 0;
                            byte blueSum = 0;
                            int[] blockHistogram = new int[256];
                            Chunk chunk = world.ChunkHost.GetChunkFromPos(worldX, worldZ);
                            int chunkOffsetX = worldX & 15;
                            int chunkOffsetZ = worldZ & 15;
                            int fluidDepth = 0;
                            double avgHeight = 0.0D;
                            int sampleX;
                            int sampleZ;
                            int currentY;
                            int colorIndex;
                            if (world.Dimension.HasCeiling)
                            {
                                sampleX = worldX + worldZ * 231871;
                                sampleX = sampleX * sampleX * 31287121 + sampleX * 11;
                                if ((sampleX >> 20 & 1) == 0)
                                {
                                    blockHistogram[Block.Dirt.id] += 10;
                                }
                                else
                                {
                                    blockHistogram[Block.Stone.id] += 10;
                                }

                                avgHeight = 100.0D;
                            }
                            else
                            {
                                for (sampleX = 0; sampleX < blocksPerPixel; ++sampleX)
                                {
                                    for (sampleZ = 0; sampleZ < blocksPerPixel; ++sampleZ)
                                    {
                                        currentY = chunk.GetHeight(sampleX + chunkOffsetX, sampleZ + chunkOffsetZ) + 1;
                                        int blockId = 0;
                                        if (currentY > 1)
                                        {
                                            processBlockHeight(chunk, sampleX, chunkOffsetX, sampleZ, chunkOffsetZ, ref currentY, out blockId, ref fluidDepth);
                                        }

                                        avgHeight += (double)currentY / (double)(blocksPerPixel * blocksPerPixel);
                                        ++blockHistogram[blockId];
                                    }
                                }
                            }

                            fluidDepth /= blocksPerPixel * blocksPerPixel;
                            int averageRed = redSum / (blocksPerPixel * blocksPerPixel);
                            int averageGreen = greenSum / (blocksPerPixel * blocksPerPixel);
                            int averageBlue = blueSum / (blocksPerPixel * blocksPerPixel);
                            sampleX = 0;
                            sampleZ = 0;

                            for (currentY = 0; currentY < 256; ++currentY)
                            {
                                if (blockHistogram[currentY] > sampleX)
                                {
                                    sampleZ = currentY;
                                    sampleX = blockHistogram[currentY];
                                }
                            }

                            double shadeFactor = (avgHeight - lastHeight) * 4.0D / (double)(blocksPerPixel + 4) + ((double)(pixelX + pixelZ & 1) - 0.5D) * 0.4D;
                            byte brightness = 1;
                            if (shadeFactor > 0.6D)
                            {
                                brightness = 2;
                            }

                            if (shadeFactor < -0.6D)
                            {
                                brightness = 0;
                            }

                            colorIndex = 0;
                            if (sampleZ > 0)
                            {
                                MapColor mapColor = Block.Blocks[sampleZ].material.MapColor;
                                if (mapColor == MapColor.Water)
                                {
                                    shadeFactor = (double)fluidDepth * 0.1D + (double)(pixelX + pixelZ & 1) * 0.2D;
                                    brightness = 1;
                                    if (shadeFactor < 0.5D)
                                    {
                                        brightness = 2;
                                    }

                                    if (shadeFactor > 0.9D)
                                    {
                                        brightness = 0;
                                    }
                                }

                                colorIndex = mapColor.Id;
                            }

                            lastHeight = avgHeight;
                            if (pixelZ >= 0 && dx * dx + dy * dy < scanRadius * scanRadius && (!IsOutside || (pixelX + pixelZ & 1) != 0))
                            {
                                byte currentColor = map.Colors[pixelX + pixelZ * mapWidth];
                                byte pixelColor = (byte)(colorIndex * 4 + brightness);
                                if (currentColor != pixelColor)
                                {
                                    if (minDirtyZ > pixelZ)
                                    {
                                        minDirtyZ = pixelZ;
                                    }

                                    if (maxDirtyZ < pixelZ)
                                    {
                                        maxDirtyZ = pixelZ;
                                    }

                                    map.Colors[pixelX + pixelZ * mapWidth] = pixelColor;
                                }
                            }
                        }
                    }

                    if (minDirtyZ <= maxDirtyZ)
                    {
                        map.MarkDirty(pixelX, minDirtyZ, maxDirtyZ);
                    }
                }
            }

        }
    }

    private static void processBlockHeight(Chunk chunk, int chunkX, int dx, int chunkZ, int dz, ref int scanY, out int blockId, ref int fluidDepth)
    {
        bool foundSurface = false;
        blockId = 0;
        bool exitLoop = false;

        while (!exitLoop)
        {
            foundSurface = true;
            blockId = chunk.GetBlockId(chunkX + dx, scanY - 1, chunkZ + dz);
            if (blockId == 0)
            {
                foundSurface = false;
            }
            else if (scanY > 0 && blockId > 0 && Block.Blocks[blockId].material.MapColor == MapColor.Air)
            {
                foundSurface = false;
            }

            if (!foundSurface)
            {
                --scanY;
                blockId = chunk.GetBlockId(chunkX + dx, scanY - 1, chunkZ + dz);
            }

            if (foundSurface)
            {
                if (blockId == 0 || !Block.Blocks[blockId].material.IsFluid)
                {
                    exitLoop = true;
                }
                else
                {
                    int depthCheckY = scanY - 1;

                    while (true)
                    {
                        int fluidBlockId = chunk.GetBlockId(chunkX + dx, depthCheckY--, chunkZ + dz);
                        ++fluidDepth;
                        if (depthCheckY <= 0 || fluidBlockId == 0 || !Block.Blocks[fluidBlockId].material.IsFluid)
                        {
                            exitLoop = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    public override void inventoryTick(ItemStack itemStack, IWorldContext world, Entity entity, int slotIndex, bool shouldUpdate)
    {
        if (!world.IsRemote)
        {
            MapState mapState = getMapState(itemStack.getDamage(), world);
            if (entity is EntityPlayer)
            {
                EntityPlayer entityPlayer = (EntityPlayer)entity;
                mapState.Update(entityPlayer, itemStack);
            }

            if (shouldUpdate)
            {
                update(world, entity, mapState);
            }

        }
    }

    public override void onCraft(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        itemStack.setDamage(world.StateManager.GetUniqueDataId("map"));
        string mapName = "map_" + itemStack.getDamage();
        MapState mapState = new MapState(mapName);
        world.StateManager.SetData(mapName, mapState);
        mapState.CenterX = MathHelper.Floor(entityPlayer.X);
        mapState.CenterZ = MathHelper.Floor(entityPlayer.Z);
        mapState.Scale = 3;
        mapState.Dimension = (sbyte)world.Dimension.Id;
        mapState.MarkDirty();
    }

    public override Packet? getUpdatePacket(ItemStack stack, IWorldContext world, EntityPlayer player)
    {
        byte[] updateData = getMapState(stack.getDamage(), world).GetPlayerMarkerPacket(player);
        return updateData == null ? null : MapUpdateS2CPacket.Get((short)Item.Map.id, (short)stack.getDamage(), updateData);
    }
}
