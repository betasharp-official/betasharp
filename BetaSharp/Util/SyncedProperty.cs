namespace BetaSharp.Util;

public interface ISyncedProperty
{
    public int DataValueId { get; }
    public int DataType { get; }
    public bool Dirty { get; set; }
}

public sealed class SyncedProperty<T> : ISyncedProperty
{
    private readonly DataSynchronizer _synchronizer;

    public int DataValueId { get; }
    public int DataType { get; }
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

    internal SyncedProperty(DataSynchronizer synchronizer, int dataValueId, int dataType, T initialValue)
    {
        _synchronizer = synchronizer;
        DataValueId = dataValueId;
        DataType = dataType;
        Value = initialValue;
    }

    public void Deconstruct(out T value) => value = Value;
}
