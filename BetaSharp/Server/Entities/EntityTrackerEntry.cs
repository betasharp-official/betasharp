using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Util.Maths;

namespace BetaSharp.Server.Entities;

internal class EntityTrackerEntry
{
    public Entity currentTrackedEntity;
    public int trackedDistance;
    public int trackingFrequency;
    public int lastX;
    public int lastY;
    public int lastZ;
    public int lastYaw;
    public int lastPitch;
    public double velocityX;
    public double velocityY;
    public double velocityZ;
    public int ticks;
    private double x;
    private double y;
    private double z;
    private bool isInitialized;
    private bool alwaysUpdateVelocity;
    private int ticksSinceLastDismount;
    public bool newPlayerDataUpdated;
    public HashSet<ServerPlayerEntity> listeners = [];

    public EntityTrackerEntry(Entity entity, int trackedDistance, int trackedFrequency, bool alwaysUpdateVelocity)
    {
        currentTrackedEntity = entity;
        this.trackedDistance = trackedDistance;
        trackingFrequency = trackedFrequency;
        this.alwaysUpdateVelocity = alwaysUpdateVelocity;
        lastX = MathHelper.Floor(entity.x * 32.0);
        lastY = MathHelper.Floor(entity.y * 32.0);
        lastZ = MathHelper.Floor(entity.z * 32.0);
        lastYaw = MathHelper.Floor(entity.yaw * 256.0F / 360.0F);
        lastPitch = MathHelper.Floor(entity.pitch * 256.0F / 360.0F);
    }

    public override bool Equals(object obj)
    {
        return obj is EntityTrackerEntry entry && entry.currentTrackedEntity.id == currentTrackedEntity.id;
    }

    public override int GetHashCode()
    {
        return currentTrackedEntity.id;
    }

    public void notifyNewLocation(IEnumerable<ServerPlayerEntity> players)
    {
        newPlayerDataUpdated = false;
        if (!isInitialized || currentTrackedEntity.getSquaredDistance(x, y, z) > 16.0)
        {
            x = currentTrackedEntity.x;
            y = currentTrackedEntity.y;
            z = currentTrackedEntity.z;
            isInitialized = true;
            newPlayerDataUpdated = true;
            updateListeners(players);
        }

        ticksSinceLastDismount++;
        if (++ticks % trackingFrequency == 0)
        {
            int var2 = MathHelper.Floor(currentTrackedEntity.x * 32.0);
            int var3 = MathHelper.Floor(currentTrackedEntity.y * 32.0);
            int var4 = MathHelper.Floor(currentTrackedEntity.z * 32.0);
            int var5 = MathHelper.Floor(currentTrackedEntity.yaw * 256.0F / 360.0F);
            int var6 = MathHelper.Floor(currentTrackedEntity.pitch * 256.0F / 360.0F);
            int var7 = var2 - lastX;
            int var8 = var3 - lastY;
            int var9 = var4 - lastZ;
            object? var10 = null;
            bool var11 = Math.Abs(var2) >= 8 || Math.Abs(var3) >= 8 || Math.Abs(var4) >= 8;
            bool var12 = Math.Abs(var5 - lastYaw) >= 8 || Math.Abs(var6 - lastPitch) >= 8;
            if (var7 < -128 || var7 >= 128 || var8 < -128 || var8 >= 128 || var9 < -128 || var9 >= 128 || ticksSinceLastDismount > 400)
            {
                ticksSinceLastDismount = 0;
                currentTrackedEntity.x = var2 / 32.0;
                currentTrackedEntity.y = var3 / 32.0;
                currentTrackedEntity.z = var4 / 32.0;
                var10 = EntityPositionS2CPacket.Get(currentTrackedEntity.id, var2, var3, var4, (byte)var5, (byte)var6);
            }
            else if (var11 && var12)
            {
                var10 = EntityRotateAndMoveRelativeS2CPacket.Get(currentTrackedEntity.id, (byte)var7, (byte)var8, (byte)var9, (byte)var5, (byte)var6);
            }
            else if (var11)
            {
                var10 = EntityMoveRelativeS2CPacket.Get(currentTrackedEntity.id, (byte)var7, (byte)var8, (byte)var9);
            }
            else if (var12)
            {
                var10 = EntityRotateS2CPacket.Get(currentTrackedEntity.id, (byte)var5, (byte)var6);
            }

            if (alwaysUpdateVelocity)
            {
                double var13 = currentTrackedEntity.velocityX - velocityX;
                double var15 = currentTrackedEntity.velocityY - velocityY;
                double var17 = currentTrackedEntity.velocityZ - velocityZ;
                double var19 = 0.02;
                double var21 = var13 * var13 + var15 * var15 + var17 * var17;
                if (var21 > var19 * var19
                    || var21 > 0.0
                    && currentTrackedEntity.velocityX == 0.0
                    && currentTrackedEntity.velocityY == 0.0
                    && currentTrackedEntity.velocityZ == 0.0)
                {
                    velocityX = currentTrackedEntity.velocityX;
                    velocityY = currentTrackedEntity.velocityY;
                    velocityZ = currentTrackedEntity.velocityZ;
                    sendToListeners(EntityVelocityUpdateS2CPacket.Get(currentTrackedEntity.id, velocityX, velocityY, velocityZ));
                }
            }

            if (var10 != null)
            {
                sendToListeners((Packet)var10);
            }

            DataWatcher var23 = currentTrackedEntity.getDataWatcher();
            if (var23.dirty)
            {
                sendToAround(EntityTrackerUpdateS2CPacket.Get(currentTrackedEntity.id, var23));
            }

            if (var11)
            {
                lastX = var2;
                lastY = var3;
                lastZ = var4;
            }

            if (var12)
            {
                lastYaw = var5;
                lastPitch = var6;
            }
        }

        if (currentTrackedEntity.velocityModified)
        {
            sendToAround(EntityVelocityUpdateS2CPacket.Get(currentTrackedEntity));
            currentTrackedEntity.velocityModified = false;
        }
    }

