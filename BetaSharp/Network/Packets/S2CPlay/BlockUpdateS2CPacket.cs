using System.Net.Sockets;
using BetaSharp.Worlds;

namespace BetaSharp.Network.Packets.S2CPlay;

public class BlockUpdateS2CPacket() : Packet(PacketId.BlockUpdateS2C)
{
    public int x;
    public int y;
    public int z;
    public int blockRawId;
    public int blockMetadata;

    public BlockUpdateS2CPacket(int x, int y, int z, World world) : this()
    {
        this.x = x;
        this.y = y;
        this.z = z;
        blockRawId = world.getBlockId(x, y, z);
        blockMetadata = world.getBlockMeta(x, y, z);
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
