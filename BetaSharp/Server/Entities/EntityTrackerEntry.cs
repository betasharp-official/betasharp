using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Util.Maths;

namespace BetaSharp.Server.Entities;

internal class EntityTrackerEntry
{
    public Entity CurrentTrackedEntity;
    public int TrackedDistance;
    public int TrackingFrequency;
    public int LastX;
    public int LastY;
    public int LastZ;
    public int LastYaw;
    public int LastPitch;
    public double VelocityX;
    public double VelocityY;
    public double VelocityZ;
    public int Ticks;
    private double x;
    private double y;
    private double z;
    private bool isInitialized;
    private bool alwaysUpdateVelocity;
    private int ticksSinceLastDismount;
    public bool NewPlayerDataUpdated;
    public HashSet<ServerPlayerEntity> Listeners = [];

    public EntityTrackerEntry(Entity entity, int trackedDistance, int trackedFrequency, bool alwaysUpdateVelocity)
    {
        CurrentTrackedEntity = entity;
        this.TrackedDistance = trackedDistance;
        TrackingFrequency = trackedFrequency;
        this.alwaysUpdateVelocity = alwaysUpdateVelocity;
        LastX = MathHelper.Floor(entity.x * 32.0);
        LastY = MathHelper.Floor(entity.y * 32.0);
        LastZ = MathHelper.Floor(entity.z * 32.0);
        LastYaw = MathHelper.Floor(entity.yaw * 256.0F / 360.0F);
        LastPitch = MathHelper.Floor(entity.pitch * 256.0F / 360.0F);
    }

    public override bool Equals(object obj)
    {
        return obj is EntityTrackerEntry entry && entry.CurrentTrackedEntity.id == CurrentTrackedEntity.id;
    }

    public override int GetHashCode()
    {
        return CurrentTrackedEntity.id;
    }

    public void NotifyNewLocation(IEnumerable<ServerPlayerEntity> players)
    {
        NewPlayerDataUpdated = false;
        if (!isInitialized || CurrentTrackedEntity.getSquaredDistance(x, y, z) > 16.0)
        {
            x = CurrentTrackedEntity.x;
            y = CurrentTrackedEntity.y;
            z = CurrentTrackedEntity.z;
            isInitialized = true;
            NewPlayerDataUpdated = true;
            UpdateListeners(players);
        }

        ticksSinceLastDismount++;
        if (++Ticks % TrackingFrequency == 0)
        {
            int encodedX = MathHelper.Floor(CurrentTrackedEntity.x * 32.0);
            int encodedY = MathHelper.Floor(CurrentTrackedEntity.y * 32.0);
            int encodedZ = MathHelper.Floor(CurrentTrackedEntity.z * 32.0);
            int encodedYaw = MathHelper.Floor(CurrentTrackedEntity.yaw * 256.0F / 360.0F);
            int encodedPitch = MathHelper.Floor(CurrentTrackedEntity.pitch * 256.0F / 360.0F);
            int deltaX = encodedX - LastX;
            int deltaY = encodedY - LastY;
            int deltaZ = encodedZ - LastZ;
            object? packet = null;
            bool positionChanged = Math.Abs(encodedX) >= 8 || Math.Abs(encodedY) >= 8 || Math.Abs(encodedZ) >= 8;
            bool rotationChanged = Math.Abs(encodedYaw - LastYaw) >= 8 || Math.Abs(encodedPitch - LastPitch) >= 8;
            if (deltaX < -128 || deltaX >= 128 || deltaY < -128 || deltaY >= 128 || deltaZ < -128 || deltaZ >= 128 || ticksSinceLastDismount > 400)
            {
                ticksSinceLastDismount = 0;
                CurrentTrackedEntity.x = encodedX / 32.0;
                CurrentTrackedEntity.y = encodedY / 32.0;
                CurrentTrackedEntity.z = encodedZ / 32.0;
                packet = new EntityPositionS2CPacket(CurrentTrackedEntity.id, encodedX, encodedY, encodedZ, (byte)encodedYaw, (byte)encodedPitch);
            }
            else if (positionChanged && rotationChanged)
            {
                packet = new EntityRotateAndMoveRelativeS2CPacket(CurrentTrackedEntity.id, (byte)deltaX, (byte)deltaY, (byte)deltaZ, (byte)encodedYaw, (byte)encodedPitch);
            }
            else if (positionChanged)
            {
                packet = new EntityMoveRelativeS2CPacket(CurrentTrackedEntity.id, (byte)deltaX, (byte)deltaY, (byte)deltaZ);
            }
            else if (rotationChanged)
            {
                packet = new EntityRotateS2CPacket(CurrentTrackedEntity.id, (byte)encodedYaw, (byte)encodedPitch);
            }

            if (alwaysUpdateVelocity)
            {
                double velDeltaX = CurrentTrackedEntity.velocityX - VelocityX;
                double velDeltaY = CurrentTrackedEntity.velocityY - VelocityY;
                double velDeltaZ = CurrentTrackedEntity.velocityZ - VelocityZ;
                double velocityThreshold = 0.02;
                double squaredVelocityDelta = velDeltaX * velDeltaX + velDeltaY * velDeltaY + velDeltaZ * velDeltaZ;
                if (squaredVelocityDelta > velocityThreshold * velocityThreshold
                    || squaredVelocityDelta > 0.0
                    && CurrentTrackedEntity.velocityX == 0.0
                    && CurrentTrackedEntity.velocityY == 0.0
                    && CurrentTrackedEntity.velocityZ == 0.0)
                {
                    VelocityX = CurrentTrackedEntity.velocityX;
                    VelocityY = CurrentTrackedEntity.velocityY;
                    VelocityZ = CurrentTrackedEntity.velocityZ;
                    SendToListeners(new EntityVelocityUpdateS2CPacket(CurrentTrackedEntity.id, VelocityX, VelocityY, VelocityZ));
                }
            }

            if (packet != null)
            {
                SendToListeners((Packet)packet);
            }

            DataWatcher dataWatcher = CurrentTrackedEntity.getDataWatcher();
            if (dataWatcher.dirty)
            {
                SendToAround(new EntityTrackerUpdateS2CPacket(CurrentTrackedEntity.id, dataWatcher));
            }

            if (positionChanged)
            {
                LastX = encodedX;
                LastY = encodedY;
                LastZ = encodedZ;
            }

            if (rotationChanged)
            {
                LastYaw = encodedYaw;
                LastPitch = encodedPitch;
            }
        }

        if (CurrentTrackedEntity.velocityModified)
        {
            SendToAround(new EntityVelocityUpdateS2CPacket(CurrentTrackedEntity));
            CurrentTrackedEntity.velocityModified = false;
        }
    }

