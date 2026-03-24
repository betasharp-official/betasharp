using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityType(Func<IWorldContext, Entity> factory, Type baseType)
{
    private readonly Func<IWorldContext, Entity> _factory = factory;

    public Type BaseType { get; } = baseType;

    public Entity Create(IWorldContext world) => _factory(world);
}
