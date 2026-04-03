namespace BetaSharp.Util.Maths;

public readonly record struct BlockPos
{
    public readonly int x;
    public readonly int y;
    public readonly int z;

    public BlockPos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
