using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using BetaSharp.Worlds.Storage.RegionFormat;

namespace BetaSharp.Worlds.Core;

public class ServerWorld : World
{
    private readonly Dictionary<int, Entity> entitiesById = [];
    private readonly BetaSharpServer server;
    public bool BypassSpawnProtection { get; }
    public ServerChunkCache ChunkCache;
    internal ChunkMap ChunkMap;
    public bool savingDisabled;

    public ServerWorld(BetaSharpServer server, IWorldStorage storage, string saveName, int dimensionId, WorldSettings settings, ServerWorld del) : base(storage, saveName, settings, Dimension.FromId(dimensionId))
    {
        this.server = server;
        BypassSpawnProtection = dimensionId != 0;

        Environment.OnRainingStateChanged += HandleWeatherChanged;

        Entities.OnEntityAdded += HandleEntityAdded;
        Entities.OnEntityRemoved += HandleEntityRemoved;
        Entities.OnEntityUpdating += HandleEntityUpdating;
        Entities.OnGlobalEntityAdded += HandleGlobalEntityAdded;
    }

    protected override IChunkSource CreateChunkCache()
    {
        IChunkStorage? chunkStorage = Storage.GetChunkStorage(Dimension);
        ChunkCache = new ServerChunkCache(this, chunkStorage, Dimension.CreateChunkGenerator());
        return ChunkCache;
    }

    private void HandleEntityAdded(Entity entity) => entitiesById.TryAdd(entity.ID, entity);

    private void HandleEntityRemoved(Entity entity) => entitiesById.Remove(entity.ID);

    private void HandleGlobalEntityAdded(Entity entity) => server.playerManager.sendToAround(entity.X, entity.Y, entity.Z, 512.0, Dimension.Id, GlobalEntitySpawnS2CPacket.Get(entity));

    private bool HandleEntityUpdating(Entity entity)
    {
        if (!server.spawnAnimals && (entity is EntityAnimal || entity is EntityWaterMob))
        {
            entity.MarkDead();
            return false;
        }

        if (entity.Passenger != null && entity.Passenger is EntityPlayer)
        {
            return false;
        }

        return true;
    }

    public Entity? getEntity(int id)
    {
        entitiesById.TryGetValue(id, out Entity? entity);
        return entity;
    }

    public List<BlockEntity> getBlockEntities(int minX, int minY, int minZ, int maxX, int maxY, int maxZ) =>
        Entities.BlockEntities
            .Where(b => b.X >= minX && b.Y >= minY && b.Z >= minZ && b.X < maxX && b.Y < maxY && b.Z < maxZ)
            .ToList();

    public override bool CanInteract(EntityPlayer player, int x, int y, int z)
    {
        int absX = Math.Abs(x - Properties.SpawnX);
        int absZ = Math.Abs(z - Properties.SpawnZ);
        return absX > 16 || absZ > 16 || server.playerManager.isOperator(player.Name) || server is InternalServer;
    }

    public override Explosion CreateExplosion(Entity? source, double x, double y, double z, float power, bool fire)
    {
        Explosion explosion = new(this, source, x, y, z, power)
        {
            isFlaming = fire
        };
        explosion.doExplosionA();
        explosion.doExplosionB(false);
        server.playerManager.sendToAround(x, y, z, 64.0, Dimension.Id, ExplosionS2CPacket.Get(x, y, z, power, explosion.destroyedBlockPositions));
        return explosion;
    }

    public void forceSave() => Storage.ForceSave();

    private void HandleWeatherChanged(bool isRaining)
    {
        server.playerManager.sendToAll(
            isRaining ? GameStateChangeS2CPacket.Get(1) : GameStateChangeS2CPacket.Get(2)
        );

        bool isThundering = Properties.IsThundering;
        server.playerManager.sendToAll(GameStateChangeS2CPacket.Get(isThundering ? 7 : 8));
    }
}
