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

    public static ItemEntitySpawnS2CPacket Get(EntityItem item)
    {
        var p = Get<ItemEntitySpawnS2CPacket>(PacketId.ItemEntitySpawnS2C);
        p.id = item.ID;
        p.itemRawId = item.Stack.ItemId;
        p.itemCount = item.Stack.Count;
        p.itemDamage = item.Stack.getDamage();
        p.x = MathHelper.Floor(item.X * 32.0D);
        p.y = MathHelper.Floor(item.Y * 32.0D);
        p.z = MathHelper.Floor(item.Z * 32.0D);
        p.velocityX = (sbyte)(int)(item.VelocityX * 128.0D);
        p.velocityY = (sbyte)(int)(item.VelocityY * 128.0D);
        p.velocityZ = (sbyte)(int)(item.VelocityZ * 128.0D);
        return p;
    }

    public override void Read(Stream stream)
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

    public override void Write(Stream stream)
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