    public void SendToListeners(Packet packet)
    {
        foreach (var player in Listeners)
        {
            player.networkHandler.SendPacket(packet);
        }
    }

    public void SendToAround(Packet packet)
    {
        SendToListeners(packet);
        if (CurrentTrackedEntity is ServerPlayerEntity entity)
        {
            entity.networkHandler.SendPacket(packet);
        }
    }

    public void NotifyEntityRemoved()
    {
        SendToListeners(new EntityDestroyS2CPacket(CurrentTrackedEntity.id));
    }

    public void NotifyEntityRemoved(ServerPlayerEntity player)
    {
        if (Listeners.Contains(player))
        {
            Listeners.Remove(player);
        }
    }

    public void UpdateListener(ServerPlayerEntity player)
    {
        if (player != CurrentTrackedEntity)
        {
            double playerDeltaX = player.x - LastX / 32;
            double playerDeltaZ = player.z - LastZ / 32;
            if (playerDeltaX >= -TrackedDistance && playerDeltaX <= TrackedDistance && playerDeltaZ >= -TrackedDistance && playerDeltaZ <= TrackedDistance)
            {
                if (!Listeners.Contains(player))
                {
                    Listeners.Add(player);
                    player.networkHandler.SendPacket(createAddEntityPacket());
                    if (alwaysUpdateVelocity)
                    {
                        player.networkHandler
                            .SendPacket(
                                new EntityVelocityUpdateS2CPacket(
                                    CurrentTrackedEntity.id,
                                    CurrentTrackedEntity.velocityX,
                                    CurrentTrackedEntity.velocityY,
                                    CurrentTrackedEntity.velocityZ
                                )
                            );
                    }

                    ItemStack[] equipment = CurrentTrackedEntity.getEquipment();
                    if (equipment != null)
                    {
                        for (int i = 0; i < equipment.Length; i++)
                        {
                            player.networkHandler.SendPacket(new EntityEquipmentUpdateS2CPacket(CurrentTrackedEntity.id, i, equipment[i]));
                        }
                    }

                    if (CurrentTrackedEntity is EntityPlayer entityPlayer)
                    {
                        if (entityPlayer.isSleeping())
                        {
                            player.networkHandler
                                .SendPacket(
                                    new PlayerSleepUpdateS2CPacket(
                                        CurrentTrackedEntity,
                                        0,
                                        MathHelper.Floor(CurrentTrackedEntity.x),
                                        MathHelper.Floor(CurrentTrackedEntity.y),
                                        MathHelper.Floor(CurrentTrackedEntity.z)
                                    )
                                );
                        }
                    }
                }
            }
            else if (Listeners.Contains(player))
            {
                Listeners.Remove(player);
                player.networkHandler.SendPacket(new EntityDestroyS2CPacket(CurrentTrackedEntity.id));
            }
        }
    }

