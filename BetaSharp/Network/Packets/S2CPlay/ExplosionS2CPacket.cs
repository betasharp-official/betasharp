using System.Net.Sockets;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ExplosionS2CPacket() : Packet(PacketId.ExplosionS2C)
{
    public double explosionX;
    public double explosionY;
    public double explosionZ;
    public float explosionSize;
    public HashSet<BlockPos> destroyedBlockPositions;

    public static ExplosionS2CPacket Get(double x, double y, double z, float radius, HashSet<BlockPos> affectedBlocks)
    {
        var p = Get<ExplosionS2CPacket>(PacketId.ExplosionS2C);
        p.explosionX = x;
        p.explosionY = y;
        p.explosionZ = z;
        p.explosionSize = radius;
        p.destroyedBlockPositions = new HashSet<BlockPos>(affectedBlocks);
        return p;
    }

    public override void Read(Stream stream)
    {
        explosionX = stream.ReadDouble();
        explosionY = stream.ReadDouble();
        explosionZ = stream.ReadDouble();
        explosionSize = stream.ReadFloat();
        int blockCount = stream.ReadInt();
        destroyedBlockPositions = new HashSet<BlockPos>();
        int x = (int)explosionX;
        int y = (int)explosionY;
        int z = (int)explosionZ;

        for (int _ = 0; _ < blockCount; ++_)
        {
            int xOffset = (sbyte)stream.ReadByte() + x;
            int yOffset = (sbyte)stream.ReadByte() + y;
            int zOffset = (sbyte)stream.ReadByte() + z;

            destroyedBlockPositions.Add(new BlockPos(xOffset, yOffset, zOffset));
        }

    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(explosionX);
        stream.WriteDouble(explosionY);
        stream.WriteDouble(explosionZ);
        stream.WriteFloat(explosionSize);
        stream.WriteInt(destroyedBlockPositions.Count);
        int x = (int)explosionX;
        int y = (int)explosionY;
        int z = (int)explosionZ;
        foreach (var pos in destroyedBlockPositions)
        {
            int xOffset = pos.x - x;
            int yOffset = pos.y - y;
            int zOffset = pos.z - z;
            stream.WriteByte((byte)xOffset);
            stream.WriteByte((byte)yOffset);
            stream.WriteByte((byte)zOffset);
        }
    }

    public override void Apply(NetHandler handler)
    {
        handler.onExplosion(this);
    }

    public override int Size()
    {
        return 32 + destroyedBlockPositions.Count * 3;
    }
}
