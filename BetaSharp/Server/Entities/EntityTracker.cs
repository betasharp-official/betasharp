using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Util;
using java.lang;
using java.util;

namespace BetaSharp.Server.Entities;

public class EntityTracker
{
    private HashSet<EntityTrackerEntry> entries = [];
    private Dictionary<int, EntityTrackerEntry> entriesById = new();
    private BetaSharpServer world;
    private int viewDistance;
    private int dimensionId;

    public EntityTracker(BetaSharpServer server, int dimensionId)
    {
        world = server;
        this.dimensionId = dimensionId;
        viewDistance = server.playerManager.getBlockViewDistance();
    }

    public void onEntityAdded(Entity entity)
    {
        if (entity is ServerPlayerEntity)
        {
            startTracking(entity, 512, 2);
            ServerPlayerEntity var2 = (ServerPlayerEntity)entity;

            foreach (EntityTrackerEntry var4 in entries)
            {
                if (var4.currentTrackedEntity != var2)
                {
                    var4.updateListener(var2);
                }
            }
        }
        else if (entity is EntityFish)
        {
            startTracking(entity, 64, 5, true);
        }
        else if (entity is EntityArrow)
        {
            startTracking(entity, 64, 20, false);
        }
        else if (entity is EntityFireball)
        {
            startTracking(entity, 64, 10, false);
        }
        else if (entity is EntitySnowball)
        {
            startTracking(entity, 64, 10, true);
        }
        else if (entity is EntityEgg)
        {
            startTracking(entity, 64, 10, true);
        }
        else if (entity is EntityItem)
        {
            startTracking(entity, 64, 20, true);
        }
        else if (entity is EntityMinecart)
        {
            startTracking(entity, 160, 5, true);
        }
        else if (entity is EntityBoat)
        {
            startTracking(entity, 160, 5, true);
        }
        else if (entity is EntitySquid)
        {
            startTracking(entity, 160, 3, true);
        }
        else if (entity is SpawnableEntity)
        {
            startTracking(entity, 160, 3);
        }
        else if (entity is EntityTNTPrimed)
        {
            startTracking(entity, 160, 10, true);
        }
        else if (entity is EntityFallingSand)
        {
            startTracking(entity, 160, 20, true);
        }
        else if (entity is EntityPainting)
        {
            startTracking(entity, 160, Integer.MAX_VALUE, false);
        }
    }

    public void startTracking(Entity entity, int trackedDistance, int tracingFrequency)
    {
        startTracking(entity, trackedDistance, tracingFrequency, false);
    }

    public void startTracking(Entity entity, int trackedDistance, int tracingFrequency, bool alwaysUpdateVelocity)
    {
        if (trackedDistance > viewDistance)
        {
            trackedDistance = viewDistance;
        }

        if (entriesById.ContainsKey(entity.id))
        {
            throw new IllegalStateException("Entity is already tracked!");
        }
        else
        {
            EntityTrackerEntry var5 = new(entity, trackedDistance, tracingFrequency, alwaysUpdateVelocity);
            entries.Add(var5);
            entriesById[entity.id] = var5;
            var5.updateListeners(world.getWorld(dimensionId).players.Cast<ServerPlayerEntity>());
        }
    }

    public void onEntityRemoved(Entity entity)
    {
        if (entity is ServerPlayerEntity)
        {
            ServerPlayerEntity var2 = (ServerPlayerEntity)entity;

            foreach (EntityTrackerEntry var4 in entries)
            {
                var4.notifyEntityRemoved(var2);
            }
        }

        if (entriesById.Remove(entity.id, out EntityTrackerEntry ent))
        {
            entries.Remove(ent);
            ent.notifyEntityRemoved();
        }
    }

    public void tick()
    {
        ArrayList var1 = new();

        foreach (EntityTrackerEntry var3 in entries)
        {
            var3.notifyNewLocation(world.getWorld(dimensionId).players.Cast<ServerPlayerEntity>());
            if (var3.newPlayerDataUpdated && var3.currentTrackedEntity is ServerPlayerEntity)
            {
                var1.add((ServerPlayerEntity)var3.currentTrackedEntity);
            }
        }

        for (int var6 = 0; var6 < var1.size(); var6++)
        {
            ServerPlayerEntity var7 = (ServerPlayerEntity)var1.get(var6);

            foreach (EntityTrackerEntry var5 in entries)
            {
                if (var5.currentTrackedEntity != var7)
                {
                    var5.updateListener(var7);
                }
            }
        }
    }

    public void sendToListeners(Entity entity, Packet packet)
    {
        if (entriesById.TryGetValue(entity.id, out EntityTrackerEntry ent))
        {
            ent.sendToListeners(packet);
        }
        packet.ReturnNoCount();
    }

    public void sendToAround(Entity entity, Packet packet)
    {
        if (entriesById.TryGetValue(entity.id, out EntityTrackerEntry ent))
        {
            ent.sendToAround(packet);
        }
        packet.ReturnNoCount();
    }

    public void removeListener(ServerPlayerEntity player)
    {
        foreach (EntityTrackerEntry var3 in entries)
        {
            var3.removeListener(player);
        }
    }
}
