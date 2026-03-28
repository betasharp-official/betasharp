using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Util.Maths;

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
        if (entity is ServerPlayerEntity player)
        {
            startTracking(entity, 512, 2);

            foreach (EntityTrackerEntry tracker in entries)
            {
                if (tracker.currentTrackedEntity != player)
                {
                    tracker.updateListener(player);
                }
            }
        }
        else if (entity is EntityFish)
        {
            startTracking(entity, 64, 5, true);
        }
        else if (entity is EntityArrow)
        {
            // There's no client side physics simulation so we need to updat often
            // modern versions actually update every tick.
            startTracking(entity, 64, 2, true);
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
            startTracking(entity, 160, int.MaxValue, false);
        }
    }

    public void startTracking(Entity entity, int trackedDistance, int tracingFrequency, bool alwaysUpdateVelocity = false)
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
            var5.updateListeners(world.getWorld(dimensionId).Entities.Players.Cast<ServerPlayerEntity>());
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
        List<ServerPlayerEntity> players = [];

        foreach (EntityTrackerEntry tracker in entries)
        {
            tracker.notifyNewLocation(world.getWorld(dimensionId).Entities.Players.Cast<ServerPlayerEntity>());
            if (tracker.newPlayerDataUpdated && tracker.currentTrackedEntity is ServerPlayerEntity player)
            {
                players.Add(player);
            }
        }

        foreach (var player in players)
        {
            foreach (EntityTrackerEntry tracker in entries)
            {
                if (tracker.currentTrackedEntity != player)
                {
                    tracker.updateListener(player);
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
        else
        {
            packet.Return();
        }
    }

    public void sendToAround(Entity entity, Packet packet)
    {
        if (entriesById.TryGetValue(entity.id, out EntityTrackerEntry ent))
        {
            ent.sendToAround(packet);
        }
        else
        {
            packet.Return();
        }
    }

    public void updateListenerForChunk(ServerPlayerEntity player, int chunkX, int chunkZ)
    {
        foreach (EntityTrackerEntry tracker in entries)
        {
            Entity entity = tracker.currentTrackedEntity;
            if (entity != player
                && !entity.dead
                && MathHelper.Floor(entity.x / 16.0) == chunkX
                && MathHelper.Floor(entity.z / 16.0) == chunkZ)
            {
                tracker.updateListener(player);
            }
        }
    }

    public void removeListener(ServerPlayerEntity player)
    {
        foreach (EntityTrackerEntry var3 in entries)
        {
            var3.removeListener(player);
        }
    }
}
