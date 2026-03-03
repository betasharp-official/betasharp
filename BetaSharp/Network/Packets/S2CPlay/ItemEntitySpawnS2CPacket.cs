using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ItemEntitySpawnS2CPacket() : Packet(PacketId.ItemEntitySpawnS2C)
{
    public int id;
    public int x;
    public int y;
    public int z;
    public sbyte velocityX;
    public sbyte velocityY;
    public sbyte velocityZ;
    public int itemRawId;
    public int itemCount;
    public int itemDamage;

    public ItemEntitySpawnS2CPacket(EntityItem item) : this()
    {
        id = item.id;
        itemRawId = item.stack.itemId;
        itemCount = item.stack.count;
        itemDamage = item.stack.getDamage();
        x = MathHelper.Floor(item.x * 32.0D);
        y = MathHelper.Floor(item.y * 32.0D);
        z = MathHelper.Floor(item.z * 32.0D);
        velocityX = (sbyte)(int)(item.velocityX * 128.0D);
        velocityY = (sbyte)(int)(item.velocityY * 128.0D);
        velocityZ = (sbyte)(int)(item.velocityZ * 128.0D);
    }

    public override void Read(NetworkStream stream)
    {
        id = stream.ReadInt();
        itemRawId = stream.ReadShort();
        itemCount = (sbyte)stream.ReadByte();
        itemDamage = stream.ReadShort();
        x = stream.ReadInt();
        y = stream.ReadInt();
        z = stream.ReadInt();
        velocityX = (sbyte)stream.ReadByte();
        velocityY = (sbyte)stream.ReadByte();
        velocityZ = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(id);
        stream.WriteShort((short)itemRawId);
        stream.WriteByte((byte)itemCount);
        stream.WriteShort((short)itemDamage);
        stream.WriteInt(x);
        stream.WriteInt(y);
        stream.WriteInt(z);
        stream.WriteByte((byte)velocityX);
        stream.WriteByte((byte)velocityY);
        stream.WriteByte((byte)velocityZ);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onItemEntitySpawn(this);
    }

    public override int Size()
    {
        return 24;
    }
}
