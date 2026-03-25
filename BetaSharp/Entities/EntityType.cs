using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityType(Func<IWorldContext, Entity> factory, Type baseType, string id)
{
    private readonly Func<IWorldContext, Entity> _factory = factory;

    public Type BaseType { get; } = baseType;
    public string Id { get; } = id;

    public Entity Create(IWorldContext world) => _factory(world);
}
