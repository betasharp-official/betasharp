namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Type of a block entity, containing a factory for creating instances of the block entity and the string ID used in
///     NBT data.
/// </summary>
public class BlockEntityType(Func<BlockEntity> factory, string id)
{
    private readonly Func<BlockEntity> _factory = factory;

    public string ID { get; } = id;
    public BlockEntity Create() => _factory();
}
