using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ExplosionS2CPacket() : Packet(PacketId.ExplosionS2C)
{
    public HashSet<BlockPos> DestroyedBlockPositions { get; private set; } = new();
    public float ExplosionSize { get; private set; }
    public double ExplosionX { get; private set; }
    public double ExplosionY { get; private set; }
    public double ExplosionZ { get; private set; }

    public static ExplosionS2CPacket Get(double x, double y, double z, float radius, HashSet<BlockPos> affectedBlocks)
    {
        ExplosionS2CPacket p = Get<ExplosionS2CPacket>(PacketId.ExplosionS2C);
        p.ExplosionX = x;
        p.ExplosionY = y;
        p.ExplosionZ = z;
        p.ExplosionSize = radius;
        p.DestroyedBlockPositions = new HashSet<BlockPos>(affectedBlocks);
        return p;
    }

    public override void Read(Stream stream)
    {
        ExplosionX = stream.ReadDouble();
        ExplosionY = stream.ReadDouble();
        ExplosionZ = stream.ReadDouble();
        ExplosionSize = stream.ReadFloat();
        int blockCount = stream.ReadInt();
        DestroyedBlockPositions = new HashSet<BlockPos>();
        int x = (int)ExplosionX;
        int y = (int)ExplosionY;
        int z = (int)ExplosionZ;

        for (int _ = 0; _ < blockCount; ++_)
        {
            int xOffset = (sbyte)stream.ReadByte() + x;
            int yOffset = (sbyte)stream.ReadByte() + y;
            int zOffset = (sbyte)stream.ReadByte() + z;

            DestroyedBlockPositions.Add(new BlockPos(xOffset, yOffset, zOffset));
        }
    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(ExplosionX);
        stream.WriteDouble(ExplosionY);
        stream.WriteDouble(ExplosionZ);
        stream.WriteFloat(ExplosionSize);
        stream.WriteInt(DestroyedBlockPositions.Count);
        int x = (int)ExplosionX;
        int y = (int)ExplosionY;
        int z = (int)ExplosionZ;
        foreach (BlockPos pos in DestroyedBlockPositions)
        {
            int xOffset = pos.x - x;
            int yOffset = pos.y - y;
            int zOffset = pos.z - z;
            stream.WriteByte((byte)xOffset);
            stream.WriteByte((byte)yOffset);
            stream.WriteByte((byte)zOffset);
        }
    }

    public override void Apply(NetHandler handler) => handler.onExplosion(this);

    public override int Size() => 32 + DestroyedBlockPositions.Count * 3;
}
