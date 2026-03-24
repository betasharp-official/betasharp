using BetaSharp.Worlds;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityType(Func<BlockEntity> factory)
{
    private readonly Func<BlockEntity> _factory = factory;

    public BlockEntity Create() => _factory();
}
