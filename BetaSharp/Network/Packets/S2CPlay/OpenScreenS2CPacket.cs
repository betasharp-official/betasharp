using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class OpenScreenS2CPacket() : Packet(PacketId.OpenScreenS2C)
{
    public int syncId;
    public int screenHandlerId;
    public string name;
    public int slotsCount;

    public static OpenScreenS2CPacket Get(int syncId, int screenHandlerId, string name, int size)
    {
        var p = Get<OpenScreenS2CPacket>(PacketId.OpenScreenS2C);
        p.syncId = syncId;
        p.screenHandlerId = screenHandlerId;
        p.name = name;
        p.slotsCount = size;
        return p;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onOpenScreen(this);
    }

    public override void Read(NetworkStream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        screenHandlerId = (sbyte)stream.ReadByte();
        name = stream.ReadString();
        slotsCount = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)syncId);
        stream.WriteByte((byte)screenHandlerId);
        // TODO: This writes a 16bit array. should index inventory, or write them as base64 or a 8bit string.
        stream.WriteString(name);
        stream.WriteByte((byte)slotsCount);
    }

    public override int Size()
    {
        return 3 + name.Length;
    }

    public enum KnownInventories : byte
    {
        Crafting = 1,
        Chest = 2,
        Furnace = 3,
        /// <summary>
        /// Also known as Dispenser
        /// </summary>
        Trap = 4,
        Minecart = 5
    }
}
