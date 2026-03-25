using BetaSharp.Worlds;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityType(Func<BlockEntity> factory, string id)
{
    private readonly Func<BlockEntity> _factory = factory;

    public string Id { get; } = id;

    public BlockEntity Create() => _factory();
}
