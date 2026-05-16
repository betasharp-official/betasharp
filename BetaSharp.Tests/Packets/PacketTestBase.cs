using BetaSharp.Network.Packets;

namespace BetaSharp.Tests.Packets;

public abstract class PacketTestBase
{
    public static IEnumerable<object[]> PacketIds =>
        Enum.GetValues(typeof(PacketId))
            .Cast<PacketId>()
            .Select(v => new object[] { v });
}