    public void UpdateListeners(IEnumerable<ServerPlayerEntity> players)
    {
        foreach (var player in players)
        {
            UpdateListener(player);
        }
    }

    private Packet createAddEntityPacket()
    {
        if (CurrentTrackedEntity is EntityItem var6)
        {
            ItemEntitySpawnS2CPacket var7 = new(var6);
            var6.x = var7.x / 32.0;
            var6.y = var7.y / 32.0;
            var6.z = var7.z / 32.0;
            return var7;
        }
        else if (CurrentTrackedEntity is ServerPlayerEntity p)
        {
            return new PlayerSpawnS2CPacket(p);
        }
        else
        {
            if (CurrentTrackedEntity is EntityMinecart var1)
            {
                if (var1.type == 0)
                {
                    return new EntitySpawnS2CPacket(CurrentTrackedEntity, 10);
                }

                if (var1.type == 1)
                {
                    return new EntitySpawnS2CPacket(CurrentTrackedEntity, 11);
                }

                if (var1.type == 2)
                {
                    return new EntitySpawnS2CPacket(CurrentTrackedEntity, 12);
                }
            }

            if (CurrentTrackedEntity is EntityBoat)
            {
                return new EntitySpawnS2CPacket(CurrentTrackedEntity, 1);
            }
            else if (CurrentTrackedEntity is SpawnableEntity)
            {
                return new LivingEntitySpawnS2CPacket((EntityLiving)CurrentTrackedEntity);
            }
            else if (CurrentTrackedEntity is EntityFish)
            {
                return new EntitySpawnS2CPacket(CurrentTrackedEntity, 90);
            }
            else if (CurrentTrackedEntity is EntityArrow arrow)
            {
                EntityLiving var5 = arrow.owner;
                return new EntitySpawnS2CPacket(CurrentTrackedEntity, 60, var5 != null ? var5.id : CurrentTrackedEntity.id);
            }
            else if (CurrentTrackedEntity is EntitySnowball)
            {
                return new EntitySpawnS2CPacket(CurrentTrackedEntity, 61);
            }
            else if (CurrentTrackedEntity is EntityFireball var4)
            {
                EntitySpawnS2CPacket var2 = new(CurrentTrackedEntity, 63, ((EntityFireball)CurrentTrackedEntity).owner.id)
                {
                    velocityX = (int)(var4.powerX * 8000.0),
                    velocityY = (int)(var4.powerY * 8000.0),
                    velocityZ = (int)(var4.powerZ * 8000.0)
                };
                return var2;
            }
            else if (CurrentTrackedEntity is EntityEgg)
            {
                return new EntitySpawnS2CPacket(CurrentTrackedEntity, 62);
            }
            else if (CurrentTrackedEntity is EntityTNTPrimed)
            {
                return new EntitySpawnS2CPacket(CurrentTrackedEntity, 50);
            }
            else
            {
                if (CurrentTrackedEntity is EntityFallingSand var3)
                {
                    if (var3.blockId == Block.Sand.id)
                    {
                        return new EntitySpawnS2CPacket(CurrentTrackedEntity, 70);
                    }

                    if (var3.blockId == Block.Gravel.id)
                    {
                        return new EntitySpawnS2CPacket(CurrentTrackedEntity, 71);
                    }
                }

                if (CurrentTrackedEntity is EntityPainting painting)
                {
                    return new PaintingEntitySpawnS2CPacket(painting);
                }
                else
                {
                    throw new ArgumentException("Don't know how to add " + CurrentTrackedEntity.GetType() + "!");
                }
            }
        }
    }

    public void RemoveListener(ServerPlayerEntity player)
    {
        if (Listeners.Contains(player))
        {
            Listeners.Remove(player);
            player.networkHandler.SendPacket(new EntityDestroyS2CPacket(CurrentTrackedEntity.id));
        }
    }
}
