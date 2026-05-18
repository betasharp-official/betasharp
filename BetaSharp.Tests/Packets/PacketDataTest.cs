using BetaSharp.Items;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.S2CPlay;

namespace BetaSharp.Tests.Packets;

public class PacketDataTest : PacketTestBase
{
    // intended for packets where size differs depending on the context.
    public static IEnumerable<object[]> ExamplePackets = new List<Packet[]>
    {
        new Packet[] { ClickSlotC2SPacket.Get(0, 0, 0, false, new ItemStack(Item.Wheat, 18), 0) },
        new Packet[] { PlayerInteractBlockC2SPacket.Get(0, 64, 0, 0, new ItemStack(Item.Stick, 2)) },
        new Packet[] { InventoryS2CPacket.Get(1, [new ItemStack(Item.Stick, 64), new ItemStack(Item.Bucket)]) }
    };

    [Theory, MemberData(nameof(PacketIds))]
    public void VerifyPacketDefaultReadWriteLenght(PacketId value)
    {
        Packet p;
        if (value == PacketId.RegistryDataS2C)
        {
            // special case for RegistryDataS2CPacket
            p = RegistryDataS2CPacket.Get(new ResourceLocation(Namespace.BetaSharp, "test"), []);
        }
        else
        {
            p = Packet.Get(value);
        }

        MemoryStream stream = new();
        p.Write(stream);
        stream.Position = 0;
        p.Read(stream);

        Assert.StrictEqual(stream.Length, stream.Position);
        stream.Dispose();
    }

    [Theory, MemberData(nameof(ExamplePackets))]
    public void VerifyPacketReadWriteLenght(Packet value)
    {
        MemoryStream stream = new();
        value.Write(stream);
        stream.Position = 0;
        value.Read(stream);

        Assert.StrictEqual(stream.Length, stream.Position);
        stream.Dispose();
    }
}
