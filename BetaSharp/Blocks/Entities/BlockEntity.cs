using BetaSharp.NBT;
using BetaSharp.Network.Packets;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Blocks.Entities;

public class BlockEntity
{
    private static readonly Dictionary<string, Type> s_idToClass = new();
    private static readonly Dictionary<Type, string> s_classToId = new();
    private static readonly ILogger<BlockEntity> s_logger = Log.Instance.For<BlockEntity>();
    protected bool Removed;
    public World World;
    public int X;
    public int Y;
    public int Z;

    static BlockEntity()
    {
        Create(typeof(BlockEntityFurnace), "Furnace");
        Create(typeof(BlockEntityChest), "Chest");
        Create(typeof(BlockEntityRecordPlayer), "RecordPlayer");
        Create(typeof(BlockEntityDispenser), "Trap");
        Create(typeof(BlockEntitySign), "Sign");
        Create(typeof(BlockEntityMobSpawner), "MobSpawner");
        Create(typeof(BlockEntityNote), "Music");
        Create(typeof(BlockEntityPiston), "Piston");
    }

    private static void Create(Type blockEntityClass, string id)
    {
        if (s_idToClass.ContainsKey(id))
        {
            throw new ArgumentException("Duplicate id: " + id, nameof(id));
        }

        s_idToClass.Add(id, blockEntityClass);
        s_classToId.Add(blockEntityClass, id);
    }

    public virtual void readNbt(NBTTagCompound nbt)
    {
        X = nbt.GetInteger("x");
        Y = nbt.GetInteger("y");
        Z = nbt.GetInteger("z");
    }

    public virtual void writeNbt(NBTTagCompound nbt)
    {
        if (!s_classToId.TryGetValue(GetType(), out string? entityId))
        {
            throw new Exception(GetType() + " is missing a mapping! This is a bug!");
        }

        nbt.SetString("id", entityId);
        nbt.SetInteger("x", X);
        nbt.SetInteger("y", Y);
        nbt.SetInteger("z", Z);
    }

    public virtual void tick()
    {
    }

    public static BlockEntity? CreateFromNbt(NBTTagCompound nbt)
    {
        BlockEntity blockEntity = null;

        try
        {
            if (s_idToClass.TryGetValue(nbt.GetString("id"), out Type? blockEntityClass))
            {
                blockEntity = (BlockEntity)Activator.CreateInstance(blockEntityClass);
            }
            else
            {
                s_logger.LogInformation(nbt.GetString("id") + " is missing a mapping!");
                return null;
            }
        }
        catch (Exception exception)
        {
            s_logger.LogError(exception.ToString());
        }

        if (blockEntity != null)
        {
            blockEntity.readNbt(nbt);
        }
        else
        {
            s_logger.LogInformation("Skipping TileEntity with id " + nbt.GetString("id"));
        }

        return blockEntity;
    }

    public virtual int getPushedBlockData() => World.getBlockMeta(X, Y, Z);

    public void markDirty()
    {
        if (World != null)
        {
            World.UpdateBlockEntity(X, Y, Z, this);
        }
    }

    public double distanceFrom(double x, double y, double z)
    {
        double dx = X + 0.5D - x;
        double dy = Y + 0.5D - y;
        double dz = Z + 0.5D - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public Block getBlock() => Block.Blocks[World.getBlockId(X, Y, Z)];

    public virtual Packet createUpdatePacket() => null;

    public bool isRemoved()
    {
        if (Removed)
        {
            return true;
        }

        if (World != null)
        {
            int id = World.getBlockId(X, Y, Z);
            if (id == 0 || !Block.BlocksWithEntity[id])
            {
                return true;
            }
        }

        return false;
    }

    public void markRemoved() => Removed = true;

    public void cancelRemoval() => Removed = false;
}