    public void sendToListeners(Packet packet)
    {
        foreach (var player in listeners)
        {
            player.networkHandler.sendPacket(packet);
        }
        packet.ReturnNoCount();
    }

    public void sendToAround(Packet packet)
    {
        sendToListeners(packet);
        if (currentTrackedEntity is ServerPlayerEntity entity)
        {
            entity.networkHandler.sendPacket(packet);
        }
        packet.ReturnNoCount();
    }

    public void notifyEntityRemoved()
    {
        sendToListeners(EntityDestroyS2CPacket.Get(currentTrackedEntity.id));
    }

    public void notifyEntityRemoved(ServerPlayerEntity player)
    {
            listeners.Remove(player);
    }

    public void updateListener(ServerPlayerEntity player)
    {
        if (player != currentTrackedEntity)
        {
            double var2 = player.x - lastX / 32.0;
            double var4 = player.z - lastZ / 32.0;
            if (var2 >= -trackedDistance && var2 <= trackedDistance && var4 >= -trackedDistance && var4 <= trackedDistance)
            {
                if (!listeners.Contains(player))
                {
                    listeners.Add(player);
                    player.networkHandler.sendPacket(createAddEntityPacket());
                    if (alwaysUpdateVelocity)
                    {
                        player.networkHandler
                            .sendPacket(
                                EntityVelocityUpdateS2CPacket.Get(
                                    currentTrackedEntity.id,
                                    currentTrackedEntity.velocityX,
                                    currentTrackedEntity.velocityY,
                                    currentTrackedEntity.velocityZ
                                )
                            );
                    }

                    ItemStack[] var6 = currentTrackedEntity.getEquipment();
                    if (var6 != null)
                    {
                        for (int var7 = 0; var7 < var6.Length; var7++)
                        {
                            player.networkHandler.sendPacket(EntityEquipmentUpdateS2CPacket.Get(currentTrackedEntity.id, var7, var6[var7]));
                        }
                    }

                    if (currentTrackedEntity is EntityPlayer var8)
                    {
                        if (var8.isSleeping())
                        {
                            player.networkHandler
                                .sendPacket(
                                    PlayerSleepUpdateS2CPacket.Get(
                                        currentTrackedEntity,
                                        0,
                                        MathHelper.Floor(currentTrackedEntity.x),
                                        MathHelper.Floor(currentTrackedEntity.y),
                                        MathHelper.Floor(currentTrackedEntity.z)
                                    )
                                );
                        }
                    }
                }
            }
            else if (listeners.Remove(player))
            {
                player.networkHandler.sendPacket(EntityDestroyS2CPacket.Get(currentTrackedEntity.id));
            }
        }
    }

