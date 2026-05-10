using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerSpawnS2CPacket() : Packet(PacketId.PlayerSpawnS2C), IPacketEntity
{
    public int CurrentItem { get; private set; }
    public string Name { get; private set; } = "";
    public sbyte Pitch { get; private set; }
    public sbyte Rotation { get; private set; }
    public int XPosition { get; private set; }
    public int YPosition { get; private set; }
    public int ZPosition { get; private set; }
    public int EntityId { get; private set; }

    public static PlayerSpawnS2CPacket Get(EntityPlayer ent)
    {
        PlayerSpawnS2CPacket p = Get<PlayerSpawnS2CPacket>(PacketId.PlayerSpawnS2C);
        p.EntityId = ent.ID;
        p.Name = ent.Name;
        p.XPosition = MathHelper.Floor(ent.X * 32.0D);
        p.YPosition = MathHelper.Floor(ent.Y * 32.0D);
        p.ZPosition = MathHelper.Floor(ent.Z * 32.0D);
        p.Rotation = (sbyte)(int)(ent.Yaw * 256.0F / 360.0F);
        p.Pitch = (sbyte)(int)(ent.Pitch * 256.0F / 360.0F);
        ItemStack itemStack = ent.Inventory.ItemInHand;
        p.CurrentItem = itemStack == null ? 0 : itemStack.ItemId;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Name = stream.ReadLongString(16);
        XPosition = stream.ReadInt();
        YPosition = stream.ReadInt();
        ZPosition = stream.ReadInt();
        Rotation = (sbyte)stream.ReadByte();
        Pitch = (sbyte)stream.ReadByte();
        CurrentItem = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteLongString(Name);
        stream.WriteInt(XPosition);
        stream.WriteInt(YPosition);
        stream.WriteInt(ZPosition);
        stream.WriteByte((byte)Rotation);
        stream.WriteByte((byte)Pitch);
        stream.WriteShort((short)CurrentItem);
    }

    public override void Apply(NetHandler handler) => handler.onPlayerSpawn(this);

    public override int Size() => 28;
}
