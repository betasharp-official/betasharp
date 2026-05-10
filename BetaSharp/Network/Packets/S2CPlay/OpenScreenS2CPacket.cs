namespace BetaSharp.Network.Packets.S2CPlay;

public class OpenScreenS2CPacket() : Packet(PacketId.OpenScreenS2C)
{
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

    public string Name { get; private set; } = "";
    public int ScreenHandlerId { get; private set; }
    public int SlotsCount { get; private set; }
    public int SyncId { get; private set; }

    public static OpenScreenS2CPacket Get(int syncId, int screenHandlerId, string name, int size)
    {
        OpenScreenS2CPacket p = Get<OpenScreenS2CPacket>(PacketId.OpenScreenS2C);
        p.SyncId = syncId;
        p.ScreenHandlerId = screenHandlerId;
        p.Name = name;
        p.SlotsCount = size;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onOpenScreen(this);

    public override void Read(Stream stream)
    {
        SyncId = (sbyte)stream.ReadByte();
        ScreenHandlerId = (sbyte)stream.ReadByte();
        Name = stream.ReadString();
        SlotsCount = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)SyncId);
        stream.WriteByte((byte)ScreenHandlerId);
        // TODO: This writes a 16bit array. should index inventory, or write them as base64 or a 8bit string.
        stream.WriteString(Name);
        stream.WriteByte((byte)SlotsCount);
    }

    public override int Size() => 3 + Name.Length;
}
