using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Util;

namespace BetaSharp.Server.Entities;

public class EntityTracker
{
    private HashSet<EntityTrackerEntry> entries = [];
    private Dictionary<int, EntityTrackerEntry> entriesById = new();
    private MinecraftServer world;
    private int viewDistance;
    private int dimensionId;

    public EntityTracker(MinecraftServer server, int dimensionId)
    {
        world = server;
        this.dimensionId = dimensionId;
        viewDistance = server.PlayerManager.GetBlockViewDistance();
    }

    public void OnEntityAdded(Entity entity)
    {
        if (entity is ServerPlayerEntity)
        {
            StartTracking(entity, 512, 2);
            ServerPlayerEntity var2 = (ServerPlayerEntity)entity;

            foreach (EntityTrackerEntry var4 in entries)
            {
                if (var4.CurrentTrackedEntity != var2)
                {
                    var4.UpdateListener(var2);
                }
            }
        }
        else if (entity is EntityFish)
        {
            StartTracking(entity, 64, 5, true);
        }
        else if (entity is EntityArrow)
        {
            StartTracking(entity, 64, 20, false);
        }
        else if (entity is EntityFireball)
        {
            StartTracking(entity, 64, 10, false);
        }
        else if (entity is EntitySnowball)
        {
            StartTracking(entity, 64, 10, true);
        }
        else if (entity is EntityEgg)
        {
            StartTracking(entity, 64, 10, true);
        }
        else if (entity is EntityItem)
        {
            StartTracking(entity, 64, 20, true);
        }
        else if (entity is EntityMinecart)
        {
            StartTracking(entity, 160, 5, true);
        }
        else if (entity is EntityBoat)
        {
            StartTracking(entity, 160, 5, true);
        }
        else if (entity is EntitySquid)
        {
            StartTracking(entity, 160, 3, true);
        }
        else if (entity is SpawnableEntity)
        {
            StartTracking(entity, 160, 3);
        }
        else if (entity is EntityTNTPrimed)
        {
            StartTracking(entity, 160, 10, true);
        }
        else if (entity is EntityFallingSand)
        {
            StartTracking(entity, 160, 20, true);
        }
        else if (entity is EntityPainting)
        {
            StartTracking(entity, 160, int.MaxValue, false);
        }
    }

    public void StartTracking(Entity entity, int trackedDistance, int tracingFrequency)
    {
        StartTracking(entity, trackedDistance, tracingFrequency, false);
    }

    public void StartTracking(Entity entity, int trackedDistance, int tracingFrequency, bool alwaysUpdateVelocity)
    {
        if (trackedDistance > viewDistance)
        {
            trackedDistance = viewDistance;
        }

        if (entriesById.ContainsKey(entity.id))
        {
            throw new InvalidOperationException("Entity is already tracked!");
        }
        else
        {
            EntityTrackerEntry var5 = new(entity, trackedDistance, tracingFrequency, alwaysUpdateVelocity);
            entries.Add(var5);
            entriesById[entity.id] = var5;
            var5.UpdateListeners(world.GetWorld(dimensionId).Players.Cast<ServerPlayerEntity>());
        }
    }

    public void OnEntityRemoved(Entity entity)
    {
        if (entity is ServerPlayerEntity)
        {
            ServerPlayerEntity var2 = (ServerPlayerEntity)entity;

            foreach (EntityTrackerEntry var4 in entries)
            {
                var4.NotifyEntityRemoved(var2);
            }
        }

        if (entriesById.Remove(entity.id, out EntityTrackerEntry ent))
        {
            entries.Remove(ent);
            ent.NotifyEntityRemoved();
        }
    }

    public void Tick()
    {
        List<ServerPlayerEntity> var1 = [];

        foreach (EntityTrackerEntry var3 in entries)
        {
            var3.NotifyNewLocation(world.GetWorld(dimensionId).Players.Cast<ServerPlayerEntity>());
            if (var3.NewPlayerDataUpdated && var3.CurrentTrackedEntity is ServerPlayerEntity)
            {
                var1.Add((ServerPlayerEntity)var3.CurrentTrackedEntity);
            }
        }

        for (int var6 = 0; var6 < var1.Count; var6++)
        {
            ServerPlayerEntity var7 = var1[var6];

            foreach (EntityTrackerEntry var5 in entries)
            {
                if (var5.CurrentTrackedEntity != var7)
                {
                    var5.UpdateListener(var7);
                }
            }
        }
    }

    public void SendToListeners(Entity entity, Packet packet)
    {
        if (entriesById.TryGetValue(entity.id, out EntityTrackerEntry ent))
        {
            ent.SendToListeners(packet);
        }
    }

    public void SendToAround(Entity entity, Packet packet)
    {
        if (entriesById.TryGetValue(entity.id, out EntityTrackerEntry ent))
        {
            ent.SendToAround(packet);
        }
    }

    public void RemoveListener(ServerPlayerEntity player)
    {
        foreach (EntityTrackerEntry var3 in entries)
        {
            var3.RemoveListener(player);
        }
    }
}
