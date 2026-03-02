using System.Net.Sockets;
using System.Runtime.CompilerServices;
using BetaSharp.Items;
using BetaSharp.Network.Packets;
using BetaSharp.Util.Maths;
using Console = System.Console;

namespace BetaSharp;

public class DataWatcher
{
    private static readonly Dictionary<Type, int> dataTypes = [];

    private readonly Dictionary<int, WatchableObject> watchedObjects = new();
    public bool dirty { get; private set; }

    public void AddObject(int id, object value)
    {
        if (!dataTypes.TryGetValue(value.GetType(), out int typeId))
        {
            throw new ArgumentException("Unknown data type: " + value.GetType());
        }

        if (id > 31)
        {
            throw new ArgumentException("Data value id is too big with " + id + "! (Max is " + 31 + ")");
        }

        if (watchedObjects.ContainsKey(id))
        {
            throw new ArgumentException("Duplicate id value for " + id + "!");
        }

        watchedObjects[id] = new WatchableObject(typeId, id, value);
    }

    public List<WatchableObject> GetDirtyEntries()
    {
	    List<WatchableObject> res = null;
        if (dirty)
        {
            foreach (var obj in watchedObjects.Values)
            {
                if (obj.dirty)
                {
                    if (res == null) res = new List<WatchableObject>();

                    obj.dirty = false;
                    res.Add(obj);
                }
            }
        }

        dirty = false;
        return res;
    }

    public sbyte getWatchableObjectByte(int id)
    {
        // This is terrible. Data watcher needs a refactor
        int intermediate = Convert.ToInt32(watchedObjects[id].watchedObject);
        return (sbyte)Math.Clamp(intermediate, sbyte.MinValue, sbyte.MaxValue);
    }

    public int GetWatchableObjectInt(int id)
    {
        return (int)watchedObjects[id].watchedObject;
    }

    public string GetWatchableObjectString(int id)
    {
        return ((string)watchedObjects[id].watchedObject);
    }

    public void UpdateObject(int id, object value)
    {
        WatchableObject obj = watchedObjects[id];
        if (!value.Equals(obj.watchedObject))
        {
            obj.watchedObject = value;
            obj.dirty = true;
            dirty = true;
        }
    }

    public static void WriteObjectsInListToStream(List<WatchableObject> list, Stream stream)
    {
        if (list != null)
        {
	        foreach (WatchableObject o in list)
	        {
		        WriteWatchableObject(stream, o);
	        }
        }

        stream.WriteByte(127);
    }

    public void WriteWatchableObjects(Stream stream)
    {
        foreach (var obj in watchedObjects.Values)
        {
            WriteWatchableObject(stream, obj);
        }

        stream.WriteByte(127);
    }

    private static void WriteWatchableObject(Stream stream, WatchableObject obj)
    {
        int header = (obj.objectType << 5 | obj.dataValueId & 31) & 255;
        stream.WriteByte((byte) header);
        switch (obj.objectType)
        {
            case 0:
                stream.WriteByte((byte)(obj.watchedObject));
                break;
            case 1:
                stream.WriteShort((short)obj.watchedObject);
                break;
            case 2:
                stream.WriteInt((int)obj.watchedObject);
                break;
            case 3:
                stream.WriteFloat((float)obj.watchedObject);
                break;
            case 4:
                stream.WriteLongString((string)obj.watchedObject);
                break;
            case 5:
                ItemStack item = (ItemStack)obj.watchedObject;
                stream.WriteShort((short) item.getItem().id);
                stream.WriteByte((byte) item.count);
                stream.WriteShort((short) item.getDamage());
                break;
            case 6:
                Vec3i vec = (Vec3i)obj.watchedObject;
                stream.WriteInt(vec.X);
                stream.WriteInt(vec.Y);
                stream.WriteInt(vec.Z);
                break;
        }
    }

    public static List<WatchableObject> ReadWatchableObjects(Stream stream)
    {
	    List<WatchableObject> res = null;

        for (sbyte b = (sbyte)stream.ReadByte(); b != 127; b = (sbyte)stream.ReadByte())
        {
            res ??= [];

            int objectType = (b & 224) >> 5;
            int dataValueId = b & 31;
            WatchableObject obj = null;
            switch (objectType)
            {
                case 0:
                    obj = new WatchableObject(objectType, dataValueId, stream.ReadByte());
                    break;
                case 1:
                    obj = new WatchableObject(objectType, dataValueId, stream.ReadShort());
                    break;
                case 2:
                    obj = new WatchableObject(objectType, dataValueId, stream.ReadInt());
                    break;
                case 3:
                    obj = new WatchableObject(objectType, dataValueId, stream.ReadFloat());
                    break;
                case 4:
                    obj = new WatchableObject(objectType, dataValueId, stream.ReadLongString(64));
                    break;
                case 5:
                    short id = stream.ReadShort();
                    sbyte count = (sbyte)stream.ReadByte();
                    short damage = stream.ReadShort();
                    obj = new WatchableObject(objectType, dataValueId, new ItemStack(id, count, damage));
                    break;
                case 6:
                    int x = stream.ReadInt();
                    int y = stream.ReadInt();
                    int z = stream.ReadInt();
                    obj = new WatchableObject(objectType, dataValueId, new Vec3i(x, y, z));
                    break;
            }

            res.Add(obj);
        }

        return res;
    }

    public void UpdateWatchedObjectsFromList(List<WatchableObject> list)
    {
	    foreach (WatchableObject obj in list)
	    {
		    if (watchedObjects.TryGetValue(obj.dataValueId, out var obj2))
		    {
			    obj2.watchedObject = obj.watchedObject;
		    }
	    }
    }

    static DataWatcher()
    {
        dataTypes[typeof(byte)] =  0;
        dataTypes[typeof(short)] =  1;
        dataTypes[typeof(int)] =  2;
        dataTypes[typeof(float)] =  3;
        dataTypes[typeof(string)] =  4;
        dataTypes[typeof(ItemStack)] =  5;
        dataTypes[typeof(Vec3i)] =  6;
    }
}
