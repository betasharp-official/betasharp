using BetaSharp.NBT;
using BetaSharp.Network.Packets;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Registries;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks.Entities;

public abstract class BlockEntity
{
    private static readonly IRegistry<BlockEntityType> s_registry = DefaultRegistries.BlockEntityTypes;
    private static readonly ILogger<BlockEntity> s_logger = Log.Instance.For<BlockEntity>();
    public IWorldContext World;
    protected bool Removed;
    public abstract BlockEntityType Type { get; }

    public int X;
    public int Y;
    public int Z;

    public static readonly BlockEntityType Furnace = Register(() => new BlockEntityFurnace(), "Furnace");
    public static readonly BlockEntityType Chest = Register(() => new BlockEntityChest(), "Chest");
    public static readonly BlockEntityType RecordPlayer = Register(() => new BlockEntityRecordPlayer(), "RecordPlayer");
    public static readonly BlockEntityType Dispenser = Register(() => new BlockEntityDispenser(), "Trap");
    public static readonly BlockEntityType Sign = Register(() => new BlockEntitySign(), "Sign");
    public static readonly BlockEntityType MobSpawner = Register(() => new BlockEntityMobSpawner(), "MobSpawner");
    public static readonly BlockEntityType Note = Register(() => new BlockEntityNote(), "Music");
    public static readonly BlockEntityType Piston = Register(() => new BlockEntityPiston(), "Piston");

    private static BlockEntityType Register<T>(Func<T> factory, string id) where T : BlockEntity
    {
        var type = new BlockEntityType(() => factory());
        s_registry.Register(ResourceLocation.Parse(id.ToLower()), type);
        return type;
    }

    public virtual void readNbt(NBTTagCompound nbt)
    {
        X = nbt.GetInteger("x");
        Y = nbt.GetInteger("y");
        Z = nbt.GetInteger("z");
    }

    public virtual void writeNbt(NBTTagCompound nbt)
    {
        ResourceLocation? key = s_registry.GetKey(Type) ?? throw new Exception(GetType() + " is missing a mapping! This is a bug!");
        nbt.SetString("id", key.Path);
        nbt.SetInteger("x", X);
        nbt.SetInteger("y", Y);
        nbt.SetInteger("z", Z);
    }

    public virtual void tick(EntityManager entities)
    {
    }

    public static BlockEntity? CreateFromNbt(NBTTagCompound nbt)
    {
        string id = nbt.GetString("id");
        if (string.IsNullOrEmpty(id)) return null;

        BlockEntityType? type = s_registry.Get(ResourceLocation.Parse(id.ToLower()));
        if (type == null)
        {
            s_logger.LogInformation($"{id} is missing a mapping!");
            return null;
        }

        try
        {
            BlockEntity blockEntity = type.Create();
            blockEntity.readNbt(nbt);
            return blockEntity;
        }
        catch (Exception exception)
        {
            s_logger.LogError(exception, $"Failed to create block entity for id {id}");
            return null;
        }
    }

    public virtual int getPushedBlockData() => World.Reader.GetBlockMeta(X, Y, Z);

    public void markDirty()
    {
        if (World == null || World.IsRemote)
        {
            return;
        }

        World.Broadcaster.UpdateBlockEntity(X, Y, Z, this);
    }

    public double distanceFrom(double x, double y, double z)
    {
        double dx = X + 0.5D - x;
        double dy = Y + 0.5D - y;
        double dz = Z + 0.5D - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public Block getBlock() => Block.Blocks[World.Reader.GetBlockId(X, Y, Z)];

    public virtual Packet createUpdatePacket() => null;

    public bool isRemoved()
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

    public void markRemoved() => Removed = true;

    public void cancelRemoval() => Removed = false;

    static BlockEntity()
    {
    }
}
