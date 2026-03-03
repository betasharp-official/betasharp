using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using javax.swing.text.html;

namespace BetaSharp.PathFinding;

internal class PathFinder
{
    private IBlockAccess _worldMap;
    private readonly Path _path = new();
    private readonly PathPoint[] _pointMap = new PathPoint[1024];
    private readonly PathPoint[] _pathOptions = new PathPoint[32];

    private readonly PathPoint[] _pointPool = new PathPoint[4096];
    private int _poolIndex;

    public PathFinder(World world)
    {
        _worldMap = world;
        for (int i = 0; i < _pointPool.Length; i++)
        {
            _pointPool[i] = new PathPoint(0, 0, 0);
        }
    }

    public void SetWorld(IBlockAccess worldMap)
    {
        _worldMap = worldMap;
    }

    internal PathEntity? CreateEntityPathTo(Entity entity, Entity target, float maxDistance)
    {
        return CreateEntityPathTo(entity, target.x, target.boundingBox.MinY, target.z, maxDistance);
    }

    internal PathEntity? CreateEntityPathTo(Entity entity, int x, int y, int z, float maxDistance)
    {
        return CreateEntityPathTo(entity, x + 0.5f, y + 0.5f, z + 0.5f, maxDistance);
    }

    private PathEntity? CreateEntityPathTo(Entity entity, double targetX, double targetY, double targetZ,
        float maxDistance)
    {
        _path.ClearPath();
        Array.Clear(_pointMap, 0, _pointMap.Length);

        _poolIndex = 0;

        PathPoint startPoint = OpenPoint(MathHelper.Floor(entity.boundingBox.MinX),
            MathHelper.Floor(entity.boundingBox.MinY), MathHelper.Floor(entity.boundingBox.MinZ));
        PathPoint targetPoint = OpenPoint(MathHelper.Floor(targetX - (entity.width / 2.0f)), MathHelper.Floor(targetY),
            MathHelper.Floor(targetZ - (entity.width / 2.0f)));

        PathPoint sizePoint = new(MathHelper.Floor(entity.width + 1.0f), MathHelper.Floor(entity.height + 1.0f),
            MathHelper.Floor(entity.width + 1.0f));

        return AddToPath(entity, startPoint, targetPoint, sizePoint, maxDistance);
    }

    private PathEntity? AddToPath(Entity entity, PathPoint start, PathPoint target, PathPoint size, float maxDistance)
    {
        start.TotalPathDistance = 0.0f;
        start.DistanceToNext = start.DistanceTo(target);
        start.DistanceToTarget = start.DistanceToNext;

        _path.ClearPath();
        _path.AddPoint(start);

        PathPoint closestPoint = start;

        int iterations = 0;
        int iterationLimit = 4096;

        while (!_path.IsPathEmpty())
        {
            if (iterations++ > iterationLimit) break;

            PathPoint current = _path.Dequeue();

            if (current.Equals(target))
            {
                return CreateEntityPath(start, target);
            }

            if (current.DistanceTo(target) < closestPoint.DistanceTo(target))
            {
                closestPoint = current;
            }

            current.IsFirst = true;
            int optionCount = FindPathOptions(entity, current, size, target, maxDistance);

            for (int i = 0; i < optionCount; ++i)
            {
                PathPoint option = _pathOptions[i];
                float totalDistance = current.TotalPathDistance + current.DistanceTo(option);

                if (!option.IsAssigned() || totalDistance < option.TotalPathDistance)
                {
                    option.Previous = current;
                    option.TotalPathDistance = totalDistance;
                    option.DistanceToNext = option.DistanceTo(target);

                    if (option.IsAssigned())
                    {
                        _path.ChangeDistance(option, option.TotalPathDistance + option.DistanceToNext);
                    }
                    else
                    {
                        option.DistanceToTarget = option.TotalPathDistance + option.DistanceToNext;
                        _path.AddPoint(option);
                    }
                }
            }
        }

        if (closestPoint == start)
        {
            return null;
        }

        return CreateEntityPath(start, closestPoint);
    }