    public void updateListeners(IEnumerable<ServerPlayerEntity> players)
    {
        foreach (var player in players)
        {
            updateListener(player);
        }
    }

    private Packet createAddEntityPacket()
    {
        if (currentTrackedEntity is EntityItem item)
        {
            var var7 = ItemEntitySpawnS2CPacket.Get(item);
            item.x = var7.x / 32.0;
            item.y = var7.y / 32.0;
            item.z = var7.z / 32.0;
            return var7;
        }
        else if (currentTrackedEntity is ServerPlayerEntity p)
        {
            return PlayerSpawnS2CPacket.Get(p);
        }
        else
        {
            if (currentTrackedEntity is EntityMinecart var1)
            {
                if (var1.type == 0)
                {
                    return EntitySpawnS2CPacket.Get(currentTrackedEntity, 10);
                }

                if (var1.type == 1)
                {
                    return EntitySpawnS2CPacket.Get(currentTrackedEntity, 11);
                }

                if (var1.type == 2)
                {
                    return EntitySpawnS2CPacket.Get(currentTrackedEntity, 12);
                }
            }

            if (currentTrackedEntity is EntityBoat)
            {
                return EntitySpawnS2CPacket.Get(currentTrackedEntity, 1);
            }
            else if (currentTrackedEntity is SpawnableEntity)
            {
                return LivingEntitySpawnS2CPacket.Get((EntityLiving)currentTrackedEntity);
            }
            else if (currentTrackedEntity is EntityFish)
            {
                return EntitySpawnS2CPacket.Get(currentTrackedEntity, 90);
            }
            else if (currentTrackedEntity is EntityArrow arrow)
            {
                EntityLiving arrowOwner = arrow.owner;
                return EntitySpawnS2CPacket.Get(currentTrackedEntity, 60, arrowOwner != null ? arrowOwner.id : currentTrackedEntity.id);
            }
            else if (currentTrackedEntity is EntitySnowball)
            {
                return EntitySpawnS2CPacket.Get(currentTrackedEntity, 61);
            }
            else if (currentTrackedEntity is EntityFireball fireball)
            {
                var packet = EntitySpawnS2CPacket.Get(fireball, 63, fireball.owner.id);
                packet.velocityX = (int)(fireball.powerX * 8000.0);
                packet.velocityY = (int)(fireball.powerY * 8000.0);
                packet.velocityZ = (int)(fireball.powerZ * 8000.0);

                return packet;
            }
            else if (currentTrackedEntity is EntityEgg)
            {
                return EntitySpawnS2CPacket.Get(currentTrackedEntity, 62);
            }
            else if (currentTrackedEntity is EntityTNTPrimed)
            {
                return EntitySpawnS2CPacket.Get(currentTrackedEntity, 50);
            }
            else
            {
                if (currentTrackedEntity is EntityFallingSand var3)
                {
                    if (var3.blockId == Block.Sand.id)
                    {
                        return EntitySpawnS2CPacket.Get(currentTrackedEntity, 70);
                    }

                    if (var3.blockId == Block.Gravel.id)
                    {
                        return EntitySpawnS2CPacket.Get(currentTrackedEntity, 71);
                    }
                }

                if (currentTrackedEntity is EntityPainting painting)
                {
                    return PaintingEntitySpawnS2CPacket.Get(painting);
                }
                else
                {
                    throw new ArgumentException("Don't know how to add " + currentTrackedEntity.GetType() + "!");
                }
            }
        }
    }

    public void removeListener(ServerPlayerEntity player)
    {
        if (listeners.Remove(player))
        {
            player.networkHandler.sendPacket(EntityDestroyS2CPacket.Get(currentTrackedEntity.id));
        }
    }
}
