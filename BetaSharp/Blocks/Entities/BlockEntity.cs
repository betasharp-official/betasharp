using BetaSharp.NBT;
using BetaSharp.Network.Packets;
using BetaSharp.Registries;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Abstract class to represent a block entity, allowing a block to store inventory or other such
///     stuff apart from the block.
/// </summary>
public abstract class BlockEntity
{
    private static readonly IRegistry<BlockEntityType> s_registry = DefaultRegistries.BlockEntityTypes;
    private static readonly ILogger<BlockEntity> s_logger = Log.Instance.For<BlockEntity>();

    // Block entity registered types
    // No documentation for each as they are self-explanatory and will just fill up the file.
    public static readonly BlockEntityType Furnace = Register<BlockEntityFurnace>("Furnace");
    public static readonly BlockEntityType Chest = Register<BlockEntityChest>("Chest");
    public static readonly BlockEntityType RecordPlayer = Register<BlockEntityRecordPlayer>("RecordPlayer");
    public static readonly BlockEntityType Dispenser = Register<BlockEntityDispenser>("Trap");
    public static readonly BlockEntityType Sign = Register<BlockEntitySign>("Sign");
    public static readonly BlockEntityType MobSpawner = Register<BlockEntityMobSpawner>("MobSpawner");
    public static readonly BlockEntityType Music = Register<BlockEntityNote>("Music");
    public static readonly BlockEntityType Piston = Register<BlockEntityPiston>("Piston");
    protected bool Removed;

    static BlockEntity()
    {
    }

    public IWorldContext World { get; set; }

    public abstract BlockEntityType Type { get; }

    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    /// <summary>
    ///     Gets the metadata value associated with the block at the current coordinates.
    /// </summary>
    public int PushedBlockData => World.Reader.GetBlockMeta(X, Y, Z);

    private static BlockEntityType Register<T>(string id) where T : BlockEntity, new()
    {
        BlockEntityType type = new(() => new T(), id);
        s_registry.Register(ResourceLocation.Parse(id.ToLower()), type);
        return type;
    }

    /// <summary>
    ///     Read data for the entity from a NBT tag compound.
    ///     Expected to be overridden (with base.ReadNbt(nbt), of course).
    /// </summary>
    /// <param name="nbt"><see cref="NBTTagCompound" /> containing the data to read.</param>
    public virtual void ReadNbt(NBTTagCompound nbt)
    {
        X = nbt.GetInteger("x");
        Y = nbt.GetInteger("y");
        Z = nbt.GetInteger("z");
    }

    /// <summary>
    ///     Write data for the entity to a NBT tag compound.
    ///     Expected to be overridden (with base.WriteNbt(nbt), of course).
    /// </summary>
    /// <param name="nbt"><see cref="NBTTagCompound" /> to write the data to.</param>
    public virtual void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetString("id", Type.ID);
        nbt.SetInteger("x", X);
        nbt.SetInteger("y", Y);
        nbt.SetInteger("z", Z);
    }

    /// <summary>
    ///     Run a single tick for the block entity.
    ///     Expected to be overridden if the block entity needs to do something every tick.
    /// </summary>
    /// <param name="entities"><see cref="EntityManager" /> containing the entities in the world.</param>
    public virtual void Tick(EntityManager entities)
    {
    }

    /// <summary>
    ///     Create a BlockEntity from a NBT tag compound.
    ///     Uses the "id" tag to determine the type of block entity to create, and then calls ReadNbt,
    ///     to, of course, get the data.
    /// </summary>
    /// <param name="nbt"><see cref="NBTTagCompound" /> containing the data to read, expected to have an "id" tag.</param>
    /// <returns>A <see cref="BlockEntity"></see> representing the NBT, or null if invalid.</returns>
    public static BlockEntity? CreateFromNbt(NBTTagCompound nbt)
    {
        string id = nbt.GetString("id");
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        BlockEntityType? type = s_registry.Get(ResourceLocation.Parse(id.ToLower()));
        if (type == null)
        {
            s_logger.LogInformation($"{id} is missing a mapping!");
            return null;
        }

        try
        {
            BlockEntity blockEntity = type.Create();
            blockEntity.ReadNbt(nbt);
            return blockEntity;
        }
        catch (Exception exception)
        {
            s_logger.LogError(exception, $"Failed to create block entity for id {id}");
            return null;
        }
    }

    public void MarkDirty()
    {
        if (World == null || World.IsRemote)
        {
            return;
        }

        World.Broadcaster.UpdateBlockEntity(X, Y, Z, this);
    }

    /// <summary>
    ///     Gets the (squared!) distance from the center of the block entity to the given coordinates.
    /// </summary>
    /// <returns>The squared distance from the center of the block entity to the given coordinates.
    public double DistanceFrom(double x, double y, double z)
    {
        double dx = X + 0.5D - x;
        double dy = Y + 0.5D - y;
        double dz = Z + 0.5D - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public Block GetBlock() => Block.Blocks[World.Reader.GetBlockId(X, Y, Z)];

    public virtual Packet CreateUpdatePacket() => null;

    public bool IsRemoved()
    {
        if (Removed)
        {
            return true;
        }

        if (World != null)
        {
            int id = World.Reader.GetBlockId(X, Y, Z);
            if (id == 0 || !Block.BlocksWithEntity[id])
            {
                return true;
            }
        }

        return false;
    }

    public void MarkRemoved() => Removed = true;

    public void CancelRemoval() => Removed = false;
}
