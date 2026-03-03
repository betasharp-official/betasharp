using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class LivingEntitySpawnS2CPacket() : Packet(PacketId.LivingEntitySpawnS2C)
{
    public int entityId;
    public sbyte type;
    public int xPosition;
    public int yPosition;
    public int zPosition;
    public sbyte yaw;
    public sbyte pitch;
    private DataWatcher metaData;
    private List<WatchableObject> receivedMetadata;

    public LivingEntitySpawnS2CPacket(EntityLiving ent) : this()
    {
        entityId = ent.id;
        type = (sbyte)EntityRegistry.GetRawId(ent);
        xPosition = MathHelper.Floor(ent.x * 32.0D);
        yPosition = MathHelper.Floor(ent.y * 32.0D);
        zPosition = MathHelper.Floor(ent.z * 32.0D);
        yaw = (sbyte)(int)(ent.yaw * 256.0F / 360.0F);
        pitch = (sbyte)(int)(ent.pitch * 256.0F / 360.0F);
        metaData = ent.getDataWatcher();
    }

    public override void Read(NetworkStream stream)
    {
        entityId = stream.ReadInt();
        type = (sbyte)stream.ReadByte();
        xPosition = stream.ReadInt();
        yPosition = stream.ReadInt();
        zPosition = stream.ReadInt();
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
        receivedMetadata = DataWatcher.ReadWatchableObjects(stream);
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteByte((byte)type);
        stream.WriteInt(xPosition);
        stream.WriteInt(yPosition);
        stream.WriteInt(zPosition);
        stream.WriteByte((byte)yaw);
        stream.WriteByte((byte)pitch);
        metaData.WriteWatchableObjects(stream);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onLivingEntitySpawn(this);
    }

    public override int Size()
    {
        return 20;
    }

    public List<WatchableObject> GetMetadata()
    {
        return receivedMetadata;
    }
}
