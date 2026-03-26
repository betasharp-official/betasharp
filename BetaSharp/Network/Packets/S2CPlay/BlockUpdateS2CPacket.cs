using System.Net.Sockets;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Network.Packets.S2CPlay;

public class BlockUpdateS2CPacket() : Packet(PacketId.BlockUpdateS2C)
{
    public int x;
    public int y;
    public int z;
    public int blockRawId;
    public int blockMetadata;

    public static BlockUpdateS2CPacket Get(int x, int y, int z, IWorldContext world)
    {
        var p = Get<BlockUpdateS2CPacket>(PacketId.BlockUpdateS2C);
        p.x = x;
        p.y = y;
        p.z = z;
        p.blockRawId = world.Reader.GetBlockId(x, y, z);
        p.blockMetadata = world.Reader.GetBlockMeta(x, y, z);
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadInt();
        y = stream.ReadByte();
        z = stream.ReadInt();
        blockRawId = stream.ReadByte();
        blockMetadata = stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(x);
        stream.WriteByte((byte)y);
        stream.WriteInt(z);
        stream.WriteByte((byte)blockRawId);
        stream.WriteByte((byte)blockMetadata);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onBlockUpdate(this);
    }

    public override int Size()
    {
        return 11;
    }
}
