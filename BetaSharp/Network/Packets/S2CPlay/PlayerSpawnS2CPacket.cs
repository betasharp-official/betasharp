using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerSpawnS2CPacket() : Packet(PacketId.PlayerSpawnS2C)
{
    public int entityId;
    public string name;
    public int xPosition;
    public int yPosition;
    public int zPosition;
    public sbyte rotation;
    public sbyte pitch;
    public int currentItem;

    public PlayerSpawnS2CPacket(EntityPlayer ent) : this()
    {
        entityId = ent.id;
        name = ent.name;
        xPosition = MathHelper.Floor(ent.x * 32.0D);
        yPosition = MathHelper.Floor(ent.y * 32.0D);
        zPosition = MathHelper.Floor(ent.z * 32.0D);
        rotation = (sbyte)(int)(ent.yaw * 256.0F / 360.0F);
        pitch = (sbyte)(int)(ent.pitch * 256.0F / 360.0F);
        ItemStack itemStack = ent.inventory.getSelectedItem();
        currentItem = itemStack == null ? 0 : itemStack.itemId;
    }

    public override void Read(NetworkStream stream)
    {
        entityId = stream.ReadInt();
        name = stream.ReadLongString(16);
        xPosition = stream.ReadInt();
        yPosition = stream.ReadInt();
        zPosition = stream.ReadInt();
        rotation = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
        currentItem = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteLongString(name);
        stream.WriteInt(xPosition);
        stream.WriteInt(yPosition);
        stream.WriteInt(zPosition);
        stream.WriteByte((byte)rotation);
        stream.WriteByte((byte)pitch);
        stream.WriteShort((short)currentItem);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerSpawn(this);
    }

    public override int Size()
    {
        return 28;
    }
}
