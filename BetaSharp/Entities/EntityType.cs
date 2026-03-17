using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public class EntityType(Func<World, Entity> factory, Type baseType)
{
    private readonly Func<World, Entity> _factory = factory;

    public Type BaseType { get; } = baseType;

    public Entity Create(World world) => _factory(world);
}
