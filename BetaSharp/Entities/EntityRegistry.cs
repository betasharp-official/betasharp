using System.Diagnostics.CodeAnalysis;
using BetaSharp.NBT;
using BetaSharp.Registries;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

public static class EntityRegistry
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(EntityRegistry));
    private static readonly IRegistry<EntityType> s_registry = DefaultRegistries.EntityTypes;

    public static readonly EntityType Arrow = Register(world => new EntityArrow(world), "Arrow", 10);
    public static readonly EntityType Snowball = Register(world => new EntitySnowball(world), "Snowball", 11);
    public static readonly EntityType Item = Register(world => new EntityItem(world), "Item", 1);
    public static readonly EntityType Painting = Register(world => new EntityPainting(world), "Painting", 9);
    public static readonly EntityType Creeper = Register(world => new EntityCreeper(world), "Creeper", 50);
    public static readonly EntityType Skeleton = Register(world => new EntitySkeleton(world), "Skeleton", 51);
    public static readonly EntityType Spider = Register(world => new EntitySpider(world), "Spider", 52);
    public static readonly EntityType Giant = Register(world => new EntityGiantZombie(world), "Giant", 53);
    public static readonly EntityType Zombie = Register(world => new EntityZombie(world), "Zombie", 54);
    public static readonly EntityType Slime = Register(world => new EntitySlime(world), "Slime", 55);
    public static readonly EntityType Ghast = Register(world => new EntityGhast(world), "Ghast", 56);
    public static readonly EntityType PigZombie = Register(world => new EntityPigZombie(world), "PigZombie", 57);
    public static readonly EntityType Pig = Register(world => new EntityPig(world), "Pig", 90);
    public static readonly EntityType Sheep = Register(world => new EntitySheep(world), "Sheep", 91);
    public static readonly EntityType Cow = Register(world => new EntityCow(world), "Cow", 92);
    public static readonly EntityType Chicken = Register(world => new EntityChicken(world), "Chicken", 93);
    public static readonly EntityType Squid = Register(world => new EntitySquid(world), "Squid", 94);
    public static readonly EntityType Wolf = Register(world => new EntityWolf(world), "Wolf", 95);
    public static readonly EntityType PrimedTnt = Register(world => new EntityTntPrimed(world), "PrimedTnt", 20);
    public static readonly EntityType FallingSand = Register(world => new EntityFallingSand(world), "FallingSand", 21);
    public static readonly EntityType Minecart = Register(world => new EntityMinecart(world), "Minecart", 40);
    public static readonly EntityType Boat = Register(world => new EntityBoat(world), "Boat", 41);

    public static readonly EntityType Egg = Register(world => new EntityEgg(world), "Egg", 62);
    public static readonly EntityType Fireball = Register(world => new EntityFireball(world), "Fireball", 63);
    public static readonly EntityType FishHook = Register(world => new EntityFish(world), "FishHook", 64);
    public static readonly EntityType LightningBolt = Register(world => new EntityLightningBolt(world), "LightningBolt", 65);
    public static readonly EntityType Player = Register<ServerPlayerEntity>(_ => throw new NotSupportedException("Players must be created via ServerPlayerEntity constructor"), "Player", 100);

    static EntityRegistry()
    {
    }

    private static EntityType Register<T>(Func<IWorldContext, T> factory, string id, int rawId) where T : Entity
    {
        EntityType type = new(w => factory(w), typeof(T), id);
        s_registry.Register(rawId, ResourceLocation.Parse(id.ToLower()), type);
        return type;
    }

    public static Entity? Create(string id, IWorldContext world) => TryCreate(id, world, out Entity? entity) ? entity : null;

    public static bool TryCreate(string id, IWorldContext world, [MaybeNullWhen(false)] out Entity entity, EntityType? skip = null)
    {
        EntityType? type = s_registry.Get(ResourceLocation.Parse(id.ToLower()));

        if (type == skip)
        {
            entity = null;
            return false;
        }

        if (type != null)
        {
            entity = type.Create(world);
            return true;
        }

        s_logger.LogInformation($"Unable to find entity with id {id}");
        entity = null;
        return false;
    }

    public static Entity? Create(int rawId, IWorldContext world) => TryCreate(rawId, world, out Entity? entity) ? entity : null;

    public static bool TryCreate(int rawId, IWorldContext world, [MaybeNullWhen(false)] out Entity entity)
    {
        EntityType? type = s_registry.Get(rawId);
        if (type != null)
        {
            entity = type.Create(world);
            return true;
        }

        s_logger.LogInformation($"Unable to find entity with raw id {rawId}");
        entity = null;
        return false;
    }

    public static int GetRawId(Entity entity) => entity.Type != null ? s_registry.GetId(entity.Type) : -1;

    public static string? GetId(Entity entity) => entity.Type != null ? s_registry.GetKey(entity.Type)?.Path : null;

    public static bool TryGetTypeFromName(string name, [MaybeNullWhen(false)] out Type type)
    {
        EntityType? entityType = s_registry.Get(ResourceLocation.Parse(name.ToLower()));
        if (entityType != null)
        {
            type = entityType.BaseType;
            return true;
        }

        type = null;
        return false;
    }

    public static Entity? GetEntityFromNbt(NBTTagCompound nbt, IWorldContext world)
    {
        string id = nbt.GetString("id");
        if (TryCreate(id, world, out Entity? entity, Player))
        {
            entity!.Read(nbt);
        }

        return entity;
    }

    public static Entity? CreateEntityAt(string name, IWorldContext world, float x, float y, float z)
    {
        name = name.ToLower();
        if (TryCreate(name, world, out Entity? entity))
        {
            entity.SetPosition(x, y, z);
            entity.SetPositionAndAngles(x, y, z, 0, 0);
            if (!world.SpawnEntity(entity))
            {
                s_logger.LogError($"Entity `{name}` failed to join world.");
            }

            return entity;
        }

        s_logger.LogError($"Failed to find entity type associated with name `{name}`");
        return null;
    }
}
