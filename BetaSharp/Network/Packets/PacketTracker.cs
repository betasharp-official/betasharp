namespace BetaSharp.Network.Packets;

internal class PacketTracker
{
    private int count;
    private long size;

    public void Update(int size)
    {
        ++count;
        this.size += size;
    }
}
