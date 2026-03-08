using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class UpdateSelectedSlotC2SPacket() : Packet(PacketId.UpdateSelectedSlotC2S)
{
    public int selectedSlot;

    public static UpdateSelectedSlotC2SPacket Get(int selectedSlot)
    {
        var p = Get<UpdateSelectedSlotC2SPacket>(PacketId.UpdateSelectedSlotC2S);
        p.selectedSlot = selectedSlot;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        selectedSlot = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteShort((short)selectedSlot);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onUpdateSelectedSlot(this);
    }

    public override int Size()
    {
        return 2;
    }
}