    private int FindPathOptions(Entity entity, PathPoint current, PathPoint size, PathPoint target, float maxDistance)
    {
        int optionCount = 0;
        byte stepUp = 0;

        if (GetVerticalOffset(entity, current.X, current.Y + 1, current.Z, size) == 1)
        {
            stepUp = 1;
        }

        PathPoint? pointSouth = GetSafePoint(entity, current.X, current.Y, current.Z + 1, size, stepUp);
        PathPoint? pointWest = GetSafePoint(entity, current.X - 1, current.Y, current.Z, size, stepUp);
        PathPoint? pointEast = GetSafePoint(entity, current.X + 1, current.Y, current.Z, size, stepUp);
        PathPoint? pointNorth = GetSafePoint(entity, current.X, current.Y, current.Z - 1, size, stepUp);

        if (pointSouth != null && !pointSouth.IsFirst && pointSouth.DistanceTo(target) < maxDistance)
            _pathOptions[optionCount++] = pointSouth;

        if (pointWest != null && !pointWest.IsFirst && pointWest.DistanceTo(target) < maxDistance)
            _pathOptions[optionCount++] = pointWest;

        if (pointEast != null && !pointEast.IsFirst && pointEast.DistanceTo(target) < maxDistance)
            _pathOptions[optionCount++] = pointEast;

        if (pointNorth != null && !pointNorth.IsFirst && pointNorth.DistanceTo(target) < maxDistance)
            _pathOptions[optionCount++] = pointNorth;

        return optionCount;
    }

    private PathPoint? GetSafePoint(Entity entity, int x, int y, int z, PathPoint size, int stepUp)
    {
        PathPoint? safePoint = null;

        if (GetVerticalOffset(entity, x, y, z, size) == 1)
        {
            safePoint = OpenPoint(x, y, z);
        }

        if (safePoint == null && stepUp > 0 && GetVerticalOffset(entity, x, y + stepUp, z, size) == 1)
        {
            safePoint = OpenPoint(x, y + stepUp, z);
            y += stepUp;
        }

        if (safePoint != null)
        {
            int fallDistance = 0;
            int offsetStatus = 0;

            while (y > 0)
            {
                offsetStatus = GetVerticalOffset(entity, x, y - 1, z, size);
                if (offsetStatus != 1)
                {
                    break;
                }

                fallDistance++;
                if (fallDistance >= 4)
                {
                    return null;
                }

                y--;
                if (y > 0)
                {
                    safePoint = OpenPoint(x, y, z);
                }
            }

            if (offsetStatus == -2)
            {
                return null;
            }
        }

        return safePoint;
    }

    private PathPoint OpenPoint(int x, int y, int z)
    {
        int hash = PathPoint.CalculateHash(x, y, z);
        int mapIndex = (hash & int.MaxValue) & 1023;

        PathPoint? point = _pointMap[mapIndex];
        while (point != null)
        {
            if (point.X == x && point.Y == y && point.Z == z)
            {
                return point;
            }

            point = point.NextMapNode;
        }

        if (_poolIndex < _pointPool.Length)
        {
            point = _pointPool[_poolIndex++];
            point.Init(x, y, z);
        }
        else
        {
            point = new PathPoint(x, y, z);
        }

        point.NextMapNode = _pointMap[mapIndex];
        _pointMap[mapIndex] = point;

        return point;
    }

    private int GetVerticalOffset(Entity entity, int x, int y, int z, PathPoint size)
    {
        for (int ix = x; ix < x + size.X; ++ix)
        {
            for (int iy = y; iy < y + size.Y; ++iy)
            {
                for (int iz = z; iz < z + size.Z; ++iz)
                {
                    int blockId = _worldMap.getBlockId(ix, iy, iz);
                    if (blockId > 0)
                    {
                        if (blockId != Block.IronDoor.id && blockId != Block.Door.id)
                        {
                            Material material = Block.Blocks[blockId].material;
                            if (material.BlocksMovement) return 0;
                            if (material == Material.Water) return -1;
                            if (material == Material.Lava) return -2;
                        }
                        else
                        {
                            int meta = _worldMap.getBlockMeta(ix, iy, iz);
                            if (!BlockDoor.isOpen(meta))
                            {
                                return 0;
                            }
                        }
                    }
                }
            }
        }

        return 1;
    }

    private PathEntity CreateEntityPath(PathPoint start, PathPoint end)
    {
        int length = 1;
        PathPoint current = end;

        while (current.Previous != null)
        {
            length++;
            current = current.Previous;
        }

        PathPoint[] pathPoints = new PathPoint[length];
        current = end;
        length--;

        pathPoints[length] = new PathPoint(end.X, end.Y, end.Z);

        while (current.Previous != null)
        {
            current = current.Previous;
            length--;

            pathPoints[length] = new PathPoint(current.X, current.Y, current.Z);
        }

        return new PathEntity(pathPoints);
    }
}
