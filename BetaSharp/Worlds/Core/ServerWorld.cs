using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using BetaSharp.Worlds.Storage.RegionFormat;

namespace BetaSharp.Worlds.Core;

public class ServerWorld : World
{
    private readonly Dictionary<int, Entity> entitiesById = [];
    private readonly BetaSharpServer server;
    public bool bypassSpawnProtection = false;
    public ServerChunkCache chunkCache;
    public bool savingDisabled;

    public ServerWorld(BetaSharpServer server, IWorldStorage storage, string name, int dimensionId, long seed) : base(storage, name, seed, Dimension.FromId(dimensionId))
    {
        this.server = server;

        // Wire up all our decoupled managers!
        Environment.OnRainingStateChanged += HandleWeatherChanged;

        Entities.OnEntityAdded += HandleEntityAdded;
        Entities.OnEntityRemoved += HandleEntityRemoved;
        Entities.OnEntityUpdating += HandleEntityUpdating;
        Entities.OnGlobalEntityAdded += HandleGlobalEntityAdded;
    }

    protected override ChunkSource CreateChunkCache()
    {
        IChunkStorage chunkStorage = Storage.GetChunkStorage(Dimension);
        chunkCache = new ServerChunkCache(this, chunkStorage, Dimension.CreateChunkGenerator());
        return chunkCache;
    }

    // --- Entity Event Handlers (Replacing the old overrides) ---

    private void HandleEntityAdded(Entity entity) => entitiesById.TryAdd(entity.id, entity);

    private void HandleEntityRemoved(Entity entity) => entitiesById.Remove(entity.id);

    private void HandleGlobalEntityAdded(Entity entity) => server.playerManager.sendToAround(entity.x, entity.y, entity.z, 512.0, Dimension.Id, new GlobalEntitySpawnS2CPacket(entity));

    private bool HandleEntityUpdating(Entity entity)
    {
        // 1. Cull animals if the server properties say so
        if (!server.spawnAnimals && (entity is EntityAnimal || entity is EntityWaterMob))
        {
            entity.markDead();
            return false; // Cancel tick
        }

        // 2. If a player is riding this vehicle, the client handles movement, so skip server tick!
        if (entity.passenger != null && entity.passenger is EntityPlayer)
        {
            return false; // Cancel tick
        }

        return true; // Allow normal ticking
    }

    public Entity getEntity(int id)
    {
        entitiesById.TryGetValue(id, out Entity? entity);
        return entity;
    }

    // --- standard World/Server methods below ---

    public List<BlockEntity> getBlockEntities(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        List<BlockEntity> var7 = [];

        for (int var8 = 0; var8 < Entities.BlockEntities.Count; var8++)
        {
            BlockEntity var9 = Entities.BlockEntities[var8];
            if (var9.X >= minX && var9.Y >= minY && var9.Z >= minZ && var9.X < maxX && var9.Y < maxY && var9.Z < maxZ)
            {
                var7.Add(var9);
            }
        }

        return var7;
    }

    public override bool CanInteract(EntityPlayer player, int x, int y, int z)
    {
        int var5 = (int)MathHelper.Abs(x - Properties.SpawnX);
        int var6 = (int)MathHelper.Abs(z - Properties.SpawnZ);
        if (var5 > var6)
        {
            var6 = var5;
        }

        return var6 > 16 || server.playerManager.isOperator(player.name) || server is InternalServer;
    }

    public override void BroadcastEntityEvent(Entity entity, byte @event)
    {
        EntityStatusS2CPacket var3 = new(entity.id, @event);
        server.getEntityTracker(Dimension.Id).sendToAround(entity, var3);
    }

    public override Explosion CreateExplosion(Entity source, double x, double y, double z, float power, bool fire)
    {
        Explosion var10 = new(this, source, x, y, z, power) { isFlaming = fire };
        var10.doExplosionA();
        var10.doExplosionB(false);
        server.playerManager.sendToAround(x, y, z, 64.0, Dimension.Id, new ExplosionS2CPacket(x, y, z, power, var10.destroyedBlockPositions));
        return var10;
    }

    public override void PlayNoteBlockActionAt(int x, int y, int z, int soundType, int pitch)
    {
        base.PlayNoteBlockActionAt(x, y, z, soundType, pitch);
        server.playerManager.sendToAround(x, y, z, 64.0, Dimension.Id, new PlayNoteSoundS2CPacket(x, y, z, soundType, pitch));
    }

    public void forceSave() => Storage.ForceSave();

    private void HandleWeatherChanged(bool isRaining)
    {
        server.playerManager.sendToAll(
            isRaining ? new GameStateChangeS2CPacket(1) : new GameStateChangeS2CPacket(2)
        );
        bool isThundering = Properties.IsThundering;
        server.playerManager.sendToAll(new GameStateChangeS2CPacket(isThundering ? 7 : 8));
    }
}
