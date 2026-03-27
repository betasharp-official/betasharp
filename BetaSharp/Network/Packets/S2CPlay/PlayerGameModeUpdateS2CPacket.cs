using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerGameModeUpdateS2CPacket() : ExtendedProtocolPacket(PacketId.PlayerGameModeUpdateS2C)
{
    public GameMode GameMode { get; private set; } = GameModes.Get(0);

    public static PlayerGameModeUpdateS2CPacket Get(GameMode mode)
    {
        var p = Get<PlayerGameModeUpdateS2CPacket>(PacketId.PlayerGameModeUpdateS2C);
        p.GameMode = mode;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        float breakSpeed = stream.ReadFloat();
        int bits = stream.ReadInt();

        GameMode = new GameMode()
        {
            Id = -1,
            Name = "remote",
            BrakeSpeed = breakSpeed,
            CanBreak = Bits(0, bits),
            CanPlace = Bits(1, bits),
            CanInteract = Bits(2, bits),
            CanReceiveDamage = Bits(3, bits),
            CanInflictDamage = Bits(4, bits),
            CanBeTargeted = Bits(5, bits),
            CanExhaustFire = Bits(6, bits),
            CanPickup = Bits(7, bits),
            FiniteResources = Bits(8, bits),
            VisibleToWorld = Bits(9, bits),
            BlockDrops = Bits(10, bits),
            CanDrop = Bits(11, bits),
        };
    }

    public override void Write(NetworkStream stream)
    {
        int bits = 0;
        bits |= Bits(0, GameMode.CanBreak);
        bits |= Bits(1, GameMode.CanPlace);
        bits |= Bits(2, GameMode.CanInteract);
        bits |= Bits(3, GameMode.CanReceiveDamage);
        bits |= Bits(4, GameMode.CanInflictDamage);
        bits |= Bits(5, GameMode.CanBeTargeted);
        bits |= Bits(6, GameMode.CanExhaustFire);
        bits |= Bits(7, GameMode.CanPickup);
        bits |= Bits(8, GameMode.FiniteResources);
        bits |= Bits(9, GameMode.VisibleToWorld);
        bits |= Bits(10, GameMode.BlockDrops);
        bits |= Bits(11, GameMode.CanDrop);

        stream.WriteFloat(GameMode.BrakeSpeed);
        stream.WriteInt(bits);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerGameModeUpdate(this);
    }

    public override int Size()
    {
        return 8;
    }

    private static int Bits(byte pos, bool value) =>
        value ? (1 << pos) : 0;

    private static bool Bits(byte pos, int value) =>
        (value & (1 << pos)) != 0;
}
