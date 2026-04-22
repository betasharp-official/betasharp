using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.PathFinding;

internal class PathEntity
{
    private readonly PathPoint[] _points;
    public int PathLength { get; }
    private int _pathIndex;

    public PathEntity(PathPoint[] points)
    {
        _points = points;
        PathLength = points.Length;
    }

    public void IncrementPathIndex()
    {
        _pathIndex++;
    }

    public bool IsFinished => _pathIndex >= _points.Length;

    public PathPoint? GetFinalPoint()
    {
        return PathLength > 0 ? _points[PathLength - 1] : null;
    }

    public Vec3D GetPosition(Entity entity)
    {
        PathPoint currentPoint = _points[_pathIndex];

        double x = currentPoint.X + (int)(entity.Width + 1.0f) * 0.5;
        double y = currentPoint.Y;
        double z = currentPoint.Z + (int)(entity.Width + 1.0f) * 0.5;

        return new Vec3D(x, y, z);
    }
}
