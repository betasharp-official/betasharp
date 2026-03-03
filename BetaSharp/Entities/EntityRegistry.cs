using System.Diagnostics.CodeAnalysis;
using BetaSharp.NBT;
using BetaSharp.Worlds;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

public static class EntityRegistry
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(EntityRegistry));
    private static readonly Dictionary<string, Func<World, Entity>> idToFactory = new ();
    private static readonly Dictionary<Type, string> typeToId = new ();
    private static readonly Dictionary<int, Func<World, Entity>> rawIdToFactory = new ();
    private static readonly Dictionary<Type, int> typeToRawId = new ();

    public static readonly Dictionary<string, int> namesToId = new();

    private static void Register<T>(Func<World, T> factory, string id, int rawId) where T : Entity
    {
        idToFactory.Add(id, factory);
        typeToId.Add(typeof(T), id);
        rawIdToFactory.Add(rawId, factory);
        typeToRawId.Add(typeof(T), rawId);
        namesToId.TryAdd(id.ToLower(), rawId);
    }

    public static Entity? Create(string id, World world)
    {
	    TryCreate(id, world, out Entity? entity);
	    return entity;
    }

    private static bool TryCreate(string id, World world, [MaybeNullWhen(false)] out Entity entity)
    {
	    if (idToFactory.TryGetValue(id, out var factory))
	    {
		    entity = factory.Invoke(world);
		    return true;
	    }

        s_logger.LogInformation($"Unable to find entity with id {id}");
	    entity = null;
	    return false;
    }

    /// <summary>
    /// Performed by first getting the id from a name, then getting by value in the typeToRawId dir.
    /// If no name is found, try to find by type name.
    /// As this is only used for the get command, the performance is not important enough to warrant a new registry.
    /// </summary>
    /// <param name="name">Entity or type name</param>
    /// <param name="type">Type of the found Entity</param>
    internal static bool TryGetTypeFromName(string name, [NotNullWhen(true)] out Type? type)
    {
        name = name.ToLower();
        if (!namesToId.TryGetValue(name, out int v)) return TryGetTypeFromTypeName(name, out type);

        foreach (KeyValuePair<Type, int> i in typeToRawId)
        {
            if (i.Value == v)
            {
                type = i.Key;
                return true;
            }
        }

        return TryGetTypeFromTypeName(name, out type);
    }

    private static bool TryGetTypeFromTypeName(string name, [NotNullWhen(true)] out Type? type)
    {
        type = null;

        foreach (KeyValuePair<Type, int> i in typeToRawId)
        {
            if (i.Key.ToString().ToLower() == name)
            {
                type = i.Key;
                return true;
            }
        }

        return false;
    }

    public static Entity? getEntityFromNbt(NBTTagCompound nbt, World world)
    {
	    string id = nbt.GetString("id");
        if (TryCreate(id, world, out Entity? entity))
        {
	        entity!.read(nbt);
        }

        return entity;
    }

    public static Entity? createEntityAt(string name, World world, float x, float y, float z)
    {
        name = name.ToLower();
        try
        {
            if (namesToId.TryGetValue(name, out int id))
            {
				if(TryCreate(id, world, out Entity? entity))
                {
                    entity.setPosition(x, y, z);
                    entity.setPositionAndAngles(x, y, z, 0, 0);
                    if (!world.SpawnEntity(entity))
                    {
                        s_logger.LogError($"Entity `{name}` with ID:`{id}` failed to join world.");
                    }

                    return entity;
                }
                else
                {
                    s_logger.LogError($"Failed to convert entity of name `{name}` and id `{id}` to a class.");
                }
            }
            else
            {
                s_logger.LogError($"Unable to find entity of name `{name}`.");
            }
        }
        catch (Exception ex)
        {
            s_logger.LogError(ex, "Failure while creating an entity");
        }

        return null;
    }

    public static Entity? Create(int rawId, World world)
    {
	    TryCreate(rawId, world, out Entity? entity);
	    return entity;
    }

    private static bool TryCreate(int rawId, World world, [MaybeNullWhen(false)] out Entity entity)
    {
	    if (rawIdToFactory.TryGetValue(rawId, out var factory))
	    {
		    entity = factory.Invoke(world);
		    return true;
	    }

        s_logger.LogInformation($"Unable to find entity with raw id {rawId}");
	    entity = null;
	    return false;
    }

    public static int GetRawId(Entity entity)
    {
        return typeToRawId[entity.GetType()];
    }

    public static string? GetId(Entity entity)
    {
        typeToId.TryGetValue(entity.GetType(), out string? id);
        return id;
    }

    static EntityRegistry()
    {
        Register(world => new EntityArrow(world), "Arrow", 10);
        Register(world => new EntitySnowball(world), "Snowball", 11);
        Register(world => new EntityItem(world), "Item", 1);
        Register(world => new EntityPainting(world), "Painting", 9);
        Register(world => new EntityLiving(world), "Mob", 48);
        Register(world => new EntityMonster(world), "Monster", 49);
        Register(world => new EntityCreeper(world), "Creeper", 50);
        Register(world => new EntitySkeleton(world), "Skeleton", 51);
        Register(world => new EntitySpider(world), "Spider", 52);
        Register(world => new EntityGiantZombie(world), "Giant", 53);
        Register(world => new EntityZombie(world), "Zombie", 54);
        Register(world => new EntitySlime(world), "Slime", 55);
        Register(world => new EntityGhast(world), "Ghast", 56);
        Register(world => new EntityPigZombie(world), "PigZombie", 57);
        Register(world => new EntityPig(world), "Pig", 90);
        Register(world => new EntitySheep(world), "Sheep", 91);
        Register(world => new EntityCow(world), "Cow", 92);
        Register(world => new EntityChicken(world), "Chicken", 93);
        Register(world => new EntitySquid(world), "Squid", 94);
        Register(world => new EntityWolf(world), "Wolf", 95);
        Register(world => new EntityTNTPrimed(world), "PrimedTnt", 20);
        Register(world => new EntityFallingSand(world), "FallingSand", 21);
        Register(world => new EntityMinecart(world), "Minecart", 40);
        Register(world => new EntityBoat(world), "Boat", 41);
    }
}
