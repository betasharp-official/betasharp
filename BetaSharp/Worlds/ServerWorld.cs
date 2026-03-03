using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Chunks.Storage;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Worlds;

public class ServerWorld : World
{
    public ServerChunkCache chunkCache;
    public bool bypassSpawnProtection = false;
    public bool savingDisabled;
    private readonly MinecraftServer server;
    private readonly Dictionary<int, Entity> entitiesById = [];

    public ServerWorld(MinecraftServer server, IWorldStorage storage, String name, int dimensionId, long seed) : base(storage, name, seed, Dimension.FromId(dimensionId))
    {
        this.server = server;
    }


    public override void updateEntity(Entity entity, bool requireLoaded)
    {
        if (!server.spawnAnimals && (entity is EntityAnimal || entity is EntityWaterMob))
        {
            entity.markDead();
        }

        if (entity.passenger == null || entity.passenger is not EntityPlayer)
        {
            base.updateEntity(entity, requireLoaded);
        }
    }

    public void tickVehicle(Entity vehicle, bool requireLoaded)
    {
        base.updateEntity(vehicle, requireLoaded);
    }


    protected override ChunkSource CreateChunkCache()
    {
        IChunkStorage var1 = Storage.GetChunkStorage(dimension);
        chunkCache = new ServerChunkCache(this, var1, dimension.CreateChunkGenerator());
        return chunkCache;
    }

    public List<BlockEntity> getBlockEntities(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        List<BlockEntity> var7 = [];

        for (int var8 = 0; var8 < blockEntities.Count; var8++)
        {
            BlockEntity var9 = blockEntities[var8];
            if (var9.X >= minX && var9.Y >= minY && var9.Z >= minZ && var9.X < maxX && var9.Y < maxY && var9.Z < maxZ)
            {
                var7.Add(var9);
            }
        }

        return var7;
    }


    public override bool canInteract(EntityPlayer player, int x, int y, int z)
    {
        int var5 = (int)MathHelper.Abs(x - Properties.SpawnX);
        int var6 = (int)MathHelper.Abs(z - Properties.SpawnZ);
        if (var5 > var6)
        {
            var6 = var5;
        }

        return var6 > 16 || server.playerManager.isOperator(player.name) || server is InternalServer;
    }


    protected override void NotifyEntityAdded(Entity entity)
    {
        base.NotifyEntityAdded(entity);
        entitiesById.Add(entity.id, entity);
    }


    protected override void NotifyEntityRemoved(Entity entity)
    {
        base.NotifyEntityRemoved(entity);
        entitiesById.Remove(entity.id);
    }

    public Entity getEntity(int id)
    {
        entitiesById.TryGetValue(id, out Entity? entity);
        return entity;
    }


    public override bool spawnGlobalEntity(Entity entity)
    {
        if (base.spawnGlobalEntity(entity))
        {
            server.playerManager.sendToAround(entity.x, entity.y, entity.z, 512.0, dimension.Id, new GlobalEntitySpawnS2CPacket(entity));
            return true;
        }
        else
        {
            return false;
        }
    }


    public override void broadcastEntityEvent(Entity entity, byte @event)
    {
        EntityStatusS2CPacket var3 = new EntityStatusS2CPacket(entity.id, @event);
        server.getEntityTracker(dimension.Id).sendToAround(entity, var3);
    }


    public override Explosion createExplosion(Entity source, double x, double y, double z, float power, bool fire)
    {
        Explosion var10 = new Explosion(this, source, x, y, z, power)
        {
            isFlaming = fire
        };
        var10.doExplosionA();
        var10.doExplosionB(false);
        server.playerManager.sendToAround(x, y, z, 64.0, dimension.Id, new ExplosionS2CPacket(x, y, z, power, var10.destroyedBlockPositions));
        return var10;
    }


    public override void playNoteBlockActionAt(int x, int y, int z, int soundType, int pitch)
    {
        base.playNoteBlockActionAt(x, y, z, soundType, pitch);
        server.playerManager.sendToAround(x, y, z, 64.0, dimension.Id, new PlayNoteSoundS2CPacket(x, y, z, soundType, pitch));
    }

    public void forceSave()
    {
        Storage.ForceSave();
    }


    protected override void UpdateWeatherCycles()
    {
        bool raining = isRaining();
        base.UpdateWeatherCycles();
        if (raining != isRaining())
        {
            if (raining)
            {
                server.playerManager.sendToAll(new GameStateChangeS2CPacket(2));
            }
            else
            {
                server.playerManager.sendToAll(new GameStateChangeS2CPacket(1));
            }
        }
    }
}
