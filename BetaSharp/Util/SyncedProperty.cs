namespace BetaSharp.Util;

public enum SyncedDataType
{
    Byte = 0,
    Short = 1,
    Int = 2,
    Float = 3,
    String = 4,
    ItemStack = 5,
    Vec3i = 6,
}

public interface ISyncedProperty
{
    public int DataValueId { get; }
    public SyncedDataType DataType { get; }
    public bool Dirty { get; set; }
}

public sealed class SyncedProperty<T> : ISyncedProperty
{
    private readonly DataSynchronizer _synchronizer;

    public int DataValueId { get; }
    public SyncedDataType DataType { get; }
    public bool Dirty { get; set; }

    public T Value
    {
        get;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                Dirty = true;
                _synchronizer.Dirty = true;
            }
        }
    }

    internal SyncedProperty(DataSynchronizer synchronizer, int dataValueId, SyncedDataType dataType, T initialValue)
    {
        _synchronizer = synchronizer;
        DataValueId = dataValueId;
        DataType = dataType;
        Value = initialValue;
    }

    public void Deconstruct(out T value) => value = Value;
}
