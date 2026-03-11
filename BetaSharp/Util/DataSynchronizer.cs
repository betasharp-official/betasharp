using BetaSharp.Items;
using BetaSharp.Util.Maths;

namespace BetaSharp.Util;

public sealed class DataSynchronizer
{
    public static readonly Dictionary<Type, int> TypeIds = [];

    private readonly Dictionary<int, ISyncedProperty> _syncedProperties = new();
    public bool Dirty { get; internal set; }

    static DataSynchronizer()
    {
        TypeIds[typeof(byte)] = 0;
        TypeIds[typeof(short)] = 1;
        TypeIds[typeof(int)] = 2;
        TypeIds[typeof(float)] = 3;
        TypeIds[typeof(string)] = 4;
        TypeIds[typeof(ItemStack)] = 5;
        TypeIds[typeof(Vec3i)] = 6;

        TypeIds[typeof(bool)] = 0; // Serialize bools as bytes
    }

    public SyncedProperty<T> MakeProperty<T>(int dataValueId, T initialValue)
    {
        if (!TypeIds.TryGetValue(typeof(T), out int dataType))
        {
            throw new ArgumentException("Unknown data type: " + typeof(T));
        }

        if (dataValueId > 31)
        {
            throw new ArgumentException("Data value id is too big with " + dataValueId + "! (Max is " + 31 + ")");
        }

        if (_syncedProperties.ContainsKey(dataValueId))
        {
            throw new ArgumentException("Duplicate id value for " + dataValueId + "!");
        }

        var prop = new SyncedProperty<T>(this, dataValueId, dataType, initialValue);
        _syncedProperties[dataValueId] = prop;
        return prop;
    }

    private static void SerializeProperty(Stream stream, ISyncedProperty obj)
    {
        byte header = (byte)(obj.DataType << 5 | obj.DataValueId & 31);
        stream.WriteByte(header);

        switch (obj)
        {
            case SyncedProperty<bool>(var b):
                stream.WriteBoolean(b);
                break;
            case SyncedProperty<byte>(var b):
                stream.WriteByte(b);
                break;
            case SyncedProperty<short>(var s):
                stream.WriteShort(s);
                break;
            case SyncedProperty<int>(var i):
                stream.WriteInt(i);
                break;
            case SyncedProperty<float>(var f):
                stream.WriteFloat(f);
                break;
            case SyncedProperty<string>(var str):
                stream.WriteLongString(str);
                break;
            case SyncedProperty<ItemStack>(var item):
                stream.WriteShort((short)item.getItem().id);
                stream.WriteByte((byte)item.count);
                stream.WriteShort((short)item.getDamage());
                break;
            case SyncedProperty<Vec3i>(var vec):
                stream.WriteInt(vec.X);
                stream.WriteInt(vec.Y);
                stream.WriteInt(vec.Z);
                break;
            default: throw new ArgumentException("Unsupported data type: " + obj.DataType);
        }
    }

    private void DeserializeProperty(Stream stream, int objectType, int dataValueId)
    {
        if (!_syncedProperties.TryGetValue(dataValueId, out var prop))
        {
            throw new ArgumentException("Property not found: " + dataValueId);
        }

        if (prop.DataType != objectType)
        {
            throw new ArgumentException("Data type mismatch for property " + dataValueId);
        }

        switch (objectType)
        {
            case 0:
                if (prop is SyncedProperty<bool> property)
                {
                    property.Value = stream.ReadByte() != 0;
                    break;
                }
                else
                {
                    ((SyncedProperty<byte>)prop).Value = (byte)stream.ReadByte();
                    break;
                }
            case 1:
                ((SyncedProperty<short>)prop).Value = stream.ReadShort();
                break;
            case 2:
                ((SyncedProperty<int>)prop).Value = stream.ReadInt();
                break;
            case 3:
                ((SyncedProperty<float>)prop).Value = stream.ReadFloat();
                break;
            case 4:
                ((SyncedProperty<string>)prop).Value = stream.ReadLongString();
                break;
            case 5:
                ((SyncedProperty<ItemStack>)prop).Value = new ItemStack(stream.ReadShort(), (sbyte)stream.ReadByte(), stream.ReadShort());
                break;
            case 6:
                ((SyncedProperty<Vec3i>)prop).Value = new Vec3i(stream.ReadInt(), stream.ReadInt(), stream.ReadInt());
                break;
            default:
                throw new ArgumentException("Unsupported data type: " + objectType);
        }
    }

    public void WriteAll(Stream stream)
    {
        foreach (var obj in _syncedProperties.Values)
        {
            SerializeProperty(stream, obj);
        }
    }

    public void WriteChanges(Stream stream)
    {
        if (Dirty)
        {
            foreach (var obj in _syncedProperties.Values)
            {
                if (obj.Dirty)
                {
                    SerializeProperty(stream, obj);
                    obj.Dirty = false;
                }
            }
        }

        Dirty = false;
    }

    public void ApplyChanges(Stream stream)
    {
        while (true)
        {
            int b = stream.ReadByte();
            if (b == -1) break;
            int objectType = (b & 224) >> 5;
            int dataValueId = b & 31;
            DeserializeProperty(stream, objectType, dataValueId);
        }
    }
}
