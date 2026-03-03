using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityTrackerUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityTrackerUpdateS2C)
{
    private List<WatchableObject> trackedValues;

    public EntityTrackerUpdateS2CPacket(int entityId, DataWatcher dataWatcher) : this()
    {
        EntityId = entityId;
        trackedValues = dataWatcher.GetDirtyEntries();
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        trackedValues = DataWatcher.ReadWatchableObjects(stream);
    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        DataWatcher.WriteObjectsInListToStream(trackedValues, stream);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityTrackerUpdate(this);
    }

    public override int Size()
    {
        // TODO : this is wrong
        return 5;
    }

    public List<WatchableObject> GetWatchedObjects()
    {
        return trackedValues;
    }
}
