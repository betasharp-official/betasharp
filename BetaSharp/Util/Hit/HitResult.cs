using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Util.Hit;

public struct HitResult
{
    public HitResultType Type;
    public int BlockX;
    public int BlockY;
    public int BlockZ;
    public int Side;
    public Vec3D Pos;
    public Entity? Entity;

    public HitResult(int blockX, int blockY, int blockZ, int side, Vec3D pos, HitResultType type)
    {
        Type = type;
        BlockX = blockX;
        BlockY = blockY;
        BlockZ = blockZ;
        Side = side;
        Pos = pos;
    }

    public HitResult(HitResultType type)
    {
        Type = type;
    }

    public HitResult(Entity entity)
    {
        Type = HitResultType.ENTITY;
        Entity = entity;
        Pos = new Vec3D(entity.X, entity.Y, entity.Z);
    }
}
