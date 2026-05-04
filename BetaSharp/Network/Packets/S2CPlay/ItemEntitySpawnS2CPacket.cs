using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ItemEntitySpawnS2CPacket() : Packet(PacketId.ItemEntitySpawnS2C), IPacketEntity
{
    public int ItemCount { get; private set; }
    public int ItemDamage { get; private set; }
    public int ItemRawId { get; private set; }
    public sbyte VelocityX { get; private set; }
    public sbyte VelocityY { get; private set; }
    public sbyte VelocityZ { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }
    public int EntityId { get; private set; }

    public static ItemEntitySpawnS2CPacket Get(EntityItem item)
    {
        ItemEntitySpawnS2CPacket p = Get<ItemEntitySpawnS2CPacket>(PacketId.ItemEntitySpawnS2C);
        p.EntityId = item.ID;
        p.ItemRawId = item.Stack.ItemId;
        p.ItemCount = item.Stack.Count;
        p.ItemDamage = item.Stack.getDamage();
        p.X = MathHelper.Floor(item.X * 32.0D);
        p.Y = MathHelper.Floor(item.Y * 32.0D);
        p.Z = MathHelper.Floor(item.Z * 32.0D);
        p.VelocityX = (sbyte)(int)(item.VelocityX * 128.0D);
        p.VelocityY = (sbyte)(int)(item.VelocityY * 128.0D);
        p.VelocityZ = (sbyte)(int)(item.VelocityZ * 128.0D);
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        ItemRawId = stream.ReadShort();
        ItemCount = (sbyte)stream.ReadByte();
        ItemDamage = stream.ReadShort();
        X = stream.ReadInt();
        Y = stream.ReadInt();
        Z = stream.ReadInt();
        VelocityX = (sbyte)stream.ReadByte();
        VelocityY = (sbyte)stream.ReadByte();
        VelocityZ = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteShort((short)ItemRawId);
        stream.WriteByte((byte)ItemCount);
        stream.WriteShort((short)ItemDamage);
        stream.WriteInt(X);
        stream.WriteInt(Y);
        stream.WriteInt(Z);
        stream.WriteByte((byte)VelocityX);
        stream.WriteByte((byte)VelocityY);
        stream.WriteByte((byte)VelocityZ);
    }

    public override void Apply(NetHandler handler) => handler.onItemEntitySpawn(this);

    public override int Size() => 24;
}
